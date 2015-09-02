// -----------------------------------------------------------------------
// <copyright file="ProgramHelper.cs"
//           project="Sonar-Git-Analyzer"
//           company="Mairegger Michael"
//           webpage="http://michaelmairegger.wordpress.com">
//     Copyright © Mairegger Michael, 2015 
//     All rights reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Sonar_Git_Analyzer
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Octokit;
    using Sonar_Git_Analyzer.Util;

    internal class ProgramHelper
    {
        private readonly ArgumentHelper _argHelper;
        private readonly Configuration _configuration;
        private bool _fistRun = true;
        private GitHubHelper _github;

        public ProgramHelper(ArgumentHelper argHelper, Configuration customConfigurationFile)
        {
            _argHelper = argHelper;
            _configuration = customConfigurationFile;
        }

        public async Task LoopFetchAndAnalyze()
        {
            _github = new GitHubHelper(_configuration, _argHelper);
            if (_configuration.RescanFrequency > TimeSpan.Zero)
            {
                Console.WriteLine("Rescan every {0:dd}d {0:hh} h {0:mm}m {0:ss}s", _configuration.RescanFrequency);

                while (true)
                {
                    var nextRescanTime = _github.GetNextApiResetTime().Result;
                    if (!nextRescanTime.HasValue)
                    {
                        await ProcessAndHandle();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Next rescan at {0}", DateTime.Now + _configuration.RescanFrequency);
                        Console.ResetColor();
                        await Task.Delay(_configuration.RescanFrequency);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Because of API-Request limitations for the current account the next scan has been rescheduled");

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Next rescan at {0}", nextRescanTime);
                        Console.ResetColor();
                        var timeSpan = nextRescanTime - DateTime.Now;
                        await Task.Delay(timeSpan.Value);
                    }
                }
            }

            await ProcessAndHandle();
        }

        private async Task DoWork()
        {
            var result = _github.FetchHistory(_argHelper.Fetch).Result;
            int commitCount = 0;
            if (_argHelper.Analyze)
            {
                var task = result.Where(i => i.CommitDateTime == DateTimeOffset.MinValue).ToList().Select(c =>
                                                                                                          {
                                                                                                              var commit = _configuration.CommitList.Single(i => i.SHA == c.SHA);

                                                                                                              return _github.SetCommitDate(commit).ContinueWith(t => { c.CommitDateTime = commit.CommitDateTime; });
                                                                                                          });

                await Task.WhenAll(task);

                foreach (var applicationState in result.OrderBy(i => i.CommitDateTime))
                {
                    var commit = _configuration.CommitList.Single(i => i.SHA == applicationState.SHA);
                    commitCount++;

                    if (commit.IsAnalyzed)
                    {
                        if (_fistRun)
                        {
                            Console.WriteLine("Skip analyzing: {0} (v{1})\t", commit.SHA, commit.Version);
                        }

                        continue;
                    }

                    if (SonarRunner.Instance.Execute(_configuration, commit))
                    {
                        Console.WriteLine("{0} out of {1} commits analyzed", commitCount, result.Count());
                        _configuration.Save();
                    }
                }

                _fistRun = false;
            }
        }

        private async Task ProcessAndHandle()
        {
            try
            {
                await DoWork();
            }
            catch (Exception ex)
            {
                var apiError = ex.InnerException as ApiException;
                if (apiError != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(apiError.Message);

                    foreach (var keyValuePair in apiError.HttpResponse.Headers.Where(i => i.Key.StartsWith("X-RateLimit")))
                    {
                        if (keyValuePair.Key == "X-RateLimit-Reset")
                        {
                            var dateTime = DateTime.Today + new TimeSpan(long.Parse(keyValuePair.Value));
                            Console.WriteLine("{0} {1}", keyValuePair.Key, dateTime);
                        }
                        else
                        {
                            Console.WriteLine("{0} {1}", keyValuePair.Key, keyValuePair.Value);
                        }
                    }

                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("FATAL ERROR");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.ToString());
                    Environment.Exit(1);
                }
            }
        }
    }
}
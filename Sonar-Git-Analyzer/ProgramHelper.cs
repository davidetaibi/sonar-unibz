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
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Octokit;
    using Sonar_Git_Analyzer.Util;

    internal class ProgramHelper
    {
        private bool _fistRun = true;
        private GitHubHelper _github;
        private readonly ArgumentHelper _argHelper;
        private readonly Configuration _configuration;

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
                        Console.ForegroundColor = ConsoleColor.DarkRed;
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
            if (string.IsNullOrEmpty(_argHelper.ConfigurationFile))
            {
                Console.WriteLine("Configuration file missing");
            }

            var result = _github.FetchHistory(_argHelper.Fetch).Result;
            int commitCount = 0;
            if (_argHelper.Analyze)
            {
                foreach (var applicationState in result)
                {
                    commitCount++;

                    var commit = _configuration.SHAs.SingleOrDefault(i => i.SHA == applicationState.SHA);

                    if (commit == null)
                    {
                        _configuration.SHAs.Add(applicationState);
                        commit = applicationState;
                    }

                    if (commit.IsAnalyzed)
                    {
                        if (_fistRun)
                        {
                            Console.WriteLine("Skip analyzing: {0} (v{1})\t", commit.SHA, commit.Version);
                        }

                        continue;
                    }

                    await _github.SetCommitDate(commit);

                    if (!SonarRunner.Instance.Execute(_configuration, commit))
                    {
                        return;
                    }
                    
                    Console.WriteLine("{0} out of {1} commits analyzed", commitCount, result.Count());
                    File.WriteAllText(_argHelper.ConfigurationFile, JsonConvert.SerializeObject(_configuration, Formatting.Indented));
                }

                _fistRun = false;
            }
        }

        private async Task ProcessAndHandle()
        {
            try
            {
                await DoWork();
                //await DoWork();
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
                    Environment.Exit(1);
                }
            }
        }
    }
}
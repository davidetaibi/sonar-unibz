// -----------------------------------------------------------------------
// <copyright file="Program.cs"
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
    using CommandLine;
    using Newtonsoft.Json;
    using Octokit;
    using Sonar_Git_Analyzer.Util;

    internal static class Program
    {
        private static bool _fistRun = true;
        private static GitHubHelper _github;

        private static async Task Process(ArgumentHelper helper, Configuration configuration)
        {
            if (string.IsNullOrEmpty(helper.ConfigurationFile))
            {
                Console.WriteLine("Configuration file missing");
            }

            //WriteFetch();
            var result = _github.FetchHistory(helper.Fetch).Result;
            int commitCount = 0;
            if (helper.Analyze)
            {
                //WriteAnalyze();

                foreach (var applicationState in result)
                {
                    commitCount++;

                    var commit = configuration.SHAs.SingleOrDefault(i => i.SHA == applicationState.SHA);

                    if (commit == null)
                    {
                        configuration.SHAs.Add(applicationState);
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

                    if (!SonarRunner.Execute(configuration, commit))
                    {
                        return;
                    }

                    Console.WriteLine("{0} out of {1} commits analyzed", commitCount, result.Count());


                    File.WriteAllText(helper.ConfigurationFile, JsonConvert.SerializeObject(configuration, Formatting.Indented));
                }

                _fistRun = false;
            }
        }

        private static void Main(string[] args)
        {
            ArgumentHelper argHelper = new ArgumentHelper();

            if (!Parser.Default.ParseArguments(args, argHelper))
            {
                Environment.Exit(1);
            }

            var readAllText = File.ReadAllText(argHelper.ConfigurationFile);
            var configuration = JsonConvert.DeserializeObject<Configuration>(readAllText);

            _github = new GitHubHelper(configuration, argHelper);
            if (configuration.RescanFrequency > TimeSpan.Zero)
            {
                Console.WriteLine("Rescan every {0:dd}d {0:hh} h {0:mm}m {0:ss}s", configuration.RescanFrequency);

                while (true)
                {
                    var nextRescanTime = _github.GetNextAPIResetTime().Result;
                    if (!nextRescanTime.HasValue)
                    {
                        ProcessAndHandle(argHelper, configuration).Wait();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Next rescan at {0}", DateTime.Now + configuration.RescanFrequency);
                        Console.ResetColor();
                        Task.Delay(configuration.RescanFrequency).Wait();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine("Because of API-Request limitations for the current account the next scan has been rescheduled");

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Next rescan at {0}", nextRescanTime);
                        Console.ResetColor();
                        var timeSpan = nextRescanTime - DateTime.Now;
                        Task.Delay(timeSpan.Value).Wait();
                    }
                }
            }

            ProcessAndHandle(argHelper, configuration).Wait();
        }

        private static async Task ProcessAndHandle(ArgumentHelper helper, Configuration configuration)
        {
            try
            {
                await Process(helper, configuration);
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

        private static void WriteAnalyze()
        {
            if (!_fistRun)
            {
                return;
            }

            Console.WriteLine();
            Console.WriteLine("      AA       NN    NN       AA       LL       YY    YY ZZZZZZZZ EEEEEEEE");
            Console.WriteLine("     AAAA      NNN   NN      AAAA      LL        YY  YY       ZZ  EE");
            Console.WriteLine("    AA  AA     NNNN  NN     AA  AA     LL         YYYY       ZZ   EE");
            Console.WriteLine("   AAAAAAAA    NN NN NN    AAAAAAAA    LL          YY       ZZ    EEEEEEEE");
            Console.WriteLine("  AA      AA   NN  NNNN   AA      AA   LL         YY       ZZ     EE");
            Console.WriteLine(" AA        AA  NN   NNN  AA        AA  LL        YY       ZZ      EE");
            Console.WriteLine("AA          AA NN    NN AA          AA LLLLLLLL YY       ZZZZZZZZ EEEEEEEE");
            Console.WriteLine();
        }

        private static void WriteFetch()
        {
            Console.WriteLine();
            Console.WriteLine("FFFFFFFF EEEEEEEE TTTTTTTT    CCCCC HH    HH");
            Console.WriteLine("FF       EE          TT      CC     HH    HH");
            Console.WriteLine("FF       EE          TT     CC      HH    HH");
            Console.WriteLine("FFFF     EEEEEEEE    TT    CC       HHHHHHHH");
            Console.WriteLine("FF       EE          TT     CC      HH    HH");
            Console.WriteLine("FF       EE          TT      CC     HH    HH");
            Console.WriteLine("FF       EEEEEEEE    TT       CCCCC HH    HH");
            Console.WriteLine();
        }
    }
}
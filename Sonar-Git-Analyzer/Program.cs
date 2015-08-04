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
    using Newtonsoft.Json;
    using Sonar_Git_Analyzer.Util;

    internal class Program
    {
        private static bool _fistRun = true;
        private static GitHubHelper _github;

        public static async Task Process(ArgumentHelper helper, Configuration configuration)
        {
            if (string.IsNullOrEmpty(helper.ConfigurationFile))
            {
                Console.WriteLine("Configuration file missing");
            }

            WriteFetch();
            var result = _github.FetchHistory(helper.Fetch).Result;
            int commitCount = 0;
            if (helper.Analyze)
            {
                WriteAnalyze();

                foreach (var applicationState in result)
                {
                    commitCount++;

                    var commit = configuration.SHAs.SingleOrDefault(i => i.SHA == applicationState.SHA);
                    if (commit != null && commit.IsAnalyzed)
                    {
                        if (_fistRun)
                        {
                            Console.WriteLine("Skip analyzing: {0} (v{1})\t", applicationState.SHA, applicationState.Version);
                        }

                        continue;
                    }

                    await _github.SetCommitDate(applicationState);

                    if (!SonarRunner.Execute(configuration, applicationState))
                    {
                        return;
                    }

                    Console.WriteLine("{0} out of {1} commits analyzed", commitCount, result.Count());

                    configuration.SHAs.Add(applicationState);

                    File.WriteAllText(helper.ConfigurationFile, JsonConvert.SerializeObject(configuration, Formatting.Indented));
                }

                _fistRun = false;
            }
        }

        private static void Main(string[] args)
        {
            ArgumentHelper argHelper = new ArgumentHelper();

            foreach (var arg in args)
            {
                if (arg.Equals("-f"))
                {
                    argHelper.Fetch = true;
                }

                if (arg.Equals("-a"))
                {
                    argHelper.Analyze = true;
                }

                if (arg.StartsWith("-c:"))
                {
                    argHelper.ConfigurationFile = arg.Substring(3);
                }
            }

            var readAllText = File.ReadAllText(argHelper.ConfigurationFile);
            var configuration = JsonConvert.DeserializeObject<Configuration>(readAllText);

            _github = new GitHubHelper(configuration);

            if (configuration.RescanFrequency > TimeSpan.Zero)
            {
                Console.WriteLine("Rescan every {0:dd}d {0:hh} h {0:mm}m {0:ss}s", configuration.RescanFrequency);

                while (true)
                {
                    Process(argHelper, configuration).Wait();
                    Console.WriteLine("Next rescan at {0}", DateTime.Now + configuration.RescanFrequency);
                    Task.Delay(configuration.RescanFrequency).Wait();
                }
            }

            Process(argHelper, configuration).Wait();
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
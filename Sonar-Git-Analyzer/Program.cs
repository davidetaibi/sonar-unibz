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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using CommandLine;
    using Newtonsoft.Json;
    using Sonar_Git_Analyzer.Util;

    internal static class Program
    {
        /// <summary>
        /// Finds Sonar-Analyzer-Configuration files in the current directory.
        /// </summary>
        /// <returns></returns>
        private static IList<Configuration> FindConfigurationFiles(ArgumentHelper arg)
        {
            IEnumerable<string> enumerable = new List<string>();
            var currentDirectory = new DirectoryInfo(Path.GetFullPath("config"));

            if (!string.IsNullOrEmpty(arg.ConfigurationFile))
            {
                enumerable = new List<string> {arg.ConfigurationFile};
            }
            else if (currentDirectory.Exists)
            {
                enumerable = currentDirectory.GetFiles("*.json").Select(file => file.FullName);
                //TODO: Validate content of json.
            }

            return enumerable.Select(i =>
            {
                var readAllText = File.ReadAllText(i);
                try
                {
                    var deserializeObject = JsonConvert.DeserializeObject<Configuration>(readAllText);
                    return deserializeObject;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in reading configuration file {0}\n\n{1}", i, ex.Message);
                    return null;
                }
            }).Where(i => i != null && i.Validate()).ToList();


        }

        private static void Main(string[] args)
        {
            var parser = Parser.Default.ParseArguments<ArgumentHelper>(args);
            Environment.ExitCode = parser.Return(helper => 0, errors => 1);

            List<ProgramHelper> programHelper = null;
            ArgumentHelper config = null;
            parser.WithParsed(helper =>
                              {
                                  config = helper;
                                  SonarRunner.CreateInstance(config);
                                  var foundConfigurationFiles = FindConfigurationFiles(config);
                                  if (foundConfigurationFiles != null)
                                  {
                                      programHelper = foundConfigurationFiles.Select(i => new ProgramHelper(helper, i)).ToList();
                                  }

                              });

            if (parser.Tag == ParserResultType.Parsed && programHelper != null)
            {
                Task.WhenAll(programHelper.Select(i => i.LoopFetchAndAnalyze())).Wait();
            }
        }
    }
}
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
    using Newtonsoft.Json;
    using System.Threading.Tasks;
    using System.Linq;

    internal class Program
    {
        private static void Main(string[] args)
        {
            ArgumentHelper argHelper = new ArgumentHelper();
             
            foreach(var arg in args)
            {
                if (arg.Equals("-f"))
                {
                    argHelper.Fetch = true;
                }
                if (arg.Equals("-a"))
                {
                    argHelper.Analyze = true;
                }
                if(arg.StartsWith("-c:"))
                {
                    argHelper.ConfigurationFile = arg.Substring(3);
                }

            }

            var errorCode = Process(argHelper).Result;
        }

        public static Task<int> Process(ArgumentHelper helper)
        {
            if(string.IsNullOrEmpty(helper.ConfigurationFile))
            {
                Console.WriteLine("Configuration file missing");

            }

            var readAllText = File.ReadAllText(helper.ConfigurationFile);
            var configuration = JsonConvert.DeserializeObject<Configuration>(readAllText);

            var result = GitHubHelper.FetchHistory(configuration, helper.Fetch).Result;
            if(helper.Analyze)
            {
                int commitCount = 1;
                foreach (var applicationState in result)
                {
                    SonarRunner.Execute(configuration, applicationState);
                    Console.WriteLine("{0} out of {1} commits analyzed", commitCount++, result.Count());
                }
            }

            return Task.FromResult(0);

        }

        
    }
    public class ArgumentHelper
    {
        public bool Fetch { get; set; }
        public bool Analyze { get; set; }
        public string ConfigurationFile { get; set; }
    }
}
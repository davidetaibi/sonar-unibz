// -----------------------------------------------------------------------
// <copyright file="ArgumentHelper.cs"
//           project="Sonar-Git-Analyzer"
//           company="Mairegger Michael"
//           webpage="http://michaelmairegger.wordpress.com">
//     Copyright © Mairegger Michael, 2015 
//     All rights reserved
// </copyright>
// -----------------------------------------------------------------------
namespace Sonar_Git_Analyzer.Util
{
    using CommandLine;

    public class ArgumentHelper
    {
        
        [VerbOption("analyze", HelpText = "Specify whether to analyze the source.")]
        public bool Analyze { get; set; }

        [Option("config", HelpText = "The location of the configuration file.", Required = true)]
        public string ConfigurationFile { get; set; }

        [VerbOption("fetch", HelpText = "Specify whether to download the source.")]
        public bool Fetch { get; set; }

        [Option("user", Required = false)]
        public string GitHubUserName { get; set; }

        [Option("password", Required = false)]
        public string Password { get; set; }
        [Option("token", Required = false)]
        public string Token { get; set; }
    }
}
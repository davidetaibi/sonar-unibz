namespace Sonar_Git_Analyzer.Util
{
    using System.IO;
    using CommandLine;

    public class ArgumentHelper
    {
        [Option("analyse", HelpText = "Analyze the downloaded source.")]
        public bool Analyse { get; set; }

        [Option("anonymous", HelpText = "Anonymous authentication", SetName = "Anonymous", Required = true)]
        public bool Anonymous { get; set; }

        [Option("config", HelpText = "The location of the configuration file.", Required = false)]
        public string ConfigurationFile { get; set; }

        [Option("user", HelpText = "The GitHub username", SetName = "BasicAuthentication", Required = true)]
        public string GitHubUserName { get; set; }

        [Option("password", HelpText = "The GitHub password", SetName = "BasicAuthentication", Required = true)]
        public string Password { get; set; }

		[Option("sonar-runner", HelpText = "Path to the sonar-runner executable file", Required = false)]
        public FileInfo SonarRunnerPath { get; set; }

        [Option("token", HelpText = "The GitHub token", SetName = "OAuth", Required = true)]
        public string Token { get; set; }
    }
}
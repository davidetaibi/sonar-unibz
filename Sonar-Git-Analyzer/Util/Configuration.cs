// -----------------------------------------------------------------------
// <copyright file="Configuration.cs"
//           project="Sonar-Git-Analyzer"
//           company="Mairegger Michael"
//           webpage="http://michaelmairegger.wordpress.com">
//     Copyright © Mairegger Michael, 2015 
//     All rights reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Sonar_Git_Analyzer.Util
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [JsonObject]
    public class Configuration
    {
        private string _sonarProperties;

        public Configuration()
        {
            CommitList = new List<CommitHelper>();
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public AnalysationBehavior AnalysationBehavior { get; set; }

        [JsonProperty(Order = 100)]
        public List<CommitHelper> CommitList { get; set; }

        [JsonIgnore]
        public string DropLocation
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "drop"); }
        }

        [JsonProperty]
        public string GitHubRepository { get; set; }

        [JsonProperty]
        public string GitHubRepositoryOwner { get; set; }

        [JsonIgnore]
        public string InstanceConfigurationFile { get; set; }

        [JsonProperty]
        public DateTimeOffset LastSuccessfulAnalyzedCommit { get; set; }

        [JsonProperty]
        public TimeSpan RescanFrequency { get; set; }

        [JsonProperty]
        public string SonarProperties
        {
            get
            {
                if (string.IsNullOrEmpty(_sonarProperties))
                {
                    FileInfo fi = new FileInfo(InstanceConfigurationFile);
                    var index = fi.FullName.LastIndexOf(".json", StringComparison.InvariantCulture);
                    if (index > 0)
                    {
                        _sonarProperties = fi.FullName.Remove(index) + ".properties";
                    }
                }
                return _sonarProperties;
            }
            set { _sonarProperties = value; }
        }

        public void Save()
        {
            File.WriteAllText(InstanceConfigurationFile, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public bool Validate()
        {
            string errorMessage = string.Empty;

            try
            {
                new FileInfo(SonarProperties);
            }
            catch (Exception)
            {
                errorMessage += string.Format("ERROR {0}: SonarProperties invalid", InstanceConfigurationFile);
            }

            if (string.IsNullOrEmpty(GitHubRepositoryOwner) || string.IsNullOrEmpty(GitHubRepository))
            {
                errorMessage += string.Format("ERROR {0} : GitHubRepositoryOwner and/or GitHubRepository missing for configuration file", InstanceConfigurationFile);
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                return true;
            }
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine(errorMessage);
            Console.ResetColor();
            return false;
        }
    }
}
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
    using System.Runtime.Serialization;

    [DataContract]
    public class Configuration
    {
        public Configuration()
        {
            SHAs = new List<CommitHelper>();
        }

        [DataMember]
        public string DropLocation { get; set; }

        [DataMember]
        public string GitHubRepository { get; set; }

        [DataMember]
        public string GitHubRepositoryOwner { get; set; }

        [DataMember]
        public TimeSpan RescanFrequency { get; set; }

        [DataMember(IsRequired = false, Order = 100)]
        public List<CommitHelper> SHAs { get; set; }

        [DataMember]
        public string SonarProperties { get; set; }

        [DataMember]
        public CommitOnlyAnalyzer CommitAnalyzer { get; set; }

        [IgnoreDataMember]
        public string InstanceConfigurationFile { get; set; }

        public bool Validate()
        {
            string errorMessage = string.Empty;

            try
            {
                new FileInfo(DropLocation);
            }
            catch (Exception ex)
            {
                errorMessage += string.Format("ERROR {0}: DropLocation invalid\n\n{1}", InstanceConfigurationFile, ex.Message);
            }

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
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(errorMessage);
            Console.ResetColor();
            return false;
        }
    }
}
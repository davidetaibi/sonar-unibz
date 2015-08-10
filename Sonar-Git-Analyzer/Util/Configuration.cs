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

        [DataMember(IsRequired = false)]
        public List<CommitHelper> SHAs { get; set; }

        [DataMember]
        public string SonarProperties { get; set; }

        [DataMember]
        public string SonarRunnerPath { get; set; }
    }
}
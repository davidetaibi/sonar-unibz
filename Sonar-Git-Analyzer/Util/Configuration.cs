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
        private List<CommitHelper> _analyzedShAs = new List<CommitHelper>();

        [DataMember(IsRequired = false)]
        public List<CommitHelper> AnalyzedSHAs
        {
            get { return _analyzedShAs; }
            set { _analyzedShAs = value; }
        }

        [DataMember]
        public string Branch { get; set; }

        [DataMember]
        public string DropLocation { get; set; }

        [DataMember]
        public string GitHubRepository { get; set; }

        [DataMember]
        public TimeSpan RescanFrequency { get; set; }

        [DataMember]
        public string SonarProperties { get; set; }

        [DataMember]
        public string SonarRunnerPath { get; set; }
    }
}
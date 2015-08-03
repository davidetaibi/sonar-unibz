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
    using System.Runtime.Serialization;

    [DataContract]
    public class Configuration
    {
        [DataMember]
        public string Branch { get; set; }

        [DataMember]
        public string DropLocation { get; set; }

        [DataMember]
        public string GitHubRepository { get; set; }

        [DataMember]
        public string SonarRunnerPath { get; set; }

        [DataMember]
        public string SonarProperties { get; set; }
    }
}
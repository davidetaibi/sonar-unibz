// -----------------------------------------------------------------------
// <copyright file="CommitHelper.cs"
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
    using System.Diagnostics;

    [DebuggerDisplay("{Version} - {SHA}")]
    public class CommitHelper
    {
        public string SHA { get; set; }

        public string Version { get; set; }

        public DateTime CommitDateTime { get; set; }

        public bool IsAnalyzed { get; set; }

        public string Url { get; set; }
    }
}
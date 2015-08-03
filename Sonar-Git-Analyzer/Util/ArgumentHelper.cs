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
    public class ArgumentHelper
    {
        public bool Analyze { get; set; }

        public string ConfigurationFile { get; set; }

        public bool Fetch { get; set; }
    }
}
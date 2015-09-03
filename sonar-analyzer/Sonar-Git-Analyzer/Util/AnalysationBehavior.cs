// -----------------------------------------------------------------------
// <copyright file="AnalyzationBehavior.cs"
//           project="Sonar-Git-Analyzer"
//           company="Mairegger Michael"
//           webpage="http://michaelmairegger.wordpress.com">
//     Copyright © Mairegger Michael, 2015 
//     All rights reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Sonar_Git_Analyzer.Util
{
    public enum AnalysationBehavior
    {
        Tags,
        All,
        Newest,
        FirstAllThenNewest
    }
}
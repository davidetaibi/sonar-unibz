namespace Sonar_Git_Analyzer.Util
{
    using System.Diagnostics;

    [DebuggerDisplay("{Version} - {SHA}")]
    internal class CommitHelper
    {
        public string SHA { get; set; }
        public string Version { get; set; }
    }
}
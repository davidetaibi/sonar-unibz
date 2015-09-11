namespace Sonar_Git_Analyzer.Util
{
    using System;
    using System.Diagnostics;
    using Newtonsoft.Json;

    [DebuggerDisplay("{Version} - {SHA} {CommitDateTime,nq}")]
    [JsonObject]
    public class CommitHelper
    {
        [JsonProperty]
        public DateTimeOffset CommitDateTime { get; set; }

        [JsonProperty]
        public bool IsAnalyzed { get; set; }

        [JsonProperty]
        public string SHA { get; set; }

        [JsonProperty]
        public string Url { get; set; }

        [JsonProperty]
        public string Version { get; set; }
    }
}
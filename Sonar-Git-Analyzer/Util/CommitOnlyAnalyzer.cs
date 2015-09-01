namespace Sonar_Git_Analyzer.Util
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [JsonObject]
    public class CommitOnlyAnalyzer
    {
        [JsonProperty]
        public DateTimeOffset LastCommitDate { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public AnalyzationBehavior AnalyzationBehavior { get; set; }
    }
}
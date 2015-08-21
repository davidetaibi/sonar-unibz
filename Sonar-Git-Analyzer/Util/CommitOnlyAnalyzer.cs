namespace Sonar_Git_Analyzer.Util
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public class CommitOnlyAnalyzer
    {
        [DataMember]
        public DateTimeOffset LastCommitDate { get; set; }

        [DataMember]
        public AnalyzationBehavior AnalyzationBehavior { get; set; }
    }
}
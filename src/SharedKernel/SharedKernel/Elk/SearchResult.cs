using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LSG.SharedKernel.Elk
{
    public class Shards
    {
        public int total { get; set; }
        public int successful { get; set; }
        public int skipped { get; set; }
        public int failed { get; set; }
    }

    public class Total
    {
        public int value { get; set; }
        public string relation { get; set; }
    }

    public class Fields
    {
        public string SourceContext { get; set; }
        public long OperatorTime { get; set; }
        public long MessageTime { get; set; }
        public string Operator { get; set; }
        public string Message { get; set; }
        public string PlayerExternalId { get; set; }
        public string RoomId { get; set; }
        public string Environment { get; set; }
        public string Server { get; set; }
        public string Site { get; set; }
    }

    public class Source
    {
        [JsonPropertyName("@timestamp")] public DateTime Timestamp { get; set; }
        public string level { get; set; }
        public string messageTemplate { get; set; }
        public string message { get; set; }
        public Fields fields { get; set; }
    }

    public class Hit
    {
        public string _index { get; set; }
        public string _type { get; set; }
        public string _id { get; set; }
        public double _score { get; set; }
        public Source _source { get; set; }
    }

    public class Hits
    {
        public Total total { get; set; }

        public List<Hit> hits { get; set; }
    }

    public class SearchResult
    {
        public int took { get; set; }
        public bool timed_out { get; set; }
        public Shards _shards { get; set; }
        public Hits hits { get; set; }
    }
}
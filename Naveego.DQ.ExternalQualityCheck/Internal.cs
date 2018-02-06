
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;

namespace Naveego.DQ.ExternalQualityCheck.Internal
{

    internal class GuidReference
    {
        public string Key { get; set; }
        public string Name { get; set; }
    }

    internal class RunExceptionDTO
    {
        public Guid Id { get; set; }
        public Internal.GuidReference Run { get; set; }

        [JsonProperty("ts")]
        public DateTime Timestamp { get; set; }

        public DateTime RunStartedAt { get; set; }

        public string Key { get; set; }

        public string Label { get; set; }

        public string Description { get; set; }

        public JObject Data { get; set; }

        public int Sequence { get; set; }
    }

    internal class RunDTO
    {
        public string Id { get; set; }

        public string QueryId { get; set; }

        public string Status { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime FinishedAt { get; set; }

        public long QueryTime { get; set; }

        public int Population { get; set; }

        /// <summary>
        /// Configuration of the run. This is set from <see cref="Query.QueryText"/>
        /// when the run is created.
        /// </summary>
        public string Configuration { get; set; }

        public int ExceptionCount { get; set; }

        public string State { get; set; }

        public string ErrorMessage { get; set; }
    }

}
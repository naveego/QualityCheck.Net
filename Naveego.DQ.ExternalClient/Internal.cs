
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;

namespace Naveego.DQ.ExternalClient.Internal
{

    internal class RunExceptionDTO
    {

        public Guid Id { get; set; }

        [JsonProperty("ts")]
        public DateTime Timestamp { get; set; }

        public DateTime RunStartedAt { get; set; }

        public string Key { get; set; }

        public string Label { get; set; }

        public string Description { get; set; }

        public JObject Data { get; set; }

        public GuidReference Query { get; set; }

        public GuidReference Run { get; set; }

        public GuidReference Source { get; set; }

        public GuidReference SyncClient { get; set; }

        public GuidReference Rule { get; set; }

        public int Sequence { get; set; }
    }

    internal class RunDTO
    {
        public string Id { get; set; }

        //[EnumSearchTerms(new[] { "new", "open", "acknowledged", "dismissed", "suppressed", "failed" })]
        public string Status { get; set; }

        //[EnumSearchTerms(new[] { "queued", "running", "complete" })]
        public string State { get; set; }

        //[EnumSearchTerms(new [] {  "transfering", "complete" })]
        public string TransferState { get; set; }

        public string Indicator { get; set; }

        public string Severity { get; set; }

        public string Impact { get; set; }

        public string Class { get; set; }

        public string Category { get; set; }

        public string Object { get; set; }

        public string Property { get; set; }

        public string AssignedTo { get; set; }

        public string CheckId { get; set; }

        public DateTime ScheduleStart { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? TransferCompletedAt { get; set; }

        public int Population { get; set; }

        public int ExceptionCount { get; set; }

        public int CommentCount { get; set; }

        public long QueryTime { get; set; }

        public string StatusChangedBy { get; set; }

        public DateTime? StatusChangedDate { get; set; }

        public string KeyColumn { get; set; }

        public string LabelColumn { get; set; }

        public string DescriptionColumn { get; set; }

        public JObject[] DetailColumns { get; set; }

        public string QueryText { get; set; }

        public string CountQueryText { get; set; }

        public string ErrorMessage { get; set; }

        public string[] Tags { get; set; }

        public Guid? SuppressedBy { get; set; }


        public GuidReference Query { get; set; }

        public GuidReference Rule { get; set; }

        public GuidReference Source { get; set; }

        public GuidReference SyncClient { get; set; }
    }

    internal class GuidReference
    {
        public string Name { get; set; }
        public string Key { get; set; }
    }


    internal class QueryDTO
    {

        public Guid Id { get; set; }

        public string Name { get; set; }

        public string QueryText { get; set; }

        public string Description { get; set; }

        public string Resolution { get; set; }

        public string CountQueryText { get; set; }

        public string KeyColumn { get; set; }

        public string LabelColumn { get; set; }

        [JsonProperty("descColumn")]
        public string DescriptionColumn { get; set; }

        public string CountColumn { get; set; }

        public string DataOwner { get; set; }

        //public RunSchedule Schedule { get; set; }

        public GuidReference Source { get; set; }

        public GuidReference Rule { get; set; }

        public JObject[] QueryProperties { get; set; }

        public JObject[] CountQueryProperties { get; set; }

        public int RunCount { get; set; }

        public DateTime? LastRun { get; set; }

        public string LastRunStatus { get; set; }

        public string LastRunMessage { get; set; }

        public DateTime NextRun { get; set; }

        public int ExceptionCount { get; set; }

        public DateTime? LastException { get; set; }

    }
}
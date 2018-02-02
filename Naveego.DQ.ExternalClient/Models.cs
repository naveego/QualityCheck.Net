
using System;
using Newtonsoft.Json.Linq;

namespace Naveego.DQ.ExternalClient
{
    public class RunException
    {
        public DateTime Timestamp { get; set; }

        public string Key { get; set; }

        public string Label { get; set; }

        public string Description { get; set; }

        public JObject Data { get; set; }

        public int Sequence { get; set; }
    }

}
using System;
using System.Linq;
using Naveego.DQ.ExternalClient;
using Newtonsoft.Json.Linq;

namespace ExternalQualityCheckExample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var client = new Client("http://api.naveegonext.com", "token");

            using (var run = client.StartQualityCheck("quality-check-id"))
            {

                for (int i = 0; i < 100; i += 10)
                {
                    var exceptions = Enumerable.Range(i, 10)
                        .Select(seq => new RunException
                        {
                            Data = JObject.Parse(@"{data: ""something""}"),
                            Description = "Description text",
                            Timestamp = DateTime.Now,
                            Key = "key text",
                            Label = "label text",
                            Sequence = seq
                        })
                        .ToArray();

                    run.SendExceptions(exceptions);
                }

            }
        }
    }
}

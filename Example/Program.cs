using System;
using System.Linq;
using System.Threading;
using Naveego.DQ.ExternalClient;
using Newtonsoft.Json.Linq;

namespace ExternalQualityCheckExample
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Expected arguments: <endpoint> <token> <quality check id>");
                return -1;
            }

            var endpoint = args[0];
            var token = args[1];
            var qualityCheckID = args[2];

            // Create a Client to communicate with the quality check API:
            var client = new Client(endpoint, token);

            // You can provide a callback which will be used for logging.
            client.Logger = (msg) => Console.WriteLine((msg.Length < 200 ? msg : msg.Substring(0, 200) + "..."));

            // Start a quality check run by calling StartQualityCheckRun, passing
            // in the ID of the quality check this run belongs to.
            // When the QualityCheckRun is disposed, it will wait until all
            // exceptions have been sent to the API, then tell the API
            // that the run is complete. If there are a large number of exceptions
            // it may take a long time to flush the queue.
            using (var run = client.StartQualityCheckRun(qualityCheckID))
            {
                var exceptionsCount = 10;

                // The QualityCheckRun.Configuration property is a string that 
                // can be set using the Configuration tab in the web UI while editing the quality check.
                // You can put any data in that string. In this example, we will check for 
                // some JSON like { "exceptionsCount": 17 } and override our default setting
                // if we find it.
                var configuration = run.Configuration;
                if (configuration != null)
                {
                    var jObject = JObject.Parse(configuration);
                    if (jObject["exceptionsCount"] != null)
                    {
                        exceptionsCount = jObject["exceptionsCount"].Value<int>();
                        Console.WriteLine($"Generating about {exceptionsCount} exceptions based on configuration.");
                    }
                }

                // Set the population count when you know how many total records
                // you are examining. Here we are going to pretend that
                // there are 5 times as many records as exceptions.
                run.SetPopulationCount(exceptionsCount * 5);

                try
                {
                    for (int i = 0; i < exceptionsCount; i += 2)
                    {
                        Console.WriteLine($"Creating exceptions {i} and {i + 1}...");

                        // Here we are simulating a process which generates quality check exceptions...
                        var exceptions = Enumerable.Range(i, 2)
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

                        // ... which we then send to the API using the SendExceptions method.
                        // You can send any number of exceptions at a time.
                        run.SendExceptions(exceptions);
                        Console.WriteLine($"Exceptions sent, going to sleep for a little while.");
                        Thread.Sleep(500);
                    }
                }
                catch (Exception ex)
                {
                    // If the process you're running to produce quality check exceptions 
                    // fails, you can record that it failed by passing a message to the
                    // Failed method on the run. After you've called Failed you should
                    // not send any more quality check exceptions.
                    run.Failed("Failed because of an exception: " + ex);

                    throw;
                }

                Console.WriteLine("Done sending exceptions.");
            }

            Console.WriteLine("Exiting.");


            return 0;
        }
    }
}

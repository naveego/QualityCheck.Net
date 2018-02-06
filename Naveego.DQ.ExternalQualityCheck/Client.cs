using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using Naveego.DQ.ExternalQualityCheck.Internal;
using Newtonsoft.Json.Converters;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace Naveego.DQ.ExternalQualityCheck
{
    /// <summary>
    /// Transport is used for communicating with the API.
    /// It must be able to take a method, a path, and (for PUT and POST methods) some JSON,    
    /// transmit that to the API, and return the response as a string.
    /// </summary>
    /// <param name="method"></param>
    /// <param name="path"></param>
    /// <param name="json"></param>
    /// <returns></returns>
    public delegate string Transport(string method, string path, string json);

    public class Client
    {
        private static readonly JsonSerializerSettings jsonSerializerSettings
            = new JsonSerializerSettings()
            {
                Converters = {
                    new StringEnumConverter { CamelCaseText = true }
                },
                Formatting = Formatting.None,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
            };

        public Action<string> Logger { get; set; }

        public Transport Transport { get; set; }

        /// <summary>
        /// Create a Client which will use the default transport system,
        /// configured with the provided values.
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="token"></param>
        /// <param name="proxyUrl"></param>
        /// <param name="proxyPort"></param>
        public Client(string baseUrl, string token, string proxyUrl = null, int proxyPort = 80)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentException("baseUrl is required", nameof(baseUrl));
            }

            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("token is required", nameof(token));
            }

            baseUrl = baseUrl.TrimEnd('/');

            this.Transport = (method, path, json) =>
            {
                using (var wc = new System.Net.WebClient())
                {
                    if (proxyUrl != null)
                    {
                        wc.Proxy = new WebProxy(proxyUrl, proxyPort);
                    }
                    else
                    {
                        wc.Proxy = null;
                    }

                    wc.Headers.Add("Authorization", string.Format("Bearer {0}", token));

                    var url = baseUrl + path;

                    return wc.UploadString(url, method, json);
                }
            };
        }

        /// <summary>
        /// Create a client, providing a custom <see cname="Transport"/>.
        /// </summary>
        /// <param name="transport"></param>
        public Client(Transport transport)
        {
            this.Transport = transport;
        }

        /// <summary>
        /// Start a QualityCheckRun using the provided <paramref name="qualityCheckId"/>.
        /// This should always be invoked in a <c>using</c> statement.
        /// </summary>
        /// <param name="qualityCheckId"></param>
        /// <returns></returns>
        public QualityCheckRun StartQualityCheckRun(string qualityCheckId)
        {
            Log($"[StartQualityCheckRun] qualityCheckId: {qualityCheckId}");

            // create run and populate from quality check
            var run = new RunDTO
            {

                Id = Guid.NewGuid().ToString("D"),
                QueryId = qualityCheckId,
                State = "running",
                Status = "new",
                StartedAt = DateTime.UtcNow,
            };

            this.Send("POST", "/v3/dataquality/runs/external", run);

            var qualityCheckRun = new QualityCheckRun(this, run);

            return qualityCheckRun;
        }

        internal void Send(string method, string path, object data)
        {
            var json = JsonConvert.SerializeObject(data, jsonSerializerSettings);
            Log($"[Send] method: {method}, path:{path}, data: {json}");
            this.Transport(method, path, json);
        }

        internal void Log(string message)
        {
            this.Logger?.Invoke(message);
        }

    }

    public class QualityCheckRun : IDisposable
    {
        private const int EXCEPTION_BATCH_SIZE = 100;
        private readonly object exceptionLock = new object();
        private readonly Stopwatch timer;
        private readonly Client client;
        private readonly RunDTO run;
        private readonly BlockingCollection<RunException> exceptionQueue = new BlockingCollection<RunException>(EXCEPTION_BATCH_SIZE * 2);
        private bool disposed;
        private string failureMessage;

        private Exception apiException;

        private Task<int> consumer;

        /// <summary>
        /// Contains any settings defined in the quality check.
        /// </summary>
        /// <returns></returns>
        public string Configuration { get; set; }

        internal QualityCheckRun(Client client, RunDTO run)
        {
            this.client = client;
            this.run = run;
            this.Configuration = run.Configuration;
            this.timer = Stopwatch.StartNew();
            this.consumer = Task.Factory.StartNew(this.ConsumeExceptions);
        }

        /// <summary>
        /// Queue one or more exceptions to be sent to service. The actual send is batched.
        /// </summary>
        /// <param name="exceptions"></param>
        public void SendExceptions(params RunException[] exceptions)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(run.Id, "This run has been completed.");
            }

            if (apiException != null)
            {
                throw new Exception("The Data Quality API is not available, or the configuration is incorrect. See internal exception and log for details.", apiException);
            }

            foreach (var exception in exceptions)
            {
                this.exceptionQueue.Add(exception);
            }
        }

        /// <summary>
        /// Mark this run as failed, with te provided message.
        /// </summary>
        /// <param name="message"></param>
        public void Failed(string message)
        {
            this.failureMessage = message;
        }

        /// <summary>
        /// Set the population count for this run (the number of records actually checked).
        /// </summary>
        /// <param name="count"></param>
        public void SetPopulationCount(int count)
        {
            this.run.Population = count;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            this.timer.Stop();

            // Signal the sender that we're done adding exceptions.
            this.exceptionQueue.CompleteAdding();

            // This will block until sending is complete.
            this.run.ExceptionCount = this.consumer.Result;

            this.run.QueryTime = this.timer.ElapsedMilliseconds;

            this.run.State = "complete";

            this.run.FinishedAt = DateTime.UtcNow;

            if (this.failureMessage != null)
            {
                this.run.ErrorMessage = this.failureMessage;
                this.run.Status = "failed";
            }

            this.client.Send("PUT", $"/v3/dataquality/runs/external/{this.run.Id}", this.run);
        }

        private int ConsumeExceptions()
        {
            var count = 0;
            var exceptions = new List<RunExceptionDTO>(EXCEPTION_BATCH_SIZE);

            var runReference = new GuidReference
            {
                Key = this.run.Id,
                Name = this.run.Id
            };

            foreach (var exception in this.exceptionQueue.GetConsumingEnumerable())
            {
                count++;
                var dto = new RunExceptionDTO
                {
                    Id = Guid.NewGuid(),
                    Run = runReference,
                    RunStartedAt = this.run.StartedAt,
                    Data = exception.Data,
                    Description = exception.Description,
                    Key = exception.Key,
                    Label = exception.Label,
                    Sequence = exception.Sequence,
                    Timestamp = exception.Timestamp == DateTime.MinValue ? DateTime.Now : exception.Timestamp,
                };
                exceptions.Add(dto);
                client.Log($"[ExceptionConsumer] Current count: {count}; Pending count: {exceptions.Count}");
                if (exceptions.Count >= EXCEPTION_BATCH_SIZE)
                {
                    client.Log($"[ExceptionConsumer] Batch size reached, sending {exceptions.Count} exceptions to API.");
                    this.SendExceptionsBatch(exceptions);

                    client.Log($"[ExceptionConsumer] Exceptions sent to API, resetting pending list.");
                    exceptions.Clear();
                }
            }

            client.Log($"[ExceptionConsumer] All exceptions received.");

            if (exceptions.Count > 0)
            {
                client.Log($"[ExceptionConsumer] Flushing remaining {exceptions.Count} exceptions to API.");
                this.SendExceptionsBatch(exceptions);
            }

            return count;
        }

        private void SendExceptionsBatch(List<RunExceptionDTO> exceptions)
        {
            this.timer.Stop();

            var message = new
            {
                data = exceptions
            };

            try
            {
                this.client.Send("POST", $"/v3/dataquality/runs/external/{this.run.Id}/exceptions", message);
            }
            catch (Exception ex)
            {
                this.client.Log($"[ExceptionConsumer] Error sending exceptions to API. Will retry once in 5 seconds. Error was {ex}");
                Thread.Sleep(5000);
                try
                {
                    this.client.Send("POST", $"/v3/dataquality/runs/external/{this.run.Id}/exceptions", message);
                }
                catch (Exception ex2)
                {
                    this.client.Log($"[ExceptionConsumer] Error resending exceptions to API. Will abort and rethrow exception. Error was {ex2}");
                    this.apiException = ex2;
                    throw;
                }
            }

            this.timer.Start();
        }

    }


}

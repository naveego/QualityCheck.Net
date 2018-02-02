using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using Naveego.DQ.ExternalClient.Internal;
using Newtonsoft.Json.Converters;

namespace Naveego.DQ.ExternalClient
{

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

        private readonly string token;
        private readonly string baseUrl;
        private readonly string proxyUrl;
        private readonly int proxyPort;

        public Client(string baseUrl, string token, string proxyUrl = null, int proxyPort = 80)
        {
            this.proxyPort = proxyPort;
            this.proxyUrl = proxyUrl;
            this.baseUrl = baseUrl.TrimEnd('/');
            this.token = token;
        }

        public QualityCheckRun StartQualityCheck(string qualityCheckId)
        {

            // get quality check from API

            // create run and populate from quality check
            var run = new RunDTO { };

            this.Send("POST", "/v3/dataquality/runs", run);

            var qualityCheckRun = new QualityCheckRun(this, run);

            return qualityCheckRun;
        }

        internal void Send(string method, string path, object data)
        {
            using (var wc = new System.Net.WebClient())
            {

                if (this.proxyUrl != null)
                {
                    wc.Proxy = new WebProxy(this.proxyUrl, this.proxyPort);
                }
                else
                {
                    wc.Proxy = null;
                }

                wc.Headers.Add("Authorization", string.Format("Bearer {0}", this.token));

                var body = JsonConvert.SerializeObject(data, jsonSerializerSettings);

                var url = this.baseUrl + path;

                wc.UploadString(url, method, body);
            }
        }

    }

    public class QualityCheckRun : IDisposable
    {
        private const int EXCEPTION_BATCH_SIZE = 100;
        private readonly object exceptionLock = new object();
        private readonly Stopwatch timer;
        private readonly Client client;
        private readonly RunDTO run;
        private readonly List<RunException> exceptions = new List<RunException>(EXCEPTION_BATCH_SIZE);
        private bool disposed;
        private string failureMessage;

        internal QualityCheckRun(Client client, RunDTO run)
        {
            this.client = client;
            this.run = run;
            this.timer = Stopwatch.StartNew();
        }

        /// <summary>
        /// Queue one or more exceptions to be sent to service. The actual send is batched.
        /// </summary>
        /// <param name="exceptions"></param>
        public void SendExceptions(params RunException[] exceptions)
        {
            lock (exceptionLock)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(run.Id, "This run has been completed.");
                }

                this.run.ExceptionCount += exceptions.Length;
                this.exceptions.AddRange(exceptions);
                SendExceptionsBatch(false);
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
            lock (exceptionLock)
            {
                this.disposed = true;
                SendExceptionsBatch(true);
            }

            this.timer.Stop();

            this.run.QueryTime = this.timer.ElapsedMilliseconds;

            this.run.State = "complete";

            this.run.FinishedAt = DateTime.UtcNow;

            if (this.failureMessage != null)
            {
                this.run.ErrorMessage = this.failureMessage;
                this.run.Status = "failed";
            }

            this.client.Send("PUT", "/v3/dataquality/runs/" + this.run.Id, this.run);
        }


        private void SendExceptionsBatch(bool force)
        {
            var ready = this.exceptions.Count >= EXCEPTION_BATCH_SIZE
                || (force && this.exceptions.Count > 0);
            if (ready)
            {
                this.timer.Stop();

                var message = new
                {
                    data = this.exceptions
                };

                this.client.Send("POST", $"/v3/dataquality/runs/{this.run.Id}/exceptions", message);

                this.exceptions.Clear();

                this.timer.Start();
            }
        }

    }


}

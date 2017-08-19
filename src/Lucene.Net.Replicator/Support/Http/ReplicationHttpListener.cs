using Lucene.Net.Replicator.Http.Abstractions;
using Lucene.Net.Support;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Lucene.Net.Replicator.Http
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// A specialized HTTP listener (web server) for replication.
    /// </summary>
    /// <remarks>
    /// This is a simple web server that can be used in any .NET application
    /// to provide a replication server, however since it has its own socket 
    /// and thread handling it is generally only recommended for thick client applications.
    /// </remarks>
    // TODO: HTTPS support
    public class ReplicationHttpListener
    {
        private const int BUFFER_LENGTH = 1024;
        private readonly static string END_OF_HEADER = "\r\n\r\n"; // NOTE: We can't use Environment.NewLine here because this is part of the HTTP spec and is not environment sensitive.

        private readonly IReplicationService replicationService;
        private readonly TcpListener tcpListener;
        private bool isStopping = false;

        /// <summary>
        /// Creates a <see cref="ReplicationHttpListener"/> with the specified
        /// <see cref="IReplicationService"/>, <paramref name="ipAddress"/> and <paramref name="port"/>.
        /// </summary>
        /// <param name="replicationService">A <see cref="IReplicationService"/> instance to use for processing requests (think of it as the "controller").</param>
        /// <param name="ipAddress">The <see cref="IPAddress"/> to bind the <see cref="ReplicationHttpListener"/> to.</param>
        /// <param name="port">The port to bind the <see cref="ReplicationHttpListener"/> to.</param>
        public ReplicationHttpListener(IReplicationService replicationService, IPAddress ipAddress, int port)
            : this(replicationService, new TcpListener(ipAddress, port))
        {
        }

        /// <summary>
        /// Creates a <see cref="ReplicationHttpListener"/> with the specified
        /// <see cref="IReplicationService"/> and <paramref name="tcpListener"/>.
        /// </summary>
        /// <param name="replicationService">A <see cref="IReplicationService"/> instance to use for processing requests (think of it as the "controller").</param>
        /// <param name="tcpListener">A <see cref="TcpListener"/> that is configured to listen on a specific IP address and/or port.</param>
        public ReplicationHttpListener(IReplicationService replicationService, TcpListener tcpListener)
        {
            if (replicationService == null)
                throw new ArgumentNullException("replicationService");
            if (tcpListener == null)
                throw new ArgumentNullException("tcpListener");

            this.replicationService = replicationService;
            this.tcpListener = tcpListener;
        }

        /// <summary>
        /// Time in milliseconds to wait for data to be received from the socket connection before aborting.
        /// </summary>
        public int ReceiveTimeout { get; set; } = 3000;

        /// <summary>
        /// The <see cref="TcpListener"/>.
        /// </summary>
        public virtual TcpListener TcpListener { get { return this.tcpListener; } }

        /// <summary>
        /// Runs the loop to accept incoming requests and put them into
        /// the thread pool.
        /// </summary>
        private void Run()
        {
            while (!isStopping)
            {
                try
                {
                    ThreadPool.QueueUserWorkItem(ProcessClient,
                        tcpListener.AcceptTcpClientAsync().Result);
                }
                catch (Exception)
                {
                    // We may get here because TcpListener throws a SocketException when
                    // tcpListener.Stop() is called during a normal shutdown.
                }
            }
        }

        /// <summary>
        /// Processes an incoming client request.
        /// </summary>
        /// <remarks>
        /// This method can be overridden to support SSL or client authentication. See the
        /// following example on MSDN: https://msdn.microsoft.com/en-us/library/system.net.security.sslstream.aspx
        /// </remarks>
        /// <param name="client">A <see cref="TcpClient"/> instance.</param>
        protected virtual void ProcessClient(object client)
        {
            using (var tcpClient = ((TcpClient)client))
            using (var stream = tcpClient.GetStream())
            {
                //var stream = tcpClient.GetStream(); // Do not close the stream...?
                try
                {
                    var request = ProcessRequest(tcpClient);
                    if (request != null)
                    {
                        ProcessResponse(request, tcpClient);
                    }
                }
                catch
                {
                    try
                    {
                        // Create bad request response if this isn't GET/POST
                        var internalServerError = Encoding.UTF8.GetBytes(CreateHtmlDocument("500 Internal Server Error", ""));
                        stream.WriteResponseHeader((int)HttpStatusCode.InternalServerError, "text/html; charset=utf-8", internalServerError.Length);
                        tcpClient.Client.Send(internalServerError);
                    }
                    catch
                    {
                        // We can't write the response, so just ignore
                    }
                }
                finally
                {
                    stream.Flush();
                }
            }
        }

        /// <summary>
        /// Reads an HTTP header and puts the minimum data that <see cref="ReplicationService"/> 
        /// uses into a <see cref="IReplicationRequest"/> instance.
        /// </summary>
        /// <param name="tcpClient">The connected <see cref="TcpClient"/>.</param>
        /// <returns>A <see cref="IReplicationRequest"/> instance populated with the data from the HTTP header.</returns>
        protected virtual IHttpRequest ProcessRequest(TcpClient tcpClient) // LUCENENET TODO: Accept a stream here so SslStream can be passed in without having to override
        {
            var header = new StringBuilder();
            bool inHeader = true;
            byte[] bytes = new byte[BUFFER_LENGTH];
            int bytesRead;
            var stream = tcpClient.GetStream();

            long start = Time.CurrentTimeMilliseconds();
            while (!stream.DataAvailable)
            {
                if (Time.CurrentTimeMilliseconds() - start > ReceiveTimeout)
                    return null; // No data sent by client
            }

            // Loop to receive all the header data sent by the client.
            do
            {
                bytesRead = stream.Read(bytes, 0, Math.Min(tcpClient.Available, bytes.Length));
                if (bytesRead == 0) break;

                // Translate data bytes to a ASCII string.
                var data = Encoding.ASCII.GetString(bytes, 0, bytesRead);

                if (data.Contains(END_OF_HEADER))
                {
                    inHeader = false;
                    header.Append(data.Substring(0, data.IndexOf(END_OF_HEADER)));
                }
                else
                {
                    header.Append(data);

                    // Corner case: If the double newline is split between 2 different buffers
                    // we need to append the data first and then check if the header contains
                    // the end of the header string and if so make the adjustment to the length.
                    var endIndex = header.IndexOf(END_OF_HEADER);
                    if (endIndex > -1)
                    {
                        inHeader = false;
                        header.Length = endIndex;
                    }
                }
            } while (inHeader && stream.DataAvailable);

            return new HttpRequest(header.ToString());
        }

        /// <summary>
        /// Processes the <see cref="IReplicationRequest"/> and writes the response.
        /// </summary>
        /// <param name="request">A <see cref="IReplicationRequest"/> instance with details about the current HTTP request such as path and query string.</param>
        /// <param name="tcpClient">The connected <see cref="TcpClient"/>.</param>
        protected virtual void ProcessResponse(IHttpRequest request, TcpClient tcpClient) // LUCENENET TODO: Accept a stream here so SslStream can be passed in without having to override
        {
            var stream = tcpClient.GetStream();
            if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Post)
            {
                var replicationRequest = new ReplicationHttpRequest(request.Path, request.QueryString);
                var replicationResponse = new ReplicationHttpResponse(stream);

                replicationService.Perform(replicationRequest, replicationResponse);
                // If the replicationService didn't send anything, write the response header
                replicationResponse.Finish();
            }
            else
            {
                // Create bad request response if this isn't GET/POST
                var badRequest = Encoding.UTF8.GetBytes(CreateHtmlDocument("400 Bad Request", "Only GET/POST are valid verbs."));
                stream.WriteResponseHeader((int)HttpStatusCode.BadRequest, "text/html; charset=utf-8", badRequest.Length);
                tcpClient.Client.Send(badRequest);
            }
        }

        private static string CreateHtmlDocument(string title, string body)
        {
            return CreateHtmlDocument(title, title, body);
        }

        private static string CreateHtmlDocument(string title, string heading, string body)
        {
            var sb = new StringBuilder("<!DOCTYPE html><html><head><title>");
            sb.Append(title);
            sb.Append("</title></head><body><h1>");
            sb.Append(heading);
            sb.Append("</h1>");
            sb.Append(body);
            sb.Append("</body></html>");
            return sb.ToString();
        }

        /// <summary>
        /// Starts the server listening for incoming requests on a background thread.
        /// </summary>
        public void Start()
        {
            tcpListener.Start();
            Thread workerThread = new Thread(new ThreadStart(Run)) { IsBackground = true };
            workerThread.Start();
        }

        /// <summary>
        /// Stops the server from listening for incoming requests.
        /// </summary>
        public void Stop()
        {
            isStopping = true;
            tcpListener.Stop();
        }
    }
}

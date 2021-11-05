using Lucene.Net.Replicator.Http;
using Lucene.Net.Replicator.Http.Abstractions;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lucene.Net.Replicator
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

    [SuppressCodecs("Lucene3x")]
    public class ReplicatorTestCase : LuceneTestCase
    {
        public static TestServer NewHttpServer<TStartUp>(IReplicationService service, MockErrorConfig mockErrorConfig) where TStartUp : class
        {
            var server = new TestServer(new WebHostBuilder()
                .ConfigureServices(container =>
                {
                    container.AddSingleton(service);
                    container.AddSingleton(mockErrorConfig);
                })
                .Configure(app =>
                {
                    app.UseMiddleware<MockErrorMiddleware>();

                    //app.Use(
                    //    async (context, next) =>
                    //{
                    //    if (mockErrorConfig.RespondWithError)
                    //    {
                    //        //throw new HttpResponseException();
                    //        throw new Exception("This failed");
                    //    }

                    //    // Call the next delegate/middleware in the pipeline
                    //    await next();
                    //});
                })
                .UseStartup<TStartUp>());
            server.BaseAddress = new Uri("http://localhost" + ReplicationService.REPLICATION_CONTEXT);
            return server;
        }

        /// <summary>
        /// Returns a <see cref="server"/>'s port. 
        /// </summary>
        public static int ServerPort(TestServer server)
        {
            return server.BaseAddress.Port;
        }

        /// <summary>
        /// Returns a <see cref="server"/>'s host. 
        /// </summary>
        public static string ServerHost(TestServer server)
        {
            return server.BaseAddress.Host;
        }

        /// <summary>
        /// Stops the given HTTP Server instance.
        /// </summary>
        public static void StopHttpServer(TestServer server)
        {
            server.Dispose();
        }

        public class HttpResponseException : Exception
        {
            public int Status { get; set; } = 500;

            public object Value { get; set; }
        }

        public class MockErrorMiddleware
        {
            private readonly RequestDelegate next;
            private readonly MockErrorConfig mockErrorConfig;

            public MockErrorMiddleware(RequestDelegate next, MockErrorConfig mockErrorConfig)
            {
                this.next = next ?? throw new ArgumentNullException(nameof(next));
                this.mockErrorConfig = mockErrorConfig ?? throw new ArgumentNullException(nameof(mockErrorConfig));
            }

            public async Task InvokeAsync(HttpContext context)
            {
                if (mockErrorConfig.RespondWithError)
                {
                    throw new HttpResponseException();
                }

                // Call the next delegate/middleware in the pipeline
                await next(context);
            }
        }

        public class MockErrorConfig
        {
            public bool RespondWithError { get; set; } = false;
        }
    }
}
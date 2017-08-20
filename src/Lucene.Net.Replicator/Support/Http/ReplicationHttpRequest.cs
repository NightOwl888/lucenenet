using Lucene.Net.Replicator.Http.Abstractions;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;

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
    /// A replication HTTP request.
    /// </summary>
    public class ReplicationHttpRequest : IReplicationRequest
    {
        private readonly HttpListenerRequest request;
        //private readonly string path;
        //private readonly NameValueCollection queryString;

        public ReplicationHttpRequest(HttpListenerRequest request)
        {
            this.request = request;
            //this.path = path;
            //this.queryString = queryString;
        }

        public string Path => request.Url.AbsolutePath;

        public string QueryParam(string name) => request.QueryString.Get(name); //queryString.Get(name);
    }


    ///// <summary>
    ///// A replication HTTP request.
    ///// </summary>
    //public class ReplicationHttpRequest : IReplicationRequest
    //{
    //    private readonly string path;
    //    private readonly NameValueCollection queryString;

    //    public ReplicationHttpRequest(string path, NameValueCollection queryString)
    //    {
    //        this.path = path;
    //        this.queryString = queryString;
    //    }

    //    public string Path => path;

    //    public string QueryParam(string name) => queryString.Get(name);
    //}
}

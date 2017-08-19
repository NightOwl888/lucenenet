using Lucene.Net.Replicator.Http.Abstractions;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;

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
    /// A parser to retrieve the replication pertainent values
    /// from the ASCII text of the HTTP header.
    /// </summary>
    public class HttpRequest : IHttpRequest
    {
        public HttpRequest(string request)
        {
            Parse(request);
        }

        public HttpMethod Method { get; private set; }

        public string Path { get; private set; }

        public NameValueCollection QueryString { get; private set; }

        private void Parse(string request)
        {
            if (string.IsNullOrWhiteSpace(request)) return;

            string[] args = request.Split(' ').Select(p => p.Trim()).ToArray();

            // Get HTTP Method
            if (args[0].ToUpperInvariant().Equals("POST"))
            {
                Method = HttpMethod.Post;
            }
            else if (args[0].ToUpperInvariant().Equals("GET"))
            {
                Method = HttpMethod.Get;
            }

            var pathAndQuery = args[1];

            // Get Path
            int startIndex = 0;
            if ((pathAndQuery.StartsWith("/", StringComparison.Ordinal)))
            {
                startIndex = 1;
            }

            if (pathAndQuery.Contains('?'))
                Path = pathAndQuery.Substring(startIndex, pathAndQuery.IndexOf('?') - startIndex);
            else
                Path = pathAndQuery.Substring(startIndex);

            // Get Query String
            QueryString = ParseQueryString(pathAndQuery);
        }

        public static NameValueCollection ParseQueryString(string s)
        {
            NameValueCollection nvc = new NameValueCollection();

            // remove anything other than query string from url
            if (s.Contains("?"))
            {
                s = s.Substring(s.IndexOf('?') + 1);
            }

            foreach (string vp in Regex.Split(s, "&"))
            {
                string[] singlePair = Regex.Split(vp, "=");
                if (singlePair.Length == 2)
                {
                    nvc.Add(singlePair[0], Uri.UnescapeDataString(singlePair[1]));
                }
                else
                {
                    // only one key with no value specified in query string
                    nvc.Add(singlePair[0], string.Empty);
                }
            }

            return nvc;
        }
    }
}

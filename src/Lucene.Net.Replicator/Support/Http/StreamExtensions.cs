using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

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

    internal static class StreamExtensions
    {
        /// <summary>
        /// Writes an HTTP response header to this stream using the provided values.
        /// </summary>
        /// <param name="stream">This stream.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="contentType">The content type (which may include encoding).</param>
        /// <param name="contentLength">The content length (if unknown, omit).</param>
        public static void WriteResponseHeader(this Stream stream, int statusCode, string contentType = "", long contentLength = -1)
        {
            // Set HTML Header
            var sb = new StringBuilder();
            sb.Append("HTTP/1.1 ");
            sb.Append(statusCode);
            sb.Append(' ');
            sb.Append(((HttpStatusCode)statusCode).ToString());
            sb.AppendLine();
            sb.Append("Server: ");
            sb.Append(typeof(ReplicationHttpListener).GetTypeInfo().Name);
            sb.Append(' ');
            sb.AppendLine(Util.Constants.LUCENE_MAIN_VERSION);
            if (!string.IsNullOrEmpty(contentType))
            {
                sb.Append("Content-Type: ");
                sb.Append(contentType);
                sb.AppendLine();
            }
            if (contentLength >= 0)
            {
                sb.Append("Content-Length: ");
                sb.Append(contentLength);
            }
            sb.AppendLine().AppendLine();

            var bytes = Encoding.ASCII.GetBytes(sb.ToString());
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}

using Lucene.Net.Replicator.Http.Abstractions;
using System.IO;
using System.Net;

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
    /// A replication HTTP response.
    /// </summary>
    public class ReplicationHttpResponse : IReplicationResponse
    {
        private readonly ReplicationStreamWrapper stream;

        public ReplicationHttpResponse(Stream stream)
        {
            this.stream = new ReplicationStreamWrapper(this, stream);
        }

        /// <summary>
        /// Gets or sets the HTTP status code of the response. This must be set
        /// before writing to <see cref="Body"/> or it will have no effect.
        /// </summary>
        public int StatusCode { get; set; } = (int)HttpStatusCode.OK;

        /// <summary>
        /// The response content.
        /// </summary>
        public Stream Body => stream;

        /// <summary>
        /// Flushes the reponse to the underlying response stream.
        /// </summary>
        public void Flush() => stream.Flush();

        /// <summary>
        /// If <see cref="Stream.Write(byte[], int, int)"/> is not called
        /// on <see cref="Body"/>, this method writes a HTTP header to the
        /// response.
        /// </summary>
        public void Finish() => stream.FinishResponse();

        /// <summary>
        /// Wrapper class to ensure a header is always written
        /// prior to writing any bytes to the stream.
        /// </summary>
        private class ReplicationStreamWrapper : Stream
        {
            private readonly IReplicationResponse outerInstance;
            private readonly Stream innerStream;
            private bool headerWritten = false;

            public ReplicationStreamWrapper(IReplicationResponse outerInstance, Stream stream)
            {
                this.outerInstance = outerInstance;
                this.innerStream = stream;
            }

            public override bool CanRead => innerStream.CanRead;

            public override bool CanSeek => innerStream.CanSeek;

            public override bool CanWrite => innerStream.CanWrite;

            public override long Length => innerStream.Length;

            public override long Position
            {
                get { return innerStream.Position; }
                set { innerStream.Position = value; }
            }

            public override void Flush()
            {
                innerStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return innerStream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return innerStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                innerStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (!headerWritten)
                {
                    innerStream.WriteResponseHeader(outerInstance.StatusCode, "application/octet-stream");
                    headerWritten = true;
                }
                innerStream.Write(buffer, offset, count);
            }

            public void FinishResponse()
            {
                if (!headerWritten)
                {
                    innerStream.WriteResponseHeader(outerInstance.StatusCode);
                    headerWritten = true;
                }
            }
        }
    }
}

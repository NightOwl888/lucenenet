// Lucene version compatibility level 8.2.0
using J2N.IO;
using J2N.Numerics;
using Lucene.Net.Codecs;
using Lucene.Net.Store;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Ko.Dict
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
    /// n-gram connection cost data
    /// </summary>
    public sealed class ConnectionCosts
    {
        public const string FILENAME_SUFFIX = ".dat";
        public const string HEADER = "ko_cc";
        public const int VERSION = 1;

        private readonly ByteBuffer buffer;
        private readonly int forwardSize;

        private ConnectionCosts()
        {
            ByteBuffer buffer = null;
            using (Stream @is = BinaryDictionary.GetTypeResource(GetType(), FILENAME_SUFFIX))
            {
                DataInput input = new InputStreamDataInput(@is);
                CodecUtil.CheckHeader(input, HEADER, VERSION, VERSION);
                this.forwardSize = input.ReadVInt32();
                int backwardSize = input.ReadVInt32();
                int size = forwardSize * backwardSize;

                // copy the matrix into a direct byte buffer
                ByteBuffer tmpBuffer = ByteBuffer.Allocate(size * 2);
                int accum = 0;
                for (int j = 0; j < backwardSize; j++)
                {
                    for (int i = 0; i < forwardSize; i++)
                    {
                        accum += ZigZagDecode(input.ReadVInt32());
                        tmpBuffer.PutInt16((short)accum);
                    }
                }
                buffer = tmpBuffer.AsReadOnlyBuffer();
            }
            this.buffer = buffer;
        }

        public int Get(int forwardId, int backwardId)
        {
            // map 2d matrix into a single dimension short array
            int offset = (backwardId * forwardSize + forwardId) * 2;
            return buffer.GetInt16(offset);
        }

        public static ConnectionCosts Instance => SingletonHolder.INSTANCE;

        private static class SingletonHolder
        {
            internal static readonly ConnectionCosts INSTANCE = LoadInstance(); // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            private static ConnectionCosts LoadInstance()
            {
                try
                {
                    return new ConnectionCosts();
                }
                catch (IOException ioe)
                {
                    throw new Exception("Cannot load ConnectionCosts.", ioe);
                }
            }
        }

        /// <summary>
        /// Decode an int previously encoded with zig zag format.
        /// </summary>
        private static int ZigZagDecode(int i) // From lucene 8.2.0
        {
            return ((i.TripleShift(1)) ^ -(i & 1));
        }

    }
}

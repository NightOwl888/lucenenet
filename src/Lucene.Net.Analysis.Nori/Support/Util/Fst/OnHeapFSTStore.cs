using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Support.Util.Fst
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
    /// Provides storage of finite state machine (FST),
    /// using byte array or byte store allocated on heap.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class OnHeapFSTStore : IFSTStore
    {
        private static readonly long BASE_RAM_BYTES_USED = RamUsageEstimator.ShallowSizeOfInstance(typeof(OnHeapFSTStore));

        /** A {@link BytesStore}, used during building, or during reading when
         *  the FST is very large (more than 1 GB).  If the FST is less than 1
         *  GB then bytesArray is set instead. */
        private BytesStore bytes;

        /** Used at read time when the FST fits into a single byte[]. */
        private byte[] bytesArray;

        private readonly int maxBlockBits;

        public OnHeapFSTStore(int maxBlockBits)
        {
            if (maxBlockBits < 1 || maxBlockBits > 30)
            {
                throw new IllegalArgumentException("maxBlockBits should be 1 .. 30; got " + maxBlockBits);
            }

            this.maxBlockBits = maxBlockBits;
        }

        public void Init(DataInput input, long numBytes)
        {
            if (numBytes > 1 << this.maxBlockBits)
            {
                // FST is big: we need multiple pages
                bytes = new BytesStore(input, numBytes, 1 << this.maxBlockBits);
            }
            else
            {
                // FST fits into a single block: use ByteArrayBytesStoreReader for less overhead
                bytesArray = new byte[(int)numBytes];
                input.ReadBytes(bytesArray, 0, bytesArray.Length);
            }
        }

        public long Length
        {
            get
            {


                if (bytesArray != null)
                {
                    return bytesArray.Length;
                }
                else
                {
                    return bytes.GetRamBytesUsed();
                }
            }
        }

        public long GetRamBytesUsed()
        {
            return BASE_RAM_BYTES_USED + Length;
        }

        public FST.BytesReader GetReverseBytesReader()
        {
            if (bytesArray != null)
            {
                return new ReverseBytesReader(bytesArray);
            }
            else
            {
                return bytes.GetReverseReader();
            }
        }

        public void WriteTo(DataOutput output)
        {
            if (bytes != null)
            {
                long numBytes = bytes.Position;
                output.WriteVInt64(numBytes);
                bytes.WriteTo(output);
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(bytesArray != null);
                output.WriteVInt64(bytesArray.Length);
                output.WriteBytes(bytesArray, 0, bytesArray.Length);
            }
        }
    }
}

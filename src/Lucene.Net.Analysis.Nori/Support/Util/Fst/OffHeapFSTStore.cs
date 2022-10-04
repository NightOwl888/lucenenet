//using Lucene.Net.Store;
//using Lucene.Net.Util;
//using System;

//namespace Lucene.Net.Support.Util.Fst
//{
//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    /// <summary>
//    /// Provides off heap storage of finite state machine (FST),
//    /// using underlying index input instead of byte store on heap
//    /// <para/>
//    /// @lucene.experimental
//    /// </summary>
//    public sealed class OffHeapFSTStore : IFSTStore
//    {
//        private static readonly long BASE_RAM_BYTES_USED = RamUsageEstimator.ShallowSizeOfInstance(typeof(OffHeapFSTStore));

//        private IndexInput input;
//        private long offset;
//        private long numBytes;

//        public void Init(DataInput input, long numBytes)
//        {
//            if (input is IndexInput indexInput)
//            {
//                this.input = indexInput;
//                this.numBytes = numBytes;
//                this.offset = this.input.Position;
//            }
//            else
//            {
//                throw new IllegalArgumentException("parameter:in should be an instance of IndexInput for using OffHeapFSTStore, not a "
//                                                   + input.GetType().Name);
//            }
//        }

//        public long GetRamBytesUsed()
//        {
//            return BASE_RAM_BYTES_USED;
//        }

//        public long Length => numBytes;

//        public FST.BytesReader GetReverseBytesReader()
//        {
//            try
//            {
//                return new ReverseRandomAccessReader(input.RandomAccessSlice(offset, numBytes));
//            }
//            catch (Exception e) when (e.IsIOException())
//            {
//                throw RuntimeException.Create(e);
//            }
//        }

//        public void WriteTo(DataOutput output)
//        {
//            throw UnsupportedOperationException.Create("writeToOutput operation is not supported for OffHeapFSTStore");
//        }
//    }
//}

// Lucene version compatibility level 8.2.0
using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Util.Fst;
using Long = J2N.Numerics.Int64;

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
    /// Thin wrapper around an FST with root-arc caching for Hangul syllables (11,172 arcs).
    /// </summary>
    public sealed class TokenInfoFST
    {
        private readonly FST<Long> fst;

        private readonly int cacheCeiling;
        private readonly FST.Arc<Long>[] rootCache;

        public readonly long? NO_OUTPUT;

        public TokenInfoFST(FST<Long> fst)
        {
            this.fst = fst;
            this.cacheCeiling = 0xD7A3;
            NO_OUTPUT = fst.Outputs.NoOutput;
            rootCache = CacheRootArcs();
        }

        private FST.Arc<Long>[] CacheRootArcs()
        {
            FST.Arc<Long>[] rootCache = new FST.Arc<Long>[1 + (cacheCeiling - 0xAC00)];
            FST.Arc<Long> firstArc = new FST.Arc<Long>();
            fst.GetFirstArc(firstArc);
            FST.Arc<Long> arc = new FST.Arc<Long>();
            FST.BytesReader fstReader = fst.GetBytesReader();
            // TODO: jump to AC00, readNextRealArc to ceiling? (just be careful we don't add bugs)
            for (int i = 0; i < rootCache.Length; i++)
            {
                if (fst.FindTargetArc(0xAC00 + i, firstArc, arc, fstReader) != null)
                {
                    rootCache[i] = new FST.Arc<Long>().CopyFrom(arc);
                }
            }
            return rootCache;
        }

        public FST.Arc<Long> FindTargetArc(int ch, FST.Arc<Long> follow, FST.Arc<Long> arc, bool useCache, FST.BytesReader fstReader)
        {
            if (useCache && ch >= 0xAC00 && ch <= cacheCeiling)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(ch != FST.END_LABEL);
                FST.Arc<Long> result = rootCache[ch - 0xAC00];
                if (result == null)
                {
                    return null;
                }
                else
                {
                    arc.CopyFrom(result);
                    return arc;
                }
            }
            else
            {
                return fst.FindTargetArc(ch, follow, arc, fstReader);
            }
        }

        public FST.Arc<Long> GetFirstArc(FST.Arc<Long> arc)
        {
            return fst.GetFirstArc(arc);
        }

        public FST.BytesReader GetBytesReader()
        {
            return fst.GetBytesReader();
        }

        /// <summary>@lucene.internal for testing only</summary>
        internal FST<Long> InternalFST => fst;
    }
}

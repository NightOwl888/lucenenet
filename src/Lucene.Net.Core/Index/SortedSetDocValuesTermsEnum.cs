using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Index
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

    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Implements a <seealso cref="TermsEnum"/> wrapping a provided
    /// <seealso cref="SortedSetDocValues"/>.
    /// </summary>

    internal class SortedSetDocValuesTermsEnum : TermsEnum
    {
        private readonly SortedSetDocValues values;
        private long currentOrd = -1;
        private readonly BytesRef term = new BytesRef();

        /// <summary>
        /// Creates a new TermsEnum over the provided values </summary>
        public SortedSetDocValuesTermsEnum(SortedSetDocValues values)
        {
            this.values = values;
        }

        public override SeekStatus SeekCeil(BytesRef text)
        {
            long ord = values.LookupTerm(text);
            if (ord >= 0)
            {
                currentOrd = ord;
                term.Offset = 0;
                // TODO: is there a cleaner way?
                // term.bytes may be pointing to codec-private byte[]
                // storage, so we must force new byte[] allocation:
                term.Bytes = new byte[text.Length];
                term.CopyBytes(text);
                return SeekStatus.FOUND;
            }
            else
            {
                currentOrd = -ord - 1;
                if (currentOrd == values.ValueCount)
                {
                    return SeekStatus.END;
                }
                else
                {
                    // TODO: hmm can we avoid this "extra" lookup?:
                    values.LookupOrd(currentOrd, term);
                    return SeekStatus.NOT_FOUND;
                }
            }
        }

        public override bool SeekExact(BytesRef text)
        {
            long ord = values.LookupTerm(text);
            if (ord >= 0)
            {
                term.Offset = 0;
                // TODO: is there a cleaner way?
                // term.bytes may be pointing to codec-private byte[]
                // storage, so we must force new byte[] allocation:
                term.Bytes = new byte[text.Length];
                term.CopyBytes(text);
                currentOrd = ord;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void SeekExact(long ord)
        {
            Debug.Assert(ord >= 0 && ord < values.ValueCount);
            currentOrd = (int)ord;
            values.LookupOrd(currentOrd, term);
        }

        public override BytesRef Next()
        {
            currentOrd++;
            if (currentOrd >= values.ValueCount)
            {
                return null;
            }
            values.LookupOrd(currentOrd, term);
            return term;
        }

        public override BytesRef Term()
        {
            return term;
        }

        public override long Ord()
        {
            return currentOrd;
        }

        public override int DocFreq()
        {
            throw new System.NotSupportedException();
        }

        public override long TotalTermFreq()
        {
            return -1;
        }

        public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
        {
            throw new System.NotSupportedException();
        }

        public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
        {
            throw new System.NotSupportedException();
        }

        public override IComparer<BytesRef> Comparator
        {
            get
            {
                return BytesRef.UTF8SortedAsUnicodeComparer;
            }
        }

        public override void SeekExact(BytesRef term, TermState state)
        {
            Debug.Assert(state != null && state is OrdTermState);
            this.SeekExact(((OrdTermState)state).ord);
        }

        public override TermState TermState()
        {
            OrdTermState state = new OrdTermState();
            state.ord = currentOrd;
            return state;
        }
    }
}
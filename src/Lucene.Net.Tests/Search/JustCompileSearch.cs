using Lucene.Net.Util;
using System;

namespace Lucene.Net.Search
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldInvertState = Lucene.Net.Index.FieldInvertState;
    using IBits = Lucene.Net.Util.IBits;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Holds all implementations of classes in the o.a.l.search package as a
    /// back-compatibility test. It does not run any tests per-se, however if
    /// someone adds a method to an interface or abstract method to an abstract
    /// class, one of the implementations here will fail to compile and so we know
    /// back-compat policy was violated.
    /// </summary>
    internal sealed class JustCompileSearch
    {
        private const string UNSUPPORTED_MSG = "unsupported: used for back-compat testing only !";

        internal sealed class JustCompileCollector : ICollector
        {
            public void Collect(int doc)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public void SetNextReader(AtomicReaderContext context)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public void SetScorer(Scorer scorer)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public bool AcceptsDocsOutOfOrder => throw new NotSupportedException(UNSUPPORTED_MSG);
        }

        internal sealed class JustCompileDocIdSet : DocIdSet
        {
            public override DocIdSetIterator GetIterator()
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileDocIdSetIterator : DocIdSetIterator
        {
            public override int DocID => throw new NotSupportedException(UNSUPPORTED_MSG);

            public override int NextDoc()
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int Advance(int target)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override long GetCost()
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileExtendedFieldCacheLongParser : FieldCache.IInt64Parser
        {
            /// <summary>
            /// NOTE: This was parseLong() in Lucene
            /// </summary>
            public long ParseInt64(BytesRef @string)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileExtendedFieldCacheDoubleParser : FieldCache.IDoubleParser
        {
            public double ParseDouble(BytesRef term)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileFieldComparer : FieldComparer<object>
        {
            public override int Compare(int slot1, int slot2)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int CompareBottom(int doc)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override void Copy(int slot, int doc)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override void SetBottom(int slot)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override void SetTopValue(object value)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override FieldComparer SetNextReader(AtomicReaderContext context)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            // LUCENENET NOTE: This was value(int) in Lucene.
            public override IComparable this[int slot] => throw new NotSupportedException(UNSUPPORTED_MSG);

            public override int CompareTop(int doc)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileFieldComparerSource : FieldComparerSource
        {
            public override FieldComparer NewComparer(string fieldname, int numHits, int sortPos, bool reversed)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileFilter : Filter
        {
            // Filter is just an abstract class with no abstract methods. However it is
            // still added here in case someone will add abstract methods in the future.

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                return null;
            }
        }

        internal sealed class JustCompileFilteredDocIdSet : FilteredDocIdSet
        {
            public JustCompileFilteredDocIdSet(DocIdSet innerSet)
                : base(innerSet)
            {
            }

            protected override bool Match(int docid)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileFilteredDocIdSetIterator : FilteredDocIdSetIterator
        {
            public JustCompileFilteredDocIdSetIterator(DocIdSetIterator innerIter)
                : base(innerIter)
            {
            }

            protected override bool Match(int doc)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override long GetCost()
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileQuery : Query
        {
            public override string ToString(string field)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileScorer : Scorer
        {
            internal JustCompileScorer(Weight weight)
                : base(weight)
            {
            }

            public override float GetScore()
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int Freq => throw new NotSupportedException(UNSUPPORTED_MSG);

            public override int DocID => throw new NotSupportedException(UNSUPPORTED_MSG);

            public override int NextDoc()
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int Advance(int target)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override long GetCost()
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileSimilarity : Similarity
        {
            public override SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override SimScorer GetSimScorer(SimWeight stats, AtomicReaderContext context)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override long ComputeNorm(FieldInvertState state)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileTopDocsCollector : TopDocsCollector<ScoreDoc>
        {
            internal JustCompileTopDocsCollector(PriorityQueue<ScoreDoc> pq)
                : base(pq)
            {
            }

            public override void Collect(int doc)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override void SetScorer(Scorer scorer)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override bool AcceptsDocsOutOfOrder => throw new NotSupportedException(UNSUPPORTED_MSG);

            public override TopDocs GetTopDocs()
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override TopDocs GetTopDocs(int start)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override TopDocs GetTopDocs(int start, int end)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileWeight : Weight
        {
            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override Query Query => throw new NotSupportedException(UNSUPPORTED_MSG);

            public override void Normalize(float norm, float topLevelBoost)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override float GetValueForNormalization()
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }

            public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
            {
                throw new NotSupportedException(UNSUPPORTED_MSG);
            }
        }
    }
}
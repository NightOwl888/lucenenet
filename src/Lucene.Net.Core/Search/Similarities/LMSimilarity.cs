using System.Globalization;

namespace Lucene.Net.Search.Similarities
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
    /// Abstract superclass for language modeling Similarities. The following inner
    /// types are introduced:
    /// <ul>
    ///   <li><seealso cref="LMStats"/>, which defines a new statistic, the probability that
    ///   the collection language model generates the current term;</li>
    ///   <li><seealso cref="ICollectionModel"/>, which is a strategy interface for object that
    ///   compute the collection language model {@code p(w|C)};</li>
    ///   <li><seealso cref="DefaultCollectionModel"/>, an implementation of the former, that
    ///   computes the term probability as the number of occurrences of the term in the
    ///   collection, divided by the total number of tokens.</li>
    /// </ul>
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class LMSimilarity : SimilarityBase
    {
        /// <summary>
        /// The collection model. </summary>
        protected internal readonly ICollectionModel collectionModel;

        /// <summary>
        /// Creates a new instance with the specified collection language model. </summary>
        public LMSimilarity(ICollectionModel collectionModel)
        {
            this.collectionModel = collectionModel;
        }

        /// <summary>
        /// Creates a new instance with the default collection language model. </summary>
        public LMSimilarity()
            : this(new DefaultCollectionModel())
        {
        }

        protected internal override BasicStats NewStats(string field, float queryBoost)
        {
            return new LMStats(field, queryBoost);
        }

        /// <summary>
        /// Computes the collection probability of the current term in addition to the
        /// usual statistics.
        /// </summary>
        protected internal override void FillBasicStats(BasicStats stats, CollectionStatistics collectionStats, TermStatistics termStats)
        {
            base.FillBasicStats(stats, collectionStats, termStats);
            LMStats lmStats = (LMStats)stats;
            lmStats.CollectionProbability = collectionModel.ComputeProbability(stats);
        }

        protected internal override void Explain(Explanation expl, BasicStats stats, int doc, float freq, float docLen)
        {
            expl.AddDetail(new Explanation(collectionModel.ComputeProbability(stats), "collection probability"));
        }

        /// <summary>
        /// Returns the name of the LM method. The values of the parameters should be
        /// included as well.
        /// <p>Used in <seealso cref="#toString()"/></p>.
        /// </summary>
        public abstract string GetName();

        /// <summary>
        /// Returns the name of the LM method. If a custom collection model strategy is
        /// used, its name is included as well. </summary>
        /// <seealso cref= #getName() </seealso>
        /// <seealso cref= CollectionModel#getName() </seealso>
        /// <seealso cref= DefaultCollectionModel  </seealso>
        public override string ToString()
        {
            string coll = collectionModel.Name;
            if (coll != null)
            {
                return string.Format(CultureInfo.InvariantCulture, "LM %s - %s", GetName(), coll); // LUCENENET TODO: Formatting
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, "LM %s", GetName()); // LUCENENET TODO: Formatting
            }
        }

        /// <summary>
        /// Stores the collection distribution of the current term. </summary>
        public class LMStats : BasicStats
        {
            /// <summary>
            /// The probability that the current term is generated by the collection. </summary>
            private float collectionProbability;

            /// <summary>
            /// Creates LMStats for the provided field and query-time boost
            /// </summary>
            public LMStats(string field, float queryBoost)
                : base(field, queryBoost)
            {
            }

            /// <summary>
            /// Returns the probability that the current term is generated by the
            /// collection.
            /// </summary>
            public float CollectionProbability
            {
                get
                {
                    return collectionProbability;
                }
                set
                {
                    this.collectionProbability = value;
                }
            }
        }

        /// <summary>
        /// A strategy for computing the collection language model. </summary>
        public interface ICollectionModel
        {
            /// <summary>
            /// Computes the probability {@code p(w|C)} according to the language model
            /// strategy for the current term.
            /// </summary>
            float ComputeProbability(BasicStats stats);

            /// <summary>
            /// The name of the collection model strategy. </summary>
            string Name { get; }
        }

        /// <summary>
        /// Models {@code p(w|C)} as the number of occurrences of the term in the
        /// collection, divided by the total number of tokens {@code + 1}.
        /// </summary>
        public class DefaultCollectionModel : ICollectionModel
        {
            /// <summary>
            /// Sole constructor: parameter-free </summary>
            public DefaultCollectionModel()
            {
            }

            public virtual float ComputeProbability(BasicStats stats)
            {
                return (stats.TotalTermFreq + 1F) / (stats.NumberOfFieldTokens + 1F);
            }

            public virtual string Name
            {
                get
                {
                    return null;
                }
            }
        }
    }
}
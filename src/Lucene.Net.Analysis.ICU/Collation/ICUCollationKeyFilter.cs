﻿// lucene version compatibility level: 4.8.1
using ICU4N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Collation
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
    /// Converts each token into its <see cref="CollationKey"/>, and
    /// then encodes the <see cref="CollationKey"/> with <see cref="IndexableBinaryStringTools"/>, to
    /// allow it to be stored as an index term.
    /// </summary>
    /// <remarks>
    /// <strong>WARNING:</strong> Make sure you use exactly the same <see cref="Collator"/> at
    /// index and query time -- CollationKeys are only comparable when produced by
    /// the same <see cref="Collator"/>.  <see cref="RuleBasedCollator"/>s are 
    /// independently versioned, so it is safe to search against stored
    /// <see cref="CollationKey"/>s if the following are exactly the same (best practice is
    /// to store this information with the index and check that they remain the
    /// same at query time):
    /// <list type="number">
    ///     <item><description>Collator version - see <see cref="Collator"/> Version</description></item>
    ///     <item><description>The collation strength used - see <see cref="Collator.Strength"/></description></item>
    /// </list>
    /// <para/>
    /// <see cref="CollationKey"/>s generated by ICU Collators are not compatible with those
    /// generated by java.text.Collators.  Specifically, if you use 
    /// <see cref="ICUCollationKeyAnalyzer"/> to generate index terms, do not use 
    /// CollationKeyAnalyzer on the query side, or vice versa.
    /// <para/>
    /// ICUCollationKeyAnalyzer is significantly faster and generates significantly
    /// shorter keys than CollationKeyAnalyzer.  See
    /// <a href="http://site.icu-project.org/charts/collation-icu4j-sun"
    /// >http://site.icu-project.org/charts/collation-icu4j-sun</a> for key
    /// generation timing and key length comparisons between ICU4J and
    /// java.text.Collator over several languages.
    /// </remarks>
    [Obsolete("Use ICUCollationAttributeFactory instead, which encodes terms directly as bytes. This filter will be removed in Lucene 5.0")]
    [ExceptionToClassNameConvention]
    public sealed class ICUCollationKeyFilter : TokenFilter
    {
        private Collator collator = null;
        private RawCollationKey reusableKey = new RawCollationKey();
        private readonly ICharTermAttribute termAtt;

        /// <summary>
        /// Creates a new <see cref="ICUCollationKeyFilter"/>.
        /// </summary>
        /// <param name="input">Source token stream.</param>
        /// <param name="collator"><see cref="CollationKey"/> generator.</param>
        public ICUCollationKeyFilter(TokenStream input, Collator collator)
            : base(input)
        {
            // clone the collator: see http://userguide.icu-project.org/collation/architecture
            this.collator = (Collator)collator.Clone();
            this.termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                char[] termBuffer = termAtt.Buffer;
                string termText = new string(termBuffer, 0, termAtt.Length);
                collator.GetRawCollationKey(termText, reusableKey);
                int encodedLength = IndexableBinaryStringTools.GetEncodedLength(
                    reusableKey.bytes, 0, reusableKey.Count);
                if (encodedLength > termBuffer.Length)
                {
                    termAtt.ResizeBuffer(encodedLength);
                }
                termAtt.SetLength(encodedLength);
                IndexableBinaryStringTools.Encode(reusableKey.bytes, 0, reusableKey.Count,
                    termAtt.Buffer, 0, encodedLength);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

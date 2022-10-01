// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Ko.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Ko
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
    /// Removes tokens that match a set of part-of-speech tags.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class KoreanPartOfSpeechStopFilter : FilteringTokenFilter
    {
        private readonly ISet<POS.Tag> stopTags;
        private readonly IPartOfSpeechAttribute posAtt;

        /// <summary>
        /// Default list of tags to filter.
        /// </summary>
        public static readonly ISet<POS.Tag> DEFAULT_STOP_TAGS = new JCG.HashSet<POS.Tag>
        {
            POS.Tag.E,
            POS.Tag.IC,
            POS.Tag.J,
            POS.Tag.MAG,
            POS.Tag.MAJ,
            POS.Tag.MM,
            POS.Tag.SP,
            POS.Tag.SSC,
            POS.Tag.SSO,
            POS.Tag.SC,
            POS.Tag.SE,
            POS.Tag.XPN,
            POS.Tag.XSA,
            POS.Tag.XSN,
            POS.Tag.XSV,
            POS.Tag.UNA,
            POS.Tag.NA,
            POS.Tag.VSV
        };

        /// <summary>
        /// Create a new <see cref="KoreanPartOfSpeechStopFilter"/> with the default
        /// list of stop tags <see cref="DEFAULT_STOP_TAGS"/>.
        /// </summary>
        /// <param name="version">The Lucene match version.</param>
        /// <param name="input">The <see cref="TokenStream"/> to consume.</param>
        public KoreanPartOfSpeechStopFilter(LuceneVersion version, TokenStream input)
            : this(version, input, DEFAULT_STOP_TAGS)
        {
        }

        /// <summary>
        /// Create a new <see cref="KoreanPartOfSpeechStopFilter"/>.
        /// </summary>
        /// <param name="version">The Lucene match version.</param>
        /// <param name="input">The <see cref="TokenStream"/> to consume.</param>
        /// <param name="stopTags">The part-of-speech tags that should be removed.</param>
        public KoreanPartOfSpeechStopFilter(LuceneVersion version, TokenStream input, ISet<POS.Tag> stopTags)
            : base(version, input)

        {
            this.posAtt = AddAttribute<IPartOfSpeechAttribute>();
            this.stopTags = stopTags;
        }

        protected override bool Accept()
        {
            POS.Tag? leftPOS = posAtt.LeftPOS;
            return leftPOS == null || !stopTags.Contains(leftPOS.Value);
        }
    }
}

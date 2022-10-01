// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Ko.Dict;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.IO;
using static Lucene.Net.Util.AttributeSource;

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
    /// Analyzer for Korean that uses morphological analysis.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="KoreanTokenizer"/>
    /// <since>7.4.0</since>
    public class KoreanAnalyzer : Analyzer
    {
        private readonly UserDictionary userDict;
        private readonly DecompoundMode mode;
        private readonly ISet<POS.Tag> stopTags;
        private readonly bool outputUnknownUnigrams;

        private readonly LuceneVersion matchVersion; // LUCENENET specific - patch because we are porting from 8.2.0

        /// <summary>
        /// Creates a new <see cref="KoreanAnalyzer"/>.
        /// </summary>
        /// <param name="matchVersion">The Lucene match version.</param>
        public KoreanAnalyzer(LuceneVersion matchVersion)
            : this(matchVersion, null, KoreanTokenizer.DEFAULT_DECOMPOUND, KoreanPartOfSpeechStopFilter.DEFAULT_STOP_TAGS, false)
        {

        }

        /// <summary>
        /// Creates a new <see cref="KoreanAnalyzer"/>.
        /// </summary>
        /// <param name="matchVersion">The Lucene match version.</param>
        /// <param name="userDict">Optional: if non-<c>null</c>, user dictionary.</param>
        /// <param name="mode">Decompound mode.</param>
        /// <param name="stopTags">The set of part of speech that should be filtered.</param>
        /// <param name="outputUnknownUnigrams">If <c>true</c> outputs unigrams for unknown words.</param>
        public KoreanAnalyzer(LuceneVersion matchVersion, UserDictionary userDict, DecompoundMode mode, ISet<POS.Tag> stopTags, bool outputUnknownUnigrams)
            : base()
        {
            this.matchVersion = matchVersion;
            this.userDict = userDict;
            this.mode = mode;
            this.stopTags = stopTags;
            this.outputUnknownUnigrams = outputUnknownUnigrams;
        }

        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer tokenizer = new KoreanTokenizer(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, reader, userDict, mode, outputUnknownUnigrams);
            TokenStream stream = new KoreanPartOfSpeechStopFilter(matchVersion, tokenizer, stopTags);
            stream = new KoreanReadingFormFilter(stream);
            stream = new LowerCaseFilter(matchVersion, stream);
            return new TokenStreamComponents(tokenizer, stream);
        }

        //      @Override
        //protected TokenStream normalize(String fieldName, TokenStream input)
        //      {
        //          TokenStream result = new LowerCaseFilter(input);
        //          return result;
        //      }
    }
}

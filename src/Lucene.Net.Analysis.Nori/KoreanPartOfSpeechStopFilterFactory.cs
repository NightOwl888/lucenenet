// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Factory for <see cref="KoreanPartOfSpeechStopFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_ko" class="solr.TextField"&gt;
    ///    &lt;analyzer&gt;
    ///      &lt;tokenizer class="solr.KoreanTokenizerFactory"/&gt;
    ///      &lt;filter class="solr.KoreanPartOfSpeechStopFilterFactory"
    ///              tags="E,J"/&gt;
    ///    &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// <para/>
    /// Supports the following attributes:
    /// <list type="bullet">
    ///     <item><description>tags: List of stop tags. if not specified, <see cref="KoreanPartOfSpeechStopFilter.DEFAULT_STOP_TAGS"/> is used.</description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <since>7.4.0</since>
    public class KoreanPartOfSpeechStopFilterFactory : TokenFilterFactory
    {
        private ISet<POS.Tag> stopTags;

        /// <summary>
        /// Creates a new <see cref="KoreanPartOfSpeechStopFilterFactory"/>.
        /// </summary>
        public KoreanPartOfSpeechStopFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            ISet<string> stopTagStr = GetSet(args, "tags");
            if (stopTagStr == null)
            {
                stopTags = KoreanPartOfSpeechStopFilter.DEFAULT_STOP_TAGS;
            }
            else
            {
                stopTags = new JCG.HashSet<POS.Tag>(stopTagStr.Select(str => POS.ResolveTag(str)).Distinct().ToArray());

            }
            if (args.Count != 0)
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream stream)
        {
            return new KoreanPartOfSpeechStopFilter(m_luceneMatchVersion, stream, stopTags);
        }
    }
}

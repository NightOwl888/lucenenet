// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;

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
    /// Factory for <see cref="KoreanNumberFilter"/>.
    /// <para/>
    /// <code>
    /// &lt;fieldType name="text_ko" class="solr.TextField"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.KoreanTokenizerFactory" discardPunctuation="false"/&gt;
    ///     &lt;filter class="solr.KoreanNumberFilterFactory"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// <para/>
    /// It is important that punctuation is not discarded by the tokenizer so use
    /// <c>discardPunctuation="false"</c> in your <see cref="KoreanTokenizerFactory"/>.
    /// </summary>
    /// <since>8.2.0</since>
    public class KoreanNumberFilterFactory : TokenFilterFactory
    {
        public KoreanNumberFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            if (args.Count != 0)
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new KoreanNumberFilter(input);
        }
    }
}

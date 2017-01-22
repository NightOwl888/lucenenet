﻿using Lucene.Net.Analysis.TokenAttributes;

namespace Lucene.Net.Analysis.Ru
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
    /// A <seealso cref="TokenFilter"/> that applies <seealso cref="RussianLightStemmer"/> to stem Russian
    /// words.
    /// <para>
    /// To prevent terms from being stemmed use an instance of
    /// <seealso cref="SetKeywordMarkerFilter"/> or a custom <seealso cref="TokenFilter"/> that sets
    /// the <seealso cref="KeywordAttribute"/> before this <seealso cref="TokenStream"/>.
    /// </para>
    /// </summary>
    public sealed class RussianLightStemFilter : TokenFilter
    {
        private readonly RussianLightStemmer stemmer = new RussianLightStemmer();
        private readonly ICharTermAttribute termAtt;
        private readonly IKeywordAttribute keywordAttr;

        public RussianLightStemFilter(TokenStream input) 
            : base(input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            keywordAttr = AddAttribute<IKeywordAttribute>();
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                if (!keywordAttr.IsKeyword)
                {
                    int newlen = stemmer.Stem(termAtt.Buffer, termAtt.Length);
                    termAtt.Length = newlen;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
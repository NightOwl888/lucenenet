// Lucene version compatibility level 8.2.0
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Simple tests for <see cref="KoreanPartOfSpeechStopFilterFactory"/>
    /// </summary>
    public class TestKoreanPartOfSpeechStopFilterFactory : BaseTokenStreamTestCase
    {
        [Test]
        public void TestStopTags()
        {
            KoreanTokenizerFactory tokenizerFactory = new KoreanTokenizerFactory(new JCG.Dictionary<String, String>());
            tokenizerFactory.Inform(new StringMockResourceLoader(""));
            TokenStream ts = tokenizerFactory.Create(new StringReader(" 한국은 대단한 나라입니다."));
            //((Tokenizer) ts).setReader(new StringReader(" 한국은 대단한 나라입니다."));
            IDictionary<String, String> args = new JCG.Dictionary<String, String>();
            args["luceneMatchVersion"] = LuceneVersion.LUCENE_CURRENT.ToString();
            args["tags"] = "E, J";
            KoreanPartOfSpeechStopFilterFactory factory = new KoreanPartOfSpeechStopFilterFactory(args);
            ts = factory.Create(ts);
            AssertTokenStreamContents(ts,
                new String[] { "한국", "대단", "하", "나라", "이" }
            );
        }

        /** Test that bogus arguments result in exception */

        [Test]
        public void TestBogusArguments()
        {
            ArgumentException expected = Assert.Throws<ArgumentException>(() =>
            {
                new KoreanPartOfSpeechStopFilterFactory(new JCG.Dictionary<String, String>() {
                    {"luceneMatchVersion", LuceneVersion.LUCENE_CURRENT.toString() },
                    { "bogusArg", "bogusValue" }
                });
            });
            //    IllegalArgumentException expected = expectThrows(IllegalArgumentException.class, () -> {
            //      new KoreanPartOfSpeechStopFilterFactory(new JCG.Dictionary<String, String>() {{
            //        put("luceneMatchVersion", Version.LATEST.toString());
            //put("bogusArg", "bogusValue");
            //      }});
            //    });
            assertTrue(expected.Message.Contains("Unknown parameters"));
        }
    }
}

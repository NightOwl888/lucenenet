// Lucene version compatibility level 8.2.0
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
    /// Simple tests for <see cref="KoreanNumberFilterFactory"/>
    /// </summary>
    public class TestKoreanNumberFilterFactory : BaseTokenStreamTestCase
    {
        [Test]
        public void TestBasics()
        {

            IDictionary<String, String> args = new JCG.Dictionary<string, string>();
            args["discardPunctuation"] = "false";

            KoreanTokenizerFactory tokenizerFactory = new KoreanTokenizerFactory(args);

            tokenizerFactory.Inform(new StringMockResourceLoader(""));
            TokenStream tokenStream = tokenizerFactory.Create(NewAttributeFactory(), new StringReader("어제 초밥 가격은 10만 원"));
            //((Tokenizer) tokenStream).SetReader(new StringReader("어제 초밥 가격은 10만 원"));
            KoreanNumberFilterFactory factory = new KoreanNumberFilterFactory(new JCG.Dictionary<string, string>());
            tokenStream = factory.Create(tokenStream);
            // Wrong analysis
            // "초밥" => "초밥" O, "초"+"밥" X
            AssertTokenStreamContents(tokenStream,
                new String[] { "어제", " ", "초", "밥", " ", "가격", "은", " ", "100000", " ", "원" }
            );
        }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            ArgumentException expected = Assert.Throws<ArgumentException>(() =>
            {
                new KoreanNumberFilterFactory(new JCG.Dictionary<String, String>() {
                    { "bogusArg", "bogusValue" }
                });
            });

            assertTrue(expected.Message.Contains("Unknown parameters"));
        }
    }
}

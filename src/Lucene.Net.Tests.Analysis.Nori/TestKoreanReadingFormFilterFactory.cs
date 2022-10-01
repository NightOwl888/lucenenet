// Lucene version compatibility level 8.2.0
using NUnit.Framework;
using System;
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
    /// Simple tests for <see cref="KoreanReadingFormFilterFactory"/>
    /// </summary>
    public class TestKoreanReadingFormFilterFactory : BaseTokenStreamTestCase
    {
        [Test]
        public void TestReadings()
        {
            KoreanTokenizerFactory tokenizerFactory = new KoreanTokenizerFactory(new JCG.Dictionary<string, string>());
            tokenizerFactory.Inform(new StringMockResourceLoader(""));
            TokenStream tokenStream = tokenizerFactory.Create(new StringReader("丞相"));
            //((Tokenizer) tokenStream).setReader(new StringReader("丞相"));
            KoreanReadingFormFilterFactory filterFactory = new KoreanReadingFormFilterFactory(new JCG.Dictionary<string, string>());
            AssertTokenStreamContents(filterFactory.Create(tokenStream),
                new String[] { "승상" }
            );
        }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            ArgumentException expected = Assert.Throws<ArgumentException>(() =>
            {
                new KoreanReadingFormFilterFactory(new JCG.Dictionary<String, String>() {
                    { "bogusArg", "bogusValue" }
                });

            });
            //IllegalArgumentException expected = expectThrows(IllegalArgumentException.class, () -> {
            //  new KoreanReadingFormFilterFactory(new JCG.Dictionary<String, String>() {{
            //    put("bogusArg", "bogusValue");
            //  }});
            //});
            assertTrue(expected.Message.Contains("Unknown parameters"));
        }
    }
}

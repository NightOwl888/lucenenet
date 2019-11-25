using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

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
    /// Simple tests for <see cref="KoreanTokenizerFactory"/>
    /// </summary>
    public class TestKoreanTokenizerFactory : BaseTokenStreamTestCase
    {
        [Test]
        public void TestSimple()
        {
            KoreanTokenizerFactory factory = new KoreanTokenizerFactory(Collections.EmptyMap<string, string>());
            factory.Inform(new StringMockResourceLoader(""));
            TokenStream ts = factory.Create(NewAttributeFactory(), new StringReader("안녕하세요"));
            //((Tokenizer) ts).setReader(new StringReader("안녕하세요"));
            AssertTokenStreamContents(ts,
                new String[] { "안녕", "하", "시", "어요" },
                new int[] { 0, 2, 3, 3 },
                new int[] { 2, 3, 5, 5 }
            );
        }

        /**
         * Test decompoundMode
         */
        [Test]
        public void TestDiscardDecompound()
        {
            IDictionary<String, String> args = new HashMap<String, String>();
            args.Put("decompoundMode", "discard");
            KoreanTokenizerFactory factory = new KoreanTokenizerFactory(args);
            factory.Inform(new StringMockResourceLoader(""));
            TokenStream ts = factory.Create(NewAttributeFactory(), new StringReader("갠지스강"));
            //((Tokenizer) ts).setReader(new StringReader("갠지스강"));
            AssertTokenStreamContents(ts,
                new String[] { "갠지스", "강" }
            );
        }

        [Test]
        public void TestNoDecompound()
        {
            IDictionary<String, String> args = new HashMap<String, String>();
            args.Put("decompoundMode", "none");
            KoreanTokenizerFactory factory = new KoreanTokenizerFactory(args);
            factory.Inform(new StringMockResourceLoader(""));
            TokenStream ts = factory.Create(NewAttributeFactory(), new StringReader("갠지스강"));
            //((Tokenizer) ts).setReader(new StringReader("갠지스강"));
            AssertTokenStreamContents(ts,
        new String[] { "갠지스강" }
    );
        }

        [Test]
        public void TestMixedDecompound()
        {
            IDictionary<String, String> args = new HashMap<String, String>();
            args.Put("decompoundMode", "mixed");
            KoreanTokenizerFactory factory = new KoreanTokenizerFactory(args);
            factory.Inform(new StringMockResourceLoader(""));
            TokenStream ts = factory.Create(NewAttributeFactory(), new StringReader("갠지스강"));
            //((Tokenizer) ts).setReader(new StringReader("갠지스강"));
            AssertTokenStreamContents(ts,
        new String[] { "갠지스강", "갠지스", "강" }
    );
        }

        /**
         * Test user dictionary
         */
        [Test]
        public void TestUserDict()
        {
            String userDict =
                "# Additional nouns\n" +
                "세종시 세종 시\n" +
                "# \n" +
                "c++\n";
            IDictionary<String, String> args = new HashMap<String, String>();
            args.Put("userDictionary", "userdict.txt");
            KoreanTokenizerFactory factory = new KoreanTokenizerFactory(args);
            factory.Inform(new StringMockResourceLoader(userDict));
            TokenStream ts = factory.Create(NewAttributeFactory(), new StringReader("세종시"));
            //((Tokenizer) ts).setReader(new StringReader("세종시"));
            AssertTokenStreamContents(ts,
                new String[] { "세종", "시" }
            );
        }

        /**
         * Test discardPunctuation True
         */
        [Test]
        public void TestDiscardPunctuation_true()
        {
            IDictionary<String, String> args = new HashMap<String, String>();
            args.Put("discardPunctuation", "true");
            KoreanTokenizerFactory factory = new KoreanTokenizerFactory(args);
            factory.Inform(new StringMockResourceLoader(""));
            TokenStream ts = factory.Create(NewAttributeFactory(), new StringReader("10.1 인치 모니터"));
            //((Tokenizer) ts).setReader(new StringReader("10.1 인치 모니터"));
            AssertTokenStreamContents(ts,
        new String[] { "10", "1", "인치", "모니터" }
    );
        }

        /**
         * Test discardPunctuation False
         */
        [Test]
        public void TestDiscardPunctuation_false()
        {
            IDictionary<String, String> args = new HashMap<String, String>();
            args.Put("discardPunctuation", "false");
            KoreanTokenizerFactory factory = new KoreanTokenizerFactory(args);
            factory.Inform(new StringMockResourceLoader(""));
            TokenStream ts = factory.Create(NewAttributeFactory(), new StringReader("10.1 인치 모니터"));
            //((Tokenizer) ts).setReader(new StringReader("10.1 인치 모니터"));
            AssertTokenStreamContents(ts,
                new String[] { "10", ".", "1", " ", "인치", " ", "모니터" }
            );
        }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            ArgumentException expected = Assert.Throws<ArgumentException>(() =>
            {
                new KoreanTokenizerFactory(new HashMap<String, String>() {
                    { "bogusArg", "bogusValue" }
                });
            });
            //IllegalArgumentException expected = expectThrows(IllegalArgumentException.class, () -> {
            //  new KoreanTokenizerFactory(new HashMap<String, String>() {{
            //    put("bogusArg", "bogusValue");
            //  }});
            //});
            assertTrue(expected.Message.Contains("Unknown parameters"));
        }
    }
}

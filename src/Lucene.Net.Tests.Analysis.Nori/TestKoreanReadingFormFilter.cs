// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Core;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

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
    /// Tests for <see cref="KoreanReadingFormFilter"/>
    /// </summary>
    public class TestKoreanReadingFormFilter : BaseTokenStreamTestCase
    {
        private Analyzer analyzer;

        public override void SetUp()
        {
            base.SetUp();
            analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer =
                    new KoreanTokenizer(NewAttributeFactory(), reader, null, DecompoundMode.DISCARD, false);
                return new TokenStreamComponents(tokenizer, new KoreanReadingFormFilter(tokenizer));
            });
            //analyzer = new Analyzer()
            //    {
            //        @Override
            //  protected TokenStreamComponents createComponents(String fieldName)
            //        {
            //            Tokenizer tokenizer =
            //                new KoreanTokenizer(newAttributeFactory(), null, KoreanTokenizer.DecompoundMode.DISCARD, false);
            //            return new TokenStreamComponents(tokenizer, new KoreanReadingFormFilter(tokenizer));
            //        }
            //    };
        }


        public override void TearDown()
        {
            IOUtils.Dispose(analyzer);
            base.TearDown();
        }

        [Test]
        public void TestReadings()
        {
            AssertAnalyzesTo(analyzer, "車丞相",
            new String[] { "차", "승상" }
        );
        }

        [Test]
        public void TestRandomData()
        {
            Random random = Random;
            CheckRandomData(random, analyzer, 1000 * RandomMultiplier);
        }

        [Test]
        public void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new KoreanReadingFormFilter(tokenizer));
            });
            //    Analyzer a = new Analyzer()
            //{
            //    @Override
            //      protected TokenStreamComponents createComponents(String fieldName)
            //    {
            //        Tokenizer tokenizer = new KeywordTokenizer();
            //        return new TokenStreamComponents(tokenizer, new KoreanReadingFormFilter(tokenizer));
            //    }
            //};
            CheckOneTerm(a, "", "");
            a.Dispose();
        }
    }
}

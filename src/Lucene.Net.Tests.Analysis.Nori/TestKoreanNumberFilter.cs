// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Ko.Dict;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

    public class TestKoreanNumberFilter : BaseTokenStreamTestCase
    {
        private Analyzer analyzer;

        public static UserDictionary ReadDict()
        {
            Stream @is = typeof(TestKoreanTokenizer).getResourceAsStream("userdict.txt");
            if (@is == null)
            {
                throw new Exception("Cannot find userdict.txt in test classpath!");
            }
            try
            {
                try
                {
                    TextReader reader = new StreamReader(@is, Encoding.UTF8);
                    return UserDictionary.Open(reader);
                }
                finally
                {
                    @is.Dispose();
                }
            }
            catch (IOException ioe)
            {
                throw new Exception(ioe.ToString(), ioe);
            }
        }

        public override void SetUp()
        {
            base.SetUp();
            UserDictionary userDictionary = ReadDict();
            ISet<POS.Tag> stopTags = new JCG.HashSet<POS.Tag>();
            stopTags.add(POS.Tag.SP);
            analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KoreanTokenizer(NewAttributeFactory(), reader, userDictionary,
                    KoreanTokenizer.DEFAULT_DECOMPOUND, false, false);
                TokenStream stream = new KoreanPartOfSpeechStopFilter(TEST_VERSION_CURRENT, tokenizer, stopTags);
                return new TokenStreamComponents(tokenizer, new KoreanNumberFilter(stream));
            });
            //    analyzer = new Analyzer()
            //{
            //    @Override
            //      protected TokenStreamComponents createComponents(String fieldName)
            //    {
            //        Tokenizer tokenizer = new KoreanTokenizer(newAttributeFactory(), userDictionary,
            //            KoreanTokenizer.DEFAULT_DECOMPOUND, false, false);
            //        TokenStream stream = new KoreanPartOfSpeechStopFilter(tokenizer, stopTags);
            //        return new TokenStreamComponents(tokenizer, new KoreanNumberFilter(stream));
            //    }
            //};
        }

        public override void TearDown()
        {
            analyzer.Dispose();
            base.TearDown();
        }

        [Test]
        public void TestBasics()
        {

            AssertAnalyzesTo(analyzer, "오늘 십만이천오백원의 와인 구입",
                new String[] { "오늘", "102500", "원", "의", "와인", "구입" },
                new int[] { 0, 3, 9, 10, 12, 15 },
                new int[] { 2, 9, 10, 11, 14, 17 }
            );

            // Wrong analysis
            // "초밥" => "초밥" O, "초"+"밥" X
            AssertAnalyzesTo(analyzer, "어제 초밥 가격은 10만 원",
                new String[] { "어제", "초", "밥", "가격", "은", "100000", "원" },
                new int[] { 0, 3, 4, 6, 8, 10, 14, 15, 13 },
                new int[] { 2, 4, 5, 8, 9, 13, 15, 13, 14 }
            );

            AssertAnalyzesTo(analyzer, "자본금 600만 원",
                new String[] { "자본", "금", "6000000", "원" },
                new int[] { 0, 2, 4, 9, 10 },
                new int[] { 2, 3, 8, 10, 11 }
            );
        }

        [Test]
        public void TestVariants()
        {
            // Test variants of three
            AssertAnalyzesTo(analyzer, "3", new String[] { "3" });
            AssertAnalyzesTo(analyzer, "３", new String[] { "3" });
            AssertAnalyzesTo(analyzer, "삼", new String[] { "3" });

            // Test three variations with trailing zero
            AssertAnalyzesTo(analyzer, "03", new String[] { "3" });
            AssertAnalyzesTo(analyzer, "０３", new String[] { "3" });
            AssertAnalyzesTo(analyzer, "영삼", new String[] { "3" });
            AssertAnalyzesTo(analyzer, "003", new String[] { "3" });
            AssertAnalyzesTo(analyzer, "００３", new String[] { "3" });
            AssertAnalyzesTo(analyzer, "영영삼", new String[] { "3" });

            // Test thousand variants
            AssertAnalyzesTo(analyzer, "천", new String[] { "1000" });
            AssertAnalyzesTo(analyzer, "1천", new String[] { "1000" });
            AssertAnalyzesTo(analyzer, "１천", new String[] { "1000" });
            AssertAnalyzesTo(analyzer, "일천", new String[] { "1000" });
            AssertAnalyzesTo(analyzer, "일영영영", new String[] { "1000" });
            AssertAnalyzesTo(analyzer, "１０백", new String[] { "1000" }); // Strange, but supported
        }

        [Test]
        public void TestLargeVariants()
        {
            // Test large numbers
            AssertAnalyzesTo(analyzer, "삼오칠팔구", new String[] { "35789" });
            AssertAnalyzesTo(analyzer, "육백이만오천일", new String[] { "6025001" });
            AssertAnalyzesTo(analyzer, "조육백만오천일", new String[] { "1000006005001" });
            AssertAnalyzesTo(analyzer, "십조육백만오천일", new String[] { "10000006005001" });
            AssertAnalyzesTo(analyzer, "일경일", new String[] { "10000000000000001" });
            AssertAnalyzesTo(analyzer, "십경십", new String[] { "100000000000000010" });
            AssertAnalyzesTo(analyzer, "해경조억만천백십일", new String[] { "100010001000100011111" });
        }

        [Test]
        public void TestNegative()
        {
            AssertAnalyzesTo(analyzer, "-백만", new String[] { "-", "1000000" });
        }

        [Test]
        public void TestMixed()
        {
            // Test mixed numbers
            AssertAnalyzesTo(analyzer, "삼천2백２십삼", new String[] { "3223" });
            AssertAnalyzesTo(analyzer, "３２이삼", new String[] { "3223" });
        }

        [Test]
        public void TestFunny()
        {
            // Test some oddities for inconsistent input
            AssertAnalyzesTo(analyzer, "십십", new String[] { "20" }); // 100?
            AssertAnalyzesTo(analyzer, "백백백", new String[] { "300" }); // 10,000?
            AssertAnalyzesTo(analyzer, "천천천천", new String[] { "4000" }); // 1,000,000,000,000?
        }

        [Test]
        public void TestHangulArabic()
        {
            // Test kanji numerals used as Arabic numbers (with head zero)
            AssertAnalyzesTo(analyzer, "영일이삼사오육칠팔구구팔칠육오사삼이일영",
                new String[] { "1234567899876543210" }
            );

            // I'm Bond, James "normalized" Bond...
            AssertAnalyzesTo(analyzer, "영영칠", new String[] { "7" });
        }

        [Test]
        public void TestDoubleZero()
        {
            AssertAnalyzesTo(analyzer, "영영",
                new String[] { "0" },
                new int[] { 0 },
                new int[] { 2 },
                new int[] { 1 }
            );
        }

        [Test]
        public void Test_Name()
        {
            // Test name that normalises to number
            AssertAnalyzesTo(analyzer, "전중경일",
                new String[] { "전중", "10000000000000001" }, // 경일 is normalized to a number
                new int[] { 0, 2 },
                new int[] { 2, 4 },
                new int[] { 1, 1 }
            );

            // An analyzer that marks 경일 as a keyword
            Analyzer keywordMarkingAnalyzer = Analyzer.NewAnonymous((fieldName, reader) =>
            {
                CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, false);
                set.add("경일");
                UserDictionary userDictionary = ReadDict();
                ISet<POS.Tag> stopTags = new JCG.HashSet<POS.Tag>();
                stopTags.add(POS.Tag.SP);
                Tokenizer tokenizer = new KoreanTokenizer(NewAttributeFactory(), reader, userDictionary,
                    KoreanTokenizer.DEFAULT_DECOMPOUND, false, false);
                TokenStream stream = new KoreanPartOfSpeechStopFilter(TEST_VERSION_CURRENT, tokenizer, stopTags);
                return new TokenStreamComponents(tokenizer, new KoreanNumberFilter(new SetKeywordMarkerFilter(stream, set)));
            });

            //    Analyzer keywordMarkingAnalyzer = new Analyzer() {

            //      @Override
            //      protected TokenStreamComponents createComponents(String fieldName)
            //{
            //    CharArraySet set = new CharArraySet(1, false);
            //    set.add("경일");
            //    UserDictionary userDictionary = readDict();
            //    Set<POS.Tag> stopTags = new HashSet<>();
            //    stopTags.add(POS.Tag.SP);
            //    Tokenizer tokenizer = new KoreanTokenizer(newAttributeFactory(), userDictionary,
            //        KoreanTokenizer.DEFAULT_DECOMPOUND, false, false);
            //    TokenStream stream = new KoreanPartOfSpeechStopFilter(tokenizer, stopTags);
            //    return new TokenStreamComponents(tokenizer, new KoreanNumberFilter(new SetKeywordMarkerFilter(stream, set)));
            //}
            //    };

            AssertAnalyzesTo(keywordMarkingAnalyzer, "전중경일",
        new String[] { "전중", "경일" }, // 경일 is not normalized
        new int[] { 0, 2 },
        new int[] { 2, 4 },
        new int[] { 1, 1 }
    );
            keywordMarkingAnalyzer.Dispose();
        }

        [Test]
        public void TestDecimal()
        {
            // Test Arabic numbers with punctuation, i.e. 3.2 thousands
            AssertAnalyzesTo(analyzer, "１．２만３４５．６７",
                new String[] { "12345.67" }
            );
        }

        [Test]
        public void TestDecimalPunctuation()
        {
            // Test Arabic numbers with punctuation, i.e. 3.2 thousands won
            AssertAnalyzesTo(analyzer, "３．２천 원",
                new String[] { "3200", "원" }
            );
        }

        [Test]
        public void TestThousandSeparator()
        {
            AssertAnalyzesTo(analyzer, "4,647",
                new String[] { "4647" }
            );
        }

        [Test]
        public void TestDecimalThousandSeparator()
        {
            AssertAnalyzesTo(analyzer, "4,647.0010",
                new String[] { "4647.001" }
            );
        }

        [Test]
        public void TestCommaDecimalSeparator()
        {
            AssertAnalyzesTo(analyzer, "15,7",
                new String[] { "157" }
            );
        }

        [Test]
        public void TestTrailingZeroStripping()
        {
            AssertAnalyzesTo(analyzer, "1000.1000",
                new String[] { "1000.1" }
            );
            AssertAnalyzesTo(analyzer, "1000.0000",
                new String[] { "1000" }
            );
        }

        [Test]
        public void TestEmpty()
        {
            AssertAnalyzesTo(analyzer, "", new String[] { });
        }

        [Test]
        public void TestRandomHugeStrings()
        {
            CheckRandomData(Random, analyzer, 50 * RandomMultiplier, 8192);
        }

        [Test]
        public void TestRandomSmallStrings()
        {
            CheckRandomData(Random, analyzer, 500 * RandomMultiplier, 128);
        }

        [Test]
        public void TestFunnyIssue()
        {
            BaseTokenStreamTestCase.CheckAnalysisConsistency(
                Random, analyzer, true, "영영\u302f\u3029\u3039\u3023\u3033\u302bB", true
            );
        }

        //[Ignore("This test is used during development when analyze normalizations in large amounts of text")]
        //  [Test]
        //public void TestLargeData() 
        //{
        //    Path input = Paths.get("/tmp/test.txt");
        //    Path tokenizedOutput = Paths.get("/tmp/test.tok.txt");
        //    Path normalizedOutput = Paths.get("/tmp/test.norm.txt");

        //            Analyzer plainAnalyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        //            {
        //                UserDictionary userDictionary = ReadDict();
        //                ISet<POS.Tag> stopTags = new JCG.HashSet<POS.Tag>();
        //                stopTags.Add(POS.Tag.SP);
        //                Tokenizer tokenizer = new KoreanTokenizer(NewAttributeFactory(), reader, userDictionary,
        //                    KoreanTokenizer.DEFAULT_DECOMPOUND, false, false);
        //                return new TokenStreamComponents(tokenizer, new KoreanPartOfSpeechStopFilter(TEST_VERSION_CURRENT, tokenizer, stopTags));
        //            });

        //            //Analyzer plainAnalyzer = new Analyzer() {
        //            //  @Override
        //            //  protected TokenStreamComponents createComponents(String fieldName)
        //            //{
        //            //    UserDictionary userDictionary = ReadDict();
        //            //    ISet<POS.Tag> stopTags = new JCG.HashSet<POS.Tag>();
        //            //    stopTags.add(POS.Tag.SP);
        //            //    Tokenizer tokenizer = new KoreanTokenizer(newAttributeFactory(), userDictionary,
        //            //        KoreanTokenizer.DEFAULT_DECOMPOUND, false, false);
        //            //    return new TokenStreamComponents(tokenizer, new KoreanPartOfSpeechStopFilter(tokenizer, stopTags));
        //            //}
        //            //};

        //            Analyze(
        //        plainAnalyzer,
        //        Files.newBufferedReader(input, StandardCharsets.UTF_8),
        //        Files.newBufferedWriter(tokenizedOutput, StandardCharsets.UTF_8)
        //    );

        //            Analyze(
        //        analyzer,
        //        Files.newBufferedReader(input, StandardCharsets.UTF_8),
        //        Files.newBufferedWriter(normalizedOutput, StandardCharsets.UTF_8)
        //    );
        //    plainAnalyzer.Dispose();
        //}

        public void Analyze(Analyzer analyzer, TextReader reader, TextWriter writer)
        {
            TokenStream stream = analyzer.GetTokenStream("dummy", reader);
            stream.Reset();

            ICharTermAttribute termAttr = stream.AddAttribute<ICharTermAttribute>();

            while (stream.IncrementToken())
            {
                writer.Write(termAttr.toString());
                writer.Write("\n");
            }

            reader.Dispose();
            writer.Dispose();
        }
    }
}

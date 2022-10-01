// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Ko.Dict;
using Lucene.Net.Analysis.Ko.TokenAttributes;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

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

    public class TestKoreanTokenizer : BaseTokenStreamTestCase
    {
        private Analyzer analyzer, analyzerWithPunctuation, analyzerUnigram, analyzerDecompound, analyzerDecompoundKeep, analyzerReading;

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
            analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KoreanTokenizer(NewAttributeFactory(), reader, userDictionary,
                    DecompoundMode.NONE, false);
                return new TokenStreamComponents(tokenizer, tokenizer);
            });
            analyzerWithPunctuation = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KoreanTokenizer(NewAttributeFactory(), reader, userDictionary,
                    DecompoundMode.NONE, false, false);
                return new TokenStreamComponents(tokenizer, tokenizer);
            });
            analyzerUnigram = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KoreanTokenizer(NewAttributeFactory(), reader, userDictionary,
                    DecompoundMode.NONE, true);
                return new TokenStreamComponents(tokenizer, tokenizer);
            });
            analyzerDecompound = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KoreanTokenizer(NewAttributeFactory(), reader, userDictionary,
                    DecompoundMode.DISCARD, false);
                return new TokenStreamComponents(tokenizer);
            });
            analyzerDecompoundKeep = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KoreanTokenizer(NewAttributeFactory(), reader, userDictionary,
                    DecompoundMode.MIXED, false);
                return new TokenStreamComponents(tokenizer);
            });
            analyzerReading = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KoreanTokenizer(NewAttributeFactory(), reader, userDictionary,
                    DecompoundMode.NONE, false);
                KoreanReadingFormFilter filter = new KoreanReadingFormFilter(tokenizer);
                return new TokenStreamComponents(tokenizer, filter);
            });


            //    analyzer = new Analyzer()
            //{
            //    @Override
            //      protected TokenStreamComponents createComponents(String fieldName)
            //    {
            //        Tokenizer tokenizer = new KoreanTokenizer(newAttributeFactory(), userDictionary,
            //            DecompoundMode.NONE, false);
            //        return new TokenStreamComponents(tokenizer, tokenizer);
            //    }
            //};
            //            analyzerWithPunctuation = new Analyzer()
            //{
            //    @Override
            //      protected TokenStreamComponents createComponents(String fieldName)
            //    {
            //        Tokenizer tokenizer = new KoreanTokenizer(newAttributeFactory(), userDictionary,
            //            DecompoundMode.NONE, false, false);
            //        return new TokenStreamComponents(tokenizer, tokenizer);
            //    }
            //};
            //analyzerUnigram = new Analyzer()
            //{
            //    @Override
            //      protected TokenStreamComponents createComponents(String fieldName)
            //    {
            //        Tokenizer tokenizer = new KoreanTokenizer(newAttributeFactory(), userDictionary,
            //            DecompoundMode.NONE, true);
            //        return new TokenStreamComponents(tokenizer, tokenizer);
            //    }
            //};
            //analyzerDecompound = new Analyzer()
            //{
            //    @Override
            //      protected TokenStreamComponents createComponents(String fieldName)
            //    {
            //        Tokenizer tokenizer = new KoreanTokenizer(newAttributeFactory(), userDictionary,
            //            DecompoundMode.DISCARD, false);
            //        return new TokenStreamComponents(tokenizer);
            //    }
            //};
            //analyzerDecompoundKeep = new Analyzer()
            //{
            //    @Override
            //      protected TokenStreamComponents createComponents(String fieldName)
            //    {
            //        Tokenizer tokenizer = new KoreanTokenizer(newAttributeFactory(), userDictionary,
            //            DecompoundMode.MIXED, false);
            //        return new TokenStreamComponents(tokenizer);
            //    }
            //};
            //analyzerReading = new Analyzer()
            //{
            //    @Override
            //      protected TokenStreamComponents createComponents(String fieldName)
            //    {
            //        Tokenizer tokenizer = new KoreanTokenizer(newAttributeFactory(), userDictionary,
            //            DecompoundMode.NONE, false);
            //        KoreanReadingFormFilter filter = new KoreanReadingFormFilter(tokenizer);
            //        return new TokenStreamComponents(tokenizer, filter);
            //    }
            //};
        }

        [Test]
        public void TestSpaces()
        {
            AssertAnalyzesTo(analyzer, "화학        이외의         것",
                new String[] { "화학", "이외", "의", "것" },
                new int[] { 0, 10, 12, 22 },
                new int[] { 2, 12, 13, 23 },
                new int[] { 1, 1, 1, 1 }
            );
            AssertPartsOfSpeech(analyzer, "화학 이외의         것",
                new POS.Type[] { POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME },
                new POS.Tag[] { POS.Tag.NNG, POS.Tag.NNG, POS.Tag.J, POS.Tag.NNB },
                new POS.Tag[] { POS.Tag.NNG, POS.Tag.NNG, POS.Tag.J, POS.Tag.NNB }
            );
        }

        [Test]
        public void TestPartOfSpeechs()
        {
            AssertAnalyzesTo(analyzer, "화학 이외의 것",
                new String[] { "화학", "이외", "의", "것" },
                new int[] { 0, 3, 5, 7 },
                new int[] { 2, 5, 6, 8 },
                new int[] { 1, 1, 1, 1 }
            );
            AssertPartsOfSpeech(analyzer, "화학 이외의 것",
                new POS.Type[] { POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME },
                new POS.Tag[] { POS.Tag.NNG, POS.Tag.NNG, POS.Tag.J, POS.Tag.NNB },
                new POS.Tag[] { POS.Tag.NNG, POS.Tag.NNG, POS.Tag.J, POS.Tag.NNB }
            );
        }

        [Test]
        public void TestPartOfSpeechsWithPunc()
        {
            AssertAnalyzesTo(analyzerWithPunctuation, "화학 이외의 것!",
                new String[] { "화학", " ", "이외", "의", " ", "것", "!" },
                new int[] { 0, 2, 3, 5, 6, 7, 8, 9 },
                new int[] { 2, 3, 5, 6, 7, 8, 9, 11 },
                new int[] { 1, 1, 1, 1, 1, 1, 1, 1 }
            );
            AssertPartsOfSpeech(analyzerWithPunctuation, "화학 이외의 것!",
                new POS.Type[] { POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME },
                new POS.Tag[] { POS.Tag.NNG, POS.Tag.SP, POS.Tag.NNG, POS.Tag.J, POS.Tag.SP, POS.Tag.NNB, POS.Tag.SF },
                new POS.Tag[] { POS.Tag.NNG, POS.Tag.SP, POS.Tag.NNG, POS.Tag.J, POS.Tag.SP, POS.Tag.NNB, POS.Tag.SF }
            );
        }

        [Test]
        public void TestFloatingPointNumber()
        {
            AssertAnalyzesTo(analyzerWithPunctuation, "10.1 인치 모니터",
                new String[] { "10", ".", "1", " ", "인치", " ", "모니터" },
                new int[] { 0, 2, 3, 4, 5, 7, 8 },
                new int[] { 2, 3, 4, 5, 7, 8, 11 },
                new int[] { 1, 1, 1, 1, 1, 1, 1 }
            );

            AssertAnalyzesTo(analyzer, "10.1 인치 모니터",
                new String[] { "10", "1", "인치", "모니터" },
                new int[] { 0, 3, 5, 8 },
                new int[] { 2, 4, 7, 11 },
                new int[] { 1, 1, 1, 1 }
            );
        }

        [Test]
        public void TestPartOfSpeechsWithCompound()
        {
            AssertAnalyzesTo(analyzer, "가락지나물은 한국, 중국, 일본",
                new String[] { "가락지나물", "은", "한국", "중국", "일본" },
                new int[] { 0, 5, 7, 11, 15 },
                new int[] { 5, 6, 9, 13, 17 },
                new int[] { 1, 1, 1, 1, 1 }
            );

            AssertPartsOfSpeech(analyzer, "가락지나물은 한국, 중국, 일본",
                new POS.Type[] { POS.Type.COMPOUND, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME },
                new POS.Tag[] { POS.Tag.NNG, POS.Tag.J, POS.Tag.NNP, POS.Tag.NNP, POS.Tag.NNP },
                new POS.Tag[] { POS.Tag.NNG, POS.Tag.J, POS.Tag.NNP, POS.Tag.NNP, POS.Tag.NNP }
            );

            AssertAnalyzesTo(analyzerDecompound, "가락지나물은 한국, 중국, 일본",
                new String[] { "가락지", "나물", "은", "한국", "중국", "일본" },
                new int[] { 0, 3, 5, 7, 11, 15 },
                new int[] { 3, 5, 6, 9, 13, 17 },
                new int[] { 1, 1, 1, 1, 1, 1 }
            );

            AssertAnalyzesTo(analyzerDecompoundKeep, "가락지나물은 한국, 중국, 일본",
                new String[] { "가락지나물", "가락지", "나물", "은", "한국", "중국", "일본" },
                new int[] { 0, 0, 3, 5, 7, 11, 15 },
                new int[] { 5, 3, 5, 6, 9, 13, 17 },
                null,
                new int[] { 1, 0, 1, 1, 1, 1, 1 },
                new int[] { 2, 1, 1, 1, 1, 1, 1 }
            );

            AssertPartsOfSpeech(analyzerDecompound, "가락지나물은 한국, 중국, 일본",
                new POS.Type[] { POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME },
                new POS.Tag[] { POS.Tag.NNG, POS.Tag.NNG, POS.Tag.J, POS.Tag.NNP, POS.Tag.NNP, POS.Tag.NNP },
                new POS.Tag[] { POS.Tag.NNG, POS.Tag.NNG, POS.Tag.J, POS.Tag.NNP, POS.Tag.NNP, POS.Tag.NNP }
            );

            AssertPartsOfSpeech(analyzerDecompoundKeep, "가락지나물은 한국, 중국, 일본",
                new POS.Type[] { POS.Type.COMPOUND, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME },
                new POS.Tag[] { POS.Tag.NNG, POS.Tag.NNG, POS.Tag.NNG, POS.Tag.J, POS.Tag.NNP, POS.Tag.NNP, POS.Tag.NNP },
                new POS.Tag[] { POS.Tag.NNG, POS.Tag.NNG, POS.Tag.NNG, POS.Tag.J, POS.Tag.NNP, POS.Tag.NNP, POS.Tag.NNP }
            );
        }

        [Test]
        public void TestPartOfSpeechsWithInflects()
        {
            AssertAnalyzesTo(analyzer, "감싸여",
                new String[] { "감싸여" },
                new int[] { 0 },
                new int[] { 3 },
                new int[] { 1 }
            );

            AssertPartsOfSpeech(analyzer, "감싸여",
                new POS.Type[] { POS.Type.INFLECT },
                new POS.Tag[] { POS.Tag.VV },
                new POS.Tag[] { POS.Tag.E }
            );

            AssertAnalyzesTo(analyzerDecompound, "감싸여",
                new String[] { "감싸이", "어" },
                new int[] { 0, 0 },
                new int[] { 3, 3 },
                new int[] { 1, 1 }
            );

            AssertAnalyzesTo(analyzerDecompoundKeep, "감싸여",
                new String[] { "감싸여", "감싸이", "어" },
                new int[] { 0, 0, 0 },
                new int[] { 3, 3, 3 },
                null,
                new int[] { 1, 0, 1 },
                new int[] { 2, 1, 1 }
            );

            AssertPartsOfSpeech(analyzerDecompound, "감싸여",
                new POS.Type[] { POS.Type.MORPHEME, POS.Type.MORPHEME },
                new POS.Tag[] { POS.Tag.VV, POS.Tag.E },
                new POS.Tag[] { POS.Tag.VV, POS.Tag.E }
            );

            AssertPartsOfSpeech(analyzerDecompoundKeep, "감싸여",
                new POS.Type[] { POS.Type.INFLECT, POS.Type.MORPHEME, POS.Type.MORPHEME },
                new POS.Tag[] { POS.Tag.VV, POS.Tag.VV, POS.Tag.E },
                new POS.Tag[] { POS.Tag.E, POS.Tag.VV, POS.Tag.E }
            );
        }

        [Test]
        public void TestUnknownWord()
        {
            AssertAnalyzesTo(analyzer, "2018 평창 동계올림픽대회",
                new String[] { "2018", "평창", "동계", "올림픽", "대회" },
                new int[] { 0, 5, 8, 10, 13 },
                new int[] { 4, 7, 10, 13, 15 },
                new int[] { 1, 1, 1, 1, 1 });

            AssertPartsOfSpeech(analyzer, "2018 평창 동계올림픽대회",
                new POS.Type[] { POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME },
                new POS.Tag[] { POS.Tag.SN, POS.Tag.NNP, POS.Tag.NNP, POS.Tag.NNP, POS.Tag.NNG },
                new POS.Tag[] { POS.Tag.SN, POS.Tag.NNP, POS.Tag.NNP, POS.Tag.NNP, POS.Tag.NNG }
            );

            AssertAnalyzesTo(analyzerUnigram, "2018 평창 동계올림픽대회",
                new String[] { "2", "0", "1", "8", "평창", "동계", "올림픽", "대회" },
                new int[] { 0, 1, 2, 3, 5, 8, 10, 13 },
                new int[] { 1, 2, 3, 4, 7, 10, 13, 15 },
                new int[] { 1, 1, 1, 1, 1, 1, 1, 1 });

            AssertPartsOfSpeech(analyzerUnigram, "2018 평창 동계올림픽대회",
                new POS.Type[] { POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME, },
                new POS.Tag[] { POS.Tag.SY, POS.Tag.SY, POS.Tag.SY, POS.Tag.SY, POS.Tag.NNP, POS.Tag.NNP, POS.Tag.NNP, POS.Tag.NNG },
                new POS.Tag[] { POS.Tag.SY, POS.Tag.SY, POS.Tag.SY, POS.Tag.SY, POS.Tag.NNP, POS.Tag.NNP, POS.Tag.NNP, POS.Tag.NNG }
            );
        }

        [Test]
        public void TestReading()
        {
            AssertReadings(analyzer, "喜悲哀歡", "희비애환");
            AssertReadings(analyzer, "五朔居廬", "오삭거려");
            AssertReadings(analyzer, "가늘라", new String[] { null });
            AssertAnalyzesTo(analyzerReading, "喜悲哀歡",
                new String[] { "희비애환" },
                new int[] { 0 },
                new int[] { 4 },
                new int[] { 1 });
            AssertAnalyzesTo(analyzerReading, "五朔居廬",
                new String[] { "오삭거려" },
                new int[] { 0 },
                new int[] { 4 },
                new int[] { 1 });
            AssertAnalyzesTo(analyzerReading, "가늘라",
                new String[] { "가늘라" },
                new int[] { 0 },
                new int[] { 3 },
                new int[] { 1 });
        }

        [Test]
        public void TestUserDict()
        {
            AssertAnalyzesTo(analyzer, "c++ 프로그래밍 언어",
                new String[] { "c++", "프로그래밍", "언어" },
                new int[] { 0, 4, 10 },
                new int[] { 3, 9, 12 },
                new int[] { 1, 1, 1 }
            );

            AssertPartsOfSpeech(analyzer, "c++ 프로그래밍 언어",
                new POS.Type[] { POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME },
                new POS.Tag[] { POS.Tag.NNG, POS.Tag.NNG, POS.Tag.NNG },
                new POS.Tag[] { POS.Tag.NNG, POS.Tag.NNG, POS.Tag.NNG }
            );

            AssertAnalyzesTo(analyzerDecompound, "정부세종청사",
                new String[] { "정부", "세종", "청사" },
                new int[] { 0, 2, 4 },
                new int[] { 2, 4, 6 },
                new int[] { 1, 1, 1 }
            );

            AssertPartsOfSpeech(analyzerDecompound, "정부세종청사",
                new POS.Type[] { POS.Type.MORPHEME, POS.Type.MORPHEME, POS.Type.MORPHEME },
                new POS.Tag[] { POS.Tag.NNG, POS.Tag.NNG, POS.Tag.NNG },
                new POS.Tag[] { POS.Tag.NNG, POS.Tag.NNG, POS.Tag.NNG }
            );

            AssertAnalyzesTo(analyzer, "대한민국날씨",
                new String[] { "대한민국날씨" },
                new int[] { 0 },
                new int[] { 6 },
                new int[] { 1 }
            );

            AssertAnalyzesTo(analyzer, "21세기대한민국",
                new String[] { "21세기대한민국" },
                new int[] { 0 },
                new int[] { 8 },
                new int[] { 1 }
            );
        }

        [Test]
        public void TestInterpunct()
        {
            AssertAnalyzesTo(analyzer, "도로ㆍ지반ㆍ수자원ㆍ건설환경ㆍ건축ㆍ화재설비연구",
                new String[] { "도로", "지반", "수자원", "건설", "환경", "건축", "화재", "설비", "연구" },
                new int[] { 0, 3, 6, 10, 12, 15, 18, 20, 22 },
                new int[] { 2, 5, 9, 12, 14, 17, 20, 22, 24 },
                new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 }
            );
        }

        /** blast some random strings through the tokenizer */

        [Test]
        public void TestRandomStrings()
        {
            CheckRandomData(Random, analyzer, 500 * RandomMultiplier);
            CheckRandomData(Random, analyzerUnigram, 500 * RandomMultiplier);
            CheckRandomData(Random, analyzerDecompound, 500 * RandomMultiplier);
        }

        /** blast some random large strings through the tokenizer */
        [Test]
        public void TestRandomHugeStrings()
        {
            Random random = Random;
            CheckRandomData(random, analyzer, 20 * RandomMultiplier, 8192);
            CheckRandomData(random, analyzerUnigram, 20 * RandomMultiplier, 8192);
            CheckRandomData(random, analyzerDecompound, 20 * RandomMultiplier, 8192);
        }

        [Test]
        public void TestRandomHugeStringsMockGraphAfter()
        {
            // Randomly inject graph tokens after KoreanTokenizer:
            Random random = Random;
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KoreanTokenizer(NewAttributeFactory(), reader, null, DecompoundMode.MIXED, false);
                TokenStream graph = new MockGraphTokenFilter(random, tokenizer);
                return new TokenStreamComponents(tokenizer, graph);
            });
            //    Analyzer analyzer = new Analyzer()
            //{
            //    @Override
            //      protected TokenStreamComponents createComponents(String fieldName)
            //    {
            //        Tokenizer tokenizer = new KoreanTokenizer(newAttributeFactory(), null, DecompoundMode.MIXED, false);
            //        TokenStream graph = new MockGraphTokenFilter(random(), tokenizer);
            //        return new TokenStreamComponents(tokenizer, graph);
            //    }
            //};
            CheckRandomData(random, analyzer, 20 * RandomMultiplier, 8192);
            analyzer.Dispose();
        }

        [Test]
        public void TestCombining()
        {
            AssertAnalyzesTo(analyzer, "Ба̀лтичко мо̑ре",
                new String[] { "Ба̀лтичко", "мо̑ре" },
                new int[] { 0, 10 },
                new int[] { 9, 15 },
                new int[] { 1, 1 }
            );
            AssertPartsOfSpeech(analyzer, "Ба̀лтичко мо̑ре",
                new POS.Type[] { POS.Type.MORPHEME, POS.Type.MORPHEME },
                new POS.Tag[] { POS.Tag.SL, POS.Tag.SL },
                new POS.Tag[] { POS.Tag.SL, POS.Tag.SL }
            );

            AssertAnalyzesTo(analyzer, "ka̠k̚t͡ɕ͈a̠k̚",
                new String[] { "ka̠k̚t͡ɕ͈a̠k̚" },
                new int[] { 0 },
                new int[] { 13 },
                new int[] { 1 }
            );
            AssertPartsOfSpeech(analyzer, "ka̠k̚t͡ɕ͈a̠k̚",
                new POS.Type[] { POS.Type.MORPHEME },
                new POS.Tag[] { POS.Tag.SL },
                new POS.Tag[] { POS.Tag.SL }
            );

            AssertAnalyzesTo(analyzer, "εἰμί",
                new String[] { "εἰμί" },
                new int[] { 0 },
                new int[] { 4 },
                new int[] { 1 }
            );
            AssertPartsOfSpeech(analyzer, "εἰμί",
                new POS.Type[] { POS.Type.MORPHEME },
                new POS.Tag[] { POS.Tag.SL },
                new POS.Tag[] { POS.Tag.SL }
            );
        }

        private void AssertReadings(Analyzer analyzer, String input, params String[] readings)
        {
            using (TokenStream ts = analyzer.GetTokenStream("ignored", input))
            {
                IReadingAttribute readingAtt = ts.AddAttribute<IReadingAttribute>();
                ts.Reset();
                foreach (String reading in readings)
                {
                    assertTrue(ts.IncrementToken());
                    assertEquals(reading, readingAtt.Reading);
                }
                assertFalse(ts.IncrementToken());
                ts.End();
            }
        }

        private void AssertPartsOfSpeech(Analyzer analyzer, String input, POS.Type[] posTypes, POS.Tag[] leftPosTags, POS.Tag[] rightPosTags)
        {
            Debug.Assert(posTypes.Length == leftPosTags.Length && posTypes.Length == rightPosTags.Length);
            using (TokenStream ts = analyzer.GetTokenStream("ignored", input))
            {
                IPartOfSpeechAttribute partOfSpeechAtt = ts.AddAttribute<IPartOfSpeechAttribute>();
                ts.Reset();
                for (int i = 0; i < posTypes.Length; i++)
                {
                    POS.Type posType = posTypes[i];
                    POS.Tag leftTag = leftPosTags[i];
                    POS.Tag rightTag = rightPosTags[i];
                    assertTrue(ts.IncrementToken());
                    assertEquals(posType, partOfSpeechAtt.POSType);
                    assertEquals(leftTag, partOfSpeechAtt.LeftPOS);
                    assertEquals(rightTag, partOfSpeechAtt.RightPOS);
                }
                assertFalse(ts.IncrementToken());
                ts.End();
            }
        }

    }
}

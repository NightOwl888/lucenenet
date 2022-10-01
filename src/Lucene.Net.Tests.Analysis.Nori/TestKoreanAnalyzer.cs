// Lucene version compatibility level 8.2.0
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
    /// Test Korean morphological analyzer
    /// </summary>
    public class TestKoreanAnalyzer : BaseTokenStreamTestCase
    {
        [Test]
        public void TestSentence()
        {
            Analyzer a = new KoreanAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "한국은 대단한 나라입니다.",
                new String[] { "한국", "대단", "나라", "이" },
                new int[] { 0, 4, 8, 10 },
                new int[] { 2, 6, 10, 13 },
                new int[] { 1, 2, 3, 1 }
            );
            a.Dispose();
        }

        [Test]
        public void TestStopTags()
        {
            ISet<POS.Tag> stopTags = new JCG.HashSet<POS.Tag> { POS.Tag.NNP, POS.Tag.NNG };
            Analyzer a = new KoreanAnalyzer(TEST_VERSION_CURRENT, null, DecompoundMode.DISCARD, stopTags, false);
            AssertAnalyzesTo(a, "한국은 대단한 나라입니다.",
                new String[] { "은", "대단", "하", "ᆫ", "이", "ᄇ니다" },
                new int[] { 2, 4, 6, 6, 10, 10 },
                new int[] { 3, 6, 7, 7, 13, 13 },
                new int[] { 2, 1, 1, 1, 2, 1 }
    );
            a.Dispose();
        }

        [Test]
        public void TestUnknownWord()
        {
            Analyzer a = new KoreanAnalyzer(TEST_VERSION_CURRENT, null, DecompoundMode.DISCARD,
                KoreanPartOfSpeechStopFilter.DEFAULT_STOP_TAGS, true);

            AssertAnalyzesTo(a, "2018 평창 동계올림픽대회",
                new String[] { "2", "0", "1", "8", "평창", "동계", "올림픽", "대회" },
                new int[] { 0, 1, 2, 3, 5, 8, 10, 13 },
                new int[] { 1, 2, 3, 4, 7, 10, 13, 15 },
                new int[] { 1, 1, 1, 1, 1, 1, 1, 1 });
            a.Dispose();

            a = new KoreanAnalyzer(TEST_VERSION_CURRENT, null, DecompoundMode.DISCARD,
                KoreanPartOfSpeechStopFilter.DEFAULT_STOP_TAGS, false);

            AssertAnalyzesTo(a, "2018 평창 동계올림픽대회",
                new String[] { "2018", "평창", "동계", "올림픽", "대회" },
                new int[] { 0, 5, 8, 10, 13 },
                new int[] { 4, 7, 10, 13, 15 },
                new int[] { 1, 1, 1, 1, 1 });
            a.Dispose();
        }

        /**
         * blast random strings against the analyzer
         */
        [Test]
        public void TestRandom()
        {
            Random random = Random;
            Analyzer a = new KoreanAnalyzer(TEST_VERSION_CURRENT);
            CheckRandomData(random, a, AtLeast(1000));
            a.Dispose();
        }

        /**
         * blast some random large strings through the analyzer
         */
        [Test]
        public void TestRandomHugeStrings()
        {
            Random random = Random;
            Analyzer a = new KoreanAnalyzer(TEST_VERSION_CURRENT);
            CheckRandomData(random, a, 2 * RandomMultiplier, 8192);
            a.Dispose();
        }

        // Copied from TestKoreanTokenizer, to make sure passing
        // user dict to analyzer works:
        [Test]
        public void TestUserDict()
        {
            Analyzer analyzer = new KoreanAnalyzer(TEST_VERSION_CURRENT, TestKoreanTokenizer.ReadDict(),
                KoreanTokenizer.DEFAULT_DECOMPOUND, KoreanPartOfSpeechStopFilter.DEFAULT_STOP_TAGS, false);
            AssertAnalyzesTo(analyzer, "c++ 프로그래밍 언어",
                new String[] { "c++", "프로그래밍", "언어" },
                new int[] { 0, 4, 10 },
                new int[] { 3, 9, 12 },
                new int[] { 1, 1, 1 }
            );
        }
    }
}

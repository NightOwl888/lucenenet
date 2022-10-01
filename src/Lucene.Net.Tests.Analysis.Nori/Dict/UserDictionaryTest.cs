// Lucene version compatibility level 8.2.0
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Ko.Dict
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

    public class UserDictionaryTest : LuceneTestCase
    {
        [Test]
        public void TestLookup()
        {
            UserDictionary dictionary = TestKoreanTokenizer.ReadDict();
            String s = "세종";
            char[] sArray = s.toCharArray();
            IList<int> wordIds = dictionary.Lookup(sArray, 0, s.Length);
            assertEquals(1, wordIds.size());
            assertNull(dictionary.GetMorphemes(wordIds[0], sArray, 0, s.Length));

            s = "세종시";
            sArray = s.ToCharArray();
            wordIds = dictionary.Lookup(sArray, 0, s.Length);
            assertEquals(2, wordIds.size());
            assertNull(dictionary.GetMorphemes(wordIds[0], sArray, 0, s.Length));

            Morpheme[] decompound = dictionary.GetMorphemes(wordIds[1], sArray, 0, s.Length);
            assertTrue(decompound.Length == 2);
            assertEquals(decompound[0].posTag, POS.Tag.NNG);
            assertEquals(decompound[0].surfaceForm, "세종");
            assertEquals(decompound[1].posTag, POS.Tag.NNG);
            assertEquals(decompound[1].surfaceForm, "시");

            s = "c++";
            sArray = s.toCharArray();
            wordIds = dictionary.Lookup(sArray, 0, s.Length);
            assertEquals(1, wordIds.size());
            assertNull(dictionary.GetMorphemes(wordIds[0], sArray, 0, s.Length));
        }

        [Test]
        public void TestRead()
        {
            UserDictionary dictionary = TestKoreanTokenizer.ReadDict();
            assertNotNull(dictionary);
        }
    }
}

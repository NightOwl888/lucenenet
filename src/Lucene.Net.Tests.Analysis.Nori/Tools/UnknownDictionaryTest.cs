// Lucene version compatibility level 8.2.0
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Ko.Util
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

    public class UnknownDictionaryTest : LuceneTestCase
    {
        [Test]
        public void TestPutCharacterCategory()
        {
            UnknownDictionaryWriter unkDic = new UnknownDictionaryWriter(10 * 1024 * 1024);

            try
            {
                unkDic.PutCharacterCategory(0, "DUMMY_NAME");
                fail();
            }
            catch (Exception e)
            {

            }

            try
            {
                unkDic.PutCharacterCategory(-1, "HANGUL");
                fail();
            }
            catch (Exception e)
            {

            }

            unkDic.PutCharacterCategory(0, "DEFAULT");
            unkDic.PutCharacterCategory(1, "GREEK");
            unkDic.PutCharacterCategory(2, "HANJA");
            unkDic.PutCharacterCategory(3, "HANGUL");
            unkDic.PutCharacterCategory(4, "KANJI");
        }

        [Test]
        public void TestPut()
        {
            UnknownDictionaryWriter unkDic = new UnknownDictionaryWriter(10 * 1024 * 1024);
            try
            {
                unkDic.Put(CSVUtil.Parse("HANGUL,1800,3562,UNKNOWN,*,*,*,*,*,*,*"));
                fail();
            }
            catch (Exception e)
            {

            }

            String entry1 = "ALPHA,1793,3533,795,SL,*,*,*,*,*,*,*";
            String entry2 = "HANGUL,1800,3562,10247,UNKNOWN,*,*,*,*,*,*,*";
            String entry3 = "HANJA,1792,3554,-821,SH,*,*,*,*,*,*,*";

            unkDic.PutCharacterCategory(0, "ALPHA");
            unkDic.PutCharacterCategory(1, "HANGUL");
            unkDic.PutCharacterCategory(2, "HANJA");

            unkDic.Put(CSVUtil.Parse(entry1));
            unkDic.Put(CSVUtil.Parse(entry2));
            unkDic.Put(CSVUtil.Parse(entry3));
        }
    }
}

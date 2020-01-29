using Lucene.Net.Attributes;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Support
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

    [TestFixture]
    public class TestWeakTablePerformance
    {
        WeakTable<object, object> dictionary;
        SmallObject[] keys;


        [SetUp]
        public void Setup()
        {
            dictionary = TestWeakTableBehavior.CreateDictionary();
        }

        private void Fill(WeakTable<object, object> dictionary)
        {
            foreach (SmallObject key in keys)
                dictionary.Add(key, "value");
        }

        [OneTimeSetUp]
        public void TestSetup()
        {
            keys = new SmallObject[100000];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = new SmallObject(i);
        }

        [Test, LuceneNetSpecific]
        public void Test_Performance_Add()
        {
            for (int i = 0; i < 10; i++)
            {
                dictionary.Clear();
                Fill(dictionary);
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_Performance_Remove()
        {
            for (int i = 0; i < 10; i++)
            {
                Fill(dictionary);
                foreach (SmallObject key in keys)
                    dictionary.Remove(key);
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_Performance_Replace()
        {
            for (int i = 0; i < 10; i++)
            {
                foreach (SmallObject key in keys)
                    dictionary.AddOrUpdate(key, "value2");
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_Performance_Access()
        {
            Fill(dictionary);
            for (int i = 0; i < 10; i++)
            {
                foreach (SmallObject key in keys)
                {
                    object value = dictionary.GetOrCreateValue(key);
                }
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_Performance_Contains()
        {
            Fill(dictionary);
            for (int i = 0; i < 10; i++)
            {
                foreach (SmallObject key in keys)
                {
                    dictionary.TryGetValue(key, out object _);
                }
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_Performance_Keys()
        {
            Fill(dictionary);
            for (int i = 0; i < 100; i++)
            {
                var keys = dictionary.Select(pair => pair.Key);
            }
        }

        [Test, LuceneNetSpecific]
        public void Test_Performance_ForEach()
        {
            Fill(dictionary);
            for (int i = 0; i < 10; i++)
            {
                foreach (var de in dictionary)
                {

                }
            }
        }
    }
}
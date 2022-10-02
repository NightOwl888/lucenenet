// Lucene version compatibility level 8.2.0
using Lucene.Net.Util;
using Lucene.Net.Support.Util.Fst;
using NUnit.Framework;
using System;
using System.Linq;
using Long = J2N.Numerics.Int64;

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

    public class TestTokenInfoDictionary : LuceneTestCase
    {
        /** enumerates the entire FST/lookup data and just does basic sanity checks */
        [Test]
        public void TestEnumerateAll()
        {
            // just for debugging
            int numTerms = 0;
            int numWords = 0;
            int lastWordId = -1;
            int lastSourceId = -1;
            CharacterDefinition charDef = CharacterDefinition.Instance;
            TokenInfoDictionary tid = TokenInfoDictionary.Instance;
            ConnectionCosts matrix = ConnectionCosts.Instance;
            FST<Long> fst = tid.FST.InternalFST;
            Int32sRefFSTEnum<Long> fstEnum = new Int32sRefFSTEnum<Long>(fst);
            Int32sRefFSTEnum.InputOutput<Long> mapping;
            Int32sRef scratch = new Int32sRef();
            while (fstEnum.MoveNext())
            {
                mapping = fstEnum.Current;
                numTerms++;
                Int32sRef input = mapping.Input;
                char[] chars = new char[input.Length];
                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] = (char)input.Int32s[input.Offset + i];
                }
                string surfaceForm = new string(chars);
                assertTrue(surfaceForm.Any());
                assertEquals(surfaceForm.Trim(), surfaceForm);
                assertTrue(UnicodeUtil.ValidUTF16String(surfaceForm));

                long? output = mapping.Output;
                int sourceId = (int)output.Value;
                // we walk in order, terms, sourceIds, and wordIds should always be increasing
                assertTrue(sourceId > lastSourceId);
                lastSourceId = sourceId;
                tid.LookupWordIds(sourceId, scratch);
                for (int i = 0; i < scratch.Length; i++)
                {
                    numWords++;
                    int wordId = scratch.Int32s[scratch.Offset + i];
                    assertTrue(wordId > lastWordId);
                    lastWordId = wordId;

                    int leftId = tid.GetLeftId(wordId);
                    int rightId = tid.GetRightId(wordId);

                    matrix.Get(rightId, leftId);

                    tid.GetWordCost(wordId);

                    POS.Type type = tid.GetPOSType(wordId);
                    POS.Tag leftPOS = tid.GetLeftPOS(wordId);
                    POS.Tag rightPOS = tid.GetRightPOS(wordId);

                    if (type == POS.Type.MORPHEME)
                    {
                        assertTrue(leftPOS == rightPOS);
                        string reading = tid.GetReading(wordId);
                        bool isHanja = charDef.IsHanja(surfaceForm[0]);
                        if (isHanja)
                        {
                            assertTrue(reading != null);
                            for (int j = 0; j < reading.Length; j++)
                            {
                                assertTrue(charDef.IsHangul(reading[j]));
                            }
                        }
                        if (reading != null)
                        {
                            assertTrue(UnicodeUtil.ValidUTF16String(reading));
                        }
                    }
                    else
                    {
                        if (type == POS.Type.COMPOUND)
                        {
                            assertTrue(leftPOS == rightPOS);
                            assertTrue(leftPOS == POS.Tag.NNG || rightPOS == POS.Tag.NNP);
                        }
                        Morpheme[] decompound = tid.GetMorphemes(wordId, chars, 0, chars.Length);
                        if (decompound != null)
                        {
                            int offset = 0;
                            foreach (Morpheme morph in decompound)
                            {
                                assertTrue(UnicodeUtil.ValidUTF16String(morph.surfaceForm));
                                assertTrue(morph.surfaceForm.Any());
                                assertEquals(morph.surfaceForm.Trim(), morph.surfaceForm);
                                if (type != POS.Type.INFLECT)
                                {
                                    assertEquals(morph.surfaceForm, surfaceForm.Substring(offset, morph.surfaceForm.Length)); // LUCENENET: (offset + morph.surfaceForm.Length) - offset == morph.surfaceForm.Length
                                    offset += morph.surfaceForm.Length;
                                }
                            }
                            assertTrue(offset <= surfaceForm.Length);
                        }
                    }
                }
            }
            if (Verbose)
            {
                Console.Out.WriteLine("checked " + numTerms + " terms, " + numWords + " words.");
            }
        }
    }
}

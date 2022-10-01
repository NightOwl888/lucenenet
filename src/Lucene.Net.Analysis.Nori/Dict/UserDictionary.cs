// Lucene version compatibility level 8.2.0
using J2N.Text;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;
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

    /// <summary>
    /// Class for building a User Dictionary.
    /// This class allows for adding custom nouns (세종) or compounds (세종시 세종 시).
    /// </summary>
    public sealed class UserDictionary : IDictionary
    {
        // text -> wordID
        private readonly TokenInfoFST fst;

        public const int WORD_COST = -100000;

        // NNG left
        public const short LEFT_ID = 1781;

        // NNG right
        public const short RIGHT_ID = 3533;
        // NNG right with hangul and a coda on the last char
        public const short RIGHT_ID_T = 3535;
        // NNG right with hangul and no coda on the last char
        public const short RIGHT_ID_F = 3534;

        // length, length... indexed by compound ID or null for simple noun
        private readonly int[][] segmentations;
        private readonly short[] rightIds;

        private static readonly Regex specialChars = new Regex(@"#.*$", RegexOptions.Compiled);
        private static readonly Regex whiteSpace = new Regex(@"\\s+", RegexOptions.Compiled);

        public static UserDictionary Open(TextReader reader)
        {

            //BufferedReader br = new BufferedReader(reader);
            string line = null;
            JCG.List<string> entries = new JCG.List<string>();

            // text + optional segmentations
            while ((line = reader.ReadLine()) != null)
            {
                // Remove comments
                //line = line.ReplaceAll("#.*$", "");
                line = specialChars.Replace(line, "");

                // Skip empty lines or comment lines
                if (line.Trim().Length == 0)
                {
                    continue;
                }
                entries.Add(line);
            }

            if (entries.Count == 0)
            {
                return null;
            }
            else
            {
                return new UserDictionary(entries);
            }
        }

        /*private class SortComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                var x0 = whiteSpace.Split(x).TrimEnd()[0];
                var y0 = whiteSpace.Split(y).TrimEnd()[0];
                return x0.CompareToOrdinal(y0);
            }
        }*/

        private UserDictionary(JCG.List<string> entries)
        {
            CharacterDefinition charDef = CharacterDefinition.Instance;
            //entries.Sort(Comparator.comparing(e -> e.split("\\s+")[0]));
            entries.Sort(Comparer<string>.Create((x, y) =>
            {
                var x0 = whiteSpace.Split(x).TrimEnd()[0];
                var y0 = whiteSpace.Split(y).TrimEnd()[0];
                return x0.CompareToOrdinal(y0);
            }));

            PositiveInt32Outputs fstOutput = PositiveInt32Outputs.Singleton;
            Builder<Long> fstBuilder = new Builder<Long>(Lucene.Net.Util.Fst.FST.INPUT_TYPE.BYTE2, fstOutput);
            Int32sRef scratch = new Int32sRef();

            string lastToken = null;
            JCG.List<int[]> segmentations = new JCG.List<int[]>(entries.Count);
            JCG.List<short> rightIds = new JCG.List<short>(entries.Count);
            long ord = 0;
            foreach (string entry in entries)
            {
                //string[] splits = entry.split("\\s+");
                string[] splits = whiteSpace.Split(entry).TrimEnd();
                string token = splits[0];
                if (token.Equals(lastToken, StringComparison.Ordinal))
                {
                    continue;
                }
                char lastChar = entry[entry.Length - 1];
                if (charDef.IsHangul(lastChar))
                {
                    if (charDef.HasCoda(lastChar))
                    {
                        rightIds.Add(RIGHT_ID_T);
                    }
                    else
                    {
                        rightIds.Add(RIGHT_ID_F);
                    }
                }
                else
                {
                    rightIds.Add(RIGHT_ID);
                }

                if (splits.Length == 1)
                {
                    segmentations.Add(null);
                }
                else
                {
                    int[] length = new int[splits.Length - 1];
                    int offset = 0;
                    for (int i = 1; i < splits.Length; i++)
                    {
                        length[i - 1] = splits[i].Length;
                        offset += splits[i].Length;
                    }
                    if (offset > token.Length)
                    {
                        throw IllegalArgumentException.Create("Illegal user dictionary entry " + entry +
                            " - the segmentation is bigger than the surface form (" + token + ")");
                    }
                    segmentations.Add(length);
                }

                // add mapping to FST
                scratch.Grow(token.Length);
                scratch.Length = token.Length;
                for (int i = 0; i < token.Length; i++)
                {
                    //scratch.setIntAt(i, (int) token[i]);
                    scratch.Int32s[i] = (int)token[i];
                }
                //fstBuilder.Add(new Int32sRef(scratch.Int32s, scratch.Offset, scratch.Length), ord);
                fstBuilder.Add(scratch, ord);
                lastToken = token;
                ord++;
            }
            this.fst = new TokenInfoFST(fstBuilder.Finish());
            this.segmentations = segmentations.ToArray();
            this.rightIds = new short[rightIds.Count];
            for (int i = 0; i < rightIds.Count; i++)
            {
                this.rightIds[i] = rightIds[i];
            }
        }

        public TokenInfoFST FST => fst;

        public int GetLeftId(int wordId)
        {
            return LEFT_ID;
        }

        public int GetRightId(int wordId)
        {
            return rightIds[wordId];
        }

        public int GetWordCost(int wordId)
        {
            return WORD_COST;
        }

        public POS.Type GetPOSType(int wordId)
        {
            if (segmentations[wordId] == null)
            {
                return POS.Type.MORPHEME;
            }
            else
            {
                return POS.Type.COMPOUND;
            }
        }

        public POS.Tag GetLeftPOS(int wordId)
        {
            return POS.Tag.NNG;
        }

        public POS.Tag GetRightPOS(int wordId)
        {
            return POS.Tag.NNG;
        }

        public string GetReading(int wordId)
        {
            return null;
        }

        public Morpheme[] GetMorphemes(int wordId, char[] surfaceForm, int off, int len)
        {
            int[] segs = segmentations[wordId];
            if (segs == null)
            {
                return null;
            }
            int offset = 0;
            Morpheme[] morphemes = new Morpheme[segs.Length];
            for (int i = 0; i < segs.Length; i++)
            {
                morphemes[i] = new Morpheme(POS.Tag.NNG, new string(surfaceForm, off + offset, segs[i]));
                offset += segs[i];
            }
            return morphemes;
        }

        /// <summary>
        /// Lookup words in text.
        /// </summary>
        /// <param name="chars">Text.</param>
        /// <param name="off">Offset into text.</param>
        /// <param name="len">Length of text.</param>
        /// <returns>Array of wordId.</returns>
        public IList<int> Lookup(char[] chars, int off, int len)
        {
            JCG.List<int> result = new JCG.List<int>();
            FST.BytesReader fstReader = fst.GetBytesReader();

            FST.Arc<Long> arc = new FST.Arc<Long>();
            int end = off + len;
            for (int startOffset = off; startOffset < end; startOffset++)
            {
                arc = fst.GetFirstArc(arc);
                int output = 0;
                int remaining = end - startOffset;
                for (int i = 0; i < remaining; i++)
                {
                    int ch = chars[startOffset + i];
                    if (fst.FindTargetArc(ch, arc, arc, i == 0, fstReader) == null)
                    {
                        break; // continue to next position
                    }
                    output += arc.Output.ToInt32();
                    if (arc.IsFinal)
                    {
                        int finalOutput = output + arc.NextFinalOutput.ToInt32();
                        result.Add(finalOutput);
                    }
                }
            }
            return result;
        }
    }
}

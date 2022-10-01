// Lucene version compatibility level 8.2.0
using J2N.IO;
using Lucene.Net.Analysis.Ko.Dict;
using Lucene.Net.Codecs;
using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;
using Short = J2N.Numerics.Int16;

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

    public abstract class BinaryDictionaryWriter
    {
        protected readonly Type m_implClazz;
        protected ByteBuffer m_buffer;
        private int targetMapEndOffset = 0, lastWordId = -1, lastSourceId = -1;
        private int[] targetMap = new int[8192];
        private int[] targetMapOffsets = new int[8192];
        private readonly IList<string> posDict = new JCG.List<string>();

        private static readonly Regex expressionSplit = new Regex("\\+", RegexOptions.Compiled);
        private static readonly Regex expressionTokenSplit = new Regex("\\/", RegexOptions.Compiled);

        public BinaryDictionaryWriter(Type implClazz, int size)
        {
            this.m_implClazz = implClazz;
            m_buffer = ByteBuffer.Allocate(size);
        }

        /// <summary>
        /// Put the entry in map.
        /// <para/>
        /// mecab-ko-dic features:
        /// <code>
        /// 0   - surface
        /// 1   - left cost
        /// 2   - right cost
        /// 3   - word cost
        /// 4   - part of speech0+part of speech1+...
        /// 5   - semantic class
        /// 6   - T if the last character of the surface form has a coda, F otherwise
        /// 7   - reading
        /// 8   - POS type (*, Compound, Inflect, Preanalysis)
        /// 9   - left POS
        /// 10  - right POS
        /// 11  - expression
        /// </code>
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>Current position of buffer, which will be wordId of next entry.</returns>
        public virtual int Put(string[] entry)
        {
            short leftId = Short.Parse(entry[1], CultureInfo.InvariantCulture);
            short rightId = Short.Parse(entry[2], CultureInfo.InvariantCulture);
            short wordCost = Short.Parse(entry[3], CultureInfo.InvariantCulture);

            POS.Type posType = POS.ResolveType(entry[8]);
            POS.Tag leftPOS;
            POS.Tag rightPOS;
            if (posType == POS.Type.MORPHEME || posType == POS.Type.COMPOUND || entry[9].Equals("*"))
            {
                leftPOS = POS.ResolveTag(entry[4]);
                if (Debugging.AssertsEnabled) Debugging.Assert(entry[9].Equals("*") && entry[10].Equals("*"));
                rightPOS = leftPOS;
            }
            else
            {
                leftPOS = POS.ResolveTag(entry[9]);
                rightPOS = POS.ResolveTag(entry[10]);
            }
            string reading = entry[7].Equals("*") ? "" : entry[0].Equals(entry[7]) ? "" : entry[7];
            string expression = entry[11].Equals("*") ? "" : entry[11];

            // extend buffer if necessary
            int left = m_buffer.Remaining;
            // worst case, 3 short + 4 bytes and features (all as utf-16)
            int worstCase = 9 + 2 * (expression.Length + reading.Length);
            if (worstCase > left)
            {
                ByteBuffer newBuffer = ByteBuffer.Allocate(ArrayUtil.Oversize(m_buffer.Limit + worstCase - left, 1));
                m_buffer.Flip();
                newBuffer.Put(m_buffer);
                m_buffer = newBuffer;
            }

            // add pos mapping
            int toFill = 1 + leftId - posDict.Count;
            for (int i = 0; i < toFill; i++)
            {
                posDict.Add(null);
            }
            string fullPOSData = leftPOS.ToString() + "," + entry[5];
            string existing = posDict[leftId];
            if (Debugging.AssertsEnabled) Debugging.Assert(existing == null || existing.Equals(fullPOSData));
            posDict[leftId] = fullPOSData;

            IList<Morpheme> morphemes = new JCG.List<Morpheme>();
            // true if the POS and decompounds of the token are all the same.
            bool hasSinglePOS = (leftPOS == rightPOS);
            if (posType != POS.Type.MORPHEME && expression.Length > 0)
            {
                string[] exprTokens = expressionSplit.Split(expression);
                for (int i = 0; i < exprTokens.Length; i++)
                {
                    string[] tokenSplit = expressionTokenSplit.Split(exprTokens[i]);
                    if (Debugging.AssertsEnabled) Debugging.Assert(tokenSplit.Length == 3);
                    string surfaceForm = tokenSplit[0].Trim();
                    if (surfaceForm.Length > 0)
                    {
                        POS.Tag exprTag = POS.ResolveTag(tokenSplit[1]);
                        morphemes.Add(new Morpheme(exprTag, tokenSplit[0]));
                        if (leftPOS != exprTag)
                        {
                            hasSinglePOS = false;
                        }
                    }
                }
            }

            int flags = 0;
            if (hasSinglePOS)
            {
                flags |= BinaryDictionary.HAS_SINGLE_POS;
            }
            if (posType == POS.Type.MORPHEME && reading.Length > 0)
            {
                flags |= BinaryDictionary.HAS_READING;
            }

            if (Debugging.AssertsEnabled) Debugging.Assert(leftId < 8192); // there are still unused bits
            if (Debugging.AssertsEnabled) Debugging.Assert((byte)posType < 4);
            m_buffer.PutInt16((short)(leftId << 2 | (byte)posType));
            m_buffer.PutInt16((short)(rightId << 2 | flags));
            m_buffer.PutInt16(wordCost);

            if (posType == POS.Type.MORPHEME)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(leftPOS == rightPOS);
                if (reading.Length > 0)
                {
                    WriteString(reading);
                }
            }
            else
            {
                if (hasSinglePOS == false)
                {
                    m_buffer.Put((byte)rightPOS);
                }
                m_buffer.Put((byte)morphemes.Count);
                int compoundOffset = 0;
                foreach (Morpheme morpheme in morphemes)
                {
                    if (hasSinglePOS == false)
                    {
                        m_buffer.Put((byte)morpheme.posTag);
                    }
                    if (posType != POS.Type.INFLECT)
                    {
                        m_buffer.Put((byte)morpheme.surfaceForm.Length);
                        compoundOffset += morpheme.surfaceForm.Length;
                    }
                    else
                    {
                        WriteString(morpheme.surfaceForm);
                    }
                    if (Debugging.AssertsEnabled) Debugging.Assert(compoundOffset <= entry[0].Length, Arrays.ToString(entry));
                }
            }
            return m_buffer.Position;
        }

        private void WriteString(string s)
        {
            m_buffer.Put((byte)s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                m_buffer.PutChar(s[i]);
            }
        }

        public virtual void AddMapping(int sourceId, int wordId)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(wordId > lastWordId, "words out of order: " + wordId + " vs lastID: " + lastWordId);

            if (sourceId > lastSourceId)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(sourceId > lastSourceId, "source ids out of order: lastSourceId=" + lastSourceId + " vs sourceId=" + sourceId);
                targetMapOffsets = ArrayUtil.Grow(targetMapOffsets, sourceId + 1);
                for (int i = lastSourceId + 1; i <= sourceId; i++)
                {
                    targetMapOffsets[i] = targetMapEndOffset;
                }
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(sourceId == lastSourceId);
            }

            targetMap = ArrayUtil.Grow(targetMap, targetMapEndOffset + 1);
            targetMap[targetMapEndOffset] = wordId;
            targetMapEndOffset++;

            lastSourceId = sourceId;
            lastWordId = wordId;
        }

        protected string GetBaseFileName(string baseDir)
        {
            // LUCENENET specific: we don't need to do a "classpath" output directory, since we
            // are changing the implementation to read files dynamically instead of making the
            // user recompile with the new files.
            return System.IO.Path.Combine(baseDir, m_implClazz.Name);

            //return baseDir + File.separator + implClazz.getName().replace('.', File.separatorChar);
        }

        /// <summary>
        /// Write dictionary in file.
        /// </summary>
        /// <exception cref="IOException">If an I/O error occurs writing the dictionary files.</exception>
        public virtual void Write(string baseDir)
        {
            string baseName = GetBaseFileName(baseDir);
            WriteDictionary(baseName + BinaryDictionary.DICT_FILENAME_SUFFIX);
            WriteTargetMap(baseName + BinaryDictionary.TARGETMAP_FILENAME_SUFFIX);
            WritePosDict(baseName + BinaryDictionary.POSDICT_FILENAME_SUFFIX);
        }

        protected virtual void WriteTargetMap(string filename)
        {
            //new File(filename).getParentFile().mkdirs();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filename));
            using (Stream os = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                //os = new BufferedOutputStream(os);
                DataOutput output = new OutputStreamDataOutput(os);
                CodecUtil.WriteHeader(output, BinaryDictionary.TARGETMAP_HEADER, BinaryDictionary.VERSION);

                int numSourceIds = lastSourceId + 1;
                output.WriteVInt32(targetMapEndOffset); // <-- size of main array
                output.WriteVInt32(numSourceIds + 1); // <-- size of offset array (+ 1 more entry)
                int prev = 0, sourceId = 0;
                for (int ofs = 0; ofs < targetMapEndOffset; ofs++)
                {
                    int val = targetMap[ofs], delta = val - prev;
                    if (Debugging.AssertsEnabled) Debugging.Assert(delta >= 0);
                    if (ofs == targetMapOffsets[sourceId])
                    {
                        output.WriteVInt32((delta << 1) | 0x01);
                        sourceId++;
                    }
                    else
                    {
                        output.WriteVInt32((delta << 1));
                    }
                    prev += delta;
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(sourceId == numSourceIds, "sourceId:" + sourceId + " != numSourceIds:" + numSourceIds);
            }
        }

        protected virtual void WritePosDict(string filename)
        {
            //new File(filename).getParentFile().mkdirs();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filename));
            using (Stream os = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                //os = new BufferedOutputStream(os);
                DataOutput output = new OutputStreamDataOutput(os);
                CodecUtil.WriteHeader(output, BinaryDictionary.POSDICT_HEADER, BinaryDictionary.VERSION);
                output.WriteVInt32(posDict.Count);
                foreach (string s in posDict)
                {
                    if (s == null)
                    {
                        output.WriteByte((byte)POS.Tag.UNKNOWN);
                    }
                    else
                    {
                        string[] data = CSVUtil.Parse(s);
                        if (Debugging.AssertsEnabled) Debugging.Assert(data.Length == 2, "malformed pos/semanticClass: " + s);
                        output.WriteByte((byte)Enum.Parse(typeof(POS.Tag), data[0], true));
                    }
                }
            }
        }

        protected virtual void WriteDictionary(string filename)
        {
            //new File(filename).getParentFile().mkdirs();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filename));
            using (FileStream os = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                DataOutput output = new OutputStreamDataOutput(os);
                CodecUtil.WriteHeader(output, BinaryDictionary.DICT_HEADER, BinaryDictionary.VERSION);
                output.WriteVInt32(m_buffer.Position);
                //WritableByteChannel channel = Channels.newChannel(os);
                
                // Write Buffer
                m_buffer.Flip();  // set position to 0, set limit to current position
                                //channel.write(buffer);

                while (m_buffer.HasRemaining)
                {
                    output.WriteByte(m_buffer.Get());
                }

                if (Debugging.AssertsEnabled) Debugging.Assert(m_buffer.Remaining == 0L);


            }
        }
    }
}

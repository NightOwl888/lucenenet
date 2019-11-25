﻿using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Reflection;
using System.Security;

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
    /// Base class for a binary-encoded in-memory dictionary.
    /// </summary>
    public abstract class BinaryDictionary : IDictionary
    {
        public const string TARGETMAP_FILENAME_SUFFIX = "$targetMap.dat";
        public const string DICT_FILENAME_SUFFIX = "$buffer.dat";
        public const string POSDICT_FILENAME_SUFFIX = "$posDict.dat";

        public const string DICT_HEADER = "ko_dict";
        public const string TARGETMAP_HEADER = "ko_dict_map";
        public const string POSDICT_HEADER = "ko_dict_pos";
        public const int VERSION = 1;

        private readonly ByteBuffer buffer;
        private readonly int[] targetMapOffsets, targetMap;
        private readonly POS.Tag[] posDict;

        // LUCENENET specific - variable to hold the name of the data directory (or empty string to load embedded resources)
        private static readonly string DATA_DIR = LoadDataDirectory();
        // LUCENENET specific - name of the subdirectory inside of the directory where the Nori dictionary files reside.
        private const string DATA_SUBDIR = "nori-data";

        private static string LoadDataDirectory()
        {
            string currentPath = SystemProperties.GetProperty("nori.data.dir",
#if NETSTANDARD1_6
                System.AppContext.BaseDirectory
#else
                AppDomain.CurrentDomain.BaseDirectory
#endif
                );

            // If a matching directory path is found, set our DATA_DIR static
            // variable. If it is null or empty after this process, we need to
            // load the embedded files.
            string candidatePath = System.IO.Path.Combine(currentPath, DATA_SUBDIR);
            if (System.IO.Directory.Exists(candidatePath))
            {
                return candidatePath;
            }

            while (new DirectoryInfo(currentPath).Parent != null)
            {
                try
                {
                    candidatePath = System.IO.Path.Combine(new DirectoryInfo(currentPath).Parent.FullName, DATA_SUBDIR);
                    if (System.IO.Directory.Exists(candidatePath))
                    {
                        return candidatePath;
                    }
                    currentPath = new DirectoryInfo(currentPath).Parent.FullName;
                }
                catch (SecurityException)
                {
                    // ignore security errors
                }
            }

            return null;
        }

        protected BinaryDictionary()
        {
            Stream mapIS = null, dictIS = null, posIS = null;
            int[] targetMapOffsets = null, targetMap = null;
            ByteBuffer buffer = null;
            DataInput input;
            //bool success = false;
            //try
            //{

                using (mapIS = GetResource(TARGETMAP_FILENAME_SUFFIX))
                {
                    //mapIS = new BufferedInputStream(mapIS);
                    input = new InputStreamDataInput(mapIS);
                    CodecUtil.CheckHeader(input, TARGETMAP_HEADER, VERSION, VERSION);
                    targetMap = new int[input.ReadVInt32()];
                    targetMapOffsets = new int[input.ReadVInt32()];
                    int accum = 0, sourceId = 0;
                    for (int ofs = 0; ofs < targetMap.Length; ofs++)
                    {
                        int val = input.ReadVInt32();
                        if ((val & 0x01) != 0)
                        {
                            targetMapOffsets[sourceId] = ofs;
                            sourceId++;
                        }
                        accum += (int)(((uint)val) >> 1);
                        targetMap[ofs] = accum;
                    }
                    if (sourceId + 1 != targetMapOffsets.Length)
                        throw new IOException("targetMap file format broken");
                    targetMapOffsets[sourceId] = targetMap.Length;
                }
                mapIS = null;

                using (posIS = GetResource(POSDICT_FILENAME_SUFFIX))
                {
                    //posIS = new BufferedInputStream(posIS);
                    input = new InputStreamDataInput(posIS);
                    CodecUtil.CheckHeader(input, POSDICT_HEADER, VERSION, VERSION);
                    int posSize = input.ReadVInt32();
                    posDict = new POS.Tag[posSize];
                    for (int j = 0; j < posSize; j++)
                    {
                        posDict[j] = POS.ResolveTag(input.ReadByte());
                    }
                }
                posIS = null;

                ByteBuffer tmpBuffer;
                using (dictIS = GetResource(DICT_FILENAME_SUFFIX))
                {
                    // no buffering here, as we load in one large buffer
                    input = new InputStreamDataInput(dictIS);
                    CodecUtil.CheckHeader(input, DICT_HEADER, VERSION, VERSION);
                    int size = input.ReadVInt32();
                    tmpBuffer = ByteBuffer.Allocate(size); // AllocateDirect..?
                    int read = dictIS.Read(tmpBuffer.Array, 0, size);
                    if (read != size)
                    {
                        throw new EndOfStreamException("Cannot read whole dictionary");
                    }

                    //ReadableByteChannel channel = Channels.newChannel(dictIS);
                    //int read = channel.read(tmpBuffer);
                    //if (read != size) {
                    //    throw new EndOfStreamException("Cannot read whole dictionary");
                    //}
                }
                dictIS = null;
                buffer = tmpBuffer.AsReadOnlyBuffer();
            //    success = true;
            //}
            //finally
            //{
            //    if (success)
            //    {
            //        IOUtils.Dispose(mapIS, dictIS);
            //    }
            //    else
            //    {
            //        IOUtils.DisposeWhileHandlingException(mapIS, dictIS);
            //    }
            //}

            this.targetMap = targetMap;
            this.targetMapOffsets = targetMapOffsets;
            this.buffer = buffer;
        }

        protected Stream GetResource(string suffix)
        {
            return GetTypeResource(GetType(), suffix);
        }

        // util, reused by ConnectionCosts and CharacterDefinition
        public static Stream GetTypeResource(Type clazz, string suffix)
        {
            string fileName = clazz.Name + suffix;

            // LUCENENET specific: Rather than forcing the end user to recompile if they want to use a custom dictionary,
            // we load the data from the nori-data directory (which can be set via the nori.data.dir environment variable).
            if (string.IsNullOrEmpty(DATA_DIR))
            {
                Stream @is = clazz.GetTypeInfo().Assembly.FindAndGetManifestResourceStream(clazz, fileName);
                if (@is == null)
                    throw new FileNotFoundException("Not in assembly: " + clazz.FullName + suffix);
                return @is;
            }

            // We have a data directory, so first check if the file exists
            string path = System.IO.Path.Combine(DATA_DIR, fileName);
            if (!System.IO.File.Exists(path))
            {
                throw new FileNotFoundException(string.Format("Expected file '{0}' not found. " +
                    "If the '{1}' directory exists, this file is required. " +
                    "Either remove the '{3}' directory or generate the required dictionary files using the lucene-cli tool.",
                    fileName, DATA_DIR, DATA_SUBDIR));
            }

            // The file exists - open a stream.
            return new FileStream(path, FileMode.Open, FileAccess.Read);

            //Stream @is = clazz.getResourceAsStream(clazz.getSimpleName() + suffix);
            //if (@is == null)
            //  throw new FileNotFoundException("Not in classpath: " + clazz.getName().replace('.','/') + suffix);
            //return @is;
        }

        public virtual void LookupWordIds(int sourceId, Int32sRef @ref)
        {
            @ref.Int32s = targetMap;
            @ref.Offset = targetMapOffsets[sourceId];
            // targetMapOffsets always has one more entry pointing behind last:
            @ref.Length = targetMapOffsets[sourceId + 1] - @ref.Offset;
        }

        public virtual int GetLeftId(int wordId)
        {
            return (short)((ushort)buffer.GetInt16(wordId)) >> 2;
        }

        public virtual int GetRightId(int wordId)
        {
            return (short)((ushort)buffer.GetInt16(wordId + 2)) >> 2; // Skip left id
        }

        public virtual int GetWordCost(int wordId)
        {
            return buffer.GetInt16(wordId + 4);  // Skip left and right id
        }

        public virtual POS.Type GetPOSType(int wordId)
        {
            byte value = (byte)(buffer.GetInt16(wordId) & 3);
            return POS.ResolveType(value);
        }

        public virtual POS.Tag GetLeftPOS(int wordId)
        {
            return posDict[GetLeftId(wordId)];
        }

        public virtual POS.Tag GetRightPOS(int wordId)
        {
            POS.Type type = GetPOSType(wordId);
            if (type == POS.Type.MORPHEME || type == POS.Type.COMPOUND || HasSinglePOS(wordId))
            {
                return GetLeftPOS(wordId);
            }
            else
            {
                byte value = buffer.Get(wordId + 6);
                return POS.ResolveTag(value);
            }
        }

        public virtual string GetReading(int wordId)
        {
            if (HasReadingData(wordId))
            {
                int offset = wordId + 6;
                return ReadString(offset);
            }
            return null;
        }

        public virtual Morpheme[] GetMorphemes(int wordId, char[] surfaceForm, int off, int len)
        {
            POS.Type posType = GetPOSType(wordId);
            if (posType == POS.Type.MORPHEME)
            {
                return null;
            }
            int offset = wordId + 6;
            bool hasSinglePos = HasSinglePOS(wordId);
            if (hasSinglePos == false)
            {
                offset++; // skip rightPOS
            }
            int length = buffer.Get(offset++);
            if (length == 0)
            {
                return null;
            }
            Morpheme[] morphemes = new Morpheme[length];
            int surfaceOffset = 0;
            POS.Tag leftPOS = GetLeftPOS(wordId);
            for (int i = 0; i < length; i++)
            {
                string form;
                POS.Tag tag = hasSinglePos ? leftPOS : POS.ResolveTag(buffer.Get(offset++));
                if (posType == POS.Type.INFLECT)
                {
                    form = ReadString(offset);
                    offset += form.Length * 2 + 1;
                }
                else
                {
                    int formLen = buffer.Get(offset++);
                    form = new string(surfaceForm, off + surfaceOffset, formLen);
                    surfaceOffset += formLen;
                }
                morphemes[i] = new Morpheme(tag, form);
            }
            return morphemes;
        }

        private string ReadString(int offset)
        {
            int strOffset = offset;
            int len = buffer.Get(strOffset++);
            char[] text = new char[len];
            for (int i = 0; i < len; i++)
            {
                text[i] = buffer.GetChar(strOffset + (i << 1));
            }
            return new string(text);
        }

        private bool HasSinglePOS(int wordId)
        {
            return (buffer.GetInt16(wordId + 2) & HAS_SINGLE_POS) != 0;
        }

        private bool HasReadingData(int wordId)
        {
            return (buffer.GetInt16(wordId + 2) & HAS_READING) != 0;
        }

        /// <summary>Flag that the entry has a single part of speech (leftPOS).</summary>
        public const int HAS_SINGLE_POS = 1;

        /// <summary>Flag that the entry has reading data. otherwise reading is surface form.</summary>
        public const int HAS_READING = 2;
    }
}
﻿using Lucene.Net.Codecs;
using Lucene.Net.Store;
using System;
using System.IO;

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
    /// Character category data.
    /// </summary>
    public sealed class CharacterDefinition
    {
        public const string FILENAME_SUFFIX = ".dat";
        public const string HEADER = "ko_cd";
        public const int VERSION = 1;

        public static readonly int CLASS_COUNT = Enum.GetValues(typeof(CharacterClass)).Length;

        // only used internally for lookup:
        internal enum CharacterClass : byte
        {
            NGRAM, DEFAULT, SPACE, SYMBOL, NUMERIC, ALPHA, CYRILLIC, GREEK, HIRAGANA, KATAKANA, KANJI, HANGUL, HANJA, HANJANUMERIC
        }

        private readonly byte[] characterCategoryMap = new byte[0x10000];

        private readonly bool[] invokeMap = new bool[CLASS_COUNT];
        private readonly bool[] groupMap = new bool[CLASS_COUNT];

        // the classes:
        public static readonly byte NGRAM = (byte)CharacterClass.NGRAM;
        public static readonly byte DEFAULT = (byte)CharacterClass.DEFAULT;
        public static readonly byte SPACE = (byte)CharacterClass.SPACE;
        public static readonly byte SYMBOL = (byte)CharacterClass.SYMBOL;
        public static readonly byte NUMERIC = (byte)CharacterClass.NUMERIC;
        public static readonly byte ALPHA = (byte)CharacterClass.ALPHA;
        public static readonly byte CYRILLIC = (byte)CharacterClass.CYRILLIC;
        public static readonly byte GREEK = (byte)CharacterClass.GREEK;
        public static readonly byte HIRAGANA = (byte)CharacterClass.HIRAGANA;
        public static readonly byte KATAKANA = (byte)CharacterClass.KATAKANA;
        public static readonly byte KANJI = (byte)CharacterClass.KANJI;
        public static readonly byte HANGUL = (byte)CharacterClass.HANGUL;
        public static readonly byte HANJA = (byte)CharacterClass.HANJA;
        public static readonly byte HANJANUMERIC = (byte)CharacterClass.HANJANUMERIC;

        private CharacterDefinition()
        {
            using (Stream @is = BinaryDictionary.GetTypeResource(GetType(), FILENAME_SUFFIX))
            {
                DataInput input = new InputStreamDataInput(@is);
                CodecUtil.CheckHeader(input, HEADER, VERSION, VERSION);
                @is.Read(characterCategoryMap, 0, characterCategoryMap.Length);
                for (int i = 0; i < CLASS_COUNT; i++)
                {
                    byte b = input.ReadByte();
                    invokeMap[i] = (b & 0x01) != 0;
                    groupMap[i] = (b & 0x02) != 0;
                }
            }
        }

        public byte GetCharacterClass(char c)
        {
            return characterCategoryMap[c];
        }

        public bool IsInvoke(char c)
        {
            return invokeMap[characterCategoryMap[c]];
        }

        public bool IsGroup(char c)
        {
            return groupMap[characterCategoryMap[c]];
        }

        public bool IsHanja(char c)
        {
            byte characterClass = GetCharacterClass(c);
            return characterClass == HANJA || characterClass == HANJANUMERIC;
        }

        public bool IsHangul(char c)
        {
            return GetCharacterClass(c) == HANGUL;
        }

        public bool HasCoda(char ch)
        {
            if (((ch - 0xAC00) % 0x001C) == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static byte LookupCharacterClass(string characterClassName)
        {
            return (byte)Enum.Parse(typeof(CharacterClass), characterClassName, true);
        }

        public static CharacterDefinition Instance => SingletonHolder.INSTANCE;

        private static class SingletonHolder
        {
            internal static readonly CharacterDefinition INSTANCE = LoadInstance(); // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)


            private static CharacterDefinition LoadInstance()
            {
                try
                {
                    return new CharacterDefinition();
                }
                catch (IOException ioe)
                {
                    throw new Exception("Cannot load CharacterDefinition.", ioe);
                }
            }
        }
    }
}

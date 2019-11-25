﻿using Lucene.Net.Analysis.Ko.Dict;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

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

    public class UnknownDictionaryBuilder
    {
        private const string NGRAM_DICTIONARY_ENTRY = "NGRAM,1798,3559,3677,SY,*,*,*,*,*,*,*";

        private string encoding = "utf-8";

        public UnknownDictionaryBuilder(string encoding)
        {
            this.encoding = encoding;
        }

        public virtual UnknownDictionaryWriter Build(string dirname)
        {
            //        UnknownDictionaryWriter unkDictionary = readDictionaryFile(dirname + File.separator + "unk.def");  //Should be only one file
            //readCharacterDefinition(dirname + File.separator + "char.def", unkDictionary);
            //return unkDictionary;
            UnknownDictionaryWriter unkDictionary = ReadDictionaryFile(dirname + System.IO.Path.DirectorySeparatorChar + "unk.def");  //Should be only one file
            ReadCharacterDefinition(dirname + System.IO.Path.DirectorySeparatorChar + "char.def", unkDictionary);
            return unkDictionary;
        }

        public virtual UnknownDictionaryWriter ReadDictionaryFile(string filename)
        {
            return ReadDictionaryFile(filename, encoding);
        }

        public virtual UnknownDictionaryWriter ReadDictionaryFile(string filename, string encoding)
        {
            UnknownDictionaryWriter dictionary = new UnknownDictionaryWriter(5 * 1024 * 1024);

            //FileInputStream inputStream = new FileInputStream(filename);
            //Charset cs = Charset.forName(encoding);
            //CharsetDecoder decoder = cs.newDecoder()
            //    .onMalformedInput(CodingErrorAction.REPORT)
            //    .onUnmappableCharacter(CodingErrorAction.REPORT);
            //InputStreamReader streamReader = new InputStreamReader(inputStream, decoder);
            //LineNumberReader lineReader = new LineNumberReader(streamReader);

            List<string[]> lines = new List<string[]>();
            Encoding decoder = Encoding.GetEncoding(encoding);
            using (Stream inputStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            using (TextReader reader = new StreamReader(inputStream, decoder))
            {
                dictionary.Put(CSVUtil.Parse(NGRAM_DICTIONARY_ENTRY));

                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    // note: unk.def only has 10 fields, it simplifies the writer to just append empty reading and pronunciation,
                    // even though the unknown dictionary returns hardcoded null here.
                    string[] parsed = CSVUtil.Parse(line + ",*,*"); // Probably we don't need to validate entry
                    lines.Add(parsed);
                }
            }

            lines.Sort(new ComparerAnonymousHelper());

            //Collections.sort(lines, new Comparator<string[]>() {
            //      public int compare(string[] left, string[] right)
            //{
            //    int leftId = CharacterDefinition.lookupCharacterClass(left[0]);
            //    int rightId = CharacterDefinition.lookupCharacterClass(right[0]);
            //    return leftId - rightId;
            //}
            //    });

            foreach (string[] entry in lines)
            {
                dictionary.Put(entry);
            }

            return dictionary;
        }

        private class ComparerAnonymousHelper : IComparer<string[]>
        {
            public int Compare(string[] left, string[] right)
            {
                int leftId = CharacterDefinition.LookupCharacterClass(left[0]);
                int rightId = CharacterDefinition.LookupCharacterClass(right[0]);
                return leftId - rightId;
            }
        }

        public void ReadCharacterDefinition(string filename, UnknownDictionaryWriter dictionary)
        {
            //    FileInputStream inputStream = new FileInputStream(filename);
            //InputStreamReader streamReader = new InputStreamReader(inputStream, encoding);
            //LineNumberReader lineReader = new LineNumberReader(streamReader);

            using (Stream inputStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            using (TextReader reader = new StreamReader(inputStream, Encoding.GetEncoding(encoding)))
            {
                string line = null;

                while ((line = reader.ReadLine()) != null)
                {
                    line = Regex.Replace(line, "^\\s", "");
                    line = Regex.Replace(line, "\\s*#.*", "");
                    line = Regex.Replace(line, "\\s+", " ");

                    // Skip empty line or comment line
                    if (line.Length == 0)
                    {
                        continue;
                    }

                    if (line.StartsWith("0x", StringComparison.Ordinal))
                    {  // Category mapping
                        string[] values = new Regex(" ").Split(line, 2);  // Split only first space

                        if (!values[0].Contains(".."))
                        {
                            int cp = Convert.ToInt32(values[0], 16);
                            dictionary.PutCharacterCategory(cp, values[1]);
                        }
                        else
                        {
                            string[] codePoints = Regex.Split(values[0], "\\.\\.").TrimEnd();
                            int cpFrom = Convert.ToInt32(codePoints[0], 16);
                            int cpTo = Convert.ToInt32(codePoints[1], 16);

                            for (int i = cpFrom; i <= cpTo; i++)
                            {
                                dictionary.PutCharacterCategory(i, values[1]);
                            }
                        }
                    }
                    else
                    {  // Invoke definition
                        string[] values = line.Split(' ').TrimEnd(); // Consecutive space is merged above
                        string characterClassName = values[0];
                        int invoke = int.Parse(values[1], CultureInfo.InvariantCulture);
                        int group = int.Parse(values[2], CultureInfo.InvariantCulture);
                        int length = int.Parse(values[3], CultureInfo.InvariantCulture);
                        dictionary.PutInvokeDefinition(characterClassName, invoke, group, length);
                    }
                }
            }
        }
    }
}

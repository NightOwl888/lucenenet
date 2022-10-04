// Lucene version compatibility level 8.2.0
using ICU4N.Text;
using J2N.Text;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Support.Util.Fst;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;
using Long = J2N.Numerics.Int64;

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

    public class TokenInfoDictionaryBuilder
    {
        /// <summary>Internal word id - incrementally assigned as entries are read and added. This will be byte offset of dictionary file.</summary>
        private int offset = 0;

        private readonly string encoding = "utf-8";

        private readonly bool normalizeEntries = false;
        private readonly Normalizer2 normalizer;

        public TokenInfoDictionaryBuilder(string encoding, bool normalizeEntries)
        {
            this.encoding = encoding;
            this.normalizeEntries = normalizeEntries;
            this.normalizer = normalizeEntries ? Normalizer2.GetInstance(null, "nfkc", Normalizer2Mode.Compose) : null;
        }

        public virtual TokenInfoDictionaryWriter Build(string dirname)
        {
            IList<string> csvFiles = new JCG.List<string>();
            foreach (FileInfo file in new DirectoryInfo(dirname).EnumerateFiles("*.csv"))
            {
                csvFiles.Add(file.FullName);
            }
            csvFiles.Sort();
            return BuildDictionary(csvFiles);
            //        FilenameFilter filter = (dir, name) -> name.EndsWith(".csv");
            //ArrayList<File> csvFiles = new ArrayList<>();
            //for (File file : new File(dirname).listFiles(filter)) {
            //  csvFiles.add(file);
            //}
            //Collections.sort(csvFiles);
            //return buildDictionary(csvFiles);
        }

        public virtual TokenInfoDictionaryWriter BuildDictionary(IList<string> csvFiles)
        {
            TokenInfoDictionaryWriter dictionary = new TokenInfoDictionaryWriter(10 * 1024 * 1024);

            // all lines in the file
            Console.Out.WriteLine("  parse...");
            IList<string[]> lines = new JCG.List<string[]>(400000);
            foreach (string file in csvFiles)
            {
                using (Stream inputStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    //Charset cs = Charset.forName(encoding);
                    //CharsetDecoder decoder = cs.newDecoder()
                    //    .onMalformedInput(CodingErrorAction.REPORT)
                    //    .onUnmappableCharacter(CodingErrorAction.REPORT);
                    //InputStreamReader streamReader = new InputStreamReader(inputStream, decoder);
                    //BufferedReader reader = new BufferedReader(streamReader);

                    Encoding decoder = Encoding.GetEncoding(encoding);
                    TextReader reader = new StreamReader(inputStream, decoder);

                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] entry = CSVUtil.Parse(line);

                        if (entry.Length < 12)
                        {
                            Console.Out.WriteLine("Entry in CSV is not valid: " + line);
                            continue;
                        }

                        // NFKC normalize dictionary entry
                        if (normalizeEntries)
                        {
                            string[] normalizedEntry = new string[entry.Length];
                            for (int i = 0; i < entry.Length; i++)
                            {
                                //normalizedEntry[i] = entry[i].Normalize(NormalizationForm.FormKC);
                                normalizer.Normalize(entry[i]);
                            }
                            lines.Add(normalizedEntry);
                        }
                        else
                        {
                            lines.Add(entry);
                        }
                    }
                }
            }

            Console.Out.WriteLine("  sort...");

            // sort by term: we sorted the files already and use a stable sort.
            //Collections.sort(lines, Comparator.comparing(left -> left[0]));
            lines.Sort(new ComparerAnonymousHelper());

            Console.Out.WriteLine("  encode...");

            PositiveInt32Outputs fstOutput = PositiveInt32Outputs.Singleton;
            Builder<Long> fstBuilder = new Builder<Long>(FST.INPUT_TYPE.BYTE2, 0, 0, true, true, int.MaxValue, fstOutput, true, 15);
            Int32sRef scratch = new Int32sRef();
            long ord = -1; // first ord will be 0
            string lastValue = null;

            // build tokeninfo dictionary
            foreach (string[] entry in lines)
            {
                string surfaceForm = entry[0].Trim();
                if (surfaceForm.Length == 0)
                {
                    continue;
                }
                int next = dictionary.Put(entry);

                if (next == offset)
                {
                    Console.Out.WriteLine("Failed to process line: " + Arrays.ToString(entry));
                    continue;
                }

                if (!surfaceForm.Equals(lastValue, StringComparison.Ordinal))
                {
                    // new word to add to fst
                    ord++;
                    lastValue = surfaceForm;
                    scratch.Grow(surfaceForm.Length);
                    scratch.Length = surfaceForm.Length;
                    for (int i = 0; i < surfaceForm.Length; i++)
                    {
                        scratch.Int32s[i] = (int)surfaceForm[i];
                    }
                    fstBuilder.Add(scratch, ord);
                }
                dictionary.AddMapping((int)ord, offset);
                offset = next;
            }

            FST<Long> fst = fstBuilder.Finish();

            Console.Out.Write(/*"  " + fst.NodeCount + " nodes, " + fst.ArcCount + " arcs, " +*/ fst.GetRamBytesUsed() + " bytes...  ");
            dictionary.SetFST(fst);
            Console.Out.WriteLine(" done");

            return dictionary;
        }

        private class ComparerAnonymousHelper : IComparer<string[]>
        {
            public int Compare(string[] left, string[] right)
            {
                return left[0].CompareToOrdinal(right[0]);
            }
        }
    }
}

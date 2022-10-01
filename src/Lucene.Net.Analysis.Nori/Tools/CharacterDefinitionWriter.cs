// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Ko.Dict;
using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Support;
using System.IO;
using Console = Lucene.Net.Util.SystemConsole;

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

    public sealed class CharacterDefinitionWriter
    {
        private readonly byte[] characterCategoryMap = new byte[0x10000];

        private readonly bool[] invokeMap = new bool[CharacterDefinition.CLASS_COUNT];
        private readonly bool[] groupMap = new bool[CharacterDefinition.CLASS_COUNT];

        /// <summary>
        /// Constructor for building. TODO: remove write access
        /// </summary>
        public CharacterDefinitionWriter()
        {
            Arrays.Fill(characterCategoryMap, CharacterDefinition.DEFAULT);
        }

        /// <summary>
        /// Put mapping from unicode code point to character class.
        /// </summary>
        /// <param name="codePoint">Code point.</param>
        /// <param name="characterClassName">Character class name.</param>
        public void PutCharacterCategory(int codePoint, string characterClassName)
        {
            characterClassName = characterClassName.Split(' ')[0]; // use first
                                                                   // category
                                                                   // class

            // Override Nakaguro
            if (codePoint == 0x30FB)
            {
                characterClassName = "SYMBOL";
            }
            characterCategoryMap[codePoint] = CharacterDefinition.LookupCharacterClass(characterClassName);
        }

        public void PutInvokeDefinition(string characterClassName, int invoke, int group, int length)
        {
            byte characterClass = CharacterDefinition.LookupCharacterClass(characterClassName);
            invokeMap[characterClass] = invoke == 1;
            groupMap[characterClass] = group == 1;
            // TODO: length def ignored
        }

        public void Write(string baseDir)
        {
            //      string filename = baseDir + File.separator +
            //CharacterDefinition.class.getName().replace('.', File.separatorChar) + CharacterDefinition.FILENAME_SUFFIX;

            string fileName = System.IO.Path.Combine(baseDir, typeof(CharacterDefinition).Name + CharacterDefinition.FILENAME_SUFFIX);
            //new File(filename).getParentFile().mkdirs();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(baseDir));
            using (Stream os = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                DataOutput output = new OutputStreamDataOutput(os);
                CodecUtil.WriteHeader(output, CharacterDefinition.HEADER, CharacterDefinition.VERSION);
                output.WriteBytes(characterCategoryMap, 0, characterCategoryMap.Length);
                for (int i = 0; i < CharacterDefinition.CLASS_COUNT; i++)
                {
                    byte b = (byte)(
                      (invokeMap[i] ? 0x01 : 0x00) |
                      (groupMap[i] ? 0x02 : 0x00)
                    );
                    output.WriteByte(b);
                }
            }
        }
    }
}

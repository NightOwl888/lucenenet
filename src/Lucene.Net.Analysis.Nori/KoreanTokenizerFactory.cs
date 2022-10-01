// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Ko.Dict;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Ko
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
    /// Factory for <see cref="KoreanTokenizer"/>.
    /// <code>
    /// &lt;fieldType name="text_ko" class="solr.TextField"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.KoreanTokenizerFactory"
    ///                decompoundMode="discard"
    ///                userDictionary="user.txt"
    ///                userDictionaryEncoding="UTF-8"
    ///                outputUnknownUnigrams="false"
    ///                discardPunctuation="true"
    ///     /&gt;
    ///  &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// <para/>
    /// Supports the following attributes:
    /// <list type="table">
    ///     <item>
    ///         <term>userDictionary</term>
    ///         <description>User dictionary path.</description>
    ///     </item>
    ///     <item>
    ///         <term>userDictionaryEncoding</term>
    ///         <description>User dictionary encoding.</description>
    ///     </item>
    ///     <item>
    ///         <term>decompoundMode</term>
    ///         <description>Decompound mode. Either 'none', 'discard', 'mixed'. Default is discard. See <see cref="DecompoundMode"/>.</description>
    ///     </item>
    ///     <item>
    ///         <term>outputUnknownUnigrams</term>
    ///         <description>If <c>true</c> outputs unigrams for unknown words.</description>
    ///     </item>
    ///     <item>
    ///         <term>discardPunctuation</term>
    ///         <description><c>true</c> if punctuation tokens should be dropped from the output.</description>
    ///     </item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <since>7.4.0</since>
    public class KoreanTokenizerFactory : TokenizerFactory, IResourceLoaderAware
    {
        private const string USER_DICT_PATH = "userDictionary";
        private const string USER_DICT_ENCODING = "userDictionaryEncoding";
        private const string DECOMPOUND_MODE = "decompoundMode";
        private const string OUTPUT_UNKNOWN_UNIGRAMS = "outputUnknownUnigrams";
        private const string DISCARD_PUNCTUATION = "discardPunctuation";

        private readonly string userDictionaryPath;
        private readonly string userDictionaryEncoding;
        private UserDictionary userDictionary;

        private readonly DecompoundMode mode;
        private readonly bool outputUnknownUnigrams;
        private readonly bool discardPunctuation;

        /// <summary>Creates a new KoreanTokenizerFactory</summary>
        public KoreanTokenizerFactory(IDictionary<string, string> args)
                  : base(args)
        {
            if (args.TryGetValue(USER_DICT_PATH, out userDictionaryPath))
                args.Remove(USER_DICT_PATH);
            if (args.TryGetValue(USER_DICT_ENCODING, out userDictionaryEncoding))
                args.Remove(USER_DICT_ENCODING);

            mode = (DecompoundMode)Enum.Parse(typeof(DecompoundMode), Get(args, DECOMPOUND_MODE, KoreanTokenizer.DEFAULT_DECOMPOUND.ToString()), true);
            outputUnknownUnigrams = GetBoolean(args, OUTPUT_UNKNOWN_UNIGRAMS, false);
            discardPunctuation = GetBoolean(args, DISCARD_PUNCTUATION, true);

            if (args.Count != 0)
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            if (userDictionaryPath != null)
            {
                using (Stream stream = loader.OpenResource(userDictionaryPath))
                {
                    string encoding = userDictionaryEncoding;
                    if (encoding == null)
                    {
                        encoding = Encoding.UTF8.WebName;
                    }
                    Encoding decoder = Encoding.GetEncoding(encoding);
                    using (TextReader reader = new StreamReader(stream, decoder))
                        userDictionary = UserDictionary.Open(reader);
                }
            }
            else
            {
                userDictionary = null;
            }
        }

        public override Tokenizer Create(AttributeSource.AttributeFactory factory, TextReader input)
        {
            return new KoreanTokenizer(factory, input, userDictionary, mode, outputUnknownUnigrams, discardPunctuation);
        }
    }
}

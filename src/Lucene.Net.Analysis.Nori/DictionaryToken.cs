// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Ko.Dict;

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
    /// A token stored in a <see cref="IDictionary"/>.
    /// </summary>
    public class DictionaryToken : Token
    {
        private readonly int wordId;
        private readonly KoreanTokenizer.Type type;
        private readonly IDictionary dictionary;

        public DictionaryToken(KoreanTokenizer.Type type, IDictionary dictionary, int wordId, char[] surfaceForm,
                                int offset, int length, int startOffset, int endOffset)
            : base(surfaceForm, offset, length, startOffset, endOffset)
        {
            this.type = type;
            this.dictionary = dictionary;
            this.wordId = wordId;
        }

        public override string ToString()
        {
            return $"DictionaryToken(\"{GetSurfaceFormString()}\" pos={StartOffset} length={Length}" +
                $" posLen={PositionLength} type={type} wordId={wordId}" +
                $" leftID={dictionary.GetLeftId(wordId)})";
        }

        /// <summary>
        /// Gets the type of this token.
        /// </summary>
        public virtual KoreanTokenizer.Type Type => type;

        /// <summary>
        /// Gets a value indicating whether this token is known word.
        /// <c>true</c> if this token is in standard dictionary; otherwise, <c>false</c>.
        /// </summary>
        public virtual bool IsKnown => type == KoreanTokenizer.Type.KNOWN;

        /// <summary>
        /// Gets a value indicating whether this token is unknown word.
        /// <c>true</c> if this token is unknown word; otherwise, <c>false</c>.
        /// </summary>
        public virtual bool IsUnknown => type == KoreanTokenizer.Type.UNKNOWN;

        /// <summary>
        /// Gets a value indicating whether this token is defined in user dictionary.
        /// <c>true</c> if this token is in user dictionary; otherwise, <c>false</c>.
        /// </summary>
        public virtual bool IsUser => type == KoreanTokenizer.Type.USER;

        public override POS.Type POSType => dictionary.GetPOSType(wordId);

        public override POS.Tag LeftPOS => dictionary.GetLeftPOS(wordId);

        public override POS.Tag RightPOS => dictionary.GetRightPOS(wordId);

        public override string Reading => dictionary.GetReading(wordId);

        public override Morpheme[] GetMorphemes()
        {
            return dictionary.GetMorphemes(wordId, GetSurfaceForm(), Offset, Length);
        }
    }
}

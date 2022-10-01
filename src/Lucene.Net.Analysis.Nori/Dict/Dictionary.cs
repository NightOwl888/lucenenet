// Lucene version compatibility level 8.2.0

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
    /// A morpheme extracted from a compound token.
    /// </summary>
    public class Morpheme
    {
        public readonly POS.Tag posTag;
        public readonly string surfaceForm;

        public Morpheme(POS.Tag posTag, string surfaceForm)
        {
            this.posTag = posTag;
            this.surfaceForm = surfaceForm;
        }
    }

    /// <summary>
    /// Dictionary interface for retrieving morphological data
    /// by id.
    /// </summary>
    public interface IDictionary
    {
        /// <summary>
        /// Get left id of specified word.
        /// </summary>
        int GetLeftId(int wordId);

        /// <summary>
        /// Get right id of specified word.
        /// </summary>
        int GetRightId(int wordId);

        /// <summary>
        /// Get word cost of specified word
        /// </summary>
        int GetWordCost(int wordId);

        /// <summary>
        /// Get the <see cref="POS.Type"/> of specified word (morpheme, compound, inflect or pre-analysis).
        /// </summary>
        POS.Type GetPOSType(int wordId);

        /// <summary>
        /// Get the left <see cref="POS.Tag"/> of specfied word.
        /// <para/>
        /// For <see cref="POS.Type.MORPHEME"/> and <see cref="POS.Type.COMPOUND"/> the left and right POS are the same.
        /// </summary>
        POS.Tag GetLeftPOS(int wordId);

        /// <summary>
        /// Get the right <see cref="POS.Tag"/> of specfied word.
        /// <para/>
        /// For <see cref="POS.Type.MORPHEME"/> and <see cref="POS.Type.COMPOUND"/> the left and right POS are the same.
        /// </summary>
        POS.Tag GetRightPOS(int wordId);

        /// <summary>
        /// Get the reading of specified word (mainly used for Hanja to Hangul conversion).
        /// </summary>
        string GetReading(int wordId);

        /// <summary>
        /// Get the morphemes of specified word (e.g. 가깝으나: 가깝 + 으나).
        /// </summary>
        Morpheme[] GetMorphemes(int wordId, char[] surfaceForm, int off, int len);
    }
}

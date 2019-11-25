﻿using Lucene.Net.Analysis.Ko.Dict;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Ko.TokenAttributes
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
    /// Part of Speech attributes for Korean.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public interface IPartOfSpeechAttribute : IAttribute
    {
        /// <summary>
        /// Get the <see cref="POS.Type"/> of the token.
        /// </summary>
        POS.Type? POSType { get; }

        /// <summary>
        /// Get the left part of speech of the token.
        /// </summary>
        POS.Tag? LeftPOS { get; }

        /// <summary>
        /// Get the right part of speech of the token.
        /// </summary>
        POS.Tag? RightPOS { get; }

        /// <summary>
        /// Get the <see cref="Morpheme"/> decomposition of the token.
        /// </summary>
        Morpheme[] GetMorphemes();

        /// <summary>
        /// Set the current token.
        /// </summary>
        void SetToken(Token token);
    }
}
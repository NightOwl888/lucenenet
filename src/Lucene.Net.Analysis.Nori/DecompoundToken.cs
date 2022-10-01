// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Ko.Dict;
using System;

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
    /// A token that was generated from a compound.
    /// </summary>
    public class DecompoundToken : Token
    {
        private readonly POS.Tag posTag;

        /// <summary>
        /// Creates a new <see cref="DecompoundToken"/>.
        /// </summary>
        /// <param name="posTag">The part of speech of the token.</param>
        /// <param name="surfaceForm">The surface form of the token.</param>
        /// <param name="startOffset">The start offset of the token in the analyzed text.</param>
        /// <param name="endOffset">The end offset of the token in the analyzed text.</param>
        public DecompoundToken(POS.Tag posTag, String surfaceForm, int startOffset, int endOffset)
            : base(surfaceForm.ToCharArray(), 0, surfaceForm.Length, startOffset, endOffset)
        {
            this.posTag = posTag;
        }

        public override string ToString()
        {
            return $"DecompoundToken(\"{GetSurfaceFormString()}\" pos={StartOffset} length={Length}" +
                $" startOffset={StartOffset} endOffset={EndOffset})";
        }

        public override POS.Type POSType => POS.Type.MORPHEME;

        public override POS.Tag LeftPOS => posTag;

        public override POS.Tag RightPOS => posTag;

        public override string Reading => null;

        public override Morpheme[] GetMorphemes()
        {
            return null;
        }
    }
}

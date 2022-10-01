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
    /// Analyzed token with morphological data.
    /// </summary>
    public abstract class Token
    {
        private readonly char[] surfaceForm;
        private readonly int offset;
        private readonly int length;

        private readonly int startOffset;
        private readonly int endOffset;

        public Token(char[] surfaceForm, int offset, int length, int startOffset, int endOffset)
        {
            this.surfaceForm = surfaceForm;
            this.offset = offset;
            this.length = length;

            this.startOffset = startOffset;
            this.endOffset = endOffset;
        }

        /// <summary>Gets surfaceForm.</summary>
        public virtual char[] GetSurfaceForm()
        {
            return surfaceForm;
        }

        /// <summary>Gets offset into surfaceForm.</summary>
        public virtual int Offset => offset;

        /// <summary>Gets length of surfaceForm.</summary>
        public virtual int Length => length;

        /// <summary>Gets surfaceForm as a <see cref="string"/>.</summary>
        public virtual string GetSurfaceFormString()
        {
            return new string(surfaceForm, offset, length);
        }

        /// <summary>
        /// Gets the <see cref="POS.Type"/> of the token.
        /// </summary>
        public abstract POS.Type POSType { get; }

        /// <summary>
        /// Gets the left part of speech of the token.
        /// </summary>
        public abstract POS.Tag LeftPOS { get; }

        /// <summary>
        /// Gets the right part of speech of the token.
        /// </summary>
        public abstract POS.Tag RightPOS { get; }

        /// <summary>
        /// Gets the reading of the token.
        /// </summary>
        public abstract string Reading { get; }

        /// <summary>
        /// Gets the <see cref="Morpheme"/> decomposition of the token.
        /// </summary>
        public abstract Morpheme[] GetMorphemes();

        /// <summary>
        /// Gets the start offset of the term in the analyzed text.
        /// </summary>
        public virtual int StartOffset => startOffset;

        /// <summary>
        /// Gets the end offset of the term in the analyzed text.
        /// </summary>
        public virtual int EndOffset => endOffset;

        public virtual int PositionIncrement { get; set; } = 1;

        public virtual int PositionLength { get; set; } = 1;
    }
}

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

        /**
         * @return surfaceForm
         */
        public virtual char[] GetSurfaceForm()
        {
            return surfaceForm;
        }

        /**
         * @return offset into surfaceForm
         */
        public virtual int Offset => offset;

        /**
         * @return length of surfaceForm
         */
        public virtual int Length => length;

        /**
         * @return surfaceForm as a String
         */
        public virtual string GetSurfaceFormString()
        {
            return new string(surfaceForm, offset, length);
        }

        /**
         * Get the {@link POS.Type} of the token.
         */
        public abstract POS.Type POSType { get; }

        /**
         * Get the left part of speech of the token.
         */
        public abstract POS.Tag LeftPOS { get; }

        /**
         * Get the right part of speech of the token.
         */
        public abstract POS.Tag RightPOS { get; }

        /**
         * Get the reading of the token.
         */
        public abstract string Reading { get; }

        /**
         * Get the {@link Morpheme} decomposition of the token.
         */
        public abstract Morpheme[] GetMorphemes();

        /**
         * Get the start offset of the term in the analyzed text.
         */
        public virtual int StartOffset => startOffset;

        /**
         * Get the end offset of the term in the analyzed text.
         */
        public virtual int EndOffset => endOffset;

        public virtual int PositionIncrement { get; set; } = 1;

        public virtual int PositionLength { get; set; } = 1;
    }
}

#if FEATURE_BREAKITERATOR
using ICU4N.Support.Text;
using Lucene.Net.Support;
﻿using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.Analysis.Util
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
    /// A <see cref="ICharacterEnumerator"/> used internally for use with <see cref="ICU4N.Text.BreakIterator"/>.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    // ICU4N specific - refactored from CharArrayIterator
    public abstract class CharArrayEnumerator : ICharacterEnumerator
    {
        /// <summary>
        /// A constant which indicates that there is no character at the current
        /// index.
        /// </summary>
        private const char DONE = '\uffff';

        private char[] array;
        private int start;
        private int index;
        private int length;
        private int limit;

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual char[] Text => array;

        public virtual int Start => start;

        public virtual int StartIndex => 0;

        public virtual int EndIndex => limit;

        public virtual int Length => length;

        public virtual int Index
        {
            get => index;
            set
            {
                if (value < StartIndex || value >= EndIndex)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Illegal Position: " + value);
                }
                index = start + value;
            }
        }

        public virtual char Current => (index == limit) ? DONE : array[index];

        object IEnumerator.Current => Current;

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public virtual bool MoveFirst()
        {
            index = start;
            return true;
        }

        public virtual bool MoveLast()
        {
            index = (limit == start) ? limit : limit - 1;
            return true;
        }

        public virtual bool MoveNext()
        {
            if (++index >= limit)
            {
                index = limit;
                return false;
            }
            else
            {
                return true;
            }
        }

        public virtual bool MovePrevious()
        {
            if (--index < start)
            {
                index = start;
                return false;
            }
            else
            {
                return true;
            }
        }

        public virtual void Reset()
        {
            index = start;
        }

        public virtual void Reset(char[] array, int start, int length)
        {
            this.array = array;
            this.start = start;
            this.index = start;
            this.length = length;
            this.limit = start + length;
        }

        public virtual bool TrySetIndex(int value)
        {
            if (value < StartIndex || value >= EndIndex)
            {
                return false;
            }
            index = start + value;
            return true;
        }
    }

    /// <summary>
    /// A CharacterIterator used internally for use with <see cref="ICU4N.Text.BreakIterator"/>
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public abstract class CharArrayIterator : CharacterIterator
    {
        private char[] array;
        private int start;
        private int index;
        private int length;
        private int limit;

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual char[] Text => array;

        public virtual int Start => start;

        public virtual int Length => length;

        /// <summary>
        /// Set a new region of text to be examined by this iterator
        /// </summary>
        /// <param name="array"> text buffer to examine </param>
        /// <param name="start"> offset into buffer </param>
        /// <param name="length"> maximum length to examine </param>
        public virtual void SetText(char[] array, int start, int length)
        {
            this.array = array;
            this.start = start;
            this.index = start;
            this.length = length;
            this.limit = start + length;
        }

        public override char Current => (index == limit) ? Done : array[index];

        protected abstract char JreBugWorkaround(char ch);
 

        public override char First()
        {
            index = start;
            return Current;
        }

        public override int BeginIndex => 0;

        public override int EndIndex => length;

        public override int Index => index - start;

        public override char Last()
        {
            index = (limit == start) ? limit : limit - 1;
            return Current;
        }

        public override char Next()
        {
            if (++index >= limit)
            {
                index = limit;
                return Done;
            }
            else
            {
                return Current;
            }
        }

        public override char Previous()
        {
            if (--index < start)
            {
                index = start;
                return Done;
            }
            else
            {
                return Current;
            }
        }

        public override char SetIndex(int position)
        {
            if (position < BeginIndex || position > EndIndex)
            {
                throw new ArgumentException("Illegal Position: " + position);
            }
            index = start + position;
            return Current;
        }

        public override object Clone()
        {
            return this.MemberwiseClone();
        }

        /// <summary>
        /// Create a new <see cref="CharArrayIterator"/> that works around JRE bugs
        /// in a manner suitable for <see cref="ICU4N.Text.BreakIterator.GetSentenceInstance()"/>.
        /// </summary>
        public static CharArrayIterator NewSentenceInstance()
        {
            return new CharArrayIteratorAnonymousInnerClassHelper2();
        }

        private class CharArrayIteratorAnonymousInnerClassHelper2 : CharArrayIterator
        {
            // no bugs
            protected override char JreBugWorkaround(char ch)
            {
                return ch;
            }
        }

        /// <summary>
        /// Create a new <see cref="CharArrayIterator"/> that works around JRE bugs
        /// in a manner suitable for <see cref="ICU4N.Text.BreakIterator.GetWordInstance()"/>.
        /// </summary>
        public static CharArrayIterator NewWordInstance()
        {
            return new CharArrayIteratorAnonymousInnerClassHelper4();
        }

        private class CharArrayIteratorAnonymousInnerClassHelper4 : CharArrayIterator
        {
            // no bugs
            protected override char JreBugWorkaround(char ch)
            {
                return ch;
            }
        }
    }
}
#endif
#if FEATURE_BREAKITERATOR
using J2N.Text;
using Lucene.Net.Support;
using Lucene.Net.Support.Text;
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
    /// Wraps a <see cref="T:char[]"/> as <see cref="ICharacterEnumerator"/> for processing with a <see cref="ICU4N.Text.BreakIterator"/>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class CharArrayEnumerator : ICharacterEnumerator
    {
        private char[] array;
        private int start;
        private int index;
        private int length;
        //private int limit;
        private readonly Func<char, char> bugWorkaround;

        public CharArrayEnumerator() : this(null) { }

        public CharArrayEnumerator(Func<char, char> bugWorkaround)
        {
            this.bugWorkaround = bugWorkaround ?? new Func<char, char>((c) => c); // Pass through by default
        }

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual char[] Text => array;

        public virtual int Start => start;

        public virtual int StartIndex => 0;

        public virtual int EndIndex => Math.Max(length - 1, 0);

        public virtual int Length => length;

        public virtual int Index
        {
            get => index - start;
            set
            {
                if (value < StartIndex || value > EndIndex)
                    throw new ArgumentOutOfRangeException(nameof(value));
                index = start + value;
            }
        }

        public virtual char Current => bugWorkaround(index >= StartIndex && index <= EndIndex && array.Length > 0 ? array[index] : unchecked((char)-1));

        object IEnumerator.Current => Current;

        public virtual bool MoveFirst()
        {
            index = start + StartIndex - 1;
            return true;
        }

        public virtual bool MoveLast()
        {
            index = start + EndIndex;
            return true;
        }

        public virtual bool MoveNext()
        {
            if (index >= start + EndIndex)
            {
                return false;
            }
            index++;
            return true;

            //if (++index >= EndIndex)
            //{
            //    index = EndIndex;
            //    return false;
            //}
            //else
            //{
            //    return true;
            //}
        }

        public virtual bool MovePrevious()
        {
            if (index <= start + StartIndex)
            {
                return false;
            }
            index--;
            return true;

            //if (--index < start)
            //{
            //    index = start;
            //    return false;
            //}
            //else
            //{
            //    return true;
            //}
        }

        void IEnumerator.Reset()
        {
            index = start;
        }

        /// <summary>
        /// Set a new region of text to be examined by this iterator
        /// </summary>
        /// <param name="array">text buffer to examine</param>
        /// <param name="start">offset into buffer</param>
        /// <param name="length"> maximum length to examine</param>
        public virtual void Reset(char[] array, int start, int length)
        {
            this.array = array;
            this.start = start;
            this.index = start - 1;
            this.length = length;
            //this.limit = start + length;
        }

        public virtual bool TrySetIndex(int value)
        {
            if (value < StartIndex)
            {
                index = start + StartIndex;
                return false;
            }
            else if (value > EndIndex)
            {
                index = start + EndIndex;
                return false;
            }
            index = start + value;
            return true;
        }

        public virtual object Clone()
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

        /// <summary>
        /// Create a new <see cref="CharArrayIterator"/> that works around JRE bugs
        /// in a manner suitable for <see cref="ICU4N.Text.BreakIterator.GetSentenceInstance()"/>.
        /// </summary>
        public static CharArrayEnumerator NewSentenceInstance()
        {
            return new CharArrayEnumerator(); // no bugs
        }

        /// <summary>
        /// Create a new <see cref="CharArrayIterator"/> that works around JRE bugs
        /// in a manner suitable for <see cref="ICU4N.Text.BreakIterator.GetWordInstance()"/>.
        /// </summary>
        public static CharArrayEnumerator NewWordInstance()
        {
            return new CharArrayEnumerator(); // no bugs
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
        public virtual char[] Text
        {
            get
            {
                return array;
            }
        }

        public virtual int Start
        {
            get
            {
                return start;
            }
        }

        public virtual int Length
        {
            get
            {
                return length;
            }
        }

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

        public override char Current
        {
            get
            {
                return (index == limit) ? Done : array[index];
            }
        }

        protected abstract char JreBugWorkaround(char ch);


        public override char First()
        {
            index = start;
            return Current;
        }

        public override int BeginIndex
        {
            get { return 0; }
        }

        public override int EndIndex
        {
            get { return length; }
        }

        public override int Index
        {
            get { return index - start; }
        }

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
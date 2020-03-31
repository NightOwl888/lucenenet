// Lucene version compatibility level 8.2.0
using ICU4N.Support.Text;
using ICU4N.Text;
using Lucene.Net.Analysis.OpenNlp.Tools;
using Lucene.Net.Analysis.Util;
using opennlp.tools.util;
using System;
using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Analysis.OpenNlp
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
    /// A <see cref="BreakIterator"/> that splits sentences using an OpenNLP sentence chunking model.
    /// </summary>
    public sealed class OpenNLPSentenceBreakIterator : BreakIterator
    {
        private CharacterIterator text;
        private int currentSentence;
        private int[] sentenceStarts;
        private NLPSentenceDetectorOp sentenceOp;

        public OpenNLPSentenceBreakIterator(NLPSentenceDetectorOp sentenceOp)
        {
            this.sentenceOp = sentenceOp;
        }

        public override int Current => text.Index;

        public override int First()
        {
            currentSentence = 0;
            text.SetIndex(text.BeginIndex);
            return Current;
        }

        public override int Last()
        {
            if (sentenceStarts.Length > 0)
            {
                currentSentence = sentenceStarts.Length - 1;
                text.SetIndex(text.EndIndex);
            }
            else
            { // there are no sentences; both the first and last positions are the begin index
                currentSentence = 0;
                text.SetIndex(text.BeginIndex);
            }
            return Current;
        }

        public override int Next()
        {
            if (text.Index == text.EndIndex || 0 == sentenceStarts.Length)
            {
                return Done;
            }
            else if (currentSentence < sentenceStarts.Length - 1)
            {
                text.SetIndex(sentenceStarts[++currentSentence]);
                return Current;
            }
            else
            {
                return Last();
            }
        }

        public override int Following(int pos)
        {
            if (pos < text.BeginIndex || pos > text.EndIndex)
            {
                throw new ArgumentException("offset out of bounds");
            }
            else if (0 == sentenceStarts.Length)
            {
                text.SetIndex(text.BeginIndex);
                return Done;
            }
            else if (pos >= sentenceStarts[sentenceStarts.Length - 1])
            {
                // this conflicts with the javadocs, but matches actual behavior (Oracle has a bug in something)
                // https://bugs.openjdk.java.net/browse/JDK-8015110
                text.SetIndex(text.EndIndex);
                currentSentence = sentenceStarts.Length - 1;
                return Done;
            }
            else
            { // there are at least two sentences
                currentSentence = (sentenceStarts.Length - 1) / 2; // start search from the middle
                MoveToSentenceAt(pos, 0, sentenceStarts.Length - 2);
                text.SetIndex(sentenceStarts[++currentSentence]);
                return Current;
            }
        }

        /// <summary>Binary search over sentences</summary>
        private void MoveToSentenceAt(int pos, int minSentence, int maxSentence)
        {
            if (minSentence != maxSentence)
            {
                if (pos < sentenceStarts[currentSentence])
                {
                    int newMaxSentence = currentSentence - 1;
                    currentSentence = minSentence + (currentSentence - minSentence) / 2;
                    MoveToSentenceAt(pos, minSentence, newMaxSentence);
                }
                else if (pos >= sentenceStarts[currentSentence + 1])
                {
                    int newMinSentence = currentSentence + 1;
                    currentSentence = maxSentence - (maxSentence - currentSentence) / 2;
                    MoveToSentenceAt(pos, newMinSentence, maxSentence);
                }
            }
            else
            {
                Debug.Assert(currentSentence == minSentence);
                Debug.Assert(pos >= sentenceStarts[currentSentence]);
                Debug.Assert((currentSentence == sentenceStarts.Length - 1 && pos <= text.EndIndex)
                    || pos < sentenceStarts[currentSentence + 1]);
            }
            // we have arrived - nothing to do
        }

        public override int Previous()
        {
            if (text.Index == text.BeginIndex)
            {
                return Done;
            }
            else
            {
                if (0 == sentenceStarts.Length)
                {
                    text.SetIndex(text.BeginIndex);
                    return Done;
                }
                if (text.Index == text.EndIndex)
                {
                    text.SetIndex(sentenceStarts[currentSentence]);
                }
                else
                {
                    text.SetIndex(sentenceStarts[--currentSentence]);
                }
                return Current;
            }
        }

        public override int Preceding(int pos)
        {
            if (pos < text.BeginIndex || pos > text.EndIndex)
            {
                throw new ArgumentException("offset out of bounds");
            }
            else if (0 == sentenceStarts.Length)
            {
                text.SetIndex(text.BeginIndex);
                currentSentence = 0;
                return Done;
            }
            else if (pos < sentenceStarts[0])
            {
                // this conflicts with the javadocs, but matches actual behavior (Oracle has a bug in something)
                // https://bugs.openjdk.java.net/browse/JDK-8015110
                text.SetIndex(text.BeginIndex);
                currentSentence = 0;
                return Done;
            }
            else
            {
                currentSentence = sentenceStarts.Length / 2; // start search from the middle
                MoveToSentenceAt(pos, 0, sentenceStarts.Length - 1);
                if (0 == currentSentence)
                {
                    text.SetIndex(text.BeginIndex);
                    return Done;
                }
                else
                {
                    text.SetIndex(sentenceStarts[--currentSentence]);
                    return Current;
                }
            }
        }

        public override int Next(int n)
        {
            currentSentence += n;
            if (n < 0)
            {
                if (text.Index == text.EndIndex)
                {
                    ++currentSentence;
                }
                if (currentSentence < 0)
                {
                    currentSentence = 0;
                    text.SetIndex(text.BeginIndex);
                    return Done;
                }
                else
                {
                    text.SetIndex(sentenceStarts[currentSentence]);
                }
            }
            else if (n > 0)
            {
                if (currentSentence >= sentenceStarts.Length)
                {
                    currentSentence = sentenceStarts.Length - 1;
                    text.SetIndex(text.EndIndex);
                    return Done;
                }
                else
                {
                    text.SetIndex(sentenceStarts[currentSentence]);
                }
            }
            return Current;
        }

        //public override CharacterIterator Text => text;

        public override ICharacterEnumerator Text => throw new NotImplementedException(); // LUCENENET TODO: Finish implementation

        public override void SetText(CharacterIterator newText)
        {
            text = newText;
            text.SetIndex(text.BeginIndex);
            currentSentence = 0;
            Span[] spans = sentenceOp.SplitSentences(CharacterIteratorToString());
            sentenceStarts = new int[spans.Length];
            for (int i = 0; i < spans.Length; ++i)
            {
                // Adjust start positions to match those of the passed-in CharacterIterator
                sentenceStarts[i] = spans[i].getStart() + text.BeginIndex;
            }
        }

        public override void SetText(ICharacterEnumerator newText)
        {
            SetText(new CharacterEnumeratorWrapper(newText));
        }

        private string CharacterIteratorToString()
        {
            string fullText;
            if (text is CharArrayIterator)
            {
                CharArrayIterator charArrayIterator = (CharArrayIterator)text;
                fullText = new string(charArrayIterator.Text, charArrayIterator.Start, charArrayIterator.Length);
            }
            else
            {
                // TODO: is there a better way to extract full text from arbitrary CharacterIterators?
                StringBuilder builder = new StringBuilder();
                for (char ch = text.First(); ch != CharacterIterator.Done; ch = text.Next())
                {
                    builder.Append(ch);
                }
                fullText = builder.ToString();
                text.SetIndex(text.BeginIndex);
            }
            return fullText;
        }
    }

    /// <summary>
    /// This class is a wrapper around <see cref="ICharacterEnumerator"/> and implements the
    /// <see cref="CharacterIterator"/> protocol.
    /// </summary>
    internal class CharacterEnumeratorWrapper : CharacterIterator
    {
        private ICharacterEnumerator enumerator;
        // ICU4N: Since our ICharacterEnumerator's EndIndex is the end of the string
        // and in CharacterIterator it is one past the end of the string, we keep track of whether
        // we are past the end of the string with this boolean flag.
        private bool pastEnd = false;

        internal ICharacterEnumerator Enumerator => enumerator;

        public CharacterEnumeratorWrapper(ICharacterEnumerator enumerator)
        {
            this.enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
        }

        /// <inheritdoc/>
        public override char Current
        {
            get
            {
                if (pastEnd)
                    return Done;
                return enumerator.Current;
            }
        }

        /// <inheritdoc/>
        public override int BeginIndex => enumerator.StartIndex;

        /// <inheritdoc/>
        public override int EndIndex => enumerator.EndIndex + 1;

        /// <inheritdoc/>
        public override int Index => enumerator.Index + (pastEnd ? 1 : 0);

        /// <inheritdoc/>
        public override char First()
        {
            pastEnd = false;
            if (enumerator.MoveFirst())
            {
                return enumerator.Current;
            }
            return CharacterIterator.Done;
        }

        /// <inheritdoc/>
        public override char Last()
        {
            pastEnd = false;
            if (enumerator.MoveLast())
            {
                return enumerator.Current;
            }
            return CharacterIterator.Done;
        }

        /// <inheritdoc/>
        public override char Next()
        {
            pastEnd = !enumerator.MoveNext();
            if (pastEnd)
                return Done;

            return enumerator.Current;
        }

        /// <inheritdoc/>
        public override char Previous()
        {
            if (pastEnd)
                pastEnd = false;
            else if (!enumerator.MovePrevious())
                return Done;
            return enumerator.Current;
        }

        /// <inheritdoc/>
        public override char SetIndex(int location)
        {
            if (location < BeginIndex || location > EndIndex + 1)
                throw new ArgumentException("Invalid index");

            pastEnd = !enumerator.TrySetIndex(location);
            if (pastEnd)
                return Done;

            return enumerator.Current;
        }

        /// <inheritdoc/>
        public override object Clone()
        {
            var result = (CharacterEnumeratorWrapper)MemberwiseClone();
            result.enumerator = (ICharacterEnumerator)enumerator.Clone();
            return result;
        }

        /// <summary>
        /// Compares the specified object with this <see cref="CharacterEnumeratorWrapper"/>
        /// and indicates if they are equal. In order to be equal, <paramref name="obj"/>
        /// must be an instance of <see cref="CharacterEnumeratorWrapper"/> that iterates over
        /// the same sequence of characters with the same index.
        /// </summary>
        /// <param name="obj">The object to compare with this object.</param>
        /// <returns><c>true</c> if the specified object is equal to this <see cref="CharacterEnumeratorWrapper"/>; <c>false</c> otherwise.</returns>
        /// <seealso cref="GetHashCode()"/>
        public override bool Equals(object obj)
        {
            if (!(obj is CharacterEnumeratorWrapper other))
            {
                return false;
            }
            return pastEnd == other.pastEnd && this.enumerator.Equals(other.Enumerator);
        }

        /// <summary>
        /// Gets the hash code for this <see cref="StringCharacterEnumerator"/>.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return pastEnd.GetHashCode() + this.enumerator.GetHashCode();
        }
    }
}

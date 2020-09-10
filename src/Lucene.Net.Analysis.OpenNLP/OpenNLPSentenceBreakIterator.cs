// Lucene version compatibility level 8.2.0
using ICU4N.Support.Text;
using ICU4N.Text;
using Lucene.Net.Analysis.OpenNlp.Tools;
using Lucene.Net.Analysis.Util;
using Lucene.Net.ICU.Support.Text;
using Lucene.Net.Support.Text;
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
        private ICharacterEnumerator text;
        private int currentSentence;
        private int[] sentenceStarts;
        private NLPSentenceDetectorOp sentenceOp;

        public OpenNLPSentenceBreakIterator(NLPSentenceDetectorOp sentenceOp)
        {
            this.sentenceOp = sentenceOp;
        }

        public override int Current => text.Index;

        private int ConvertedEndIndex => text.EndIndex + (text.Length > 0 ? 1 : 0);

        public override int First()
        {
            currentSentence = 0;
            text.Index = text.StartIndex;
            return Current;
        }

        public override int Last()
        {
            if (sentenceStarts.Length > 0)
            {
                currentSentence = sentenceStarts.Length - 1;
                //text.Index = ConvertedEndIndex;
                //if (!text.TrySetIndex(ConvertedEndIndex))
                //return Done;
                var endIndex = ConvertedEndIndex;
                text.TrySetIndex(endIndex);
                return endIndex;
            }
            else
            { // there are no sentences; both the first and last positions are the begin index
                currentSentence = 0;
                text.Index = text.StartIndex;
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
                text.Index = sentenceStarts[++currentSentence];
                return Current;
            }
            else
            {
                return Last();
            }
        }

        public override int Following(int pos)
        {
            if (pos < text.StartIndex || pos > ConvertedEndIndex)
            {
                throw new ArgumentException("offset out of bounds");
            }
            else if (0 == sentenceStarts.Length)
            {
                text.Index = text.StartIndex;
                return Done;
            }
            else if (pos >= sentenceStarts[sentenceStarts.Length - 1])
            {
                // this conflicts with the javadocs, but matches actual behavior (Oracle has a bug in something)
                // https://bugs.openjdk.java.net/browse/JDK-8015110
                text.Index = ConvertedEndIndex;
                currentSentence = sentenceStarts.Length - 1;
                return Done;
            }
            else
            { // there are at least two sentences
                currentSentence = (sentenceStarts.Length - 1) / 2; // start search from the middle
                MoveToSentenceAt(pos, 0, sentenceStarts.Length - 2);
                text.Index = sentenceStarts[++currentSentence];
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
                Debug.Assert((currentSentence == sentenceStarts.Length - 1 && pos <= ConvertedEndIndex)
                    || pos < sentenceStarts[currentSentence + 1]);
            }
            // we have arrived - nothing to do
        }

        public override int Previous()
        {
            if (text.Index == text.StartIndex)
            {
                return Done;
            }
            else
            {
                if (0 == sentenceStarts.Length)
                {
                    text.Index = text.StartIndex;
                    return Done;
                }
                if (text.Index == ConvertedEndIndex)
                {
                    text.Index = sentenceStarts[currentSentence];
                }
                else
                {
                    text.Index = sentenceStarts[--currentSentence];
                }
                return Current;
            }
        }

        public override int Preceding(int pos)
        {
            if (pos < text.StartIndex || pos > ConvertedEndIndex)
            {
                throw new ArgumentException("offset out of bounds");
            }
            else if (0 == sentenceStarts.Length)
            {
                text.Index = text.StartIndex;
                currentSentence = 0;
                return Done;
            }
            else if (pos < sentenceStarts[0])
            {
                // this conflicts with the javadocs, but matches actual behavior (Oracle has a bug in something)
                // https://bugs.openjdk.java.net/browse/JDK-8015110
                text.Index = text.StartIndex;
                currentSentence = 0;
                return Done;
            }
            else
            {
                currentSentence = sentenceStarts.Length / 2; // start search from the middle
                MoveToSentenceAt(pos, 0, sentenceStarts.Length - 1);
                if (0 == currentSentence)
                {
                    text.Index = text.StartIndex;
                    return Done;
                }
                else
                {
                    text.Index = sentenceStarts[--currentSentence];
                    return Current;
                }
            }
        }

        public override int Next(int n)
        {
            currentSentence += n;
            if (n < 0)
            {
                if (text.Index == ConvertedEndIndex)
                {
                    ++currentSentence;
                }
                if (currentSentence < 0)
                {
                    currentSentence = 0;
                    text.Index = text.StartIndex;
                    return Done;
                }
                else
                {
                    text.Index = sentenceStarts[currentSentence];
                }
            }
            else if (n > 0)
            {
                if (currentSentence >= sentenceStarts.Length)
                {
                    currentSentence = sentenceStarts.Length - 1;
                    //text.Index = ConvertedEndIndex;
                    text.TrySetIndex(ConvertedEndIndex);
                    return Done;
                }
                else
                {
                    text.Index = sentenceStarts[currentSentence];
                }
            }
            return Current;
        }

        //public override CharacterIterator Text => text;

        public override ICharacterEnumerator Text => text;
        //{
        //    get
        //    {
        //        if (text is CharacterEnumeratorWrapper wrapper)
        //            return wrapper.Enumerator;
        //        return null;
        //    }
        //}

        public override void SetText(ICharacterEnumerator newText)
        {
            text = newText;
            text.Index = text.StartIndex;
            currentSentence = 0;
            Span[] spans = sentenceOp.SplitSentences(CharacterEnumeratorToString());
            sentenceStarts = new int[spans.Length];
            for (int i = 0; i < spans.Length; ++i)
            {
                // Adjust start positions to match those of the passed-in CharacterIterator
                sentenceStarts[i] = spans[i].getStart() + text.StartIndex;
            }
        }

        private string CharacterEnumeratorToString()
        {
            string fullText;
            if (text is CharArrayEnumerator)
            {
#pragma warning disable IDE0020 // Use pattern matching
                CharArrayEnumerator charArrayIterator = (CharArrayEnumerator)text;
#pragma warning restore IDE0020 // Use pattern matching
                fullText = new string(charArrayIterator.Text, charArrayIterator.Start, charArrayIterator.Length);
            }
            else
            {
                // TODO: is there a better way to extract full text from arbitrary CharacterIterators?
                StringBuilder builder = new StringBuilder();
                for (bool hasNext = text.MoveFirst(); hasNext; hasNext = text.MoveNext())
                {
                    builder.Append(text.Current);
                }
                fullText = builder.ToString();
                text.Index = text.StartIndex;
            }
            return fullText;
        }

        public override void SetText(CharacterIterator newText)
        {
            text = new CharacterIteratorWrapper(newText);
            text.Index = text.StartIndex;
            currentSentence = 0;
            Span[] spans = sentenceOp.SplitSentences(CharacterIteratorToString());
            sentenceStarts = new int[spans.Length];
            for (int i = 0; i < spans.Length; ++i)
            {
                // Adjust start positions to match those of the passed-in CharacterIterator
                sentenceStarts[i] = spans[i].getStart() + text.StartIndex;
            }
        }

        private string CharacterIteratorToString()
        {
            string fullText;
            if (text is CharacterIteratorWrapper wrapper && wrapper.CharacterIterator is CharArrayIterator)
            {
                CharArrayIterator charArrayIterator = (CharArrayIterator)wrapper.CharacterIterator;
                fullText = new string(charArrayIterator.Text, charArrayIterator.Start, charArrayIterator.Length);
            }
            else
            {
                // TODO: is there a better way to extract full text from arbitrary CharacterIterators?
                StringBuilder builder = new StringBuilder();
                for (bool hasNext = text.MoveFirst(); hasNext; hasNext = text.MoveNext())
                {
                    builder.Append(text.Current);
                }
                fullText = builder.ToString();
                text.Index = text.StartIndex;
            }
            return fullText;
        }
    }
}

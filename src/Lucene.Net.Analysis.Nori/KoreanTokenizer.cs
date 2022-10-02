// Lucene version compatibility level 8.2.0
using ICU4N.Globalization;
using J2N;
using Lucene.Net.Analysis.Ko.Dict;
using Lucene.Net.Analysis.Ko.TokenAttributes;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Support.Util.Fst;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using JCG = J2N.Collections.Generic;
using Long = J2N.Numerics.Int64;

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
    /// Tokenizer for Korean that uses morphological analysis.
    /// <para/>
    /// This tokenizer sets a number of additional attributes:
    /// <list type="bullet">
    ///     <item><description><see cref="IPartOfSpeechAttribute"/> containing part-of-speech.</description></item>
    ///     <item><description><see cref="IReadingAttribute"/> containing reading.</description></item>
    /// </list>
    /// <para/>
    /// This tokenizer uses a rolling Viterbi search to find the
    /// least cost segmentation (path) of the incoming characters.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class KoreanTokenizer : Tokenizer
    {
        /// <summary>
        /// Token type reflecting the original source of this token.
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// Known words from the system dictionary.
            /// </summary>
            KNOWN,
            /// <summary>
            /// Unknown words (heuristically segmented).
            /// </summary>
            UNKNOWN,
            /// <summary>
            /// Known words from the user dictionary.
            /// </summary>
            USER
        }

        // LUCENENET specific - de-nested DecompoundMode

        /// <summary>
        /// Default mode for the decompound of tokens <see cref="DecompoundMode.DISCARD"/>.
        /// </summary>
        public const DecompoundMode DEFAULT_DECOMPOUND = DecompoundMode.DISCARD;

        private static readonly bool VERBOSE = false;

        // For safety:
        private const int MAX_UNKNOWN_WORD_LENGTH = 1024;
        private const int MAX_BACKTRACE_GAP = 1024;

        private readonly IDictionary<Type, IDictionary> dictionaryMap = new Dictionary<Type, IDictionary>();

        private readonly TokenInfoFST fst;
        private readonly TokenInfoDictionary dictionary;
        private readonly UnknownDictionary unkDictionary;
        private readonly ConnectionCosts costs;
        private readonly UserDictionary userDictionary;
        private readonly CharacterDefinition characterDefinition;

        private readonly FST.Arc<Long> arc = new FST.Arc<Long>();
        private readonly FST.BytesReader fstReader;
        private readonly Int32sRef wordIdRef = new Int32sRef();

        private readonly FST.BytesReader userFSTReader;
        private readonly TokenInfoFST userFST;

        private readonly bool discardPunctuation;
        private readonly DecompoundMode mode;
        private readonly bool outputUnknownUnigrams;

        private readonly RollingCharBuffer buffer = new RollingCharBuffer();

        private readonly WrappedPositionArray positions = new WrappedPositionArray();

        // True once we've hit the EOF from the input reader:
        private bool end;

        // Last absolute position we backtraced from:
        private int lastBackTracePos;

        // Next absolute position to process:
        private int pos;

        // Already parsed, but not yet passed to caller, tokens:
        private readonly IList<Token> pending = new JCG.List<Token>();

        private readonly ICharTermAttribute termAtt;
        private readonly IOffsetAttribute offsetAtt;
        private readonly IPositionIncrementAttribute posIncAtt;
        private readonly IPositionLengthAttribute posLengthAtt;
        private readonly IPartOfSpeechAttribute posAtt;
        private readonly IReadingAttribute readingAtt;

        /// <summary>
        /// Creates a new <see cref="KoreanTokenizer"/> with default parameters.
        /// <para/>
        /// Uses the default <see cref="Lucene.Net.Util.AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY"/>.
        /// </summary>
        /// <param name="input"></param>
        public KoreanTokenizer(TextReader input)
            : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input, null, DEFAULT_DECOMPOUND, false, true)
        {
        }

        /// <summary>
        /// Create a new <see cref="KoreanTokenizer"/>.
        /// </summary>
        /// <param name="factory">The <see cref="AttributeSource.AttributeFactory"/> to use.</param>
        /// <param name="input"></param>
        /// <param name="userDictionary">Optional: if non-<c>null</c>, user dictionary.</param>
        /// <param name="mode">Decompound mode.</param>
        /// <param name="outputUnknownUnigrams">If <c>true</c> outputs unigrams for unknown words.</param>
        public KoreanTokenizer(AttributeFactory factory, TextReader input, UserDictionary userDictionary, DecompoundMode mode, bool outputUnknownUnigrams)
            : this(factory, input, userDictionary, mode, outputUnknownUnigrams, true)
        {
        }

        /// <summary>
        /// Create a new <see cref="KoreanTokenizer"/>.
        /// </summary>
        /// <param name="factory">The <see cref="AttributeSource.AttributeFactory"/> to use.</param>
        /// <param name="input"></param>
        /// <param name="userDictionary">Optional: if non-<c>null</c>, user dictionary.</param>
        /// <param name="mode">Decompound mode.</param>
        /// <param name="outputUnknownUnigrams">If <c>true</c> outputs unigrams for unknown words.</param>
        /// <param name="discardPunctuation"><c>true</c> if punctuation tokens should be dropped from the output.</param>
        public KoreanTokenizer(AttributeFactory factory, TextReader input, UserDictionary userDictionary, DecompoundMode mode, bool outputUnknownUnigrams, bool discardPunctuation)
            : base(factory, input)
        {
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.offsetAtt = AddAttribute<IOffsetAttribute>();
            this.posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            this.posLengthAtt = AddAttribute<IPositionLengthAttribute>();
            this.posAtt = AddAttribute<IPartOfSpeechAttribute>();
            this.readingAtt = AddAttribute<IReadingAttribute>();

            this.mode = mode;
            this.discardPunctuation = discardPunctuation;
            this.outputUnknownUnigrams = outputUnknownUnigrams;
            dictionary = TokenInfoDictionary.Instance;
            fst = dictionary.FST;
            unkDictionary = UnknownDictionary.Instance;
            characterDefinition = unkDictionary.CharacterDefinition;
            this.userDictionary = userDictionary;
            costs = ConnectionCosts.Instance;
            fstReader = fst.GetBytesReader();
            if (userDictionary != null)
            {
                userFST = userDictionary.FST;
                userFSTReader = userFST.GetBytesReader();
            }
            else
            {
                userFST = null;
                userFSTReader = null;
            }

            buffer.Reset(this.m_input);

            ResetState();

            dictionaryMap[Type.KNOWN] = dictionary;
            dictionaryMap[Type.UNKNOWN] = unkDictionary;
            dictionaryMap[Type.USER] = userDictionary;
        }

        private GraphvizFormatter dotOut;

        /// <summary>
        /// Expert: set this to produce graphviz (dot) output of
        /// the Viterbi lattice.
        /// </summary>
        public void SetGraphvizFormatter(GraphvizFormatter dotOut)
        {
            this.dotOut = dotOut;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                buffer.Reset(m_input);
            }
        }

        public override void Reset()
        {
            base.Reset();
            buffer.Reset(m_input);
            ResetState();
        }

        private void ResetState()
        {
            positions.Reset();
            pos = 0;
            end = false;
            lastBackTracePos = 0;
            pending.Clear();

            // Add BOS:
            positions.Get(0).Add(0, 0, -1, -1, -1, -1, Type.KNOWN);
        }

        public override void End()
        {
            base.End();
            // Set final offset
            int finalOffset = CorrectOffset(pos);
            offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        // LUCENENET specific - de-nested Position

        /// <summary>
        /// Returns the space penalty associated with the provided <see cref="POS.Tag"/>.
        /// </summary>
        /// <param name="leftPOS">The left part of speech of the current token.</param>
        /// <param name="numSpaces">The number of spaces before the current token.</param>
        private int ComputeSpacePenalty(POS.Tag leftPOS, int numSpaces)
        {
            int spacePenalty = 0;
            if (numSpaces > 0)
            {
                // TODO we should extract the penalty (left-space-penalty-factor) from the dicrc file.
                switch (leftPOS)
                {
                    case POS.Tag.E:
                    case POS.Tag.J:
                    case POS.Tag.VCP:
                    case POS.Tag.XSA:
                    case POS.Tag.XSN:
                    case POS.Tag.XSV:
                        spacePenalty = 3000;
                        break;

                    default:
                        break;
                }
            }
            return spacePenalty;

        }

        private void Add(IDictionary dict, Position fromPosData, int wordPos, int endPos, int wordID, Type type)
        {
            POS.Tag leftPOS = dict.GetLeftPOS(wordID);
            int wordCost = dict.GetWordCost(wordID);
            int leftID = dict.GetLeftId(wordID);
            int leastCost = int.MaxValue;
            int leastIDX = -1;
            if (Debugging.AssertsEnabled) Debugging.Assert(fromPosData.count > 0);
            for (int idx = 0; idx < fromPosData.count; idx++)
            {
                // The number of spaces before the term
                int numSpaces = wordPos - fromPosData.pos;

                // Cost is path cost so far, plus word cost (added at
                // end of loop), plus bigram cost and space penalty cost.
                int cost = fromPosData.costs[idx] + costs.Get(fromPosData.lastRightID[idx], leftID) + ComputeSpacePenalty(leftPOS, numSpaces);
                if (VERBOSE)
                {
                    Console.Out.WriteLine("      fromIDX=" + idx + ": cost=" + cost + " (prevCost=" + fromPosData.costs[idx] + " wordCost=" + wordCost + " bgCost=" + costs.Get(fromPosData.lastRightID[idx], leftID) +
                        " spacePenalty=" + ComputeSpacePenalty(leftPOS, numSpaces) + ") leftID=" + leftID + " leftPOS=" + leftPOS + ")");
                }
                if (cost < leastCost)
                {
                    leastCost = cost;
                    leastIDX = idx;
                    if (VERBOSE)
                    {
                        Console.Out.WriteLine("        **");
                    }
                }
            }

            leastCost += wordCost;

            if (VERBOSE)
            {
                Console.Out.WriteLine("      + cost=" + leastCost + " wordID=" + wordID + " leftID=" + leftID + " leastIDX=" + leastIDX + " toPos=" + endPos + " toPos.idx=" + positions.Get(endPos).count);
            }

            positions.Get(endPos).Add(leastCost, dict.GetRightId(wordID), fromPosData.pos, wordPos, leastIDX, wordID, type);
        }

        public override bool IncrementToken()
        {

            // parse() is able to return w/o producing any new
            // tokens, when the tokens it had produced were entirely
            // punctuation.  So we loop here until we get a real
            // token or we end:
            while (pending.Count == 0)
            {
                if (end)
                {
                    return false;
                }

                // Push Viterbi forward some more:
                Parse();
            }

            //Token token = pending.RemoveAt(pending.Count - 1);
            Token token = pending[pending.Count - 1];
            pending.Remove(token);

            int length = token.Length;
            ClearAttributes();
            if (Debugging.AssertsEnabled) Debugging.Assert(length > 0);
            //System.out.println("off=" + token.getOffset() + " len=" + length + " vs " + token.getSurfaceForm().length);
            termAtt.CopyBuffer(token.GetSurfaceForm(), token.Offset, length);
            offsetAtt.SetOffset(CorrectOffset(token.StartOffset), CorrectOffset(token.EndOffset));
            posAtt.SetToken(token);
            readingAtt.SetToken(token);
            posIncAtt.PositionIncrement = token.PositionIncrement;
            posLengthAtt.PositionLength = token.PositionLength;
            if (VERBOSE)
            {
                Console.Out.WriteLine(Thread.CurrentThread.Name + ":    incToken: return token=" + token);
            }
            return true;
        }

        // LUCENENET specific - de-nested WrappedPositionArray

        /// <summary>
        /// Incrementally parse some more characters.  This runs
        /// the viterbi search forwards "enough" so that we
        /// generate some more tokens.  How much forward depends on
        /// the chars coming in, since some chars could cause
        /// longer-lasting ambiguity in the parsing.  Once the
        /// ambiguity is resolved, then we back trace, produce
        /// the pending tokens, and return.
        /// </summary>
        private void Parse()
        {
            if (VERBOSE)
            {
                Console.Out.WriteLine("\nPARSE");
            }

            // Index of the last character of unknown word:
            int unknownWordEndIndex = -1;

            // Maximum posAhead of user word in the entire input
            int userWordMaxPosAhead = -1;

            // Advances over each position (character):
            while (true)
            {

                if (buffer.Get(pos) == -1)
                {
                    // End
                    break;
                }

                Position posData = positions.Get(pos);
                bool isFrontier = positions.NextPos == pos + 1;

                if (posData.count == 0)
                {
                    // No arcs arrive here; move to next position:
                    if (VERBOSE)
                    {
                        Console.Out.WriteLine("    no arcs in; skip pos=" + pos);
                    }
                    pos++;
                    continue;
                }

                if (pos > lastBackTracePos && posData.count == 1 && isFrontier)
                {
                    // We are at a "frontier", and only one node is
                    // alive, so whatever the eventual best path is must
                    // come through this node.  So we can safely commit
                    // to the prefix of the best path at this point:
                    Backtrace(posData, 0);

                    // Re-base cost so we don't risk int overflow:
                    posData.costs[0] = 0;
                    if (pending.Count > 0)
                    {
                        return;
                    }
                    else
                    {
                        // This means the backtrace only produced
                        // punctuation tokens, so we must keep parsing.
                    }
                }

                if (pos - lastBackTracePos >= MAX_BACKTRACE_GAP)
                {
                    // Safety: if we've buffered too much, force a
                    // backtrace now.  We find the least-cost partial
                    // path, across all paths, backtrace from it, and
                    // then prune all others.  Note that this, in
                    // general, can produce the wrong result, if the
                    // total best path did not in fact back trace
                    // through this partial best path.  But it's the
                    // best we can do... (short of not having a
                    // safety!).

                    // First pass: find least cost partial path so far,
                    // including ending at future positions:
                    int leastIDX = -1;
                    int leastCost = int.MaxValue;
                    Position leastPosData = null;
                    for (int pos2 = pos; pos2 < positions.NextPos; pos2++)
                    {
                        Position posData2 = positions.Get(pos2);
                        for (int idx = 0; idx < posData2.count; idx++)
                        {
                            //System.out.println("    idx=" + idx + " cost=" + cost);
                            int cost = posData2.costs[idx];
                            if (cost < leastCost)
                            {
                                leastCost = cost;
                                leastIDX = idx;
                                leastPosData = posData2;
                            }
                        }
                    }

                    // We will always have at least one live path:
                    if (Debugging.AssertsEnabled) Debugging.Assert(leastIDX != -1);

                    // Second pass: prune all but the best path:
                    for (int pos2 = pos; pos2 < positions.NextPos; pos2++)
                    {
                        Position posData2 = positions.Get(pos2);
                        if (posData2 != leastPosData)
                        {
                            posData2.Reset();
                        }
                        else
                        {
                            if (leastIDX != 0)
                            {
                                posData2.costs[0] = posData2.costs[leastIDX];
                                posData2.lastRightID[0] = posData2.lastRightID[leastIDX];
                                posData2.backPos[0] = posData2.backPos[leastIDX];
                                posData2.backWordPos[0] = posData2.backWordPos[leastIDX];
                                posData2.backIndex[0] = posData2.backIndex[leastIDX];
                                posData2.backID[0] = posData2.backID[leastIDX];
                                posData2.backType[0] = posData2.backType[leastIDX];
                            }
                            posData2.count = 1;
                        }
                    }

                    Backtrace(leastPosData, 0);

                    // Re-base cost so we don't risk int overflow:
                    Arrays.Fill(leastPosData.costs, 0, leastPosData.count, 0);

                    if (pos != leastPosData.pos)
                    {
                        // We jumped into a future position:
                        if (Debugging.AssertsEnabled) Debugging.Assert(pos < leastPosData.pos);
                        pos = leastPosData.pos;
                    }
                    if (pending.Count > 0)
                    {
                        return;
                    }
                    else
                    {
                        // This means the backtrace only produced
                        // punctuation tokens, so we must keep parsing.
                        continue;
                    }
                }

                if (VERBOSE)
                {
                    Console.Out.WriteLine("\n  extend @ pos=" + pos + " char=" + (char)buffer.Get(pos) + " hex=" + buffer.Get(pos).ToString("x4"));
                }

                if (VERBOSE)
                {
                    Console.Out.WriteLine("    " + posData.count + " arcs in");
                }

                // Move to the first character that is not a whitespace.
                // The whitespaces are added as a prefix for the term that we extract,
                // this information is then used when computing the cost for the term using
                // the space penalty factor.
                // They are removed when the final tokens are generated.
                if (Character.GetType(buffer.Get(pos)) == UnicodeCategory.SpaceSeparator)
                {
                    int nextChar = buffer.Get(++pos);
                    while (nextChar != -1 && Character.GetType(nextChar) == UnicodeCategory.SpaceSeparator)
                    {
                        pos++;
                        nextChar = buffer.Get(pos);
                    }
                }
                if (buffer.Get(pos) == -1)
                {
                    pos = posData.pos;
                }

                bool anyMatches = false;

                // First try user dict:
                if (userFST != null)
                {
                    userFST.GetFirstArc(arc);
                    int output = 0;
                    int maxPosAhead = 0;
                    int outputMaxPosAhead = 0;
                    int arcFinalOutMaxPosAhead = 0;

                    for (int posAhead = pos; ; posAhead++)
                    {
                        int ch = buffer.Get(posAhead);
                        if (ch == -1)
                        {
                            break;
                        }
                        if (userFST.FindTargetArc(ch, arc, arc, posAhead == pos, userFSTReader) == null)
                        {
                            break;
                        }
                        output += arc.Output.ToInt32();
                        if (arc.IsFinal)
                        {
                            maxPosAhead = posAhead;
                            outputMaxPosAhead = output;
                            arcFinalOutMaxPosAhead = arc.NextFinalOutput.ToInt32();
                            anyMatches = true;
                        }
                    }

                    // Longest matching for user word
                    if (anyMatches && maxPosAhead > userWordMaxPosAhead)
                    {
                        if (VERBOSE)
                        {
                            Console.Out.WriteLine("    USER word " + new string(buffer.Get(pos, maxPosAhead + 1)) + " toPos=" + (maxPosAhead + 1));
                        }
                        Add(userDictionary, posData, pos, maxPosAhead + 1, outputMaxPosAhead + arcFinalOutMaxPosAhead, Type.USER);
                        userWordMaxPosAhead = Math.Max(userWordMaxPosAhead, maxPosAhead);
                    }
                }

                // TODO: we can be more aggressive about user
                // matches?  if we are "under" a user match then don't
                // extend KNOWN/UNKNOWN paths?

                if (!anyMatches)
                {
                    // Next, try known dictionary matches
                    fst.GetFirstArc(arc);
                    int output = 0;

                    for (int posAhead = pos; ; posAhead++)
                    {
                        int ch = buffer.Get(posAhead);
                        if (ch == -1)
                        {
                            break;
                        }
                        //System.out.println("    match " + (char) ch + " posAhead=" + posAhead);

                        if (fst.FindTargetArc(ch, arc, arc, posAhead == pos, fstReader) == null)
                        {
                            break;
                        }

                        output += arc.Output.ToInt32();

                        // Optimization: for known words that are too-long
                        // (compound), we should pre-compute the 2nd
                        // best segmentation and store it in the
                        // dictionary instead of recomputing it each time a
                        // match is found.

                        if (arc.IsFinal)
                        {
                            dictionary.LookupWordIds(output + arc.NextFinalOutput.ToInt32(), wordIdRef);
                            if (VERBOSE)
                            {
                                Console.Out.WriteLine("    KNOWN word " + new string(buffer.Get(pos, posAhead - pos + 1)) + " toPos=" + (posAhead + 1) + " " + wordIdRef.Length + " wordIDs");
                            }
                            for (int ofs = 0; ofs < wordIdRef.Length; ofs++)
                            {
                                Add(dictionary, posData, pos, posAhead + 1, wordIdRef.Int32s[wordIdRef.Offset + ofs], Type.KNOWN);
                                anyMatches = true;
                            }
                        }
                    }
                }

                if (unknownWordEndIndex > posData.pos)
                {
                    pos++;
                    continue;
                }

                char firstCharacter = (char)buffer.Get(pos);
                if (!anyMatches || characterDefinition.IsInvoke(firstCharacter))
                {

                    // Find unknown match:
                    int characterId = characterDefinition.GetCharacterClass(firstCharacter);
                    // NOTE: copied from UnknownDictionary.lookup:
                    int unknownWordLength;
                    if (!characterDefinition.IsGroup(firstCharacter))
                    {
                        unknownWordLength = 1;
                    }
                    else
                    {
                        // Extract unknown word. Characters with the same script are considered to be part of unknown word
                        unknownWordLength = 1;
                        int scriptCode = UScript.GetScript(firstCharacter);
                        //UnicodeScript scriptCode = UnicodeScript.of((int)firstCharacter);
                        bool isPunct = IsPunctuation(firstCharacter);
                        for (int posAhead = pos + 1; unknownWordLength < MAX_UNKNOWN_WORD_LENGTH; posAhead++)
                        {
                            int next = buffer.Get(posAhead);
                            if (next == -1)
                            {
                                break;
                            }
                            char ch = (char)next;
                            UnicodeCategory chType = Character.GetType(ch);
                            //UnicodeScript sc = UnicodeScript.of(next);
                            int sc = UScript.GetScript(next);
                            bool sameScript = IsSameScript(scriptCode, sc)
                                // Non-spacing marks inherit the script of their base character,
                                // following recommendations from UTR #24.
                                || chType == UnicodeCategory.NonSpacingMark;

                            if (sameScript
                                  && IsPunctuation(ch, chType) == isPunct
                                  && characterDefinition.IsGroup(ch))
                            {
                                unknownWordLength++;
                            }
                            else
                            {
                                break;
                            }
                            // Update the script code and character class if the original script
                            // is Inherited or Common.
                            if (IsCommonOrInherited(scriptCode) && IsCommonOrInherited(sc) == false)
                            {
                                scriptCode = sc;
                                characterId = characterDefinition.GetCharacterClass(ch);
                            }
                        }
                    }

                    unkDictionary.LookupWordIds(characterId, wordIdRef); // characters in input text are supposed to be the same
                    if (VERBOSE)
                    {
                        Console.Out.WriteLine("    UNKNOWN word len=" + unknownWordLength + " " + wordIdRef.Length + " wordIDs");
                    }
                    for (int ofs = 0; ofs < wordIdRef.Length; ofs++)
                    {
                        Add(unkDictionary, posData, pos, pos + unknownWordLength, wordIdRef.Int32s[wordIdRef.Offset + ofs], Type.UNKNOWN);
                    }
                }

                pos++;
            }

            end = true;

            if (pos > 0)
            {

                Position endPosData = positions.Get(pos);
                int leastCost = int.MaxValue;
                int leastIDX = -1;
                if (VERBOSE)
                {
                    Console.Out.WriteLine("  end: " + endPosData.count + " nodes");
                }
                for (int idx = 0; idx < endPosData.count; idx++)
                {
                    // Add EOS cost:
                    int cost = endPosData.costs[idx] + costs.Get(endPosData.lastRightID[idx], 0);
                    //System.out.println("    idx=" + idx + " cost=" + cost + " (pathCost=" + endPosData.costs[idx] + " bgCost=" + costs.get(endPosData.lastRightID[idx], 0) + ") backPos=" + endPosData.backPos[idx]);
                    if (cost < leastCost)
                    {
                        leastCost = cost;
                        leastIDX = idx;
                    }
                }

                Backtrace(endPosData, leastIDX);
            }
            else
            {
                // No characters in the input string; return no tokens!
            }
        }

        // the pending list.  The pending list is then in-reverse
        // (last token should be returned first).
        private void Backtrace(Position endPosData, int fromIDX)
        {
            int endPos = endPosData.pos;

            if (VERBOSE)
            {
                Console.Out.WriteLine("\n  backtrace: endPos=" + endPos + " pos=" + this.pos + "; " + (this.pos - lastBackTracePos) + " characters; last=" + lastBackTracePos + " cost=" + endPosData.costs[fromIDX]);
            }

            char[] fragment = buffer.Get(lastBackTracePos, endPos - lastBackTracePos);

            if (dotOut != null)
            {
                dotOut.OnBacktrace(this, this.positions, lastBackTracePos, endPosData, fromIDX, fragment, end);
            }

            int pos = endPos;
            int bestIDX = fromIDX;

            // TODO: sort of silly to make Token instances here; the
            // back trace has all info needed to generate the
            // token.  So, we could just directly set the attrs,
            // from the backtrace, in incrementToken w/o ever
            // creating Token; we'd have to defer calling freeBefore
            // until after the backtrace was fully "consumed" by
            // incrementToken.

            while (pos > lastBackTracePos)
            {
                //System.out.println("BT: back pos=" + pos + " bestIDX=" + bestIDX);
                Position posData = positions.Get(pos);
                if (Debugging.AssertsEnabled) Debugging.Assert(bestIDX < posData.count);

                int backPos = posData.backPos[bestIDX];
                int backWordPos = posData.backWordPos[bestIDX];
                if (Debugging.AssertsEnabled) Debugging.Assert(backPos >= lastBackTracePos, "backPos=" + backPos + " vs lastBackTracePos=" + lastBackTracePos);
                // the length of the word without the whitespaces at the beginning.
                int length = pos - backWordPos;
                Type backType = posData.backType[bestIDX];
                int backID = posData.backID[bestIDX];
                int nextBestIDX = posData.backIndex[bestIDX];
                // the start of the word after the whitespace at the beginning.
                int fragmentOffset = backWordPos - lastBackTracePos;
                if (Debugging.AssertsEnabled) Debugging.Assert(fragmentOffset >= 0);

                IDictionary dict = GetDict(backType);

                if (outputUnknownUnigrams && backType == Type.UNKNOWN)
                {
                    // outputUnknownUnigrams converts unknown word into unigrams:
                    for (int i = length - 1; i >= 0; i--)
                    {
                        int charLen = 1;
                        if (i > 0 && char.IsLowSurrogate(fragment[fragmentOffset + i]))
                        {
                            i--;
                            charLen = 2;
                        }
                        DictionaryToken token = new DictionaryToken(Type.UNKNOWN,
                            unkDictionary,
                            CharacterDefinition.NGRAM,
                            fragment,
                            fragmentOffset + i,
                            charLen,
                            backWordPos + i,
                            backWordPos + i + charLen
                        );
                        pending.Add(token);
                        if (VERBOSE)
                        {
                            Console.Out.WriteLine("    add token=" + pending[pending.Count - 1]);
                        }
                    }
                }
                else
                {
                    DictionaryToken token = new DictionaryToken(backType,
                        dict,
                        backID,
                        fragment,
                        fragmentOffset,
                        length,
                        backWordPos,
                        backWordPos + length
                    );
                    if (token.POSType == POS.Type.MORPHEME || mode == DecompoundMode.NONE)
                    {
                        if (ShouldFilterToken(token) == false)
                        {
                            pending.Add(token);
                            if (VERBOSE)
                            {
                                Console.Out.WriteLine("    add token=" + pending[pending.Count - 1]);
                            }
                        }
                    }
                    else
                    {
                        Morpheme[] morphemes = token.GetMorphemes();
                        if (morphemes == null)
                        {
                            pending.Add(token);
                            if (VERBOSE)
                            {
                                Console.Out.WriteLine("    add token=" + pending[pending.Count - 1]);
                            }
                        }
                        else
                        {
                            int endOffset = backWordPos + length;
                            int posLen = 0;
                            // decompose the compound
                            for (int i = morphemes.Length - 1; i >= 0; i--)
                            {
                                Morpheme morpheme = morphemes[i];
                                Token compoundToken;
                                if (token.POSType == POS.Type.COMPOUND)
                                {
                                    if (Debugging.AssertsEnabled) Debugging.Assert(endOffset - morpheme.surfaceForm.Length >= 0);
                                    compoundToken = new DecompoundToken(morpheme.posTag, morpheme.surfaceForm,
                                        endOffset - morpheme.surfaceForm.Length, endOffset);
                                }
                                else
                                {
                                    compoundToken = new DecompoundToken(morpheme.posTag, morpheme.surfaceForm, token.StartOffset, token.EndOffset);
                                }
                                if (i == 0 && mode == DecompoundMode.MIXED)
                                {
                                    compoundToken.PositionIncrement = 0;
                                }
                                ++posLen;
                                endOffset -= morpheme.surfaceForm.Length;
                                pending.Add(compoundToken);
                                if (VERBOSE)
                                {
                                    Console.Out.WriteLine("    add token=" + pending[pending.Count - 1]);
                                }
                            }
                            if (mode == DecompoundMode.MIXED)
                            {
                                token.PositionLength = Math.Max(1, posLen);
                                pending.Add(token);
                                if (VERBOSE)
                                {
                                    Console.Out.WriteLine("    add token=" + pending[pending.Count - 1]);
                                }
                            }
                        }
                    }
                }
                if (discardPunctuation == false && backWordPos != backPos)
                {
                    // Add a token for whitespaces between terms
                    int offset = backPos - lastBackTracePos;
                    int len = backWordPos - backPos;
                    //System.out.println(offset + " " + fragmentOffset + " " + len + " " + backWordPos + " " + backPos);
                    unkDictionary.LookupWordIds(characterDefinition.GetCharacterClass(' '), wordIdRef);
                    DictionaryToken spaceToken = new DictionaryToken(Type.UNKNOWN, unkDictionary,
                        wordIdRef.Int32s[wordIdRef.Offset], fragment, offset, len, backPos, backPos + len);
                    pending.Add(spaceToken);
                }

                pos = backPos;
                bestIDX = nextBestIDX;
            }

            lastBackTracePos = endPos;

            if (VERBOSE)
            {
                Console.Out.WriteLine("  freeBefore pos=" + endPos);
            }
            // Notify the circular buffers that we are done with
            // these positions:
            buffer.FreeBefore(endPos);
            positions.FreeBefore(endPos);
        }

        internal IDictionary GetDict(Type type)
        {
            //return dictionaryMap.get(type);
            dictionaryMap.TryGetValue(type, out IDictionary result);
            return result;
        }

        private bool ShouldFilterToken(Token token)
        {
            return discardPunctuation && IsPunctuation(token.GetSurfaceForm()[token.Offset]);
        }

        private static bool IsPunctuation(char ch)
        {
            return IsPunctuation(ch, Character.GetType(ch));
        }

        private static bool IsPunctuation(char ch, UnicodeCategory cid)
        {
            // special case for Hangul Letter Araea (interpunct)
            if (ch == 0x318D)
            {
                return true;
            }
            switch (cid)
            {
                case UnicodeCategory.SpaceSeparator:
                case UnicodeCategory.LineSeparator:
                case UnicodeCategory.ParagraphSeparator:
                case UnicodeCategory.Control:
                case UnicodeCategory.Format:
                case UnicodeCategory.DashPunctuation:
                case UnicodeCategory.OpenPunctuation:
                case UnicodeCategory.ClosePunctuation:
                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.OtherPunctuation:
                case UnicodeCategory.MathSymbol:
                case UnicodeCategory.CurrencySymbol:
                case UnicodeCategory.ModifierSymbol:
                case UnicodeCategory.OtherSymbol:
                case UnicodeCategory.InitialQuotePunctuation:
                case UnicodeCategory.FinalQuotePunctuation:
                    //case Character.SPACE_SEPARATOR:
                    //case Character.LINE_SEPARATOR:
                    //case Character.PARAGRAPH_SEPARATOR:
                    //case Character.CONTROL:
                    //case Character.FORMAT:
                    //case Character.DASH_PUNCTUATION:
                    //case Character.START_PUNCTUATION:
                    //case Character.END_PUNCTUATION:
                    //case Character.CONNECTOR_PUNCTUATION:
                    //case Character.OTHER_PUNCTUATION:
                    //case Character.MATH_SYMBOL:
                    //case Character.CURRENCY_SYMBOL:
                    //case Character.MODIFIER_SYMBOL:
                    //case Character.OTHER_SYMBOL:
                    //case Character.INITIAL_QUOTE_PUNCTUATION:
                    //case Character.FINAL_QUOTE_PUNCTUATION:
                    return true;
                default:
                    return false;
            }
        }

        //private static bool IsCommonOrInherited(UnicodeScript script)
        //{
        //    return script == UnicodeScript.INHERITED ||
        //        script == UnicodeScript.COMMON;
        //}

        ///** Determine if two scripts are compatible. */
        //private static bool IsSameScript(UnicodeScript scriptOne, UnicodeScript scriptTwo)
        //{
        //    return scriptOne == scriptTwo
        //        || IsCommonOrInherited(scriptOne)
        //        || IsCommonOrInherited(scriptTwo);
        //}

        private static bool IsCommonOrInherited(int script)
        {
            return script == UScript.Inherited ||
                script == UScript.Common;
        }

        /// <summary>Determine if two scripts are compatible.</summary>
        private static bool IsSameScript(int scriptOne, int scriptTwo)
        {
            return scriptOne <= UScript.Inherited || scriptTwo <= UScript.Inherited
                || scriptOne == scriptTwo;
        }
    }

    /// <summary>
    /// Decompound mode: this determines how the tokenizer handles
    /// <see cref="POS.Type.COMPOUND"/>, <see cref="POS.Type.INFLECT"/>, and <see cref="POS.Type.PREANALYSIS"/> tokens.
    /// </summary>
    public enum DecompoundMode
    {
        /// <summary>
        /// No decomposition for compound.
        /// </summary>
        NONE,

        /// <summary>
        /// Decompose compounds and discards the original form (default).
        /// </summary>
        DISCARD,

        /// <summary>
        /// Decompose compounds and keeps the original form.
        /// </summary>
        MIXED
    }


    // LUCENENET specific - de-nested Position

    // Holds all back pointers arriving to this position:
    internal sealed class Position
    {

        internal int pos;

        internal int count;

        // maybe single int array * 5?
        internal int[] costs = new int[8];
        internal int[] lastRightID = new int[8];
        internal int[] backPos = new int[8];
        internal int[] backWordPos = new int[8];
        internal int[] backIndex = new int[8];
        internal int[] backID = new int[8];
        internal KoreanTokenizer.Type[] backType = new KoreanTokenizer.Type[8];

        public void Grow()
        {
            costs = ArrayUtil.Grow(costs, 1 + count);
            lastRightID = ArrayUtil.Grow(lastRightID, 1 + count);
            backPos = ArrayUtil.Grow(backPos, 1 + count);
            backWordPos = ArrayUtil.Grow(backWordPos, 1 + count);
            backIndex = ArrayUtil.Grow(backIndex, 1 + count);
            backID = ArrayUtil.Grow(backID, 1 + count);

            // NOTE: sneaky: grow separately because
            // ArrayUtil.grow will otherwise pick a different
            // length than the int[]s we just grew:
            KoreanTokenizer.Type[] newBackType = new KoreanTokenizer.Type[backID.Length];
            System.Array.Copy(backType, 0, newBackType, 0, backType.Length);
            backType = newBackType;
        }

        public void Add(int cost, int lastRightID, int backPos, int backRPos, int backIndex, int backID, KoreanTokenizer.Type backType)
        {
            // NOTE: this isn't quite a true Viterbi search,
            // because we should check if lastRightID is
            // already present here, and only update if the new
            // cost is less than the current cost, instead of
            // simply appending.  However, that will likely hurt
            // performance (usually we add a lastRightID only once),
            // and it means we actually create the full graph
            // intersection instead of a "normal" Viterbi lattice:
            if (count == costs.Length)
            {
                Grow();
            }
            this.costs[count] = cost;
            this.lastRightID[count] = lastRightID;
            this.backPos[count] = backPos;
            this.backWordPos[count] = backRPos;
            this.backIndex[count] = backIndex;
            this.backID[count] = backID;
            this.backType[count] = backType;
            count++;
        }

        public void Reset()
        {
            count = 0;
        }
    }

    // LUCENENET specific - de-nested WrappedPositionArray

    // TODO: make generic'd version of this "circular array"?
    // It's a bit tricky because we do things to the Position
    // (eg, set .pos = N on reuse)...
    internal sealed class WrappedPositionArray
    {
        private Position[] positions = new Position[8];

        public WrappedPositionArray()
        {
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = new Position();
            }
        }

        // Next array index to write to in positions:
        private int nextWrite;

        // Next position to write:
        private int nextPos;

        // How many valid Position instances are held in the
        // positions array:
        private int count;

        public void Reset()
        {
            nextWrite--;
            while (count > 0)
            {
                if (nextWrite == -1)
                {
                    nextWrite = positions.Length - 1;
                }
                positions[nextWrite--].Reset();
                count--;
            }
            nextWrite = 0;
            nextPos = 0;
            count = 0;
        }

        /// <summary>
        /// Get <see cref="Position"/> instance for this absolute position;
        /// this is allowed to be arbitrarily far "in the
        /// future" but cannot be before the last freeBefore.
        /// </summary>
        public Position Get(int pos)
        {
            while (pos >= nextPos)
            {
                //System.out.println("count=" + count + " vs len=" + positions.length);
                if (count == positions.Length)
                {
                    Position[] newPositions = new Position[ArrayUtil.Oversize(1 + count, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    //System.out.println("grow positions " + newPositions.length);
                    System.Array.Copy(positions, nextWrite, newPositions, 0, positions.Length - nextWrite);
                    System.Array.Copy(positions, 0, newPositions, positions.Length - nextWrite, nextWrite);
                    for (int i = positions.Length; i < newPositions.Length; i++)
                    {
                        newPositions[i] = new Position();
                    }
                    nextWrite = positions.Length;
                    positions = newPositions;
                }
                if (nextWrite == positions.Length)
                {
                    nextWrite = 0;
                }
                // Should have already been reset:
                if (Debugging.AssertsEnabled) Debugging.Assert(positions[nextWrite].count == 0);
                positions[nextWrite++].pos = nextPos++;
                count++;
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(InBounds(pos));
            int index = GetIndex(pos);
            if (Debugging.AssertsEnabled) Debugging.Assert(positions[index].pos == pos);
            return positions[index];
        }

        public int NextPos => nextPos;

        // For assert:
        private bool InBounds(int pos)
        {
            return pos < nextPos && pos >= nextPos - count;
        }

        private int GetIndex(int pos)
        {
            int index = nextWrite - (nextPos - pos);
            if (index < 0)
            {
                index += positions.Length;
            }
            return index;
        }

        public void FreeBefore(int pos)
        {
            int toFree = count - (nextPos - pos);
            if (Debugging.AssertsEnabled) Debugging.Assert(toFree >= 0);
            if (Debugging.AssertsEnabled) Debugging.Assert(toFree <= count);
            int index = nextWrite - count;
            if (index < 0)
            {
                index += positions.Length;
            }
            for (int i = 0; i < toFree; i++)
            {
                if (index == positions.Length)
                {
                    index = 0;
                }
                //System.out.println("  fb idx=" + index);
                positions[index].Reset();
                index++;
            }
            count -= toFree;
        }
    }
}

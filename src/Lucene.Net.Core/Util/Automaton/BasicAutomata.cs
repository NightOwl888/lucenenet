using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Text;

/*
 * dk.brics.automaton
 *
 * Copyright (c) 2001-2009 Anders Moeller
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 *
 * this SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * this SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace Lucene.Net.Util.Automaton
{
    /// <summary>
    /// Construction of basic automata.
    ///
    /// @lucene.experimental
    /// </summary>
    internal sealed class BasicAutomata
    {
        private BasicAutomata()
        {
        }

        /// <summary>
        /// Returns a new (deterministic) automaton with the empty language.
        /// </summary>
        public static Automaton MakeEmpty()
        {
            Automaton a = new Automaton();
            State s = new State();
            a.initial = s;
            a.deterministic = true;
            return a;
        }

        /// <summary>
        /// Returns a new (deterministic) automaton that accepts only the empty string.
        /// </summary>
        public static Automaton MakeEmptyString()
        {
            Automaton a = new Automaton();
            a.singleton = "";
            a.deterministic = true;
            return a;
        }

        /// <summary>
        /// Returns a new (deterministic) automaton that accepts all strings.
        /// </summary>
        public static Automaton MakeAnyString()
        {
            Automaton a = new Automaton();
            State s = new State();
            a.initial = s;
            s.accept = true;
            s.AddTransition(new Transition(Character.MIN_CODE_POINT, Character.MAX_CODE_POINT, s));
            a.deterministic = true;
            return a;
        }

        /// <summary>
        /// Returns a new (deterministic) automaton that accepts any single codepoint.
        /// </summary>
        public static Automaton MakeAnyChar()
        {
            return MakeCharRange(Character.MIN_CODE_POINT, Character.MAX_CODE_POINT);
        }

        /// <summary>
        /// Returns a new (deterministic) automaton that accepts a single codepoint of
        /// the given value.
        /// </summary>
        public static Automaton MakeChar(int c)
        {
            Automaton a = new Automaton();
            a.singleton = new string(Character.ToChars(c));
            a.deterministic = true;
            return a;
        }

        /// <summary>
        /// Returns a new (deterministic) automaton that accepts a single codepoint whose
        /// value is in the given interval (including both end points).
        /// </summary>
        public static Automaton MakeCharRange(int min, int max)
        {
            if (min == max)
            {
                return MakeChar(min);
            }
            Automaton a = new Automaton();
            State s1 = new State();
            State s2 = new State();
            a.initial = s1;
            s2.accept = true;
            if (min <= max)
            {
                s1.AddTransition(new Transition(min, max, s2));
            }
            a.deterministic = true;
            return a;
        }

        /// <summary>
        /// Constructs sub-automaton corresponding to decimal numbers of length
        /// x.substring(n).length().
        /// </summary>
        private static State AnyOfRightLength(string x, int n)
        {
            State s = new State();
            if (x.Length == n)
            {
                s.Accept = true;
            }
            else
            {
                s.AddTransition(new Transition('0', '9', AnyOfRightLength(x, n + 1)));
            }
            return s;
        }

        /// <summary>
        /// Constructs sub-automaton corresponding to decimal numbers of value at least
        /// x.substring(n) and length x.substring(n).length().
        /// </summary>
        private static State AtLeast(string x, int n, ICollection<State> initials, bool zeros)
        {
            State s = new State();
            if (x.Length == n)
            {
                s.Accept = true;
            }
            else
            {
                if (zeros)
                {
                    initials.Add(s);
                }
                char c = x[n];
                s.AddTransition(new Transition(c, AtLeast(x, n + 1, initials, zeros && c == '0')));
                if (c < '9')
                {
                    s.AddTransition(new Transition((char)(c + 1), '9', AnyOfRightLength(x, n + 1)));
                }
            }
            return s;
        }

        /// <summary>
        /// Constructs sub-automaton corresponding to decimal numbers of value at most
        /// x.substring(n) and length x.substring(n).length().
        /// </summary>
        private static State AtMost(string x, int n)
        {
            State s = new State();
            if (x.Length == n)
            {
                s.Accept = true;
            }
            else
            {
                char c = x[n];
                s.AddTransition(new Transition(c, AtMost(x, (char)n + 1)));
                if (c > '0')
                {
                    s.AddTransition(new Transition('0', (char)(c - 1), AnyOfRightLength(x, n + 1)));
                }
            }
            return s;
        }

        /// <summary>
        /// Constructs sub-automaton corresponding to decimal numbers of value between
        /// x.substring(n) and y.substring(n) and of length x.substring(n).length()
        /// (which must be equal to y.substring(n).length()).
        /// </summary>
        private static State Between(string x, string y, int n, ICollection<State> initials, bool zeros)
        {
            State s = new State();
            if (x.Length == n)
            {
                s.Accept = true;
            }
            else
            {
                if (zeros)
                {
                    initials.Add(s);
                }
                char cx = x[n];
                char cy = y[n];
                if (cx == cy)
                {
                    s.AddTransition(new Transition(cx, Between(x, y, n + 1, initials, zeros && cx == '0')));
                }
                else // cx<cy
                {
                    s.AddTransition(new Transition(cx, AtLeast(x, n + 1, initials, zeros && cx == '0')));
                    s.AddTransition(new Transition(cy, AtMost(y, n + 1)));
                    if (cx + 1 < cy)
                    {
                        s.AddTransition(new Transition((char)(cx + 1), (char)(cy - 1), AnyOfRightLength(x, n + 1)));
                    }
                }
            }
            return s;
        }

        /// <summary>
        /// Returns a new automaton that accepts strings representing decimal
        /// non-negative integers in the given interval.
        /// </summary>
        /// <param name="min"> minimal value of interval </param>
        /// <param name="max"> maximal value of interval (both end points are included in the
        ///          interval) </param>
        /// <param name="digits"> if >0, use fixed number of digits (strings must be prefixed
        ///          by 0's to obtain the right length) - otherwise, the number of
        ///          digits is not fixed </param>
        /// <exception cref="IllegalArgumentException"> if min>max or if numbers in the
        ///              interval cannot be expressed with the given fixed number of
        ///              digits </exception>
        public static Automaton MakeInterval(int min, int max, int digits)
        {
            Automaton a = new Automaton();
            string x = Convert.ToString(min);
            string y = Convert.ToString(max);
            if (min > max || (digits > 0 && y.Length > digits))
            {
                throw new System.ArgumentException();
            }
            int d;
            if (digits > 0)
            {
                d = digits;
            }
            else
            {
                d = y.Length;
            }
            StringBuilder bx = new StringBuilder();
            for (int i = x.Length; i < d; i++)
            {
                bx.Append('0');
            }
            bx.Append(x);
            x = bx.ToString();
            StringBuilder by = new StringBuilder();
            for (int i = y.Length; i < d; i++)
            {
                by.Append('0');
            }
            by.Append(y);
            y = by.ToString();
            ICollection<State> initials = new List<State>();
            a.initial = Between(x, y, 0, initials, digits <= 0);
            if (digits <= 0)
            {
                List<StatePair> pairs = new List<StatePair>();
                foreach (State p in initials)
                {
                    if (a.initial != p)
                    {
                        pairs.Add(new StatePair(a.initial, p));
                    }
                }
                BasicOperations.AddEpsilons(a, pairs);
                a.initial.AddTransition(new Transition('0', a.initial));
                a.deterministic = false;
            }
            else
            {
                a.deterministic = true;
            }
            a.CheckMinimizeAlways();
            return a;
        }

        /// <summary>
        /// Returns a new (deterministic) automaton that accepts the single given
        /// string.
        /// </summary>
        public static Automaton MakeString(string s)
        {
            Automaton a = new Automaton();
            a.singleton = s;
            a.deterministic = true;
            return a;
        }

        public static Automaton MakeString(int[] word, int offset, int length)
        {
            Automaton a = new Automaton();
            a.IsDeterministic = true;
            State s = new State();
            a.initial = s;
            for (int i = offset; i < offset + length; i++)
            {
                State s2 = new State();
                s.AddTransition(new Transition(word[i], s2));
                s = s2;
            }
            s.accept = true;
            return a;
        }

        /// <summary>
        /// Returns a new (deterministic and minimal) automaton that accepts the union
        /// of the given collection of <seealso cref="BytesRef"/>s representing UTF-8 encoded
        /// strings.
        /// </summary>
        /// <param name="utf8Strings">
        ///          The input strings, UTF-8 encoded. The collection must be in sorted
        ///          order.
        /// </param>
        /// <returns> An <seealso cref="Automaton"/> accepting all input strings. The resulting
        ///         automaton is codepoint based (full unicode codepoints on
        ///         transitions). </returns>
        public static Automaton MakeStringUnion(ICollection<BytesRef> utf8Strings)
        {
            if (utf8Strings.Count == 0)
            {
                return MakeEmpty();
            }
            else
            {
                return DaciukMihovAutomatonBuilder.Build(utf8Strings);
            }
        }
    }
}
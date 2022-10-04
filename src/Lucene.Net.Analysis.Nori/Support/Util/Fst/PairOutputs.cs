// Lucene version compatibility level 4.8.0
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Support.Util.Fst
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

    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;

    /// <summary>
    /// An FST <see cref="Outputs{T}"/> implementation, holding two other outputs.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class PairOutputs<A, B> : Outputs<Pair<A, B>>
        where A : class // LUCENENET specific - added class constraints because we compare reference equality
        where B : class
    {
        private readonly Pair<A, B> NO_OUTPUT;
        private readonly Outputs<A> outputs1;
        private readonly Outputs<B> outputs2;

        // LUCENENET specific - de-nested Pair

        public PairOutputs(Outputs<A> outputs1, Outputs<B> outputs2)
        {
            this.outputs1 = outputs1;
            this.outputs2 = outputs2;
            NO_OUTPUT = new Pair<A, B>(outputs1.NoOutput, outputs2.NoOutput);
        }

        /// <summary>
        /// Create a new <see cref="Pair{A, B}"/> </summary>
        public virtual Pair<A, B> NewPair(A a, B b)
        {
            if (a.Equals(outputs1.NoOutput))
            {
                a = outputs1.NoOutput;
            }
            if (b.Equals(outputs2.NoOutput))
            {
                b = outputs2.NoOutput;
            }

            if (a == outputs1.NoOutput && b == outputs2.NoOutput)
            {
                return NO_OUTPUT;
            }
            else
            {
                var p = new Pair<A, B>(a, b);
                if (Debugging.AssertsEnabled) Debugging.Assert(Valid(p));
                return p;
            }
        }

        // for assert
        private bool Valid(Pair<A, B> pair)
        {
            bool noOutput1 = pair.Output1.Equals(outputs1.NoOutput);
            bool noOutput2 = pair.Output2.Equals(outputs2.NoOutput);

            if (noOutput1 && pair.Output1 != outputs1.NoOutput)
            {
                return false;
            }

            if (noOutput2 && pair.Output2 != outputs2.NoOutput)
            {
                return false;
            }

            if (noOutput1 && noOutput2)
            {
                if (pair != NO_OUTPUT)
                    return false;

                return true;
            }
            else
            {
                return true;
            }
        }

        public override Pair<A, B> Common(Pair<A, B> pair1, Pair<A, B> pair2)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(Valid(pair1));
                Debugging.Assert(Valid(pair2));
            }
            return NewPair(outputs1.Common(pair1.Output1, pair2.Output1),
                           outputs2.Common(pair1.Output2, pair2.Output2));
        }

        public override Pair<A, B> Subtract(Pair<A, B> output, Pair<A, B> inc)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(Valid(output));
                Debugging.Assert(Valid(inc));
            }
            return NewPair(outputs1.Subtract(output.Output1, inc.Output1),
                           outputs2.Subtract(output.Output2, inc.Output2));
        }

        public override Pair<A, B> Add(Pair<A, B> prefix, Pair<A, B> output)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(Valid(prefix));
                Debugging.Assert(Valid(output));
            }
            return NewPair(outputs1.Add(prefix.Output1, output.Output1),
                           outputs2.Add(prefix.Output2, output.Output2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write(Pair<A, B> output, DataOutput writer)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(Valid(output));
            outputs1.Write(output.Output1, writer);
            outputs2.Write(output.Output2, writer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Pair<A, B> Read(DataInput @in)
        {
            A output1 = outputs1.Read(@in);
            B output2 = outputs2.Read(@in);
            return NewPair(output1, output2);
        }

        public override void SkipOutput(DataInput input)
        {
            outputs1.SkipOutput(input);
            outputs2.SkipOutput(input);
        }

        public override Pair<A, B> NoOutput => NO_OUTPUT;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string OutputToString(Pair<A, B> output)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(Valid(output));
            return "<pair:" + outputs1.OutputToString(output.Output1) + "," + outputs2.OutputToString(output.Output2) + ">";
        }

        public override string ToString()
        {
            return "PairOutputs<" + outputs1 + "," + outputs2 + ">";
        }

        private static readonly long BASE_NUM_BYTES = RamUsageEstimator.ShallowSizeOf(new Pair<A, B>(null, null));

        public override long GetRamBytesUsed(Pair<A, B> output)
        {
            long ramBytesUsed = BASE_NUM_BYTES;
            if (output.Output1 != null)
            {
                ramBytesUsed += outputs1.GetRamBytesUsed(output.Output1);
            }
            if (output.Output2 != null)
            {
                ramBytesUsed += outputs2.GetRamBytesUsed(output.Output2);
            }
            return ramBytesUsed;
        }
    }

    /// <summary>
    /// Holds a single pair of two outputs. </summary>
    public class Pair<A, B>
    {
        public A Output1 { get; private set; }
        public B Output2 { get; private set; }

        // use newPair
        internal Pair(A output1, B output2)
        {
            this.Output1 = output1;
            this.Output2 = output2;
        }

        public override bool Equals(object other)
        {
            // LUCENENET specific - simplified expression
            return ReferenceEquals(other, this) || (other is Pair<A, B> pair && Output1.Equals(pair.Output1) && Output2.Equals(pair.Output2));
        }

        public override int GetHashCode()
        {
            return Output1.GetHashCode() + Output2.GetHashCode();
        }

        public override string ToString()
        {
            return $"Pair({Output1},{Output2})";
        }
    }
}
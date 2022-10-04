// Lucene version compatibility level 4.8.0
using J2N.Collections;
using J2N.Numerics;
using Lucene.Net.Codecs;
using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using JCG = J2N.Collections.Generic;

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

    // TODO: break this into WritableFST and ReadOnlyFST.. then
    // we can have subclasses of ReadOnlyFST to handle the
    // different byte[] level encodings (packed or
    // not)... and things like nodeCount, arcCount are read only

    // TODO: if FST is pure prefix trie we can do a more compact
    // job, ie, once we are at a 'suffix only', just store the
    // completion labels as a string not as a series of arcs.

    // NOTE: while the FST is able to represent a non-final
    // dead-end state (NON_FINAL_END_NODE=0), the layers above
    // (FSTEnum, Util) have problems with this!!

    /// <summary>
    /// Represents an finite state machine (FST), using a
    /// compact <see cref="T:byte[]"/> format.
    /// <para/> The format is similar to what's used by Morfologik
    /// (http://sourceforge.net/projects/morfologik).
    ///
    /// <para/> See the <a href="https://lucene.apache.org/core/4_8_0/core/org/apache/lucene/util/fst/package-summary.html">
    /// FST package documentation</a> for some simple examples.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class FST<T>
        where T : class // LUCENENET specific - added class constraint, since we compare reference equality
    {
        private static readonly long BASE_RAM_BYTES_USED = RamUsageEstimator.ShallowSizeOfInstance(typeof(FST<T>));
        private static readonly long ARC_SHALLOW_RAM_BYTES_USED = RamUsageEstimator.ShallowSizeOfInstance(typeof(FST.Arc<T>));

        // LUCENENET specific - moved INPUT_TYPE to FST class

        // LUCENENET specific - moved BIT_FINAL_ARC to FST class
        // LUCENENET specific - moved BIT_LAST_ARC to FST class
        // LUCENENET specific - moved BIT_TARGET_NEXT to FST class

        // LUCENENET specific - moved BIT_STOP_NODE to FST class

        // LUCENENET specific - moved BIT_ARC_HAS_FINAL_OUTPUT to FST class

        // LUCENENET specific - moved ARCS_AS_FIXED_ARRAY to FST class

        // LUCENENET specific - moved BIT_MISSING_ARC to FST class
        // LUCENENET specific - moved ARCS_AS_ARRAY_WITH_GAPS to FST class

        // LUCENENET specific - moved FIXED_ARRAY_SHALLOW_DISTANCE to FST class

        // LUCENENET specific - moved FIXED_ARRAY_NUM_ARCS_SHALLOW to FST class

        // LUCENENET specific - moved FIXED_ARRAY_NUM_ARCS_DEEP to FST class

        // LUCENENET specific - moved FILE_FORMAT_NAME to FST class
        // LUCENENET specific - moved VERSION_START to FST class

        // LUCENENET specific - moved VERSION_CURRENT to FST class

        // LUCENENET specific - moved FINAL_END_NODE to FST class

        // LUCENENET specific - moved NON_FINAL_END_NODE to FST class

        // LUCENENET specific - moved END_LABEL to FST class

        private readonly FST.INPUT_TYPE inputType; // LUCENENET: Made accessible through public property

        // if non-null, this FST accepts the empty string and
        // produces this output
        internal T emptyOutput;

        /// <summary>
        /// A <see cref="BytesStore"/>, used during building, or during reading when
        /// the FST is very large (more than 1 GB).  If the FST is less than 1
        /// GB then bytesArray is set instead.
        /// </summary>
        internal readonly BytesStore bytes;

        private readonly IFSTStore fstStore;

        private long startNode = -1;

        public Outputs<T> Outputs { get; private set; }

        private FST.Arc<T>[] cachedRootArcs;

        // LUCENENET specific: Arc<T> moved into FST class

        internal static bool Flag(int flags, int bit)
        {
            return (flags & bit) != 0;
        }

        private readonly int version;

        // make a new empty FST, for building; Builder invokes
        // this ctor
        internal FST(FST.INPUT_TYPE inputType, Outputs<T> outputs, int bytesPageBits)
        {
            this.inputType = inputType;
            this.Outputs = outputs;
            version = FST.VERSION_CURRENT;
            fstStore = null;
            bytes = new BytesStore(bytesPageBits);
            // pad: ensure no node gets address 0 which is reserved to mean
            // the stop state w/ no arcs
            bytes.WriteByte(0);
            emptyOutput = null;
        }

        // LUCENENET specific: DEFAULT_MAX_BLOCK_BITS moved into FST class

        /// <summary>
        /// Load a previously saved FST. </summary>
        public FST(DataInput input, Outputs<T> outputs)
            : this(input, outputs, new OnHeapFSTStore(FST.DEFAULT_MAX_BLOCK_BITS))
        {
        }

        /// <summary>
        /// Load a previously saved FST; <paramref name="fstStore"/> allows you to
        /// control the size of the <see cref="T:byte[]"/> pages used to hold the FST bytes.
        /// </summary>
        public FST(DataInput input, Outputs<T> outputs, IFSTStore fstStore)
        {
            bytes = null;
            this.fstStore = fstStore;
            this.Outputs = outputs;

            // NOTE: only reads most recent format; we don't have
            // back-compat promise for FSTs (they are experimental):
            version = CodecUtil.CheckHeader(input, FST.FILE_FORMAT_NAME, FST.VERSION_START, FST.VERSION_CURRENT);
            if (input.ReadByte() == 1)
            {
                // accepts empty string
                // 1 KB blocks:
                BytesStore emptyBytes = new BytesStore(10);
                int numBytes = input.ReadVInt32();
                emptyBytes.CopyBytes(input, numBytes);

                // De-serialize empty-string output:
                FST.BytesReader reader = emptyBytes.GetReverseReader();
                // NoOutputs uses 0 bytes when writing its output,
                // so we have to check here else BytesStore gets
                // angry:
                if (numBytes > 0)
                {
                    reader.Position = numBytes - 1;
                }
                emptyOutput = outputs.ReadFinalOutput(reader);
            }
            else
            {
                emptyOutput = null;
            }
            var t = input.ReadByte();
            inputType = t switch
            {
                0 => FST.INPUT_TYPE.BYTE1,
                1 => FST.INPUT_TYPE.BYTE2,
                2 => FST.INPUT_TYPE.BYTE4,
                _ => throw IllegalStateException.Create("invalid input type " + t),
            };
            startNode = input.ReadVInt64();

            long numBytes_ = input.ReadVInt64();
            this.fstStore.Init(input, numBytes_);
            CacheRootArcs();
        }

        public FST.INPUT_TYPE InputType => inputType;

        private long GetRamBytesUsed(FST.Arc<T>[] arcs)
        {
            long size = 0;
            if (arcs != null)
            {
                size += RamUsageEstimator.ShallowSizeOf(arcs);
                foreach (FST.Arc<T> arc in arcs)
                {
                    if (arc != null)
                    {
                        size += ARC_SHALLOW_RAM_BYTES_USED;
                        if (arc.Output != null && arc.Output != Outputs.NoOutput)
                        {
                            size += Outputs.GetRamBytesUsed(arc.Output);
                        }
                        if (arc.NextFinalOutput != null && arc.NextFinalOutput != Outputs.NoOutput)
                        {
                            size += Outputs.GetRamBytesUsed(arc.NextFinalOutput);
                        }
                    }
                }
            }
            return size;
        }

        private int cachedArcsBytesUsed;

        /// <summary>
        /// Returns bytes used to represent the FST </summary>
        public long GetRamBytesUsed()
        {
            long size = BASE_RAM_BYTES_USED;
            if (this.fstStore != null)
            {
                size += this.fstStore.GetRamBytesUsed();
            }
            else
            {
                size += bytes.GetRamBytesUsed();
            }

            size += cachedArcsBytesUsed;
            return size;
        }

        public override string ToString()
        {
            return GetType().Name + "(input=" + inputType + ",output=" + Outputs;
        }

        internal void Finish(long newStartNode)
        {
            if (startNode != -1)
            {
                throw IllegalStateException.Create("already finished");
            }
            if (newStartNode == FST.FINAL_END_NODE && !EqualityComparer<T>.Default.Equals(emptyOutput, default))
            {
                newStartNode = 0;
            }
            startNode = newStartNode;
            bytes.Finish();

            CacheRootArcs();
        }

        // Caches first 128 labels
        private void CacheRootArcs()
        {
            // We should only be called once per FST:
            if (Debugging.AssertsEnabled) Debugging.Assert(cachedArcsBytesUsed == 0);
            FST.Arc<T> arc = new FST.Arc<T>();
            GetFirstArc(arc);
            if (FST.TargetHasArcs(arc))
            {
                FST.BytesReader @in = GetBytesReader();
                FST.Arc<T>[] arcs = new FST.Arc<T>[0x80];
                ReadFirstRealTargetArc(arc.Target, arc, @in);
                int count = 0;
                while (true)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(arc.Label != FST.END_LABEL);
                    if (arc.Label < arcs.Length)
                    {
                        arcs[arc.Label] = new FST.Arc<T>().CopyFrom(arc);
                    }
                    else
                    {
                        break;
                    }
                    if (arc.IsLast)
                    {
                        break;
                    }
                    ReadNextRealArc(arc, @in);
                    count++;
                }

                int cacheRAM = (int)GetRamBytesUsed(arcs);

                // Don't cache if there are only a few arcs or if the cache would use > 20% RAM of the FST itself:
                if (count >= FST.FIXED_ARRAY_NUM_ARCS_SHALLOW && cacheRAM < GetRamBytesUsed() / 5)
                {
                    cachedRootArcs = arcs;
                    cachedArcsBytesUsed = cacheRAM;
                }
            }
        }

        public T EmptyOutput => emptyOutput;

        internal void SetEmptyOutput(T value)
        {
            if (emptyOutput != null)
            {
                emptyOutput = Outputs.Merge(emptyOutput, value);
            }
            else
            {
                emptyOutput = value;
            }
        }

        public void Save(DataOutput @out)
        {
            if (startNode == -1)
            {
                throw IllegalStateException.Create("call finish first");
            }
            CodecUtil.WriteHeader(@out, FST.FILE_FORMAT_NAME, FST.VERSION_CURRENT);
            // TODO: really we should encode this as an arc, arriving
            // to the root node, instead of special casing here:
            if (!EqualityComparer<T>.Default.Equals(emptyOutput, default))
            {
                // Accepts empty string
                @out.WriteByte(1);

                // Serialize empty-string output:
                var ros = new RAMOutputStream();
                Outputs.WriteFinalOutput(emptyOutput, ros);

                var emptyOutputBytes = new byte[(int)ros.Position]; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                ros.WriteTo(emptyOutputBytes, 0);

                // reverse
                int stopAt = emptyOutputBytes.Length / 2;
                int upto = 0;
                while (upto < stopAt)
                {
                    var b = emptyOutputBytes[upto];
                    emptyOutputBytes[upto] = emptyOutputBytes[emptyOutputBytes.Length - upto - 1];
                    emptyOutputBytes[emptyOutputBytes.Length - upto - 1] = b;
                    upto++;
                }
                @out.WriteVInt32(emptyOutputBytes.Length);
                @out.WriteBytes(emptyOutputBytes, 0, emptyOutputBytes.Length);
            }
            else
            {
                @out.WriteByte(0);
            }
            sbyte t;
            if (inputType == FST.INPUT_TYPE.BYTE1)
            {
                t = 0;
            }
            else if (inputType == FST.INPUT_TYPE.BYTE2)
            {
                t = 1;
            }
            else
            {
                t = 2;
            }
            @out.WriteByte((byte)t);
            @out.WriteVInt64(startNode);
            if (bytes != null)
            {
                long numBytes = bytes.Position;
                @out.WriteVInt64(numBytes);
                bytes.WriteTo(@out);
            } else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(fstStore != null);
                fstStore.WriteTo(@out);
            }
        }

        /// <summary>
        /// Writes an automaton to a file.
        /// </summary>
        public void Save(FileInfo file) // LUCENENET: .NET doesn't have a Path object, so we use FileInfo
        {
            using var os = file.OpenWrite();
            Save(new OutputStreamDataOutput(os));
        }

        // LUCENENET specific - static Read<T>() moved to FST class

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteLabel(DataOutput @out, int v)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(v >= 0,"v={0}", v);
            if (inputType == FST.INPUT_TYPE.BYTE1)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(v <= 255,"v={0}", v);
                @out.WriteByte((byte)v);
            }
            else if (inputType == FST.INPUT_TYPE.BYTE2)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(v <= 65535,"v={0}", v);
                @out.WriteInt16((short)v);
            }
            else
            {
                @out.WriteVInt32(v);
            }
        }

        public int ReadLabel(DataInput input)
        {
            int v;
            if (inputType == FST.INPUT_TYPE.BYTE1)
            {
                // Unsigned byte:
                v = input.ReadByte() & 0xFF;
            }
            else if (inputType == FST.INPUT_TYPE.BYTE2)
            {
                // Unsigned short:
                v = input.ReadInt16() & 0xFFFF;
            }
            else
            {
                v = input.ReadVInt32();
            }
            return v;
        }

        // LUCENENET specific - static TargetHasArcs() moved to FST class


        // serializes new node by appending its bytes to the end
        // of the current byte[]
        internal long AddNode(Builder<T> builder, Builder.UnCompiledNode<T> nodeIn)
        {
            T NO_OUTPUT = Outputs.NoOutput;

            //System.out.println("FST.addNode pos=" + bytes.getPosition() + " numArcs=" + nodeIn.numArcs);
            if (nodeIn.NumArcs == 0)
            {
                if (nodeIn.IsFinal)
                {
                    return FST.FINAL_END_NODE;
                }
                else
                {
                    return FST.NON_FINAL_END_NODE;
                }
            }

            long startAddress = builder.bytes.Position;
            //System.out.println("  startAddr=" + startAddress);

            bool doFixedArray = ShouldExpand(builder, nodeIn);
            if (doFixedArray)
            {
                //System.out.println("  fixedArray");
                if (builder.reusedBytesPerArc.Length < nodeIn.NumArcs)
                {
                    builder.reusedBytesPerArc = new int[ArrayUtil.Oversize(nodeIn.NumArcs, 1)];
                }
            }

            builder.arcCount += nodeIn.NumArcs;

            int lastArc = nodeIn.NumArcs - 1;

            long lastArcStart = builder.bytes.Position;
            int maxBytesPerArc = 0;
            for (int arcIdx = 0; arcIdx < nodeIn.NumArcs; arcIdx++)
            {
                Builder.Arc<T> arc = nodeIn.Arcs[arcIdx];
                var target = (Builder.CompiledNode)arc.Target;
                int flags = 0;
                //System.out.println("  arc " + arcIdx + " label=" + arc.Label + " -> target=" + target.Node);

                if (arcIdx == lastArc)
                {
                    flags += FST.BIT_LAST_ARC;
                }

                if (builder.lastFrozenNode == target.Node && !doFixedArray)
                {
                    // TODO: for better perf (but more RAM used) we
                    // could avoid this except when arc is "near" the
                    // last arc:
                    flags += FST.BIT_TARGET_NEXT;
                }

                if (arc.IsFinal)
                {
                    flags += FST.BIT_FINAL_ARC;
                    if (arc.NextFinalOutput != NO_OUTPUT)
                    {
                        flags += FST.BIT_ARC_HAS_FINAL_OUTPUT;
                    }
                }
                else if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(arc.NextFinalOutput == NO_OUTPUT);
                }

                bool targetHasArcs = target.Node > 0;

                if (!targetHasArcs)
                {
                    flags += FST.BIT_STOP_NODE;
                }

                if (arc.Output != NO_OUTPUT)
                {
                    flags += FST.BIT_ARC_HAS_OUTPUT;
                }

                builder.bytes.WriteByte((byte)flags);
                WriteLabel(builder.bytes, arc.Label);

                // System.out.println("  write arc: label=" + (char) arc.Label + " flags=" + flags + " target=" + target.Node + " pos=" + bytes.getPosition() + " output=" + outputs.outputToString(arc.Output));

                if (arc.Output != NO_OUTPUT)
                {
                    Outputs.Write(arc.Output, builder.bytes);
                    //System.out.println("    write output");
                }

                if (arc.NextFinalOutput != NO_OUTPUT)
                {
                    //System.out.println("    write final output");
                    Outputs.WriteFinalOutput(arc.NextFinalOutput, builder.bytes);
                }

                if (targetHasArcs && (flags & FST.BIT_TARGET_NEXT) == 0)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(target.Node > 0);
                    //System.out.println("    write target");
                    builder.bytes.WriteVInt64(target.Node);
                }

                // just write the arcs "like normal" on first pass,
                // but record how many bytes each one took, and max
                // byte size:
                if (doFixedArray)
                {
                    builder.reusedBytesPerArc[arcIdx] = (int)(builder.bytes.Position - lastArcStart);
                    lastArcStart = builder.bytes.Position;
                    maxBytesPerArc = Math.Max(maxBytesPerArc, builder.reusedBytesPerArc[arcIdx]);
                    //System.out.println("    bytes=" + builder.reusedBytesPerArc[arcIdx]);
                }
            }

            // TODO: try to avoid wasteful cases: disable doFixedArray in that case
            /*
             *
             * LUCENE-4682: what is a fair heuristic here?
             * It could involve some of these:
             * 1. how "busy" the node is: nodeIn.inputCount relative to frontier[0].inputCount?
             * 2. how much binSearch saves over scan: nodeIn.numArcs
             * 3. waste: numBytes vs numBytesExpanded
             *
             * the one below just looks at #3
            if (doFixedArray) {
              // rough heuristic: make this 1.25 "waste factor" a parameter to the phd ctor????
              int numBytes = lastArcStart - startAddress;
              int numBytesExpanded = maxBytesPerArc * nodeIn.numArcs;
              if (numBytesExpanded > numBytes*1.25) {
                doFixedArray = false;
              }
            }
            */

            if (doFixedArray)
            {
                const int MAX_HEADER_SIZE = 11; // header(byte) + numArcs(vint) + numBytes(vint)
                if (Debugging.AssertsEnabled) Debugging.Assert(maxBytesPerArc > 0);
                // 2nd pass just "expands" all arcs to take up a fixed byte size

                // If more than (1 / DIRECT_ARC_LOAD_FACTOR) of the "slots" would be occupied, write an arc
                // array that may have holes in it so that we can address the arcs directly by label without
                // binary search
                int labelRange = nodeIn.Arcs[nodeIn.NumArcs - 1].Label - nodeIn.Arcs[0].Label + 1;
                bool writeDirectly = labelRange > 0 && labelRange < Builder.DIRECT_ARC_LOAD_FACTOR * nodeIn.NumArcs;
                // TODO: LUCENE-8920 tighten up RAM usage. Until then, direct
                // arc encoding is disabled due to excessive memory usage found
                // in some cases
                writeDirectly = false;

                //System.out.println("write int @pos=" + (fixedArrayStart-4) + " numArcs=" + nodeIn.numArcs);
                // create the header
                // TODO: clean this up: or just rewind+reuse and deal with it
                byte[] header = new byte[MAX_HEADER_SIZE];
                var bad = new ByteArrayDataOutput(header);
                // write a "false" first arc:
                if (writeDirectly)
                {
                    bad.WriteByte(FST.ARCS_AS_ARRAY_WITH_GAPS);
                    bad.WriteVInt32(labelRange);
                }
                else
                {
                    bad.WriteByte(FST.ARCS_AS_ARRAY_PACKED);
                    bad.WriteVInt32(nodeIn.NumArcs);
                }
                bad.WriteVInt32(maxBytesPerArc);
                int headerLen = bad.Position;

                long fixedArrayStart = startAddress + headerLen;

                if (writeDirectly)
                {
                    WriteArrayWithGaps(builder, nodeIn, fixedArrayStart, maxBytesPerArc, labelRange);
                }
                else
                {
                    WriteArrayPacked(builder, nodeIn, fixedArrayStart, maxBytesPerArc);
                }

                // now write the header
                builder.bytes.WriteBytes(startAddress, header, 0, headerLen);
            }

            long thisNodeAddress = builder.bytes.Position - 1;

            builder.bytes.Reverse(startAddress, thisNodeAddress);

            builder.nodeCount++;
            return thisNodeAddress;
        }

        private static void WriteArrayPacked(Builder<T> builder, Builder.UnCompiledNode<T> nodeIn, long fixedArrayStart, int maxBytesPerArc)
        {
            // expand the arcs in place, backwards
            long srcPos = builder.bytes.Position;
            long destPos = fixedArrayStart + nodeIn.NumArcs * maxBytesPerArc;
            if (Debugging.AssertsEnabled) Debugging.Assert(destPos >= srcPos);
            if (destPos > srcPos)
            {
                builder.bytes.SkipBytes((int)(destPos - srcPos));
                for (int arcIdx = nodeIn.NumArcs - 1; arcIdx >= 0; arcIdx--)
                {
                    destPos -= maxBytesPerArc;
                    srcPos -= builder.reusedBytesPerArc[arcIdx];
                    //System.out.println("  repack arcIdx=" + arcIdx + " srcPos=" + srcPos + " destPos=" + destPos);
                    if (srcPos != destPos)
                    {
                        //System.out.println("  copy len=" + bytesPerArc[arcIdx]);
                        if (Debugging.AssertsEnabled) Debugging.Assert(destPos > srcPos, "destPos={0} srcPos={1} arcIdx={2} maxBytesPerArc={3} reusedBytesPerArc[arcIdx]={4} nodeIn.numArcs={5}", destPos, srcPos, arcIdx, maxBytesPerArc, builder.reusedBytesPerArc[arcIdx], nodeIn.NumArcs);
                        builder.bytes.CopyBytes(srcPos, destPos, builder.reusedBytesPerArc[arcIdx]);
                    }
                }
            }
        }

        private static void WriteArrayWithGaps(Builder<T> builder, Builder.UnCompiledNode<T> nodeIn, long fixedArrayStart, int maxBytesPerArc, int labelRange)
        {
            // expand the arcs in place, backwards
            long srcPos = builder.bytes.Position;
            long destPos = fixedArrayStart + labelRange * maxBytesPerArc;
            // if destPos == srcPos it means all the arcs were the same length, and the array of them is *already* direct
            if (Debugging.AssertsEnabled) Debugging.Assert(destPos >= srcPos);
            if (destPos > srcPos)
            {
                builder.bytes.SkipBytes((int)(destPos - srcPos));
                int arcIdx = nodeIn.NumArcs - 1;
                int firstLabel = nodeIn.Arcs[0].Label;
                int nextLabel = nodeIn.Arcs[arcIdx].Label;
                for (int directArcIdx = labelRange - 1; directArcIdx >= 0; directArcIdx--)
                {
                    destPos -= maxBytesPerArc;
                    if (directArcIdx == nextLabel - firstLabel)
                    {
                        int arcLen = builder.reusedBytesPerArc[arcIdx];
                        srcPos -= arcLen;
                        //System.out.println("  direct pack idx=" + directArcIdx + " arcIdx=" + arcIdx + " srcPos=" + srcPos + " destPos=" + destPos + " label=" + nextLabel);
                        if (srcPos != destPos)
                        {
                            //System.out.println("  copy len=" + builder.reusedBytesPerArc[arcIdx]);
                            if (Debugging.AssertsEnabled) Debugging.Assert(destPos > srcPos, "destPos={0} srcPos={1} arcIdx={2} maxBytesPerArc={3} reusedBytesPerArc[arcIdx]={4} nodeIn.numArcs={5}", destPos, srcPos, arcIdx, maxBytesPerArc, builder.reusedBytesPerArc[arcIdx], nodeIn.NumArcs);
                            builder.bytes.CopyBytes(srcPos, destPos, arcLen);
                            if (arcIdx == 0)
                            {
                                break;
                            }
                        }
                        --arcIdx;
                        nextLabel = nodeIn.Arcs[arcIdx].Label;
                    }
                    else
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(directArcIdx > arcIdx);
                        // mark this as a missing arc
                        //System.out.println("  direct pack idx=" + directArcIdx + " no arc");
                        builder.bytes.WriteByte(destPos, FST.BIT_MISSING_ARC);
                    }
                }
            }
        }

        /// <summary>
        /// Fills virtual 'start' arc, ie, an empty incoming arc to
        /// the FST's start node
        /// </summary>
        public FST.Arc<T> GetFirstArc(FST.Arc<T> arc)
        {
            T NO_OUTPUT = Outputs.NoOutput;

            if (null != emptyOutput) // LUCENENET: intentionally putting null on the left to avoid custom equality overrides
            {
                arc.Flags = FST.BIT_FINAL_ARC | FST.BIT_LAST_ARC;
                arc.NextFinalOutput = emptyOutput;
                if (emptyOutput != NO_OUTPUT)
                {
                    arc.Flags |= FST.BIT_ARC_HAS_FINAL_OUTPUT;
                }
            }
            else
            {
                arc.Flags = FST.BIT_LAST_ARC;
                arc.NextFinalOutput = NO_OUTPUT;
            }
            arc.Output = NO_OUTPUT;

            // If there are no nodes, ie, the FST only accepts the
            // empty string, then startNode is 0
            arc.Target = startNode;
            return arc;
        }

        /// <summary>
        /// Follows the <paramref name="follow"/> arc and reads the last
        /// arc of its target; this changes the provided
        /// <paramref name="arc"/> (2nd arg) in-place and returns it.
        /// </summary>
        /// <returns> Returns the second argument
        /// (<paramref name="arc"/>).  </returns>
        public FST.Arc<T> ReadLastTargetArc(FST.Arc<T> follow, FST.Arc<T> arc, FST.BytesReader @in)
        {
            //System.out.println("readLast");
            if (!FST.TargetHasArcs(follow))
            {
                //System.out.println("  end node");
                if (Debugging.AssertsEnabled) Debugging.Assert(follow.IsFinal);
                arc.Label = FST.END_LABEL;
                arc.Target = FST.FINAL_END_NODE;
                arc.Output = follow.NextFinalOutput;
                arc.Flags = (sbyte)FST.BIT_LAST_ARC;
                return arc;
            }
            else
            {
                @in.Position = follow.Target;
                var b = (sbyte)@in.ReadByte();
                if (b == FST.ARCS_AS_ARRAY_PACKED || b == FST.ARCS_AS_ARRAY_WITH_GAPS)
                {
                    // array: jump straight to end
                    arc.NumArcs = @in.ReadVInt32();
                    arc.BytesPerArc = @in.ReadVInt32();
                    //System.out.println("  array numArcs=" + arc.numArcs + " bpa=" + arc.bytesPerArc);
                    arc.PosArcsStart = @in.Position;
                    if (b == FST.ARCS_AS_ARRAY_WITH_GAPS)
                    {
                        arc.ArcIdx = int.MinValue;
                        arc.NextArc = arc.PosArcsStart - (arc.NumArcs - 1) * arc.BytesPerArc;
                    }
                    else
                    {
                        arc.ArcIdx = arc.NumArcs - 2;
                    }
                }
                else
                {
                    arc.Flags = b;
                    // non-array: linear scan
                    arc.BytesPerArc = 0;
                    //System.out.println("  scan");
                    while (!arc.IsLast)
                    {
                        // skip this arc:
                        ReadLabel(@in);
                        if (arc.Flag(FST.BIT_ARC_HAS_OUTPUT))
                        {
                            Outputs.SkipOutput(@in);
                        }
                        if (arc.Flag(FST.BIT_ARC_HAS_FINAL_OUTPUT))
                        {
                            Outputs.SkipFinalOutput(@in);
                        }
                        if (arc.Flag(FST.BIT_STOP_NODE))
                        {
                        }
                        else if (arc.Flag(FST.BIT_TARGET_NEXT))
                        {
                        }
                        else
                        {
                            ReadUnpackedNodeTarget(@in);
                        }
                        arc.Flags = (sbyte)@in.ReadByte();
                    }
                    // Undo the byte flags we read:
                    @in.SkipBytes(-1);
                    arc.NextArc = @in.Position;
                }
                ReadNextRealArc(arc, @in);
                if (Debugging.AssertsEnabled) Debugging.Assert(arc.IsLast);
                return arc;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long ReadUnpackedNodeTarget(FST.BytesReader @in)
        {
            return @in.ReadVInt64();
        }

        /// <summary>
        /// Follow the <paramref name="follow"/> arc and read the first arc of its target;
        /// this changes the provided <paramref name="arc"/> (2nd arg) in-place and returns
        /// it.
        /// </summary>
        /// <returns> Returns the second argument (<paramref name="arc"/>). </returns>
        public FST.Arc<T> ReadFirstTargetArc(FST.Arc<T> follow, FST.Arc<T> arc, FST.BytesReader @in)
        {
            //int pos = address;
            //Debug.WriteLine("    readFirstTarget follow.target=" + follow.Target + " isFinal=" + follow.IsFinal);
            if (follow.IsFinal)
            {
                // Insert "fake" final first arc:
                arc.Label = FST.END_LABEL;
                arc.Output = follow.NextFinalOutput;
                arc.Flags = (sbyte)FST.BIT_FINAL_ARC;
                if (follow.Target <= 0)
                {
                    arc.Flags |= (sbyte)FST.BIT_LAST_ARC;
                }
                else
                {
                    // NOTE: nextArc is a node (not an address!) in this case:
                    arc.NextArc = follow.Target;
                }
                arc.Target = FST.FINAL_END_NODE;
                //Debug.WriteLine("    insert isFinal; nextArc=" + follow.Target + " isLast=" + arc.IsLast + " output=" + Outputs.OutputToString(arc.Output));
                return arc;
            }
            else
            {
                return ReadFirstRealTargetArc(follow.Target, arc, @in);
            }
        }

        public FST.Arc<T> ReadFirstRealTargetArc(long node, FST.Arc<T> arc, FST.BytesReader @in)
        {
            long address = node;
            @in.Position = address;
            //System.out.println("   flags=" + arc.flags);

            var flags = (sbyte)@in.ReadByte();
            if (flags == FST.ARCS_AS_ARRAY_PACKED || flags == FST.ARCS_AS_ARRAY_WITH_GAPS)
            {
                //System.out.println("  fixedArray");
                // this is first arc in a fixed-array
                arc.NumArcs = @in.ReadVInt32();
                arc.BytesPerArc = @in.ReadVInt32();
                if (flags == FST.ARCS_AS_ARRAY_PACKED)
                {
                    arc.ArcIdx = -1;
                }
                else
                {
                    arc.ArcIdx = int.MinValue;
                }
                arc.NextArc = arc.PosArcsStart = @in.Position;
                //System.out.println("  bytesPer=" + arc.bytesPerArc + " numArcs=" + arc.numArcs + " arcsStart=" + pos);
            }
            else
            {
                //arc.flags = b;
                arc.NextArc = address;
                arc.BytesPerArc = 0;
            }

            return ReadNextRealArc(arc, @in);
        }

        /// <summary>
        /// Checks if arc's target state is in expanded (or vector) format.
        /// </summary>
        /// <returns> Returns <c>true</c> if arc points to a state in an
        /// expanded array format. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsExpandedTarget(FST.Arc<T> follow, FST.BytesReader @in)
        {
            if (!FST.TargetHasArcs(follow))
            {
                return false;
            }
            else
            {
                @in.Position = follow.Target;
                var flags = @in.ReadByte();
                return flags == FST.ARCS_AS_ARRAY_PACKED || flags == FST.ARCS_AS_ARRAY_WITH_GAPS;
            }
        }

        /// <summary>
        /// In-place read; returns the arc. </summary>
        public FST.Arc<T> ReadNextArc(FST.Arc<T> arc, FST.BytesReader @in)
        {
            if (arc.Label == FST.END_LABEL)
            {
                // this was a fake inserted "final" arc
                if (arc.NextArc <= 0)
                {
                    throw new ArgumentException("cannot readNextArc when arc.IsLast=true");
                }
                return ReadFirstRealTargetArc(arc.NextArc, arc, @in);
            }
            else
            {
                return ReadNextRealArc(arc, @in);
            }
        }

        /// <summary>
        /// Peeks at next arc's label; does not alter <paramref name="arc"/>.  Do
        /// not call this if arc.IsLast!
        /// </summary>
        public int ReadNextArcLabel(FST.Arc<T> arc, FST.BytesReader @in)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(!arc.IsLast);

            if (arc.Label == FST.END_LABEL)
            {
                //Debug.WriteLine("    nextArc fake " + arc.NextArc);

                long pos = arc.NextArc;
                @in.Position = pos;

                var flags = (sbyte)@in.ReadByte();
                if (flags == FST.ARCS_AS_ARRAY_PACKED || flags == FST.ARCS_AS_ARRAY_WITH_GAPS)
                {
                    //System.out.println("    nextArc fixed array");
                    @in.ReadVInt32();

                    // Skip bytesPerArc:
                    @in.ReadVInt32();
                }
                else
                {
                    @in.Position = pos;
                }
                // skip flags
                @in.ReadByte();
            }
            else
            {
                if (arc.BytesPerArc != 0)
                {
                    //System.out.println("    nextArc real array");
                    // arcs are in an array
                    if (arc.ArcIdx >= 0)
                    {
                        @in.Position = arc.PosArcsStart;
                        // point at next arc, -1 to skip flags
                        @in.SkipBytes((1 + arc.ArcIdx) * arc.BytesPerArc + 1);
                    }
                    else
                    {
                        @in.Position = arc.NextArc;
                        byte flags = @in.ReadByte();
                        // skip missing arcs
                        while (Flag(flags, FST.BIT_MISSING_ARC))
                        {
                            @in.SkipBytes(arc.BytesPerArc - 1);
                            flags = @in.ReadByte();
                        }
                    }
                }
                else
                {
                    // arcs are packed
                    //System.out.println("    nextArc real packed");
                    // -1 to skip flags
                    @in.Position = arc.NextArc - 1;
                }
            }
            return ReadLabel(@in);
        }

        /// <summary>
        /// Never returns <c>null</c>, but you should never call this if
        /// arc.IsLast is <c>true</c>.
        /// </summary>
        public FST.Arc<T> ReadNextRealArc(FST.Arc<T> arc, FST.BytesReader @in)
        {
            // TODO: can't assert this because we call from readFirstArc
            // assert !flag(arc.flags, BIT_LAST_ARC);

            // this is a continuing arc in a fixed array
            if (arc.BytesPerArc != 0)
            {
                // arcs are in an array
                if (arc.ArcIdx > int.MinValue)
                {
                    arc.ArcIdx++;
                    if (Debugging.AssertsEnabled) Debugging.Assert(arc.ArcIdx < arc.NumArcs);
                    @in.Position = arc.PosArcsStart - arc.ArcIdx * arc.BytesPerArc;
                    arc.Flags = (sbyte)@in.ReadByte();
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(arc.NextArc <= arc.PosArcsStart && arc.NextArc > arc.PosArcsStart - arc.NumArcs * arc.BytesPerArc);
                    @in.Position = arc.NextArc;
                    arc.Flags = (sbyte)@in.ReadByte();
                    while (Flag(arc.Flags, FST.BIT_MISSING_ARC))
                    {
                        // skip empty arcs
                        arc.NextArc -= arc.BytesPerArc;
                        @in.SkipBytes(arc.BytesPerArc - 1);
                        arc.Flags = (sbyte)@in.ReadByte();
                    }
                }
            }
            else
            {
                // arcs are packed
                @in.Position = arc.NextArc;
                arc.Flags = (sbyte)@in.ReadByte();
            }
            arc.Label = ReadLabel(@in);

            if (arc.Flag(FST.BIT_ARC_HAS_OUTPUT))
            {
                arc.Output = Outputs.Read(@in);
            }
            else
            {
                arc.Output = Outputs.NoOutput;
            }

            if (arc.Flag(FST.BIT_ARC_HAS_FINAL_OUTPUT))
            {
                arc.NextFinalOutput = Outputs.ReadFinalOutput(@in);
            }
            else
            {
                arc.NextFinalOutput = Outputs.NoOutput;
            }

            if (arc.Flag(FST.BIT_STOP_NODE))
            {
                if (arc.Flag(FST.BIT_FINAL_ARC))
                {
                    arc.Target = FST.FINAL_END_NODE;
                }
                else
                {
                    arc.Target = FST.NON_FINAL_END_NODE;
                }
                if (arc.BytesPerArc == 0)
                {
                    arc.NextArc = @in.Position;
                }
                else
                {
                    arc.NextArc -= arc.BytesPerArc;
                }
            }
            else if (arc.Flag(FST.BIT_TARGET_NEXT))
            {
                arc.NextArc = @in.Position;
                // TODO: would be nice to make this lazy -- maybe
                // caller doesn't need the target and is scanning arcs...
                if (!arc.Flag(FST.BIT_LAST_ARC))
                {
                    if (arc.BytesPerArc == 0)
                    {
                        // must scan
                        SeekToNextNode(@in);
                    }
                    else
                    {
                        @in.Position = arc.PosArcsStart;
                        @in.SkipBytes(arc.BytesPerArc * arc.NumArcs);
                    }
                }
                arc.Target = @in.Position;
            }
            else
            {
                arc.Target = ReadUnpackedNodeTarget(@in);
                if (arc.BytesPerArc > 0 && arc.ArcIdx == int.MinValue)
                {
                    // nextArc was pointing to *this* arc when we entered; advance to the next
                    // if it is a missing arc, we will skip it later
                    arc.NextArc -= arc.BytesPerArc;
                }
                else
                {
                    // in list and fixed table encodings, the next arc always follows this one
                    arc.NextArc = @in.Position;
                }
            }
            return arc;
        }

        // LUCENE-5152: called only from asserts, to validate that the
        // non-cached arc lookup would produce the same result, to
        // catch callers that illegally modify shared structures with
        // the result (we shallow-clone the Arc itself, but e.g. a BytesRef
        // output is still shared):
        private bool AssertRootCachedArc(int label, FST.Arc<T> cachedArc)
        {
            if (!Debugging.AssertsEnabled) return true;
            FST.Arc<T> arc = new FST.Arc<T>();
            GetFirstArc(arc);
            FST.BytesReader @in = GetBytesReader();
            FST.Arc<T> result = FindTargetArc(label, arc, arc, @in, false);
            if (result == null)
            {
                Debugging.Assert(cachedArc == null);
            }
            else
            {
                Debugging.Assert(cachedArc != null);
                Debugging.Assert(cachedArc.ArcIdx == result.ArcIdx);
                Debugging.Assert(cachedArc.BytesPerArc == result.BytesPerArc);
                Debugging.Assert(cachedArc.Flags == result.Flags);
                Debugging.Assert(cachedArc.Label == result.Label);
                if (cachedArc.BytesPerArc == 0 || cachedArc.ArcIdx == int.MinValue)
                {
                    // in the sparse array case, this value is not valid, so don't assert it
                    Debugging.Assert(cachedArc.NextArc == result.NextArc);
                }
                // LUCENENET NOTE: In .NET, IEnumerable will not equal another identical IEnumerable
                // because it checks for reference equality, not that the list contents
                // are the same. StructuralEqualityComparer.Default.Equals() will make that check.
                Debugging.Assert(typeof(T).IsValueType
                    ? JCG.EqualityComparer<T>.Default.Equals(cachedArc.NextFinalOutput, result.NextFinalOutput)
                    : StructuralEqualityComparer.Default.Equals(cachedArc.NextFinalOutput, result.NextFinalOutput));
                Debugging.Assert(cachedArc.NumArcs == result.NumArcs);
                Debugging.Assert(typeof(T).IsValueType
                        ? JCG.EqualityComparer<T>.Default.Equals(cachedArc.Output, result.Output)
                        : StructuralEqualityComparer.Default.Equals(cachedArc.Output, result.Output));
                Debugging.Assert(cachedArc.PosArcsStart == result.PosArcsStart);
                Debugging.Assert(cachedArc.Target == result.Target);
            }

            return true;
        }

        // TODO: could we somehow [partially] tableize arc lookups
        // look automaton?

        /// <summary>
        /// Finds an arc leaving the incoming <paramref name="arc"/>, replacing the arc in place.
        /// this returns <c>null</c> if the arc was not found, else the incoming <paramref name="arc"/>.
        /// </summary>
        public FST.Arc<T> FindTargetArc(int labelToMatch, FST.Arc<T> follow, FST.Arc<T> arc, FST.BytesReader @in)
        {
            return FindTargetArc(labelToMatch, follow, arc, @in, true);
        }

        /// <summary>
        /// Finds an arc leaving the incoming <paramref name="arc"/>, replacing the arc in place.
        /// this returns <c>null</c> if the arc was not found, else the incoming <paramref name="arc"/>.
        /// </summary>
        private FST.Arc<T> FindTargetArc(int labelToMatch, FST.Arc<T> follow, FST.Arc<T> arc, FST.BytesReader @in, bool useRootArcCache)
        {
            if (labelToMatch == FST.END_LABEL)
            {
                if (follow.IsFinal)
                {
                    if (follow.Target <= 0)
                    {
                        arc.Flags = (sbyte)FST.BIT_LAST_ARC;
                    }
                    else
                    {
                        arc.Flags = 0;
                        // NOTE: nextArc is a node (not an address!) in this case:
                        arc.NextArc = follow.Target;
                    }
                    arc.Output = follow.NextFinalOutput;
                    arc.Label = FST.END_LABEL;
                    return arc;
                }
                else
                {
                    return null;
                }
            }

            // Short-circuit if this arc is in the root arc cache:
            if (useRootArcCache && cachedRootArcs != null && follow.Target == startNode && labelToMatch < cachedRootArcs.Length)
            {
                FST.Arc<T> result = cachedRootArcs[labelToMatch];

                // LUCENE-5152: detect tricky cases where caller
                // modified previously returned cached root-arcs:
                if (Debugging.AssertsEnabled) Debugging.Assert(AssertRootCachedArc(labelToMatch, result));
                if (result is null)
                {
                    return null;
                }
                else
                {
                    arc.CopyFrom(result);
                    return arc;
                }
            }

            if (!FST.TargetHasArcs(follow))
            {
                return null;
            }

            @in.Position = follow.Target;

            // System.out.println("fta label=" + (char) labelToMatch);
            sbyte flags = (sbyte)@in.ReadByte();
            if (flags == FST.ARCS_AS_ARRAY_WITH_GAPS)
            {
                // Arcs are full array; do binary search:
                arc.NumArcs = @in.ReadVInt32();
                arc.BytesPerArc = @in.ReadVInt32();
                arc.PosArcsStart = @in.Position;

                // Array is direct; address by label
                @in.SkipBytes(1);
                int firstLabel = ReadLabel(@in);
                int arcPos = labelToMatch - firstLabel;
                if (arcPos == 0)
                {
                    arc.NextArc = arc.PosArcsStart;
                }
                else if (arcPos > 0)
                {
                    if (arcPos >= arc.NumArcs)
                    {
                        return null;
                    }
                    @in.Position = arc.PosArcsStart - arc.BytesPerArc * arcPos;
                    flags = (sbyte)@in.ReadByte();
                    if (Flag(flags, FST.BIT_MISSING_ARC))
                    {
                        return null;
                    }
                    // point to flags that we just read
                    arc.NextArc = @in.Position + 1;
                }
                else
                {
                    return null;
                }
                arc.ArcIdx = int.MinValue;
                return ReadNextRealArc(arc, @in);
            }
            else if (flags == FST.ARCS_AS_ARRAY_PACKED)
            {
                arc.NumArcs = @in.ReadVInt32();
                arc.BytesPerArc = @in.ReadVInt32();
                arc.PosArcsStart = @in.Position;

                // Array is sparse; do binary search:
                int low = 0;
                int high = arc.NumArcs - 1;
                while (low <= high)
                {
                    //System.out.println("    cycle");
                    int mid = (low + high).TripleShift(1);
                    // +1 to skip over flags
                    @in.Position = arc.PosArcsStart - (arc.BytesPerArc * mid + 1);
                    int midLabel = ReadLabel(@in);
                    int cmp = midLabel - labelToMatch;
                    if (cmp < 0)
                    {
                        low = mid + 1;
                    }
                    else if (cmp > 0)
                    {
                        high = mid - 1;
                    }
                    else
                    {
                        arc.ArcIdx = mid - 1;
                        //System.out.println("    found!");
                        return ReadNextRealArc(arc, @in);
                    }
                }
                return null;
            }

            // Linear scan
            ReadFirstRealTargetArc(follow.Target, arc, @in);

            while (true)
            {
                //System.out.println("  non-bs cycle");
                // TODO: we should fix this code to not have to create
                // object for the output of every arc we scan... only
                // for the matching arc, if found
                if (arc.Label == labelToMatch)
                {
                    //System.out.println("    found!");
                    return arc;
                }
                else if (arc.Label > labelToMatch)
                {
                    return null;
                }
                else if (arc.IsLast)
                {
                    return null;
                }
                else
                {
                    ReadNextRealArc(arc, @in);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SeekToNextNode(FST.BytesReader @in)
        {
            while (true)
            {
                int flags = @in.ReadByte();
                ReadLabel(@in);

                if (Flag(flags, FST.BIT_ARC_HAS_OUTPUT))
                {
                    Outputs.SkipOutput(@in);
                }

                if (Flag(flags, FST.BIT_ARC_HAS_FINAL_OUTPUT))
                {
                    Outputs.SkipFinalOutput(@in);
                }

                if (!Flag(flags, FST.BIT_STOP_NODE) && !Flag(flags, FST.BIT_TARGET_NEXT))
                {
                    ReadUnpackedNodeTarget(@in);
                }

                if (Flag(flags, FST.BIT_LAST_ARC))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Nodes will be expanded if their depth (distance from the root node) is
        /// &lt;= this value and their number of arcs is &gt;=
        /// <see cref="FST.FIXED_ARRAY_NUM_ARCS_SHALLOW"/>.
        ///
        /// <para/>
        /// Fixed array consumes more RAM but enables binary search on the arcs
        /// (instead of a linear scan) on lookup by arc label.
        /// </summary>
        /// <returns> <c>true</c> if <paramref name="node"/> should be stored in an
        ///         expanded (array) form.
        /// </returns>
        /// <seealso cref="FST.FIXED_ARRAY_NUM_ARCS_DEEP"/>
        /// <seealso cref="Builder.UnCompiledNode{S}.Depth"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldExpand(Builder<T> builder, Builder.UnCompiledNode<T> node)
        {
            return builder.allowArrayArcs &&
                ((node.Depth <= FST.FIXED_ARRAY_SHALLOW_DISTANCE && node.NumArcs >= FST.FIXED_ARRAY_NUM_ARCS_SHALLOW) ||
                node.NumArcs >= FST.FIXED_ARRAY_NUM_ARCS_DEEP);
        }

        /// <summary>
        /// Returns a <see cref="FST.BytesReader"/> for this FST, positioned at
        /// position 0.
        /// </summary>
        public FST.BytesReader GetBytesReader()
        {
            if (this.fstStore != null)
            {
                return this.fstStore.GetReverseBytesReader();
            }
            else
            {
                return bytes.GetReverseReader();
            }
        }

        // LUCENENET specific - BytesReader moved to FST class


        /*
        public void countSingleChains() throws IOException {
          // TODO: must assert this FST was built with
          // "willRewrite"

          final List<ArcAndState<T>> queue = new ArrayList<>();

          // TODO: use bitset to not revisit nodes already
          // visited

          FixedBitSet seen = new FixedBitSet(1+nodeCount);
          int saved = 0;

          queue.add(new ArcAndState<T>(getFirstArc(new Arc<T>()), new IntsRef()));
          Arc<T> scratchArc = new Arc<>();
          while(queue.size() > 0) {
            //System.out.println("cycle size=" + queue.size());
            //for(ArcAndState<T> ent : queue) {
            //  System.out.println("  " + Util.toBytesRef(ent.chain, new BytesRef()));
            //  }
            final ArcAndState<T> arcAndState = queue.get(queue.size()-1);
            seen.set(arcAndState.arc.Node);
            final BytesRef br = Util.toBytesRef(arcAndState.chain, new BytesRef());
            if (br.length > 0 && br.bytes[br.length-1] == -1) {
              br.length--;
            }
            //System.out.println("  top node=" + arcAndState.arc.Target + " chain=" + br.utf8ToString());
            if (targetHasArcs(arcAndState.arc) && !seen.get(arcAndState.arc.target)) {
              // push
              readFirstTargetArc(arcAndState.arc, scratchArc);
              //System.out.println("  push label=" + (char) scratchArc.Label);
              //System.out.println("    tonode=" + scratchArc.Target + " last?=" + scratchArc.isLast());

              final IntsRef chain = IntsRef.deepCopyOf(arcAndState.chain);
              chain.grow(1+chain.length);
              // TODO
              //assert scratchArc.Label != END_LABEL;
              chain.ints[chain.length] = scratchArc.Label;
              chain.length++;

              if (scratchArc.isLast()) {
                if (scratchArc.Target != -1 && inCounts[scratchArc.target] == 1) {
                  //System.out.println("    append");
                } else {
                  if (arcAndState.chain.length > 1) {
                    saved += chain.length-2;
                    try {
                      System.out.println("chain: " + Util.toBytesRef(chain, new BytesRef()).utf8ToString());
                    } catch (AssertionError ae) {
                      System.out.println("chain: " + Util.toBytesRef(chain, new BytesRef()));
                    }
                  }
                  chain.length = 0;
                }
              } else {
                //System.out.println("    reset");
                if (arcAndState.chain.length > 1) {
                  saved += arcAndState.chain.length-2;
                  try {
                    System.out.println("chain: " + Util.toBytesRef(arcAndState.chain, new BytesRef()).utf8ToString());
                  } catch (AssertionError ae) {
                    System.out.println("chain: " + Util.toBytesRef(arcAndState.chain, new BytesRef()));
                  }
                }
                if (scratchArc.Target != -1 && inCounts[scratchArc.target] != 1) {
                  chain.length = 0;
                } else {
                  chain.ints[0] = scratchArc.Label;
                  chain.length = 1;
                }
              }
              // TODO: instead of new Arc() we can re-use from
              // a by-depth array
              queue.add(new ArcAndState<T>(new Arc<T>().copyFrom(scratchArc), chain));
            } else if (!arcAndState.arc.isLast()) {
              // next
              readNextArc(arcAndState.arc);
              //System.out.println("  next label=" + (char) arcAndState.arc.Label + " len=" + arcAndState.chain.length);
              if (arcAndState.chain.length != 0) {
                arcAndState.chain.ints[arcAndState.chain.length-1] = arcAndState.arc.Label;
              }
            } else {
              if (arcAndState.chain.length > 1) {
                saved += arcAndState.chain.length-2;
                System.out.println("chain: " + Util.toBytesRef(arcAndState.chain, new BytesRef()).utf8ToString());
              }
              // pop
              //System.out.println("  pop");
              queue.remove(queue.size()-1);
              while(queue.size() > 0 && queue.get(queue.size()-1).arc.isLast()) {
                queue.remove(queue.size()-1);
              }
              if (queue.size() > 0) {
                final ArcAndState<T> arcAndState2 = queue.get(queue.size()-1);
                readNextArc(arcAndState2.arc);
                //System.out.println("  read next=" + (char) arcAndState2.arc.Label + " queue=" + queue.size());
                assert arcAndState2.arc.Label != END_LABEL;
                if (arcAndState2.chain.length != 0) {
                  arcAndState2.chain.ints[arcAndState2.chain.length-1] = arcAndState2.arc.Label;
                }
              }
            }
          }

          System.out.println("TOT saved " + saved);
        }
       */

    }

    /// <summary>
    /// LUCENENET specific: This new base class is to mimic Java's ability to use nested types without specifying
    /// a type parameter. i.e. FST.BytesReader instead of FST&lt;BytesRef&gt;.BytesReader
    /// </summary>
    public static class FST
    {
        public static readonly int DEFAULT_MAX_BLOCK_BITS = Constants.RUNTIME_IS_64BIT ? 30 : 28;

        internal const int BIT_FINAL_ARC = 1 << 0;
        internal const int BIT_LAST_ARC = 1 << 1;
        internal const int BIT_TARGET_NEXT = 1 << 2;

        // TODO: we can free up a bit if we can nuke this:
        internal const int BIT_STOP_NODE = 1 << 3;

        /// <summary>This flag is set if the arc has an output.</summary>
        public const int BIT_ARC_HAS_OUTPUT = 1 << 4;

        internal const int BIT_ARC_HAS_FINAL_OUTPUT = 1 << 5;

        // We use this as a marker (because this one flag is
        // illegal by itself ...):
        internal const byte ARCS_AS_ARRAY_PACKED = BIT_ARC_HAS_FINAL_OUTPUT;

        // this means either of these things in different contexts
        // in the midst of a direct array:
        internal const byte BIT_MISSING_ARC = 1 << 6;
        // at the start of a direct array:
        internal const byte ARCS_AS_ARRAY_WITH_GAPS = BIT_MISSING_ARC;

        /// <summary>
        /// <see cref="Builder.UnCompiledNode{S}"/>
        /// </summary>
        public const int FIXED_ARRAY_SHALLOW_DISTANCE = 3;

        /// <summary>
        /// <see cref="Builder.UnCompiledNode{S}"/>
        /// </summary>
        public const int FIXED_ARRAY_NUM_ARCS_SHALLOW = 5;

        /// <summary>
        /// <see cref="Builder.UnCompiledNode{S}"/>
        /// </summary>
        public const int FIXED_ARRAY_NUM_ARCS_DEEP = 10;

        // Increment version to change it
        internal const string FILE_FORMAT_NAME = "FST";
        internal const int VERSION_START = 6;

        internal const int VERSION_CURRENT = 6;

        /// <summary>
        /// Never serialized; just used to represent the virtual
        /// final node w/ no arcs:
        /// </summary>
        internal const long FINAL_END_NODE = -1;

        /// <summary>
        /// Never serialized; just used to represent the virtual
        /// non-final node w/ no arcs:
        /// </summary>
        internal const long NON_FINAL_END_NODE = 0;

        /// <summary>
        /// If arc has this label then that arc is final/accepted </summary>
        public const int END_LABEL = -1;

        /// <summary>
        /// returns <c>true</c> if the node at this address has any
        /// outgoing arcs
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TargetHasArcs<T>(Arc<T> arc) where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            return arc.Target > 0;
        }

        /// <summary>
        /// Reads an automaton from a file.
        /// </summary>
        public static FST<T> Read<T>(FileInfo file, Outputs<T> outputs) where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            using var @is = file.OpenRead();
            return new FST<T>(new InputStreamDataInput(new BufferedStream(@is)), outputs);
        }

        /// <summary>
        /// Reads bytes stored in an FST.
        /// </summary>
        public abstract class BytesReader : DataInput
        {
            /// <summary>
            /// Current read position
            /// </summary>
            public abstract long Position { get; set; }

            /// <summary>
            /// Returns <c>true</c> if this reader uses reversed bytes
            /// under-the-hood.
            /// </summary>
            /// <returns></returns>
            public abstract bool IsReversed { get; }
        }

        /// <summary>
        /// Specifies allowed range of each int input label for this FST.
        /// </summary>
        public enum INPUT_TYPE { BYTE1, BYTE2, BYTE4 }

        /// <summary>
        /// Represents a single arc.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class Arc<T>
            where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            public int Label { get; set; }

            public T Output { get; set; }

            /// <summary>
            /// To node (ord or address)
            /// </summary>
            public long Target { get; set; }

            internal sbyte Flags { get; set; }
            public T NextFinalOutput { get; set; }

            /// <summary>
            /// address (into the byte[]), or ord/address if label == END_LABEL
            /// </summary>
            internal long NextArc { get; set; }

            /// <summary>
            /// Where the first arc in the array starts; only valid if
            /// <see cref="BytesPerArc"/> != 0 
            /// </summary>
            public long PosArcsStart { get; set; }

            /// <summary>
            /// Non-zero if this arc is part of an array, which means all
            /// arcs for the node are encoded with a fixed number of bytes so
            /// that we can random access by index.  We do when there are enough
            /// arcs leaving one node.  It wastes some bytes but gives faster
            /// lookups.
            /// </summary>
            public int BytesPerArc { get; set; }

            /// <summary>
            /// Where we are in the array; only valid if bytesPerArc != 0, and the array has no holes.
            /// ArcIdx = <see cref="int.MinValue"/> indicates that the arc is part of a direct array, addressed by
            /// label.
            /// </summary>
            public int ArcIdx { get; set; }

            /// <summary>
            /// How many arc, if bytesPerArc == 0. Otherwise, the size of the arc array. If the array is
            /// direct, this may include holes. Otherwise it is also how many arcs are in the array.
            /// </summary>
            public int NumArcs { get; set; }

            /// <summary>
            /// Return this
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public Arc<T> CopyFrom(Arc<T> other)
            {
                Label = other.Label;
                Target = other.Target;
                Flags = other.Flags;
                Output = other.Output;
                NextFinalOutput = other.Output;
                NextFinalOutput = other.NextFinalOutput;
                NextArc = other.NextArc;
                BytesPerArc = other.BytesPerArc;
                if (BytesPerArc != 0)
                {
                    PosArcsStart = other.PosArcsStart;
                    ArcIdx = other.ArcIdx;
                    NumArcs = other.NumArcs;
                }
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal virtual bool Flag(int flag)
            {
                return FST<T>.Flag(Flags, flag);
            }

            public virtual bool IsLast => Flag(BIT_LAST_ARC);

            public virtual bool IsFinal => Flag(BIT_FINAL_ARC);

            public override string ToString()
            {
                var b = new StringBuilder();
                b.Append(" target=").Append(Target);
                b.Append(" label=").Append(Label);
                if (Flag(BIT_LAST_ARC)) b.Append(" last");
                if (Flag(BIT_FINAL_ARC)) b.Append(" final");
                if (Flag(BIT_TARGET_NEXT)) b.Append(" targetNext");
                if (Flag(BIT_ARC_HAS_OUTPUT)) b.Append(" output=").Append(Output);
                if (Flag(BIT_ARC_HAS_FINAL_OUTPUT)) b.Append(" nextFinalOutput=").Append(NextFinalOutput);
                if (BytesPerArc != 0) b.Append(" arcArray(idx=").Append(ArcIdx).Append(" of ").Append(NumArcs).Append(")");
                return b.ToString();
            }
        }
    }
}
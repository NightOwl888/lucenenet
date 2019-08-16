using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene3x
{
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexOptions = Lucene.Net.Index.IndexOptions;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

#pragma warning disable 612, 618
    internal class PreFlexRWFieldsWriter : FieldsConsumer
    {
        private readonly TermInfosWriter termsOut;
        private readonly IndexOutput freqOut;
        private readonly IndexOutput proxOut;
        private readonly PreFlexRWSkipListWriter skipListWriter;
        private readonly int totalNumDocs;

        public PreFlexRWFieldsWriter(SegmentWriteState state)
        {
            termsOut = new TermInfosWriter(state.Directory, state.SegmentInfo.Name, state.FieldInfos, state.TermIndexInterval);

            bool success = false;
            try
            {
                string freqFile = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, "", Lucene3xPostingsFormat.FREQ_EXTENSION);
                freqOut = state.Directory.CreateOutput(freqFile, state.Context);
                totalNumDocs = state.SegmentInfo.DocCount;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(termsOut);
                }
            }

            success = false;
            try
            {
                if (state.FieldInfos.HasProx)
                {
                    string proxFile = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, "", Lucene3xPostingsFormat.PROX_EXTENSION);
                    proxOut = state.Directory.CreateOutput(proxFile, state.Context);
                }
                else
                {
                    proxOut = null;
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(termsOut, freqOut);
                }
            }

            skipListWriter = new PreFlexRWSkipListWriter(termsOut.skipInterval, termsOut.maxSkipLevels, totalNumDocs, freqOut, proxOut);
            //System.out.println("\nw start seg=" + segment);
        }

        public override TermsConsumer AddField(FieldInfo field)
        {
            Debug.Assert(field.Number != -1);
            if (field.IndexOptions.CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0)
            {
                throw new System.NotSupportedException("this codec cannot index offsets");
            }
            //System.out.println("w field=" + field.Name + " storePayload=" + field.storePayloads + " number=" + field.number);
            return new PreFlexTermsWriter(this, field);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IOUtils.Dispose(termsOut, freqOut, proxOut);
            }
        }

        private class PreFlexTermsWriter : TermsConsumer
        {
            internal virtual void InitializeInstanceFields()
            {
                postingsWriter = new PostingsWriter(this);
            }

            private readonly PreFlexRWFieldsWriter outerInstance;

            internal readonly FieldInfo fieldInfo;
            internal readonly bool omitTF;
            internal readonly bool storePayloads;

            internal readonly TermInfo termInfo = new TermInfo();
            internal PostingsWriter postingsWriter;

            public PreFlexTermsWriter(PreFlexRWFieldsWriter outerInstance, FieldInfo fieldInfo)
            {
                this.outerInstance = outerInstance;

                InitializeInstanceFields();
                this.fieldInfo = fieldInfo;
                omitTF = fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY;
                storePayloads = fieldInfo.HasPayloads;
            }

            internal class PostingsWriter : PostingsConsumer
            {
                private readonly PreFlexRWFieldsWriter.PreFlexTermsWriter outerInstance;

                public PostingsWriter(PreFlexRWFieldsWriter.PreFlexTermsWriter outerInstance)
                {
                    this.outerInstance = outerInstance;
                }

                internal int lastDocID;
                internal int lastPayloadLength = -1;
                internal int lastPosition;
                internal int df;

                public PostingsWriter Reset()
                {
                    df = 0;
                    lastDocID = 0;
                    lastPayloadLength = -1;
                    return this;
                }

                public override void StartDoc(int docID, int termDocFreq)
                {
                    //System.out.println("    w doc=" + docID);

                    int delta = docID - lastDocID;
                    if (docID < 0 || (df > 0 && delta <= 0))
                    {
                        throw new CorruptIndexException("docs out of order (" + docID + " <= " + lastDocID + " )");
                    }

                    if ((++df % outerInstance.outerInstance.termsOut.skipInterval) == 0)
                    {
                        outerInstance.outerInstance.skipListWriter.SetSkipData(lastDocID, outerInstance.storePayloads, lastPayloadLength);
                        outerInstance.outerInstance.skipListWriter.BufferSkip(df);
                    }

                    lastDocID = docID;

                    Debug.Assert(docID < outerInstance.outerInstance.totalNumDocs, "docID=" + docID + " totalNumDocs=" + outerInstance.outerInstance.totalNumDocs);

                    if (outerInstance.omitTF)
                    {
                        outerInstance.outerInstance.freqOut.WriteVInt32(delta);
                    }
                    else
                    {
                        int code = delta << 1;
                        if (termDocFreq == 1)
                        {
                            outerInstance.outerInstance.freqOut.WriteVInt32(code | 1);
                        }
                        else
                        {
                            outerInstance.outerInstance.freqOut.WriteVInt32(code);
                            outerInstance.outerInstance.freqOut.WriteVInt32(termDocFreq);
                        }
                    }
                    lastPosition = 0;
                }

                public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
                {
                    Debug.Assert(outerInstance.outerInstance.proxOut != null);
                    Debug.Assert(startOffset == -1);
                    Debug.Assert(endOffset == -1);
                    //System.out.println("      w pos=" + position + " payl=" + payload);
                    int delta = position - lastPosition;
                    lastPosition = position;

                    if (outerInstance.storePayloads)
                    {
                        int payloadLength = payload == null ? 0 : payload.Length;
                        if (payloadLength != lastPayloadLength)
                        {
                            //System.out.println("        write payload len=" + payloadLength);
                            lastPayloadLength = payloadLength;
                            outerInstance.outerInstance.proxOut.WriteVInt32((delta << 1) | 1);
                            outerInstance.outerInstance.proxOut.WriteVInt32(payloadLength);
                        }
                        else
                        {
                            outerInstance.outerInstance.proxOut.WriteVInt32(delta << 1);
                        }
                        if (payloadLength > 0)
                        {
                            outerInstance.outerInstance.proxOut.WriteBytes(payload.Bytes, payload.Offset, payload.Length);
                        }
                    }
                    else
                    {
                        outerInstance.outerInstance.proxOut.WriteVInt32(delta);
                    }
                }

                public override void FinishDoc()
                {
                }
            }

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                //System.out.println("  w term=" + text.utf8ToString());
                outerInstance.skipListWriter.ResetSkip();
                termInfo.FreqPointer = outerInstance.freqOut.GetFilePointer();
                if (outerInstance.proxOut != null)
                {
                    termInfo.ProxPointer = outerInstance.proxOut.GetFilePointer();
                }
                return postingsWriter.Reset();
            }

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                if (stats.DocFreq > 0)
                {
                    long skipPointer = outerInstance.skipListWriter.WriteSkip(outerInstance.freqOut);
                    termInfo.DocFreq = stats.DocFreq;
                    termInfo.SkipOffset = (int)(skipPointer - termInfo.FreqPointer);
                    //System.out.println("  w finish term=" + text.utf8ToString() + " fnum=" + fieldInfo.number);
                    outerInstance.termsOut.Add(fieldInfo.Number, text, termInfo);
                }
            }

            public override void Finish(long sumTotalTermCount, long sumDocFreq, int docCount)
            {
            }

            public override IComparer<BytesRef> Comparer
            {
                get
                {
                    return BytesRef.UTF8SortedAsUTF16Comparer;
                }
            }
        }
    }
#pragma warning restore 612, 618
}
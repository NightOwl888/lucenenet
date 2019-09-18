using System;
using IndexFileNames = Lucene.Net.Index.IndexFileNames;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

namespace Lucene.Net.Codecs.Lucene40
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
    /// Read-write version of <see cref="Lucene40DocValuesFormat"/> for testing. </summary>
#pragma warning disable 612, 618
    public class Lucene40RWDocValuesFormat : Lucene40DocValuesFormat
    {
#if FEATURE_INSTANCE_CODEC_IMPERSONATION
        private readonly LuceneTestCase luceneTestCase;
        public Lucene40RWDocValuesFormat(LuceneTestCase luceneTestCase)
            : base(luceneTestCase)
        {
            this.luceneTestCase = luceneTestCase ?? throw new ArgumentNullException(nameof(luceneTestCase));
        }
#else
        public Lucene40RWDocValuesFormat(ICodecProvider codecProvider)
            : base(codecProvider)
        { }
#endif

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
#if FEATURE_INSTANCE_CODEC_IMPERSONATION
            if (!luceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
#else
            if (!LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
#endif
            {
                return base.FieldsConsumer(state);
            }
            else
            {
                string filename = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, "dv", IndexFileNames.COMPOUND_FILE_EXTENSION);
                return new Lucene40DocValuesWriter(state, filename, Lucene40FieldInfosReader.LEGACY_DV_TYPE_KEY);
            }
        }
    }
#pragma warning restore 612, 618
}
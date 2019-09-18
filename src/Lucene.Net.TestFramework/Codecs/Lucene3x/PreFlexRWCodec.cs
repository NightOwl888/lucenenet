using System;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Codecs.Lucene3x
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
    /// Writes 3.x-like indexes (not perfect emulation yet) for testing only!
    /// <para/>
    /// @lucene.experimental
    /// </summary>
#pragma warning disable 612, 618
    public class PreFlexRWCodec : Lucene3xCodec
    {
#if FEATURE_INSTANCE_CODEC_IMPERSONATION
        private readonly LuceneTestCase luceneTestCase;
        public PreFlexRWCodec(LuceneTestCase luceneTestCase)
            : base(luceneTestCase)
        {
            this.luceneTestCase = luceneTestCase ?? throw new ArgumentNullException(nameof(luceneTestCase));
            this.postings = new PreFlexRWPostingsFormat(luceneTestCase);
        }
#else
        public PreFlexRWCodec(ICodecProvider codecProvider)
            : base(codecProvider)
        {
            this.postings = new PreFlexRWPostingsFormat(codecProvider);
        }
#endif

        private readonly PostingsFormat postings;
        private readonly Lucene3xNormsFormat norms = new PreFlexRWNormsFormat();
        private readonly FieldInfosFormat fieldInfos = new PreFlexRWFieldInfosFormat();
        private readonly TermVectorsFormat termVectors = new PreFlexRWTermVectorsFormat();
        private readonly SegmentInfoFormat segmentInfos = new PreFlexRWSegmentInfoFormat();
        private readonly StoredFieldsFormat storedFields = new PreFlexRWStoredFieldsFormat();


        public override PostingsFormat PostingsFormat
        {
            get
            {
#if FEATURE_INSTANCE_CODEC_IMPERSONATION
                if (luceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
#else
                if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
#endif
                {
                    return postings;
                }
                else
                {
                    return base.PostingsFormat;
                }
            }
        }

        public override NormsFormat NormsFormat
        {
            get
            {
#if FEATURE_INSTANCE_CODEC_IMPERSONATION
                if (luceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
#else
                if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
#endif
                {
                    return norms;
                }
                else
                {
                    return base.NormsFormat;
                }
            }
        }

        public override SegmentInfoFormat SegmentInfoFormat
        {
            get
            {
#if FEATURE_INSTANCE_CODEC_IMPERSONATION
                if (luceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
#else
                if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
#endif
                {
                    return segmentInfos;
                }
                else
                {
                    return base.SegmentInfoFormat;
                }
            }
        }

        public override FieldInfosFormat FieldInfosFormat
        {
            get
            {
#if FEATURE_INSTANCE_CODEC_IMPERSONATION
                if (luceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
#else
                if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
#endif
                {
                    return fieldInfos;
                }
                else
                {
                    return base.FieldInfosFormat;
                }
            }
        }

        public override TermVectorsFormat TermVectorsFormat
        {
            get
            {
#if FEATURE_INSTANCE_CODEC_IMPERSONATION
                if (luceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
#else
                if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
#endif
                {
                    return termVectors;
                }
                else
                {
                    return base.TermVectorsFormat;
                }
            }
        }

        public override StoredFieldsFormat StoredFieldsFormat
        {
            get
            {
#if FEATURE_INSTANCE_CODEC_IMPERSONATION
                if (luceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
#else
                if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
#endif
                {
                    return storedFields;
                }
                else
                {
                    return base.StoredFieldsFormat;
                }
            }
        }
    }
#pragma warning restore 612, 618
}
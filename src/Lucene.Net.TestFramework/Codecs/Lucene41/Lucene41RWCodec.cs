using System;
using Lucene40FieldInfosFormat = Lucene.Net.Codecs.Lucene40.Lucene40FieldInfosFormat;
using Lucene40FieldInfosWriter = Lucene.Net.Codecs.Lucene40.Lucene40FieldInfosWriter;
using Lucene40RWDocValuesFormat = Lucene.Net.Codecs.Lucene40.Lucene40RWDocValuesFormat;
using Lucene40RWNormsFormat = Lucene.Net.Codecs.Lucene40.Lucene40RWNormsFormat;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Codecs.Lucene41
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
    /// Read-write version of <see cref="Lucene41Codec"/> for testing.
    /// </summary>
#pragma warning disable 612, 618
    public class Lucene41RWCodec : Lucene41Codec
    {
        private readonly StoredFieldsFormat fieldsFormat = new Lucene41StoredFieldsFormat();
        private readonly FieldInfosFormat fieldInfos
#if !FEATURE_INSTANCE_CODEC_IMPERSONATION
            = new Lucene40FieldInfosFormatAnonymousInnerClassHelper()
#endif
            ;
#if FEATURE_INSTANCE_CODEC_IMPERSONATION
        public Lucene41RWCodec(LuceneTestCase luceneTestCase)
            : base(luceneTestCase)
        {
            this.fieldInfos = new Lucene40FieldInfosFormatAnonymousInnerClassHelper(luceneTestCase);
            this.docValues = new Lucene40RWDocValuesFormat(luceneTestCase);
            this.norms = new Lucene40RWNormsFormat(luceneTestCase);
        }
#else
        public Lucene41RWCodec(ICodecProvider codecProvider)
            : base(codecProvider)
        {
            this.docValues = new Lucene40RWDocValuesFormat(codecProvider);
        }
#endif

        private class Lucene40FieldInfosFormatAnonymousInnerClassHelper : Lucene40FieldInfosFormat
        {
#if FEATURE_INSTANCE_CODEC_IMPERSONATION
            private readonly LuceneTestCase luceneTestCase;
            public Lucene40FieldInfosFormatAnonymousInnerClassHelper(LuceneTestCase luceneTestCase)
            {
                this.luceneTestCase = luceneTestCase ?? throw new ArgumentNullException(nameof(luceneTestCase));
            }
#endif

            public override FieldInfosWriter FieldInfosWriter
            {
                get
                {
#if FEATURE_INSTANCE_CODEC_IMPERSONATION
                    if (!luceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
#else
                    if (!LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
#endif
                    {
                        return base.FieldInfosWriter;
                    }
                    else
                    {
                        return new Lucene40FieldInfosWriter();
                    }
                }
            }
        }

        private readonly DocValuesFormat docValues;
        private readonly NormsFormat norms
#if !FEATURE_INSTANCE_CODEC_IMPERSONATION
            = new Lucene40RWNormsFormat()
#endif
            ;


        public override FieldInfosFormat FieldInfosFormat
        {
            get { return fieldInfos; }
        }

        public override StoredFieldsFormat StoredFieldsFormat
        {
            get { return fieldsFormat; }
        }

        public override DocValuesFormat DocValuesFormat
        {
            get { return docValues; }
        }

        public override NormsFormat NormsFormat
        {
            get { return norms; }
        }
    }
#pragma warning restore 612, 618
}
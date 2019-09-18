using System;
using Lucene42FieldInfosFormat = Lucene.Net.Codecs.Lucene42.Lucene42FieldInfosFormat;
using Lucene42FieldInfosWriter = Lucene.Net.Codecs.Lucene42.Lucene42FieldInfosWriter;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Codecs.Lucene45
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
    /// Read-write version of <see cref="Lucene45Codec"/> for testing.
    /// </summary>
#pragma warning disable 612, 618
    public class Lucene45RWCodec : Lucene45Codec
    {
#if FEATURE_INSTANCE_CODEC_IMPERSONATION
        public Lucene45RWCodec(LuceneTestCase luceneTestCase)
            : base(luceneTestCase)
        {
            this.fieldInfosFormat = new Lucene42FieldInfosFormatAnonymousInnerClassHelper(luceneTestCase);
        }
#else
        public Lucene45RWCodec(ICodecProvider codecProvider)
            : base(codecProvider)
        { }
#endif

        private readonly FieldInfosFormat fieldInfosFormat
#if !FEATURE_INSTANCE_CODEC_IMPERSONATION
            = new Lucene42FieldInfosFormatAnonymousInnerClassHelper()
#endif
            ;

        private class Lucene42FieldInfosFormatAnonymousInnerClassHelper : Lucene42FieldInfosFormat
        {
#if FEATURE_INSTANCE_CODEC_IMPERSONATION
            private readonly LuceneTestCase luceneTestCase;
            public Lucene42FieldInfosFormatAnonymousInnerClassHelper(LuceneTestCase luceneTestCase)
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
                        return new Lucene42FieldInfosWriter();
                    }
                }
            }
        }

        public override FieldInfosFormat FieldInfosFormat
        {
            get { return fieldInfosFormat; }
        }
    }
#pragma warning restore 612, 618
}
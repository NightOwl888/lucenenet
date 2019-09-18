/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using Lucene.Net.Util;
using System;
using System.Reflection;

namespace Lucene.Net.Codecs
{
    /// <summary>
    /// LUCENENET specific class used to add the PostingsFormats from the test framework.
    /// </summary>
    public class TestPostingsFormatFactory : DefaultPostingsFormatFactory
    {
#if FEATURE_INSTANCE_CODEC_IMPERSONATION
        private readonly LuceneTestCase luceneTestCase;

        public TestPostingsFormatFactory(LuceneTestCase luceneTestCase)
            : base(luceneTestCase)
        {
            this.luceneTestCase = luceneTestCase ?? throw new ArgumentNullException(nameof(luceneTestCase));
        }

        protected override PostingsFormat NewPostingsFormat(Type type)
        {
            // Inject our LuceneTestCase instance into the codec if there is 
            // a single parameter of type LuceneTestCase
            var constructor = type.GetConstructor(new Type[] { typeof(LuceneTestCase) });
            if (constructor != null)
            {
                return (PostingsFormat)constructor.Invoke(new object[] { luceneTestCase });
            }
            return base.NewPostingsFormat(type);
        }
#else
        public TestPostingsFormatFactory()
            : base(Codecs.CodecProvider.Default)
        { }
#endif
        protected override void Initialize()
        {
            base.Initialize();
            base.ScanForPostingsFormats(this.GetType().GetTypeInfo().Assembly);
        }
    }
}

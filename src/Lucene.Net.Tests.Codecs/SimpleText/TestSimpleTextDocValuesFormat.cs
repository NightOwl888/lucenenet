﻿using System;
using Lucene.Net.Index;
using NUnit.Framework;
using Lucene.Net.Analysis;
using Lucene.Net.Store;
using Lucene.Net.Documents;
using Lucene.Net.Util;
using Lucene.Net.Search;
using System.Diagnostics;
using Lucene.Net.Analysis.Standard;

namespace Lucene.Net.Codecs.SimpleText
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
    /// Tests SimpleTextDocValuesFormat
    /// </summary>
    public class TestSimpleTextDocValuesFormat : BaseDocValuesFormatTestCase
    {
        private readonly Codec codec;

        // LUCENENET specific - pass test instance as ICodecProvider
        public TestSimpleTextDocValuesFormat()
        {
            codec = new SimpleTextCodec(this);
        }

        protected override Codec GetCodec()
        {
            return codec;
        }
    }
}
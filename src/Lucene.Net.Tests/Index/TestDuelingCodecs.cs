﻿using J2N.Text;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using System.Text.RegularExpressions;

namespace Lucene.Net.Index
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using LineFileDocs = Lucene.Net.Util.LineFileDocs;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using NumericDocValuesField = NumericDocValuesField;
    using SortedSetDocValuesField = SortedSetDocValuesField;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Compares one codec against another
    /// </summary>
    [TestFixture]
    public class TestDuelingCodecs : LuceneTestCase
    {
        private Directory leftDir;
        private IndexReader leftReader;
        private Codec leftCodec;

        private Directory rightDir;
        private IndexReader rightReader;
        private Codec rightCodec;

        private string info; // for debugging

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // for now its SimpleText vs Lucene46(random postings format)
            // as this gives the best overall coverage. when we have more
            // codecs we should probably pick 2 from Codec.availableCodecs()

            leftCodec = Codec.ForName("SimpleText");
            rightCodec = new RandomCodec(Random);

            leftDir = NewDirectory();
            rightDir = NewDirectory();

            long seed = Random.Next();

            // must use same seed because of random payloads, etc
            int maxTermLength = TestUtil.NextInt32(Random, 1, IndexWriter.MAX_TERM_LENGTH);
            MockAnalyzer leftAnalyzer = new MockAnalyzer(new Random((int)seed));
            leftAnalyzer.MaxTokenLength = maxTermLength;
            MockAnalyzer rightAnalyzer = new MockAnalyzer(new Random((int)seed));
            rightAnalyzer.MaxTokenLength = maxTermLength;

            // but these can be different
            // TODO: this turns this into a really big test of Multi*, is that what we want?
            IndexWriterConfig leftConfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, leftAnalyzer);
            leftConfig.SetCodec(leftCodec);
            // preserve docids
            leftConfig.SetMergePolicy(NewLogMergePolicy());

            IndexWriterConfig rightConfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, rightAnalyzer);
            rightConfig.SetCodec(rightCodec);
            // preserve docids
            rightConfig.SetMergePolicy(NewLogMergePolicy());

            // must use same seed because of random docvalues fields, etc
            RandomIndexWriter leftWriter = new RandomIndexWriter(new Random((int)seed), leftDir, leftConfig);
            RandomIndexWriter rightWriter = new RandomIndexWriter(new Random((int)seed), rightDir, rightConfig);

            int numdocs = AtLeast(100);
            CreateRandomIndex(numdocs, leftWriter, seed);
            CreateRandomIndex(numdocs, rightWriter, seed);

            leftReader = MaybeWrapReader(leftWriter.GetReader());
            leftWriter.Dispose();
            rightReader = MaybeWrapReader(rightWriter.GetReader());
            rightWriter.Dispose();

            // check that our readers are valid
            TestUtil.CheckReader(leftReader);
            TestUtil.CheckReader(rightReader);

            info = "left: " + leftCodec.ToString() + " / right: " + rightCodec.ToString();
        }

        [TearDown]
        public override void TearDown()
        {
            if (leftReader != null)
            {
                leftReader.Dispose();
            }
            if (rightReader != null)
            {
                rightReader.Dispose();
            }

            if (leftDir != null)
            {
                leftDir.Dispose();
            }
            if (rightDir != null)
            {
                rightDir.Dispose();
            }

            base.TearDown();
        }

        /// <summary>
        /// populates a writer with random stuff. this must be fully reproducable with the seed!
        /// </summary>
        public static void CreateRandomIndex(int numdocs, RandomIndexWriter writer, long seed)
        {
            Random random = new Random((int)seed);
            // primary source for our data is from linefiledocs, its realistic.
            LineFileDocs lineFileDocs = new LineFileDocs(random);

            // LUCENENET: compile a regex so we don't have to do it in each loop (for regex.split())
            Regex whiteSpace = new Regex("\\s+", RegexOptions.Compiled);

            // TODO: we should add other fields that use things like docs&freqs but omit positions,
            // because linefiledocs doesn't cover all the possibilities.
            for (int i = 0; i < numdocs; i++)
            {
                Document document = lineFileDocs.NextDoc();
                // grab the title and add some SortedSet instances for fun
                string title = document.Get("titleTokenized");
                string[] split = whiteSpace.Split(title).TrimEnd();
                foreach (string trash in split)
                {
                    document.Add(new SortedSetDocValuesField("sortedset", new BytesRef(trash)));
                }
                // add a numeric dv field sometimes
                document.RemoveFields("sparsenumeric");
                if (random.Next(4) == 2)
                {
                    document.Add(new NumericDocValuesField("sparsenumeric", random.Next()));
                }
                writer.AddDocument(document);
            }

            lineFileDocs.Dispose();
        }

        /// <summary>
        /// checks the two indexes are equivalent
        /// </summary>
        [Test]
        [AwaitsFix(BugUrl = "https://github.com/apache/lucenenet/issues/545")] // LUCENENET TODO: This test occasionally fails
        public virtual void TestEquals()
        {
            AssertReaderEquals(info, leftReader, rightReader);
        }
    }
}
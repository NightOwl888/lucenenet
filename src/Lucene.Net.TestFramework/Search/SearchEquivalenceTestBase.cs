using Lucene.Net.Documents;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace Lucene.Net.Search
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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;
    using CharacterRunAutomaton = Lucene.Net.Util.Automaton.CharacterRunAutomaton;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using StringField = StringField;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    /// <summary>
    /// Simple base class for checking search equivalence.
    /// Extend it, and write tests that create <see cref="RandomTerm()"/>s
    /// (all terms are single characters a-z), and use
    /// <see cref="AssertSameSet(Query, Query)"/> and
    /// <see cref="AssertSubsetOf(Query, Query)"/>.
    /// </summary>
    public abstract class SearchEquivalenceTestBase : LuceneTestCase
    {
        protected static IndexSearcher m_s1, m_s2;
        protected static Directory m_directory;
        protected static IndexReader m_reader;
        protected static Analyzer m_analyzer;
        protected static string m_stopword; // we always pick a character as a stopword

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because ClassEnvRule is no longer static.
        /// </summary>
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            Random random = Random;
            m_directory = NewDirectory();
            m_stopword = "" + RandomChar();
            CharacterRunAutomaton stopset = new CharacterRunAutomaton(BasicAutomata.MakeString(m_stopword));
            m_analyzer = new MockAnalyzer(random, MockTokenizer.WHITESPACE, false, stopset);
            RandomIndexWriter iw = new RandomIndexWriter(random, m_directory, m_analyzer, ClassEnvRule.similarity, ClassEnvRule.timeZone);
            Document doc = new Document();
            Field id = new StringField("id", "", Field.Store.NO);
            Field field = new TextField("field", "", Field.Store.NO);
            doc.Add(id);
            doc.Add(field);

            // index some docs
            int numDocs = AtLeast(1000);
            for (int i = 0; i < numDocs; i++)
            {
                id.SetStringValue(Convert.ToString(i, CultureInfo.InvariantCulture));
                field.SetStringValue(RandomFieldContents());
                iw.AddDocument(doc);
            }

            // delete some docs
            int numDeletes = numDocs / 20;
            for (int i = 0; i < numDeletes; i++)
            {
                Term toDelete = new Term("id", Convert.ToString(random.Next(numDocs), CultureInfo.InvariantCulture));
                if (random.NextBoolean())
                {
                    iw.DeleteDocuments(toDelete);
                }
                else
                {
                    iw.DeleteDocuments(new TermQuery(toDelete));
                }
            }

            m_reader = iw.GetReader();
            m_s1 = NewSearcher(m_reader);
            m_s2 = NewSearcher(m_reader);
            iw.Dispose();
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            m_reader.Dispose();
            m_directory.Dispose();
            m_analyzer.Dispose();
            m_reader = null;
            m_directory = null;
            m_analyzer = null;
            m_s1 = m_s2 = null;
            base.AfterClass();
        }

        /// <summary>
        /// Populate a field with random contents.
        /// Terms should be single characters in lowercase (a-z)
        /// tokenization can be assumed to be on whitespace.
        /// </summary>
        internal static string RandomFieldContents()
        {
            // TODO: zipf-like distribution
            StringBuilder sb = new StringBuilder();
            int numTerms = Random.Next(15);
            for (int i = 0; i < numTerms; i++)
            {
                if (sb.Length > 0)
                {
                    sb.Append(' '); // whitespace
                }
                sb.Append(RandomChar());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns random character (a-z)
        /// </summary>
        internal static char RandomChar()
        {
            return (char)TestUtil.NextInt32(Random, 'a', 'z');
        }

        /// <summary>
        /// Returns a term suitable for searching.
        /// Terms are single characters in lowercase (a-z).
        /// </summary>
        protected virtual Term RandomTerm()
        {
            return new Term("field", "" + RandomChar());
        }

        /// <summary>
        /// Returns a random filter over the document set.
        /// </summary>
        protected virtual Filter RandomFilter()
        {
            return new QueryWrapperFilter(TermRangeQuery.NewStringRange("field", "a", "" + RandomChar(), true, true));
        }

        /// <summary>
        /// Asserts that the documents returned by <paramref name="q1"/>
        /// are the same as of those returned by <paramref name="q2"/>.
        /// </summary>
        public virtual void AssertSameSet(Query q1, Query q2)
        {
            AssertSubsetOf(q1, q2);
            AssertSubsetOf(q2, q1);
        }

        /// <summary>
        /// Asserts that the documents returned by <paramref name="q1"/>
        /// are a subset of those returned by <paramref name="q2"/>.
        /// </summary>
        public virtual void AssertSubsetOf(Query q1, Query q2)
        {
            // test without a filter
            AssertSubsetOf(q1, q2, null);

            // test with a filter (this will sometimes cause advance'ing enough to test it)
            AssertSubsetOf(q1, q2, RandomFilter());
        }

        /// <summary>
        /// Asserts that the documents returned by <paramref name="q1"/>
        /// are a subset of those returned by <paramref name="q2"/>.
        /// <para/>
        /// Both queries will be filtered by <paramref name="filter"/>.
        /// </summary>
        protected virtual void AssertSubsetOf(Query q1, Query q2, Filter filter)
        {
            // TRUNK ONLY: test both filter code paths
            if (filter != null && Random.NextBoolean())
            {
                q1 = new FilteredQuery(q1, filter, TestUtil.RandomFilterStrategy(Random));
                q2 = new FilteredQuery(q2, filter, TestUtil.RandomFilterStrategy(Random));
                filter = null;
            }

            // not efficient, but simple!
            TopDocs td1 = m_s1.Search(q1, filter, m_reader.MaxDoc);
            TopDocs td2 = m_s2.Search(q2, filter, m_reader.MaxDoc);
            Assert.IsTrue(td1.TotalHits <= td2.TotalHits);

            // fill the superset into a bitset
            var bitset = new BitArray(td2.ScoreDocs.Length);
            for (int i = 0; i < td2.ScoreDocs.Length; i++)
            {
                bitset.SafeSet(td2.ScoreDocs[i].Doc, true);
            }

            // check in the subset, that every bit was set by the super
            for (int i = 0; i < td1.ScoreDocs.Length; i++)
            {
                Assert.IsTrue(bitset.SafeGet(td1.ScoreDocs[i].Doc));
            }
        }
    }
}
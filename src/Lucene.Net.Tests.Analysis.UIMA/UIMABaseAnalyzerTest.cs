using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Uima
{
    /// <summary>
    /// Testcase for <see cref="UIMABaseAnalyzer"/>
    /// </summary>
    public class UIMABaseAnalyzerTest : BaseTokenStreamTestCase
    {
        private DirectoryManager manager = new DirectoryManager(); 
        private UIMABaseAnalyzer analyzer;

        public override void BeforeClass()
        {
            base.BeforeClass();
            manager.BeforeClass();
        }

        public override void AfterClass()
        {
            manager.AfterClass();
            base.AfterClass();
        }

        public override void SetUp()
        {
            base.SetUp();
            analyzer = new UIMABaseAnalyzer("/uima/AggregateSentenceAE.xml", "org.apache.uima.TokenAnnotation", null);
        }

        public override void TearDown()
        {
            analyzer.Dispose();
            base.TearDown();
        }

        [Test]
        public void BaseUIMAAnalyzerStreamTest()
        {
            TokenStream ts = analyzer.GetTokenStream("text", "the big brown fox jumped on the wood");
            AssertTokenStreamContents(ts, new String[] { "the", "big", "brown", "fox", "jumped", "on", "the", "wood" });
        }

        [Test]
        public void BaseUIMAAnalyzerIntegrationTest()
        {
            Directory dir = new RAMDirectory();
            IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            // add the first doc
            Document doc = new Document();
            String dummyTitle = "this is a dummy title ";
            doc.Add(new TextField("title", dummyTitle, Field.Store.YES));
            String dummyContent = "there is some content written here";
            doc.Add(new TextField("contents", dummyContent, Field.Store.YES));
            writer.AddDocument(doc, analyzer);
            writer.Commit();

            // try the search over the first doc
            DirectoryReader directoryReader = DirectoryReader.Open(dir);
            IndexSearcher indexSearcher = NewSearcher(directoryReader);
            TopDocs result = indexSearcher.Search(new MatchAllDocsQuery(), 1);
            assertTrue(result.TotalHits > 0);
            Document d = indexSearcher.Doc(result.ScoreDocs[0].Doc);
            assertNotNull(d);
            assertNotNull(d.GetField("title"));
            assertEquals(dummyTitle, d.GetField("title").GetStringValue());
            assertNotNull(d.GetField("contents"));
            assertEquals(dummyContent, d.GetField("contents").GetStringValue());

            // add a second doc
            doc = new Document();
            String dogmasTitle = "dogmas";
            doc.Add(new TextField("title", dogmasTitle, Field.Store.YES));
            String dogmasContents = "white men can't jump";
            doc.Add(new TextField("contents", dogmasContents, Field.Store.YES));
            writer.AddDocument(doc, analyzer);
            writer.Commit();

            directoryReader.Dispose();
            directoryReader = DirectoryReader.Open(dir);
            indexSearcher = NewSearcher(directoryReader);
            result = indexSearcher.Search(new MatchAllDocsQuery(), 2);
            Document d1 = indexSearcher.Doc(result.ScoreDocs[1].Doc);
            assertNotNull(d1);
            assertNotNull(d1.GetField("title"));
            assertEquals(dogmasTitle, d1.GetField("title").GetStringValue());
            assertNotNull(d1.GetField("contents"));
            assertEquals(dogmasContents, d1.GetField("contents").GetStringValue());

            // do a matchalldocs query to retrieve both docs
            indexSearcher = NewSearcher(directoryReader);
            result = indexSearcher.Search(new MatchAllDocsQuery(), 2);
            assertEquals(2, result.TotalHits);
            writer.Dispose();
            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestRandomStrings()
        {
            CheckRandomData(Random(), new UIMABaseAnalyzer("/uima/TestAggregateSentenceAE.xml", "org.apache.lucene.uima.ts.TokenAnnotation", null),
                100 * RANDOM_MULTIPLIER);
        }

        [Test]
        public void TestRandomStringsWithConfigurationParameters()
        {
            IDictionary<String, Object> cp = new Dictionary<String, Object>();
            cp.Put("line-end", "\r");
            CheckRandomData(Random(), new UIMABaseAnalyzer("/uima/TestWSTokenizerAE.xml", "org.apache.lucene.uima.ts.TokenAnnotation", cp),
                100 * RANDOM_MULTIPLIER);
        }
    }
}

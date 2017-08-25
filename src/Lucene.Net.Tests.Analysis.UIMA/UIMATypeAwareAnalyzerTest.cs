using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Uima
{
    /// <summary>
    /// Testcase for <see cref="UIMATypeAwareAnalyzer"/>.
    /// </summary>
    public class UIMATypeAwareAnalyzerTest : BaseTokenStreamTestCase
    {
        private UIMATypeAwareAnalyzer analyzer;

        public override void SetUp()
        {
            base.SetUp();
            analyzer = new UIMATypeAwareAnalyzer("/uima/AggregateSentenceAE.xml",
                "org.apache.uima.TokenAnnotation", "posTag", null);
        }

        public override void TearDown()
        {
            analyzer.Dispose();
            base.TearDown();
        }

        [Test]
        public void BaseUIMATypeAwareAnalyzerStreamTest()
        {

            // create a token stream
            TokenStream ts = analyzer.GetTokenStream("text", "the big brown fox jumped on the wood");

            // check that 'the big brown fox jumped on the wood' tokens have the expected PoS types
            AssertTokenStreamContents(ts,
            new String[] { "the", "big", "brown", "fox", "jumped", "on", "the", "wood" },
            new String[] { "at", "jj", "jj", "nn", "vbd", "in", "at", "nn" });

        }

        [Test]
        public void TestRandomStrings()
        {
            CheckRandomData(Random(), new UIMATypeAwareAnalyzer("/uima/TestAggregateSentenceAE.xml",
                "org.apache.lucene.uima.ts.TokenAnnotation", "pos", null), 100 * RANDOM_MULTIPLIER);
        }
    }
}

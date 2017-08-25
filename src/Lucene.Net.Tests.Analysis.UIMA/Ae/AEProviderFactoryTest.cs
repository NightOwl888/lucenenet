using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Analysis.Uima.Ae
{
    /// <summary>
    /// Testcase for <see cref="AEProviderFactory"/>
    /// </summary>
    public class AEProviderFactoryTest : LuceneTestCase
    {
        [Test]
        public void TestCorrectCaching()
        {
            IAEProvider aeProvider = AEProviderFactory.Instance.GetAEProvider("/uima/TestAggregateSentenceAE.xml");
            assertTrue(aeProvider == AEProviderFactory.Instance.GetAEProvider("/uima/TestAggregateSentenceAE.xml"));
        }

        [Test]
        public void TestCorrectCachingWithParameters()
        {
            IAEProvider aeProvider = AEProviderFactory.Instance.GetAEProvider("prefix", "/uima/TestAggregateSentenceAE.xml",
            new Dictionary<String, Object>());
            assertTrue(aeProvider == AEProviderFactory.Instance.GetAEProvider("prefix", "/uima/TestAggregateSentenceAE.xml",
                new Dictionary<String, Object>()));
        }
    }
}

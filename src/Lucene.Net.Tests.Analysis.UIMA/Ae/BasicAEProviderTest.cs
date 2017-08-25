using Lucene.Net.Util;
using NUnit.Framework;
using org.apache.uima.analysis_engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Analysis.Uima.Ae
{
    /// <summary>
    /// TestCase for <see cref="BasicAEProvider"/>
    /// </summary>
    public class BasicAEProviderTest : LuceneTestCase
    {
        [Test]
        public void TestBasicInitialization()
        {
            IAEProvider basicAEProvider = new BasicAEProvider("/uima/TestEntityAnnotatorAE.xml");
            AnalysisEngine analysisEngine = basicAEProvider.GetAE();
            assertNotNull(analysisEngine);
        }
    }
}

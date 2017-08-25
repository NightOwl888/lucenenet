using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using org.apache.uima.analysis_engine;
using org.apache.uima.resource;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lucene.Net.Analysis.Uima.Ae
{
    /// <summary>
    /// TestCase for <see cref="OverridingParamsAEProvider"/>
    /// </summary>
    public class OverridingParamsAEProviderTest : LuceneTestCase
    {
        [Test]
        public void TestNullMapInitialization()
        {
            try
            {
                IAEProvider aeProvider = new OverridingParamsAEProvider("/uima/TestEntityAnnotatorAE.xml", null);
                aeProvider.GetAE();
                fail("should fail due to null Map passed");
            }
            catch (ResourceInitializationException e)
            {
                // everything ok
            }
        }

        [Test]
        public void TestEmptyMapInitialization()
        {
            IAEProvider aeProvider = new OverridingParamsAEProvider("/uima/TestEntityAnnotatorAE.xml", new Dictionary<String, Object>());
            AnalysisEngine analysisEngine = aeProvider.GetAE();
            assertNotNull(analysisEngine);
        }

        [Test]
        public void TestOverridingParamsInitialization()
        {
            IDictionary<String, Object> runtimeParameters = new Dictionary<String, Object>();
            runtimeParameters.Put("ngramsize", "3");
            IAEProvider aeProvider = new OverridingParamsAEProvider("/uima/AggregateSentenceAE.xml", runtimeParameters);
            AnalysisEngine analysisEngine = aeProvider.GetAE();
            assertNotNull(analysisEngine);
            Object parameterValue = analysisEngine.getConfigParameterValue("ngramsize");
            assertNotNull(parameterValue);
            assertEquals(Convert.ToInt32(3), Convert.ToInt32(parameterValue.toString(), CultureInfo.InvariantCulture));
        }
    }
}

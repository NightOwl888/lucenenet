using J2N;
using Lucene.Net.Analysis.Cn.Smart;
using Lucene.Net.Analysis.Cn.Smart.Hhmm;
using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Analysis.Cn.Smart.Hhmm
{
    [LuceneNetSpecific]
    public class TestBuildDictionary : LuceneTestCase
    {
        private DirectoryInfo tempDir;

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            tempDir = CreateTempDir("smartcn-data");
            AnalyzerProfile.ANALYSIS_DATA_DIR = tempDir.FullName;
            using (var zipFileStream = typeof(TestBuildDictionary).FindAndGetManifestResourceStream("custom-dictionary-input.zip"))
            {
                TestUtil.Unzip(zipFileStream, tempDir);
            }
        }

        public override void OneTimeTearDown()
        {
            AnalyzerProfile.ANALYSIS_DATA_DIR = null; // Ensure this test data is not loaded for other tests
            base.OneTimeTearDown();
        }

        [Test]
        public void TestBigramDictionary()
        {
            BigramDictionary bigramDict = BigramDictionary.GetInstance();
        }

        [Test]
        public void TestWordDictionary()
        {
            WordDictionary wordDict = WordDictionary.GetInstance();
        }
    }
}

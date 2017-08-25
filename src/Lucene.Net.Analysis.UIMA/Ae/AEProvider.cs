using org.apache.uima.analysis_engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Analysis.Uima.Ae
{
    /// <summary>
    /// provide an Apache UIMA <see cref="AnalysisEngine"/>
    /// </summary>
    public interface IAEProvider
    {
        /// <summary>
        /// Returns the AnalysisEngine
        /// </summary>
        AnalysisEngine GetAE();
    }
}

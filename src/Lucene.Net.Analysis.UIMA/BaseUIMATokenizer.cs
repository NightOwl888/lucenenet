using Lucene.Net.Analysis.Uima.Ae;
using org.apache.uima.analysis_engine;
using org.apache.uima.cas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Uima
{
    /// <summary>
    /// Abstract base implementation of a <see cref="Tokenizer"/> which is able to analyze the given input with a
    /// UIMA <see cref="AnalysisEngine"/>.
    /// </summary>
    public abstract class BaseUIMATokenizer : Tokenizer
    {
        protected FSIterator /*<AnnotationFS>*/ iterator;

        private readonly string descriptorPath;
        private readonly IDictionary<string, object> configurationParameters;

        protected AnalysisEngine ae;
        protected CAS cas;

        protected BaseUIMATokenizer
            (AttributeFactory factory, TextReader reader, string descriptorPath, IDictionary<string, object> configurationParameters)
            : base(factory, reader)
        {
            this.descriptorPath = descriptorPath;
            this.configurationParameters = configurationParameters;
        }

        /**
         * analyzes the tokenizer input using the given analysis engine
         * <p/>
         * {@link #cas} will be filled with  extracted metadata (UIMA annotations, feature structures)
         *
         * @throws IOException If there is a low-level I/O error.
         */
        protected virtual void AnalyzeInput() //throws ResourceInitializationException, AnalysisEngineProcessException, IOException 
        {
            if (ae == null)
            {
                ae = AEProviderFactory.Instance.GetAEProvider(null, descriptorPath, configurationParameters).GetAE();
            }
            if (cas == null)
            {
                cas = ae.newCAS();
            }
            else
            {
                cas.reset();
            }
            cas.setDocumentText(ToString(m_input));
            ae.process(cas);
        }

        /**
         * initialize the FSIterator which is used to build tokens at each incrementToken() method call
         *
         * @throws IOException If there is a low-level I/O error.
         */
        protected abstract void InitializeIterator(); //throws IOException;

        private String ToString(TextReader reader) //throws IOException
        {
            StringBuilder stringBuilder = new StringBuilder();
            int ch;
            while ((ch = reader.Read()) > -1)
            {
                stringBuilder.Append((char)ch);
            }
            return stringBuilder.ToString();
        }

        public override void Reset() //throws IOException
        {
            base.Reset();
            iterator = null;
        }
    }
}

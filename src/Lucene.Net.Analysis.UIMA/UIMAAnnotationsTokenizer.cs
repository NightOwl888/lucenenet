using Lucene.Net.Analysis.TokenAttributes;
using org.apache.uima.analysis_engine;
using org.apache.uima.cas.text;
using org.apache.uima.resource;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Uima
{
    /// <summary>
    /// a <see cref="Tokenizer"/> which creates tokens from UIMA Annotations
    /// </summary>
    public sealed class UIMAAnnotationsTokenizer : BaseUIMATokenizer
    {
        private readonly ICharTermAttribute termAttr;

        private readonly IOffsetAttribute offsetAttr;

        private readonly string tokenTypeString;

        private int finalOffset = 0;

        public UIMAAnnotationsTokenizer(string descriptorPath, string tokenType, IDictionary<string, object> configurationParameters, TextReader input)
            : this(descriptorPath, tokenType, configurationParameters, AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input)
        {
        }

        public UIMAAnnotationsTokenizer(string descriptorPath, string tokenType, IDictionary<string, object> configurationParameters,
                                        AttributeFactory factory, TextReader input)
            : base(factory, input, descriptorPath, configurationParameters)
        {
            this.tokenTypeString = tokenType;
            this.termAttr = AddAttribute<ICharTermAttribute>();
            this.offsetAttr = AddAttribute<IOffsetAttribute>();
        }

        protected override void InitializeIterator() //throws IOException
        {
            try
            {
                AnalyzeInput();
            }
            catch (AnalysisEngineProcessException e)
            {
                throw new IOException(e.ToString(), e);
            }
            catch (ResourceInitializationException e)
            {
                throw new IOException(e.ToString(), e);
            }
            finalOffset = CorrectOffset(cas.getDocumentText().Length);
            org.apache.uima.cas.Type tokenType = cas.getTypeSystem().getType(tokenTypeString);
            iterator = cas.getAnnotationIndex(tokenType).iterator();
        }

        public override bool IncrementToken() //throws IOException
        {
            if (iterator == null)
            {
                InitializeIterator();
            }
            if (iterator.hasNext())
            {
                ClearAttributes();
                AnnotationFS next = (AnnotationFS)iterator.next();
                termAttr.Append(next.getCoveredText());
                offsetAttr.SetOffset(CorrectOffset(next.getBegin()), CorrectOffset(next.getEnd()));
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void End() //throws IOException
        {
            base.End();
            offsetAttr.SetOffset(finalOffset, finalOffset);
        }
    }
}

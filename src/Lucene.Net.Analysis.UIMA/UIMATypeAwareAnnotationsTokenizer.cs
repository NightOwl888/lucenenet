using Lucene.Net.Analysis.TokenAttributes;
using org.apache.uima.analysis_engine;
using org.apache.uima.cas;
using org.apache.uima.cas.text;
using org.apache.uima.resource;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Uima
{
    /// <summary>
    /// A <see cref="Tokenizer"/> which creates token from UIMA Annotations filling also their <see cref="ITypeAttribute"/> according to
    /// <see cref="org.apache.uima.cas.FeaturePath"/>s specified
    /// </summary>
    public sealed class UIMATypeAwareAnnotationsTokenizer : BaseUIMATokenizer
    {
        private readonly ITypeAttribute typeAttr;
        private readonly ICharTermAttribute termAttr;
        private readonly IOffsetAttribute offsetAttr;
        private readonly string tokenTypeString;
        private readonly string typeAttributeFeaturePath;
        private FeaturePath featurePath;
        private int finalOffset = 0;

        public UIMATypeAwareAnnotationsTokenizer(string descriptorPath, string tokenType, string typeAttributeFeaturePath, IDictionary<string, object> configurationParameters, TextReader input)
            : this(descriptorPath, tokenType, typeAttributeFeaturePath, configurationParameters, AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input)
        {
        }

        public UIMATypeAwareAnnotationsTokenizer(string descriptorPath, string tokenType, string typeAttributeFeaturePath,
                                                 IDictionary<string, object> configurationParameters, AttributeFactory factory, TextReader input)
            : base(factory, input, descriptorPath, configurationParameters)
        {
            this.tokenTypeString = tokenType;
            this.termAttr = AddAttribute<ICharTermAttribute>();
            this.typeAttr = AddAttribute<ITypeAttribute>();
            this.offsetAttr = AddAttribute<IOffsetAttribute>();
            this.typeAttributeFeaturePath = typeAttributeFeaturePath;
        }

        protected override void InitializeIterator()
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
            featurePath = cas.createFeaturePath();
            try
            {
                featurePath.initialize(typeAttributeFeaturePath);
            }
            catch (CASException e)
            {
                featurePath = null;
                throw new IOException(e.ToString(), e);
            }
            finalOffset = CorrectOffset(cas.getDocumentText().Length);
            org.apache.uima.cas.Type tokenType = cas.getTypeSystem().getType(tokenTypeString);
            iterator = cas.getAnnotationIndex(tokenType).iterator();

        }

        public override bool IncrementToken()
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
                typeAttr.Type = featurePath.getValueAsString(next);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void End()
        {
            base.End();
            offsetAttr.SetOffset(finalOffset, finalOffset);
        }
    }
}

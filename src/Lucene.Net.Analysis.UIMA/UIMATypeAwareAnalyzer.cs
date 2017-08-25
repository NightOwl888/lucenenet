using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Uima
{
    /// <summary>
    /// <see cref="Analyzer"/> which uses the <see cref="UIMATypeAwareAnnotationsTokenizer"/> for the tokenization phase
    /// </summary>
    public sealed class UIMATypeAwareAnalyzer : Analyzer
    {
        private readonly string descriptorPath;
        private readonly string tokenType;
        private readonly string featurePath;
        private readonly IDictionary<string, object> configurationParameters;

        public UIMATypeAwareAnalyzer(string descriptorPath, string tokenType, string featurePath, IDictionary<string, object> configurationParameters)
        {
            this.descriptorPath = descriptorPath;
            this.tokenType = tokenType;
            this.featurePath = featurePath;
            this.configurationParameters = configurationParameters;
        }

        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            return new TokenStreamComponents(new UIMATypeAwareAnnotationsTokenizer(descriptorPath, tokenType, featurePath, configurationParameters, reader));
        }
    }
}

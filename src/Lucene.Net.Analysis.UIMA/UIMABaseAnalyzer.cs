using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Uima
{
    /// <summary>
    /// An <see cref="Analyzer"/> which use the <see cref="UIMAAnnotationsTokenizer"/> for creating tokens
    /// </summary>
    public sealed class UIMABaseAnalyzer : Analyzer
    {
        private readonly string descriptorPath;
        private readonly string tokenType;
        private readonly IDictionary<string, object> configurationParameters;

        public UIMABaseAnalyzer(string descriptorPath, string tokenType, IDictionary<string, object> configurationParameters)
        {
            this.descriptorPath = descriptorPath;
            this.tokenType = tokenType;
            this.configurationParameters = configurationParameters;
        }

        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            return new TokenStreamComponents(new UIMAAnnotationsTokenizer(descriptorPath, tokenType, configurationParameters, reader));
        }
    }
}

using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Uima
{
    /// <summary>
    /// <see cref="TokenizerFactory"/> for <see cref="UIMATypeAwareAnnotationsTokenizer"/>.
    /// </summary>
    public class UIMATypeAwareAnnotationsTokenizerFactory : TokenizerFactory
    {
        private string descriptorPath;
        private string tokenType;
        private string featurePath;
        private readonly IDictionary<string, object> configurationParameters = new Dictionary<string, object>();

        /// <summary>
        /// Creates a new <see cref="UIMATypeAwareAnnotationsTokenizerFactory"/>
        /// </summary>
        public UIMATypeAwareAnnotationsTokenizerFactory(IDictionary<string, string> args)
            : base(args)
        {
            featurePath = Require(args, "featurePath");
            tokenType = Require(args, "tokenType");
            descriptorPath = Require(args, "descriptorPath");
            foreach (var arg in args)
            {
                configurationParameters[arg.Key] = arg.Value;
            }
        }

        public override Tokenizer Create(AttributeSource.AttributeFactory factory, TextReader input)
        {
            return new UIMATypeAwareAnnotationsTokenizer
                (descriptorPath, tokenType, featurePath, configurationParameters, factory, input);
        }
    }
}

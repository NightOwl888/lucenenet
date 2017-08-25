using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Analysis.Uima
{
    /// <summary>
    /// <see cref="TokenizerFactory"/> for <see cref="UIMAAnnotationsTokenizer"/>
    /// </summary>
    public class UIMAAnnotationsTokenizerFactory : TokenizerFactory
    {
        private string descriptorPath;
        private string tokenType;
        private readonly IDictionary<string, object> configurationParameters = new Dictionary<string, object>();

        /// <summary>
        /// Creates a new <see cref="UIMAAnnotationsTokenizerFactory"/>
        /// </summary>
        /// <param name="args"></param>
        public UIMAAnnotationsTokenizerFactory(IDictionary<string, string> args)
            : base(args)
        {
            tokenType = Require(args, "tokenType");
            descriptorPath = Require(args, "descriptorPath");
            foreach (var arg in args)
            {
                configurationParameters[arg.Key] = arg.Value;
            }
        }

  public override Tokenizer Create(AttributeSource.AttributeFactory factory, TextReader input)
        {
            return new UIMAAnnotationsTokenizer(descriptorPath, tokenType, configurationParameters, factory, input);
        }
    }
}

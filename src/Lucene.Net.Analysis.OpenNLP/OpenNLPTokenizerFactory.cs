using Lucene.Net.Analysis.OpenNlp.Tools;
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AttributeFactory = Lucene.Net.Util.AttributeSource.AttributeFactory;

namespace Lucene.Net.Analysis.OpenNlp
{
    /// <summary>
    /// Factory for <see cref="OpenNLPTokenizer"/>.
    /// <code>
    /// &lt;fieldType name="text_opennlp" class="solr.TextField" positionIncrementGap="100"
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.OpenNLPTokenizerFactory" sentenceModel="filename" tokenizerModel="filename"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    /// <since>7.3.0</since>
    public class OpenNLPTokenizerFactory : TokenizerFactory, IResourceLoaderAware
    {
        public const string SENTENCE_MODEL = "sentenceModel";
        public const string TOKENIZER_MODEL = "tokenizerModel";

        private readonly string sentenceModelFile;
        private readonly string tokenizerModelFile;

        public OpenNLPTokenizerFactory(IDictionary<string, string> args)
            : base(args)
        {
            sentenceModelFile = Require(args, SENTENCE_MODEL);
            tokenizerModelFile = Require(args, TOKENIZER_MODEL);
            if (args.Any())
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override Tokenizer Create(AttributeFactory factory, TextReader reader)
        {
            try
            {
                NLPSentenceDetectorOp sentenceOp = OpenNLPOpsFactory.GetSentenceDetector(sentenceModelFile);
                NLPTokenizerOp tokenizerOp = OpenNLPOpsFactory.GetTokenizer(tokenizerModelFile);
                return new OpenNLPTokenizer(factory, reader, sentenceOp, tokenizerOp);
            }
            catch (IOException e)
            {
                throw new Exception(e.ToString(), e);
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            // register models in cache with file/resource names
            if (sentenceModelFile != null)
            {
                OpenNLPOpsFactory.GetSentenceModel(sentenceModelFile, loader);
            }
            if (tokenizerModelFile != null)
            {
                OpenNLPOpsFactory.GetTokenizerModel(tokenizerModelFile, loader);
            }
        }
    }
}

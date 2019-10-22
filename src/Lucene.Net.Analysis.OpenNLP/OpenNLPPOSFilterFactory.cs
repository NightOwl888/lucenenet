using Lucene.Net.Analysis.OpenNlp.Tools;
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lucene.Net.Analysis.OpenNlp
{
    /// <summary>
    /// Factory for <see cref="OpenNLPPOSFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_opennlp_pos" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.OpenNLPTokenizerFactory" sentenceModel="filename" tokenizerModel="filename"/&gt;
    ///     &lt;filter class="solr.OpenNLPPOSFilterFactory" posTaggerModel="filename"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    /// <since>7.3.0</since>
    public class OpenNLPPOSFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        public const string POS_TAGGER_MODEL = "posTaggerModel";

        private readonly string posTaggerModelFile;

        public OpenNLPPOSFilterFactory(IDictionary<string, string> args)
                  : base(args)
        {
            posTaggerModelFile = Require(args, POS_TAGGER_MODEL);
            if (args.Any())
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            try
            {
                return new OpenNLPPOSFilter(input, OpenNLPOpsFactory.GetPOSTagger(posTaggerModelFile));
            }
            catch (IOException e)
            {
                throw new ArgumentException(e.ToString(), e);
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            try
            { // load and register the read-only model in cache with file/resource name
                OpenNLPOpsFactory.GetPOSTaggerModel(posTaggerModelFile, loader);
            }
            catch (IOException e)
            {
                throw new ArgumentException(e.ToString(), e);
            }
        }
    }
}

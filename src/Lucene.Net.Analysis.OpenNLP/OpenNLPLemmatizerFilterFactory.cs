using Lucene.Net.Analysis.OpenNlp.Tools;
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lucene.Net.Analysis.OpenNlp
{
    /// <summary>
    /// Factory for <see cref="OpenNLPLemmatizerFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_opennlp_lemma" class="solr.TextField" positionIncrementGap="100"
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.OpenNLPTokenizerFactory"
    ///                sentenceModel="filename"
    ///                tokenizerModel="filename"/&gt;
    ///     /&gt;
    ///     &lt;filter class="solr.OpenNLPLemmatizerFilterFactory"
    ///             dictionary="filename"
    ///             lemmatizerModel="filename"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    public class OpenNLPLemmatizerFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        public static readonly string DICTIONARY = "dictionary";
        public static readonly string LEMMATIZER_MODEL = "lemmatizerModel";

        private readonly string dictionaryFile;
        private readonly string lemmatizerModelFile;

        public OpenNLPLemmatizerFilterFactory(IDictionary<string, string> args)
                  : base(args)
        {
            dictionaryFile = Get(args, DICTIONARY);
            lemmatizerModelFile = Get(args, LEMMATIZER_MODEL);

            if (dictionaryFile == null && lemmatizerModelFile == null)
            {
                throw new ArgumentException("Configuration Error: missing parameter: at least one of '"
                    + DICTIONARY + "' and '" + LEMMATIZER_MODEL + "' must be provided.");
            }

            if (args.Any())
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            try
            {
                NLPLemmatizerOp lemmatizerOp = OpenNLPOpsFactory.GetLemmatizer(dictionaryFile, lemmatizerModelFile);
                return new OpenNLPLemmatizerFilter(input, lemmatizerOp);
            }
            catch (IOException e)
            {
                throw new Exception(e.ToString(), e);
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            // register models in cache with file/resource names
            if (dictionaryFile != null)
            {
                OpenNLPOpsFactory.GetLemmatizerDictionary(dictionaryFile, loader);
            }
            if (lemmatizerModelFile != null)
            {
                OpenNLPOpsFactory.GetLemmatizerModel(lemmatizerModelFile, loader);
            }
        }
    }
}

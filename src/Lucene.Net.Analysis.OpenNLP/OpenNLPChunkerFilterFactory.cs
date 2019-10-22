using Lucene.Net.Analysis.OpenNlp.Tools;
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lucene.Net.Analysis.OpenNlp
{
    /// <summary>
    /// Factory for <see cref="OpenNLPChunkerFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_opennlp_chunked" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.OpenNLPTokenizerFactory" sentenceModel="filename" tokenizerModel="filename"/&gt;
    ///     &lt;filter class="solr.OpenNLPPOSFilterFactory" posTaggerModel="filename"/&gt;
    ///     &lt;filter class="solr.OpenNLPChunkerFilterFactory" chunkerModel="filename"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    /// <since>7.3.0</since>
    public class OpenNLPChunkerFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        public static readonly string CHUNKER_MODEL = "chunkerModel";

        private readonly string chunkerModelFile;

        public OpenNLPChunkerFilterFactory(IDictionary<string, string> args)
                  : base(args)
        {
            chunkerModelFile = Get(args, CHUNKER_MODEL);
            if (args.Any())
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            try
            {
                NLPChunkerOp chunkerOp = null;

                if (chunkerModelFile != null)
                {
                    chunkerOp = OpenNLPOpsFactory.GetChunker(chunkerModelFile);
                }
                return new OpenNLPChunkerFilter(input, chunkerOp);
            }
            catch (IOException e)
            {
                throw new ArgumentException(e.ToString(), e);
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            try
            {
                // load and register read-only models in cache with file/resource names
                if (chunkerModelFile != null)
                {
                    OpenNLPOpsFactory.GetChunkerModel(chunkerModelFile, loader);
                }
            }
            catch (IOException e)
            {
                throw new ArgumentException(e.ToString(), e);
            }
        }
    }
}

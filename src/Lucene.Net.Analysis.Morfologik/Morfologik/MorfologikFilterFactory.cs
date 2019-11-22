using Lucene.Net.Analysis.Util;
using Morfologik.Stemming;
using Morfologik.Stemming.Polish;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Morfologik
{
    /// <summary>
    /// Filter factory for <see cref="MorfologikFilter"/>.
    /// <para/>
    /// An explicit resource name of the dictionary (<c>".dict"</c>) can be 
    /// provided via the <code>dictionary</code> attribute, as the example below demonstrates:
    /// <code>
    /// &lt;fieldType name="text_mylang" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.MorfologikFilterFactory" dictionary="mylang.dict" /&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// <para/>
    /// If the dictionary attribute is not provided, the Polish dictionary is loaded
    /// and used by default.
    /// <para/>
    /// See: <a href="http://morfologik.blogspot.com/">Morfologik web site</a>
    /// </summary>
    /// <since>4.0.0</since>
    public class MorfologikFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        /// <summary>Dictionary resource attribute (should have <c>".dict"</c> suffix), loaded from <see cref="IResourceLoader"/>.</summary>
        public const string DICTIONARY_ATTRIBUTE = "dictionary";

        /// <summary><see cref="DICTIONARY_ATTRIBUTE"/> value passed to <see cref="Inform(IResourceLoader)"/>.</summary>
        private string resourceName;

        /// <summary>Loaded <see cref="Dictionary"/>, initialized on <see cref="Inform(IResourceLoader)"/>.</summary>
        private Dictionary dictionary;

        /// <summary>Creates a new <see cref="MorfologikFilterFactory"/></summary>
        public MorfologikFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            // Be specific about no-longer-supported dictionary attribute.
            string DICTIONARY_RESOURCE_ATTRIBUTE = "dictionary-resource";
            string dictionaryResource = Get(args, DICTIONARY_RESOURCE_ATTRIBUTE);
            if (!string.IsNullOrEmpty(dictionaryResource))
            {
                throw new ArgumentException("The " + DICTIONARY_RESOURCE_ATTRIBUTE + " attribute is no "
                    + "longer supported. Use the '" + DICTIONARY_ATTRIBUTE + "' attribute instead (see LUCENE-6833).");
            }

            resourceName = Get(args, DICTIONARY_ATTRIBUTE);

            if (args.Count != 0)
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            if (resourceName == null)
            {
                // Get the dictionary lazily, does not hold up memory.
                this.dictionary = new PolishStemmer().Dictionary;
            }
            else
            {
                using (Stream dict = loader.OpenResource(resourceName))
                using (Stream meta = loader.OpenResource(DictionaryMetadata.GetExpectedMetadataFileName(resourceName)))
                {
                    this.dictionary = Dictionary.Read(dict, meta);
                }
            }
        }

        public override TokenStream Create(TokenStream ts)
        {
            if (this.dictionary == null)
                throw new ArgumentException("MorfologikFilterFactory was not fully initialized.");

            return new MorfologikFilter(ts, dictionary);
        }
    }
}

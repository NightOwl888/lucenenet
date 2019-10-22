using opennlp.tools.lemmatizer;
using System.Diagnostics;
using System.IO;

namespace Lucene.Net.Analysis.OpenNlp.Tools
{
    /// <summary>
    /// Supply OpenNLP Lemmatizer tools.
    /// <para/>
    /// Both a dictionary-based lemmatizer and a MaxEnt lemmatizer are supported.
    /// If both are configured, the dictionary-based lemmatizer is tried first,
    /// and then the MaxEnt lemmatizer is consulted for out-of-vocabulary tokens.
    /// <para/>
    /// The MaxEnt implementation requires binary models from OpenNLP project on SourceForge.
    /// </summary>
    public class NLPLemmatizerOp
    {
        private readonly DictionaryLemmatizer dictionaryLemmatizer;
        private readonly LemmatizerME lemmatizerME;

        public NLPLemmatizerOp(Stream dictionary, LemmatizerModel lemmatizerModel)
        {
            Debug.Assert(dictionary != null || lemmatizerModel != null, "At least one parameter must be non-null");
            dictionaryLemmatizer = dictionary == null ? null : new DictionaryLemmatizer(new ikvm.io.InputStreamWrapper(dictionary));
            lemmatizerME = lemmatizerModel == null ? null : new LemmatizerME(lemmatizerModel);
        }

        public virtual string[] Lemmatize(string[] words, string[] postags)
        {
            string[] lemmas = null;
            string[] maxEntLemmas = null;
            if (dictionaryLemmatizer != null)
            {
                lemmas = dictionaryLemmatizer.lemmatize(words, postags);
                for (int i = 0; i < lemmas.Length; ++i)
                {
                    if (lemmas[i].Equals("O"))
                    {   // this word is not in the dictionary
                        if (lemmatizerME != null)
                        {  // fall back to the MaxEnt lemmatizer if it's enabled
                            if (maxEntLemmas == null)
                            {
                                maxEntLemmas = lemmatizerME.lemmatize(words, postags);
                            }
                            if ("_".Equals(maxEntLemmas[i]))
                            {
                                lemmas[i] = words[i];    // put back the original word if no lemma is found
                            }
                            else
                            {
                                lemmas[i] = maxEntLemmas[i];
                            }
                        }
                        else
                        {                     // there is no MaxEnt lemmatizer
                            lemmas[i] = words[i];      // put back the original word if no lemma is found
                        }
                    }
                }
            }
            else
            {                           // there is only a MaxEnt lemmatizer
                maxEntLemmas = lemmatizerME.lemmatize(words, postags);
                for (int i = 0; i < maxEntLemmas.Length; ++i)
                {
                    if ("_".Equals(maxEntLemmas[i]))
                    {
                        maxEntLemmas[i] = words[i];  // put back the original word if no lemma is found
                    }
                }
                lemmas = maxEntLemmas;
            }
            return lemmas;
        }
    }
}

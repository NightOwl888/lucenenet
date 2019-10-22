using opennlp.tools.tokenize;
using opennlp.tools.util;

namespace Lucene.Net.Analysis.OpenNlp.Tools
{
    /// <summary>
    /// Supply OpenNLP Sentence Tokenizer tool.
    /// Requires binary models from OpenNLP project on SourceForge.
    /// </summary>
    public class NLPTokenizerOp
    {
        private readonly TokenizerME tokenizer;

        public NLPTokenizerOp(TokenizerModel model)
        {
            tokenizer = new TokenizerME(model);
        }

        public NLPTokenizerOp()
        {
            tokenizer = null;
        }

        public virtual Span[] GetTerms(string sentence)
        {
            lock (this)
            {
                if (tokenizer == null)
                {
                    Span[] span1 = new Span[1];
                    span1[0] = new Span(0, sentence.Length);
                    return span1;
                }
                return tokenizer.tokenizePos(sentence);
            }
        }
    }
}

using opennlp.tools.postag;

namespace Lucene.Net.Analysis.OpenNlp.Tools
{
    /// <summary>
    /// Supply OpenNLP Parts-Of-Speech Tagging tool.
    /// Requires binary models from OpenNLP project on SourceForge.
    /// </summary>
    public class NLPPOSTaggerOp
    {
        private POSTagger tagger = null;

        public NLPPOSTaggerOp(POSModel model)
        {
            tagger = new POSTaggerME(model);
        }

        public virtual string[] GetPOSTags(string[] words)
        {
            lock (this)
            {
                return tagger.tag(words);
            }
        }
    }
}

using opennlp.tools.chunker;


namespace Lucene.Net.Analysis.OpenNlp.Tools
{
    /// <summary>
    /// Supply OpenNLP Chunking tool
    /// Requires binary models from OpenNLP project on SourceForge.
    /// </summary>
    public class NLPChunkerOp
    {
        private ChunkerME chunker = null;

        public NLPChunkerOp(ChunkerModel chunkerModel) 
        {
            chunker = new ChunkerME(chunkerModel);
        }

        public virtual string[] GetChunks(string[] words, string[] tags, double[] probs)
        {
            lock (this)
            {
                string[] chunks = chunker.chunk(words, tags);
                if (probs != null)
                    chunker.probs(probs);
                return chunks;
            }
        }
    }
}

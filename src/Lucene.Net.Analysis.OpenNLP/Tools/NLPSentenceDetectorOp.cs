using opennlp.tools.sentdetect;
using opennlp.tools.util;

namespace Lucene.Net.Analysis.OpenNlp.Tools
{
    /// <summary>
    /// Supply OpenNLP Sentence Detector tool.
    /// Requires binary models from OpenNLP project on SourceForge.
    /// </summary>
    public class NLPSentenceDetectorOp
    {
        private readonly SentenceDetectorME sentenceSplitter;

        public NLPSentenceDetectorOp(SentenceModel model)
        {
            sentenceSplitter = new SentenceDetectorME(model);
        }

        public NLPSentenceDetectorOp()
        {
            sentenceSplitter = null;
        }

        public virtual Span[] SplitSentences(string line)
        {
            lock (this)
            {
                if (sentenceSplitter != null)
                {
                    return sentenceSplitter.sentPosDetect(line);
                }
                else
                {
                    Span[] shorty = new Span[1];
                    shorty[0] = new Span(0, line.Length);
                    return shorty;
                }
            }
        }
    }
}

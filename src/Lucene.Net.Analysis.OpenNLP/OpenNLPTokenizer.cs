using Lucene.Net.Analysis.OpenNlp.Tools;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using opennlp.tools.util;
using System;
using System.IO;

namespace Lucene.Net.Analysis.OpenNlp
{
    /// <summary>
    /// Run OpenNLP SentenceDetector and <see cref="Tokenizer"/>.
    /// The last token in each sentence is marked by setting the <see cref="EOS_FLAG_BIT"/> in the <see cref="IFlagsAttribute"/>;
    /// following filters can use this information to apply operations to tokens one sentence at a time.
    /// </summary>
    public sealed class OpenNLPTokenizer : SegmentingTokenizerBase
    {
        public static int EOS_FLAG_BIT = 1;

        private readonly ICharTermAttribute termAtt;
        private readonly IFlagsAttribute flagsAtt;
        private readonly IOffsetAttribute offsetAtt;

        private Span[] termSpans = null;
        private int termNum = 0;
        private int sentenceStart = 0;

        private NLPSentenceDetectorOp sentenceOp = null;
        private NLPTokenizerOp tokenizerOp = null;

        /// <summary>
        /// Creates a new <see cref="OpenNLPTokenizer"/> </summary>
        public OpenNLPTokenizer(TextReader reader, NLPSentenceDetectorOp sentenceOp, NLPTokenizerOp tokenizerOp) // LUCENENET 4.8.0 specific overload to default AttributeFactory
            : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, reader, sentenceOp, tokenizerOp)
        {
        }

        public OpenNLPTokenizer(AttributeFactory factory, TextReader reader, NLPSentenceDetectorOp sentenceOp, NLPTokenizerOp tokenizerOp) // LUCENENET: Added reader param for compatibility with 4.8 - remove when upgrading
            : base(factory, reader, new OpenNLPSentenceBreakIterator(sentenceOp))
        {
            if (sentenceOp == null || tokenizerOp == null)
            {
                throw new ArgumentException("OpenNLPTokenizer: both a Sentence Detector and a Tokenizer are required");
            }
            this.sentenceOp = sentenceOp;
            this.tokenizerOp = tokenizerOp;
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.flagsAtt = AddAttribute<IFlagsAttribute>();
            this.offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                termSpans = null;
                termNum = sentenceStart = 0;
            }
        }

        protected override void SetNextSentence(int sentenceStart, int sentenceEnd)
        {
            this.sentenceStart = sentenceStart;
            string sentenceText = new string(m_buffer, sentenceStart, sentenceEnd - sentenceStart);
            termSpans = tokenizerOp.GetTerms(sentenceText);
            termNum = 0;
        }

        protected override bool IncrementWord()
        {
            if (termSpans == null || termNum == termSpans.Length)
            {
                return false;
            }
            ClearAttributes();
            Span term = termSpans[termNum];
            termAtt.CopyBuffer(m_buffer, sentenceStart + term.getStart(), term.length());
            offsetAtt.SetOffset(CorrectOffset(m_offset + sentenceStart + term.getStart()),
                                CorrectOffset(m_offset + sentenceStart + term.getEnd()));
            if (termNum == termSpans.Length - 1)
            {
                flagsAtt.Flags = flagsAtt.Flags | EOS_FLAG_BIT; // mark the last token in the sentence with EOS_FLAG_BIT
            }
            ++termNum;
            return true;
        }

        public override void Reset()
        {
            base.Reset();
            termSpans = null;
            termNum = sentenceStart = 0;
        }
    }
}

using Lucene.Net.Analysis.OpenNlp.Tools;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Analysis.OpenNlp
{
    /// <summary>
    /// Run OpenNLP POS tagger.  Tags all terms in the <see cref="ITypeAttribute"/>.
    /// </summary>
    public sealed class OpenNLPPOSFilter : TokenFilter
    {
        private IList<AttributeSource> sentenceTokenAttrs = new List<AttributeSource>();
        string[] tags = null;
        private int tokenNum = 0;
        private bool moreTokensAvailable = true;

        private readonly NLPPOSTaggerOp posTaggerOp;
        private readonly ITypeAttribute typeAtt;
        private readonly IFlagsAttribute flagsAtt;
        private readonly ICharTermAttribute termAtt;

        public OpenNLPPOSFilter(TokenStream input, NLPPOSTaggerOp posTaggerOp)
                  : base(input)
        {
            this.posTaggerOp = posTaggerOp;
            this.typeAtt = AddAttribute<ITypeAttribute>();
            this.flagsAtt = AddAttribute<IFlagsAttribute>();
            this.termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override sealed bool IncrementToken()
        {
            if (!moreTokensAvailable)
            {
                Clear();
                return false;
            }
            if (tokenNum == sentenceTokenAttrs.Count)
            { // beginning of stream, or previous sentence exhausted
                string[] sentenceTokens = NextSentence();
                if (sentenceTokens == null)
                {
                    Clear();
                    return false;
                }
                tags = posTaggerOp.GetPOSTags(sentenceTokens);
                tokenNum = 0;
            }
            ClearAttributes();
            sentenceTokenAttrs[tokenNum].CopyTo(this);
            typeAtt.Type = tags[tokenNum++];
            return true;
        }

        private string[] NextSentence()
        {
            IList<string> termList = new List<string>();
            sentenceTokenAttrs.Clear();
            bool endOfSentence = false;
            while (!endOfSentence && (moreTokensAvailable = m_input.IncrementToken()))
            {
                termList.Add(termAtt.ToString());
                endOfSentence = 0 != (flagsAtt.Flags & OpenNLPTokenizer.EOS_FLAG_BIT);
                sentenceTokenAttrs.Add(m_input.CloneAttributes());
            }
            return termList.Count > 0 ? termList.ToArray() : null;
        }

        public override void Reset()
        {
            base.Reset();
            moreTokensAvailable = true;
            Clear();
        }

        private void Clear()
        {
            sentenceTokenAttrs.Clear();
            tags = null;
            tokenNum = 0;
        }
    }
}

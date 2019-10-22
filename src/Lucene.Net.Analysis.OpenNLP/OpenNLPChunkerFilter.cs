using Lucene.Net.Analysis.OpenNlp.Tools;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Analysis.OpenNlp
{
    /// <summary>
    /// Run OpenNLP chunker. Prerequisite: the <see cref="OpenNLPTokenizer"/> and <see cref="OpenNLPPOSFilter"/> must precede this filter.
    /// Tags terms in the TypeAttribute, replacing the POS tags previously put there by <see cref="OpenNLPPOSFilter"/>.
    /// </summary>
    public sealed class OpenNLPChunkerFilter : TokenFilter
    {
        private List<AttributeSource> sentenceTokenAttrs = new List<AttributeSource>();
        private int tokenNum = 0;
        private bool moreTokensAvailable = true;
        private string[] sentenceTerms = null;
        private string[] sentenceTermPOSTags = null;

        private readonly NLPChunkerOp chunkerOp;
        private readonly ITypeAttribute typeAtt;
        private readonly IFlagsAttribute flagsAtt;
        private readonly ICharTermAttribute termAtt;

        public OpenNLPChunkerFilter(TokenStream input, NLPChunkerOp chunkerOp)
                  : base(input)
        {
            this.chunkerOp = chunkerOp;
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
            {
                NextSentence();
                if (sentenceTerms == null)
                {
                    Clear();
                    return false;
                }
                AssignTokenTypes(chunkerOp.GetChunks(sentenceTerms, sentenceTermPOSTags, null));
                tokenNum = 0;
            }
            ClearAttributes();
            sentenceTokenAttrs[tokenNum++].CopyTo(this);
            return true;
        }

        private void NextSentence()
        {
            IList<string> termList = new List<string>();
            IList<string> posTagList = new List<string>();
            sentenceTokenAttrs.Clear();
            bool endOfSentence = false;
            while (!endOfSentence && (moreTokensAvailable = m_input.IncrementToken()))
            {
                termList.Add(termAtt.ToString());
                posTagList.Add(typeAtt.Type);
                endOfSentence = 0 != (flagsAtt.Flags & OpenNLPTokenizer.EOS_FLAG_BIT);
                sentenceTokenAttrs.Add(m_input.CloneAttributes());
            }
            sentenceTerms = termList.Count > 0 ? termList.ToArray() : null;
            sentenceTermPOSTags = posTagList.Count > 0 ? posTagList.ToArray() : null;
        }

        private void AssignTokenTypes(string[] tags)
        {
            for (int i = 0; i < tags.Length; ++i)
            {
                sentenceTokenAttrs[i].GetAttribute<ITypeAttribute>().Type = tags[i];
            }
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
            sentenceTerms = null;
            sentenceTermPOSTags = null;
            tokenNum = 0;
        }
    }
}

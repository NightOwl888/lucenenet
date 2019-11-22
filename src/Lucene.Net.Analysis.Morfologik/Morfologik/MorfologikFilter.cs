﻿using Lucene.Net.Analysis.Morfologik.TokenAttributes;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Morfologik.Stemming;
using Morfologik.Stemming.Polish;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Morfologik
{
    /// <summary>
    /// <see cref="TokenFilter"/> using Morfologik library to transform input tokens into lemma and
    /// morphosyntactic (POS) tokens. Applies to Polish only.
    /// <para/>
    /// MorfologikFilter contains a <see cref="MorphosyntacticTagsAttribute"/>, which provides morphosyntactic
    /// annotations for produced lemmas. See the Morfologik documentation for details.
    /// </summary>
    public class MorfologikFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAtt;
        private readonly IMorphosyntacticTagsAttribute tagsAtt;
        private readonly IPositionIncrementAttribute posIncrAtt;
        private readonly IKeywordAttribute keywordAttr;

        //private readonly CharsRefBuilder scratch = new CharsRefBuilder();
        private readonly CharsRef scratch = new CharsRef();

        private State current;
        private readonly TokenStream input;
        private readonly IStemmer stemmer;

        private IList<WordData> lemmaList;
        private readonly List<StringBuilder> tagsList = new List<StringBuilder>();

        private int lemmaListIndex;

        /// <summary>
        /// Creates a filter with the default (Polish) dictionary.
        /// </summary>
        /// <param name="input">Input token stream.</param>
        public MorfologikFilter(TokenStream input)
            : this(input, new PolishStemmer().Dictionary)
        {
        }

        /// <summary>
        /// Creates a filter with a given dictionary.
        /// </summary>
        /// <param name="input">Input token stream.</param>
        /// <param name="dict"><see cref="Dictionary"/> to use for stemming.</param>
        public MorfologikFilter(TokenStream input, Dictionary dict)
            : base(input)
        {
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.tagsAtt = AddAttribute<IMorphosyntacticTagsAttribute>();
            this.posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            this.keywordAttr = AddAttribute<IKeywordAttribute>();

            this.input = input;
            this.stemmer = new DictionaryLookup(dict);
            this.lemmaList = new List<WordData>();
        }

        /// <summary>
        /// A regex used to split lemma forms.
        /// </summary>
        private readonly static Regex lemmaSplitter = new Regex("\\+|\\|", RegexOptions.Compiled);

        private void PopNextLemma()
        {
            // One tag (concatenated) per lemma.
            WordData lemma = lemmaList[lemmaListIndex++];
            termAtt.SetEmpty().Append(lemma.GetStem().ToString());
            var tag = lemma.GetTag();
            if (tag != null)
            {
                string[] tags = lemmaSplitter.Split(tag.ToString());
                for (int i = 0; i < tags.Length; i++)
                {
                    if (tagsList.Count <= i)
                    {
                        tagsList.Add(new StringBuilder());
                    }
                    StringBuilder buffer = tagsList[i];
                    buffer.Length = 0;
                    buffer.Append(tags[i]);
                }
                tagsAtt.Tags = tagsList.SubList(0, tags.Length);
            }
            else
            {
                tagsAtt.Tags = Collections.EmptyList<StringBuilder>();
            }
        }

        /// <summary>
        /// Lookup a given surface form of a token and update
        /// <see cref="lemmaList"/> and <see cref="lemmaListIndex"/> accordingly.
        /// </summary>
        private bool LookupSurfaceForm(string token)
        {
            lemmaList = this.stemmer.Lookup(token);
            lemmaListIndex = 0;
            return lemmaList.Count > 0;
        }

        /// <summary>Retrieves the next token (possibly from the list of lemmas).</summary>
        public override sealed bool IncrementToken()
        {
            if (lemmaListIndex < lemmaList.Count)
            {
                RestoreState(current);
                posIncrAtt.PositionIncrement = 0;
                PopNextLemma();
                return true;
            }
            else if (this.input.IncrementToken())
            {
                if (!keywordAttr.IsKeyword &&
                    (LookupSurfaceForm(termAtt.ToString()) || LookupSurfaceForm(ToLowercase(termAtt.ToString()))))
                {
                    current = CaptureState();
                    PopNextLemma();
                }
                else
                {
                    tagsAtt.Clear();
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>Convert to lowercase in-place.</summary>
        private string ToLowercase(string chs)
        {
            int length = chs.Length;
            scratch.Length = length;
            scratch.Grow(length);

            char[] buffer = scratch.Chars;
            for (int i = 0; i < length;)
            {
                i += Character.ToChars(
                    Character.ToLower(Character.CodePointAt(chs, i)), buffer, i);
            }

            return scratch.ToString();
        }

        /// <summary>Resets stems accumulator and hands over to superclass.</summary>
        public override void Reset()
        {
            lemmaListIndex = 0;
            lemmaList = new List<WordData>();
            tagsList.Clear();
            base.Reset();
        }
    }
}

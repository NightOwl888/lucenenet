// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.Globalization;
using System.Text;

namespace Lucene.Net.Analysis.Ko
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// A <see cref="TokenFilter"/> that normalizes Korean numbers to regular Arabic
    /// decimal numbers in half-width characters.
    /// <para/>
    /// Korean numbers are often written using a combination of Hangul and Arabic numbers with
    /// various kinds punctuation. For example, ３．２천 means 3200. This filter does this kind
    /// of normalization and allows a search for 3200 to match ３．２천 in text, but can also be
    /// used to make range facets based on the normalized numbers and so on.
    /// <para/>
    /// Notice that this analyzer uses a token composition scheme and relies on punctuation
    /// tokens being found in the token stream. Please make sure your <see cref="KoreanTokenizer"/>
    /// has <c>discardPunctuation</c> set to false. In case punctuation characters, such as ．
    /// (U+FF0E FULLWIDTH FULL STOP), is removed from the token stream, this filter would find
    /// input tokens tokens ３ and ２천 and give outputs 3 and 2000 instead of 3200, which is
    /// likely not the intended result. If you want to remove punctuation characters from your
    /// index that are not part of normalized numbers, add a
    /// <see cref="Core.StopFilter"/> with the punctuation you wish to
    /// remove after <see cref="KoreanNumberFilter"/> in your analyzer chain.
    /// <para/>
    /// Below are some examples of normalizations this filter supports. The input is untokenized
    /// text and the result is the single term attribute emitted for the input.
    /// <list type="bullet">
    ///     <item><description>영영칠 becomes 7</description></item>
    ///     <item><description>일영영영 becomes 1000</description></item>
    ///     <item><description>삼천2백2십삼 becomes 3223</description></item>
    ///     <item><description>조육백만오천일 becomes 1000006005001</description></item>
    ///     <item><description>３.２천 becomes 3200</description></item>
    ///     <item><description>１.２만３４５.６７ becomes 12345.67</description></item>
    ///     <item><description>4,647.100 becomes 4647.1</description></item>
    ///     <item><description>15,7 becomes 157 (be aware of this weakness)</description></item>
    /// </list>
    /// <para/>
    /// Tokens preceded by a token with <see cref="IPositionIncrementAttribute"/> of zero are left
    /// left untouched and emitted as-is.
    /// <para/>
    /// This filter does not use any part-of-speech information for its normalization and
    /// the motivation for this is to also support n-grammed token streams in the future.
    /// <para/>
    /// This filter may in some cases normalize tokens that are not numbers in their context.
    /// For example, is 전중경일 is a name and means Tanaka Kyōichi, but 경일 (Kyōichi) out of
    /// context can strictly speaking also represent the number 10000000000000001. This filter
    /// respects the <see cref="KeywordAttribute"/>, which can be used to prevent specific
    /// normalizations from happening.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class KoreanNumberFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAttr;
        private readonly IOffsetAttribute offsetAttr;
        private readonly IKeywordAttribute keywordAttr;
        private readonly IPositionIncrementAttribute posIncrAttr;
        private readonly IPositionLengthAttribute posLengthAttr;

        private const char NO_NUMERAL = char.MaxValue;

        private static readonly char[] numerals = LoadNumerals();

        private static readonly char[] exponents = LoadExponents();

        private State state;

        private StringBuilder numeral;

        private int fallThroughTokens;

        private bool exhausted = false;

        private static char[] LoadNumerals()
        {
            var numerals = new char[0x10000];
            for (int i = 0; i < numerals.Length; i++)
            {
                numerals[i] = NO_NUMERAL;
            }
            numerals['영'] = (char)0; // 영 U+C601 0
            numerals['일'] = (char)1; // 일 U+C77C 1
            numerals['이'] = (char)2; // 이 U+C774 2
            numerals['삼'] = (char)3; // 삼 U+C0BC 3
            numerals['사'] = (char)4; // 사 U+C0AC 4
            numerals['오'] = (char)5; // 오 U+C624 5
            numerals['육'] = (char)6; // 육 U+C721 6
            numerals['칠'] = (char)7; // 칠 U+CE60 7
            numerals['팔'] = (char)8; // 팔 U+D314 8
            numerals['구'] = (char)9; // 구 U+AD6C 9

            return numerals;
        }

        private static char[] LoadExponents()
        {
            var exponents = new char[0x10000];
            for (int i = 0; i < exponents.Length; i++)
            {
                exponents[i] = (char)0;
            }
            exponents['십'] = (char)1;  // 십 U+C2ED 10
            exponents['백'] = (char)2;  // 백 U+BC31 100
            exponents['천'] = (char)3;  // 천 U+CC9C 1,000
            exponents['만'] = (char)4;  // 만 U+B9CC 10,000
            exponents['억'] = (char)8;  // 억 U+C5B5 100,000,000
            exponents['조'] = (char)12; // 조 U+C870 1,000,000,000,000
            exponents['경'] = (char)16; // 경 U+ACBD 10,000,000,000,000,000
            exponents['해'] = (char)20; // 해 U+D574 100,000,000,000,000,000,000

            return exponents;
        }

        public KoreanNumberFilter(TokenStream input)
            : base(input)
        {
            this.termAttr = AddAttribute<ICharTermAttribute>();
            this.offsetAttr = AddAttribute<IOffsetAttribute>();
            this.keywordAttr = AddAttribute<IKeywordAttribute>();
            this.posIncrAttr = AddAttribute<IPositionIncrementAttribute>();
            this.posLengthAttr = AddAttribute<IPositionLengthAttribute>();
        }

        public override sealed bool IncrementToken()
        {

            // Emit previously captured token we read past earlier
            if (state != null)
            {
                RestoreState(state);
                state = null;
                return true;
            }

            if (exhausted)
            {
                return false;
            }

            if (!m_input.IncrementToken())
            {
                exhausted = true;
                return false;
            }

            if (keywordAttr.IsKeyword)
            {
                return true;
            }

            if (fallThroughTokens > 0)
            {
                fallThroughTokens--;
                return true;
            }

            if (posIncrAttr.PositionIncrement == 0)
            {
                fallThroughTokens = posLengthAttr.PositionLength - 1;
                return true;
            }

            bool moreTokens = true;
            bool composedNumberToken = false;
            int startOffset = 0;
            int endOffset = 0;
            State preCompositionState = CaptureState();
            string term = termAttr.ToString();
            bool numeralTerm = IsNumeral(term);

            while (moreTokens && numeralTerm)
            {

                if (!composedNumberToken)
                {
                    startOffset = offsetAttr.StartOffset;
                    composedNumberToken = true;
                }

                endOffset = offsetAttr.EndOffset;
                moreTokens = m_input.IncrementToken();
                if (moreTokens == false)
                {
                    exhausted = true;
                }

                if (posIncrAttr.PositionIncrement == 0)
                {
                    // This token is a stacked/synonym token, capture number of tokens "under" this token,
                    // except the first token, which we will emit below after restoring state
                    fallThroughTokens = posLengthAttr.PositionLength - 1;
                    state = CaptureState();
                    RestoreState(preCompositionState);
                    return moreTokens;
                }

                numeral.Append(term);

                if (moreTokens)
                {
                    term = termAttr.ToString();
                    numeralTerm = IsNumeral(term) || IsNumeralPunctuation(term);
                }
            }

            if (composedNumberToken)
            {
                if (moreTokens)
                {
                    // We have read past all numerals and there are still tokens left, so
                    // capture the state of this token and emit it on our next incrementToken()
                    state = CaptureState();
                }

                string normalizedNumber = NormalizeNumber(numeral.ToString());

                termAttr.SetEmpty();
                termAttr.Append(normalizedNumber);
                offsetAttr.SetOffset(startOffset, endOffset);

                numeral = new StringBuilder();
                return true;
            }
            return moreTokens;
        }

        public override void Reset()
        {
            base.Reset();
            fallThroughTokens = 0;
            numeral = new StringBuilder();
            state = null;
            exhausted = false;
        }

        /// <summary>
        /// Normalizes a Korean number
        /// </summary>
        /// <param name="number">Number or normalize.</param>
        /// <returns>Normalized number, or number to normalize on error (no op).</returns>
        public string NormalizeNumber(string number)
        {
            try
            {
                decimal? normalizedNumber = ParseNumber(new NumberBuffer(number));
                if (normalizedNumber == null)
                {
                    return number;
                }
                //return normalizedNumber.Value.ToString("J", new DecimalFormatter()); // .stripTrailingZeros().toPlainString();
                //return string.Format(new DecimalFormatter(), "{0:J}", normalizedNumber.Value);
                //return normalizedNumber.Value.ToString(CultureInfo.InvariantCulture);

                //var numberFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
                //numberFormat.NumberDecimalDigits = 0;
                //numberFormat.CurrencyDecimalDigits = 0;
                //numberFormat.PercentDecimalDigits = 0;

                return normalizedNumber.Value.ToString("0.############################", CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                // Return the source number in case of error, i.e. malformed input
                return number;
            }
            catch (ArithmeticException)
            {
                return number;
            }
        }

        /// <summary>
        /// Parses a Korean number.
        /// </summary>
        /// <param name="buffer">Buffer to parse.</param>
        /// <returns>Parsed number, or <c>null</c> on error or end of input.</returns>
        private decimal? ParseNumber(NumberBuffer buffer)
        {
            decimal sum = decimal.Zero;
            decimal? result = ParseLargePair(buffer);

            if (result == null)
            {
                return null;
            }

            while (result != null)
            {
                sum += result.Value;
                result = ParseLargePair(buffer);
            }

            return sum;
        }

        /// <summary>
        /// Parses a pair of large numbers, i.e. large Hangul factor is 10,000（만）or larger.
        /// </summary>
        /// <param name="buffer">Buffer to parse.</param>
        /// <returns>Parsed pair, or <c>null</c> on error or end of input.</returns>
        private decimal? ParseLargePair(NumberBuffer buffer)
        {
            decimal? first = ParseMediumNumber(buffer);
            decimal? second = ParseLargeHangulNumeral(buffer);

            if (first == null && second == null)
            {
                return null;
            }

            if (second == null)
            {
                // If there's no second factor, we return the first one
                // This can happen if we our number is smaller than 10,000 (만)
                return first;
            }

            if (first == null)
            {
                // If there's no first factor, just return the second one,
                // which is the same as multiplying by 1, i.e. with 만
                return second;
            }

            return first * second;
        }

        /// <summary>
        /// Parses a "medium sized" number, typically less than 10,000（만）, but might be larger
        /// due to a larger factor from <see cref="ParseBasicNumber(NumberBuffer)"/>.
        /// </summary>
        /// <param name="buffer">Buffer to parse.</param>
        /// <returns>Parsed number, or <c>null</c> on error or end of input.</returns>
        private decimal? ParseMediumNumber(NumberBuffer buffer)
        {
            decimal? sum = decimal.Zero;
            decimal? result = ParseMediumPair(buffer);

            if (result == null)
            {
                return null;
            }

            while (result != null)
            {
                sum = sum + result.Value;
                result = ParseMediumPair(buffer);
            }

            return sum;
        }

        /// <summary>
        /// Parses a pair of "medium sized" numbers, i.e. large Hangul factor is at most 1,000（천）
        /// </summary>
        /// <param name="buffer">Buffer to parse.</param>
        /// <returns>Parsed pair, or <c>null</c> on error or end of input.</returns>
        private decimal? ParseMediumPair(NumberBuffer buffer)
        {

            decimal? first = ParseBasicNumber(buffer);
            decimal? second = ParseMediumHangulNumeral(buffer);

            if (first == null && second == null)
            {
                return null;
            }

            if (second == null)
            {
                // If there's no second factor, we return the first one
                // This can happen if we just have a plain number such as 오
                return first;
            }

            if (first == null)
            {
                // If there's no first factor, just return the second one,
                // which is the same as multiplying by 1, i.e. with 천
                return second;
            }

            // Return factors multiplied
            return first.Value * second.Value;
        }

        /// <summary>
        /// Parse a basic number, which is a sequence of Arabic numbers or a sequence or 0-9 Hangul numerals (영 to 구).
        /// </summary>
        /// <param name="buffer">Buffer to parse.</param>
        /// <returns>Parsed number, or <c>null</c> on error or end of input.</returns>
        private decimal? ParseBasicNumber(NumberBuffer buffer)
        {
            StringBuilder builder = new StringBuilder();
            int i = buffer.Position;

            while (i < buffer.Length)
            {
                char c = buffer[i];

                if (IsArabicNumeral(c))
                {
                    // Arabic numerals; 0 to 9 or ０ to ９ (full-width)
                    builder.Append(ArabicNumeralValue(c));
                }
                else if (IsHangulNumeral(c))
                {
                    // Hangul numerals; 영, 일, 이, 삼, 사, 오, 육, 칠, 팔, or 구
                    builder.Append(HangulNumeralValue(c));
                }
                else if (IsDecimalPoint(c))
                {
                    builder.Append(".");
                }
                else if (IsThousandSeparator(c))
                {
                    // Just skip and move to the next character
                }
                else
                {
                    // We don't have an Arabic nor Hangul numeral, nor separation or punctuation, so we'll stop.
                    break;
                }

                i++;
                buffer.Advance();
            }

            if (builder.Length == 0)
            {
                // We didn't build anything, so we don't have a number
                return null;
            }

            //return new BigDecimal(builder.ToString());
            return decimal.Parse(builder.ToString(), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parse large Hangul numerals (ten thousands or larger).
        /// </summary>
        /// <param name="buffer">Buffer to parse.</param>
        /// <returns>Parsed number, or <c>null</c> on error or end of input.</returns>
        public decimal? ParseLargeHangulNumeral(NumberBuffer buffer)
        {
            int i = buffer.Position;

            if (i >= buffer.Length)
            {
                return null;
            }

            char c = buffer[i];
            int power = exponents[c];

            if (power > 3)
            {
                buffer.Advance();
                //return BigDecimal.TEN.pow(power);
                return (decimal)Math.Pow(10, power);
            }

            return null;
        }

        /// <summary>
        /// Parse medium Hangul numerals (tens, hundreds or thousands).
        /// </summary>
        /// <param name="buffer">Buffer to parse.</param>
        /// <returns>Parsed number or <c>null</c> on error.</returns>
        public decimal? ParseMediumHangulNumeral(NumberBuffer buffer)
        {
            int i = buffer.Position;

            if (i >= buffer.Length)
            {
                return null;
            }

            char c = buffer[i];
            int power = exponents[c];

            if (1 <= power && power <= 3)
            {
                buffer.Advance();
                //return BigDecimal.TEN.pow(power);
                return (decimal)Math.Pow(10, power);
            }

            return null;
        }

        /// <summary>
        /// Numeral predicate.
        /// </summary>
        /// <param name="input">Input string to test.</param>
        /// <returns><c>true</c> if and only if input is a numeral.</returns>
        public bool IsNumeral(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (!IsNumeral(input[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Numeral predicate.
        /// </summary>
        /// <param name="c">Character to test.</param>
        /// <returns><c>true</c> if and only if c is a numeral.</returns>
        public bool IsNumeral(char c)
        {
            return IsArabicNumeral(c) || IsHangulNumeral(c) || exponents[c] > 0;
        }

        /// <summary>
        /// Numeral punctuation predicate.
        /// </summary>
        /// <param name="input">Input string to test.</param>
        /// <returns><c>true</c> if and only if <paramref name="input"/> is a numeral punctuation string.</returns>
        public bool IsNumeralPunctuation(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (!IsNumeralPunctuation(input[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Numeral punctuation predicate.
        /// </summary>
        /// <param name="c">Character to test.</param>
        /// <returns><c>true</c> if and only if c is a numeral punctuation character.</returns>
        public bool IsNumeralPunctuation(char c)
        {
            return IsDecimalPoint(c) || IsThousandSeparator(c);
        }

        /// <summary>
        /// Arabic numeral predicate. Both half-width and full-width characters are supported.
        /// </summary>
        /// <param name="c">Character to test.</param>
        /// <returns><c>true</c> if and only if c is an Arabic numeral.</returns>
        public bool IsArabicNumeral(char c)
        {
            return IsHalfWidthArabicNumeral(c) || IsFullWidthArabicNumeral(c);
        }

        /// <summary>
        /// Arabic half-width numeral predicate.
        /// </summary>
        /// <param name="c">Character to test.</param>
        /// <returns><c>true</c> if and only if c is a half-width Arabic numeral.</returns>
        internal bool IsHalfWidthArabicNumeral(char c)
        {
            // 0 U+0030 - 9 U+0039
            return '0' <= c && c <= '9';
        }

        /// <summary>
        /// Arabic full-width numeral predicate.
        /// </summary>
        /// <param name="c">Character to test.</param>
        /// <returns><c>true</c> if and only if c is a full-width Arabic numeral.</returns>
        internal bool IsFullWidthArabicNumeral(char c)
        {
            // ０ U+FF10 - ９ U+FF19
            return '０' <= c && c <= '９';
        }

        /// <summary>
        /// Returns the numeric value for the specified character Arabic numeral.
        /// Behavior is undefined if a non-Arabic numeral is provided.
        /// </summary>
        /// <param name="c">Arabic numeral character.</param>
        /// <returns>Numeral value.</returns>
        internal int ArabicNumeralValue(char c)
        {
            int offset;
            if (IsHalfWidthArabicNumeral(c))
            {
                offset = '0';
            }
            else
            {
                offset = '０';
            }
            return c - offset;
        }

        /// <summary>
        /// Hangul numeral predicate that tests if the provided character is one of 영, 일, 이, 삼, 사, 오, 육, 칠, 팔, or 구.
        /// Larger number Hangul gives a false value.
        /// </summary>
        /// <param name="c">Character to test.</param>
        /// <returns><c>true</c> if and only is character is one of 영, 일, 이, 삼, 사, 오, 육, 칠, 팔, or 구 (0 to 9).</returns>
        internal bool IsHangulNumeral(char c)
        {
            return numerals[c] != NO_NUMERAL;
        }

        /// <summary>
        /// Returns the value for the provided Hangul numeral. Only numeric values for the characters where
        /// <see cref="IsHangulNumeral(char)"/> return <c>true</c> are supported - behavior is undefined for other characters.
        /// </summary>
        /// <param name="c">Hangul numeral character.</param>
        /// <returns>Numeral value.</returns>
        /// <seealso cref="IsHangulNumeral(char)"/>
        internal int HangulNumeralValue(char c)
        {
            return numerals[c];
        }

        /// <summary>
        /// Decimal point predicate.
        /// </summary>
        /// <param name="c">Character to test.</param>
        /// <returns><c>true</c> if and only if c is a decimal point.</returns>
        internal bool IsDecimalPoint(char c)
        {
            return c == '.'   // U+002E FULL STOP
                || c == '．'; // U+FF0E FULLWIDTH FULL STOP
        }

        /// <summary>
        /// Thousand separator predicate.
        /// </summary>
        /// <param name="c">Character to test.</param>
        /// <returns><c>true</c> if and only if c is a thousand separator predicate.</returns>
        internal bool IsThousandSeparator(char c)
        {
            return c == ','   // U+002C COMMA
                || c == '，'; // U+FF0C FULLWIDTH COMMA
        }

        /// <summary>
        /// Buffer that holds a Korean number string and a position index used as a parsed-to marker.
        /// </summary>
        public class NumberBuffer
        {
            private int position;
            private readonly string str;

            public NumberBuffer(string str)
            {
                this.str = str;
                this.position = 0;
            }

            public char this[int index] => str[index];

            public int Length => str.Length;

            public void Advance() => position++;

            public int Position => position;
        }


        private class DecimalFormatter : ICustomFormatter, IFormatProvider
        {
            

            public object GetFormat(Type formatType)
            {
                if (typeof(ICustomFormatter).Equals(formatType))
                    return this;

                return null;
            }

            public string Format(string format, object arg, IFormatProvider formatProvider)
            {
                if (!this.Equals(formatProvider))
                    return null;

                //// Set default format specifier             
                //if (string.IsNullOrEmpty(format))
                //    format = "J";

                if (!(format == "J" || format == "j"))
                    return null;

                if (arg is decimal dec)
                    return FormatDecimal(dec, CultureInfo.InvariantCulture.NumberFormat);

                return null;
            }

            private static NumberFormatInfo GetNumberFormat(IFormatProvider formatProvider)
            {
                var provider = formatProvider.GetFormat(typeof(NumberFormatInfo));
                if (provider is NumberFormatInfo)
                    return (NumberFormatInfo)provider;

                return CultureInfo.CurrentCulture.NumberFormat;
            }

            private static string FormatDecimal(decimal d, NumberFormatInfo numberFormat)
            {
                if (d % 1 == 0)
                {
                    // Special case: When we have an integer value,
                    // the standard .NET formatting removes the decimal point
                    // and everything to the right. But we need to always
                    // have at least 1 decimal place to match Java.
                    return d.ToString("0.0", numberFormat);
                }

                return d.ToString(numberFormat);
            }

            //private static string FormatDecimal(decimal dec, IFormatProvider formatProvider)
            //{
            //    var numberFormat = GetNumberFormat(formatProvider);


            //}
        }
    }
}
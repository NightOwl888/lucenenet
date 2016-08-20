using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support.Collation
{
    /// <summary>
    /// The <code>CollationElementIterator</code> class is used as an iterator
    /// to walk through each character of an international string. Use the iterator
    /// to return the ordering priority of the positioned character. The ordering
    /// priority of a character, which we refer to as a key, defines how a character
    /// is collated in the given collation object.
    /// 
    /// <p>
    /// For example, consider the following in Spanish:
    /// <blockquote>
    /// <pre>
    /// "ca" &rarr; the first key is key('c') and second key is key('a').
    /// "cha" &rarr; the first key is key('ch') and second key is key('a').
    /// </pre>
    /// </blockquote>
    /// And in German,
    /// <blockquote>
    /// <pre>
    /// "\u00e4b" &rarr; the first key is key('a'), the second key is key('e'), and
    /// the third key is key('b').
    /// </pre>
    /// </blockquote>
    /// The key of a character is an integer composed of primary order(short),
    /// secondary order(byte), and tertiary order(byte). Java strictly defines
    /// the size and signedness of its primitive data types. Therefore, the static
    /// functions <code>primaryOrder</code>, <code>secondaryOrder</code>, and
    /// <code>tertiaryOrder</code> return <code>int</code>, <code>short</code>,
    /// and <code>short</code> respectively to ensure the correctness of the key
    /// value.
    /// 
    /// <p>
    /// Example of the iterator usage,
    /// <blockquote>
    /// <pre>
    /// 
    ///  String testString = "This is a test";
    ///  Collator col = Collator.getInstance();
    ///  if (col instanceof RuleBasedCollator) {
    ///      RuleBasedCollator ruleBasedCollator = (RuleBasedCollator)col;
    ///      CollationElementIterator collationElementIterator = ruleBasedCollator.getCollationElementIterator(testString);
    ///      int primaryOrder = CollationElementIterator.primaryOrder(collationElementIterator.next());
    ///          :
    ///  }
    /// </pre>
    /// </blockquote>
    /// 
    /// <p>
    /// <code>CollationElementIterator.next</code> returns the collation order
    /// of the next character. A collation order consists of primary order,
    /// secondary order and tertiary order. The data type of the collation
    /// order is <strong>int</strong>. The first 16 bits of a collation order
    /// is its primary order; the next 8 bits is the secondary order and the
    /// last 8 bits is the tertiary order.
    /// 
    /// <p><b>Note:</b> <code>CollationElementIterator</code> is a part of
    /// <code>RuleBasedCollator</code> implementation. It is only usable
    /// with <code>RuleBasedCollator</code> instances.
    /// </summary>
    public sealed class CollationElementIterator
    {
        /// <summary>
        /// Null order which indicates the end of string is reached by the
        /// cursor.
        /// </summary>
        public readonly static uint NULLORDER = 0xffffffff;

        /// <summary>
        /// CollationElementIterator constructor.  This takes the source string and
        /// the collation object.  The cursor will walk thru the source string based
        /// on the predefined collation rules.If the source string is empty,
        /// NULLORDER will be returned on the calls to next().
        /// </summary>
        /// <param name="sourceText">the source string.</param>
        /// <param name="owner">the collation object.</param>
        internal CollationElementIterator(string sourceText, RuleBasedCollator owner)
        {
            this.owner = owner;
            ordering = owner.Tables;
            if (sourceText.Length != 0)
            {
                NormalizerBase.Mode mode =
                    CollatorUtilities.toNormalizerMode(owner.Decomposition);
                text = new NormalizerBase(sourceText, mode);
            }
        }

        /// <summary>
        /// CollationElementIterator constructor.  This takes the source string and
        /// the collation object.  The cursor will walk thru the source string based
        /// on the predefined collation rules.If the source string is empty,
        /// NULLORDER will be returned on the calls to next().
        /// </summary>
        /// <param name="sourceText">the source string.</param>
        /// <param name="owner">the collation object.</param>
        internal CollationElementIterator(CharacterIterator sourceText, RuleBasedCollator owner)
        {
            this.owner = owner;
            ordering = owner.Tables;
            NormalizerBase.Mode mode =
                CollatorUtilities.toNormalizerMode(owner.Decomposition);
            text = new NormalizerBase(sourceText, mode);
        }

        /// <summary>
        /// Resets the cursor to the beginning of the string.  The next call
        /// to next() will return the first collation element in the string.
        /// </summary>
        public void Reset()
        {
            if (text != null)
            {
                text.reset();
                NormalizerBase.Mode mode =
                    CollatorUtilities.toNormalizerMode(owner.Decomposition);
                text.setMode(mode);
            }
            buffer = null;
            expIndex = 0;
            swapOrder = 0;
        }

        /// <summary>
        /// Get the next collation element in the string.  <p>This iterator iterates
        /// over a sequence of collation elements that were built from the string.
        /// Because there isn't necessarily a one-to-one mapping from characters to
        /// collation elements, this doesn't mean the same thing as "return the
        /// collation element [or ordering priority] of the next character in the
        /// string".</p>
        /// <p>This function returns the collation element that the iterator is currently
        /// pointing to and then updates the internal pointer to point to the next element.
        /// previous() updates the pointer first and then returns the element.  This
        /// means that when you change direction while iterating (i.e., call next() and
        /// then call previous(), or call previous() and then call next()), you'll get
        /// back the same element twice.</p>
        /// </summary>
        /// <returns>the next collation element</returns>
        public int Next()
        {
            if (text == null)
            {
                return (int)NULLORDER;
            }
            NormalizerBase.Mode textMode = text.getMode();
            // convert the owner's mode to something the Normalizer understands
            NormalizerBase.Mode ownerMode =
                CollatorUtilities.toNormalizerMode(owner.Decomposition);
            if (textMode != ownerMode)
            {
                text.setMode(ownerMode);
            }

            // if buffer contains any decomposed char values
            // return their strength orders before continuing in
            // the Normalizer's CharacterIterator.
            if (buffer != null)
            {
                if (expIndex < buffer.Length)
                {
                    return StrengthOrder(buffer[expIndex++]);
                }
                else
                {
                    buffer = null;
                    expIndex = 0;
                }
            }
            else if (swapOrder != 0)
            {
                if (Character.IsSupplementaryCodePoint(swapOrder))
                {
                    char[] chars = Character.ToChars(swapOrder);
                    swapOrder = chars[1];
                    return chars[0] << 16;
                }
                int order = swapOrder << 16;
                swapOrder = 0;
                return order;
            }
            int ch = text.next();

            // are we at the end of Normalizer's text?
            if (ch == NormalizerBase.DONE)
            {
                return (int)NULLORDER;
            }

            int value = ordering.GetUnicodeOrder(ch);
            if (value == RuleBasedCollator.UNMAPPED)
            {
                swapOrder = ch;
                return UNMAPPEDCHARVALUE;
            }
            else if (value >= RuleBasedCollator.CONTRACTCHARINDEX)
            {
                value = NextContractChar(ch);
            }
            if (value >= RuleBasedCollator.EXPANDCHARINDEX)
            {
                buffer = ordering.GetExpandValueList(value);
                expIndex = 0;
                value = buffer[expIndex++];
            }

            if (ordering.IsSEAsianSwapping)
            {
                int consonant;
                if (IsThaiPreVowel(ch))
                {
                    consonant = text.next();
                    if (IsThaiBaseConsonant(consonant))
                    {
                        buffer = MakeReorderedBuffer(consonant, value, buffer, true);
                        value = buffer[0];
                        expIndex = 1;
                    }
                    else if (consonant != NormalizerBase.DONE)
                    {
                        text.previous();
                    }
                }
                if (IsLaoPreVowel(ch))
                {
                    consonant = text.next();
                    if (IsLaoBaseConsonant(consonant))
                    {
                        buffer = MakeReorderedBuffer(consonant, value, buffer, true);
                        value = buffer[0];
                        expIndex = 1;
                    }
                    else if (consonant != NormalizerBase.DONE)
                    {
                        text.previous();
                    }
                }
            }

            CompareInfo ci = new CultureInfo("th-TH").CompareInfo;

            return StrengthOrder(value);
        }

        /// <summary>
        /// Get the previous collation element in the string.  <p>This iterator iterates
        ///  over a sequence of collation elements that were built from the string.
        ///  Because there isn't necessarily a one-to-one mapping from characters to
        ///  collation elements, this doesn't mean the same thing as "return the
        ///  collation element [or ordering priority] of the previous character in the
        ///  string".</p>
        ///  <p>This function updates the iterator's internal pointer to point to the
        ///  collation element preceding the one it's currently pointing to and then
        ///  returns that element, while next() returns the current element and then
        ///  updates the pointer.  This means that when you change direction while
        ///  iterating (i.e., call next() and then call previous(), or call previous()
        ///  and then call next()), you'll get back the same element twice.</p>
        /// </summary>
        /// <returns>the previous collation element</returns>
        public int Previous()
        {
            if (text == null)
            {
                return (int)NULLORDER;
            }
            NormalizerBase.Mode textMode = text.getMode();
            // convert the owner's mode to something the Normalizer understands
            NormalizerBase.Mode ownerMode =
                CollatorUtilities.toNormalizerMode(owner.Decomposition);
            if (textMode != ownerMode)
            {
                text.setMode(ownerMode);
            }
            if (buffer != null)
            {
                if (expIndex > 0)
                {
                    return StrengthOrder(buffer[--expIndex]);
                }
                else
                {
                    buffer = null;
                    expIndex = 0;
                }
            }
            else if (swapOrder != 0)
            {
                if (Character.IsSupplementaryCodePoint(swapOrder))
                {
                    char[] chars = Character.ToChars(swapOrder);
                    swapOrder = chars[1];
                    return chars[0] << 16;
                }
                int order = swapOrder << 16;
                swapOrder = 0;
                return order;
            }
            int ch = text.previous();
            if (ch == NormalizerBase.DONE)
            {
                return (int)NULLORDER;
            }

            int value = ordering.GetUnicodeOrder(ch);

            if (value == RuleBasedCollator.UNMAPPED)
            {
                swapOrder = UNMAPPEDCHARVALUE;
                return ch;
            }
            else if (value >= RuleBasedCollator.CONTRACTCHARINDEX)
            {
                value = PrevContractChar(ch);
            }
            if (value >= RuleBasedCollator.EXPANDCHARINDEX)
            {
                buffer = ordering.GetExpandValueList(value);
                expIndex = buffer.Length;
                value = buffer[--expIndex];
            }

            if (ordering.IsSEAsianSwapping)
            {
                int vowel;
                if (IsThaiBaseConsonant(ch))
                {
                    vowel = text.previous();
                    if (IsThaiPreVowel(vowel))
                    {
                        buffer = MakeReorderedBuffer(vowel, value, buffer, false);
                        expIndex = buffer.Length - 1;
                        value = buffer[expIndex];
                    }
                    else
                    {
                        text.next();
                    }
                }
                if (IsLaoBaseConsonant(ch))
                {
                    vowel = text.previous();
                    if (IsLaoPreVowel(vowel))
                    {
                        buffer = MakeReorderedBuffer(vowel, value, buffer, false);
                        expIndex = buffer.Length - 1;
                        value = buffer[expIndex];
                    }
                    else
                    {
                        text.next();
                    }
                }
            }

            return StrengthOrder(value);
        }

        /// <summary>
        /// Return the primary component of a collation element.
        /// </summary>
        /// <param name="order">the collation element</param>
        /// <returns>the element's primary component</returns>
        public static int PrimaryOrder(int order)
        {
            uint order2 = ((uint)order & RBCollationTables.PRIMARYORDERMASK);
            return (int)(order2 >> RBCollationTables.PRIMARYORDERSHIFT);
            //order &= RBCollationTables.PRIMARYORDERMASK;
            //return (order >>> RBCollationTables.PRIMARYORDERSHIFT);
        }

        /// <summary>
        /// Return the secondary component of a collation element.
        /// </summary>
        /// <param name="order">the collation element</param>
        /// <returns>the element's secondary component</returns>
        public static short SecondaryOrder(int order)
        {
            uint order2 = ((uint)order & RBCollationTables.SECONDARYORDERMASK);
            return (short)(order2 >> RBCollationTables.SECONDARYORDERSHIFT);
            //order = order & RBCollationTables.SECONDARYORDERMASK;
            //return ((short)(order >> RBCollationTables.SECONDARYORDERSHIFT));
        }

        /// <summary>
        /// Return the tertiary component of a collation element.
        /// </summary>
        /// <param name="order">the collation element</param>
        /// <returns>the element's tertiary component</returns>
        public static short TertiaryOrder(int order)
        {
            uint order2 = (uint)order & RBCollationTables.TERTIARYORDERMASK;
            return (short)order2;
            //return ((short)(order &= RBCollationTables.TERTIARYORDERMASK));
        }

        /// <summary>
        /// Get the comparison order in the desired strength.  Ignore the other
        /// differences.
        /// </summary>
        /// <param name="order">The order value</param>
        /// <returns></returns>
        internal int StrengthOrder(int order)
        {
            CollatorStrength s = owner.Strength;
            if (s == CollatorStrength.PRIMARY)
            {
                order = (int)((uint)order & RBCollationTables.PRIMARYDIFFERENCEONLY);
            }
            else if (s == CollatorStrength.SECONDARY)
            {
                order = (int)((uint)order & RBCollationTables.SECONDARYDIFFERENCEONLY);
            }
            return order;
        }

        /// <summary>
        /// Sets the iterator to point to the collation element corresponding to
        /// the specified character (the parameter is a CHARACTER offset in the
        /// original string, not an offset into its corresponding sequence of
        /// collation elements).  The value returned by the next call to next()
        /// will be the collation element corresponding to the specified position
        /// in the text.  If that position is in the middle of a contracting
        /// character sequence, the result of the next call to next() is the
        /// collation element for that sequence.  This means that getOffset()
        /// is not guaranteed to return the same value as was passed to a preceding
        /// call to setOffset().
        /// </summary>
        /// <param name="newOffset">The new character offset into the original text.</param>
        public void SetOffset(int newOffset)
        {
            if (text != null)
            {
#pragma warning disable 612, 618 // getBeginIndex, getEndIndex and setIndex are deprecated
                if (newOffset < text.getBeginIndex()
                    || newOffset >= text.getEndIndex())
                {
                    text.setIndexOnly(newOffset);
                }
                else
                {
                    int c = text.setIndex(newOffset);

                    // if the desired character isn't used in a contracting character
                    // sequence, bypass all the backing-up logic-- we're sitting on
                    // the right character already
                    if (ordering.UsedInContractSeq(c))
                    {
                        // walk backwards through the string until we see a character
                        // that DOESN'T participate in a contracting character sequence
                        while (ordering.UsedInContractSeq(c))
                        {
                            c = text.previous();
                        }
                        // now walk forward using this object's next() method until
                        // we pass the starting point and set our current position
                        // to the beginning of the last "character" before or at
                        // our starting position
                        int last = text.getIndex();
                        while (text.getIndex() <= newOffset)
                        {
                            last = text.getIndex();
                            Next();
                        }
                        text.setIndexOnly(last);
                        // we don't need this, since last is the last index
                        // that is the starting of the contraction which encompass
                        // newOffset
                        // text.previous();
                    }
                }
#pragma warning restore 612, 618
            }
            buffer = null;
            expIndex = 0;
            swapOrder = 0;
        }

        /// <summary>
        /// Returns the character offset in the original text corresponding to the next
        /// collation element.  (That is, getOffset() returns the position in the text
        /// corresponding to the collation element that will be returned by the next
        /// call to next().)  This value will always be the index of the FIRST character
        /// corresponding to the collation element (a contracting character sequence is
        /// when two or more characters all correspond to the same collation element).
        /// This means if you do setOffset(x) followed immediately by getOffset(), getOffset()
        /// won't necessarily return x.
        /// </summary>
        /// <returns>The character offset in the original text corresponding to the collation
        /// element that will be returned by the next call to next().</returns>
        public int Offset
        {
            get { return (text != null) ? text.getIndex() : 0; }
        }

        /// <summary>
        /// Return the maximum length of any expansion sequences that end
        /// with the specified comparison order.
        /// </summary>
        /// <param name="order">a collation order returned by previous or next.</param>
        /// <returns>the maximum length of any expansion sequences ending
        /// with the specified order.</returns>
        public int GetMaxExpansion(int order)
        {
            return ordering.GetMaxExpansion(order);
        }

        /// <summary>
        /// Set a new string over which to iterate.
        /// </summary>
        /// <param name="source">the new source text</param>
        public void SetText(string source)
        {
            buffer = null;
            swapOrder = 0;
            expIndex = 0;
            NormalizerBase.Mode mode =
                CollatorUtilities.toNormalizerMode(owner.Decomposition);
            if (text == null)
            {
                text = new NormalizerBase(source, mode);
            }
            else
            {
                text.setMode(mode);
                text.setText(source);
            }
        }

        /// <summary>
        /// Set a new string over which to iterate.
        /// </summary>
        /// <param name="source">the new source text.</param>
        public void SetText(CharacterIterator source)
        {
            buffer = null;
            swapOrder = 0;
            expIndex = 0;
            NormalizerBase.Mode mode =
                CollatorUtilities.toNormalizerMode(owner.Decomposition);
            if (text == null)
            {
                text = new NormalizerBase(source, mode);
            }
            else
            {
                text.setMode(mode);
                text.setText(source);
            }
        }

        //============================================================
        // privates
        //============================================================

        /// <summary>
        /// Determine if a character is a Thai vowel (which sorts after
        /// its base consonant).
        /// </summary>
        private static bool IsThaiPreVowel(int ch)
        {
            return (ch >= 0x0e40) && (ch <= 0x0e44);
        }

        /// <summary>
        /// Determine if a character is a Thai base consonant
        /// </summary>
        private static bool IsThaiBaseConsonant(int ch)
        {
            return (ch >= 0x0e01) && (ch <= 0x0e2e);
        }

        /// <summary>
        /// Determine if a character is a Lao vowel (which sorts after
        /// its base consonant).
        /// </summary>
        private static bool IsLaoPreVowel(int ch)
        {
            return (ch >= 0x0ec0) && (ch <= 0x0ec4);
        }

        /// <summary>
        /// Determine if a character is a Lao base consonant
        /// </summary>
        private static bool IsLaoBaseConsonant(int ch)
        {
            return (ch >= 0x0e81) && (ch <= 0x0eae);
        }

        /// <summary>
        /// This method produces a buffer which contains the collation
        /// elements for the two characters, with colFirst's values preceding
        /// another character's.  Presumably, the other character precedes colFirst
        /// in logical order (otherwise you wouldn't need this method would you?).
        /// The assumption is that the other char's value(s) have already been
        /// computed.  If this char has a single element it is passed to this
        /// method as lastValue, and lastExpansion is null.  If it has an
        /// expansion it is passed in lastExpansion, and colLastValue is ignored.
        /// </summary>
        private int[] MakeReorderedBuffer(int colFirst,
                                          int lastValue,
                                          int[] lastExpansion,
                                          bool forward)
        {

            int[] result;

            int firstValue = ordering.GetUnicodeOrder(colFirst);
            if (firstValue >= RuleBasedCollator.CONTRACTCHARINDEX)
            {
                firstValue = forward ? NextContractChar(colFirst) : PrevContractChar(colFirst);
            }

            int[] firstExpansion = null;
            if (firstValue >= RuleBasedCollator.EXPANDCHARINDEX)
            {
                firstExpansion = ordering.GetExpandValueList(firstValue);
            }

            if (!forward)
            {
                int temp1 = firstValue;
                firstValue = lastValue;
                lastValue = temp1;
                int[] temp2 = firstExpansion;
                firstExpansion = lastExpansion;
                lastExpansion = temp2;
            }

            if (firstExpansion == null && lastExpansion == null)
            {
                result = new int[2];
                result[0] = firstValue;
                result[1] = lastValue;
            }
            else
            {
                int firstLength = firstExpansion == null ? 1 : firstExpansion.Length;
                int lastLength = lastExpansion == null ? 1 : lastExpansion.Length;
                result = new int[firstLength + lastLength];

                if (firstExpansion == null)
                {
                    result[0] = firstValue;
                }
                else
                {
                    System.Array.Copy(firstExpansion, 0, result, 0, firstLength);
                }

                if (lastExpansion == null)
                {
                    result[firstLength] = lastValue;
                }
                else
                {
                    System.Array.Copy(lastExpansion, 0, result, firstLength, lastLength);
                }
            }

            return result;
        }
        /**
         *  
         *  @return 
         */
        /// <summary>
        /// Check if a comparison order is ignorable.
        /// </summary>
        /// <param name="order"></param>
        /// <returns><b>true</b> if a character is ignorable; otherwise <b>false</b>.</returns>
        internal static bool IsIgnorable(int order)
        {
            return ((PrimaryOrder(order) == 0) ? true : false);
        }

        /// <summary>
        /// Get the ordering priority of the next contracting character in the
        /// string.
        /// </summary>
        /// <param name="ch">the starting character of a contracting character token</param>
        /// <returns>the next contracting character's ordering.  Returns NULLORDER
        /// if the end of string is reached.</returns>
        private int NextContractChar(int ch)
        {
            // First get the ordering of this single character,
            // which is always the first element in the list
            IList<EntryPair> list = ordering.GetContractValues(ch);
            EntryPair pair = list.First();
            int order = pair.Value;

            // find out the length of the longest contracting character sequence in the list.
            // There's logic in the builder code to make sure the longest sequence is always
            // the last.
            pair = list.Last();
            int maxLength = pair.EntryName.Length;

            // (the Normalizer is cloned here so that the seeking we do in the next loop
            // won't affect our real position in the text)
            NormalizerBase tempText = (NormalizerBase)text.clone();

            // extract the next maxLength characters in the string (we have to do this using the
            // Normalizer to ensure that our offsets correspond to those the rest of the
            // iterator is using) and store it in "fragment".
            tempText.previous();
            key.Length=0;
            int c = tempText.next();
            while (maxLength > 0 && c != NormalizerBase.DONE)
            {
                if (Character.IsSupplementaryCodePoint(c))
                {
                    key.Append(Character.ToChars(c));
                    maxLength -= 2;
                }
                else
                {
                    key.Append((char)c);
                    --maxLength;
                }
                c = tempText.next();
            }
            string fragment = key.ToString();
            // now that we have that fragment, iterate through this list looking for the
            // longest sequence that matches the characters in the actual text.  (maxLength
            // is used here to keep track of the length of the longest sequence)
            // Upon exit from this loop, maxLength will contain the length of the matching
            // sequence and order will contain the collation-element value corresponding
            // to this sequence
            maxLength = 1;
            for (int i = list.Count - 1; i > 0; i--)
            {
                pair = list.ElementAt(i);
                if (!pair.Fwd)
                    continue;

                if (fragment.StartsWith(pair.EntryName) && pair.EntryName.Length
                        > maxLength)
                {
                    maxLength = pair.EntryName.Length;
                    order = pair.Value;
                }
            }

            // seek our current iteration position to the end of the matching sequence
            // and return the appropriate collation-element value (if there was no matching
            // sequence, we're already seeked to the right position and order already contains
            // the correct collation-element value for the single character)
            while (maxLength > 1)
            {
                c = text.next();
                maxLength -= Character.CharCount(c);
            }
            return order;
        }

        /// <summary>
        /// Get the ordering priority of the previous contracting character in the
        /// string.
        /// </summary>
        /// <param name="ch">the starting character of a contracting character token</param>
        /// <returns>the next contracting character's ordering.  Returns NULLORDER
        /// if the end of string is reached.</returns>
        private int PrevContractChar(int ch)
        {
            // This function is identical to nextContractChar(), except that we've
            // switched things so that the next() and previous() calls on the Normalizer
            // are switched and so that we skip entry pairs with the fwd flag turned on
            // rather than off.  Notice that we still use append() and startsWith() when
            // working on the fragment.  This is because the entry pairs that are used
            // in reverse iteration have their names reversed already.
            IList<EntryPair> list = ordering.GetContractValues(ch);
            EntryPair pair = list.First();
            int order = pair.Value;

            pair = list.Last();
            int maxLength = pair.EntryName.Length;

            NormalizerBase tempText = (NormalizerBase)text.clone();

            tempText.next();
            key.Length = 0;
            int c = tempText.previous();
            while (maxLength > 0 && c != NormalizerBase.DONE)
            {
                if (Character.IsSupplementaryCodePoint(c))
                {
                    key.Append(Character.ToChars(c));
                    maxLength -= 2;
                }
                else
                {
                    key.Append((char)c);
                    --maxLength;
                }
                c = tempText.previous();
            }
            String fragment = key.ToString();

            maxLength = 1;
            for (int i = list.Count - 1; i > 0; i--)
            {
                pair = list.ElementAt(i);
                if (pair.Fwd)
                    continue;

                if (fragment.StartsWith(pair.EntryName) && pair.EntryName.Length
                        > maxLength)
                {
                    maxLength = pair.EntryName.Length;
                    order = pair.Value;
                }
            }

            while (maxLength > 1)
            {
                c = text.previous();
                maxLength -= Character.CharCount(c);
            }
            return order;
        }

        internal readonly static int UNMAPPEDCHARVALUE = 0x7FFF0000;

        private NormalizerBase text = null;
        private int[] buffer = null;
        private int expIndex = 0;
        private StringBuilder key = new StringBuilder(5);
        private int swapOrder = 0;
        private RBCollationTables ordering;
        private RuleBasedCollator owner;
    }
}

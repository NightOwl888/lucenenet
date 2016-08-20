using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support.Collation
{
    /// <summary>
    /// Utility class for normalizing and merging patterns for collation.
    /// Patterns are strings of the form <entry>*, where <entry> has the
    /// form:
    /// <pattern> := <entry>*
    /// <entry> := <separator><chars>{"/"<extension>}
    /// <separator> := "=", ",", ";", "&lt;", "&amp;"
    /// <chars>, and <extension> are both arbitrary strings.
    /// unquoted whitespaces are ignored.
    /// 'xxx' can be used to quote characters
    /// One difference from Collator is that & is used to reset to a current
    /// point. Or, in other words, it introduces a new sequence which is to
    /// be added to the old.
    /// That is: "a &lt; b &lt; c &lt; d" is the same as "a &lt; b &amp; b &lt; c &amp; c &lt; d" OR
    /// "a &lt; b &lt; d &amp; b &lt; c"
    /// XXX: make '' be a single quote.
    /// </summary>
    internal sealed class MergeCollation
    {
        /**
     * Creates from a pattern
     * @exception ParseException If the input pattern is incorrect.
     */
        public MergeCollation(string pattern)
        {
            for (int i = 0; i < statusArray.Length; i++)
                statusArray[i] = 0;
            SetPattern(pattern);
        }

        /**
         * recovers current pattern
         */
        public string GetPattern()
        {
            return GetPattern(true);
        }

        /**
         * recovers current pattern.
         * @param withWhiteSpace puts spacing around the entries, and \n
         * before & and <
         */
        public string GetPattern(bool withWhiteSpace)
        {
            StringBuilder result = new StringBuilder();
            PatternEntry tmp = null;
            List<PatternEntry> extList = null;
            int i;
            for (i = 0; i < patterns.Count; ++i)
            {
                PatternEntry entry = patterns.ElementAtOrDefault(i);
                if (entry.extension.Length != 0)
                {
                    if (extList == null)
                        extList = new List<PatternEntry>();
                    extList.Add(entry);
                }
                else
                {
                    if (extList != null)
                    {
                        PatternEntry last = FindLastWithNoExtension(i - 1);
                        for (int j = extList.Count - 1; j >= 0; j--)
                        {
                            tmp = extList.ElementAtOrDefault(j);
                            tmp.AddToBuffer(result, false, withWhiteSpace, last);
                        }
                        extList = null;
                    }
                    entry.AddToBuffer(result, false, withWhiteSpace, null);
                }
            }
            if (extList != null)
            {
                PatternEntry last = FindLastWithNoExtension(i - 1);
                for (int j = extList.Count - 1; j >= 0; j--)
                {
                    tmp = extList.ElementAtOrDefault(j);
                    tmp.AddToBuffer(result, false, withWhiteSpace, last);
                }
                extList = null;
            }
            return result.ToString();
        }

        private PatternEntry FindLastWithNoExtension(int i)
        {
            for (--i; i >= 0; --i)
            {
                PatternEntry entry = patterns.ElementAtOrDefault(i);
                if (entry.extension.Length == 0)
                {
                    return entry;
                }
            }
            return null;
        }

        /**
         * emits the pattern for collation builder.
         * @return emits the string in the format understable to the collation
         * builder.
         */
        public string EmitPattern()
        {
            return EmitPattern(true);
        }

        /**
         * emits the pattern for collation builder.
         * @param withWhiteSpace puts spacing around the entries, and \n
         * before & and <
         * @return emits the string in the format understable to the collation
         * builder.
         */
        public string EmitPattern(bool withWhiteSpace)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < patterns.Count; ++i)
            {
                PatternEntry entry = patterns.ElementAtOrDefault(i);
                if (entry != null)
                {
                    entry.AddToBuffer(result, true, withWhiteSpace, null);
                }
            }
            return result.ToString();
        }

        /**
         * sets the pattern.
         */
        public void SetPattern(string pattern)
        {
            patterns.Clear();
            AddPattern(pattern);
        }

        /**
         * adds a pattern to the current one.
         * @param pattern the new pattern to be added
         */
        public void AddPattern(string pattern)
        {
            if (pattern == null)
                return;

            PatternEntry.Parser parser = new PatternEntry.Parser(pattern);

            PatternEntry entry = parser.Next();
            while (entry != null)
            {
                FixEntry(entry);
                entry = parser.Next();
            }
        }

        /**
         * gets count of separate entries
         * @return the size of pattern entries
         */
        public int Count
        {
            get { return patterns.Count; }
        }

        /**
         * gets count of separate entries
         * @param index the offset of the desired pattern entry
         * @return the requested pattern entry
         */
        public PatternEntry GetItemAt(int index)
        {
            return patterns.ElementAtOrDefault(index);
        }

        //============================================================
        // privates
        //============================================================
        List<PatternEntry> patterns = new List<PatternEntry>(); // a list of PatternEntries

        [NonSerialized]
        private PatternEntry saveEntry = null;
        [NonSerialized]
        private PatternEntry lastEntry = null;

        // This is really used as a local variable inside fixEntry, but we cache
        // it here to avoid newing it up every time the method is called.
        [NonSerialized]
        private StringBuilder excess = new StringBuilder();

        //
        // When building a MergeCollation, we need to do lots of searches to see
        // whether a given entry is already in the table.  Since we're using an
        // array, this would make the algorithm O(N*N).  To speed things up, we
        // use this bit array to remember whether the array contains any entries
        // starting with each Unicode character.  If not, we can avoid the search.
        // Using BitSet would make this easier, but it's significantly slower.
        //
        [NonSerialized]
        private byte[] statusArray = new byte[8192];
        private static byte BITARRAYMASK = (byte)0x1;
        private static int BYTEPOWER = 3;
        private static int BYTEMASK = (1 << BYTEPOWER) - 1;

        /*
          If the strength is RESET, then just change the lastEntry to
          be the current. (If the current is not in patterns, signal an error).
          If not, then remove the current entry, and add it after lastEntry
          (which is usually at the end).
          */
        private void FixEntry(PatternEntry newEntry)
        {
            // check to see whether the new entry has the same characters as the previous
            // entry did (this can happen when a pattern declaring a difference between two
            // strings that are canonically equivalent is normalized).  If so, and the strength
            // is anything other than IDENTICAL or RESET, throw an exception (you can't
            // declare a string to be unequal to itself).       --rtg 5/24/99
            if (lastEntry != null && newEntry.chars.Equals(lastEntry.chars) // LUCENENET TODO: Culture
                    && newEntry.extension.Equals(lastEntry.extension))// LUCENENET TODO: Culture
            {
                if (newEntry.strength != CollatorStrength.IDENTICAL
                    && (int)newEntry.strength != PatternEntry.RESET)
                {
                    throw new FormatException("The entries " + lastEntry + " and "
                            + newEntry + " are adjacent in the rules, but have conflicting "
                            + "strengths: A character can't be unequal to itself."/*, -1*/);
                }
                else
                {
                    // otherwise, just skip this entry and behave as though you never saw it
                    return;
                }
            }

            bool changeLastEntry = true;
            if ((int)newEntry.strength != PatternEntry.RESET)
            {
                int oldIndex = -1;

                if ((newEntry.chars.Length == 1))
                {

                    char c = newEntry.chars[0];
                    int statusIndex = c >> BYTEPOWER;
                    byte bitClump = statusArray[statusIndex];
                    byte setBit = (byte)(BITARRAYMASK << (c & BYTEMASK));

                    if (bitClump != 0 && (bitClump & setBit) != 0)
                    {
                        oldIndex = patterns.LastIndexOf(newEntry);
                    }
                    else
                    {
                        // We're going to add an element that starts with this
                        // character, so go ahead and set its bit.
                        statusArray[statusIndex] = (byte)(bitClump | setBit);
                    }
                }
                else
                {
                    oldIndex = patterns.LastIndexOf(newEntry);
                }
                if (oldIndex != -1)
                {
                    patterns.RemoveAt(oldIndex);
                }

                excess.Length = 0;
                int lastIndex = FindLastEntry(lastEntry, excess);

                if (excess.Length != 0)
                {
                    newEntry.extension = excess + newEntry.extension;
                    if (lastIndex != patterns.Count)
                    {
                        lastEntry = saveEntry;
                        changeLastEntry = false;
                    }
                }
                if (lastIndex == patterns.Count)
                {
                    patterns.Add(newEntry);
                    saveEntry = newEntry;
                }
                else
                {
                    patterns.Insert(lastIndex, newEntry);
                }
            }
            if (changeLastEntry)
            {
                lastEntry = newEntry;
            }
        }

        private int FindLastEntry(PatternEntry entry,
                                  StringBuilder excessChars)
        {
            if (entry == null)
                return 0;

            if ((int)entry.strength != PatternEntry.RESET)
            {
                // Search backwards for string that contains this one;
                // most likely entry is last one

                int oldIndex = -1;
                if ((entry.chars.Length == 1))
                {
                    int index = entry.chars[0] >> BYTEPOWER;
                    if ((statusArray[index] &
                        (BITARRAYMASK << (entry.chars[0] & BYTEMASK))) != 0)
                    {
                        oldIndex = patterns.LastIndexOf(entry);
                    }
                }
                else
                {
                    oldIndex = patterns.LastIndexOf(entry);
                }
                if ((oldIndex == -1))
                    throw new FormatException("couldn't find last entry: "
                                              + entry /*, oldIndex*/);
                return oldIndex + 1;
            }
            else
            {
                int i;
                for (i = patterns.Count - 1; i >= 0; --i)
                {
                    PatternEntry e = patterns.ElementAtOrDefault(i);
                    //if (e.chars.regionMatches(0, entry.chars, 0,
                    //                              e.chars.Length))
                    if (e.chars.Equals(entry.chars.Substring(0, e.chars.Length)))
                    {
                        excessChars.Append(entry.chars.Substring(e.chars.Length,
                                                                entry.chars.Length - e.chars.Length));
                        break;
                    }
                }
                if (i == -1)
                    throw new FormatException("couldn't find: " + entry/*, i*/);
                return i + 1;
            }
        }
    }
}

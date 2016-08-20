using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support.Collation
{
    /// <summary>
    /// This class contains all the code to parse a RuleBasedCollator pattern
    /// and build a RBCollationTables object from it.  A particular instance
    /// of tis class exists only during the actual build process-- once an
    /// RBCollationTables object has been built, the RBTableBuilder object
    /// goes away.  This object carries all of the state which is only needed
    /// during the build process, plus a "shadow" copy of all of the state
    /// that will go into the tables object itself.  This object communicates
    /// with RBCollationTables through a separate class, RBCollationTables.BuildAPI,
    /// this is an inner class of RBCollationTables and provides a separate
    /// private API for communication with RBTableBuilder.
    /// This class isn't just an inner class of RBCollationTables itself because
    /// of its large size.  For source-code readability, it seemed better for the
    /// builder to have its own source file.
    /// </summary>
    internal sealed class RBTableBuilder
    {
        public RBTableBuilder(RBCollationTables.BuildAPI tables)
        {
            this.tables = tables;
        }

        public void Build(string pattern, DecompositionMode decmp)
        {
            bool isSource = true;
            int i = 0;
            string expChars;
            string groupChars;
            if (pattern.Length == 0)
                throw new FormatException("Build rules empty."/*, 0*/);

            // This array maps Unicode characters to their collation ordering
            mapping = new UCompactIntArray((int)RBCollationTables.UNMAPPED);
            // Normalize the build rules.  Find occurances of all decomposed characters
            // and normalize the rules before feeding into the builder.  By "normalize",
            // we mean that all precomposed Unicode characters must be converted into
            // a base character and one or more combining characters (such as accents).
            // When there are multiple combining characters attached to a base character,
            // the combining characters must be in their canonical order
            //
            // sherman/Note:
            //(1)decmp will be NO_DECOMPOSITION only in ko locale to prevent decompose
            //hangual syllables to jamos, so we can actually just call decompose with
            //normalizer's IGNORE_HANGUL option turned on
            //
            //(2)just call the "special version" in NormalizerImpl directly
            //pattern = Normalizer.decompose(pattern, false, Normalizer.IGNORE_HANGUL, true);
            //
            //Normalizer.Mode mode = CollatorUtilities.toNormalizerMode(decmp);
            //pattern = Normalizer.normalize(pattern, mode, 0, true);

            pattern = NormalizerImpl.canonicalDecomposeWithSingleQuotation(pattern);

            // Build the merged collation entries
            // Since rules can be specified in any order in the string
            // (e.g. "c , C < d , D < e , E .... C < CH")
            // this splits all of the rules in the string out into separate
            // objects and then sorts them.  In the above example, it merges the
            // "C < CH" rule in just before the "C < D" rule.
            //

            mPattern = new MergeCollation(pattern);

            int order = 0;

            // Now walk though each entry and add it to my own tables
            for (i = 0; i < mPattern.Count; ++i)
            {
                PatternEntry entry = mPattern.GetItemAt(i);
                if (entry != null)
                {
                    groupChars = entry.Chars;
                    if (groupChars.Length > 1)
                    {
                        switch (groupChars[groupChars.Length - 1])
                        {
                            case '@':
                                frenchSec = true;
                                groupChars = groupChars.Substring(0, groupChars.Length - 1);
                                break;
                            case '!':
                                seAsianSwapping = true;
                                groupChars = groupChars.Substring(0, groupChars.Length - 1);
                                break;
                        }
                    }

                    order = Increment(entry.Strength, order);
                    expChars = entry.Extension;

                    if (expChars.Length != 0)
                    {
                        AddExpandOrder(groupChars, expChars, order);
                    }
                    else if (groupChars.Length > 1)
                    {
                        char ch = groupChars[0];
                        if (char.IsHighSurrogate(ch) && groupChars.Length == 2)
                        {
                            AddOrder(Character.ToCodePoint(ch, groupChars[1]), order);
                        }
                        else
                        {
                            AddContractOrder(groupChars, order);
                        }
                    }
                    else
                    {
                        char ch = groupChars[0];
                        AddOrder(ch, order);
                    }
                }
            }
            AddComposedChars();

            Commit();
            mapping.Compact();
            /*
            System.out.println("mappingSize=" + mapping.getKSize());
            for (int j = 0; j < 0xffff; j++) {
                int value = mapping.elementAt(j);
                if (value != RBCollationTables.UNMAPPED)
                    System.out.println("index=" + Integer.toString(j, 16)
                               + ", value=" + Integer.toString(value, 16));
            }
            */
            tables.FillInTables(frenchSec, seAsianSwapping, mapping, contractTable, expandTable,
                        contractFlags, maxSecOrder, maxTerOrder);
        }

        /** Add expanding entries for pre-composed unicode characters so that this
         * collator can be used reasonably well with decomposition turned off.
         */
        private void AddComposedChars()
        {
            // Iterate through all of the pre-composed characters in Unicode
            ComposedCharIter iter = new ComposedCharIter();
            int c;
            while ((c = iter.next()) != ComposedCharIter.DONE)
            {
                if (GetCharOrder(c) == RBCollationTables.UNMAPPED)
                {
                    //
                    // We don't already have an ordering for this pre-composed character.
                    //
                    // First, see if the decomposed string is already in our
                    // tables as a single contracting-string ordering.
                    // If so, just map the precomposed character to that order.
                    //
                    // TODO: What we should really be doing here is trying to find the
                    // longest initial substring of the decomposition that is present
                    // in the tables as a contracting character sequence, and find its
                    // ordering.  Then do this recursively with the remaining chars
                    // so that we build a list of orderings, and add that list to
                    // the expansion table.
                    // That would be more correct but also significantly slower, so
                    // I'm not totally sure it's worth doing.
                    //
                    string s = iter.decomposition();

                    //sherman/Note: if this is 1 character decomposed string, the
                    //only thing need to do is to check if this decomposed character
                    //has an entry in our order table, this order is not necessary
                    //to be a contraction order, if it does have one, add an entry
                    //for the precomposed character by using the same order, the
                    //previous impl unnecessarily adds a single character expansion
                    //entry.
                    if (s.Length == 1)
                    {
                        int order = GetCharOrder(s[0]);
                        if (order != RBCollationTables.UNMAPPED)
                        {
                            AddOrder(c, order);
                        }
                        continue;
                    }
                    else if (s.Length == 2)
                    {
                        char ch0 = s[0];
                        if (char.IsHighSurrogate(ch0))
                        {
                            int order = GetCharOrder(s.CodePointAt(0));
                            if (order != RBCollationTables.UNMAPPED)
                            {
                                AddOrder(c, order);
                            }
                            continue;
                        }
                    }
                    int contractOrder = GetContractOrder(s);
                    if (contractOrder != RBCollationTables.UNMAPPED)
                    {
                        AddOrder(c, contractOrder);
                    }
                    else
                    {
                        //
                        // We don't have a contracting ordering for the entire string
                        // that results from the decomposition, but if we have orders
                        // for each individual character, we can add an expanding
                        // table entry for the pre-composed character
                        //
                        bool allThere = true;
                        for (int i = 0; i < s.Length; i++)
                        {
                            if (GetCharOrder(s[i]) == RBCollationTables.UNMAPPED)
                            {
                                allThere = false;
                                break;
                            }
                        }
                        if (allThere)
                        {
                            AddExpandOrder(c, s, (int)RBCollationTables.UNMAPPED);
                        }
                    }
                }
            }
        }

        /**
         * Look up for unmapped values in the expanded character table.
         *
         * When the expanding character tables are built by addExpandOrder,
         * it doesn't know what the final ordering of each character
         * in the expansion will be.  Instead, it just puts the raw character
         * code into the table, adding CHARINDEX as a flag.  Now that we've
         * finished building the mapping table, we can go back and look up
         * that character to see what its real collation order is and
         * stick that into the expansion table.  That lets us avoid doing
         * a two-stage lookup later.
         */
        private void Commit()
        {
            if (expandTable != null)
            {
                for (int i = 0; i < expandTable.Count; i++)
                {
                    int[] valueList = expandTable.ElementAt(i);
                    for (int j = 0; j < valueList.Length; j++)
                    {
                        int order = valueList[j];
                        if (order < RBCollationTables.EXPANDCHARINDEX && order > CHARINDEX)
                        {
                            // found a expanding character that isn't filled in yet
                            int ch = order - CHARINDEX;

                            // Get the real values for the non-filled entry
                            int realValue = GetCharOrder(ch);

                            if (realValue == RBCollationTables.UNMAPPED)
                            {
                                // The real value is still unmapped, maybe it's ignorable
                                valueList[j] = IGNORABLEMASK & ch;
                            }
                            else
                            {
                                // just fill in the value
                                valueList[j] = realValue;
                            }
                        }
                    }
                }
            }
        }
        /**
         *  Increment of the last order based on the comparison level.
         */
        private int Increment(CollatorStrength aStrength, int lastValue)
        {
            switch (aStrength)
            {
                case CollatorStrength.PRIMARY:
                    // increment priamry order  and mask off secondary and tertiary difference
                    lastValue += PRIMARYORDERINCREMENT;
                    lastValue = (int)((uint)lastValue & RBCollationTables.PRIMARYORDERMASK);
                    isOverIgnore = true;
                    break;
                case CollatorStrength.SECONDARY:
                    // increment secondary order and mask off tertiary difference
                    lastValue += SECONDARYORDERINCREMENT;
                    lastValue = (int)((uint)lastValue & RBCollationTables.SECONDARYDIFFERENCEONLY);
                    // record max # of ignorable chars with secondary difference
                    if (!isOverIgnore)
                        maxSecOrder++;
                    break;
                case CollatorStrength.TERTIARY:
                    // increment tertiary order
                    lastValue += TERTIARYORDERINCREMENT;
                    // record max # of ignorable chars with tertiary difference
                    if (!isOverIgnore)
                        maxTerOrder++;
                    break;
            }
            return lastValue;
        }

        /**
         *  Adds a character and its designated order into the collation table.
         */
        private void AddOrder(int ch, int anOrder)
        {
            // See if the char already has an order in the mapping table
            int order = mapping.ElementAt(ch);

            if (order >= RBCollationTables.CONTRACTCHARINDEX)
            {
                // There's already an entry for this character that points to a contracting
                // character table.  Instead of adding the character directly to the mapping
                // table, we must add it to the contract table instead.
                int length = 1;
                if (Character.IsSupplementaryCodePoint(ch))
                {
                    length = Character.ToChars(ch, keyBuf, 0);
                }
                else
                {
                    keyBuf[0] = (char)ch;
                }
                AddContractOrder(new string(keyBuf, 0, length), anOrder);
            }
            else
            {
                // add the entry to the mapping table,
                // the same later entry replaces the previous one
                mapping.SetElementAt(ch, anOrder);
            }
        }

        private void AddContractOrder(string groupChars, int anOrder)
        {
            AddContractOrder(groupChars, anOrder, true);
        }

        /**
         *  Adds the contracting string into the collation table.
         */
        private void AddContractOrder(string groupChars, int anOrder,
                                              bool fwd)
        {
            if (contractTable == null)
            {
                contractTable = new List<IList<EntryPair>>(INITIALTABLESIZE);
            }

            //initial character
            int ch = groupChars.CodePointAt(0);
            /*
            char ch0 = groupChars.charAt(0);
            int ch = Character.isHighSurrogate(ch0)?
              Character.toCodePoint(ch0, groupChars.charAt(1)):ch0;
              */
            // See if the initial character of the string already has a contract table.
            int entry = mapping.ElementAt(ch);
            IList<EntryPair> entryTable = GetContractValuesImpl(entry - RBCollationTables.CONTRACTCHARINDEX);

            if (entryTable == null)
            {
                // We need to create a new table of contract entries for this base char
                int tableIndex = RBCollationTables.CONTRACTCHARINDEX + contractTable.Count;
                entryTable = new List<EntryPair>(INITIALTABLESIZE);
                contractTable.Add(entryTable);

                // Add the initial character's current ordering first. then
                // update its mapping to point to this contract table
                entryTable.Add(new EntryPair(groupChars.Substring(0, Character.CharCount(ch) - 1), entry));
                mapping.SetElementAt(ch, tableIndex);
            }

            // Now add (or replace) this string in the table
            int index = RBCollationTables.GetEntry(entryTable, groupChars, fwd);
            if (index != RBCollationTables.UNMAPPED)
            {
                EntryPair pair = entryTable.ElementAt(index);
                pair.Value = anOrder;
            }
            else
            {
                EntryPair pair = entryTable.Last();

                // NOTE:  This little bit of logic is here to speed CollationElementIterator
                // .nextContractChar().  This code ensures that the longest sequence in
                // this list is always the _last_ one in the list.  This keeps
                // nextContractChar() from having to search the entire list for the longest
                // sequence.
                if (groupChars.Length > pair.EntryName.Length)
                {
                    entryTable.Add(new EntryPair(groupChars, anOrder, fwd));
                }
                else
                {
                    entryTable.Insert(entryTable.Count - 1, new EntryPair(groupChars, anOrder, fwd));
                }
            }

            // If this was a forward mapping for a contracting string, also add a
            // reverse mapping for it, so that CollationElementIterator.previous
            // can work right
            if (fwd && groupChars.Length > 1)
            {
                AddContractFlags(groupChars);
                AddContractOrder(new StringBuilder(groupChars).Reverse().ToString(),
                                 anOrder, false);
            }
        }

        /**
         * If the given string has been specified as a contracting string
         * in this collation table, return its ordering.
         * Otherwise return UNMAPPED.
         */
        private int GetContractOrder(string groupChars)
        {
            int result = (int)RBCollationTables.UNMAPPED;
            if (contractTable != null)
            {
                int ch = groupChars.CodePointAt(0);
                /*
                char ch0 = groupChars.charAt(0);
                int ch = Character.isHighSurrogate(ch0)?
                  Character.toCodePoint(ch0, groupChars.charAt(1)):ch0;
                  */
                IList<EntryPair> entryTable = GetContractValues(ch);
                if (entryTable != null)
                {
                    int index = RBCollationTables.GetEntry(entryTable, groupChars, true);
                    if (index != RBCollationTables.UNMAPPED)
                    {
                        EntryPair pair = entryTable.ElementAt(index);
                        result = pair.Value;
                    }
                }
            }
            return result;
        }

        private int GetCharOrder(int ch)
        {
            int order = mapping.ElementAt(ch);

            if (order >= RBCollationTables.CONTRACTCHARINDEX)
            {
                IList<EntryPair> groupList = GetContractValuesImpl(order - RBCollationTables.CONTRACTCHARINDEX);
                EntryPair pair = groupList.First();
                order = pair.Value;
            }
            return order;
        }

        /**
         *  Get the entry of hash table of the contracting string in the collation
         *  table.
         *  @param ch the starting character of the contracting string
         */
        private IList<EntryPair> GetContractValues(int ch)
        {
            int index = mapping.ElementAt(ch);
            return GetContractValuesImpl(index - RBCollationTables.CONTRACTCHARINDEX);
        }

        private IList<EntryPair> GetContractValuesImpl(int index)
        {
            if (index >= 0)
            {
                return contractTable.ElementAtOrDefault(index);
            }
            else // not found
            {
                return null;
            }
        }

        /**
         *  Adds the expanding string into the collation table.
         */
        private void AddExpandOrder(string contractChars,
                                    string expandChars,
                                    int anOrder)
        {
            // Create an expansion table entry
            int tableIndex = AddExpansion(anOrder, expandChars);

            // And add its index into the main mapping table
            if (contractChars.Length > 1)
            {
                char ch = contractChars[0];
                if (char.IsHighSurrogate(ch) && contractChars.Length == 2)
                {
                    char ch2 = contractChars[1];
                    if (char.IsLowSurrogate(ch2))
                    {
                        //only add into table when it is a legal surrogate
                        AddOrder(Character.ToCodePoint(ch, ch2), tableIndex);
                    }
                }
                else
                {
                    AddContractOrder(contractChars, tableIndex);
                }
            }
            else
            {
                AddOrder(contractChars[0], tableIndex);
            }
        }

        private void AddExpandOrder(int ch, string expandChars, int anOrder)
        {
            int tableIndex = AddExpansion(anOrder, expandChars);
            AddOrder(ch, tableIndex);
        }

        /**
         * Create a new entry in the expansion table that contains the orderings
         * for the given characers.  If anOrder is valid, it is added to the
         * beginning of the expanded list of orders.
         */
        private int AddExpansion(int anOrder, string expandChars)
        {
            if (expandTable == null)
            {
                expandTable = new List<int[]>(INITIALTABLESIZE);
            }

            // If anOrder is valid, we want to add it at the beginning of the list
            int offset = (anOrder == RBCollationTables.UNMAPPED) ? 0 : 1;

            int[] valueList = new int[expandChars.Length + offset];
            if (offset == 1)
            {
                valueList[0] = anOrder;
            }

            int j = offset;
            for (int i = 0; i < expandChars.Length; i++)
            {
                char ch0 = expandChars[i];
                char ch1;
                int ch;
                if (char.IsHighSurrogate(ch0))
                {
                    if (++i == expandChars.Length ||
                        !char.IsLowSurrogate(ch1 = expandChars[i]))
                    {
                        //ether we are missing the low surrogate or the next char
                        //is not a legal low surrogate, so stop loop
                        break;
                    }
                    ch = Character.ToCodePoint(ch0, ch1);

                }
                else
                {
                    ch = ch0;
                }

                int mapValue = GetCharOrder(ch);

                if (mapValue != RBCollationTables.UNMAPPED)
                {
                    valueList[j++] = mapValue;
                }
                else
                {
                    // can't find it in the table, will be filled in by commit().
                    valueList[j++] = CHARINDEX + ch;
                }
            }
            if (j < valueList.Length)
            {
                //we had at least one supplementary character, the size of valueList
                //is bigger than it really needs...
                int[] tmpBuf = new int[j];
                while (--j >= 0)
                {
                    tmpBuf[j] = valueList[j];
                }
                valueList = tmpBuf;
            }
            // Add the expanding char list into the expansion table.
            int tableIndex = RBCollationTables.EXPANDCHARINDEX + expandTable.Count;
            expandTable.Add(valueList);

            return tableIndex;
        }

        private void AddContractFlags(string chars)
        {
            char c0;
            int c;
            int len = chars.Length;
            for (int i = 0; i < len; i++)
            {
                c0 = chars[i];
                c = char.IsHighSurrogate(c0)
                              ? Character.ToCodePoint(c0, chars[++i])
                              : c0;
                contractFlags[c] = 1;
            }
        }

        // ==============================================================
        // constants
        // ==============================================================
        internal readonly static int CHARINDEX = 0x70000000;  // need look up in .commit()

        private readonly static int IGNORABLEMASK = 0x0000ffff;
        private readonly static int PRIMARYORDERINCREMENT = 0x00010000;
        private readonly static int SECONDARYORDERINCREMENT = 0x00000100;
        private readonly static int TERTIARYORDERINCREMENT = 0x00000001;
        private readonly static int INITIALTABLESIZE = 20;
        private readonly static int MAXKEYSIZE = 5;

        // ==============================================================
        // instance variables
        // ==============================================================

        // variables used by the build process
        private RBCollationTables.BuildAPI tables = null;
        private MergeCollation mPattern = null;
        private bool isOverIgnore = false;
        private char[] keyBuf = new char[MAXKEYSIZE];
        private IDictionary<int, int> contractFlags = new Dictionary<int, int>(100);

        // "shadow" copies of the instance variables in RBCollationTables
        // (the values in these variables are copied back into RBCollationTables
        // at the end of the build process)
        private bool frenchSec = false;
        private bool seAsianSwapping = false;

        private UCompactIntArray mapping = null;
        private List<IList<EntryPair>> contractTable = null;
        private List<int[]> expandTable = null;

        private short maxSecOrder = 0;
        private short maxTerOrder = 0;
    }
}

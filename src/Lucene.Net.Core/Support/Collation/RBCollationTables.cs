using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support.Collation
{
    /// <summary>
    /// This class contains the static state of a RuleBasedCollator: The various
    /// tables that are used by the collation routines.Several RuleBasedCollators
    /// can share a single RBCollationTables object, easing memory requirements and
    /// improving performance.
    /// </summary>
    internal sealed class RBCollationTables
    {
        //===========================================================================================
        //  The following diagram shows the data structure of the RBCollationTables object.
        //  Suppose we have the rule, where 'o-umlaut' is the unicode char 0x00F6.
        //  "a, A < b, B < c, C, ch, cH, Ch, CH < d, D ... < o, O; 'o-umlaut'/E, 'O-umlaut'/E ...".
        //  What the rule says is, sorts 'ch'ligatures and 'c' only with tertiary difference and
        //  sorts 'o-umlaut' as if it's always expanded with 'e'.
        //
        // mapping table                     contracting list           expanding list
        // (contains all unicode char
        //  entries)                   ___    ____________       _________________________
        //  ________                +>|_*_|->|'c' |v('c') |  +>|v('o')|v('umlaut')|v('e')|
        // |_\u0001_|-> v('\u0001') | |_:_|  |------------|  | |-------------------------|
        // |_\u0002_|-> v('\u0002') | |_:_|  |'ch'|v('ch')|  | |             :           |
        // |____:___|               | |_:_|  |------------|  | |-------------------------|
        // |____:___|               |        |'cH'|v('cH')|  | |             :           |
        // |__'a'___|-> v('a')      |        |------------|  | |-------------------------|
        // |__'b'___|-> v('b')      |        |'Ch'|v('Ch')|  | |             :           |
        // |____:___|               |        |------------|  | |-------------------------|
        // |____:___|               |        |'CH'|v('CH')|  | |             :           |
        // |___'c'__|----------------         ------------   | |-------------------------|
        // |____:___|                                        | |             :           |
        // |o-umlaut|----------------------------------------  |_________________________|
        // |____:___|
        //
        // Noted by Helena Shih on 6/23/97
        //============================================================================================

        public RBCollationTables(string rules, DecompositionMode decmp)
        {
            this.rules = rules;

            RBTableBuilder builder = new RBTableBuilder(new BuildAPI(this));
            builder.Build(rules, decmp); // this object is filled in through
                                         // the BuildAPI object
        }

        internal sealed class BuildAPI
        {
            private readonly RBCollationTables outerInstance;

            /// <summary>
            /// Private constructor.  Prevents anyone else besides RBTableBuilder
            /// from gaining direct access to the internals of this class.
            /// </summary>
            public BuildAPI(RBCollationTables outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            /// <summary>
            /// This function is used by RBTableBuilder to fill in all the members of this
            /// object.  (Effectively, the builder class functions as a "friend" of this
            /// class, but to avoid changing too much of the logic, it carries around "shadow"
            /// copies of all these variables until the end of the build process and then
            /// copies them en masse into the actual tables object once all the construction
            /// logic is complete.This function does that "copying en masse".
            /// </summary>
            /// <param name="f2ary">The value for frenchSec (the French-secondary flag)</param>
            /// <param name="swap">The value for SE Asian swapping rule</param>
            /// <param name="map">The collator's character-mapping table (the value for mapping)</param>
            /// <param name="cTbl">The collator's contracting-character table (the value for contractTable)</param>
            /// <param name="eTbl">The collator's expanding-character table (the value for expandTable)</param>
            /// <param name="cFlgs">The hash table of characters that participate in contracting-
            /// character sequences (the value for contractFlags)</param>
            /// <param name="mso">The value for maxSecOrder</param>
            /// <param name="mto">The value for maxTerOrder</param>
            internal void FillInTables(bool f2ary,
                              bool swap,
                              UCompactIntArray map,
                              IList<IList<EntryPair>> cTbl,
                              IList<int[]> eTbl,
                              IDictionary<int, int> cFlgs,
                              short mso,
                              short mto)
            {
                outerInstance.frenchSec = f2ary;
                outerInstance.seAsianSwapping = swap;
                outerInstance.mapping = map;
                outerInstance.contractTable = cTbl;
                outerInstance.expandTable = eTbl;
                outerInstance.contractFlags = cFlgs;
                outerInstance.maxSecOrder = mso;
                outerInstance.maxTerOrder = mto;
            }
        }

        /// <summary>
        /// Gets the table-based rules for the collation object.
        /// </summary>
        public string Rules
        {
            get { return rules; }
        }

        public bool IsFrenchSec
        {
            get { return frenchSec; }
        }

        public bool IsSEAsianSwapping
        {
            get { return seAsianSwapping; }
        }

        // ==============================================================
        // internal (for use by CollationElementIterator)
        // ==============================================================

        /// <summary>
        /// Get the entry of hash table of the contracting string in the collation
        /// table.
        /// </summary>
        /// <param name="ch">the starting character of the contracting string</param>
        internal IList<EntryPair> GetContractValues(int ch)
        {
            int index = mapping.ElementAt(ch);
            return GetContractValuesImpl(index - CONTRACTCHARINDEX);
        }

        //get contract values from contractTable by index
        private IList<EntryPair> GetContractValuesImpl(int index)
        {
            if (index >= 0)
            {
                return contractTable.ElementAt(index);
            }
            return null; // not found
        }

        /// <summary>
        /// Returns true if this character appears anywhere in a contracting
        /// character sequence.  (Used by CollationElementIterator.SetOffset().)
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        internal bool UsedInContractSeq(int c)
        {
            return (contractFlags.ContainsKey(c) ? contractFlags[c] : 0) == 1;
        }

        /// <summary>
        /// Return the maximum length of any expansion sequences that end
        /// with the specified comparison order.
        /// </summary>
        /// <param name="order">a collation order returned by previous or next.</param>
        /// <returns>the maximum length of any expansion seuences ending
        /// with the specified order.</returns>
        internal int GetMaxExpansion(int order)
        {
            int result = 1;

            if (expandTable != null)
            {
                // Right now this does a linear search through the entire
                // expansion table.  If a collator had a large number of expansions,
                // this could cause a performance problem, but in practise that
                // rarely happens
                for (int i = 0; i < expandTable.Count; i++)
                {
                    int[] valueList = expandTable.ElementAt(i);
                    int length = valueList.Length;

                    if (length > result && valueList[length - 1] == order)
                    {
                        result = length;
                    }
                }
            }

            return result;
        }

        internal int[] GetExpandValueList(int idx)
        {
            return expandTable.ElementAt(idx - EXPANDCHARINDEX);
        }

        /// <summary>
        /// Get the comparison order of a character from the collation table.
        /// </summary>
        /// <param name="ch"></param>
        /// <returns>the comparison order of a character.</returns>
        internal int GetUnicodeOrder(int ch)
        {
            return mapping.ElementAt(ch);
        }

        internal short MaxSecOrder
            {
            get { return maxSecOrder; }
            }

        internal short MaxTerOrder
        {
            get { return maxTerOrder; }
        }

        /// <summary>
        /// Reverse a string.
        /// </summary>
        //shemran/Note: this is used for secondary order value reverse, no
        //              need to consider supplementary pair.
        internal static void Reverse(StringBuilder result, int from, int to)
        {
            int i = from;
            char swap;

            int j = to - 1;
            while (i < j)
            {
                swap = result[i];
                result[i] = result[j];
                result[j]= swap;
                i++;
                j--;
            }
        }

        internal static int GetEntry(IList<EntryPair> list, string name, bool fwd)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var pair = list.ElementAt(i);
                if (pair.Fwd == fwd && pair.EntryName.Equals(name)) // LUCENENET TODO: Culture in Equals()
                {
                    return i;
                }
            }
            return (int)UNMAPPED;
        }

        // ==============================================================
        // constants
        // ==============================================================
        //sherman/Todo: is the value big enough?????
        internal readonly static int EXPANDCHARINDEX = 0x7E000000; // Expand index follows
        internal readonly static int CONTRACTCHARINDEX = 0x7F000000;  // contract indexes follow
        internal readonly static uint UNMAPPED = 0xFFFFFFFF;

        internal readonly static uint PRIMARYORDERMASK = 0xffff0000;
        internal readonly static uint SECONDARYORDERMASK = 0x0000ff00;
        internal readonly static uint TERTIARYORDERMASK = 0x000000ff;
        internal readonly static uint PRIMARYDIFFERENCEONLY = 0xffff0000;
        internal readonly static uint SECONDARYDIFFERENCEONLY = 0xffffff00;
        internal readonly static int PRIMARYORDERSHIFT = 16;
        internal readonly static int SECONDARYORDERSHIFT = 8;

        // ==============================================================
        // instance variables
        // ==============================================================
        private string rules = null;
        private bool frenchSec = false;
        private bool seAsianSwapping = false;

        private UCompactIntArray mapping = null;
        private IList<IList<EntryPair>> contractTable = null;
        private IList<int[]> expandTable = null;
        private IDictionary<int, int> contractFlags = null;

        private short maxSecOrder = 0;
        private short maxTerOrder = 0;
    }
}

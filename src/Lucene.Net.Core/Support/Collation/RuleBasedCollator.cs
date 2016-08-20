using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support.Collation
{
    public class RuleBasedCollator : Collator
    {
        // IMPLEMENTATION NOTES:  The implementation of the collation algorithm is
        // divided across three classes: RuleBasedCollator, RBCollationTables, and
        // CollationElementIterator.  RuleBasedCollator contains the collator's
        // transient state and includes the code that uses the other classes to
        // implement comparison and sort-key building.  RuleBasedCollator also
        // contains the logic to handle French secondary accent sorting.
        // A RuleBasedCollator has two CollationElementIterators.  State doesn't
        // need to be preserved in these objects between calls to compare() or
        // getCollationKey(), but the objects persist anyway to avoid wasting extra
        // creation time.  compare() and getCollationKey() are synchronized to ensure
        // thread safety with this scheme.  The CollationElementIterator is responsible
        // for generating collation elements from strings and returning one element at
        // a time (sometimes there's a one-to-many or many-to-one mapping between
        // characters and collation elements-- this class handles that).
        // CollationElementIterator depends on RBCollationTables, which contains the
        // collator's static state.  RBCollationTables contains the actual data
        // tables specifying the collation order of characters for a particular locale
        // or use.  It also contains the base logic that CollationElementIterator
        // uses to map from characters to collation elements.  A single RBCollationTables
        // object is shared among all RuleBasedCollators for the same locale, and
        // thus by all the CollationElementIterators they create.

            /// <summary>
            /// RuleBasedCollator constructor.  This takes the table rules and builds
            /// a collation table out of them.Please see RuleBasedCollator class
            /// description for more details on the collation rule syntax.
            /// </summary>
            /// <param name="rules">the collation rules to build the collation table from.</param>
            /// <exception cref="FormatException">A format exception
            /// will be thrown if the build process of the rules fails.For
            /// example, build rule "a &lt; ? &lt; d" will cause the constructor to
            /// throw the ParseException because the '?' is not quoted.
            /// </exception>
        public RuleBasedCollator(string rules) 
            //: this(rules, DecompositionMode.CANONICAL_DECOMPOSITION)
        {
            //var x = new System.Globalization.SortKey(); // LUCENENET TODO: Replace CollationKey??
            //var y = new System.Globalization.StringInfo("");
        }

        /// <summary>
        /// RuleBasedCollator constructor.  This takes the table rules and builds
        /// a collation table out of them.Please see RuleBasedCollator class
        /// description for more details on the collation rule syntax.
        /// </summary>
        /// <param name="rules">the collation rules to build the collation table from.</param>
        /// <param name="decomposition">the decomposition strength used to build the
        /// collation table and to perform comparisons.</param>
        /// <exception cref="FormatException">A format exception
        /// will be thrown if the build process of the rules fails.For
        /// example, build rule "a &lt; ? &lt; d" will cause the constructor to
        /// throw the ParseException because the '?' is not quoted.
        /// </exception>
        internal RuleBasedCollator(string rules, DecompositionMode decomposition) 
        {
            Strength = CollatorStrength.TERTIARY;
            Decomposition = decomposition;
            tables = new RBCollationTables(rules, decomposition);
        }

        /// <summary>
        /// "Copy constructor."  Used in clone() for performance.
        /// </summary>
        /// <param name="that"></param>
        private RuleBasedCollator(RuleBasedCollator that)
        {
            Strength = that.Strength;
            Decomposition = that.Decomposition;
            tables = that.tables;
        }

        /// <summary>
        /// Gets the table-based rules for the collation object.
        /// </summary>
        public virtual string Rules
        {
            get { return tables.Rules; }
        }

        /// <summary>
        /// Returns a CollationElementIterator for the given string.
        /// </summary>
        /// <param name="source">the string to be collated</param>
        /// <returns>A <seealso cref="CollationElementIterator"/> instance.</returns>
        public virtual CollationElementIterator GetCollationElementIterator(string source)
        {
            return new CollationElementIterator(source, this);
        }

        /// <summary>
        /// Returns a CollationElementIterator for the given CharacterIterator.
        /// </summary>
        /// <param name="source">the character iterator to be collated</param>
        /// <returns>A <seealso cref="CollationElementIterator"/> instance.</returns>
        public virtual CollationElementIterator GetCollationElementIterator(CharacterIterator source)
        {
            return new CollationElementIterator(source, this);
        }

        /// <summary>
        /// Compares the character data stored in two different strings based on the
        /// collation rules.Returns information about whether a string is less
        /// than, greater than or equal to another string in a language.
        /// This can be overriden in a subclass.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public override int Compare(string source, string target)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            // The basic algorithm here is that we use CollationElementIterators
            // to step through both the source and target strings.  We compare each
            // collation element in the source string against the corresponding one
            // in the target, checking for differences.
            //
            // If a difference is found, we set <result> to LESS or GREATER to
            // indicate whether the source string is less or greater than the target.
            //
            // However, it's not that simple.  If we find a tertiary difference
            // (e.g. 'A' vs. 'a') near the beginning of a string, it can be
            // overridden by a primary difference (e.g. "A" vs. "B") later in
            // the string.  For example, "AA" < "aB", even though 'A' > 'a'.
            //
            // To keep track of this, we use strengthResult to keep track of the
            // strength of the most significant difference that has been found
            // so far.  When we find a difference whose strength is greater than
            // strengthResult, it overrides the last difference (if any) that
            // was found.

            int result = Collator.EQUAL;

            if (sourceCursor == null)
            {
                sourceCursor = GetCollationElementIterator(source);
            }
            else
            {
                sourceCursor.SetText(source);
            }
            if (targetCursor == null)
            {
                targetCursor = GetCollationElementIterator(target);
            }
            else
            {
                targetCursor.SetText(target);
            }

            int sOrder = 0, tOrder = 0;

            bool initialCheckSecTer = Strength >= CollatorStrength.SECONDARY;
            bool checkSecTer = initialCheckSecTer;
            bool checkTertiary = Strength >= CollatorStrength.TERTIARY;

            bool gets = true, gett = true;

            while (true)
            {
                // Get the next collation element in each of the strings, unless
                // we've been requested to skip it.
                if (gets) sOrder = sourceCursor.Next(); else gets = true;
                if (gett) tOrder = targetCursor.Next(); else gett = true;

                // If we've hit the end of one of the strings, jump out of the loop
                if ((sOrder == CollationElementIterator.NULLORDER) ||
                    (tOrder == CollationElementIterator.NULLORDER))
                    break;

                int pSOrder = CollationElementIterator.PrimaryOrder(sOrder);
                int pTOrder = CollationElementIterator.PrimaryOrder(tOrder);

                // If there's no difference at this position, we can skip it
                if (sOrder == tOrder)
                {
                    if (tables.IsFrenchSec && pSOrder != 0)
                    {
                        if (!checkSecTer)
                        {
                            // in french, a secondary difference more to the right is stronger,
                            // so accents have to be checked with each base element
                            checkSecTer = initialCheckSecTer;
                            // but tertiary differences are less important than the first
                            // secondary difference, so checking tertiary remains disabled
                            checkTertiary = false;
                        }
                    }
                    continue;
                }

                // Compare primary differences first.
                if (pSOrder != pTOrder)
                {
                    if (sOrder == 0)
                    {
                        // The entire source element is ignorable.
                        // Skip to the next source element, but don't fetch another target element.
                        gett = false;
                        continue;
                    }
                    if (tOrder == 0)
                    {
                        gets = false;
                        continue;
                    }

                    // The source and target elements aren't ignorable, but it's still possible
                    // for the primary component of one of the elements to be ignorable....

                    if (pSOrder == 0)  // primary order in source is ignorable
                    {
                        // The source's primary is ignorable, but the target's isn't.  We treat ignorables
                        // as a secondary difference, so remember that we found one.
                        if (checkSecTer)
                        {
                            result = Collator.GREATER;  // (strength is SECONDARY)
                            checkSecTer = false;
                        }
                        // Skip to the next source element, but don't fetch another target element.
                        gett = false;
                    }
                    else if (pTOrder == 0)
                    {
                        // record differences - see the comment above.
                        if (checkSecTer)
                        {
                            result = Collator.LESS;  // (strength is SECONDARY)
                            checkSecTer = false;
                        }
                        // Skip to the next source element, but don't fetch another target element.
                        gets = false;
                    }
                    else
                    {
                        // Neither of the orders is ignorable, and we already know that the primary
                        // orders are different because of the (pSOrder != pTOrder) test above.
                        // Record the difference and stop the comparison.
                        if (pSOrder < pTOrder)
                        {
                            return Collator.LESS;  // (strength is PRIMARY)
                        }
                        else
                        {
                            return Collator.GREATER;  // (strength is PRIMARY)
                        }
                    }
                }
                else
                { // else of if ( pSOrder != pTOrder )
                  // primary order is the same, but complete order is different. So there
                  // are no base elements at this point, only ignorables (Since the strings are
                  // normalized)

                    if (checkSecTer)
                    {
                        // a secondary or tertiary difference may still matter
                        short secSOrder = CollationElementIterator.SecondaryOrder(sOrder);
                        short secTOrder = CollationElementIterator.SecondaryOrder(tOrder);
                        if (secSOrder != secTOrder)
                        {
                            // there is a secondary difference
                            result = (secSOrder < secTOrder) ? Collator.LESS : Collator.GREATER;
                            // (strength is SECONDARY)
                            checkSecTer = false;
                            // (even in french, only the first secondary difference within
                            //  a base character matters)
                        }
                        else
                        {
                            if (checkTertiary)
                            {
                                // a tertiary difference may still matter
                                short terSOrder = CollationElementIterator.TertiaryOrder(sOrder);
                                short terTOrder = CollationElementIterator.TertiaryOrder(tOrder);
                                if (terSOrder != terTOrder)
                                {
                                    // there is a tertiary difference
                                    result = (terSOrder < terTOrder) ? Collator.LESS : Collator.GREATER;
                                    // (strength is TERTIARY)
                                    checkTertiary = false;
                                }
                            }
                        }
                    } // if (checkSecTer)

                }  // if ( pSOrder != pTOrder )
            } // while()

            if (sOrder != CollationElementIterator.NULLORDER)
            {
                // (tOrder must be CollationElementIterator::NULLORDER,
                //  since this point is only reached when sOrder or tOrder is NULLORDER.)
                // The source string has more elements, but the target string hasn't.
                do
                {
                    if (CollationElementIterator.PrimaryOrder(sOrder) != 0)
                    {
                        // We found an additional non-ignorable base character in the source string.
                        // This is a primary difference, so the source is greater
                        return Collator.GREATER; // (strength is PRIMARY)
                    }
                    else if (CollationElementIterator.SecondaryOrder(sOrder) != 0)
                    {
                        // Additional secondary elements mean the source string is greater
                        if (checkSecTer)
                        {
                            result = Collator.GREATER;  // (strength is SECONDARY)
                            checkSecTer = false;
                        }
                    }
                } while ((sOrder = sourceCursor.Next()) != CollationElementIterator.NULLORDER);
            }
            else if (tOrder != CollationElementIterator.NULLORDER)
            {
                // The target string has more elements, but the source string hasn't.
                do
                {
                    if (CollationElementIterator.PrimaryOrder(tOrder) != 0)
                        // We found an additional non-ignorable base character in the target string.
                        // This is a primary difference, so the source is less
                        return Collator.LESS; // (strength is PRIMARY)
                    else if (CollationElementIterator.SecondaryOrder(tOrder) != 0)
                    {
                        // Additional secondary elements in the target mean the source string is less
                        if (checkSecTer)
                        {
                            result = Collator.LESS;  // (strength is SECONDARY)
                            checkSecTer = false;
                        }
                    }
                } while ((tOrder = targetCursor.Next()) != CollationElementIterator.NULLORDER);
            }

            // For IDENTICAL comparisons, we use a bitwise character comparison
            // as a tiebreaker if all else is equal
            if (result == 0 && Strength == CollatorStrength.IDENTICAL)
            {
                DecompositionMode mode = Decomposition;
                NormalizationForm form;
                if (mode == DecompositionMode.CANONICAL_DECOMPOSITION)
                {
                    form = NormalizationForm.FormD;
                }
                else if (mode == DecompositionMode.FULL_DECOMPOSITION)
                {
                    form = NormalizationForm.FormKD;
                }
                else
                {
                    return string.Compare(source, target);
                }

                string sourceDecomposition = source.Normalize(form);
                string targetDecomposition = target.Normalize(form);
                return string.Compare(sourceDecomposition, targetDecomposition); // LUCENENET TODO: Should this be the current culture? Should it be CompareOrdinal()?

                //CompareInfo x = System.Globalization.CompareInfo.GetCompareInfo("en-US");
                //SortKey y = x.GetSortKey("", CompareOptions.StringSort);

                // LUCENENET TODO: Subclass CompareInfo (Collator) and make an extension method for GetCollator() on CultureInfo
                // that gets a culture-specific CollatorProvider from a cache and, failing that, instantiate a default RuleBasedCollator
                //CultureInfo foo = new CultureInfo("en-US");
                //SortKey bar = foo.CompareInfo.GetSortKey("bar");
                ////CultureInfo.GetCultures()
                //foo.CompareInfo.GetSortKey("test").KeyData;

                //DecompositionMode mode = Decomposition;
                //Normalizer.Form form;
                //if (mode == DecompositionMode.CANONICAL_DECOMPOSITION)
                //{
                //    form = Normalizer.Form.NFD;
                //}
                //else if (mode == DecompositionMode.FULL_DECOMPOSITION)
                //{
                //    form = Normalizer.Form.NFKD;
                //}
                //else
                //{
                //    return source.CompareToOrdinal(target);
                //}

                //string sourceDecomposition = Normalizer.normalize(source, form);
                //string targetDecomposition = Normalizer.normalize(target, form);
                //return sourceDecomposition.CompareToOrdinal(targetDecomposition); // LUCENENET TODO: Should this be the current culture?
            }
            return result;
        }

        /// <summary>
        /// Transforms the string into a series of characters that can be compared 
        /// with CollationKey.compareTo. This overrides <see cref="Collator.GetCollationKey(string)"/>.
        /// It can be overriden in a subclass.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public override CollationKey GetCollationKey(string source)
        {
            //
            // The basic algorithm here is to find all of the collation elements for each
            // character in the source string, convert them to a char representation,
            // and put them into the collation key.  But it's trickier than that.
            // Each collation element in a string has three components: primary (A vs B),
            // secondary (A vs A-acute), and tertiary (A' vs a); and a primary difference
            // at the end of a string takes precedence over a secondary or tertiary
            // difference earlier in the string.
            //
            // To account for this, we put all of the primary orders at the beginning of the
            // string, followed by the secondary and tertiary orders, separated by nulls.
            //
            // Here's a hypothetical example, with the collation element represented as
            // a three-digit number, one digit for primary, one for secondary, etc.
            //
            // String:              A     a     B   \u00e9 <--(e-acute)
            // Collation Elements: 101   100   201  510
            //
            // Collation Key:      1125<null>0001<null>1010
            //
            // To make things even trickier, secondary differences (accent marks) are compared
            // starting at the *end* of the string in languages with French secondary ordering.
            // But when comparing the accent marks on a single base character, they are compared
            // from the beginning.  To handle this, we reverse all of the accents that belong
            // to each base character, then we reverse the entire string of secondary orderings
            // at the end.  Taking the same example above, a French collator might return
            // this instead:
            //
            // Collation Key:      1125<null>1000<null>1010
            //

            if (source == null)
                return null;

            if (primResult == null)
            {
                primResult = new StringBuilder();
                secResult = new StringBuilder();
                terResult = new StringBuilder();
            }
            else
            {
                primResult.Length=0;
                secResult.Length = 0;
                terResult.Length = 0;
            }
            int order = 0;
            bool compareSec = (Strength >= CollatorStrength.SECONDARY);
            bool compareTer = (Strength >= CollatorStrength.TERTIARY);
            int secOrder = (int)CollationElementIterator.NULLORDER;
            int terOrder = (int)CollationElementIterator.NULLORDER;
            int preSecIgnore = 0;

            if (sourceCursor == null)
            {
                sourceCursor = GetCollationElementIterator(source);
            }
            else
            {
                sourceCursor.SetText(source);
            }

            // walk through each character
            while ((order = sourceCursor.Next()) !=
                   CollationElementIterator.NULLORDER)
            {
                secOrder = CollationElementIterator.SecondaryOrder(order);
                terOrder = CollationElementIterator.TertiaryOrder(order);
                if (!CollationElementIterator.IsIgnorable(order))
                {
                    primResult.Append((char)(CollationElementIterator.PrimaryOrder(order)
                                        + COLLATIONKEYOFFSET));

                    if (compareSec)
                    {
                        //
                        // accumulate all of the ignorable/secondary characters attached
                        // to a given base character
                        //
                        if (tables.IsFrenchSec && preSecIgnore < secResult.Length)
                        {
                            //
                            // We're doing reversed secondary ordering and we've hit a base
                            // (non-ignorable) character.  Reverse any secondary orderings
                            // that applied to the last base character.  (see block comment above.)
                            //
                            RBCollationTables.Reverse(secResult, preSecIgnore, secResult.Length);
                        }
                        // Remember where we are in the secondary orderings - this is how far
                        // back to go if we need to reverse them later.
                        secResult.Append((char)(secOrder + COLLATIONKEYOFFSET));
                        preSecIgnore = secResult.Length;
                    }
                    if (compareTer)
                    {
                        terResult.Append((char)(terOrder + COLLATIONKEYOFFSET));
                    }
                }
                else
                {
                    if (compareSec && secOrder != 0)
                        secResult.Append((char)
                            (secOrder + tables.MaxSecOrder + COLLATIONKEYOFFSET));
                    if (compareTer && terOrder != 0)
                        terResult.Append((char)
                            (terOrder + tables.MaxTerOrder + COLLATIONKEYOFFSET));
                }
            }
            if (tables.IsFrenchSec)
            {
                if (preSecIgnore < secResult.Length)
                {
                    // If we've accumulated any secondary characters after the last base character,
                    // reverse them.
                    RBCollationTables.Reverse(secResult, preSecIgnore, secResult.Length);
                }
                // And now reverse the entire secResult to get French secondary ordering.
                RBCollationTables.Reverse(secResult, 0, secResult.Length);
            }
            primResult.Append((char)0);
            secResult.Append((char)0);
            secResult.Append(terResult.ToString());
            primResult.Append(secResult.ToString());

            if (Strength == CollatorStrength.IDENTICAL)
            {
                primResult.Append((char)0);
                DecompositionMode mode = Decomposition;
                if (mode == DecompositionMode.CANONICAL_DECOMPOSITION)
                {
                    //primResult.Append(Normalizer.normalize(source, Normalizer.Form.NFD));
                    primResult.Append(source.Normalize(NormalizationForm.FormD));
                }
                else if (mode == DecompositionMode.FULL_DECOMPOSITION)
                {
                    primResult.Append(source.Normalize(NormalizationForm.FormKD));
                }
                else
                {
                    primResult.Append(source);
                }
            }
            return new RuleBasedCollationKey(source, primResult.ToString());
        }

        /// <summary>
        /// Standard override; no change in semantics.
        /// </summary>
        public override object Clone()
        {
            // bypass Object.clone() and use our "copy constructor".  This is faster.
            return new RuleBasedCollator(this);
        }

        /// <summary>
        /// Compares the equality of two collation objects.
        /// </summary>
        /// <param name="that">the table-based collation object to be compared with this.</param>
        /// <returns><b>true</b> if the current table-based collation object is the same
        /// as the table-based collation object obj; otherwise <b>false</b>.</returns>
        public override bool Equals(object that)
        {
            if (that == null) return false;
            if (!base.Equals(that)) return false;  // super does class check
            RuleBasedCollator other = (RuleBasedCollator)that;
            // all other non-transient information is also contained in rules.
            return (Rules.Equals(other.Rules));
        }

        /// <summary>
        /// Generates the hash code for the table-based collation object
        /// </summary>
        public override int GetHashCode()
        {
            return Rules.GetHashCode();
        }

        internal RBCollationTables Tables
        {
            get { return tables; }
        }

        // ==============================================================
        // private
        // ==============================================================

        internal readonly static int CHARINDEX = 0x70000000;  // need look up in .commit()
        internal readonly static int EXPANDCHARINDEX = 0x7E000000; // Expand index follows
        internal readonly static int CONTRACTCHARINDEX = 0x7F000000;  // contract indexes follow
        internal readonly static uint UNMAPPED = 0xFFFFFFFF;

        private readonly static int COLLATIONKEYOFFSET = 1;

        private RBCollationTables tables = null;

        // Internal objects that are cached across calls so that they don't have to
        // be created/destroyed on every call to compare() and getCollationKey()
        private StringBuilder primResult = null;
        private StringBuilder secResult = null;
        private StringBuilder terResult = null;
        private CollationElementIterator sourceCursor = null;
        private CollationElementIterator targetCursor = null;
    }
}

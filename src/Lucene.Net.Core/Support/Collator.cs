using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Mimick's Java's Collator class
    /// </summary>
    public abstract class Collator<T> : IComparer<T>, ICloneable
    {
        /**
         * Collator strength value.  When set, only PRIMARY differences are
         * considered significant during comparison. The assignment of strengths
         * to language features is locale dependant. A common example is for
         * different base letters ("a" vs "b") to be considered a PRIMARY difference.
         * @see java.text.Collator#setStrength
         * @see java.text.Collator#getStrength
         */
        public readonly static int PRIMARY = 0;
        /**
         * Collator strength value.  When set, only SECONDARY and above differences are
         * considered significant during comparison. The assignment of strengths
         * to language features is locale dependant. A common example is for
         * different accented forms of the same base letter ("a" vs "\u00E4") to be
         * considered a SECONDARY difference.
         * @see java.text.Collator#setStrength
         * @see java.text.Collator#getStrength
         */
        public readonly static int SECONDARY = 1;
        /**
         * Collator strength value.  When set, only TERTIARY and above differences are
         * considered significant during comparison. The assignment of strengths
         * to language features is locale dependant. A common example is for
         * case differences ("a" vs "A") to be considered a TERTIARY difference.
         * @see java.text.Collator#setStrength
         * @see java.text.Collator#getStrength
         */
        public readonly static int TERTIARY = 2;

        /**
        * Collator strength value.  When set, all differences are
        * considered significant during comparison. The assignment of strengths
        * to language features is locale dependant. A common example is for control
        * characters ("&#092;u0001" vs "&#092;u0002") to be considered equal at the
        * PRIMARY, SECONDARY, and TERTIARY levels but different at the IDENTICAL
        * level.  Additionally, differences between pre-composed accents such as
        * "&#092;u00C0" (A-grave) and combining accents such as "A&#092;u0300"
        * (A, combining-grave) will be considered significant at the IDENTICAL
        * level if decomposition is set to NO_DECOMPOSITION.
        */
        public readonly static int IDENTICAL = 3;

        /**
         * Decomposition mode value. With NO_DECOMPOSITION
         * set, accented characters will not be decomposed for collation. This
         * is the default setting and provides the fastest collation but
         * will only produce correct results for languages that do not use accents.
         * @see java.text.Collator#getDecomposition
         * @see java.text.Collator#setDecomposition
         */
        public readonly static int NO_DECOMPOSITION = 0;

        /**
         * Decomposition mode value. With CANONICAL_DECOMPOSITION
         * set, characters that are canonical variants according to Unicode
         * standard will be decomposed for collation. This should be used to get
         * correct collation of accented characters.
         * <p>
         * CANONICAL_DECOMPOSITION corresponds to Normalization Form D as
         * described in
         * <a href="http://www.unicode.org/unicode/reports/tr15/tr15-23.html">Unicode
         * Technical Report #15</a>.
         * @see java.text.Collator#getDecomposition
         * @see java.text.Collator#setDecomposition
         */
        public readonly static int CANONICAL_DECOMPOSITION = 1;

        /**
         * Decomposition mode value. With FULL_DECOMPOSITION
         * set, both Unicode canonical variants and Unicode compatibility variants
         * will be decomposed for collation.  This causes not only accented
         * characters to be collated, but also characters that have special formats
         * to be collated with their norminal form. For example, the half-width and
         * full-width ASCII and Katakana characters are then collated together.
         * FULL_DECOMPOSITION is the most complete and therefore the slowest
         * decomposition mode.
         * <p>
         * FULL_DECOMPOSITION corresponds to Normalization Form KD as
         * described in
         * <a href="http://www.unicode.org/unicode/reports/tr15/tr15-23.html">Unicode
         * Technical Report #15</a>.
         * @see java.text.Collator#getDecomposition
         * @see java.text.Collator#setDecomposition
         */
        public readonly static int FULL_DECOMPOSITION = 2;

        /// <summary>
        /// Gets the Collator for the current culture.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Collator<T> GetInstance()
        {
            return GetInstance(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Gets the Collator for the specified culture.
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static Collator<T> GetInstance(CultureInfo culture)
        {
            WeakReference<Collator<T>> @ref = cache.Get(culture);
            Collator<T> result = null;
            if (@ref == null || !@ref.TryGetTarget(out result))
            {
                // LUCENENET TODO: Get the collator instance from a provider...
                
                //while(true)
                //{
                //    if (@ref != null)
                //    {
                //        // Remove the empty WeakReference if any
                //        cache.Remove(culture, @ref);
                //    }
                //    @ref = cache.p
                //}
            }
            return (Collator<T>)result.Clone(); // make the world safe
        }

        /// <summary>
        /// Compares the source string to the target string according to the
        /// collation rules for this Collator.Returns an integer less than,
        /// equal to or greater than zero depending on whether the source String is
        /// less than, equal to or greater than the target string.  See the Collator
        /// class description for an example of use.
        /// <p>
        /// For a one time comparison, this method has the best performance.If a
        /// given String will be involved in multiple comparisons, CollationKey.compareTo
        /// has the best performance.See the Collator class description for an example
        /// using CollationKeys.
        /// </summary>
        /// <param name="source">the source string.</param>
        /// <param name="target">the target string.</param>
        /// <returns>Returns an integer value.Value is less than zero if source is less than 
        /// target, value is zero if source and target are equal, value is greater than zero
        /// if source is greater than target.</returns>
        public abstract int Compare(string source, string target);

        public int Compare(T source, T target)
        {
            var s = source as string;
            var t = target as string;
            s = (s == null) ? s.ToString() : s;
            t = (t == null) ? s.ToString() : t;
            return Compare(s, t);
        }



        public object Clone()
        {
            throw new NotImplementedException();
        }

        private int strength = 0;
        private int decmp = 0;
        private static readonly ConcurrentHashMapWrapper<CultureInfo, WeakReference<Collator<T>>> cache =
            new ConcurrentHashMapWrapper<CultureInfo, WeakReference<Collator<T>>>(new Dictionary<CultureInfo, WeakReference<Collator<T>>>());
    }
}

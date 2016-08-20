using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Support.Collation
{
    /// <summary>
    /// The <code>Collator</code> class performs locale-sensitive
    /// <code>string</code> comparison. You use this class to build
    /// searching and sorting routines for natural language text.
    /// 
    /// <p>
    /// <code>Collator</code> is an abstract base class. Subclasses
    /// implement specific collation strategies. One subclass,
    /// <code>RuleBasedCollator</code>, is currently provided with
    /// the Java Platform and is applicable to a wide set of languages. Other
    /// subclasses may be created to handle more specialized needs.
    /// 
    /// <p>
    /// Like other locale-sensitive classes, you can use the static
    /// factory method, <code>GetInstance</code>, to obtain the appropriate
    /// <code>Collator</code> object for a given locale. You will only need
    /// to look at the subclasses of <code>Collator</code> if you need
    /// to understand the details of a particular collation strategy or
    /// if you need to modify that strategy.
    /// 
    /// <p>
    /// The following example shows how to compare two strings using
    /// the <code>Collator</code> for the default locale.
    /// <blockquote>
    /// <pre>{@code
    /// // Compare two strings in the default locale
    /// Collator{object} myCollator = Collator{object}.GetInstance();
    /// if( myCollator.Compare("abc", "ABC") < 0 )
    ///     Console.WriteLine("abc is less than ABC");
    /// else
    ///     Console.WriteLine("abc is greater than or equal to ABC");
    /// }</pre>
    /// </blockquote>
    /// 
    /// <p>
    /// You can set a <code>Collator</code>'s <em>strength</em> property
    /// to determine the level of difference considered significant in
    /// comparisons. Four strengths are provided: <code>PRIMARY</code>,
    /// <code>SECONDARY</code>, <code>TERTIARY</code>, and <code>IDENTICAL</code>.
    /// The exact assignment of strengths to language features is
    /// locale dependant.  For example, in Czech, "e" and "f" are considered
    /// primary differences, while "e" and "&#283;" are secondary differences,
    /// "e" and "E" are tertiary differences and "e" and "e" are identical.
    /// The following shows how both case and accents could be ignored for
    /// US English.
    /// <blockquote>
    /// <pre>
    /// //Get the Collator for US English and set its strength to PRIMARY
    /// Collator usCollator = Collator.getInstance(Locale.US);
    /// usCollator.setStrength(Collator.PRIMARY);
    /// if( usCollator.compare("abc", "ABC") == 0 ) {
    ///     System.out.println("Strings are equivalent");
    /// }
    /// </pre>
    /// </blockquote>
    /// <p>
    /// For comparing <code>String</code>s exactly once, the <code>compare</code>
    /// method provides the best performance. When sorting a list of
    /// <code>String</code>s however, it is generally necessary to compare each
    /// <code>String</code> multiple times. In this case, <code>CollationKey</code>s
    /// provide better performance. The <code>CollationKey</code> class converts
    /// a <code>String</code> to a series of bits that can be compared bitwise
    /// against other <code>CollationKey</code>s. A <code>CollationKey</code> is
    /// created by a <code>Collator</code> object for a given <code>String</code>.
    /// <br>
    /// <strong>Note:</strong> <code>CollationKey</code>s from different
    /// <code>Collator</code>s can not be compared. See the class description
    /// for {@link CollationKey}
    /// for an example using <code>CollationKey</code>s.
    /// </summary>
    public abstract class Collator : IComparer, ICloneable
    {
        // LUCENENET TODO: This class should inherit System.Globalization.SortInfo.
        // It would be ideal if the SortInfo property on System.Globalization.CultureInfo
        // can be replaced with this new instance, but failing that we could just have an
        // extension method on CultureInfo named GetCollator().

        /// <summary>
        /// Gets the Collator for the current culture.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Collator GetInstance()
        {
            return GetInstance(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Gets the Collator for the specified culture.
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static Collator GetInstance(CultureInfo culture)
        {
            WeakReference<Collator> @ref = cache.ContainsKey(culture) ? cache[culture] : null;
            Collator result = null;
            if (@ref == null || !@ref.TryGetTarget(out result))
            {
                // LUCENENET TODO: Get the collator instance from a provider...

                while (true)
                {
                    if (@ref != null)
                    {
                        // Remove the empty WeakReference if any
                        cache.Remove(culture);
                    }
                    lock (syncLock)
                    {
                        @ref = cache.ContainsKey(culture) ? cache[culture] : null;
                        if (@ref == null)
                        {
                            cache[culture] = new WeakReference<Collator>(result);
                            break;
                        }
                    }
                    Collator cachedColl;
                    if (@ref.TryGetTarget(out cachedColl))
                    {
                        result = cachedColl;
                        break;
                    }
                }
            }
            return (Collator)result.Clone(); // make the world safe
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

        /// <summary>
        /// Compares its two arguments for order.  Returns a negative integer,
        /// zero, or a positive integer as the first argument is less than, equal
        /// to, or greater than the second.
        /// <p>
        /// This implementation merely returns
        ///     <code> compare((String)o1, (String)o2) </code>.
        /// </summary>
        /// <param name="source">the source string.</param>
        /// <param name="target">the target string.</param>
        /// <returns>a negative integer, zero, or a positive integer as the
        /// first argument is less than, equal to, or greater than the
        /// second.</returns>
        public int Compare(object source, object target)
        {
            var s = source as string;
            var t = target as string;
            s = (s == null) ? s.ToString() : s;
            t = (t == null) ? s.ToString() : t;
            return Compare(s, t);
        }

        /// <summary>
        /// Transforms the string into a series of bits that can be compared bitwise
        /// to other SortKeys. SortKeys provide better performance than
        /// Collator.compare when strings are involved in multiple comparisons.
        /// See the Collator class description for an example using SortKeys.
        /// </summary>
        /// <param name="source">source the string to be transformed into a collation key.</param>
        /// <returns>the SortKey for the given string based on this Collator's collation
        /// rules. If the source string is null, a null SortKey is returned.</returns>
        public abstract CollationKey GetCollationKey(string source);

        /// <summary>
        /// Convenience method for comparing the equality of two strings based on
        /// this Collator's collation rules.
        /// </summary>
        /// <param name="source">the source string to be compared with.</param>
        /// <param name="target">the target string to be compared with.</param>
        /// <returns><b>true</b> if the strings are equal according to the collation
        /// rules, otherwise <b>false</b>.</returns>
        public bool Equals(string source, string target)
        {
            return (Compare(source, target) == 0);
        }

        /// <summary>
        /// Gets or sets this Collator's strength property.  The strength property determines
        /// the minimum level of difference considered significant during comparison.
        /// See the Collator class description for an example of use.
        /// </summary>
        public CollatorStrength Strength
        {
            get { return strength; }
            set
            {
                if (!Enum.IsDefined(typeof(CollatorStrength), value))
                {
                    throw new ArgumentException("Incorrect comparison level.");
                }
                strength = value;
            }
        }

        /// <summary>
        /// Gets or sets the decomposition mode of this Collator. Decomposition mode
        /// determines how Unicode composed characters are handled. Adjusting
        /// decomposition mode allows the user to select between faster and more
        /// complete collation behavior.
        /// </summary>
        public DecompositionMode Decomposition
        {
            get { return decomposition; }
            set
            {
                if (!Enum.IsDefined(typeof(CollatorStrength), value))
                {
                    throw new ArgumentException("Wrong decomposition mode.");
                }
                decomposition = value;
            }
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public static IEnumerable<CultureInfo> GetAvaliableCultures()
        {
            // LUCENENET TODO: Finish
            throw new NotImplementedException();
        }

        /// <summary>
        /// Overrides ICloneable
        /// </summary>
        public abstract object Clone();

        /// <summary>
        /// Compares the equality of two Collators.
        /// </summary>
        /// <param name="that">the Collator to be compared with this.</param>
        /// <returns><b>true</b> if this Collator is the same as that Collator;
        /// otherwise <b>false</b>.</returns>
        public override bool Equals(object that)
        {
            if (this == that)
            {
                return true;
            }
            if (that == null)
            {
                return false;
            }
            if (GetType() != that.GetType())
            {
                return false;
            }
            Collator other = (Collator)that;
            return ((strength == other.strength) &&
                    (decomposition == other.decomposition));
        }

        /// <summary>
        /// Generates the hash code for this Collator.
        /// </summary>
        /// <returns></returns>
        public abstract override int GetHashCode();


        protected Collator()
        {
            strength = CollatorStrength.TERTIARY;
            decomposition = DecompositionMode.CANONICAL_DECOMPOSITION;
        }

        private CollatorStrength strength = CollatorStrength.PRIMARY;
        private DecompositionMode decomposition = DecompositionMode.NO_DECOMPOSITION;
        private static readonly ConcurrentHashMapWrapper<CultureInfo, WeakReference<Collator>> cache =
            new ConcurrentHashMapWrapper<CultureInfo, WeakReference<Collator>>(new Dictionary<CultureInfo, WeakReference<Collator>>());
        private static object syncLock = new object();

        //
        // FIXME: These three constants should be removed.
        //

        /// <summary>
        /// LESS is returned if source string is compared to be less than target
        /// string in the compare() method.
        /// </summary>
        internal readonly static int LESS = -1;
        /// <summary>
        /// EQUAL is returned if source string is compared to be equal to target
        /// string in the compare() method.
        /// </summary>
        internal readonly static int EQUAL = 0;
        /// <summary>
        /// GREATER is returned if source string is compared to be greater than
        /// target string in the compare() method.
        /// </summary>
        internal readonly static int GREATER = 1;
    }

    public enum CollatorStrength
    {
        /// <summary>
        /// Collator strength value.  When set, only PRIMARY differences are
        /// considered significant during comparison. The assignment of strengths
        /// to language features is locale dependant. A common example is for
        /// different base letters ("a" vs "b") to be considered a PRIMARY difference.
        /// </summary>
        PRIMARY = 0,
        /// <summary>
        /// Collator strength value.  When set, only SECONDARY and above differences are
        /// considered significant during comparison. The assignment of strengths
        /// to language features is locale dependant. A common example is for
        /// different accented forms of the same base letter ("a" vs "\u00E4") to be
        /// considered a SECONDARY difference.
        /// </summary>
        SECONDARY = 1,
        /// <summary>
        /// Collator strength value.  When set, only TERTIARY and above differences are
        /// considered significant during comparison. The assignment of strengths
        /// to language features is locale dependant. A common example is for
        /// case differences ("a" vs "A") to be considered a TERTIARY difference.
        /// </summary>
        TERTIARY = 2,
        /// <summary>
        /// Collator strength value.  When set, all differences are
        /// considered significant during comparison. The assignment of strengths
        /// to language features is locale dependant. A common example is for control
        /// characters ("&#092;u0001" vs "&#092;u0002") to be considered equal at the
        /// PRIMARY, SECONDARY, and TERTIARY levels but different at the IDENTICAL
        /// level.  Additionally, differences between pre-composed accents such as
        /// "&#092;u00C0" (A-grave) and combining accents such as "A&#092;u0300"
        /// (A, combining-grave) will be considered significant at the IDENTICAL
        /// level if decomposition is set to NO_DECOMPOSITION.
        /// </summary>
        IDENTICAL = 3
    }

    public enum DecompositionMode
    {
        /// <summary>
        /// Decomposition mode value. With NO_DECOMPOSITION
        /// set, accented characters will not be decomposed for collation. This
        /// is the default setting and provides the fastest collation but
        /// will only produce correct results for languages that do not use accents.
        /// </summary>
        NO_DECOMPOSITION = 0,
        /// <summary>
        /// Decomposition mode value. With CANONICAL_DECOMPOSITION
        /// set, characters that are canonical variants according to Unicode
        /// standard will be decomposed for collation. This should be used to get
        /// correct collation of accented characters.
        /// <p>
        /// CANONICAL_DECOMPOSITION corresponds to Normalization Form D as
        /// described in
        /// <a href = "http://www.unicode.org/unicode/reports/tr15/tr15-23.html" > Unicode
        /// Technical Report #15</a>.
        /// </summary>
        CANONICAL_DECOMPOSITION = 1,
        /// <summary>
        /// Decomposition mode value. With FULL_DECOMPOSITION
        /// set, both Unicode canonical variants and Unicode compatibility variants
        /// will be decomposed for collation.  This causes not only accented
        /// characters to be collated, but also characters that have special formats
        /// to be collated with their norminal form. For example, the half-width and
        /// full-width ASCII and Katakana characters are then collated together.
        /// FULL_DECOMPOSITION is the most complete and therefore the slowest
        /// decomposition mode.
        /// <p>
        /// FULL_DECOMPOSITION corresponds to Normalization Form KD as
        /// described in
        /// <a href = "http://www.unicode.org/unicode/reports/tr15/tr15-23.html" > Unicode
        /// Technical Report #15</a>.
        /// </summary>
        FULL_DECOMPOSITION = 2
    }
}

using System;

namespace Lucene.Net.Support.Collation
{
    /// <summary>
    /// A <code>CollationKey</code> represents a <code>String</code> under the
    /// rules of a specific <code>Collator</code> object. Comparing two
    /// <code>CollationKey</code>s returns the relative order of the
    /// <code>String</code>s they represent. Using <code>CollationKey</code>s
    /// to compare <code>String</code>s is generally faster than using
    /// <code>Collator.compare</code>. Thus, when the <code>String</code>s
    /// must be compared multiple times, for example when sorting a list
    /// of <code>String</code>s. It's more efficient to use <code>CollationKey</code>s.
    ///
    /// <p>
    /// You can not create <code>CollationKey</code>s directly. Rather,
    /// generate them by calling <code>Collator.getCollationKey</code>.
    /// You can only compare <code>CollationKey</code>s generated from
    /// the same <code>Collator</code> object.
    ///
    /// <p>
    /// Generating a <code>CollationKey</code> for a <code>String</code>
    /// involves examining the entire <code>String</code>
    /// and converting it to series of bits that can be compared bitwise. This
    /// allows fast comparisons once the keys are generated. The cost of generating
    /// keys is recouped in faster comparisons when <code>String</code>s need
    /// to be compared many times. On the other hand, the result of a comparison
    /// is often determined by the first couple of characters of each <code>String</code>.
    /// <code>Collator.compare</code> examines only as many characters as it needs which
    /// allows it to be faster when doing single comparisons.
    /// <p>
    /// The following example shows how <code>CollationKey</code>s might be used
    /// to sort a list of <code>String</code>s.
    /// <blockquote>
    /// <pre>{@code
    /// // Create an array of CollationKeys for the Strings to be sorted.
    /// Collator myCollator = Collator.GetInstance();
    /// CollationKey[] keys = new CollationKey[3];
    /// keys[0] = myCollator.GetCollationKey("Tom");
    /// keys[1] = myCollator.GetCollationKey("Dick");
    /// keys[2] = myCollator.GetCollationKey("Harry");
    /// sort(keys);
    ///
    /// //...
    ///
    /// // Inside body of sort routine, compare keys this way
    /// if (keys[i].CompareTo(keys[j]) > 0)
    ///    // swap keys[i] and keys[j]
    ///
    /// //...
    ///
    /// // Finally, when we've returned from sort.
    /// Console.WriteLine(keys[0].SourceString);
    /// Console.WriteLine(keys[1].SourceString);
    /// Console.WriteLine(keys[2].SourceString);
    /// }</pre>
    /// </blockquote>
    /// </summary>
    public abstract class CollationKey : IComparable<CollationKey>
    {
        // LUCENENET TODO: This class should be replaced by System.Globalization.SortKey

        /// <summary>
        /// Compare this CollationKey to the target CollationKey. The collation rules of the
        /// Collator object which created these keys are applied. <strong>Note:</strong>
        /// CollationKeys created by different Collators can not be compared.
        /// </summary>
        /// <param name="target">target CollationKey</param>
        /// <returns>Returns an integer value. Value is less than zero if this is less
        /// than target, value is zero if this and target are equal and value is greater than
        /// zero if this is greater than target.</returns>
        public abstract int CompareTo(CollationKey target);

        /// <summary>
        /// Returns the String that this CollationKey represents.
        /// </summary>
        public string SourceString
        {
            get { return source; }
        }

        /// <summary>
        /// Converts the CollationKey to a sequence of bits. If two CollationKeys
        /// could be legitimately compared, then one could compare the byte arrays
        /// for each of those keys to obtain the same result.  Byte arrays are
        /// organized most significant byte first.
        /// </summary>
        /// <returns>a byte array representation of the CollationKey</returns>
        public abstract byte[] ToByteArray();

        /// <summary>
        /// CollationKey constructor.
        /// </summary>
        /// <param name="source">the source string</param>
        /// <exception cref="ArgumentNullException">if the source string is null</exception>
        protected CollationKey(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            this.source = source;
        }

        private readonly string source;
    }
}

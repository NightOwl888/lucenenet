using J2N.Text;
using System;
using System.Collections;
using System.Collections.Generic;

namespace J2N.Collections.Generic
{
    /// <summary>
    /// Represents a collection of objects that is maintained in sorted order.
    /// <para/>
    /// <see cref="SortedSet{T}"/> adds the following features to <see cref="System.Collections.Generic.SortedSet{T}"/>:
    /// <list type="bullet">
    ///     <item><description>
    ///         Overrides the <see cref="Equals(object)"/> and <see cref="GetHashCode()"/> methods to compare lists
    ///         using structural equality by default. Also, <see cref="IStructuralEquatable"/> is implemented so the
    ///         default behavior can be overridden.
    ///     </description></item>
    ///     <item><description>
    ///         Overrides the <see cref="ToString()"/> methods to list the contents of the list
    ///         by default. Also, <see cref="IFormatProvider"/> is implemented so the
    ///         default behavior can be overridden.
    ///     </description></item>
    /// </list>
    /// <para/>
    /// Usage Note: This class is intended to be a direct replacement for <see cref="System.Collections.Generic.SortedSet{T}"/> in order
    /// to provide default structural equality and formatting behavior similar to Java. Note that the <see cref="ToString()"/>
    /// method uses the current culture by default to behave like other components in .NET. To exactly match Java's culture-neutral behavior,
    /// call <c>ToString(StringFormatter.InvariantCulture)</c>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the set.</typeparam>
    public class SortedSet<T> : System.Collections.Generic.SortedSet<T>, IStructuralEquatable, IFormattable
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SortedSet{T}"/> class.
        /// </summary>
        public SortedSet() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SortedSet{T}"/> class that uses a specified comparer.
        /// </summary>
        /// <param name="comparer">The default comparer to use for comparing objects.</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <c>null</c>.</exception>
        /// <remarks>
        /// If <paramref name="comparer"/> does not implement <see cref="IComparable{T}"/>, you must specify
        /// an <see cref="IComparer{T}"/> object to be used.
        /// </remarks>
        public SortedSet(IComparer<T> comparer) : base(comparer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SortedSet{T}"/> class that contains elements copied
        /// from a specified enumerable collection.
        /// </summary>
        /// <param name="collection">The enumerable collection to be copied.</param>
        /// <remarks>
        /// Duplicate elements in the enumerable collection are not copied into the new instance of the
        /// <see cref="SortedSet{T}"/> class, and no exceptions are thrown.
        /// <para/>
        /// This constructor is an <c>O(n log n)</c> operation, where <c>n</c> is the number of elements
        /// in the <paramref name="collection"/> parameter.
        /// </remarks>
        public SortedSet(IEnumerable<T> collection) : base(collection) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SortedSet{T}"/> class that contains elements copied
        /// from a specified enumerable collection and that uses a specified comparer.
        /// </summary>
        /// <param name="collection">The enumerable collection to be copied.</param>
        /// <param name="comparer">The default comparer to use for comparing objects.</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public SortedSet(IEnumerable<T> collection, IComparer<T> comparer) : base(collection, comparer) { }

#if FEATURE_SERIALIZABLE

        /// <summary>
        /// Initializes a new instance of the <see cref="SortedSet{T}"/> class that contains serialized data.
        /// </summary>
        /// <param name="info">The object that contains the information that is required to serialize
        /// the <see cref="SortedSet{T}"/> object.</param>
        /// <param name="context">The structure that contains the source and destination of the serialized
        /// stream associated with the <see cref="SortedSet{T}"/> object.</param>
        /// <remarks>
        /// This constructor is called during deserialization to reconstitute an object that is transmitted over a stream.
        /// </remarks>
        protected SortedSet(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

#endif

        #endregion

        #region Structural Equality

        /// <summary>
        /// Determines whether the specified object is structurally equal to the current set
        /// using rules provided by the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer"/> implementation to use to determine
        /// whether the current object and <paramref name="other"/> are structurally equal.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is structurally equal to the current set;
        /// otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="comparer"/> is <c>null</c>.</exception>
        public virtual bool Equals(object other, IEqualityComparer comparer)
            => SetEqualityComparer<T>.Equals(this, other, comparer);

        /// <summary>
        /// Gets the hash code representing the current set using rules specified by the
        /// provided <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="IEqualityComparer"/> implementation to use to generate
        /// the hash code.</param>
        /// <returns>A hash code representing the current set.</returns>
        public virtual int GetHashCode(IEqualityComparer comparer)
            => SetEqualityComparer<T>.GetHashCode(this, comparer);

        /// <summary>
        /// Determines whether the specified object is structurally equal to the current set
        /// using rules similar to those in the JDK's AbstactSet class. Two sets are considered
        /// equal when they both contain the same objects (in any order).
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object implements <see cref="ISet{T}"/>
        /// and it contains the same elements; otherwise, <c>false</c>.</returns>
        /// <seealso cref="Equals(object, IEqualityComparer)"/>
        public override bool Equals(object obj)
            => Equals(obj, SetEqualityComparer<T>.Default);

        /// <summary>
        /// Gets the hash code for the current list. The hash code is calculated 
        /// by taking each nested element's hash code into account.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        /// <seealso cref="GetHashCode(IEqualityComparer)"/>
        public override int GetHashCode()
            => GetHashCode(SetEqualityComparer<T>.Default);

        #endregion

        #region ToString

        /// <summary>
        /// Returns a string that represents the current set using the specified
        /// <paramref name="format"/> and <paramref name="formatProvider"/>.
        /// </summary>
        /// <returns>A string that represents the current set.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="format"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">
        /// <paramref name="format"/> is invalid.
        /// <para/>
        /// -or-
        /// <para/>
        /// The index of a format item is not zero.
        /// </exception>
        public virtual string ToString(string format, IFormatProvider formatProvider)
            => string.Format(formatProvider, format, this);

        /// <summary>
        /// Returns a string that represents the current set using
        /// <see cref="StringFormatter.CurrentCulture"/>.
        /// <para/>
        /// The presentation has a specific format. It is enclosed by square
        /// brackets ("[]"). Elements are separated by ', ' (comma and space).
        /// </summary>
        /// <returns>A string that represents the current set.</returns>
        public override string ToString()
            => ToString("{0}", StringFormatter.CurrentCulture);

        /// <summary>
        /// Returns a string that represents the current set using the specified
        /// <paramref name="formatProvider"/>.
        /// </summary>
        /// <returns>A string that represents the current set.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="formatProvider"/> is <c>null</c>.</exception>
        public virtual string ToString(IFormatProvider formatProvider)
            => ToString("{0}", formatProvider);

        /// <summary>
        /// Returns a string that represents the current set using the specified
        /// <paramref name="format"/> and <see cref="StringFormatter.CurrentCulture"/>.
        /// <para/>
        /// The presentation has a specific format. It is enclosed by square
        /// brackets ("[]"). Elements are separated by ', ' (comma and space).
        /// </summary>
        /// <returns>A string that represents the current set.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="format"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">
        /// <paramref name="format"/> is invalid.
        /// <para/>
        /// -or-
        /// <para/>
        /// The index of a format item is not zero.
        /// </exception>
        public virtual string ToString(string format)
            => ToString(format, StringFormatter.CurrentCulture);

        #endregion
    }
}

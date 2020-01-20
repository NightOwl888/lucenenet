using J2N.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Support
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

    internal sealed class ConcurrentSet<T> : ISet<T>, ICollection, IStructuralEquatable, IFormattable
    {
        private readonly object syncRoot = new object();
        private readonly ISet<T> set;

        public ConcurrentSet(ISet<T> set)
        {
            this.set = set ?? throw new ArgumentNullException(nameof(set));
        }

        public bool Add(T item)
        {
            lock (syncRoot)
                return set.Add(item);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            lock (syncRoot)
                set.ExceptWith(other);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            lock (syncRoot)
                set.IntersectWith(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            lock (syncRoot)
                return set.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            lock (syncRoot)
                return set.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            lock (syncRoot)
                return set.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            lock (syncRoot)
                return set.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            lock (syncRoot)
                return set.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            lock (syncRoot)
                return set.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            lock (syncRoot)
                set.SymmetricExceptWith(other);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            lock (syncRoot)
                set.UnionWith(other);
        }

        void ICollection<T>.Add(T item)
        {
            lock (syncRoot)
                set.Add(item);
        }

        public void Clear()
        {
            lock (syncRoot)
                set.Clear();
        }

        public bool Contains(T item)
        {
            lock (syncRoot)
                return set.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (syncRoot)
                set.CopyTo(array, arrayIndex);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException("Only single dimensional arrays are supported for the requested action.", nameof(array));
                //throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            if (array.GetLowerBound(0) != 0)
                throw new ArgumentException("The lower bound of target array must be zero.", nameof(array));
                //throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Non-negative number required.");
                //throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (array.Length - index < Count)
                throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");
                //throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);

            T[]/*?*/ tarray = array as T[];
            if (tarray != null)
            {
                CopyTo(tarray, index);
            }
            else
            {
                object/*?*/[]/*?*/ objects = array as object[];
                if (objects == null)
                {
                    throw new ArgumentException("Target array type is not compatible with the type of items in the collection.", nameof(array));
                    //throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }

                try
                {
                    foreach (var item in set)
                        objects[index++] = item;
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException("Target array type is not compatible with the type of items in the collection.", nameof(array));
                    //throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }
            }
        }

        public int Count
        {
            get
            {
                lock (syncRoot)
                    return set.Count;
            }
        }

        public bool IsReadOnly => set.IsReadOnly;

        public bool IsSynchronized => true;

        public object SyncRoot => syncRoot;

        public bool Remove(T item)
        {
            lock (syncRoot)
                return set.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            // Make a copy of the contents since enumeration is lazy and not thread-safe
            T[] array = new T[set.Count];
            lock (syncRoot)
            {
                set.CopyTo(array, 0);
            }
            return ((IEnumerable<T>)array).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

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
        public bool Equals(object other, IEqualityComparer comparer)
        {
            lock (syncRoot)
                return JCG.SetEqualityComparer<T>.Equals(set, other, comparer);
        }

        /// <summary>
        /// Gets the hash code representing the current set using rules specified by the
        /// provided <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="IEqualityComparer"/> implementation to use to generate
        /// the hash code.</param>
        /// <returns>A hash code representing the current set.</returns>
        public int GetHashCode(IEqualityComparer comparer)
        {
            lock (syncRoot)
                return JCG.SetEqualityComparer<T>.GetHashCode(set, comparer);
        }

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
            => Equals(obj, JCG.SetEqualityComparer<T>.Default);

        /// <summary>
        /// Gets the hash code for the current list. The hash code is calculated 
        /// by taking each nested element's hash code into account.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        /// <seealso cref="GetHashCode(IEqualityComparer)"/>
        public override int GetHashCode()
            => GetHashCode(JCG.SetEqualityComparer<T>.Default);

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
        public string ToString(string format, IFormatProvider formatProvider)
        {
            lock (syncRoot)
            {
                if (set is IFormattable formattable)
                    return formattable.ToString(format, formatProvider);

                return string.Format(formatProvider, format, set);
            }
        }

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
        public string ToString(IFormatProvider formatProvider)
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
        public string ToString(string format)
            => ToString(format, StringFormatter.CurrentCulture);

        

        #endregion
    }
}
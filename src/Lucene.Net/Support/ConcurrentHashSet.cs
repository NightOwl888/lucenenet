using J2N.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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

    public sealed class ConcurrentHashSet<T> : ISet<T>, IStructuralEquatable, IFormattable
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly JCG.HashSet<T> _set;

        public ConcurrentHashSet()
        {
            _set = new JCG.HashSet<T>();
        }

        public ConcurrentHashSet(IEnumerable<T> collection)
        {
            _set = new JCG.HashSet<T>(collection);
        }

        public ConcurrentHashSet(IEqualityComparer<T> comparer)
        {
            _set = new JCG.HashSet<T>(comparer);
        }

        public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
        {
            _set = new JCG.HashSet<T>(collection, comparer);
        }

        public bool Add(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                return _set.Add(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            try
            {
                _set.ExceptWith(other);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            try
            {
                _set.IntersectWith(other);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            _lock.EnterReadLock();
            try
            {
                return _set.IsProperSubsetOf(other);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            _lock.EnterReadLock();
            try
            {
                return _set.IsProperSupersetOf(other);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            _lock.EnterReadLock();
            try
            {
                return _set.IsSubsetOf(other); ;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            _lock.EnterReadLock();
            try
            {
                return _set.IsSupersetOf(other);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            _lock.EnterReadLock();
            try
            {
                return _set.Overlaps(other);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            _lock.EnterReadLock();
            try
            {
                return _set.SetEquals(other);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            try
            {
                _set.SymmetricExceptWith(other);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void UnionWith(IEnumerable<T> other)
        {
            _lock.EnterWriteLock();
            try
            {
                _set.UnionWith(other);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        void ICollection<T>.Add(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                _set.Add(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _set.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool Contains(T item)
        {
            _lock.EnterReadLock();
            try
            {
                return _set.Contains(item);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _lock.EnterReadLock();
            try
            {
                _set.CopyTo(array, arrayIndex);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _set.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public bool IsReadOnly => ((ISet<T>)_set).IsReadOnly;

        public bool Remove(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                return _set.Remove(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            _lock.EnterReadLock();
            try
            {
                // Make a copy of the contents since enumeration is lazy and not thread-safe
                T[] array = new T[_set.Count];
                _set.CopyTo(array, 0);
                return ((IEnumerable<T>)array).GetEnumerator();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
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
            _lock.EnterReadLock();
            try
            {
                
                return _set.Equals(other, comparer);
            }
            finally
            {
                _lock.ExitReadLock();
            }
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
            _lock.EnterReadLock();
            try
            {
                return _set.GetHashCode(comparer);
            }
            finally
            {
                _lock.ExitReadLock();
            }
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
            _lock.EnterReadLock();
            try
            {
                return _set.ToString(format, formatProvider);
            }
            finally
            {
                _lock.ExitReadLock();
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
﻿using J2N;
using J2N.Collections.Generic.Extensions;
using J2N.Globalization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
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

    public static class Collections
    {
        public static IList<T> EmptyList<T>()
        {
            return new List<T>(); // LUCENENET NOTE: Enumerable.Empty<T>() fails to cast to IList<T> on .NET Core 3.x, so we just create a new list
        }

        public static IDictionary<TKey, TValue> EmptyMap<TKey, TValue>()
        {
            return new Dictionary<TKey, TValue>();
        }

        public static ISet<T> NewSetFromMap<T, S>(IDictionary<T, bool?> map)
        {
            return new SetFromMap<T>(map);
        }

        public static void Reverse<T>(IList<T> list)
        {
            int size = list.Count;
            for (int i = 0, mid = size >> 1, j = size - 1; i < mid; i++, j--)
            {
                list.Swap(i, j);
            }
        }

        public static IComparer<T> ReverseOrder<T>()
        {
            return (IComparer<T>)ReverseComparer<T>.REVERSE_ORDER;
        }

        public static IComparer<T> ReverseOrder<T>(IComparer<T> cmp)
        {
            if (cmp == null)
                return ReverseOrder<T>();

            if (cmp is ReverseComparer2<T>)
                return ((ReverseComparer2<T>)cmp).cmp;

            return new ReverseComparer2<T>(cmp);
        }

        public static IDictionary<TKey, TValue> SingletonMap<TKey, TValue>(TKey key, TValue value)
        {
            return new Dictionary<TKey, TValue> { { key, value } };
        }


        /// <summary>
        /// This is the same implementation of ToString from Java's AbstractCollection
        /// (the default implementation for all sets and lists)
        /// </summary>
        public static string ToString<T>(ICollection<T> collection)
        {
            if (collection.Count == 0)
            {
                return "[]";
            }

            bool isValueType = typeof(T).GetTypeInfo().IsValueType;
            using (var it = collection.GetEnumerator())
            {
                StringBuilder sb = new StringBuilder();
                sb.Append('[');
                it.MoveNext();
                while (true)
                {
                    T e = it.Current;
                    sb.Append(object.ReferenceEquals(e, collection) ? "(this Collection)" : (isValueType ? e.ToString() : ToString(e)));
                    if (!it.MoveNext())
                    {
                        return sb.Append(']').ToString();
                    }
                    sb.Append(',').Append(' ');
                }
            }
        }

        /// <summary>
        /// This is the same implementation of ToString from Java's AbstractCollection
        /// (the default implementation for all sets and lists), plus the ability
        /// to specify culture for formatting of nested numbers and dates. Note that
        /// this overload will change the culture of the current thread.
        /// </summary>
        public static string ToString<T>(ICollection<T> collection, CultureInfo culture)
        {
            using (var context = new CultureContext(culture))
            {
                return ToString(collection);
            }
        }

        /// <summary>
        /// This is the same implementation of ToString from Java's AbstractMap
        /// (the default implementation for all dictionaries)
        /// </summary>
        public static string ToString<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary.Count == 0)
            {
                return "{}";
            }

            bool keyIsValueType = typeof(TKey).GetTypeInfo().IsValueType;
            bool valueIsValueType = typeof(TValue).GetTypeInfo().IsValueType;
            using (var i = dictionary.GetEnumerator())
            {
                StringBuilder sb = new StringBuilder();
                sb.Append('{');
                i.MoveNext();
                while (true)
                {
                    KeyValuePair<TKey, TValue> e = i.Current;
                    TKey key = e.Key;
                    TValue value = e.Value;
                    sb.Append(object.ReferenceEquals(key, dictionary) ? "(this Dictionary)" : (keyIsValueType ? key.ToString() : ToString(key)));
                    sb.Append('=');
                    sb.Append(object.ReferenceEquals(value, dictionary) ? "(this Dictionary)" : (valueIsValueType ? value.ToString() : ToString(value)));
                    if (!i.MoveNext())
                    {
                        return sb.Append('}').ToString();
                    }
                    sb.Append(',').Append(' ');
                }
            }
        }

        /// <summary>
        /// This is the same implementation of ToString from Java's AbstractMap
        /// (the default implementation for all dictionaries), plus the ability
        /// to specify culture for formatting of nested numbers and dates. Note that
        /// this overload will change the culture of the current thread.
        /// </summary>
        public static string ToString<TKey, TValue>(IDictionary<TKey, TValue> dictionary, CultureInfo culture)
        {
            using (var context = new CultureContext(culture))
            {
                return ToString(dictionary);
            }
        }

        /// <summary>
        /// This is a helper method that assists with recursively building
        /// a string of the current collection and all nested collections.
        /// </summary>
        public static string ToString(object obj)
        {
            Type t = obj.GetType();
            if (t.GetTypeInfo().IsGenericType
                && (t.ImplementsGenericInterface(typeof(ICollection<>)))
                || t.ImplementsGenericInterface(typeof(IDictionary<,>)))
            {
                dynamic genericType = Convert.ChangeType(obj, t);
                return ToString(genericType);
            }

            return obj.ToString();
        }

        /// <summary>
        /// This is a helper method that assists with recursively building
        /// a string of the current collection and all nested collections, plus the ability
        /// to specify culture for formatting of nested numbers and dates. Note that
        /// this overload will change the culture of the current thread.
        /// </summary>
        public static string ToString(object obj, CultureInfo culture)
        {
            using (var context = new CultureContext(culture))
            {
                return ToString(obj);
            }
        }

        #region Nested Types

        #region SetFromMap
        internal class SetFromMap<T> : ICollection<T>, IEnumerable<T>, IEnumerable, ISet<T>, IReadOnlyCollection<T>
#if FEATURE_SERIALIZABLE
            , ISerializable, IDeserializationCallback
#endif
        {
            private readonly IDictionary<T, bool?> m; // The backing map
#if FEATURE_SERIALIZABLE
            [NonSerialized]
#endif
            private ICollection<T> s;

            internal SetFromMap(IDictionary<T, bool?> map)
            {
                if (map.Any())
                    throw new ArgumentException("Map is not empty");
                m = map;
                s = map.Keys;
            }

            public void Clear()
            {
                m.Clear();
            }

            public int Count
            {
                get
                {
                    return m.Count;
                }
            }

            // LUCENENET: IsEmpty doesn't exist here

            public bool Contains(T item)
            {
                return m.ContainsKey(item);
            }

            public bool Remove(T item)
            {
                return m.Remove(item);
            }

            public bool Add(T item)
            {
                m.Add(item, true);
                return m.ContainsKey(item);
            }

            void ICollection<T>.Add(T item)
            {
                m.Add(item, true);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return s.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return s.GetEnumerator();
            }

            // LUCENENET: ToArray() is part of LINQ

            public override string ToString()
            {
                return s.ToString();
            }

            public override int GetHashCode()
            {
                return s.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj == this || s.Equals(obj);
            }

            public virtual bool ContainsAll(IEnumerable<T> other)
            {
                // we don't care about order, so sort both sequences before comparing
                return this.OrderBy(x => x).SequenceEqual(other.OrderBy(x => x));
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                m.Keys.CopyTo(array, arrayIndex);
            }


            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public bool SetEquals(IEnumerable<T> other)
            {
                if (other == null)
                {
                    throw new ArgumentNullException("other");
                }
                SetFromMap<T> set = other as SetFromMap<T>;
                if (set != null)
                {
                    if (this.m.Count != set.Count)
                    {
                        return false;
                    }
                    return this.ContainsAll(set);
                }
                ICollection<T> is2 = other as ICollection<T>;
                if (((is2 != null) && (this.m.Count == 0)) && (is2.Count > 0))
                {
                    return false;
                }
                foreach (var item in this)
                {
                    if (!is2.Contains(item))
                    {
                        return false;
                    }
                }
                return true;
            }

            #region Not Implemented Members
            public void ExceptWith(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public void IntersectWith(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public bool IsProperSubsetOf(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public bool IsProperSupersetOf(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public bool IsSubsetOf(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public bool IsSupersetOf(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public bool Overlaps(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public void SymmetricExceptWith(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

            public void UnionWith(IEnumerable<T> other)
            {
                throw new NotImplementedException();
            }

#if FEATURE_SERIALIZABLE
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                throw new NotImplementedException();
            }
#endif

            public void OnDeserialization(object sender)
            {
                throw new NotImplementedException();
            }
            #endregion
        }
        #endregion SetFromMap

        #region ReverseComparer

        //private class ReverseComparer : IComparer<IComparable>
        //{
        //    internal static readonly ReverseComparer REVERSE_ORDER = new ReverseComparer();


        //    public int Compare(IComparable c1, IComparable c2)
        //    {
        //        return c2.CompareTo(c1);
        //    }
        //}

        // LUCENENET NOTE: When consolidating this, it turns out that only the 
        // CaseInsensitiveComparer works correctly in .NET (not sure why).
        // So, this hybrid was made from the original Java implementation and the
        // original implemenation (above) that used CaseInsensitiveComparer.
        private class ReverseComparer<T> : IComparer<T>
        {
            internal static readonly ReverseComparer<T> REVERSE_ORDER = new ReverseComparer<T>();

            public int Compare(T x, T y)
            {
                return (new CaseInsensitiveComparer()).Compare(y, x);
            }
        }

        #endregion ReverseComparer

        #region ReverseComparer2

        private class ReverseComparer2<T> : IComparer<T>

        {
            /**
             * The comparer specified in the static factory.  This will never
             * be null, as the static factory returns a ReverseComparer
             * instance if its argument is null.
             *
             * @serial
             */
            internal readonly IComparer<T> cmp;

            public ReverseComparer2(IComparer<T> cmp)
            {
                Debug.Assert(cmp != null);
                this.cmp = cmp;
            }

            public int Compare(T t1, T t2)
            {
                return cmp.Compare(t2, t1);
            }

            public override bool Equals(object o)
            {
                return (o == this) ||
                    (o is ReverseComparer2<T> &&
                     cmp.Equals(((ReverseComparer2<T>)o).cmp));
            }

            public override int GetHashCode()
            {
                return cmp.GetHashCode() ^ int.MinValue;
            }

            public IComparer<T> Reversed()
            {
                return cmp;
            }
        }

        #endregion ReverseComparer2

        #endregion Nested Types
    }
}

/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Lucene.Net.Support
{
    public class HashMap<TKey, TValue> : NullableKeyDictionary<TKey, TValue>
    {
        public HashMap()
            : this(0)
        { }

        public HashMap(IEqualityComparer<TKey> comparer)
            : this(0, comparer)
        { }

        public HashMap(int initialCapacity)
            : this(initialCapacity, EqualityComparer<TKey>.Default)
        { }

        public HashMap(int initialCapacity, IEqualityComparer<TKey> comparer)
            : this(null, new Dictionary<NullableKey<TKey>, TValue>(initialCapacity), comparer)
        { }

        public HashMap(IEnumerable<KeyValuePair<TKey, TValue>> dictionary, IEqualityComparer<TKey> comparer)
            : this(dictionary, new Dictionary<NullableKey<TKey>, TValue>(), comparer)
        { }

        internal HashMap(IEnumerable<KeyValuePair<TKey, TValue>> toCopy, IDictionary<NullableKey<TKey>, TValue> dictionary, IEqualityComparer<TKey> comparer)
            : base(dictionary, comparer)
        {
            if (toCopy != null)
            {
                foreach (var pair in toCopy)
                {
                    dictionary.Add(base.ConvertExternalKey(pair.Key), pair.Value);
                }
            }
        }
    }

        //public class HashMap<TKey, TValue> : IDictionary<TKey, TValue>
        //{
        //    private KeyCollection keys;

        //    private readonly IDictionary<NullableKey<TKey>, TValue> dictionary;


        //    public HashMap()
        //        : this(0)
        //    { }

        //    public HashMap(IEqualityComparer<TKey> comparer)
        //        : this(0, comparer)
        //    {
        //    }

        //    public HashMap(int initialCapacity)
        //        : this(initialCapacity, EqualityComparer<TKey>.Default)
        //    {
        //    }

        //    public HashMap(int initialCapacity, IEqualityComparer<TKey> comparer)
        //        //: this(new Dictionary<NullObject<TKey>, TValue>(initialCapacity, comparer), comparer)
        //    {
        //        this.dictionary = new Dictionary<NullableKey<TKey>, TValue>(initialCapacity, new EqualityComparer(comparer));
        //    }

        //    public HashMap(IEnumerable<KeyValuePair<TKey, TValue>> dictionary, IEqualityComparer<TKey> comparer)
        //    //: this(new Dictionary<NullObject<TKey>, TValue>(initialCapacity, comparer), comparer)
        //    {
        //        this.dictionary = new Dictionary<NullableKey<TKey>, TValue>(new EqualityComparer(comparer));
        //        foreach (var pair in dictionary)
        //        {
        //            this.dictionary.Add(pair.Key, pair.Value);
        //        }
        //    }


        //    //public HashMap(IEnumerable<KeyValuePair<TKey, TValue>> other)
        //    //    : this(0)
        //    //{
        //    //    if (other == null)
        //    //        throw new ArgumentNullException(nameof(other));

        //    //    foreach (var kvp in other)
        //    //    {
        //    //        Add(kvp.Key, kvp.Value);
        //    //    }
        //    //}

        //    public class EqualityComparer : IEqualityComparer<NullableKey<TKey>>
        //    {
        //        private readonly IEqualityComparer<TKey> comparer;
        //        public EqualityComparer(IEqualityComparer<TKey> comparer)
        //        {
        //            this.comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        //        }

        //        public bool Equals(NullableKey<TKey> x, NullableKey<TKey> y)
        //        {
        //            return comparer.Equals(x, y);
        //        }

        //        public int GetHashCode(NullableKey<TKey> obj)
        //        {
        //            return comparer.GetHashCode(obj);
        //        }
        //    }

        //    // .NET behavior - throw on invalid key
        //    public virtual TValue this[TKey key]
        //    {
        //        get => dictionary[key];
        //        set => dictionary[key] = value;
        //    }

        //    //// Java behavior - return null on invalid key
        //    //public virtual TValue this[TKey key]
        //    //{
        //    //    get
        //    //    {
        //    //        if (dictionary.TryGetValue(key, out TValue value))
        //    //            return value;
        //    //        return default;
        //    //    }
        //    //    set => dictionary[key] = value;
        //    //}

        //    public virtual ICollection<TKey> Keys
        //    {
        //        get
        //        {
        //            if (keys == null) keys = new KeyCollection(dictionary.Keys);
        //            return keys;
        //        }
        //    }

        //    public virtual ICollection<TValue> Values => dictionary.Values;

        //    public virtual int Count => dictionary.Count;

        //    public virtual bool IsReadOnly => dictionary.IsReadOnly;

        //    public virtual void Add(TKey key, TValue value)
        //    {
        //        dictionary.Add(key, value);
        //    }

        //    public virtual void Add(KeyValuePair<TKey, TValue> item)
        //    {
        //        dictionary.Add(ConvertExternalItem(item));
        //    }

        //    public virtual void Clear()
        //    {
        //        dictionary.Clear();
        //    }

        //    public virtual bool Contains(KeyValuePair<TKey, TValue> item)
        //    {
        //        return dictionary.Contains(ConvertExternalItem(item));
        //    }

        //    public virtual bool ContainsKey(TKey key)
        //    {
        //        return dictionary.ContainsKey(key);
        //    }

        //    public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        //    {
        //        foreach (var item in this)
        //            array[index++] = item;
        //    }

        //    public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        //    {
        //        return new Enumerator(dictionary.GetEnumerator());
        //    }

        //    public virtual bool Remove(TKey key)
        //    {
        //        return dictionary.Remove(key);
        //    }

        //    public virtual bool Remove(KeyValuePair<TKey, TValue> item)
        //    {
        //        return dictionary.Remove(ConvertExternalItem(item));
        //    }

        //    public virtual bool TryGetValue(TKey key, out TValue value)
        //    {
        //        return dictionary.TryGetValue(key, out value);
        //    }

        //    IEnumerator IEnumerable.GetEnumerator()
        //    {
        //        return GetEnumerator();
        //    }

        //    public virtual bool ContainsValue(TValue value)
        //    {
        //        return dictionary.Values.Contains(value);
        //    }


        //    public override bool Equals(object obj)
        //    {
        //        if (!(obj is IDictionary<TKey, TValue>))
        //            return false;

        //        return Collections.Equals(this, obj as IDictionary<TKey, TValue>);
        //    }
        //    public override int GetHashCode()
        //    {
        //        return Collections.GetHashCode(this);
        //    }

        //    public override string ToString()
        //    {
        //        return Collections.ToString(this);
        //    }

        //    private class KeyCollection : ICollection<TKey>
        //    {
        //        private readonly ICollection<NullableKey<TKey>> collection;
        //        public KeyCollection(ICollection<NullableKey<TKey>> collection)
        //        {
        //            this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
        //        }

        //        public int Count => collection.Count;

        //        public bool IsReadOnly => collection.IsReadOnly;

        //        public void Add(TKey item)
        //        {
        //            collection.Add(item);
        //        }

        //        public void Clear()
        //        {
        //            collection.Clear();
        //        }

        //        public bool Contains(TKey item)
        //        {
        //            return collection.Contains(item);
        //        }

        //        public void CopyTo(TKey[] array, int index)
        //        {
        //            foreach (var item in collection)
        //                array[index++] = item.Value;
        //        }

        //        public IEnumerator<TKey> GetEnumerator()
        //        {
        //            foreach (var item in collection)
        //                yield return item.Value;
        //        }

        //        public bool Remove(TKey item)
        //        {
        //            return collection.Remove(item);
        //        }

        //        IEnumerator IEnumerable.GetEnumerator()
        //        {
        //            return GetEnumerator();
        //        }
        //    }

        //    private class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        //    {
        //        private readonly IEnumerator<KeyValuePair<NullableKey<TKey>, TValue>> enumerator;

        //        public Enumerator(IEnumerator<KeyValuePair<NullableKey<TKey>, TValue>> enumerator)
        //        {
        //            this.enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
        //        }

        //        public KeyValuePair<TKey, TValue> Current => ConvertInternalItem(enumerator.Current);

        //        object IEnumerator.Current => Current;

        //        public void Dispose()
        //        {
        //            enumerator.Dispose();
        //        }

        //        public bool MoveNext()
        //        {
        //            return enumerator.MoveNext();
        //        }

        //        public void Reset()
        //        {
        //            enumerator.Reset();
        //        }
        //    }

        //    private static KeyValuePair<NullableKey<TKey>, TValue> ConvertExternalItem(KeyValuePair<TKey, TValue> item)
        //    {
        //        return new KeyValuePair<NullableKey<TKey>, TValue>(new NullableKey<TKey>(item.Key), item.Value);
        //    }

        //    private static KeyValuePair<TKey, TValue> ConvertInternalItem(KeyValuePair<NullableKey<TKey>, TValue> item)
        //    {
        //        return new KeyValuePair<TKey, TValue>(item.Key.Value, item.Value);
        //    }
        //}




        //    /// <summary>
        //    /// A C# emulation of the <a href="http://download.oracle.com/javase/1,5.0/docs/api/java/util/HashMap.html">Java Hashmap</a>
        //    /// <para>
        //    /// A <see cref="Dictionary{TKey, TValue}" /> is a close equivalent to the Java
        //    /// Hashmap.  One difference java implementation of the class is that
        //    /// the Hashmap supports both null keys and values, where the C# Dictionary
        //    /// only supports null values not keys.  Also, <c>V Get(TKey)</c>
        //    /// method in Java returns null if the key doesn't exist, instead of throwing
        //    /// an exception.  This implementation doesn't throw an exception when a key
        //    /// doesn't exist, it will return null.  This class is slower than using a
        //    /// <see cref="Dictionary{TKey, TValue}"/>, because of extra checks that have to be
        //    /// done on each access, to check for null.
        //    /// </para>
        //    /// <para>
        //    /// <b>NOTE:</b> This class works best with nullable types.  default(T) is returned
        //    /// when a key doesn't exist in the collection (this being similar to how Java returns
        //    /// null).  Therefore, if the expected behavior of the java code is to execute code
        //    /// based on if the key exists, when the key is an integer type, it will return 0 instead of null.
        //    /// </para>
        //    /// <remaks>
        //    /// Consider also implementing IDictionary, IEnumerable, and ICollection
        //    /// like <see cref="Dictionary{TKey, TValue}" /> does, so HashMap can be
        //    /// used in substituted in place for the same interfaces it implements.
        //    /// </remaks>
        //    /// </summary>
        //    /// <typeparam name="TKey">The type of keys in the dictionary</typeparam>
        //    /// <typeparam name="TValue">The type of values in the dictionary</typeparam>
        //    /// <remarks>
        //    /// <h2>Unordered Dictionaries</h2>
        //    /// <list type="bullet">
        //    ///     <item><description><see cref="Dictionary{TKey, TValue}"/> - use when order is not important and all keys are non-null.</description></item>
        //    ///     <item><description><see cref="HashMap{TKey, TValue}"/> - use when order is not important and support for a null key is required.</description></item>
        //    /// </list>
        //    /// <h2>Ordered Dictionaries</h2>
        //    /// <list type="bullet">
        //    ///     <item><description><see cref="LinkedHashMap{TKey, TValue}"/> - use when you need to preserve entry insertion order. Keys are nullable.</description></item>
        //    ///     <item><description><see cref="SortedDictionary{TKey, TValue}"/> - use when you need natural sort order. Keys must be unique.</description></item>
        //    ///     <item><description><see cref="TreeDictionary{K, V}"/> - use when you need natural sort order. Keys are nullable.</description></item>
        //    ///     <item><description><see cref="LurchTable{TKey, TValue}"/> - use when you need to sort by most recent access or most recent update. Works well for LRU caching.</description></item>
        //    /// </list>
        //    /// </remarks>
        //#if FEATURE_SERIALIZABLE
        //    [Serializable]
        //#endif
        //    public class HashMap<TKey, TValue> : IDictionary<TKey, TValue>
        //    {
        //        internal IEqualityComparer<TKey> _comparer;
        //        internal IDictionary<TKey, TValue> _dict;

        //        // Indicates if a null key has been assigned, used for iteration
        //        private bool _hasNullValue;

        //        // stores the value for the null key
        //        private TValue _nullValue;

        //        // Indicates the type of key is a non-nullable valuetype
        //        private readonly bool _isValueType;

        //        public HashMap()
        //            : this(0)
        //        { }

        //        public HashMap(IEqualityComparer<TKey> comparer)
        //            : this(0, comparer)
        //        {
        //        }

        //        public HashMap(int initialCapacity)
        //            : this(initialCapacity, EqualityComparer<TKey>.Default)
        //        {
        //        }

        //        public HashMap(int initialCapacity, IEqualityComparer<TKey> comparer)
        //            : this(new Dictionary<TKey, TValue>(initialCapacity, comparer), comparer)
        //        {
        //        }

        //        public HashMap(IEnumerable<KeyValuePair<TKey, TValue>> other)
        //            : this(0)
        //        {
        //            if (other == null)
        //                throw new ArgumentNullException(nameof(other));

        //            foreach (var kvp in other)
        //            {
        //                Add(kvp.Key, kvp.Value);
        //            }
        //        }

        //        internal HashMap(IDictionary<TKey, TValue> wrappedDict, IEqualityComparer<TKey> comparer)
        //        {
        //            if (wrappedDict == null)
        //                throw new ArgumentNullException(nameof(wrappedDict));
        //            if (comparer == null)
        //                throw new ArgumentNullException(nameof(comparer));

        //            _comparer = comparer;
        //            _dict = wrappedDict;
        //            _hasNullValue = false;

        //            if (typeof(TKey).GetTypeInfo().IsValueType)
        //            {
        //                _isValueType = Nullable.GetUnderlyingType(typeof(TKey)) == null;
        //            }
        //        }

        //        public bool ContainsValue(TValue value)
        //        {
        //            if (!_isValueType && _hasNullValue && _nullValue.Equals(value))
        //                return true;

        //            return _dict.Values.Contains(value);
        //        }

        //        public TValue AddIfAbsent(TKey key, TValue value)
        //        {
        //            if (!ContainsKey(key))
        //            {
        //                Add(key, value);
        //                return default(TValue);
        //            }
        //            return this[key];
        //        }

        //        #region Object overrides

        //        public override bool Equals(object obj)
        //        {
        //            if (obj == this)
        //                return true;

        //            if (!(obj is IDictionary<TKey, TValue>))
        //                return false;
        //            IDictionary<TKey, TValue> m = (IDictionary<TKey, TValue>)obj;
        //            if (m.Count != Count)
        //                return false;

        //            try
        //            {
        //                using (var i = GetEnumerator())
        //                {
        //                    while (i.MoveNext())
        //                    {
        //                        KeyValuePair<TKey, TValue> e = i.Current;
        //                        TKey key = e.Key;
        //                        TValue value = e.Value;
        //                        if (value == null)
        //                        {
        //                            if (!(m[key] == null && m.ContainsKey(key)))
        //                                return false;
        //                        }
        //                        else
        //                        {
        //                            if (!value.Equals(m[key]))
        //                                return false;
        //                        }
        //                    }
        //                }
        //            }
        //            catch (InvalidCastException)
        //            {
        //                return false;
        //            }
        //            catch (NullReferenceException)
        //            {
        //                return false;
        //            }

        //            return true;
        //        }

        //        public override int GetHashCode()
        //        {
        //            int h = 0;
        //            using (var i = GetEnumerator())
        //            {
        //                while (i.MoveNext())
        //                    h += i.Current.GetHashCode();
        //            }
        //            return h;
        //        }

        //        public override string ToString()
        //        {
        //            using (var i = GetEnumerator())
        //            {
        //                if (!i.MoveNext())
        //                    return "{}";

        //                StringBuilder sb = new StringBuilder();
        //                sb.Append('{');
        //                for (; ; )
        //                {
        //                    var e = i.Current;
        //                    TKey key = e.Key;
        //                    TValue value = e.Value;
        //                    sb.Append(key);
        //                    sb.Append('=');
        //                    sb.Append(value);
        //                    if (!i.MoveNext())
        //                        return sb.Append('}').ToString();
        //                    sb.Append(',').Append(' ');
        //                }
        //            }
        //        }

        //        #endregion

        //        #region Implementation of IEnumerable

        //        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        //        {
        //            if (!_isValueType && _hasNullValue)
        //            {
        //                yield return new KeyValuePair<TKey, TValue>(default(TKey), _nullValue);
        //            }
        //            foreach (var kvp in _dict)
        //            {
        //                yield return kvp;
        //            }
        //        }

        //        IEnumerator IEnumerable.GetEnumerator()
        //        {
        //            return GetEnumerator();
        //        }

        //        #endregion Implementation of IEnumerable

        //        #region Implementation of ICollection<KeyValuePair<TKey,TValue>>

        //        public virtual void Add(KeyValuePair<TKey, TValue> item)
        //        {
        //            Add(item.Key, item.Value);
        //        }

        //        public virtual void Clear()
        //        {
        //            _hasNullValue = false;
        //            _nullValue = default(TValue);
        //            _dict.Clear();
        //        }

        //        public virtual bool Contains(KeyValuePair<TKey, TValue> item)
        //        {
        //            if (!_isValueType && _comparer.Equals(item.Key, default(TKey)))
        //            {
        //                return _hasNullValue && EqualityComparer<TValue>.Default.Equals(item.Value, _nullValue);
        //            }

        //            return _dict.Contains(item);
        //        }

        //        public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        //        {
        //            _dict.CopyTo(array, arrayIndex);
        //            if (!_isValueType && _hasNullValue)
        //            {
        //                array[array.Length - 1] = new KeyValuePair<TKey, TValue>(default(TKey), _nullValue);
        //            }
        //        }

        //        public virtual bool Remove(KeyValuePair<TKey, TValue> item)
        //        {
        //            if (!_isValueType && _comparer.Equals(item.Key, default(TKey)))
        //            {
        //                if (!_hasNullValue)
        //                    return false;

        //                _hasNullValue = false;
        //                _nullValue = default(TValue);
        //                return true;
        //            }

        //            return _dict.Remove(item);
        //        }

        //        public virtual int Count
        //        {
        //            get { return _dict.Count + (_hasNullValue ? 1 : 0); }
        //        }

        //        public virtual bool IsReadOnly
        //        {
        //            get { return false; }
        //        }

        //        #endregion Implementation of ICollection<KeyValuePair<TKey,TValue>>

        //        #region Implementation of IDictionary<TKey,TValue>

        //        public virtual bool ContainsKey(TKey key)
        //        {
        //            if (!_isValueType && _comparer.Equals(key, default(TKey)))
        //            {
        //                if (_hasNullValue)
        //                {
        //                    return true;
        //                }
        //                return false;
        //            }

        //            return _dict.ContainsKey(key);
        //        }

        //        public virtual void Add(TKey key, TValue value)
        //        {
        //            if (!_isValueType && _comparer.Equals(key, default(TKey)))
        //            {
        //                _hasNullValue = true;
        //                _nullValue = value;
        //            }
        //            else
        //            {
        //                _dict[key] = value;
        //            }
        //        }

        //        public virtual bool Remove(TKey key)
        //        {
        //            if (!_isValueType && _comparer.Equals(key, default(TKey)))
        //            {
        //                _hasNullValue = false;
        //                _nullValue = default(TValue);
        //                return true;
        //            }
        //            else
        //            {
        //                return _dict.Remove(key);
        //            }
        //        }

        //        public virtual bool TryGetValue(TKey key, out TValue value)
        //        {
        //            if (!_isValueType && _comparer.Equals(key, default(TKey)))
        //            {
        //                if (_hasNullValue)
        //                {
        //                    value = _nullValue;
        //                    return true;
        //                }

        //                value = default(TValue);
        //                return false;
        //            }
        //            else
        //            {
        //                return _dict.TryGetValue(key, out value);
        //            }
        //        }

        //        public virtual TValue this[TKey key]
        //        {
        //            get
        //            {
        //                if (!_isValueType && _comparer.Equals(key, default(TKey)))
        //                {
        //                    if (!_hasNullValue)
        //                    {
        //                        return default(TValue);
        //                    }
        //                    return _nullValue;
        //                }
        //                return _dict.ContainsKey(key) ? _dict[key] : default(TValue);
        //            }
        //            set { Add(key, value); }
        //        }

        //        public virtual ICollection<TKey> Keys
        //        {
        //            get
        //            {
        //                if (!_hasNullValue) return _dict.Keys;

        //                // Using a List<T> to generate an ICollection<TKey>
        //                // would incur a costly copy of the dict's KeyCollection
        //                // use out own wrapper instead
        //                return new NullKeyCollection(_dict);
        //            }
        //        }

        //        public virtual ICollection<TValue> Values
        //        {
        //            get
        //            {
        //                if (!_hasNullValue) return _dict.Values;

        //                // Using a List<T> to generate an ICollection<TValue>
        //                // would incur a costly copy of the dict's ValueCollection
        //                // use out own wrapper instead
        //                return new NullValueCollection(_dict, _nullValue);
        //            }
        //        }

        //        #endregion Implementation of IDictionary<TKey,TValue>

        //        #region NullValueCollection

        //        /// <summary>
        //        /// Wraps a dictionary and adds the value
        //        /// represented by the null key
        //        /// </summary>
        //        private class NullValueCollection : ICollection<TValue>
        //        {
        //            private readonly TValue _nullValue;
        //            private readonly IDictionary<TKey, TValue> _internalDict;

        //            public NullValueCollection(IDictionary<TKey, TValue> dict, TValue nullValue)
        //            {
        //                _internalDict = dict;
        //                _nullValue = nullValue;
        //            }

        //            #region Implementation of IEnumerable

        //            public IEnumerator<TValue> GetEnumerator()
        //            {
        //                yield return _nullValue;

        //                foreach (var val in _internalDict.Values)
        //                {
        //                    yield return val;
        //                }
        //            }

        //            IEnumerator IEnumerable.GetEnumerator()
        //            {
        //                return GetEnumerator();
        //            }

        //            #endregion Implementation of IEnumerable

        //            #region Implementation of ICollection<TValue>

        //            public void CopyTo(TValue[] array, int arrayIndex)
        //            {
        //                throw new NotImplementedException("Implement as needed");
        //            }

        //            public int Count
        //            {
        //                get { return _internalDict.Count + 1; }
        //            }

        //            public bool IsReadOnly
        //            {
        //                get { return true; }
        //            }

        //            #region Explicit Interface Methods

        //            void ICollection<TValue>.Add(TValue item)
        //            {
        //                throw new NotSupportedException();
        //            }

        //            void ICollection<TValue>.Clear()
        //            {
        //                throw new NotSupportedException();
        //            }

        //            bool ICollection<TValue>.Contains(TValue item)
        //            {
        //                throw new NotSupportedException();
        //            }

        //            bool ICollection<TValue>.Remove(TValue item)
        //            {
        //                throw new NotSupportedException("Collection is read only!");
        //            }

        //            #endregion Explicit Interface Methods

        //            #endregion Implementation of ICollection<TValue>
        //        }

        //        #endregion NullValueCollection

        //        #region NullKeyCollection

        //        /// <summary>
        //        /// Wraps a dictionary's collection, adding in a
        //        /// null key.
        //        /// </summary>
        //        private class NullKeyCollection : ICollection<TKey>
        //        {
        //            private readonly IDictionary<TKey, TValue> _internalDict;

        //            public NullKeyCollection(IDictionary<TKey, TValue> dict)
        //            {
        //                _internalDict = dict;
        //            }

        //            public IEnumerator<TKey> GetEnumerator()
        //            {
        //                yield return default(TKey);
        //                foreach (var key in _internalDict.Keys)
        //                {
        //                    yield return key;
        //                }
        //            }

        //            IEnumerator IEnumerable.GetEnumerator()
        //            {
        //                return GetEnumerator();
        //            }

        //            public void CopyTo(TKey[] array, int arrayIndex)
        //            {
        //                throw new NotImplementedException("Implement this as needed");
        //            }

        //            public int Count
        //            {
        //                get { return _internalDict.Count + 1; }
        //            }

        //            public bool IsReadOnly
        //            {
        //                get { return true; }
        //            }

        //            #region Explicit Interface Definitions

        //            bool ICollection<TKey>.Contains(TKey item)
        //            {
        //                throw new NotSupportedException();
        //            }

        //            void ICollection<TKey>.Add(TKey item)
        //            {
        //                throw new NotSupportedException();
        //            }

        //            void ICollection<TKey>.Clear()
        //            {
        //                throw new NotSupportedException();
        //            }

        //            bool ICollection<TKey>.Remove(TKey item)
        //            {
        //                throw new NotSupportedException();
        //            }

        //            #endregion Explicit Interface Definitions
        //        }

        //        #endregion NullKeyCollection
        //    }
    }
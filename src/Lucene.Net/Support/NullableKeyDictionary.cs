using System;
using System.Collections;
using System.Collections.Generic;

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

    /// <summary>
    /// A dictionary that wraps a backing dictionary, enabling support for nullable keys.
    /// A <see cref="NullableKey{T}"/> struct is used as a key for the backing dictionary.
    /// <see cref="NullableKeyDictionary{TKey, TValue}"/> provides the conversions necessary
    /// to make that struct invisible to the consumer.
    /// <para/>
    /// This class overrides <see cref="Equals(object)"/> and <see cref="GetHashCode()"/>,
    /// providing support to be used as a dictionary key that compares all values and, if applicable,
    /// values of any nested <see cref="IDictionary{TKey, TValue}"/>, <see cref="IList{T}"/>, and
    /// <see cref="ISet{T}"/>. The default implementation for the equality rules for each collection type
    /// are exactly the same rules as the default rules for corresponding collection types in the OpenJDK.
    /// However, these equality rules may be overridden by subclasses.
    /// <para/>
    /// The default behavior for dictionary equality is to compare all values (including nulls), but ignore sort order.
    /// If <typeparamref name="TKey"/> and/or <typeparamref name="TValue"/> is defined as <see cref="IDictionary{TKey, TValue}"/>, 
    /// <see cref="IList{T}"/>, or <see cref="ISet{T}"/>, the values of those collections will also be compared for equality
    /// using the rules for the specific collection interface unless the class provides an override of
    /// <see cref="Equals(object)"/> and/or <see cref="GetHashCode()"/>.
    /// <para/>
    /// The <see cref="ToString()"/> method is also overridden, providing a delimited list of each of the
    /// values in the collection and any nested <see cref="IDictionary{TKey, TValue}"/>, 
    /// <see cref="IList{T}"/>, or <see cref="ISet{T}"/> types. The default format used for building the string is
    /// identical to the default format used in the OpenJDK.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary. This can be either a value type or a reference type.
    /// For the nullable feature to function, a value type should be specified as nullable.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    public class NullableKeyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        private KeyCollection keys;
        private readonly IDictionary<NullableKey<TKey>, TValue> dictionary;
        private readonly IEqualityComparer<TKey> comparer;

        /// <summary>
        /// Creates an instance of <see cref="NullableKeyDictionary{TKey, TValue}"/> using the provided
        /// <paramref name="backingDictionary"/> instance and the default equality comparer for the key type.
        /// The <paramref name="backingDictionary"/> will be wrapped to enable nullable key support.
        /// <para/>
        /// IMPORTANT: Do not pass a custom comparer to the constructor of <paramref name="backingDictionary"/>.
        /// Instead, pass the custom comparer to the
        /// <see cref="NullableKeyDictionary{TKey, TValue}.NullableKeyDictionary(IDictionary{NullableKey{TKey}, TValue}, IEqualityComparer{TKey})"/>
        /// constructor.
        /// </summary>
        /// <param name="backingDictionary">A dictionary implementation that provides the desired behavior which 
        /// will be wrapped to make it support null keys.</param>
        public NullableKeyDictionary(IDictionary<NullableKey<TKey>, TValue> backingDictionary)
            : this(backingDictionary, EqualityComparer<TKey>.Default)
        { }

        /// <summary>
        /// Creates an instance of <see cref="NullableKeyDictionary{TKey, TValue}"/> using the provided
        /// <paramref name="backingDictionary"/> instance and the specified <see cref="IEqualityComparer{TKey}"/>.
        /// The <paramref name="backingDictionary"/> will be wrapped to enable nullable key support.
        /// <para/>
        /// IMPORTANT: Do not pass a custom comparer to the constructor of <paramref name="backingDictionary"/>.
        /// Instead, pass the custom comparer as the <paramref name="comparer"/> argument here.
        /// </summary>
        /// <param name="backingDictionary">A dictionary implementation that provides the desired behavior which 
        /// will be wrapped to make it support null keys.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer"/> implementation to use when comparing keys,
        /// or <c>null</c> to use the default <see cref="EqualityComparer{T}"/> for the type of the key.</param>
        public NullableKeyDictionary(IDictionary<NullableKey<TKey>, TValue> backingDictionary, IEqualityComparer<TKey> comparer)
        {
            this.dictionary = backingDictionary ?? throw new ArgumentNullException(nameof(backingDictionary));
            this.comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        /// <summary>
        /// Gets the <see cref="IEqualityComparer{T}"/> that is used to determine equality of keys for the dictionary.
        /// </summary>
        public IEqualityComparer<TKey> Comparer => comparer;

        #region IDictionary<TKey, TValue> Members

        /// <summary>
        /// Gets the number of key/value pairs contained in the dictionary.
        /// </summary>
        public virtual int Count => dictionary.Count;

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// The key type may be <c>null</c>.
        /// </summary>
        /// <param name="key">The key of the value to get or set. May be <c>null</c>.</param>
        /// <value>The value associated with the specified key. If the specified key is not found, a get
        /// operation throws a <see cref="KeyNotFoundException"/>, and a set operation creates a new element 
        /// with the specified key.</value>
        /// <exception cref="KeyNotFoundException">The property is retrieved and <paramref name="key"/>
        /// does not exist in the collection.</exception>
        public virtual TValue this[TKey key]
        {
            get => dictionary[ConvertExternalKey(key)];
            set => dictionary[ConvertExternalKey(key)] = value;
        }

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        public virtual ICollection<TKey> Keys
        {
            get
            {
                if (keys == null) keys = new KeyCollection(this, dictionary.Keys);
                return keys;
            }
        }

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
        public virtual ICollection<TValue> Values => dictionary.Values;

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to add. The key can be <c>null</c> for either reference types or nullable value types.</param>
        /// <param name="value">The value of the element to add. The value can be <c>null</c> for either reference types or nullable value types.</param>
        public virtual void Add(TKey key, TValue value) => dictionary.Add(ConvertExternalKey(key), value);

        /// <summary>
        /// Removes all keys and values from the dictionary.
        /// </summary>
        public virtual void Clear() => dictionary.Clear();

        /// <summary>
        /// Determines whether the dictionary contains the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key to locate in the dictionary.</param>
        /// <returns><c>true</c> if the dictionary 
        /// contains an element with the specified key; otherwise, <c>false</c>.
        /// The key can be <c>null</c> for reference types or nullable value types.</returns>
        public virtual bool ContainsKey(TKey key) => dictionary.ContainsKey(ConvertExternalKey(key));

        /// <summary>
        /// Determines whether the dictionary contains a specific <paramref name="value"/>.
        /// </summary>
        /// <remarks>
        /// This method determines equality using the default equality comparer <see cref="EqualityComparer{T}.Default"/> 
        /// for <typeparamref name="TValue"/>, the type of values in the dictionary.
        /// <para/>
        /// This method performs a linear search; therefore, the average execution
        /// time is proportional to <see cref="Count"/>. That is, this method is an O(<c>n</c>) operation,
        /// where <c>n</c> is <see cref="Count"/>.
        /// </remarks>
        /// <param name="value">The value to locate in the dictionary. The value can be <c>null</c>
        /// for reference types or nullable value types.</param>
        /// <returns><c>true</c> if the dictionary contains an element with the specified value;
        /// otherwise, <c>false</c>.</returns>
        public virtual bool ContainsValue(TValue value) => dictionary.Values.Contains(value);

        /// <summary>
        /// Returns an enumerator that iterates through the dictionary.
        /// </summary>
        /// <returns>An enumerator for the dictionary.</returns>
        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new Enumerator(dictionary.GetEnumerator());
        }

        /// <summary>
        /// Removes the value with the specified <paramref name="key"/> from the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns><c>true</c> if the element is successfully found and removed; otherwise, <c>false</c>.
        /// This method returns <c>false</c> if key is not found in the dictionary.</returns>
        public virtual bool Remove(TKey key) => dictionary.Remove(ConvertExternalKey(key));

        /// <summary>
        /// Gets the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <remarks>
        /// This method combines the functionality of the <see cref="ContainsKey(TKey)"/> method and the <see cref="this[TKey]"/> property. 
        /// <para/>
        /// If the key is not found, then the value parameter gets the appropriate default value for the type 
        /// <typeparamref name="TValue"/>; for example, 0 (zero) for integer types, <c>false</c> for Boolean types, 
        /// and <c>null</c> for reference types.
        /// <para/>
        /// Use the <see cref="TryGetValue(TKey, out TValue)"/> method if your code frequently attempts to
        /// access keys that are not in the dictionary. Using this method is more efficient than catching 
        /// the <see cref="KeyNotFoundException"/> thrown by the <see cref="this[TKey]"/> property.
        /// <para/>
        /// This method approaches an O(1) operation.
        /// </remarks>
        /// <param name="key">The key of the value to get. The key can be <c>null</c>
        /// for reference types or nullable value types.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified <paramref name="key"/>,
        /// if the key is found; otherwise, the default value for the type of the value parameter.
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the dictionary contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public virtual bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(ConvertExternalKey(key), out value);
        }

        #endregion

        #region ICollection<KeyValuePair<TKey, TValue>> Members

        /// <summary>
        /// Gets a value that indicates whether the dictionary is read-only.
        /// </summary>
        public virtual bool IsReadOnly => dictionary.IsReadOnly;

        /// <summary>
        /// Adds the specified <see cref="KeyValuePair{TKey, TValue}"/> to the collection with the specified key.
        /// </summary>
        /// <param name="keyValuePair">The <see cref="KeyValuePair{TKey, TValue}"/> structure representing the key and value to add to the dictionary.</param>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair) => Collection_KeyValuePair_Add(keyValuePair);

        /// <summary>
        /// Adds the specified <see cref="KeyValuePair{TKey, TValue}"/> to the collection with the specified key.
        /// <para/>
        /// This method may be overridden to change the implementation of <see cref="ICollection{T}.Add(T)"/> for this dictionary.
        /// </summary>
        /// <param name="keyValuePair">The <see cref="KeyValuePair{TKey, TValue}"/> structure representing the key and value to add to the dictionary.</param>
        protected virtual void Collection_KeyValuePair_Add(KeyValuePair<TKey, TValue> keyValuePair)
        {
            dictionary.Add(ConvertExternalItem(keyValuePair));
        }

        /// <summary>
        /// Determines whether the <see cref="ICollection{T}"/> contains a specific key and value.
        /// </summary>
        /// <param name="keyValuePair">The <see cref="KeyValuePair{TKey, TValue}"/> structure to locate in the <see cref="ICollection{T}"/>.</param>
        /// <returns><c>true</c> if <paramref name="keyValuePair"/> is found in the <see cref="ICollection{T}"/>; otherwise, <c>false</c>.</returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair) => Collection_KeyValuePair_Contains(keyValuePair);

        /// <summary>
        /// Determines whether the <see cref="ICollection{T}"/> contains a specific key and value.
        /// <para/>
        /// This method may be overridden to change the implementation of <see cref="ICollection{T}.Contains(T)"/> for this dictionary.
        /// </summary>
        /// <param name="keyValuePair">The <see cref="KeyValuePair{TKey, TValue}"/> structure to locate in the <see cref="ICollection{T}"/>.</param>
        /// <returns><c>true</c> if <paramref name="keyValuePair"/> is found in the <see cref="ICollection{T}"/>; otherwise, <c>false</c>.</returns>
        protected virtual bool Collection_KeyValuePair_Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            return dictionary.Contains(ConvertExternalItem(keyValuePair));
        }

        /// <summary>
        /// Copies the elements of the <see cref="ICollection{T}"/> to an array, starting at the specified array <paramref name="index"/>.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from <see cref="ICollection{T}"/>.
        /// The array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index) => Collection_KeyValuePair_CopyTo(array, index);

        /// <summary>
        /// Copies the elements of the <see cref="ICollection{T}"/> to an array, starting at the specified array <paramref name="index"/>.
        /// <para/>
        /// This method may be overridden to change the implementation of <see cref="ICollection{T}.CopyTo(T[], int)"/> for this dictionary.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from <see cref="ICollection{T}"/>.
        /// The array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        protected virtual void Collection_KeyValuePair_CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            foreach (var item in this)
                array[index++] = item;
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="ICollection{T}"/>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="ICollection{T}"/>.</param>
        /// <returns><c>true</c> if item was successfully removed from the <see cref="ICollection{T}"/>; otherwise, <c>false</c>.
        /// This method also returns <c>false</c> if item is not found in the original <see cref="ICollection{T}"/>.</returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => Collection_KeyValuePair_Remove(item);

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="ICollection{T}"/>.
        /// <para/>
        /// This method may be overridden to change the implementation of <see cref="ICollection{T}.Remove(T)"/> for this dictionary.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="ICollection{T}"/>.</param>
        /// <returns><c>true</c> if item was successfully removed from the <see cref="ICollection{T}"/>; otherwise, <c>false</c>.
        /// This method also returns <c>false</c> if item is not found in the original <see cref="ICollection{T}"/>.</returns>
        protected virtual bool Collection_KeyValuePair_Remove(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Remove(ConvertExternalItem(item));
        }

        #endregion

        #region IReadOnlyDictionary<TKey, TValue>

        /// <summary>
        /// Gets a collection containing the keys of the <see cref="IReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        /// <summary>
        /// Gets a collection containing the values of the <see cref="IReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        /// <summary>
        /// Gets the value associated with the specified key.
        /// The key type may be <c>null</c>.
        /// </summary>
        /// <param name="key">The key of the value to get or set. May be <c>null</c>.</param>
        /// <value>The value associated with the specified key. If the specified key is not found, a get
        /// operation throws a <see cref="KeyNotFoundException"/>, and a set operation creates a new element 
        /// with the specified key.</value>
        /// <exception cref="KeyNotFoundException">The property is retrieved and <paramref name="key"/>
        /// does not exist in the collection.</exception>
        TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key] => this[key];

        /// <summary>
        /// Determines whether the read-only dictionary contains an element that has the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns><c>true</c> if the read-only dictionary contains an element that has the specified key; otherwise, <c>false</c>.</returns>
        bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key) => ReadOnlyDictionary_ContainsKey(key);

        /// <summary>
        /// Determines whether the read-only dictionary contains an element that has the specified <paramref name="key"/>.
        /// <para/>
        /// This method may be overridden to change the implementation of <see cref="IReadOnlyDictionary{TKey, TValue}.ContainsKey(TKey)"/>
        /// for this dictionary.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns><c>true</c> if the read-only dictionary contains an element that has the specified key; otherwise, <c>false</c>.</returns>
        protected virtual bool ReadOnlyDictionary_ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(ConvertExternalKey(key));
        }

        /// <summary>
        /// Gets the value that is associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <param name="value">When this method returns, the value associated with the specified <paramref name="key"/>,
        /// if the key is found; otherwise, the default value for the type of the value parameter.
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if this dictionary contains an element that has the specified key; otherwise, <c>false</c>.</returns>
        bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value) => ReadOnlyDictionary_TryGetValue(key, out value);

        /// <summary>
        /// Gets the value that is associated with the specified <paramref name="key"/>.
        /// <para/>
        /// This method may be overridden to change the implementation of <see cref="IReadOnlyDictionary{TKey, TValue}.TryGetValue(TKey, out TValue)"/>
        /// for this dictionary.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <param name="value">When this method returns, the value associated with the specified <paramref name="key"/>,
        /// if the key is found; otherwise, the default value for the type of the value parameter.
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if this dictionary contains an element that has the specified key; otherwise, <c>false</c>.</returns>
        protected virtual bool ReadOnlyDictionary_TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(ConvertExternalKey(key), out value);
        }

        #endregion

        #region IReadOnlyCollection<KeyValuePair<TKey, TValue>> Members

        /// <summary>
        /// Gets the number of key/value pairs contained in the <see cref="IReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        int IReadOnlyCollection<KeyValuePair<TKey, TValue>>.Count => Count;

        #endregion

        #region IEnumerator Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => Enumerable_GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// <para/>
        /// This method may be overridden to change the implementation of <see cref="IEnumerable.GetEnumerator()"/> for this dictionary.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> that can be used to iterate through the collection.</returns>
        protected virtual IEnumerator Enumerable_GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region KeyValuePair Conversion

        /// <summary>
        /// Converts a <typeparamref name="TKey"/> to a <see cref="NullableKey{TKey}"/>
        /// with the current key <see cref="Comparer"/>.
        /// </summary>
        /// <param name="key">A <typeparamref name="TKey"/>.</param>
        /// <returns>The converted <see cref="NullableKey{TKey}"/> with the current key <see cref="Comparer"/>.</returns>
        protected NullableKey<TKey> ConvertExternalKey(TKey key)
        {
            return new NullableKey<TKey>(key, this.comparer);
        }

        /// <summary>
        /// Converts a <see cref="KeyValuePair{TKey, TValue}"/> to a <c>KeyValuePair&lt;NullableKey&lt;TKey&gt;, TValue&gt;</c>.
        /// </summary>
        /// <param name="item">A <see cref="KeyValuePair{TKey, TValue}"/>.</param>
        /// <returns>The converted <c>KeyValuePair&lt;NullableKey&lt;TKey&gt;, TValue&gt;</c>.</returns>
        protected KeyValuePair<NullableKey<TKey>, TValue> ConvertExternalItem(KeyValuePair<TKey, TValue> item)
        {
            return new KeyValuePair<NullableKey<TKey>, TValue>(ConvertExternalKey(item.Key), item.Value);
        }

        /// <summary>
        /// Converts a <c>KeyValuePair&lt;NullableKey&lt;TKey&gt;, TValue&gt;</c> to a <see cref="KeyValuePair{TKey, TValue}"/>.
        /// </summary>
        /// <param name="item">A <c>KeyValuePair&lt;NullableKey&lt;TKey&gt;, TValue&gt;</c>.</param>
        /// <returns>The converted <see cref="KeyValuePair{TKey, TValue}"/>.</returns>
        protected static KeyValuePair<TKey, TValue> ConvertInternalItem(KeyValuePair<NullableKey<TKey>, TValue> item)
        {
            return new KeyValuePair<TKey, TValue>(item.Key.Value, item.Value);
        }

        #endregion

        #region System.Object Overrides

        /// <summary>
        /// Compares the specified object with this dictionary for equality. Returns <c>true</c> if the
        /// given object is also a map and the two maps represent the same mappings. More formally,
        /// two dictionaries <c>m1</c> and <c>m2</c> represent the same mappings if the values of <paramref name="obj"/>
        /// match the values of this dictionary (without regard to order, but with regard to any nested collections).
        /// </summary>
        /// <param name="obj">Object to be compared for equality with this dictionary.</param>
        /// <returns><c>true</c> if the specified object's values are equal to this dictionary.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is IDictionary<TKey, TValue>))
                return false;

            return Collections.Equals(this, obj as IDictionary<TKey, TValue>);
        }

        /// <summary>
        /// Returns the hash code value for this dictionary. The hash code of a dictionary is defined to be
        /// the sum of the hash codes of each entry in the dictionary. This ensures that <c>m1.Equals(m2)</c>
        /// implies that <c>m1.GetHashCode() == m2.GetHashCode()</c> for any two dictionaries <c>m1</c> and <c>m2</c>.
        /// </summary>
        /// <returns>The hash code value for this dictionary.</returns>
        public override int GetHashCode()
        {
            return Collections.GetHashCode(this);
        }

        /// <summary>
        /// Returns a string representation of this dictionary. The string representation consists
        /// of a list of key-value mappings in the order returned by the dictionary's iterator, enclosed
        /// in braces ("{}"). Adjacent mappings are separated by the characters ", " (comma and space).
        /// Each key-value mapping is rendered as the key followed by an equals sign ("=") followed by the associated value.
        /// </summary>
        /// <returns>A string representation of this dictionary.</returns>
        public override string ToString()
        {
            return Collections.ToString(this);
        }

        #endregion

        #region Nested Class: KeyCollection

        /// <summary>
        /// Represents the collection of keys in a <see cref="NullableKeyDictionary{TKey, TValue}"/>. This class cannot be inherited.
        /// </summary>
        private sealed class KeyCollection : ICollection<TKey>
        {
            private readonly NullableKeyDictionary<TKey, TValue> nullableKeyDictionary;
            private readonly ICollection<NullableKey<TKey>> collection;
            public KeyCollection(NullableKeyDictionary<TKey, TValue> nullableKeyDictionary, ICollection<NullableKey<TKey>> collection)
            {
                this.nullableKeyDictionary = nullableKeyDictionary ?? throw new ArgumentNullException(nameof(nullableKeyDictionary));
                this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
            }

            /// <summary>
            ///  Gets the number of elements contained in the <see cref="KeyCollection"/>.
            /// </summary>
            public int Count => collection.Count;

            public bool IsReadOnly => collection.IsReadOnly;

            public void Add(TKey item) => collection.Add(nullableKeyDictionary.ConvertExternalKey(item));

            public void Clear() => collection.Clear();

            public bool Contains(TKey item) => collection.Contains(nullableKeyDictionary.ConvertExternalKey(item));

            public void CopyTo(TKey[] array, int index)
            {
                foreach (var item in collection)
                    array[index++] = item.Value;
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                foreach (var item in collection)
                    yield return item.Value;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public bool Remove(TKey item) => collection.Remove(nullableKeyDictionary.ConvertExternalKey(item));
        }

        #endregion

        #region Nested Class: Enumerator

        /// <summary>
        /// Enumerates the elemensts of a <see cref="NullableKeyDictionary{TKey, TValue}"/>.
        /// </summary>
        private class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly IEnumerator<KeyValuePair<NullableKey<TKey>, TValue>> enumerator;

            public Enumerator(IEnumerator<KeyValuePair<NullableKey<TKey>, TValue>> enumerator)
            {
                this.enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
            }

            public KeyValuePair<TKey, TValue> Current => ConvertInternalItem(enumerator.Current);

            object IEnumerator.Current => Current;

            public void Dispose() => enumerator.Dispose();

            public bool MoveNext() => enumerator.MoveNext();

            public void Reset() => enumerator.Reset();
        }

        #endregion
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SCG = System.Collections.Generic;

namespace J2N.Collections.Generic
{
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class SortedDictionary<TKey, TValue> : IConcreteSortedDictionary<TKey, TValue>
    {
        private static readonly bool TKeyIsNullable = typeof(TKey).IsNullableType();

        private readonly IConcreteSortedDictionary<TKey, TValue> dictionary;

        public SortedDictionary() : this((IComparer<TKey>)null) { }

        public SortedDictionary(IDictionary<TKey, TValue> dictionary)
            : this(dictionary, null)
        { }

        public SortedDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            comparer = comparer ?? Comparer<TKey>.Default;

            if (TKeyIsNullable)
                this.dictionary = new NullableKeySortedDictionary(dictionary, comparer);
            else
                this.dictionary = new ConcreteSortedDictionary(dictionary, comparer);
        }

        public SortedDictionary(IComparer<TKey> comparer)
        {
            comparer = comparer ?? Comparer<TKey>.Default;

            if (TKeyIsNullable)
                this.dictionary = new NullableKeySortedDictionary(comparer);
            else
                this.dictionary = new ConcreteSortedDictionary(comparer);
        }

        public IComparer<TKey> Comparer => dictionary.Comparer;

        public ICollection<TKey> Keys => dictionary.Keys;

        public ICollection<TValue> Values => dictionary.Values;

        public int Count => dictionary.Count;

        public bool IsReadOnly => dictionary.IsReadOnly;

        public TValue this[TKey key]
        {
            get => dictionary[key];
            set => dictionary[key] = value;
        }

        public bool ContainsKey(TKey key)
            => dictionary.ContainsKey(key);

        public void Add(TKey key, TValue value)
            => dictionary.Add(key, value);

        public bool Remove(TKey key)
            => dictionary.Remove(key);

        public bool TryGetValue(TKey key, out TValue value)
            => dictionary.TryGetValue(key, out value);

        public void Add(KeyValuePair<TKey, TValue> item)
            => dictionary.Add(item);

        public void Clear()
            => dictionary.Clear();

        public bool Contains(KeyValuePair<TKey, TValue> item)
            => dictionary.Contains(item);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            => dictionary.CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<TKey, TValue> item)
            => dictionary.Remove(item);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            => dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();


        #region Nested Class: NullableKeySortedDictionary<TKey, TValue>

        /// <summary>
        /// A <see cref="IConcreteSortedDictionary{TKey, TValue}"/> implementation that supports null keys.
        /// </summary>
#if !NETSTANDARD
        [Serializable]
#endif
        internal class NullableKeySortedDictionary : IConcreteSortedDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>
        {
#if !NETSTANDARD
            [NonSerialized]
#endif
            private KeyCollection keys;
            private readonly SCG.SortedDictionary<NullableKey<TKey>, TValue> dictionary;
            private readonly IComparer<TKey> comparer;

            public NullableKeySortedDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer)
            {
                if (dictionary == null)
                    throw new ArgumentNullException(nameof(dictionary));

                this.comparer = comparer ?? Comparer<TKey>.Default;
                this.dictionary = new SCG.SortedDictionary<NullableKey<TKey>, TValue>(new NullableKeyComparer(comparer)); // TODO: J2N Comparer (similar to NaturalComparer<T>)
                if (dictionary != null)
                {
                    foreach (var pair in dictionary)
                        Add(pair);
                }
            }

            public NullableKeySortedDictionary(IComparer<TKey> comparer)
            {
                this.comparer = comparer ?? Comparer<TKey>.Default;
                this.dictionary = new SCG.SortedDictionary<NullableKey<TKey>, TValue>(new NullableKeyComparer(comparer)); // TODO: J2N Comparer (similar to NaturalComparer<T>)
            }

            public IComparer<TKey> Comparer => comparer;

            public ICollection<TKey> Keys
            {
                get
                {
                    if (keys == null) keys = new KeyCollection(this, dictionary.Keys);
                    return keys;
                }
            }

            public ICollection<TValue> Values => dictionary.Values;

            public int Count => dictionary.Count;

            public bool IsReadOnly => ((ICollection<KeyValuePair<NullableKey<TKey>, TValue>>)dictionary).IsReadOnly;

            public bool IsFixedSize => ((IDictionary)dictionary).IsFixedSize;

            ICollection IDictionary.Keys => (ICollection)Keys;

            ICollection IDictionary.Values => dictionary.Values;

            public bool IsSynchronized => ((ICollection)dictionary).IsSynchronized;

            public object SyncRoot => ((ICollection)dictionary).SyncRoot;

            IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

            IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

            TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key] => dictionary[ConvertExternalKey(key)];

            object IDictionary.this[object key]
            {
                get
                {
                    if (key is TKey)
                        return this[(TKey)key];
                    return null;
                }
                set
                {
                    try
                    {
                        TKey tempKey = (TKey)key;

                        try
                        {
                            this[tempKey] = (TValue)value;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException($"The value '{value}' is not of type '{typeof(TValue)}' and cannot be used in this generic collection. Parameter name: {nameof(value)}");
                        }
                    }
                    catch (InvalidCastException)
                    {
                        throw new ArgumentException($"The value '{key}' is not of type '{typeof(TKey)}' and cannot be used in this generic collection. Parameter name: {nameof(key)}");
                    }
                }
            }

            public TValue this[TKey key]
            {
                get => dictionary[ConvertExternalKey(key)];
                set => dictionary[ConvertExternalKey(key)] = value;
            }

            public void Add(TKey key, TValue value)
                => dictionary.Add(ConvertExternalKey(key), value);

            public bool ContainsKey(TKey key)
                => dictionary.ContainsKey(ConvertExternalKey(key));

            public bool Remove(TKey key)
                => dictionary.Remove(ConvertExternalKey(key));

            public bool TryGetValue(TKey key, out TValue value)
                => dictionary.TryGetValue(ConvertExternalKey(key), out value);

            public void Add(KeyValuePair<TKey, TValue> item)
                => ((ICollection<KeyValuePair<NullableKey<TKey>, TValue>>)dictionary).Add(ConvertExternalItem(item));

            public void Clear()
                => dictionary.Clear();

            public bool Contains(KeyValuePair<TKey, TValue> item)
                => ((ICollection<KeyValuePair<NullableKey<TKey>, TValue>>)dictionary).Contains(ConvertExternalItem(item));

            public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            {
                foreach (var item in this)
                    array[arrayIndex++] = item;
            }

            public bool Remove(KeyValuePair<TKey, TValue> item)
                => ((ICollection<KeyValuePair<NullableKey<TKey>, TValue>>)dictionary).Remove(ConvertExternalItem(item));

            public void Add(object key, object value)
            {
                try
                {
                    TKey tempKey = (TKey)key;

                    try
                    {
                        Add(tempKey, (TValue)value);
                    }
                    catch (InvalidCastException)
                    {
                        throw new ArgumentException($"The value '{value}' is not of type '{typeof(TValue)}' and cannot be used in this generic collection. Parameter name: {nameof(value)}");
                    }
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException($"The value '{key}' is not of type '{typeof(TKey)}' and cannot be used in this generic collection. Parameter name: {nameof(key)}");
                }
            }

            public bool Contains(object key)
            {
                if (key is TKey)
                    return ContainsKey((TKey)key);
                return false;
            }

            IDictionaryEnumerator IDictionary.GetEnumerator()
                => new Enumerator(dictionary, Enumerator.DictEntry);

            public void Remove(object key)
            {
                if (key is TKey)
                    Remove((TKey)key);
            }

            public void CopyTo(Array array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (array.Rank != 1)
                    throw new ArgumentException("Only single dimensional arrays are supported for the requested action.");
                if (array.GetLowerBound(0) != 0)
                    throw new ArgumentException("The lower bound of target array must be zero.");
                if (index < 0 || index > array.Length)
                    throw new ArgumentOutOfRangeException($"Non-negative number required. Parameter name: {nameof(index)}");
                if (array.Length - index < Count)
                    throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");

                if (array is KeyValuePair<TKey, TValue>[] pairs)
                {
                    CopyTo(pairs, index);
                }
                else if (array is DictionaryEntry[] dictEntryArray)
                {
                    foreach (var entry in this)
                        dictEntryArray[index++] = new DictionaryEntry(entry.Key, entry.Value);
                }
                else
                {
                    if (!(array is object[] objects))
                    {
                        throw new ArgumentException("Invalid array type.");
                    }

                    try
                    {
                        foreach (var entry in this)
                            objects[index++] = ConvertExternalItem(entry);
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw new ArgumentException("Invalid array type.");
                    }
                }
            }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
                => new Enumerator(dictionary, Enumerator.KeyValuePair);

            IEnumerator IEnumerable.GetEnumerator()
                => new Enumerator(dictionary, Enumerator.KeyValuePair);



            #region Nullable Conversion

            /// <summary>
            /// Converts a <typeparamref name="TKey"/> to a <see cref="NullableKey{TKey}"/>
            /// with J2N's default equality comparer.
            /// </summary>
            /// <param name="key">A <typeparamref name="TKey"/>.</param>
            /// <returns>The converted <see cref="NullableKey{TKey}"/> with the default key comparer.</returns>
            private NullableKey<TKey> ConvertExternalKey(TKey key)
            {
                return new NullableKey<TKey>(key, EqualityComparer<TKey>.Default);
            }

            /// <summary>
            /// Converts a <see cref="KeyValuePair{TKey, TValue}"/> to a <c>KeyValuePair&lt;NullableKey, TValue&gt;</c>.
            /// </summary>
            /// <param name="item">A <see cref="KeyValuePair{TKey, TValue}"/>.</param>
            /// <returns>The converted <c>KeyValuePair&lt;NullableKey, TValue&gt;</c>.</returns>
            private KeyValuePair<NullableKey<TKey>, TValue> ConvertExternalItem(KeyValuePair<TKey, TValue> item)
            {
                return new KeyValuePair<NullableKey<TKey>, TValue>(ConvertExternalKey(item.Key), item.Value);
            }

            /// <summary>
            /// Converts a <c>KeyValuePair&lt;NullableKey&lt;TKey&gt;, TValue&gt;</c> to a <see cref="KeyValuePair{TKey, TValue}"/>.
            /// </summary>
            /// <param name="item">A <c>KeyValuePair&lt;NullableKey&lt;TKey&gt;, TValue&gt;</c>.</param>
            /// <returns>The converted <see cref="KeyValuePair{TKey, TValue}"/>.</returns>
            private static KeyValuePair<TKey, TValue> ConvertInternalItem(KeyValuePair<NullableKey<TKey>, TValue> item)
            {
                return new KeyValuePair<TKey, TValue>(item.Key.Value, item.Value);
            }

            #endregion Nullable Conversion

            #region Nested Class: KeyCollection

            /// <summary>
            /// Represents the collection of keys in a <see cref="NullableKeyDictionary{TKey, TValue}"/>. This class cannot be inherited.
            /// </summary>
#if FEATURE_SERIALIZABLE
            [Serializable]
#endif
            private sealed class KeyCollection : ICollection<TKey>, ICollection
            {
                private readonly NullableKeySortedDictionary nullableKeyDictionary;
                private readonly ICollection<NullableKey<TKey>> collection;
                public KeyCollection(NullableKeySortedDictionary nullableKeyDictionary, ICollection<NullableKey<TKey>> collection)
                {
                    this.nullableKeyDictionary = nullableKeyDictionary ?? throw new ArgumentNullException(nameof(nullableKeyDictionary));
                    this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
                }

                /// <summary>
                ///  Gets the number of elements contained in the <see cref="KeyCollection"/>.
                /// </summary>
                public int Count => collection.Count;

                public bool IsReadOnly => collection.IsReadOnly;

                public bool IsSynchronized => nullableKeyDictionary.IsSynchronized;

                public object SyncRoot => nullableKeyDictionary.SyncRoot;

                public void Add(TKey item) => collection.Add(nullableKeyDictionary.ConvertExternalKey(item));

                public void Clear() => collection.Clear();

                public bool Contains(TKey item) => collection.Contains(nullableKeyDictionary.ConvertExternalKey(item));

                public void CopyTo(TKey[] array, int index)
                {
                    if (array == null)
                        throw new ArgumentNullException(nameof(array));
                    if (index < 0 || index > array.Length)
                        throw new ArgumentOutOfRangeException($"Non-negative number required. Parameter name: {nameof(index)}");
                    if (array.Length - index < Count)
                        throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");

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

                public void CopyTo(Array array, int index)
                {
                    if (array == null)
                        throw new ArgumentNullException(nameof(array));
                    if (array.Rank != 1)
                        throw new ArgumentException("Only single dimensional arrays are supported for the requested action.");
                    if (array.GetLowerBound(0) != 0)
                        throw new ArgumentException("The lower bound of target array must be zero.");
                    if (index < 0 || index > array.Length)
                        throw new ArgumentOutOfRangeException($"Non-negative number required. Parameter name: {nameof(index)}");
                    if (array.Length - index < Count)
                        throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");

                    if (array is TKey[] keys)
                    {
                        CopyTo(keys, index);
                    }
                    else
                    {
                        if (!(array is object[] objects))
                        {
                            throw new ArgumentException("Invalid array type.");
                        }
                        try
                        {
                            foreach (var item in collection)
                                objects[index++] = item.Value;
                        }
                        catch (ArrayTypeMismatchException)
                        {
                            throw new ArgumentException("Invalid array type.");
                        }
                    }
                }
            }

            #endregion

            #region Nested Class: Enumerator

            /// <summary>
            /// Enumerates the elemensts of a <see cref="NullableKeyDictionary{TKey, TValue}"/>.
            /// </summary>
            private class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
            {
                private readonly IDictionary<NullableKey<TKey>, TValue> dictionary;
                private readonly IEnumerator<KeyValuePair<NullableKey<TKey>, TValue>> enumerator;
                private int index;
                private KeyValuePair<TKey, TValue> current;
                private readonly int getEnumeratorRetType;  // What should Enumerator.Current return?

                internal const int DictEntry = 1;
                internal const int KeyValuePair = 2;

                public Enumerator(IDictionary<NullableKey<TKey>, TValue> dictionary, int getEnumeratorRetType)
                {
                    this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
                    this.enumerator = dictionary.GetEnumerator();
                    this.getEnumeratorRetType = getEnumeratorRetType;
                    index = 0;
                }

                public KeyValuePair<TKey, TValue> Current => current;

                object IEnumerator.Current
                {
                    get
                    {
                        if (index == 0 || (index == dictionary.Count + 1))
                            throw new InvalidOperationException("Enumeration has either not started or has already finished.");

                        if (getEnumeratorRetType == DictEntry)
                            return new DictionaryEntry(current.Key, current.Value);
                        else
                            return new KeyValuePair<TKey, TValue>(current.Key, current.Value);
                    }
                }

                public void Dispose() => enumerator.Dispose();

                public bool MoveNext()
                {
                    if (enumerator.MoveNext())
                    {
                        index++;
                        current = ConvertInternalItem(enumerator.Current);
                        return true;
                    }
                    index = dictionary.Count + 1;
                    current = new KeyValuePair<TKey, TValue>();
                    return false;
                }

                public void Reset()
                {
                    index = 0;
                    enumerator.Reset();
                }

                #region IDictionaryEnumerator Members

                DictionaryEntry IDictionaryEnumerator.Entry
                {
                    get
                    {
                        if (index == 0 || (index == dictionary.Count + 1))
                            throw new InvalidOperationException("Enumeration has either not started or has already finished.");

                        return new DictionaryEntry(current.Key, current.Value);
                    }
                }

                object IDictionaryEnumerator.Key
                {
                    get
                    {
                        if (index == 0 || (index == dictionary.Count + 1))
                            throw new InvalidOperationException("Enumeration has either not started or has already finished.");

                        return current.Key;
                    }
                }

                object IDictionaryEnumerator.Value
                {
                    get
                    {
                        if (index == 0 || (index == dictionary.Count + 1))
                            throw new InvalidOperationException("Enumeration has either not started or has already finished.");

                        return current.Value;
                    }
                }

                #endregion
            }

            #endregion

            #region Nested Class NullableKeyComparer

#if !NETSTANDARD
            [Serializable]
#endif
            internal class NullableKeyComparer : Comparer<NullableKey<TKey>>
            {
                internal IComparer<TKey> keyComparer;

                public NullableKeyComparer(IComparer<TKey> keyComparer)
                {
                    if (keyComparer == null)
                    {
                        this.keyComparer = Comparer<TKey>.Default; // TODO: Make custom comparer
                    }
                    else
                    {
                        this.keyComparer = keyComparer;
                    }
                }

                public override int Compare(NullableKey<TKey> x, NullableKey<TKey> y)
                {
                    return keyComparer.Compare(x, y); // Implicit conversion
                }
            }

            #endregion

            #region Nested Class NullableKeyValuePairComparer

#if !NETSTANDARD
            [Serializable]
#endif
            internal class NullableKeyValuePairComparer : Comparer<KeyValuePair<NullableKey<TKey>, TValue>>
            {
                internal IComparer<TKey> keyComparer;

                public NullableKeyValuePairComparer(IComparer<TKey> keyComparer)
                {
                    if (keyComparer == null)
                    {
                        this.keyComparer = Comparer<TKey>.Default; // TODO: Make custom comparer
                    }
                    else
                    {
                        this.keyComparer = keyComparer;
                    }
                }

                public override int Compare(KeyValuePair<NullableKey<TKey>, TValue> x, KeyValuePair<NullableKey<TKey>, TValue> y)
                {
                    return keyComparer.Compare(x.Key.Value, y.Key.Value);
                }
            }

            #endregion
        }

        #endregion

        #region Nested Class: ConcreteSortedDictionary<TKey, TValue>

        /// <summary>
        /// An adapter class for <see cref="SCG.Dictionary{TKey, TValue}"/> to implement <see cref="IConcreteSortedDictionary{TKey, TValue}"/>,
        /// which is an interface that is used to share all of the members between <see cref="SCG.Dictionary{TKey, TValue}"/>
        /// and <see cref="NullableKeySortedDictionary"/>.
        /// </summary>
#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        internal class ConcreteSortedDictionary : SCG.SortedDictionary<TKey, TValue>, IConcreteSortedDictionary<TKey, TValue>
        {
            // TODO: Default Comparer implementation for J2N
            public ConcreteSortedDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer) : base(dictionary, comparer ?? Comparer<TKey>.Default) { }

            public ConcreteSortedDictionary(IComparer<TKey> comparer) : base(comparer ?? Comparer<TKey>.Default) { }
        }

        #endregion
    }

    /// <summary>
    /// Interface to expose all of the members of the concrete <see cref="System.Collections.Generic.SortedDictionary{TKey, TValue}"/> type,
    /// so we can duplicate them in other types without having to cast.
    /// </summary>
    internal interface IConcreteSortedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        IComparer<TKey> Comparer { get; }

        //bool ContainsValue(TValue value); // NOTE: We don't want to utilize the built-in method because
        // it uses the .NET default equality comparer, and we want to swap that.
    }
}

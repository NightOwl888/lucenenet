using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace J2N.Collections.Generic
{
    public class LinkedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, KeyValuePair<TKey, TValue>> dictionary = new NullableKeyedCollection().Dictionary;

        public TValue this[TKey key] 
        { 
            get => dictionary[key].Value; 
            set => dictionary[key] = new KeyValuePair<TKey, TValue>(key, value); 
        }

        public ICollection<TKey> Keys => dictionary.Keys;

        public ICollection<TValue> Values => dictionary.Values.Select(x => x.Value).ToList(); // TODO:Create wrapper collection to enumerate these

        public int Count => dictionary.Count;

        public bool IsReadOnly => dictionary.IsReadOnly;

        public void Add(TKey key, TValue value)
            => dictionary.Add(key, new KeyValuePair<TKey, TValue>(key, value));

        public void Add(KeyValuePair<TKey, TValue> item)
            => dictionary.Add(new KeyValuePair<TKey, KeyValuePair<TKey, TValue>>(item.Key, new KeyValuePair<TKey, TValue>(item.Key, item.Value)));

        public void Clear()
            => dictionary.Clear();

        public bool Contains(KeyValuePair<TKey, TValue> item)
            => dictionary.Contains(new KeyValuePair<TKey, KeyValuePair<TKey, TValue>>(item.Key, new KeyValuePair<TKey, TValue>(item.Key, item.Value)));

        public bool ContainsKey(TKey key)
            => dictionary.ContainsKey(key);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            // TODO: Guard clauses

            foreach (var item in this)
                array[arrayIndex++] = item;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var item in dictionary)
                yield return new KeyValuePair<TKey, TValue>(item.Key, item.Value.Value);
        }

        public bool Remove(TKey key)
            => dictionary.Remove(key);

        public bool Remove(KeyValuePair<TKey, TValue> item)
            => dictionary.Remove(new KeyValuePair<TKey, KeyValuePair<TKey, TValue>>(item.Key, new KeyValuePair<TKey, TValue>(item.Key, item.Value)));

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (dictionary.TryGetValue(key, out KeyValuePair<TKey, TValue> pair))
            {
                value = pair.Value;
                return true;
            }
            value = default;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        internal class NullableKeyedCollection : KeyedCollection<TKey, KeyValuePair<TKey, TValue>>
        {
            protected override TKey GetKeyForItem(KeyValuePair<TKey, TValue> item)
                => item.Key;

            new public IDictionary<TKey, KeyValuePair<TKey, TValue>> Dictionary => base.Dictionary;
        }
    }
}

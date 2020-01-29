using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Lucene.Net.Support
{
    /// <summary>
    /// A wrapper class for <see cref="ConditionalWeakTable{TKey, TValue}"/> to provide an enumerator
    /// that is missing from the &lt; .NET Standard 2.1 implementations.
    /// <para/>
    /// If enumeration isn't required for a specific use case, using <see cref="ConditionalWeakTable{TKey, TValue}"/>
    /// directly is more efficient, as extra locking and garbage collection is required to maintain an internal table to keep
    /// track of the keys to enumerate.
    /// </summary>
    /// <typeparam name="TKey">The reference type to which the field is attached.</typeparam>
    /// <typeparam name="TValue">The field's type. This must be a reference type.</typeparam>
    internal class WeakTable<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : class
        where TValue : class/*?*/
    {
        private ConditionalWeakTable<TKey, TValue> weakTable = new ConditionalWeakTable<TKey, TValue>();
#if !FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
        // For the enumerator < .NET Standard 2.1 we need to track the keys in another structure.
        // So, we will also need to clean this list occasionally to prevent consuming too much RAM.
        private List<WeakKey<TKey>> keys = new List<WeakKey<TKey>>();
        //private int activeEnumeratorRefCount;
        private readonly object syncLock = new object();
        private int gcCollections = 0;
#endif

        /// <summary>Gets the value of the specified key.</summary>
        /// <param name="key">key of the value to find. Cannot be null.</param>
        /// <param name="value">
        /// If the key is found, contains the value associated with the key upon method return.
        /// If the key is not found, contains default(TValue).
        /// </param>
        /// <returns>Returns "true" if key was found, "false" otherwise.</returns>
        /// <remarks>
        /// The key may get garbaged collected during the TryGetValue operation. If so, TryGetValue
        /// may at its discretion, return "false" and set "value" to the default (as if the key was not present.)
        /// </remarks>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return weakTable.TryGetValue(key, out value);
        }

        /// <summary>Adds a key to the table.</summary>
        /// <param name="key">key to add. May not be null.</param>
        /// <param name="value">value to associate with key.</param>
        /// <remarks>
        /// If the key is already entered into the dictionary, this method throws an exception.
        /// The key may get garbage collected during the Add() operation. If so, Add()
        /// has the right to consider any prior entries successfully removed and add a new entry without
        /// throwing an exception.
        /// </remarks>
        public void Add(TKey key, TValue value)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

#if FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
            weakTable.Add(key, value);
#else
            lock (syncLock)
            {
                CleanIfNeeded();
                CreateEntry(key, value);
            }
#endif
        }

        /// <summary>Adds the key and value if the key doesn't exist, or updates the existing key's value if it does exist.</summary>
        /// <param name="key">key to add or update. May not be null.</param>
        /// <param name="value">value to associate with key.</param>
        public void AddOrUpdate(TKey key, TValue value)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

#if FEATURE_CONDITIONALWEAKTABLE_ADDORUPDATE
            weakTable.AddOrUpdate(key, value);
#else
            lock (syncLock)
            {
                CleanIfNeeded();
                if (weakTable.TryGetValue(key, out TValue _)) // HACK: Update not supported, so we remove and re-add
                    weakTable.Remove(key);
                CreateEntry(key, value);
            }
#endif
        }

        /// <summary>Removes a key and its value from the table.</summary>
        /// <param name="key">key to remove. May not be null.</param>
        /// <returns>true if the key is found and removed. Returns false if the key was not in the dictionary.</returns>
        /// <remarks>
        /// The key may get garbage collected during the Remove() operation. If so,
        /// Remove() will not fail or throw, however, the return value can be either true or false
        /// depending on who wins the race.
        /// </remarks>
        public bool Remove(TKey key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

#if FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
            return weakTable.Remove(key);
#else
            lock (syncLock)
            {
                bool removed = weakTable.Remove(key);
                keys.Remove(new WeakKey<TKey>(key));
                return removed;
            }
#endif
        }


        /// <summary>Clear all the key/value pairs</summary>
        public void Clear()
        {
#if FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
            weakTable.Clear();
#else
            lock (syncLock)
            {
                weakTable = new ConditionalWeakTable<TKey, TValue>(); // HACK: Clear() not supported, so we reset the instance
                keys.Clear();
            }
#endif
        }


        /// <summary>
        /// Atomically searches for a specified key in the table and returns the corresponding value.
        /// If the key does not exist in the table, the method invokes a callback method to create a
        /// value that is bound to the specified key.
        /// </summary>
        /// <param name="key">key of the value to find. Cannot be null.</param>
        /// <param name="createValueCallback">callback that creates value for key. Cannot be null.</param>
        /// <returns></returns>
        /// <remarks>
        /// If multiple threads try to initialize the same key, the table may invoke createValueCallback
        /// multiple times with the same key. Exactly one of these calls will succeed and the returned
        /// value of that call will be the one added to the table and returned by all the racing GetValue() calls.
        /// This rule permits the table to invoke createValueCallback outside the internal table lock
        /// to prevent deadlocks.
        /// </remarks>
        public TValue GetValue(TKey key, ConditionalWeakTable<TKey, TValue>.CreateValueCallback createValueCallback)
        {
            // key is validated by TryGetValue

            if (createValueCallback is null)
            {
                throw new ArgumentNullException(nameof(createValueCallback));
            }

#if FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
            return weakTable.GetValue(key, createValueCallback);
#else
            return weakTable.TryGetValue(key, out TValue existingValue) ?
                existingValue :
                GetValueLocked(key, createValueCallback);
#endif
        }

#if !FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
        private TValue GetValueLocked(TKey key, ConditionalWeakTable<TKey, TValue>.CreateValueCallback createValueCallback)
        {
            // If we got here, the key was not in the table. Invoke the callback (outside the lock)
            // to generate the new value for the key.
            TValue newValue = createValueCallback(key);

            lock (syncLock)
            {
                // Now that we've taken the lock, must recheck in case we lost a race to add the key.
                if (weakTable.TryGetValue(key, out TValue existingValue))
                {
                    return existingValue;
                }
                else
                {
                    CleanIfNeeded();
                    // Verified in-lock that we won the race to add the key. Add it now.
                    CreateEntry(key, newValue);
                    return newValue;
                }
            }
        }

        /// <summary>Worker for adding a new key/value pair. Will resize the container if it is full.</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void CreateEntry(TKey key, TValue value)
        {
            Debug.Assert(Monitor.IsEntered(syncLock));
            Debug.Assert(key != null); // key already validated as non-null and not already in table.

            weakTable.Add(key, value);
            var weakKey = new WeakKey<TKey>(key);
            if (!keys.Contains(weakKey))
                keys.Add(weakKey);
        }
#endif

        /// <summary>
        /// Helper method to call GetValue without passing a creation delegate.  Uses Activator.CreateInstance
        /// to create new instances as needed.  If TValue does not have a default constructor, this will throw.
        /// </summary>
        /// <param name="key">key of the value to find. Cannot be null.</param>
        public TValue GetOrCreateValue(TKey key) => GetValue(key, _ => Activator.CreateInstance<TValue>());


        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
#if FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)weakTable).GetEnumerator();
#else
            return new Enumerator(this);
#endif
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


#if !FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
        private void Clean()
        {
            if (keys.Count == 0) return;
            var newList = new List<WeakKey<TKey>>(keys.Count);
            foreach (var entry in keys)
            {
                if (entry.Target != null && entry.IsAlive)
                    newList.Add(entry);
            }
            keys = newList;
        }

        private void CleanIfNeeded()
        {
            int currentColCount = GC.CollectionCount(0);
            if (currentColCount > gcCollections)
            {
                Clean();
                gcCollections = currentColCount;
            }
        }
#endif

#if !FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
        private class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private WeakTable<TKey, TValue> parentTable; // parent table, set to null when disposed
            private int currentIndex;                    // the current index into the container (only used to determine if we haven't yet started enumeration)
            private KeyValuePair<TKey, TValue> current;  // the current entry set by MoveNext and returned from Current
            private readonly IEnumerator<WeakKey<TKey>> enumerator;

            public Enumerator(WeakTable<TKey, TValue> parentTable)
            {
                this.parentTable = parentTable ?? throw new ArgumentNullException(nameof(parentTable));

                current = default;
                currentIndex = -1;

                // We create a clone of the keys and will be enumerating those. This way, we can
                // make changes to the underlying keys list as we enumerate.

                // This is specialized - since Lucene only opens enumerators long enough to
                // read them to the end, we don't care about new keys being added.
                // Most of the time, we are only using the enumerator to keep track of RAM usage, anyway.
                lock (parentTable.syncLock)
                {
                    enumerator = new List<WeakKey<TKey>>(parentTable.keys).GetEnumerator();
                }
            }

            public KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    if (currentIndex < 0)
                    {
                        throw new InvalidOperationException("Enumeration has either not started or has already finished.");
                    }
                    return current;
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                parentTable = null;
                // Ensure we don't keep the last current alive unnecessarily
                current = default;
                enumerator.Dispose();
            }

            public bool MoveNext()
            {
                while (enumerator.MoveNext())
                {
                    var currentKey = enumerator.Current;
                    var target = currentKey.Target; // Careful: can be null in the mean time...
                    if (target != null)
                    {
                        // The ConditionalWeakTable has the final say whether a key really is alive.
                        // Try to get the value. If it exists, we return the KVP. If not, we
                        // reap the key and keep going.
                        if (parentTable.weakTable.TryGetValue(target, out TValue value))
                        {
                            currentIndex++;
                            current = new KeyValuePair<TKey, TValue>(target, value);
                            return true;
                        }
                        else
                        {
                            RemoveKey(currentKey); // Reap the key, it is dead
                            // No return - keep enumerating until we find a live entry or the end
                        }
                    }
                    else
                    {
                        RemoveKey(currentKey); // Reap the key, it is dead
                        // No return - keep enumerating until we find a live entry or the end
                    }
                }

                // Nothing more to enumerate.
                current = default;
                return false;
            }

            private void RemoveKey(WeakKey<TKey> key)
            {
                lock (parentTable.syncLock)
                {
                    parentTable.keys.Remove(key);
                }
            }

            public void Reset() { }
        }

        /// <summary>
        /// A weak reference wrapper for the hashtable keys. Whenever a key\value pair
        /// is added to the hashtable, the key is wrapped using a WeakKey. WeakKey saves the
        /// value of the original object hashcode for fast comparison.
        /// </summary>
        private class WeakKey<T> where T : class
        {
            private readonly WeakReference reference;
            private readonly int hashCode;

            public WeakKey(T key)
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                hashCode = key.GetHashCode();
                reference = new WeakReference(key);
            }

            public override int GetHashCode()
            {
                return hashCode;
            }

            public override bool Equals(object obj)
            {
                if (!reference.IsAlive || obj == null) return false;

                if (object.ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj is WeakKey<T> other)
                {
                    var referenceTarget = reference.Target; // Careful: can be null in the mean time...
                    return referenceTarget != null && referenceTarget.Equals(other.Target);
                }

                return false;
            }

            public T Target => (T)reference.Target;

            public bool IsAlive => reference.IsAlive;
        }
#endif
    }
}

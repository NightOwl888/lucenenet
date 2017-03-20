using Lucene.Net.Support;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Diagnostics;

namespace Lucene.Net.Util
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
    /// Implements a combination of <seealso cref="java.util.WeakHashMap"/> and
    /// <seealso cref="java.util.IdentityHashMap"/>.
    /// Useful for caches that need to key off of a {@code ==} comparison
    /// instead of a {@code .equals}.
    ///
    /// <p>this class is not a general-purpose <seealso cref="java.util.Map"/>
    /// implementation! It intentionally violates
    /// Map's general contract, which mandates the use of the equals method
    /// when comparing objects. this class is designed for use only in the
    /// rare cases wherein reference-equality semantics are required.
    ///
    /// <p>this implementation was forked from <a href="http://cxf.apache.org/">Apache CXF</a>
    /// but modified to <b>not</b> implement the <seealso cref="java.util.Map"/> interface and
    /// without any set views on it, as those are error-prone and inefficient,
    /// if not implemented carefully. The map only contains <seealso cref="Iterator"/> implementations
    /// on the values and not-GCed keys. Lucene's implementation also supports {@code null}
    /// keys, but those are never weak!
    ///
    /// <p><a name="reapInfo" />The map supports two modes of operation:
    /// <ul>
    ///  <li>{@code reapOnRead = true}: this behaves identical to a <seealso cref="java.util.WeakHashMap"/>
    ///  where it also cleans up the reference queue on every read operation (<seealso cref="#get(Object)"/>,
    ///  <seealso cref="#containsKey(Object)"/>, <seealso cref="#size()"/>, <seealso cref="#valueIterator()"/>), freeing map entries
    ///  of already GCed keys.</li>
    ///  <li>{@code reapOnRead = false}: this mode does not call <seealso cref="#reap()"/> on every read
    ///  operation. In this case, the reference queue is only cleaned up on write operations
    ///  (like <seealso cref="#put(Object, Object)"/>). this is ideal for maps with few entries where
    ///  the keys are unlikely be garbage collected, but there are lots of <seealso cref="#get(Object)"/>
    ///  operations. The code can still call <seealso cref="#reap()"/> to manually clean up the queue without
    ///  doing a write operation.</li>
    /// </ul>
    ///
    /// @lucene.internal
    /// </summary>
    public sealed class WeakIdentityMap<TKey, TValue>
        where TKey : class
    {
        //private readonly ReferenceQueue<object> queue = new ReferenceQueue<object>();
        private readonly IDictionary<IdentityWeakReference, TValue> backingStore;

        private readonly bool reapOnRead;

        /// <summary>
        /// Creates a new {@code WeakIdentityMap} based on a non-synchronized <seealso cref="HashMap"/>.
        /// The map <a href="#reapInfo">cleans up the reference queue on every read operation</a>.
        /// </summary>
        public static WeakIdentityMap<TKey, TValue> NewHashMap()
        {
            return NewHashMap(false);
        }

        /// <summary>
        /// Creates a new {@code WeakIdentityMap} based on a non-synchronized <seealso cref="HashMap"/>. </summary>
        /// <param name="reapOnRead"> controls if the map <a href="#reapInfo">cleans up the reference queue on every read operation</a>. </param>
        public static WeakIdentityMap<TKey, TValue> NewHashMap(bool reapOnRead)
        {
            return new WeakIdentityMap<TKey, TValue>(new HashMap<IdentityWeakReference, TValue>(), reapOnRead);
        }

        /// <summary>
        /// Creates a new {@code WeakIdentityMap} based on a <seealso cref="ConcurrentHashMap"/>.
        /// The map <a href="#reapInfo">cleans up the reference queue on every read operation</a>.
        /// </summary>
        public static WeakIdentityMap<TKey, TValue> NewConcurrentHashMap()
        {
            return NewConcurrentHashMap(true);
        }

        /// <summary>
        /// Creates a new {@code WeakIdentityMap} based on a <seealso cref="ConcurrentHashMap"/>. </summary>
        /// <param name="reapOnRead"> controls if the map <a href="#reapInfo">cleans up the reference queue on every read operation</a>. </param>
        public static WeakIdentityMap<TKey, TValue> NewConcurrentHashMap(bool reapOnRead)
        {
            return new WeakIdentityMap<TKey, TValue>(new ConcurrentDictionary<IdentityWeakReference, TValue>(), reapOnRead);
        }

        /// <summary>
        /// Private only constructor, to create use the static factory methods. </summary>
        private WeakIdentityMap(IDictionary<IdentityWeakReference, TValue> backingStore, bool reapOnRead)
        {
            this.backingStore = backingStore;
            this.reapOnRead = reapOnRead;
        }

        /// <summary>
        /// Removes all of the mappings from this map. </summary>
        public void Clear()
        {
            backingStore.Clear();
            Reap();
        }

        /// <summary>
        /// Returns {@code true} if this map contains a mapping for the specified key. </summary>
        public bool ContainsKey(object key)
        {
            if (reapOnRead)
            {
                Reap();
            }
            return backingStore.ContainsKey(new IdentityWeakReference(key));
        }

        /// <summary>
        /// Returns the value to which the specified key is mapped. </summary>
        public TValue Get(object key)
        {
            if (reapOnRead)
            {
                Reap();
            }

            TValue val;
            if (backingStore.TryGetValue(new IdentityWeakReference(key), out val))
            {
                return val;
            }
            else
            {
                return default(TValue);
            }
        }

        /// <summary>
        /// Associates the specified value with the specified key in this map.
        /// If the map previously contained a mapping for this key, the old value
        /// is replaced.
        /// </summary>
        public TValue Put(TKey key, TValue value)
        {
            Reap();
            return backingStore[new IdentityWeakReference(key)] = value;
        }

        public IEnumerable<TKey> Keys
        {
            get
            {
                return new KeyWrapper(this);
            }
        }

        /// <summary>
        /// LUCENENET specific class to allow the 
        /// GetEnumerator() method to be overridden
        /// for the keys so we can return an enumerator
        /// that is smart enough to clean up the dead keys
        /// and also so that MoveNext() returns false in the
        /// event there are no more values left (instead of returning
        /// a null value in an extra enumeration).
        /// </summary>
        internal class KeyWrapper : IEnumerable<TKey>
        {
            private readonly WeakIdentityMap<TKey, TValue> outerInstance;
            public KeyWrapper(WeakIdentityMap<TKey, TValue> outerInstance)
            {
                this.outerInstance = outerInstance;
            }
            public IEnumerator<TKey> GetEnumerator()
            {
                outerInstance.Reap();

                // Get a clone of the iterator, so we can delete items from our backingStore
                // without having to worry about "collection modified" exceptions.
                IEnumerator<IdentityWeakReference> iterator = outerInstance.backingStore.Keys.ToList().GetEnumerator();
                return new KeyEnumerator(this, iterator);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class KeyEnumerator : IEnumerator<TKey>
            {
                private readonly KeyWrapper outerInstance;
                private readonly IEnumerator<IdentityWeakReference> iterator;
                public KeyEnumerator(KeyWrapper outerInstance, IEnumerator<IdentityWeakReference> iterator)
                {
                    this.outerInstance = outerInstance;
                    this.iterator = iterator;
                }

                private IdentityWeakReference nextRef = null;
                // holds strong reference to next element in backing iterator:
                private object next = null;
                // the backing iterator was already consumed:
                private bool nextIsSet = false;

                internal bool HasNext()
                {
                    return nextIsSet || SetNext();
                }

                public TKey Current
                {
                    get
                    {
                        if (!HasNext())
                        {
                            // LUCENENET NOTE: This is unusual for .NET, but
                            // to ensure that another thread didn't eat our last item,
                            // we need to test for its presence here. The only logical thing to do
                            // in this case is throw an exception (since it is already too late
                            // to return false from MoveNext()) and expect that the caller will
                            // catch the exception and skip over the loop if it happens.
                            //throw new NoSuchElementException();
                            return null;
                        }

                        try
                        {
                            return (TKey)next;
                        }
                        finally
                        {
                            // release strong reference and invalidate current value:
                            nextIsSet = false;
                            nextRef = null;
                            next = null;
                        }
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    return HasNext();
                }

                private bool SetNext()
                {
                    Debug.Assert(!nextIsSet);

                    while (iterator.MoveNext())
                    {
                        nextRef = iterator.Current;
                        next = nextRef.Target;
                        if (next == null || (nextRef != null && !nextRef.IsAlive))
                        {
                            // the key was already GCed, we can remove it from backing map:
                            outerInstance.outerInstance.backingStore.Remove(nextRef);
                        }
                        else
                        {
                            // unfold "null" special value:
                            if (next == NULL)
                            {
                                next = null;
                            }
                            return nextIsSet = true;
                        }
                    }
                    return false;
                }

                public void Reset()
                {
                    throw new NotSupportedException();
                }
            }
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                if (reapOnRead) Reap();
                return backingStore.Values;
            }
        }

        /// <summary>
        /// Returns {@code true} if this map contains no key-value mappings. </summary>
        public bool IsEmpty
        {
            get
            {
                return Count == 0;
            }
        }

        /// <summary>
        /// Removes the mapping for a key from this weak hash map if it is present.
        /// Returns the value to which this map previously associated the key,
        /// or {@code null} if the map contained no mapping for the key.
        /// A return value of {@code null} does not necessarily indicate that
        /// the map contained.
        /// </summary>
        public bool Remove(object key)
        {
            Reap();
            return backingStore.Remove(new IdentityWeakReference(key));
        }

        /// <summary>
        /// Returns the number of key-value mappings in this map. this result is a snapshot,
        /// and may not reflect unprocessed entries that will be removed before next
        /// attempted access because they are no longer referenced.
        /// NOTE: This was size() in Lucene.
        /// </summary>
        public int Count
        {
            get
            {
                if (backingStore.Count == 0)
                {
                    return 0;
                }
                if (reapOnRead)
                {
                    Reap();
                }
                return backingStore.Count;
            }
        }

        /// <summary>
        /// Returns an iterator over all values of this map.
        /// this iterator may return values whose key is already
        /// garbage collected while iterator is consumed,
        /// especially if {@code reapOnRead} is {@code false}.
        /// <para/>
        /// NOTE: This was valueIterator() in Lucene.
        /// </summary>
        public IEnumerator<TValue> GetValueEnumerator()
        {
            if (reapOnRead)
            {
                Reap();
            }
            return backingStore.Values.GetEnumerator();
        }

        /// <summary>
        /// this method manually cleans up the reference queue to remove all garbage
        /// collected key/value pairs from the map. Calling this method is not needed
        /// if {@code reapOnRead = true}. Otherwise it might be a good idea
        /// to call this method when there is spare time (e.g. from a background thread). </summary>
        /// <seealso cref= <a href="#reapInfo">Information about the <code>reapOnRead</code> setting</a> </seealso>
        public void Reap()
        {
            foreach (IdentityWeakReference zombie in backingStore.Keys.ToArray())
            {
                if (!zombie.IsAlive)
                {
                    backingStore.Remove(zombie);
                }
            }
        }

        // we keep a hard reference to our NULL key, so map supports null keys that never get GCed:
        internal static readonly object NULL = new object();

        private sealed class IdentityWeakReference : WeakReference
        {
            private readonly int hash;

            internal IdentityWeakReference(object obj/*, ReferenceQueue<object> queue*/)
                : base(obj == null ? NULL : obj/*, queue*/)
            {
                hash = RuntimeHelpers.GetHashCode(obj);
            }

            public override int GetHashCode()
            {
                return hash;
            }

            public override bool Equals(object o)
            {
                if (this == o)
                {
                    return true;
                }
                if (o is IdentityWeakReference)
                {
                    IdentityWeakReference @ref = (IdentityWeakReference)o;
                    if (this.Target == @ref.Target)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
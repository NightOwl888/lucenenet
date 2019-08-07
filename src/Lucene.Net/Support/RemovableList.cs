using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Support
{

    public interface IRemovableEnumerator<T> : IEnumerator<T>
    {
        void Remove();
    }

    public interface IRemovableEnumerable<T> : IEnumerable<T>
    {
        IRemovableEnumerator<T> GetRemovableEnumerator();
    }

    /// <summary>
    /// An <see cref="IList{T}"/> implementation with an <see cref="IRemovableEnumerator{T}"/>.
    /// That is, an enumerator with a <see cref="IRemovableEnumerator{T}.Remove()"/> method that allows
    /// removing items from the list while looping through it.
    /// <para/>
    /// Each instance of <see cref="RemovableList{T}"/> can be used with multiple enumerators by calling
    /// <see cref="GetEnumerator()"/> or to expose the <see cref="IRemovableEnumerator{T}.Remove()"/> method 
    /// without doing a cast, the <see cref="GetRemovableEnumerator()"/> method. As long as the enumerators
    /// are all created from the same <see cref="RemovableList{T}"/> instance, they are thread safe. However,
    /// wrapping the same list using multiple <see cref="RemovableList{T}"/> instances is not thread safe.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class RemovableList<T> : IList<T>, IRemovableEnumerable<T>
    {
        private readonly IList<T> list;
        private readonly ReaderWriterLockSlim syncLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        internal event EventHandler<ItemRemovedEventArgs> ItemRemoved;

        public RemovableList(IList<T> wrappedList)
        {
            this.list = wrappedList ?? throw new ArgumentNullException(nameof(wrappedList));
        }

        public RemovableList(ICollection<T> wrappedList)
        {
            if (wrappedList == null)
                throw new ArgumentNullException(nameof(wrappedList));
            if (wrappedList is IList<T>)
                this.list = (IList<T>)wrappedList;
            else
                this.list = new List<T>(wrappedList);
        }

        public RemovableList()
            : this(new List<T>())
        {
        }

        public T this[int index]
        {
            get
            {
                syncLock.EnterReadLock();
                try
                {
                    return list[index];
                }
                finally
                {
                    syncLock.ExitReadLock();
                }
            }
            set
            {
                syncLock.EnterWriteLock();
                try
                {
                    list[index] = value;
                }
                finally
                {
                    syncLock.ExitWriteLock();
                }
            }
        }

        public int Count
        {
            get
            {
                syncLock.EnterReadLock();
                try
                {
                    return list.Count;
                }
                finally
                {
                    syncLock.ExitReadLock();
                }
            }
        }

        public bool IsReadOnly => false; // NOTE: This should always be false, but depends on the list that is passed in

        public void Add(T item)
        {
            syncLock.EnterWriteLock();
            try
            {
                list.Add(item);
            }
            finally
            {
                syncLock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            syncLock.EnterWriteLock();
            try
            {
                list.Clear();
            }
            finally
            {
                syncLock.ExitWriteLock();
            }
        }

        public bool Contains(T item)
        {
            syncLock.EnterReadLock();
            try
            {
                return list.Contains(item);
            }
            finally
            {
                syncLock.EnterReadLock();
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            syncLock.EnterReadLock();
            try
            {
                list.CopyTo(array, arrayIndex);
            }
            finally
            {
                syncLock.EnterReadLock();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetRemovableEnumerator();
        }

        public IRemovableEnumerator<T> GetRemovableEnumerator()
        {
            var removable = new RemovableListEnumerator(this);
            this.ItemRemoved += removable.List_ItemRemoved;
            return removable;
        }

        public int IndexOf(T item)
        {
            syncLock.EnterReadLock();
            try
            {
                return list.IndexOf(item);
            }
            finally
            {
                syncLock.EnterReadLock();
            }
        }

        public void Insert(int index, T item)
        {
            syncLock.EnterWriteLock();
            try
            {
                list.Insert(index, item);
            }
            finally
            {
                syncLock.ExitWriteLock();
            }
        }

        public bool Remove(T item)
        {
            syncLock.EnterWriteLock();
            try
            {
                int itemIndex = list.IndexOf(item);
                bool removed = list.Remove(item);
                OnItemRemoved(new ItemRemovedEventArgs { ItemIndex = itemIndex });
                return removed;
            }
            finally
            {
                syncLock.ExitWriteLock();
            }
        }

        public void RemoveAt(int index)
        {
            syncLock.EnterWriteLock();
            try
            {
                list.RemoveAt(index);
                OnItemRemoved(new ItemRemovedEventArgs { ItemIndex = index });
            }
            finally
            {
                syncLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes and returns the first item from the list.
        /// </summary>
        /// <returns></returns>
        public T Pop()
        {
            syncLock.EnterWriteLock();
            try
            {
                if (list.Count == 0)
                    return default;
                T first = list[0];
                while (!list.Remove(first))
                    first = list.Count == 0 ? default : list[0];
                OnItemRemoved(new ItemRemovedEventArgs { ItemIndex = 0 });
                return first;
            }
            finally
            {
                syncLock.ExitWriteLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetRemovableEnumerator();
        }

        internal void OnItemRemoved(ItemRemovedEventArgs e)
        {
            ItemRemoved?.Invoke(this, e);
        }

        internal class RemovableListEnumerator : IRemovableEnumerator<T>
        {
            private readonly RemovableList<T> outerInstance;
            private int currentIndex = -1; // Start 1 before our list begins
            private AtomicBoolean isRemoved = new AtomicBoolean(false);
            public RemovableListEnumerator(RemovableList<T> outerInstance)
            {
                this.outerInstance = outerInstance ?? throw new ArgumentNullException(nameof(outerInstance));
            }

            public T Current
            {
                get
                {
                    outerInstance.syncLock.EnterReadLock();
                    try
                    {
                        var current = Interlocked.CompareExchange(ref currentIndex, 0, 0);
                        if (current >= outerInstance.Count || current < 0)
                            return default;
                        return outerInstance.list[current];
                    }
                    finally
                    {
                        outerInstance.syncLock.ExitReadLock();
                    }
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                // Detatch our event
                outerInstance.ItemRemoved -= List_ItemRemoved;
            }

            public bool MoveNext()
            {
                if (isRemoved.CompareAndSet(true, false) == true)
                    return true;
                if (Interlocked.Increment(ref currentIndex) >= outerInstance.Count)
                {
                    Interlocked.Decrement(ref currentIndex);
                    return false;
                }
                return true;
            }

            public void Remove()
            {
                // An item can only be removed one time per MoveNext() call
                if (isRemoved.CompareAndSet(false, true))
                {
                    outerInstance.syncLock.EnterWriteLock();
                    try
                    {
                        int current;
                        outerInstance.list.RemoveAt(current = Interlocked.CompareExchange(ref currentIndex, 0, 0));
                        outerInstance.OnItemRemoved(new ItemRemovedEventArgs { ItemIndex = current, EnumeratorInstance = this });
                    }
                    finally
                    {
                        outerInstance.syncLock.ExitWriteLock();
                    }
                }
            }

            public void Reset()
            {
                Interlocked.Exchange(ref currentIndex, 0);
            }

            internal void List_ItemRemoved(object sender, ItemRemovedEventArgs e)
            {
                if (e.EnumeratorInstance != this)
                {
                    // Decrementing doesn't necessarily need to be atomic, so long as it happens.
                    // We care more about the end count being right than which thread goes first.
                    if (e.ItemIndex >= 0 && e.ItemIndex < Interlocked.CompareExchange(ref currentIndex, 0, 0))
                        Interlocked.Decrement(ref currentIndex);
                }
            }
        }

        internal class ItemRemovedEventArgs : EventArgs
        {
            public int ItemIndex { get; set; }

            /// <summary>
            /// The enumerator instance that called the event. This
            /// may be null if the event was called from <see cref="Remove(T)"/>
            /// or <see cref="RemoveAt(int)"/>.
            /// </summary>
            public RemovableListEnumerator EnumeratorInstance { get; set; }
        }
    }
}

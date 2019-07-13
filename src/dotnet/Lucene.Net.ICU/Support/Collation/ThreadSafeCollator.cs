using Icu.Collation;
using System;
using System.Threading;
#if NETSTANDARD
using SortKey = Icu.SortKey;
#else
using SortKey = System.Globalization.SortKey;
#endif


namespace Lucene.Net.Collation
{

    internal class ThreadSafeCollator : Collator
    {
        private readonly Collator collator;
        private readonly object syncLock = new object();

        public ThreadSafeCollator(Collator collator)
        {
            this.collator = (Collator)collator.Clone();
        }

        public override CollationStrength Strength
        {
            get
            {
                using (var helper = new CollatorThreadHelper<CollationStrength>(this.collator, (clone) => clone.Strength))
                {
                    helper.Invoke();
                    return helper.Result;
                }
            }
            set
            {
                lock (syncLock)
                {
                    collator.Strength = value;
                }
            }
        }

        public override NormalizationMode NormalizationMode
        {
            get
            {
                using (var helper = new CollatorThreadHelper<NormalizationMode>(this.collator, (clone) => clone.NormalizationMode))
                {
                    helper.Invoke();
                    return helper.Result;
                }
            }
            set
            {
                lock (syncLock)
                {
                    collator.NormalizationMode = value;
                }
            }
        }

        public override FrenchCollation FrenchCollation
        {
            get
            {
                using (var helper = new CollatorThreadHelper<FrenchCollation>(this.collator, (clone) => clone.FrenchCollation))
                {
                    helper.Invoke();
                    return helper.Result;
                }
            }
            set
            {
                lock (syncLock)
                {
                    collator.FrenchCollation = value;
                }
            }
        }

        public override CaseLevel CaseLevel
        {
            get
            {
                using (var helper = new CollatorThreadHelper<CaseLevel>(this.collator, (clone) => clone.CaseLevel))
                {
                    helper.Invoke();
                    return helper.Result;
                }
            }
            set
            {
                lock (syncLock)
                {
                    collator.CaseLevel = value;
                }
            }
        }

        [Obsolete]
        public override HiraganaQuaternary HiraganaQuaternary
        {
            get
            {
                using (var helper = new CollatorThreadHelper<HiraganaQuaternary>(this.collator, (clone) => clone.HiraganaQuaternary))
                {
                    helper.Invoke();
                    return helper.Result;
                }
            }
            set
            {
                lock (syncLock)
                {
                    collator.HiraganaQuaternary = value;
                }
            }
        }

        public override NumericCollation NumericCollation
        {
            get
            {
                using (var helper = new CollatorThreadHelper<NumericCollation>(this.collator, (clone) => clone.NumericCollation))
                {
                    helper.Invoke();
                    return helper.Result;
                }
            }
            set
            {
                lock (syncLock)
                {
                    collator.NumericCollation = value;
                }
            }
        }

        public override CaseFirst CaseFirst
        {
            get
            {
                using (var helper = new CollatorThreadHelper<CaseFirst>(this.collator, (clone) => clone.CaseFirst))
                {
                    helper.Invoke();
                    return helper.Result;
                }
            }
            set
            {
                lock (syncLock)
                {
                    collator.CaseFirst = value;
                }
            }
        }

        public override AlternateHandling AlternateHandling
        {
            get
            {
                using (var helper = new CollatorThreadHelper<AlternateHandling>(this.collator, (col) => col.AlternateHandling))
                {
                    helper.Invoke();
                    return helper.Result;
                }
            }
            set
            {
                lock (syncLock)
                {
                    collator.AlternateHandling = value;
                }
            }
        }

        public override object Clone()
        {
            // No need to use our helper here...
            return this.collator.Clone();
        }

        public override int Compare(string source, string target)
        {
            using (var helper = new CollatorThreadHelper<int>(this.collator, (col) => col.Compare(source, target)))
            {
                helper.Invoke();
                return helper.Result;
            }
        }

        public override SortKey GetSortKey(string source)
        {
            using (var helper = new CollatorThreadHelper<SortKey>(this.collator, (col) => col.GetSortKey(source)))
            {
                helper.Invoke();
                return helper.Result;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.collator?.Dispose();
            }
            base.Dispose(disposing);
        }

        internal class CollatorThreadHelper<TResult> : IDisposable
        {
            private readonly Collator clonedCollator;
            private readonly Thread thread;
            private Exception exception;

            public CollatorThreadHelper(Collator collator, Func<Collator, TResult> action)
            {
                this.clonedCollator = (Collator)collator.Clone();
                this.thread = new Thread(() =>
                {
                    try
                    {
                        this.Result = action(this.clonedCollator);
                    }
                    catch (Exception ex)
                    {
                        this.exception = ex;
                    }
                });
            }
            public TResult Result { get; private set; }

            public void Invoke()
            {
                thread.Start();
                thread.Join();
                if (exception != null) throw exception;
            }

            public void Dispose()
            {
                this.clonedCollator.Dispose();
            }
        }
    }




    //internal class ThreadSafeCollator : Collator
    //{
    //    private const int cleanupIntervalInSeconds = 5;

    //    private readonly Collator collator;
    //    private readonly IDictionary<int, ThreadReference> cache = new Dictionary<int, ThreadReference>();
    //    private readonly ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    //    private readonly CancellationTokenSource cleanReferencesTask;


    //    public ThreadSafeCollator(Collator collator)
    //    {
    //        if (collator == null)
    //            throw new ArgumentNullException(nameof(collator));

    //        this.collator = (Collator)collator.Clone();
    //        this.cleanReferencesTask = new CancellationTokenSource();
    //        Repeat.Interval(TimeSpan.FromSeconds(cleanupIntervalInSeconds), () => CleanCollatorReferences(), cleanReferencesTask.Token);
    //    }

    //    private void CleanCollatorReferences()
    //    {
    //        cacheLock.EnterUpgradeableReadLock();
    //        try
    //        {
    //            var deadReferences = cache.Where(r => !r.Value.IsAlive).ToArray();
    //            if (deadReferences.Any())
    //            {
    //                cacheLock.EnterWriteLock();
    //                try
    //                {
    //                    foreach (var deadReference in deadReferences)
    //                        cache.Remove(deadReference);
    //                }
    //                finally
    //                {
    //                    cacheLock.ExitWriteLock();
    //                }
    //            }
    //        }
    //        finally
    //        {
    //            cacheLock.ExitUpgradeableReadLock();
    //        }
    //    }

    //    [DllImport("kernel32.dll")]
    //    static extern int GetCurrentThreadId();

    //    private Collator GetCurrentCollator()
    //    {
    //        cacheLock.EnterUpgradeableReadLock();
    //        try
    //        {
    //            //int id = Thread.CurrentThread.ManagedThreadId;
    //            int id = GetCurrentThreadId();
    //            if (cache.ContainsKey(id))
    //            {
    //                return cache[id].Collator;
    //            }
    //            else
    //            {
    //                cacheLock.EnterWriteLock();
    //                try
    //                {
    //                    return (cache[id] = new ThreadReference(collator, Thread.CurrentThread)).Collator;
    //                }
    //                finally
    //                {
    //                    cacheLock.ExitWriteLock();
    //                }
    //            }
    //        }
    //        finally
    //        {
    //            cacheLock.ExitUpgradeableReadLock();
    //        }
    //    }

    //    public override CollationStrength Strength
    //    {
    //        get => GetCurrentCollator().Strength;
    //        set => GetCurrentCollator().Strength = value;
    //    }

    //    public override NormalizationMode NormalizationMode
    //    {
    //        get => GetCurrentCollator().NormalizationMode;
    //        set => GetCurrentCollator().NormalizationMode = value;
    //    }

    //    public override FrenchCollation FrenchCollation
    //    {
    //        get => GetCurrentCollator().FrenchCollation;
    //        set => GetCurrentCollator().FrenchCollation = value;
    //    }

    //    public override CaseLevel CaseLevel
    //    {
    //        get => GetCurrentCollator().CaseLevel;
    //        set => GetCurrentCollator().CaseLevel = value;
    //    }

    //    [Obsolete]
    //    public override HiraganaQuaternary HiraganaQuaternary
    //    {
    //        get => GetCurrentCollator().HiraganaQuaternary;
    //        set => GetCurrentCollator().HiraganaQuaternary = value;
    //    }

    //    public override NumericCollation NumericCollation
    //    {
    //        get => GetCurrentCollator().NumericCollation;
    //        set => GetCurrentCollator().NumericCollation = value;
    //    }

    //    public override CaseFirst CaseFirst
    //    {
    //        get => GetCurrentCollator().CaseFirst;
    //        set => GetCurrentCollator().CaseFirst = value;
    //    }

    //    public override AlternateHandling AlternateHandling
    //    {
    //        get => GetCurrentCollator().AlternateHandling;
    //        set => GetCurrentCollator().AlternateHandling = value;
    //    }

    //    public override object Clone()
    //    {
    //        // No need to use our helper here...
    //        return this.collator.Clone();
    //    }

    //    public override int Compare(string source, string target)
    //    {
    //        return GetCurrentCollator().Compare(source, target);
    //    }

    //    public override SortKey GetSortKey(string source)
    //    {
    //        return GetCurrentCollator().GetSortKey(source);
    //    }

    //    protected override void Dispose(bool disposing)
    //    {
    //        if (disposing)
    //        {
    //            cacheLock.EnterWriteLock();
    //            try
    //            {
    //                this.cleanReferencesTask.Cancel();
    //                this.collator.Dispose();
    //                foreach (var threadReference in cache.Values)
    //                {
    //                    threadReference.Collator?.Dispose();
    //                }
    //                this.cleanReferencesTask.Dispose();
    //                this.cacheLock.Dispose();
    //            }
    //            finally
    //            {
    //                cacheLock.ExitWriteLock();
    //            }
    //        }
    //        base.Dispose(disposing);
    //    }

    //    internal class ThreadReference
    //    {
    //        private readonly WeakReference<Thread> thread;
    //        public ThreadReference(Collator collator, Thread thread)
    //        {
    //            this.Collator = (Collator)collator.Clone();
    //            this.thread = new WeakReference<Thread>(thread);
    //        }

    //        public Collator Collator { get; private set; }
    //        public bool IsAlive => this.thread.TryGetTarget(out Thread target);
    //    }

    //    internal static class Repeat
    //    {
    //        public static Task Interval(
    //            TimeSpan pollInterval,
    //            Action action,
    //            CancellationToken token)
    //        {
    //            // We don't use Observable.Interval:
    //            // If we block, the values start bunching up behind each other.
    //            return Task.Factory.StartNew(
    //                () =>
    //                {
    //                    while (true)
    //                    {
    //                        if (token.WaitHandle.WaitOne(pollInterval))
    //                            break;

    //                        action();
    //                    }
    //                }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    //        }
    //    }
    //}
}

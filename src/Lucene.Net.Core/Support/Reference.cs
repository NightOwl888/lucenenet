using System;
using System.Threading;

namespace Lucene.Net.Support
{
    public abstract class Reference<T> where T : class
    {
        private T referent;         /* Treated specially by GC */

        internal volatile ReferenceQueue<T> queue;

        /// <summary>
        /// When active:   NULL
        ///     pending:   this
        ///    Enqueued:   next reference in queue (or this if last)
        ///    Inactive:   this   
        /// </summary>
        internal Reference<T> next;

        /// <summary>
        /// When active:   next element in a discovered reference list maintained by GC (or this if last)
        ///     pending:   next element in the pending list (or null if last)
        ///   otherwise:   NULL    
        /// </summary>
        [NonSerialized]
        private Reference<T> discovered;  /* used by VM */

        /// <summary>
        /// Object used to synchronize with the garbage collector.  The collector
        /// must acquire this lock at the beginning of each collection cycle.  It is
        /// therefore critical that any code holding this lock complete as quickly
        /// as possible, allocate no new objects, and avoid calling user code.
        /// </summary>
        private class Lock { };
        private static Lock @lock = new Lock();

        /// <summary>
        /// List of References waiting to be enqueued.  The collector adds
        /// References to this list, while the Reference-handler thread removes
        /// them.  This list is protected by the above lock object. The
        /// list uses the discovered field to link its elements.
        /// </summary>
        private static Reference<T> pending = null;

        private class ReferenceHandler : ThreadClass
        {
            internal ReferenceHandler(/*ThreadStart g,*/ string name)
                : base(/*g,*/ name)
            {
            }

            public override void Run()
            {
                for (;;)
                {
                    Reference<T> r;

                    lock (@lock)
                    {
                        if (pending != null)
                        {
                            r = pending;
                            pending = r.discovered;
                            r.discovered = null;
                        }
                        else
                        {
                            // The waiting on the lock may cause an OOME because it may try to allocate
                            // exception objects, so also catch OOME here to avoid silent exit of the
                            // reference handler thread.
                            //
                            // Explicitly define the order of the two exceptions we catch here
                            // when waiting for the lock.
                            //
                            // We do not want to try to potentially load the InterruptedException class
                            // (which would be done if this was its first use, and InterruptedException
                            // were checked first) in this situation.
                            //
                            // This may lead to the VM not ever trying to load the InterruptedException
                            // class again.
                            try
                            {
                                try
                                {
                                    Monitor.Wait(@lock);
                                }
                                catch (OutOfMemoryException x) { }
                            }
                            catch (ThreadInterruptedException x) { }
                            continue;
                        }
                    }

                    //// Fast path for cleaners
                    //if (r is ICleaner)
                    //{
                    //    ((ICleaner)r).Clean();
                    //}

                    ReferenceQueue<T> q = r.queue;
                    if (q != ReferenceQueue<T>.NULL) q.Enqueue(r);
                }
            }
        }

        static Reference()
        {
            //ThreadStart tg = new ThreadStart(new object()); // LUCENENET TODO: somehow attach to current thread?

            //ThreadClass handler = new ReferenceHandler(tg, "Reference Handler");
            ThreadClass handler = new ReferenceHandler("Reference Handler");
            // If there were a special system-only priority greater than
            // MAX_PRIORITY, it would be used here
            handler.Priority = ThreadPriority.Highest;
            handler.SetDaemon(true);
            handler.Start();
        }

        //  -- Referent accessor and setters --

        /// <summary>
        /// Returns this reference object's referent.  If this reference object has
        /// been cleared, either by the program or by the garbage collector, then
        /// this method returns <c>null</c>.
        /// </summary>
        /// <returns>The object to which this reference refers, or
        /// <c>null</c> if this reference object has been cleared</returns>
        public T Get()
        {
            return this.referent;
        }

        /// <summary>
        /// Clears this reference object.  Invoking this method will not cause this
        /// object to be enqueued.
        /// <para/>
        /// This method is invoked only by internal code; when the garbage collector
        /// clears references it does so directly, without invoking this method.
        /// </summary>
        public void Clear()
        {
            this.referent = null;
        }

        //  -- Queue operations --

        /// <summary>
        /// Tells whether or not this reference object has been enqueued, either by
        /// the program or by the garbage collector.  If this reference object was
        /// not registered with a queue when it was created, then this method will
        /// always return <c>false</c>.
        /// </summary>
        public bool IsEnqueued
        {
            get { return (this.queue == ReferenceQueue<T>.ENQUEUED); }
        }

        /// <summary>
        /// Adds this reference object to the queue with which it is registered,
        /// if any.
        /// <para/>
        /// This method is invoked only by internal code; when the garbage collector
        /// enqueues references it does so directly, without invoking this method.
        /// </summary>
        /// <returns><c>true</c> if this reference object was successfully
        /// enqueued; <c>false</c> if it was already enqueued or if
        /// it was not registered with a queue when it was created</returns>
        public bool Enqueue()
        {
            return this.queue.Enqueue(this);
        }

        // -- Constructors --

        internal Reference(T referent)
            : this(referent, null)
        {
        }

        internal Reference(T referent, ReferenceQueue<T> queue)
        {
            this.referent = referent;
            this.queue = (queue == null) ? ReferenceQueue<T>.NULL : queue;
        }
    }
}

using System;
using System.Diagnostics;
using System.Threading;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Reference queues, to which registered reference objects are appended by the
    /// garbage collector after the appropriate reachability changes are detected.
    /// <para/>
    /// @author   Mark Reinhold
    /// @since    1.2
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ReferenceQueue<T> where T : class
    {
        /// <summary>
        /// Constructs a new reference-object queue.
        /// </summary>
        public ReferenceQueue() { }

        private class Null<S> : ReferenceQueue<S> where S : class
        {
            internal bool Enqueue(ReferenceQueue<S> r)
            {
                return false;
            }
        }

        public static ReferenceQueue<T> NULL = new Null<T>();
        public static ReferenceQueue<T> ENQUEUED = new Null<T>();

        private class Lock { };
        private Lock @lock = new Lock();
        private volatile Reference<T> head = null;
        private long queueLength = 0;

        internal bool Enqueue(Reference<T> r) // Called only by Reference class
        {
            lock (@lock)
            {
                // Check that since getting the lock this reference hasn't already been
                // enqueued (and even then removed)
                ReferenceQueue<T> queue = r.queue;
                if ((queue == NULL) || (queue == ENQUEUED))
                {
                    return false;
                }
                Debug.Assert(queue == this);
                r.queue = ENQUEUED;
                r.next = (head == null) ? r : head;
                head = r;
                queueLength++;
                // LUCENENET TODO:
                //if (r is FinalReference) {
                //    sun.misc.VM.addFinalRefCount(1);
                //}
                //lock.notifyAll();
                Monitor.PulseAll(@lock);
                return true;
            }
        }

        private Reference<T> ReallyPoll() // Must hold lock
        {
            Reference<T> r = head;
            if (r != null)
            {
                head = (r.next == r) ?
                    null :
                    r.next; // Unchecked due to the next field having a raw type in Reference
                r.queue = NULL;
                r.next = r;
                queueLength--;
                // LUCENENET TODO:
                //if (r is FinalReference) {
                //    sun.misc.VM.addFinalRefCount(-1);
                //}
                return r;
            }
            return null;
        }

        public Reference<T> Poll()
        {
            if (head == null)
            {
                return null;
            }
            lock (@lock)
            {
                return ReallyPoll();
            }
        }

        public Reference<T> Remove(int timeout)
        {
            if (timeout < 0)
            {
                throw new ArgumentException("Negative timeout value");
            }
            lock (@lock)
            {
                Reference<T> r = ReallyPoll();
                if (r != null)
                {
                    return r;
                }
                for (;;)
                {
                    Monitor.Wait(@lock, timeout);
                    r = ReallyPoll();
                    if (r != null) return r;
                    if (timeout != 0) return null;
                }
            }
        }

        public Reference<T> Remove()
        {
            return Remove(0);
        }
    }

    //public class ReferenceQueue
    //{
    //    private ReferenceQueue() { } // Disallow creation

    //    public static 
    //}
}

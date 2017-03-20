namespace Lucene.Net.Support
{
    public class WeakReference<T> : Reference<T> where T : class
    {
        public WeakReference(T referent)
            : base(referent)
        {
        }

        public WeakReference(T referent, ReferenceQueue<T> q)
            : base(referent, q)
        {
        }
    }
}

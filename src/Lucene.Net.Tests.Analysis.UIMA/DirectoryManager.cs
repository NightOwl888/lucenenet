using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Analysis.Uima
{
    /// <summary>
    /// LUCENENET specific utility for ensuring the 
    /// </summary>
    public class DirectoryManager
    {
        private readonly AtomicInt32 refCount = new AtomicInt32();
        private DirectoryInfo dir;
        private readonly object syncLock = new object();

        public void BeforeClass()
        {
            lock (syncLock)
            {
                refCount.IncrementAndGet();
#if NETSTANDARD1_5
            string currentPath = System.AppContext.BaseDirectory;
#else
                string currentPath = AppDomain.CurrentDomain.BaseDirectory;
#endif

                dir = new DirectoryInfo(System.IO.Path.Combine(currentPath, "uima"));

                if (!dir.Exists)
                {
                    dir.Create();

                    using (var stream = GetType().Assembly.FindAndGetManifestResourceStream(GetType(), "uima.zip"))
                    {
                        TestUtil.Unzip(stream, dir);
                    }
                }
            }
        }

        public void AfterClass()
        {
            lock (syncLock)
            {
                if (refCount.DecrementAndGet() <= 0)
                    dir.Delete(true);
            }
        }
    }
}

using Lucene.Net.Support;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using AssertionError = Lucene.Net.Diagnostics.AssertionException;
using Console = Lucene.Net.Support.SystemConsole;
using Debug = Lucene.Net.Diagnostics.Debug; // LUCENENET NOTE: We cannot use System.Diagnostics.Debug because those calls will be optimized out of the release!
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif

namespace Lucene.Net.Store
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

    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using NoDeletionPolicy = Lucene.Net.Index.NoDeletionPolicy;
    using SegmentInfos = Lucene.Net.Index.SegmentInfos;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using ThrottledIndexOutput = Lucene.Net.Util.ThrottledIndexOutput;

    /// <summary>
    /// Enum for controlling hard disk throttling.
    /// Set via <see cref="MockDirectoryWrapper.Throttling"/>
    /// <para/>
    /// WARNING: can make tests very slow.
    /// </summary>
    public enum Throttling
    {
        /// <summary>
        /// always emulate a slow hard disk. could be very slow! </summary>
        ALWAYS,

        /// <summary>
        /// sometimes (2% of the time) emulate a slow hard disk. </summary>
        SOMETIMES,

        /// <summary>
        /// never throttle output </summary>
        NEVER
    }

    /// <summary>
    /// This is a Directory Wrapper that adds methods
    /// intended to be used only by unit tests.
    /// It also adds a number of features useful for testing:
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///             Instances created by <see cref="LuceneTestCase.NewDirectory()"/> are tracked
    ///             to ensure they are disposed by the test.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             When a <see cref="MockDirectoryWrapper"/> is disposed, it will throw an exception if
    ///             it has any open files against it (with a stacktrace indicating where
    ///             they were opened from).
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             When a <see cref="MockDirectoryWrapper"/> is disposed, it runs <see cref="Index.CheckIndex"/> to test if
    ///             the index was corrupted.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             <see cref="MockDirectoryWrapper"/> simulates some "features" of Windows, such as
    ///             refusing to write/delete to open files.
    ///         </description>
    ///     </item>
    /// </list>
    /// </summary>
    public class MockDirectoryWrapper : BaseDirectoryWrapper
    {
        internal long maxSize;

        // Max actual bytes used. this is set by MockRAMOutputStream:
        internal long maxUsedSize;

        internal double randomIOExceptionRate;
        internal double randomIOExceptionRateOnOpen;
        internal Random randomState;
        internal bool noDeleteOpenFile = true;
        internal bool assertNoDeleteOpenFile = false;
        internal bool preventDoubleWrite = true;
        internal bool trackDiskUsage = false;
        internal bool wrapLockFactory = true;
        internal bool allowRandomFileNotFoundException = true;
        internal bool allowReadingFilesStillOpenForWrite = false;
        private ISet<string> unSyncedFiles;
        private ISet<string> createdFiles;
        private ISet<string> openFilesForWrite = new HashSet<string>();
        internal ISet<string> openLocks = new ConcurrentHashSet<string>();
        internal volatile bool crashed;
        private ThrottledIndexOutput throttledOutput;
        private Throttling throttling = Throttling.SOMETIMES;
        protected LockFactory m_lockFactory;

        internal readonly AtomicInt64 inputCloneCount = new AtomicInt64();

        // use this for tracking files for crash.
        // additionally: provides debugging information in case you leave one open
        private readonly ConcurrentDictionary<IDisposable, Exception> openFileHandles = new ConcurrentDictionary<IDisposable, Exception>(new IdentityComparer<IDisposable>());

        // NOTE: we cannot initialize the Map here due to the
        // order in which our constructor actually does this
        // member initialization vs when it calls super.  It seems
        // like super is called, then our members are initialized:
        private IDictionary<string, int> openFiles;

        // Only tracked if noDeleteOpenFile is true: if an attempt
        // is made to delete an open file, we enroll it here.
        private ISet<string> openFilesDeleted;

        private void Init()
        {
            lock (this)
            {
                if (openFiles == null)
                {
                    openFiles = new Dictionary<string, int>();
                    openFilesDeleted = new HashSet<string>();
                }

                if (createdFiles == null)
                {
                    createdFiles = new HashSet<string>();
                }
                if (unSyncedFiles == null)
                {
                    unSyncedFiles = new HashSet<string>();
                }
            }
        }

        public MockDirectoryWrapper(Random random, Directory @delegate)
            : base(@delegate)
        {
            // must make a private random since our methods are
            // called from different threads; else test failures may
            // not be reproducible from the original seed
            this.randomState = new Random(random.Next());
            this.throttledOutput = new ThrottledIndexOutput(ThrottledIndexOutput.MBitsToBytes(40 + randomState.Next(10)), 5 + randomState.Next(5), null);
            // force wrapping of lockfactory
            this.m_lockFactory = new MockLockFactoryWrapper(this, @delegate.LockFactory);
            Init();
        }

        public virtual int InputCloneCount
        {
            get
            {
                return (int)inputCloneCount.Get();
            }
        }

        public virtual bool TrackDiskUsage
        {
            get // LUCENENET specific - added getter (to follow MSDN property guidelines)
            {
                return trackDiskUsage;
            }
            set
            {
                trackDiskUsage = value;
            }
        }

        /// <summary>
        /// If set to true, we throw an <see cref="System.IO.IOException"/> if the same
        /// file is opened by <see cref="CreateOutput(string, IOContext)"/>, ever.
        /// </summary>
        public virtual bool PreventDoubleWrite
        {
            get // LUCENENET specific - added getter (to follow MSDN property guidelines)
            {
                return preventDoubleWrite;
            }
            set
            {
                preventDoubleWrite = value;
            }
        }

        /// <summary>
        /// If set to true (the default), when we throw random
        /// <see cref="System.IO.IOException"/> on <see cref="OpenInput(string, IOContext)"/> or 
        /// <see cref="CreateOutput(string, IOContext)"/>, we may
        /// sometimes throw <see cref="FileNotFoundException"/>.
        /// </summary>
        public virtual bool AllowRandomFileNotFoundException
        {
            get // LUCENENET specific - added getter (to follow MSDN property guidelines)
            {
                return allowRandomFileNotFoundException;
            }
            set
            {
                allowRandomFileNotFoundException = value;
            }
        }

        /// <summary>
        /// If set to true, you can open an inputstream on a file
        /// that is still open for writes.
        /// </summary>
        public virtual bool AllowReadingFilesStillOpenForWrite
        {
            get // LUCENENET specific - added getter (to follow MSDN property guidelines)
            {
                return allowRandomFileNotFoundException;
            }
            set
            {
                allowReadingFilesStillOpenForWrite = value;
            }
        }

        // LUCENENET specific - de-nested Throttling enum

        public virtual Throttling Throttling
        {
            get // LUCENENET specific - added getter (to follow MSDN property guidelines)
            {
                return this.throttling;
            }
            set
            {
                this.throttling = value;
            }
        }

        /// <summary>
        /// Returns true if <see cref="m_input"/> must sync its files.
        /// Currently, only <see cref="NRTCachingDirectory"/> requires sync'ing its files
        /// because otherwise they are cached in an internal <see cref="RAMDirectory"/>. If
        /// other directories require that too, they should be added to this method.
        /// </summary>
        private bool MustSync()
        {
            Directory @delegate = m_input;
            while (@delegate is FilterDirectory)
            {
                @delegate = ((FilterDirectory)@delegate).Delegate;
            }
            return @delegate is NRTCachingDirectory;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Sync(ICollection<string> names)
        {
            lock (this)
            {
                MaybeYield();
                MaybeThrowDeterministicException();
                if (crashed)
                {
                    throw new System.IO.IOException("cannot sync after crash");
                }
                // don't wear out our hardware so much in tests.
                if (LuceneTestCase.Rarely(randomState) || MustSync())
                {
                    foreach (string name in names)
                    {
                        // randomly fail with IOE on any file
                        MaybeThrowIOException(name);
                        m_input.Sync(new[] { name });
                        unSyncedFiles.Remove(name);
                    }
                }
                else
                {
                    unSyncedFiles.RemoveAll(names);
                }
            }
        }

        public long GetSizeInBytes()
        {
            lock (this)
            {
                if (m_input is RAMDirectory)
                {
                    return ((RAMDirectory)m_input).GetSizeInBytes();
                }
                else
                {
                    // hack
                    long size = 0;
                    foreach (string file in m_input.ListAll())
                    {
                        size += m_input.FileLength(file);
                    }
                    return size;
                }
            }
        }

        /// <summary>
        /// Simulates a crash of OS or machine by overwriting
        /// unsynced files.
        /// </summary>
        public virtual void Crash()
        {
            lock (this)
            {
                crashed = true;
                openFiles = new Dictionary<string, int>();
                openFilesForWrite = new HashSet<string>();
                openFilesDeleted = new HashSet<string>();
                using (IEnumerator<string> it = unSyncedFiles.GetEnumerator())
                {
                    unSyncedFiles = new HashSet<string>();
                    // first force-close all files, so we can corrupt on windows etc.
                    // clone the file map, as these guys want to remove themselves on close.
                    var m = new IdentityHashMap<IDisposable, Exception>(openFileHandles);
                    foreach (IDisposable f in m.Keys)
                    {
                        try
                        {
                            f.Dispose();
                        }
#pragma warning disable 168
                        catch (Exception ignored)
#pragma warning restore 168
                        {
                            //Debug.WriteLine("Crash(): f.Dispose() FAILED for {0}:\n{1}", f.ToString(), ignored.ToString());
                        }
                    }

                    while (it.MoveNext())
                    {
                        string name = it.Current;
                        int damage = randomState.Next(5);
                        string action = null;

                        if (damage == 0)
                        {
                            action = "deleted";
                            DeleteFile(name, true);
                        }
                        else if (damage == 1)
                        {
                            action = "zeroed";
                            // Zero out file entirely
                            long length = FileLength(name);
                            var zeroes = new byte[256];
                            long upto = 0;
                            using (IndexOutput @out = m_input.CreateOutput(name, LuceneTestCase.NewIOContext(randomState)))
                            {
                                while (upto < length)
                                {
                                    var limit = (int)Math.Min(length - upto, zeroes.Length);
                                    @out.WriteBytes(zeroes, 0, limit);
                                    upto += limit;
                                }
                            }
                        }
                        else if (damage == 2)
                        {
                            action = "partially truncated";
                            // Partially Truncate the file:

                            // First, make temp file and copy only half this
                            // file over:
                            string tempFileName;
                            while (true)
                            {
                                tempFileName = "" + randomState.Next();
                                if (!LuceneTestCase.SlowFileExists(m_input, tempFileName))
                                {
                                    break;
                                }
                            }
                            using (IndexOutput tempOut = m_input.CreateOutput(tempFileName, LuceneTestCase.NewIOContext(randomState)))
                            {
                                using (IndexInput ii = m_input.OpenInput(name, LuceneTestCase.NewIOContext(randomState)))
                                {
                                    tempOut.CopyBytes(ii, ii.Length / 2);
                                }
                            }

                            // Delete original and copy bytes back:
                            DeleteFile(name, true);

                            using (IndexOutput @out = m_input.CreateOutput(name, LuceneTestCase.NewIOContext(randomState)))
                            {
                                using (IndexInput ii = m_input.OpenInput(tempFileName, LuceneTestCase.NewIOContext(randomState)))
                                {
                                    @out.CopyBytes(ii, ii.Length);
                                }
                            }
                            DeleteFile(tempFileName, true);
                        }
                        else if (damage == 3)
                        {
                            // The file survived intact:
                            action = "didn't change";
                        }
                        else
                        {
                            action = "fully truncated";
                            // Totally truncate the file to zero bytes
                            DeleteFile(name, true);
                            using (IndexOutput @out = m_input.CreateOutput(name, LuceneTestCase.NewIOContext(randomState)))
                            {
                                @out.Length = 0;
                            }
                        }
                        if (LuceneTestCase.VERBOSE)
                        {
                            Console.WriteLine("MockDirectoryWrapper: " + action + " unsynced file: " + name);
                        }
                    }
                }
            }
        }

        public virtual void ClearCrash()
        {
            lock (this)
            {
                crashed = false;
                openLocks.Clear();
            }
        }

        public virtual long MaxSizeInBytes
        {
            set
            {
                this.maxSize = value;
            }
            get
            {
                return this.maxSize;
            }
        }

        /// <summary>
        /// Returns the peek actual storage used (bytes) in this
        /// directory.
        /// </summary>
        public virtual long MaxUsedSizeInBytes
        {
            get
            {
                return this.maxUsedSize;
            }
        }

        public virtual void ResetMaxUsedSizeInBytes()
        {
            this.maxUsedSize = GetRecomputedActualSizeInBytes();
        }

        /// <summary>
        /// Emulate Windows whereby deleting an open file is not
        /// allowed (raise <see cref="IOException"/>).
        /// </summary>
        public virtual bool NoDeleteOpenFile
        {
            set
            {
                this.noDeleteOpenFile = value;
            }
            get
            {
                return noDeleteOpenFile;
            }
        }

        /// <summary>
        /// Trip a test assert if there is an attempt
        /// to delete an open file.
        /// </summary>
        public virtual bool AssertNoDeleteOpenFile
        {
            set
            {
                this.assertNoDeleteOpenFile = value;
            }
            get
            {
                return assertNoDeleteOpenFile;
            }
        }

        /// <summary>
        /// If 0.0, no exceptions will be thrown.  Else this should
        /// be a double 0.0 - 1.0.  We will randomly throw an
        /// <see cref="IOException"/> on the first write to a <see cref="Stream"/> based
        /// on this probability.
        /// </summary>
        public virtual double RandomIOExceptionRate
        {
            set
            {
                randomIOExceptionRate = value;
            }
            get
            {
                return randomIOExceptionRate;
            }
        }

        /// <summary>
        /// If 0.0, no exceptions will be thrown during <see cref="OpenInput(string, IOContext)"/>
        /// and <see cref="CreateOutput(string, IOContext)"/>.  Else this should
        /// be a double 0.0 - 1.0 and we will randomly throw an
        /// <see cref="IOException"/> in <see cref="OpenInput(string, IOContext)"/> and <see cref="CreateOutput(string, IOContext)"/> with
        /// this probability.
        /// </summary>
        public virtual double RandomIOExceptionRateOnOpen
        {
            set
            {
                randomIOExceptionRateOnOpen = value;
            }
            get
            {
                return randomIOExceptionRateOnOpen;
            }
        }

        internal virtual void MaybeThrowIOException(string message)
        {
            if (randomState.NextDouble() < randomIOExceptionRate)
            {
                if (LuceneTestCase.VERBOSE)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": MockDirectoryWrapper: now throw random exception" + (message == null ? "" : " (" + message + ")"));
                    //(new Exception()).printStackTrace(System.out);
                }
                throw new System.IO.IOException("a randomSystem.IO.IOException" + (message == null ? "" : " (" + message + ")"));
            }
        }

        internal virtual void MaybeThrowIOExceptionOnOpen(string name)
        {
            if (randomState.NextDouble() < randomIOExceptionRateOnOpen)
            {
                if (LuceneTestCase.VERBOSE)
                {
                  Console.WriteLine(Thread.CurrentThread.Name + ": MockDirectoryWrapper: now throw random exception during open file=" + name);
                  //(new Exception()).printStackTrace(System.out);
                }
                if (allowRandomFileNotFoundException == false || randomState.NextBoolean())
                {
                    throw new System.IO.IOException("a randomSystem.IO.IOException (" + name + ")");
                }
                else
                {
                    throw randomState.NextBoolean() ? (IOException)new FileNotFoundException("a randomSystem.IO.IOException (" + name + ")") : new DirectoryNotFoundException("a randomSystem.IO.IOException (" + name + ")");
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void DeleteFile(string name)
        {
            lock (this)
            {
                MaybeYield();
                DeleteFile(name, false);
            }
        }

        // if there are any exceptions in OpenFileHandles
        // capture those as inner exceptions
        private Exception WithAdditionalErrorInformation(Exception t, string name, bool input)
        {
            lock (this)
            {
                foreach (var ent in openFileHandles)
                {
                    if (input && ent.Key is MockIndexInputWrapper && ((MockIndexInputWrapper)ent.Key).name.Equals(name, StringComparison.Ordinal))
                    {
                        t = CreateException(t, ent.Value);
                        break;
                    }
                    else if (!input && ent.Key is MockIndexOutputWrapper && ((MockIndexOutputWrapper)ent.Key).name.Equals(name, StringComparison.Ordinal))
                    {
                        t = CreateException(t, ent.Value);
                        break;
                    }
                }
                return t;
            }
        }

        private Exception CreateException(Exception exception, Exception innerException)
        {
            return (Exception)Activator.CreateInstance(exception.GetType(), exception.Message, innerException);
        }

        private void MaybeYield()
        {
            if (randomState.NextBoolean())
            {
#if NETSTANDARD1_6
                Thread.Sleep(0);
#else
                Thread.Yield();
#endif
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void DeleteFile(string name, bool forced)
        {
            lock (this)
            {
                MaybeYield();

                MaybeThrowDeterministicException();

                if (crashed && !forced)
                {
                    throw new System.IO.IOException("cannot delete after crash");
                }

                if (unSyncedFiles.Contains(name))
                {
                    unSyncedFiles.Remove(name);
                }
                if (!forced && (noDeleteOpenFile || assertNoDeleteOpenFile))
                {
                    if (openFiles.ContainsKey(name))
                    {
                        openFilesDeleted.Add(name);

                        if (!assertNoDeleteOpenFile)
                        {
                            throw WithAdditionalErrorInformation(new IOException("MockDirectoryWrapper: file \"" + name + "\" is still open: cannot delete"), name, true);
                        }
                        else
                        {
                            throw WithAdditionalErrorInformation(new AssertionError("MockDirectoryWrapper: file \"" + name + "\" is still open: cannot delete"), name, true);
                        }
                    }
                    else
                    {
                        openFilesDeleted.Remove(name);
                    }
                }
                m_input.DeleteFile(name);
            }
        }

        public virtual ICollection<string> GetOpenDeletedFiles()
        {
            lock (this)
            {
                return new HashSet<string>(openFilesDeleted);
            }
        }

        private bool failOnCreateOutput = true;

        public virtual bool FailOnCreateOutput
        {
            get // LUCENENET specific - added getter (to follow MSDN property guidelines)
            {
                return failOnCreateOutput;
            }
            set
            {
                failOnCreateOutput = value;
            }
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            lock (this)
            {
                MaybeThrowDeterministicException();
                MaybeThrowIOExceptionOnOpen(name);
                MaybeYield();
                if (failOnCreateOutput)
                {
                    MaybeThrowDeterministicException();
                }
                if (crashed)
                {
                    throw new System.IO.IOException("cannot createOutput after crash");
                }
                Init();
                lock (this)
                {
                    if (preventDoubleWrite && createdFiles.Contains(name) && !name.Equals("segments.gen", StringComparison.Ordinal))
                    {
                        throw new System.IO.IOException("file \"" + name + "\" was already written to");
                    }
                }
                if ((noDeleteOpenFile || assertNoDeleteOpenFile) && openFiles.ContainsKey(name))
                {
                    if (!assertNoDeleteOpenFile)
                    {
                        throw new System.IO.IOException("MockDirectoryWrapper: file \"" + name + "\" is still open: cannot overwrite");
                    }
                    else
                    {
                        throw new InvalidOperationException("MockDirectoryWrapper: file \"" + name + "\" is still open: cannot overwrite");
                    }
                }

                if (crashed)
                {
                    throw new System.IO.IOException("cannot createOutput after crash");
                }
                unSyncedFiles.Add(name);
                createdFiles.Add(name);

                if (m_input is RAMDirectory)
                {
                    RAMDirectory ramdir = (RAMDirectory)m_input;
                    RAMFile file = new RAMFile(ramdir);
                    RAMFile existing = ramdir.m_fileMap.ContainsKey(name) ? ramdir.m_fileMap[name] : null;

                    // Enforce write once:
                    if (existing != null && !name.Equals("segments.gen", StringComparison.Ordinal) && preventDoubleWrite)
                    {
                        throw new System.IO.IOException("file " + name + " already exists");
                    }
                    else
                    {
                        if (existing != null)
                        {
                            ramdir.m_sizeInBytes.AddAndGet(-existing.GetSizeInBytes());
                            existing.directory = null;
                        }
                        ramdir.m_fileMap.Put(name, file);
                    }
                }
                //System.out.println(Thread.currentThread().getName() + ": MDW: create " + name);
                IndexOutput delegateOutput = m_input.CreateOutput(name, LuceneTestCase.NewIOContext(randomState, context));
                if (randomState.Next(10) == 0)
                {
                    // once in a while wrap the IO in a Buffered IO with random buffer sizes
                    delegateOutput = new BufferedIndexOutputWrapper(this, 1 + randomState.Next(BufferedIndexOutput.DEFAULT_BUFFER_SIZE), delegateOutput);
                }
                IndexOutput io = new MockIndexOutputWrapper(this, delegateOutput, name);
                AddFileHandle(io, name, Handle.Output);
                openFilesForWrite.Add(name);

                // throttling REALLY slows down tests, so don't do it very often for SOMETIMES.
                if (throttling == Throttling.ALWAYS || (throttling == Throttling.SOMETIMES && randomState.Next(50) == 0) && !(m_input is RateLimitedDirectoryWrapper))
                {
                    if (LuceneTestCase.VERBOSE)
                    {
                        Console.WriteLine("MockDirectoryWrapper: throttling indexOutput (" + name + ")");
                    }
                    return throttledOutput.NewFromDelegate(io);
                }
                else
                {
                    return io;
                }
            }
        }

        internal enum Handle
        {
            Input,
            Output,
            Slice
        }

        internal void AddFileHandle(IDisposable c, string name, Handle handle)
        {
            //Trace.TraceInformation("Add {0} {1}", c, name);

            lock (this)
            {
                int v;
                if (openFiles.TryGetValue(name, out v))
                {
                    v++;
                    //Debug.WriteLine("Add {0} - {1} - {2}", c, name, v);
                    openFiles[name] = v;
                }
                else
                {
                    //Debug.WriteLine("Add {0} - {1} - {2}", c, name, 1);
                    openFiles[name] = 1;
                }

                openFileHandles[c] = new Exception("unclosed Index" + handle.ToString() + ": " + name);
            }
        }

        private bool failOnOpenInput = true;

        public virtual bool FailOnOpenInput
        {
            get // LUCENENET specific - added getter (to follow MSDN property guidelines)
            {
                return FailOnOpenInput;
            }
            set
            {
                failOnOpenInput = value;
            }
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            lock (this)
            {
                MaybeThrowDeterministicException();
                MaybeThrowIOExceptionOnOpen(name);
                MaybeYield();
                if (failOnOpenInput)
                {
                    MaybeThrowDeterministicException();
                }
                if (!LuceneTestCase.SlowFileExists(m_input, name))
                {
                    throw new FileNotFoundException(name + " in dir=" + m_input);
                }

                // cannot open a file for input if it's still open for
                // output, except for segments.gen and segments_N
                if (!allowReadingFilesStillOpenForWrite && openFilesForWrite.Contains(name, StringComparer.Ordinal) && !name.StartsWith("segments", StringComparison.Ordinal))
                {
                    throw WithAdditionalErrorInformation(new IOException("MockDirectoryWrapper: file \"" + name + "\" is still open for writing"), name, false);
                }

                IndexInput delegateInput = m_input.OpenInput(name, LuceneTestCase.NewIOContext(randomState, context));

                IndexInput ii;
                int randomInt = randomState.Next(500);
                if (randomInt == 0)
                {
                    if (LuceneTestCase.VERBOSE)
                    {
                        Console.WriteLine("MockDirectoryWrapper: using SlowClosingMockIndexInputWrapper for file " + name);
                    }
                    ii = new SlowClosingMockIndexInputWrapper(this, name, delegateInput);
                }
                else if (randomInt == 1)
                {
                    if (LuceneTestCase.VERBOSE)
                    {
                        Console.WriteLine("MockDirectoryWrapper: using SlowOpeningMockIndexInputWrapper for file " + name);
                    }
                    ii = new SlowOpeningMockIndexInputWrapper(this, name, delegateInput);
                }
                else
                {
                    ii = new MockIndexInputWrapper(this, name, delegateInput);
                }
                AddFileHandle(ii, name, Handle.Input);
                return ii;
            }
        }

        /// <summary>
        /// Provided for testing purposes.  Use <see cref="GetSizeInBytes()"/> instead. </summary>
        public long GetRecomputedSizeInBytes()
        {
            lock (this)
            {
                if (!(m_input is RAMDirectory))
                {
                    return GetSizeInBytes();
                }
                long size = 0;
                foreach (RAMFile file in ((RAMDirectory)m_input).m_fileMap.Values)
                {
                    size += file.GetSizeInBytes();
                }
                return size;
            }
        }

        /// <summary>
        /// Like <see cref="GetRecomputedSizeInBytes()"/>, but, uses actual file
        /// lengths rather than buffer allocations (which are
        /// quantized up to nearest
        /// <see cref="RAMOutputStream.BUFFER_SIZE"/> (now 1024) bytes.
        /// </summary>

        public long GetRecomputedActualSizeInBytes()
        {
            lock (this)
            {
                if (!(m_input is RAMDirectory))
                {
                    return GetSizeInBytes();
                }
                long size = 0;
                foreach (RAMFile file in ((RAMDirectory)m_input).m_fileMap.Values)
                {
                    size += file.Length;
                }
                return size;
            }
        }

        // NOTE: this is off by default; see LUCENE-5574
        private bool assertNoUnreferencedFilesOnClose;

        public virtual bool AssertNoUnreferencedFilesOnClose // LUCENENET TODO: Rename AssertNoUnreferencedFilesOnDispose ?
        {
            get // LUCENENET specific - added getter (to follow MSDN property guidelines)
            {
                return assertNoUnreferencedFilesOnClose;
            }
            set
            {
                assertNoUnreferencedFilesOnClose = value;
            }
        }

        /// <summary>
        /// Set to false if you want to return the pure lockfactory
        /// and not wrap it with <see cref="MockLockFactoryWrapper"/>.
        /// <para/>
        /// Be careful if you turn this off: <see cref="MockDirectoryWrapper"/> might
        /// no longer be able to detect if you forget to close an <see cref="IndexWriter"/>,
        /// and spit out horribly scary confusing exceptions instead of
        /// simply telling you that.
        /// </summary>
        public virtual bool WrapLockFactory
        {
            get // LUCENENET specific - added getter (to follow MSDN property guidelines)
            {
                return wrapLockFactory;
            }
            set
            {
                this.wrapLockFactory = value;
            }
        }

        protected override void Dispose(bool disposing)
        {
            lock (this)
            {
                if (disposing)
                {
                    // files that we tried to delete, but couldn't because readers were open.
                    // all that matters is that we tried! (they will eventually go away)
                    ISet<string> pendingDeletions = new HashSet<string>(openFilesDeleted);
                    MaybeYield();
                    if (openFiles == null)
                    {
                        openFiles = new Dictionary<string, int>();
                        openFilesDeleted = new HashSet<string>();
                    }
                    if (openFiles.Count > 0)
                    {
                        // print the first one as its very verbose otherwise
                        Exception cause = openFileHandles.Values.FirstOrDefault();

                        // RuntimeException instead ofSystem.IO.IOException because
                        // super() does not throwSystem.IO.IOException currently:
                        throw new Exception("MockDirectoryWrapper: cannot close: there are still open files: "
                            + string.Join(" ,", openFiles.ToArray().Select(x => x.Key)), cause);
                    }
                    if (openLocks.Count > 0)
                    {
                        throw new Exception("MockDirectoryWrapper: cannot close: there are still open locks: "
                            + string.Join(" ,", openLocks.ToArray()));
                    }

                    IsOpen = false;
                    if (CheckIndexOnDispose)
                    {
                        randomIOExceptionRate = 0.0;
                        randomIOExceptionRateOnOpen = 0.0;
                        if (DirectoryReader.IndexExists(this))
                        {
                            if (LuceneTestCase.VERBOSE)
                            {
                                Console.WriteLine("\nNOTE: MockDirectoryWrapper: now crush");
                            }
                            Crash(); // corrupt any unsynced-files
                            if (LuceneTestCase.VERBOSE)
                            {
                                Console.WriteLine("\nNOTE: MockDirectoryWrapper: now run CheckIndex");
                            }
                            TestUtil.CheckIndex(this, CrossCheckTermVectorsOnDispose);

                            // TODO: factor this out / share w/ TestIW.assertNoUnreferencedFiles
                            if (assertNoUnreferencedFilesOnClose)
                            {
                                // now look for unreferenced files: discount ones that we tried to delete but could not
                                HashSet<string> allFiles = new HashSet<string>(Arrays.AsList(ListAll()));
                                allFiles.RemoveAll(pendingDeletions);
                                string[] startFiles = allFiles.ToArray(/*new string[0]*/);
                                IndexWriterConfig iwc = new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, null);
                                iwc.SetIndexDeletionPolicy(NoDeletionPolicy.INSTANCE);
                                (new IndexWriter(m_input, iwc)).Rollback();
                                string[] endFiles = m_input.ListAll();

                                ISet<string> startSet = new SortedSet<string>(Arrays.AsList(startFiles), StringComparer.Ordinal);
                                ISet<string> endSet = new SortedSet<string>(Arrays.AsList(endFiles), StringComparer.Ordinal);

                                if (pendingDeletions.Contains("segments.gen") && endSet.Contains("segments.gen"))
                                {
                                    // this is possible if we hit an exception while writing segments.gen, we try to delete it
                                    // and it ends out in pendingDeletions (but IFD wont remove this).
                                    startSet.Add("segments.gen");
                                    if (LuceneTestCase.VERBOSE)
                                    {
                                        Console.WriteLine("MDW: Unreferenced check: Ignoring segments.gen that we could not delete.");
                                    }
                                }

                                // its possible we cannot delete the segments_N on windows if someone has it open and
                                // maybe other files too, depending on timing. normally someone on windows wouldnt have
                                // an issue (IFD would nuke this stuff eventually), but we pass NoDeletionPolicy...
                                foreach (string file in pendingDeletions)
                                {
                                    if (file.StartsWith("segments", StringComparison.Ordinal) && !file.Equals("segments.gen", StringComparison.Ordinal) && endSet.Contains(file, StringComparer.Ordinal))
                                    {
                                        startSet.Add(file);
                                        if (LuceneTestCase.VERBOSE)
                                        {
                                            Console.WriteLine("MDW: Unreferenced check: Ignoring segments file: " + file + " that we could not delete.");
                                        }
                                        SegmentInfos sis = new SegmentInfos();
                                        try
                                        {
                                            sis.Read(m_input, file);
                                        }
#pragma warning disable 168
                                        catch (System.IO.IOException ioe)
#pragma warning restore 168
                                        {
                                            // OK: likely some of the .si files were deleted
                                        }

                                        try
                                        {
                                            ISet<string> ghosts = new HashSet<string>(sis.GetFiles(m_input, false));
                                            foreach (string s in ghosts)
                                            {
                                                if (endSet.Contains(s) && !startSet.Contains(s))
                                                {
                                                    Debug.Assert(pendingDeletions.Contains(s));
                                                    if (LuceneTestCase.VERBOSE)
                                                    {
                                                        Console.WriteLine("MDW: Unreferenced check: Ignoring referenced file: " + s + " " + "from " + file + " that we could not delete.");
                                                    }
                                                    startSet.Add(s);
                                                }
                                            }
                                        }
                                        catch (Exception t)
                                        {
                                            Console.Error.WriteLine("ERROR processing leftover segments file " + file + ":");
                                            Console.WriteLine(t.ToString());
                                            Console.Write(t.StackTrace);
                                        }
                                    }
                                }

                                startFiles = startSet.ToArray(/*new string[0]*/);
                                endFiles = endSet.ToArray(/*new string[0]*/);

                                if (!Arrays.Equals(startFiles, endFiles))
                                {
                                    IList<string> removed = new List<string>();
                                    foreach (string fileName in startFiles)
                                    {
                                        if (!endSet.Contains(fileName))
                                        {
                                            removed.Add(fileName);
                                        }
                                    }

                                    IList<string> added = new List<string>();
                                    foreach (string fileName in endFiles)
                                    {
                                        if (!startSet.Contains(fileName))
                                        {
                                            added.Add(fileName);
                                        }
                                    }

                                    string extras;
                                    if (removed.Count != 0)
                                    {
                                        extras = "\n\nThese files were removed: " + removed;
                                    }
                                    else
                                    {
                                        extras = "";
                                    }

                                    if (added.Count != 0)
                                    {
                                        extras += "\n\nThese files were added (waaaaaaaaaat!): " + added;
                                    }

                                    if (pendingDeletions.Count != 0)
                                    {
                                        extras += "\n\nThese files we had previously tried to delete, but couldn't: " + pendingDeletions;
                                    }

                                    Debug.Assert(false, "unreferenced files: before delete:\n    " + Arrays.ToString(startFiles) + "\n  after delete:\n    " + Arrays.ToString(endFiles) + extras);
                                }

                                DirectoryReader ir1 = DirectoryReader.Open(this);
                                int numDocs1 = ir1.NumDocs;
                                ir1.Dispose();
                                (new IndexWriter(this, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, null))).Dispose();
                                DirectoryReader ir2 = DirectoryReader.Open(this);
                                int numDocs2 = ir2.NumDocs;
                                ir2.Dispose();
                                Debug.Assert(numDocs1 == numDocs2, "numDocs changed after opening/closing IW: before=" + numDocs1 + " after=" + numDocs2);
                            }
                        }
                    }
                    m_input.Dispose(); // LUCENENET TODO: using blocks in this entire class
                }
            }
        }

        internal virtual void RemoveOpenFile(IDisposable c, string name)
        {
            //Trace.TraceInformation("Rem {0} {1}", c, name);

            lock (this)
            {
                int v;
                if (openFiles.TryGetValue(name, out v))
                {
                    if (v == 1)
                    {
                        //Debug.WriteLine("RemoveOpenFile OpenFiles.Remove {0} - {1}", c, name);
                        openFiles.Remove(name);
                    }
                    else
                    {
                        v--;
                        openFiles[name] = v;
                        //Debug.WriteLine("RemoveOpenFile OpenFiles DECREMENT {0} - {1} - {2}", c, name, v);
                    }
                }

                Exception _;
                openFileHandles.TryRemove(c, out _);
            }
        }

        public virtual void RemoveIndexOutput(IndexOutput @out, string name)
        {
            lock (this)
            {
                openFilesForWrite.Remove(name);
                RemoveOpenFile(@out, name);
            }
        }

        public virtual void RemoveIndexInput(IndexInput @in, string name)
        {
            lock (this)
            {
                RemoveOpenFile(@in, name);
            }
        }

        // LUCENENET specific - de-nested Failure

        internal List<Failure> failures;

        /// <summary>
        /// Add a <see cref="Failure"/> object to the list of objects to be evaluated
        /// at every potential failure point.
        /// </summary>
        public virtual void FailOn(Failure fail)
        {
            lock (this)
            {
                if (failures == null)
                {
                    failures = new List<Failure>();
                }
                failures.Add(fail);
            }
        }

        /// <summary>
        /// Iterate through the failures list, giving each object a
        /// chance to throw an <see cref="IOException"/>.
        /// </summary>
        internal virtual void MaybeThrowDeterministicException()
        {
            lock (this)
            {
                if (failures != null)
                {
                    for (int i = 0; i < failures.Count; i++)
                    {
                        failures[i].Eval(this);
                    }
                }
            }
        }

        public override string[] ListAll()
        {
            lock (this)
            {
                MaybeYield();
                return m_input.ListAll();
            }
        }

        [Obsolete("this method will be removed in 5.0")]
        public override bool FileExists(string name)
        {
            lock (this)
            {
                MaybeYield();
                return m_input.FileExists(name);
            }
        }

        public override long FileLength(string name)
        {
            lock (this)
            {
                MaybeYield();
                return m_input.FileLength(name);
            }
        }

        public override Lock MakeLock(string name)
        {
            lock (this)
            {
                MaybeYield();
                return LockFactory.MakeLock(name);
            }
        }

        public override void ClearLock(string name)
        {
            lock (this)
            {
                MaybeYield();
                LockFactory.ClearLock(name);
            }
        }

        public override void SetLockFactory(LockFactory lockFactory)
        {
            lock (this)
            {
                MaybeYield();
                // sneaky: we must pass the original this way to the dir, because
                // some impls (e.g. FSDir) do instanceof here.
                m_input.SetLockFactory(lockFactory);
                // now set our wrapped factory here
                this.m_lockFactory = new MockLockFactoryWrapper(this, lockFactory);
            }
        }

        public override LockFactory LockFactory
        {
            get
            {
                lock (this)
                {
                    MaybeYield();
                    if (wrapLockFactory)
                    {
                        return m_lockFactory;
                    }
                    else
                    {
                        return m_input.LockFactory;
                    }
                }
            }
        }

        public override string GetLockID()
        {
            lock (this)
            {
                MaybeYield();
                return m_input.GetLockID();
            }
        }

        public override void Copy(Directory to, string src, string dest, IOContext context)
        {
            lock (this)
            {
                MaybeYield();
                // randomize the IOContext here?
                m_input.Copy(to, src, dest, context);
            }
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            MaybeYield();
            if (!LuceneTestCase.SlowFileExists(m_input, name))
            {
                throw randomState.NextBoolean() ? (IOException)new FileNotFoundException(name) : new DirectoryNotFoundException(name);
            }
            // cannot open a file for input if it's still open for
            // output, except for segments.gen and segments_N

            if (openFilesForWrite.Contains(name) && !name.StartsWith("segments", StringComparison.Ordinal))
            {
                throw WithAdditionalErrorInformation(new IOException("MockDirectoryWrapper: file \"" + name + "\" is still open for writing"), name, false);
            }

            IndexInputSlicer delegateHandle = m_input.CreateSlicer(name, context);
            IndexInputSlicer handle = new IndexInputSlicerAnonymousInnerClassHelper(this, name, delegateHandle);
            AddFileHandle(handle, name, Handle.Slice);
            return handle;
        }

        private class IndexInputSlicerAnonymousInnerClassHelper : IndexInputSlicer
        {
            private readonly MockDirectoryWrapper outerInstance;

            private string name;
            private IndexInputSlicer delegateHandle;

            public IndexInputSlicerAnonymousInnerClassHelper(MockDirectoryWrapper outerInstance, string name, IndexInputSlicer delegateHandle)
            {
                this.outerInstance = outerInstance;
                this.name = name;
                this.delegateHandle = delegateHandle;
            }

            private int disposed = 0;

            protected override void Dispose(bool disposing)
            {
                if (0 == Interlocked.CompareExchange(ref this.disposed, 1, 0))
                {
                    if (disposing)
                    {
                        delegateHandle.Dispose();
                        outerInstance.RemoveOpenFile(this, name);
                    }
                }
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                outerInstance.MaybeYield();
                IndexInput ii = new MockIndexInputWrapper(outerInstance, name, delegateHandle.OpenSlice(sliceDescription, offset, length));
                outerInstance.AddFileHandle(ii, name, Handle.Input);
                return ii;
            }

            [Obsolete("Only for reading CFS files from 3.x indexes.")]
            public override IndexInput OpenFullSlice()
            {
                outerInstance.MaybeYield();
                IndexInput ii = new MockIndexInputWrapper(outerInstance, name, delegateHandle.OpenFullSlice());
                outerInstance.AddFileHandle(ii, name, Handle.Input);
                return ii;
            }
        }

        internal sealed class BufferedIndexOutputWrapper : BufferedIndexOutput
        {
            private readonly MockDirectoryWrapper outerInstance;

            private readonly IndexOutput io;

            public BufferedIndexOutputWrapper(MockDirectoryWrapper outerInstance, int bufferSize, IndexOutput io)
                : base(bufferSize)
            {
                this.outerInstance = outerInstance;
                this.io = io;
            }

            public override long Length
            {
                get
                {
                    return io.Length;
                }
            }

            protected internal override void FlushBuffer(byte[] b, int offset, int len)
            {
                io.WriteBytes(b, offset, len);
            }

            [Obsolete("(4.1) this method will be removed in Lucene 5.0")]
            public override void Seek(long pos)
            {
                Flush();
                io.Seek(pos);
            }

            public override void Flush()
            {
                try
                {
                    base.Flush();
                }
                finally
                {
                    io.Flush();
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    try
                    {
                        base.Dispose(disposing);
                    }
                    finally
                    {
                        io.Dispose();
                    }
                }
            }
        }

        // LUCENENET specific - de-nested FakeIOException
    }

    /// <summary>
    /// Objects that represent fail-able conditions. Objects of a derived
    /// class are created and registered with the mock directory. After
    /// register, each object will be invoked once for each first write
    /// of a file, giving the object a chance to throw an <see cref="IOException"/>.
    /// </summary>
    public class Failure
    {
        /// <summary>
        /// Eval is called on the first write of every new file.
        /// </summary>
        public virtual void Eval(MockDirectoryWrapper dir)
        {
        }

        /// <summary>
        /// Reset should set the state of the failure to its default
        /// (freshly constructed) state. Reset is convenient for tests
        /// that want to create one failure object and then reuse it in
        /// multiple cases. This, combined with the fact that <see cref="Failure"/>
        /// subclasses are often anonymous classes makes reset difficult to
        /// do otherwise.
        /// <para/>
        /// A typical example of use is
        /// <code>
        /// Failure failure = new Failure() { ... };
        /// ...
        /// mock.FailOn(failure.Reset())
        /// </code>
        /// </summary>
        public virtual Failure Reset()
        {
            return this;
        }

        protected internal bool m_doFail;

        public virtual void SetDoFail()
        {
            m_doFail = true;
        }

        public virtual void ClearDoFail()
        {
            m_doFail = false;
        }
    }

    /// <summary>
    /// Use this when throwing fake <see cref="IOException"/>,
    /// e.g. from <see cref="MockDirectoryWrapper.Failure"/>.
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    public class FakeIOException : IOException
    {
        public FakeIOException() { } // LUCENENET specific - added public constructor for serialization

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected FakeIOException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
using J2N.Runtime.CompilerServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

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
    /// .NET's built-in <see cref="ThreadLocal{T}"/> has a serious flaw:
    /// internally, it creates an array with an internal lattice structure
    /// which in turn causes the garbage collector to cause long blocking pauses
    /// when tearing the structure down. See
    /// <a href="https://ayende.com/blog/189761-A/production-postmortem-the-slow-slowdown-of-large-systems">
    /// https://ayende.com/blog/189761-A/production-postmortem-the-slow-slowdown-of-large-systems</a>
    /// for a more detailed explanation.
    /// <para/>
    /// This is a completely different problem than in Java which the ClosableThreadLocal&lt;T&gt; class is
    /// meant to solve, so <see cref="LightWeightThreadLocal{T}"/> is specific to Lucene.NET and can be used
    /// as a direct replacement for ClosableThreadLocal&lt;T&gt;.
    /// <para/>
    /// This class works around the issue by using an alternative approach than using <see cref="ThreadLocal{T}"/>.
    /// It keeps track of each thread's local and global state in order to later optimize disposal.
    /// A complete explanation can be found at 
    /// <a href="https://ayende.com/blog/189793-A/the-design-and-implementation-of-a-better-threadlocal-t">
    /// https://ayende.com/blog/189793-A/the-design-and-implementation-of-a-better-threadlocal-t</a>.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    /// <typeparam name="T">Specifies the type of data stored per-thread.</typeparam>
    public sealed class LightWeightThreadLocal<T> : IDisposable
    {
        [ThreadStatic]
        private static CurrentThreadState _state;
        private ConcurrentDictionary<CurrentThreadState, T> _values = new ConcurrentDictionary<CurrentThreadState, T>(IdentityEqualityComparer<CurrentThreadState>.Default);
        private readonly Func<T> _valueFactory;
        private readonly GlobalState _globalState = new GlobalState();
        private readonly object _valuesLock = new object();

        /// <summary>
        /// Initializes the <see cref="LightWeightThreadLocal{T}"/> instance.
        /// </summary>
        /// <remarks>
        /// The default value of <typeparamref name="T"/> is used to initialize
        /// the instance when <see cref="Value"/> is accessed for the first time.
        /// </remarks>
        public LightWeightThreadLocal()
        {
            _valueFactory = null;
        }

        /// <summary>
        /// Initializes the <see cref="LightWeightThreadLocal{T}"/> instance with the
        /// specified <paramref name="valueFactory"/> function.
        /// </summary>
        /// <param name="valueFactory">The <see cref="Func{T, TResult}"/> invoked to produce a
        /// lazily-initialized value when an attempt is made to retrieve <see cref="Value"/>
        /// without it having been previously initialized.</param>
        /// <exception cref="ArgumentNullException"><paramref name="valueFactory"/> is <c>null</c>.</exception>
        public LightWeightThreadLocal(Func<T> valueFactory)
        {
            _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
        }

        /// <summary>
        /// Gets a collection for all of the values currently stored by all of the threads that have accessed this instance.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The <see cref="LightWeightThreadLocal{T}"/> instance has been disposed.</exception>
        public ICollection<T> Values
        {
            get
            {
                if (_globalState.Disposed != 0)
                    throw new ObjectDisposedException(nameof(LightWeightThreadLocal<T>));

                return _values.Values;
            }
        }

        /// <summary>
        /// Gets whether Value is initialized on the current thread.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The <see cref="LightWeightThreadLocal{T}"/> instance has been disposed.</exception>
        public bool IsValueCreated
        {
            get
            {
                if (_globalState.Disposed != 0)
                    throw new ObjectDisposedException(nameof(LightWeightThreadLocal<T>));

                return _state != null && _values.ContainsKey(_state);
            }
        }

        /// <summary>
        /// Gets or sets the value of this instance for the current thread.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The <see cref="LightWeightThreadLocal{T}"/> instance has been disposed.</exception>
        /// <remarks>
        /// If this instance was not previously initialized for the current thread, accessing Value will attempt to
        /// initialize it. If an initialization function was supplied during the construction, that initialization
        /// will happen by invoking the function to retrieve the initial value for <see cref="Value"/>. Otherwise, the default
        /// value of <typeparamref name="T"/> will be used.
        /// </remarks>
        public T Value
        {
            get
            {
                if (_globalState.Disposed != 0)
                    throw new ObjectDisposedException(nameof(LightWeightThreadLocal<T>));

                (_state ??= new CurrentThreadState()).Register(this);
                return _values.GetOrAdd(_state, (key) =>
                {
                    return _valueFactory != null ? _valueFactory() : default;
                });
            }
            set
            {
                if (_globalState.Disposed != 0)
                    throw new ObjectDisposedException(nameof(LightWeightThreadLocal<T>));

                (_state ??= new CurrentThreadState()).Register(this);
                _values[_state] = value;
            }
        }

        /// <summary>
        /// Releases the resources used by this <see cref="LightWeightThreadLocal{T}"/> instance.
        /// </summary>
        public void Dispose()
        {
            var copy = _values;
            if (copy == null)
                return;

            copy = Interlocked.CompareExchange(ref _values, null, copy);
            if (copy == null)
                return;

            _globalState.Dispose();
            _values = null;

            while (copy.Count > 0)
            {
                foreach (var kvp in copy)
                {
                    if (copy.TryRemove(kvp.Key, out var item) &&
                        item is IDisposable d)
                    {
                        d.Dispose();
                    }
                }
            }
        }

        private sealed class CurrentThreadState
        {
            private readonly HashSet<WeakReferenceToLightWeightThreadLocal> _parents
                = new HashSet<WeakReferenceToLightWeightThreadLocal>();

            private readonly LocalState _localState = new LocalState();

            public void Register(LightWeightThreadLocal<T> parent)
            {
                parent._globalState.UsedThreads.TryAdd(_localState, null);
                _parents.Add(new WeakReferenceToLightWeightThreadLocal(parent));
                int parentsDisposed = _localState.ParentsDisposed;
                if (parentsDisposed > 0)
                {
                    RemoveDisposedParents();
                    Interlocked.Add(ref _localState.ParentsDisposed, -parentsDisposed);
                }
            }

            private void RemoveDisposedParents()
            {
                var toRemove = new List<WeakReferenceToLightWeightThreadLocal>();
                foreach (var local in _parents)
                {
                    if (local.TryGetTarget(out var target) == false || target._globalState.Disposed != 0)
                    {
                        toRemove.Add(local);
                    }
                }

                foreach (var remove in toRemove)
                {
                    _parents.Remove(remove);
                }

            }
            ~CurrentThreadState()
            {
                foreach (var parent in _parents)
                {
                    if (parent.TryGetTarget(out var liveParent) == false)
                        continue;
                    var copy = liveParent._values;
                    if (copy == null)
                        continue;
                    if (copy.TryRemove(this, out var value)
                        && value is IDisposable d)
                    {
                        d.Dispose();
                    }
                }
            }
        }

        private sealed class WeakReferenceToLightWeightThreadLocal : IEquatable<WeakReferenceToLightWeightThreadLocal>
        {
            private readonly WeakReference<LightWeightThreadLocal<T>> _weak;
            private readonly int _hashCode;

            public bool TryGetTarget(out LightWeightThreadLocal<T> target)
            {
                return _weak.TryGetTarget(out target);
            }

            public WeakReferenceToLightWeightThreadLocal(LightWeightThreadLocal<T> instance)
            {
                _hashCode = instance.GetHashCode();
                _weak = new WeakReference<LightWeightThreadLocal<T>>(instance);
            }

            public bool Equals(WeakReferenceToLightWeightThreadLocal other)
            {
                if (ReferenceEquals(null, other))
                    return false;
                if (ReferenceEquals(this, other))
                    return true;
                if (_hashCode != other._hashCode)
                    return false;
                if (_weak.TryGetTarget(out var x) == false ||
                    other._weak.TryGetTarget(out var y) == false)
                    return false;
                return ReferenceEquals(x, y);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() != GetType())
                    return false;
                return Equals((WeakReferenceToLightWeightThreadLocal)obj);
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }
        }

        private sealed class GlobalState
        {
            public int Disposed;
            public readonly ConcurrentDictionary<LocalState, object> UsedThreads
                = new ConcurrentDictionary<LocalState, object>(IdentityEqualityComparer<LocalState>.Default);

            public void Dispose()
            {
                Interlocked.Exchange(ref Disposed, 1);
                foreach (var localState in UsedThreads)
                {
                    Interlocked.Increment(ref localState.Key.ParentsDisposed);
                }
            }
        }

        private sealed class LocalState
        {
            public int ParentsDisposed;
        }
    }
}
using System;
using System.Threading;

namespace Lucene.Net.Support.Threading
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
    /// A drop-in replacement for <see cref="Monitor"/> that doesn't throw <see cref="ThreadInterruptedException"/>
    /// when entering locks, but defers the excepetion until a wait, or a pulse occurs. This is to mimic the behavior in Java,
    /// which does not throw when entering a lock.
    /// </summary>
    internal static class UninterruptableMonitor
    {
        [ThreadStatic]
        private static bool isInterrupted = false; 

        public static bool Enter(object obj)
        {
            // enter the lock and ignore any System.Threading.ThreadInterruptedException
            try
            {
                Monitor.Enter(obj);
            }
            catch (Exception ie) when (ie.IsInterruptedException())
            {
                RetryEnter(obj);
                isInterrupted = true;
            }
            return isInterrupted;
        }

        private static void RetryEnter(object obj)
        {
            // "clear" the interrupt state by ignoring System.Threading.ThreadInterruptedException
            // from Thread.Sleep()
            try
            {
                // LUCENENET TODO: After trying 5 or so times, we should randomly adjust
                // the timeout here to try to break out of any deadlock
                Thread.Sleep(0);
            }
            catch (Exception ie) when (ie.IsInterruptedException())
            {
                // ignore
            }
            try
            {
                Monitor.Enter(obj);
            }
            catch (Exception ie) when (ie.IsInterruptedException())
            {
                // try again until we succeed, since an interrupt could happen between the 2 executable lines above
                RetryEnter(obj);
            }
        }

        public static void Exit(object obj)
        {
            Monitor.Exit(obj);
        }

        public static bool IsEntered(object obj)
        {
            return Monitor.IsEntered(obj);
        }

        public static bool TryEnter(object obj)
        {
            return Monitor.TryEnter(obj);
        }

        public static void TryEnter(object obj, ref bool lockTaken)
        {
            Monitor.TryEnter(obj, ref lockTaken);
        }

        public static bool TryEnter(object obj, int millisecondsTimeout)
        {
            return Monitor.TryEnter(obj, millisecondsTimeout);
        }

        public static bool TryEnter(object obj, TimeSpan timeout)
        {
            return Monitor.TryEnter(obj, timeout);
        }

        public static void TryEnter(object obj, int millisecondsTimeout, ref bool lockTaken)
        {
            Monitor.TryEnter(obj, millisecondsTimeout, ref lockTaken);
        }

        public static void TryEnter(object obj, TimeSpan timeout, ref bool lockTaken)
        {
            Monitor.TryEnter(obj, timeout, ref lockTaken);
        }

        public static void Pulse(object obj)
        {
            RestoreInterrupt();
            Monitor.Pulse(obj);
        }

        public static void PulseAll(object obj)
        {
            RestoreInterrupt();
            Monitor.PulseAll(obj);
        }

        public static void Wait(object obj)
        {
            RestoreInterrupt();
            Monitor.Wait(obj);
        }

        public static void Wait(object obj, int millisecondsTimeout)
        {
            RestoreInterrupt();
            Monitor.Wait(obj, millisecondsTimeout);
        }

        public static void Wait(object obj, TimeSpan timeout)
        {
            RestoreInterrupt();
            Monitor.Wait(obj, timeout);
        }

        public static void Wait(object obj, int millisecondsTimeout, bool exitContext)
        {
            RestoreInterrupt();
            Monitor.Wait(obj, millisecondsTimeout, exitContext);
        }

        public static void Wait(object obj, TimeSpan timeout, bool exitContext)
        {
            RestoreInterrupt();
            Monitor.Wait(obj, timeout, exitContext);
        }

        // NOTE: we need to call this manually before calling Thread.Sleep()
        internal static void RestoreInterrupt()
        {
            if (isInterrupted)
            {
                isInterrupted = false;
                Thread.CurrentThread.Interrupt();
            }
        }
    }
}

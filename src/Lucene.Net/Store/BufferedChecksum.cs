using Lucene.Net.Support;
using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

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

    public class BufferedHashAlgorithm : HashAlgorithm
    {
        private readonly HashAlgorithm input;
        private readonly byte[] buffer;
        private int upto;

        /// <summary>
        /// Default buffer size: 256 </summary>
        public const int DEFAULT_BUFFERSIZE = 256;

        /// <summary>
        /// Initializes a new <see cref="BufferedHashAlgorithm"/> with <see cref="DEFAULT_BUFFERSIZE"/>.
        /// </summary>
        /// <param name="input">The <see cref="HashAlgorithm"/> to wrap.</param>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <c>null</c>.</exception>
        public BufferedHashAlgorithm(HashAlgorithm input)
            : this(input, DEFAULT_BUFFERSIZE)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="BufferedHashAlgorithm"/> with the specified <paramref name="bufferSize"/>.
        /// </summary>
        /// <param name="input">The <see cref="HashAlgorithm"/> to wrap.</param>
        /// <param name="bufferSize">The buffer size.</param>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferSize"/> is less than 0.</exception>
        public BufferedHashAlgorithm(HashAlgorithm input, int bufferSize)
        {
            this.input = input ?? throw new ArgumentNullException(nameof(input));

            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            this.buffer = new byte[bufferSize];
        }

        public override bool CanReuseTransform => input.CanReuseTransform;

        public override bool CanTransformMultipleBlocks => input.CanTransformMultipleBlocks;

        public override byte[] Hash => base.Hash;

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            throw new NotImplementedException();
        }

        protected override byte[] HashFinal()
        {
            throw new NotImplementedException();
        }

        public override void Initialize()
        {
            throw new NotImplementedException();
        }



        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Flush()
        {
            if (upto > 0)
            {
                input.ComputeHash(buffer, 0, upto);
            }
            upto = 0;
        }

        
    }

    /// <summary>
    /// Wraps another <see cref="IChecksum"/> with an internal buffer
    /// to speed up checksum calculations.
    /// </summary>
    public class BufferedChecksum : IChecksum
    {
        private readonly IChecksum @in;
        private readonly byte[] buffer;
        private int upto;

        /// <summary>
        /// Default buffer size: 256 </summary>
        public const int DEFAULT_BUFFERSIZE = 256;

        /// <summary>
        /// Create a new <see cref="BufferedChecksum"/> with <see cref="DEFAULT_BUFFERSIZE"/> </summary>
        public BufferedChecksum(IChecksum @in)
            : this(@in, DEFAULT_BUFFERSIZE)
        {
        }

        /// <summary>
        /// Create a new <see cref="BufferedChecksum"/> with the specified <paramref name="bufferSize"/> </summary>
        public BufferedChecksum(IChecksum @in, int bufferSize)
        {
            this.@in = @in;
            this.buffer = new byte[bufferSize];
        }

        public virtual void Update(int b)
        {
            if (upto == buffer.Length)
            {
                Flush();
            }
            buffer[upto++] = (byte)b;
        }

        // LUCENENET specific overload for updating a whole byte[] array
        public virtual void Update(byte[] b)
        {
            Update(b, 0, b.Length);
        }

        public virtual void Update(byte[] b, int off, int len)
        {
            if (len >= buffer.Length)
            {
                Flush();
                @in.Update(b, off, len);
            }
            else
            {
                if (upto + len > buffer.Length)
                {
                    Flush();
                }
                System.Buffer.BlockCopy(b, off, buffer, upto, len);
                upto += len;
            }
        }

        public virtual long Value
        {
            get
            {
                Flush();
                return @in.Value;
            }
        }

        public virtual void Reset()
        {
            upto = 0;
            @in.Reset();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Flush()
        {
            if (upto > 0)
            {
                @in.Update(buffer, 0, upto);
            }
            upto = 0;
        }
    }
}
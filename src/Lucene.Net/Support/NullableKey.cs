﻿namespace Lucene.Net.Support
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
    /// A struct that can be used as a key for an standard dictionary implementation in
    /// order to make it support nullable keys.
    /// </summary>
    /// <typeparam name="T">The type of key. This can be either a value type or a reference type.
    /// For the nullable feature to function, a value type should be specified as nullable.</typeparam>
    // Inspired by: https://stackoverflow.com/a/22261282
    public struct NullableKey<T>
    {
        [System.ComponentModel.DefaultValue(true)]
        private readonly bool isNull;// default property initializers are not supported for structs

        private NullableKey(T value, bool isNull)
            : this()
        {
            this.isNull = isNull;
            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NullableKey{T}"/> structure with the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value of this key.</param>
        public NullableKey(T value)
            : this(value, value == null)
        { }

        /// <summary>
        /// Gets the value of the current <see cref="NullableKey{T}"/> structure if it has been assigned a valid underlying value.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the current <see cref="NullableKey{T}"/> structure has a valid value of its underlying type.
        /// A value of <c>false</c> indicates it is <c>null</c>.
        /// </summary>
        public bool HasValue => !this.isNull;

        /// <summary>
        /// Implicitly converts a <see cref="NullableKey{T}"/> structure to its value, which may be <c>null</c>.
        /// </summary>
        /// <param name="nullableKey">The <see cref="NullableKey{T}"/> structure to convert.</param>
        public static implicit operator T(NullableKey<T> nullableKey)
        {
            return nullableKey.Value;
        }

        /// <summary>
        /// Implicitly converts a <typeparamref name="T"/> to a <see cref="NullableKey{T}"/> structure.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator NullableKey<T>(T value)
        {
            return new NullableKey<T>(value);
        }

        public static bool operator ==(NullableKey<T> left, NullableKey<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NullableKey<T> left, NullableKey<T> right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Returns the value of <c>Value.ToString()</c> unless it is <c>null</c>,
        /// in which case the return value will be "null".
        /// </summary>
        public override string ToString()
        {
            return (Value != null) ? Value.ToString() : "null";
        }

        /// <summary>
        /// Returns <c>true</c> if <see cref="Value"/> is equal to <paramref name="other"/>,
        /// including if both <see cref="Value"/> and <paramref name="other"/> are <c>null</c>.
        /// </summary>
        /// <param name="other">The value to compare.</param>
        /// <returns><c>true</c> if <see cref="Value"/> is equal to <paramref name="other"/>; otherwise <c>false</c>.</returns>
        public override bool Equals(object other)
        {
            if (other == null)
                return this.isNull;

            if (!(other is NullableKey<T>))
                return false;

            var no = (NullableKey<T>)other;

            if (this.isNull)
                return no.isNull;

            if (no.isNull)
                return false;

            return this.Value.Equals(no.Value);
        }

        /// <summary>
        /// Returns a hash code for the current <see cref="Value"/>. If the value is
        /// <c>null</c>, returns <see cref="int.MaxValue"/>.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (this.isNull)
                return int.MaxValue; // Less likely to collide than 0, faster than a computation

            return Value.GetHashCode();
        }
    }
}

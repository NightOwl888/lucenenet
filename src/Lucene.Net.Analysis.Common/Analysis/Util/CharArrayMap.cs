﻿// Lucene version compatibility level 4.8.1
using J2N;
using J2N.Globalization;
using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using JCG = J2N.Collections.Generic;
#nullable enable

namespace Lucene.Net.Analysis.Util
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
    /// A simple class that stores key <see cref="string"/>s as <see cref="T:char[]"/>'s in a
    /// hash table. Note that this is not a general purpose
    /// class.  For example, it cannot remove items from the
    /// map, nor does it resize its hash table to be smaller,
    /// etc.  It is designed to be quick to retrieve items
    /// by <see cref="T:char[]"/> keys without the necessity of converting
    /// to a <see cref="string"/> first.
    /// 
    /// <a name="version"></a>
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating <see cref="CharArrayDictionary{TValue}"/>:
    /// <list type="bullet">
    ///   <item><description> As of 3.1, supplementary characters are
    ///       properly lowercased.</description></item>
    /// </list>
    /// Before 3.1 supplementary characters could not be
    /// lowercased correctly due to the lack of Unicode 4
    /// support in JDK 1.4. To use instances of
    /// <see cref="CharArrayDictionary{TValue}"/> with the behavior before Lucene
    /// 3.1 pass a <see cref="LuceneVersion"/> &lt; 3.1 to the constructors.
    /// </para>
    /// </summary>
    [DebuggerDisplay("Count = {Count}, Values = {ToString()}")]
    public class CharArrayDictionary<TValue> : ICharArrayDictionary, IDictionary<string, TValue>, IDictionary, IReadOnlyDictionary<string, TValue>
    {
        // LUCENENET: Made public, renamed Empty
        /// <summary>
        /// Returns an empty, unmodifiable map. </summary>
        public static readonly CharArrayDictionary<TValue> Empty = new CharArrayDictionary.EmptyCharArrayDictionary<TValue>();

        private const int INIT_SIZE = 8;
        private readonly CharacterUtils charUtils;
        private readonly bool ignoreCase;
        private int count;
        private readonly LuceneVersion matchVersion; // package private because used in CharArraySet
        internal char[][] keys; // package private because used in CharArraySet's non Set-conform CharArraySetIterator
        internal MapValue[] values; // package private because used in CharArraySet's non Set-conform CharArraySetIterator

        private int version; // LUCENENET specific - protection so mutating the state of the collection causes enumerators to throw exceptions.

        /// <summary>
        /// LUCENENET: Moved this from CharArraySet so it doesn't need to know the generic type of CharArrayDictionary
        /// </summary>
        internal static readonly MapValue PLACEHOLDER = new MapValue();

        bool ICharArrayDictionary.IgnoreCase => ignoreCase;

        /// <summary>
        /// LUCENENET SPECIFIC type used to act as a placeholder. Since <c>null</c>
        /// means that our value is not populated, we need an instance of something
        /// to indicate it is. Using an instance of <typeparamref name="TValue"/> would only work if
        /// we could constrain it with the new() constraint, which isn't possible because
        /// some types such as <see cref="string"/> don't have a default constructor.
        /// So, this is a workaround that allows any type regardless of the type of constructor.
        /// 
        /// <para>
        /// Note also that we gain the ability to use value types for <typeparamref name="TValue"/>, but
        /// also create a difference in behavior from Java Lucene where the actual values 
        /// returned could be <c>null</c>.
        /// </para>
        /// </summary>
        internal class MapValue
        {
            [AllowNull]
            private TValue value = default;

            [AllowNull]
            public TValue Value
            {
                get => value!; // We are lying here - if this is a reference type, it could be null. But we don't care because IDictionary<TKey, TValue> doesn't care.
                set => this.value = value;
            }

            public MapValue()
            { }

            public MapValue([AllowNull] TValue value)
            {
                this.value = value;
            }
        }

        /// <summary>
        /// Create map with enough capacity to hold <paramref name="capacity"/> terms
        /// </summary>
        /// <param name="matchVersion">
        ///          lucene compatibility version - see <see cref="CharArrayDictionary{TValue}"/> for details. </param>
        /// <param name="capacity">
        ///          the initial capacity </param>
        /// <param name="ignoreCase">
        ///          <c>false</c> if and only if the set should be case sensitive;
        ///          otherwise <c>true</c>. </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than zero.</exception>
        public CharArrayDictionary(LuceneVersion matchVersion, int capacity, bool ignoreCase)
        {
            // LUCENENET: Added guard clause
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ArgumentOutOfRange_NeedNonNegNum);

            this.ignoreCase = ignoreCase;
            var size = INIT_SIZE;
            while (capacity + (capacity >> 2) > size)
            {
                size <<= 1;
            }
            keys = new char[size][];
            values = new MapValue[size];
            this.charUtils = CharacterUtils.GetInstance(matchVersion);
            this.matchVersion = matchVersion;
        }

        /// <summary>
        /// Creates a map from the mappings in another map. 
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. </param>
        /// <param name="collection">
        ///          a map (<see cref="T:IDictionary{string, V}"/>) whose mappings to be copied </param>
        /// <param name="ignoreCase">
        ///          <c>false</c> if and only if the set should be case sensitive;
        ///          otherwise <c>true</c>. </param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public CharArrayDictionary(LuceneVersion matchVersion, IDictionary<string, TValue> collection, bool ignoreCase)
            : this(matchVersion, collection?.Count ?? 0, ignoreCase)
        {
            // LUCENENET: Added guard clause
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var v in collection)
            {
                // LUCENENET: S1699: Don't call call protected members in the constructor
                if (keys[GetSlot(v.Key)] != null) // ContainsKey
                {
                    throw new ArgumentException(string.Format(SR.Argument_AddingDuplicate, v.Key));
                }
                SetImpl(v.Key, new MapValue(v.Value));
            }
        }

        /// <summary>
        /// Create set from the supplied map (used internally for readonly maps...)
        /// </summary>
        internal CharArrayDictionary(CharArrayDictionary<TValue> toCopy)
        {
            this.keys = toCopy.keys;
            this.values = toCopy.values;
            this.ignoreCase = toCopy.ignoreCase;
            this.count = toCopy.count;
            this.charUtils = toCopy.charUtils;
            this.matchVersion = toCopy.matchVersion;
        }

        /// <summary>
        /// Adds the <see cref="T:KeyValuePair{string, V}.Value"/> for the passed in <see cref="T:KeyValuePair{string, V}.Key"/>.
        /// Note that the <see cref="T:KeyValuePair{string, V}"/> instance is not added to the dictionary.
        /// </summary>
        /// <param name="item">A <see cref="T:KeyValuePair{string, V}"/> whose <see cref="T:KeyValuePair{string, V}.Value"/> 
        /// will be added for the corresponding <see cref="T:KeyValuePair{string, V}.Key"/>. </param>
        public virtual void Add(KeyValuePair<string, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Adds the <paramref name="value"/> for the passed in <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The string-able type to be added/updated in the dictionary.</param>
        /// <param name="value">The corresponding value for the given <paramref name="key"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">An element with <paramref name="key"/> already exists in the dictionary.</exception>
        public virtual void Add(string key, TValue value)
        {
            if (ContainsKey(key))
            {
                throw new ArgumentException(string.Format(SR.Argument_AddingDuplicate, key), nameof(key));
            }
            Set(key, value);
        }

        /// <summary>
        /// Adds the <paramref name="value"/> for the passed in <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The string-able type to be added/updated in the dictionary.</param>
        /// <param name="value">The corresponding value for the given <paramref name="key"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">An element with <paramref name="key"/> already exists in the dictionary.</exception>
        public virtual void Add(char[] key, TValue value)
        {
            if (ContainsKey(key))
            {
                throw new ArgumentException(string.Format(SR.Argument_AddingDuplicate, key), nameof(key));
            }
            Set(key, value);
        }

        /// <summary>
        /// Adds the <paramref name="value"/> for the passed in <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The string-able type to be added/updated in the dictionary.</param>
        /// <param name="value">The corresponding value for the given <paramref name="key"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The <paramref name="key"/>'s <see cref="ICharSequence.HasValue"/> property returns <c>false</c>.
        /// </exception>
        /// <exception cref="ArgumentException">An element with <paramref name="key"/> already exists in the dictionary.</exception>
        public virtual void Add(ICharSequence key, TValue value)
        {
            if (ContainsKey(key))
            {
                throw new ArgumentException(string.Format(SR.Argument_AddingDuplicate, key), nameof(key));
            }
            Set(key, value);
        }

        /// <summary>
        /// Adds the <paramref name="value"/> for the passed in <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The string-able type to be added/updated in the dictionary.</param>
        /// <param name="value">The corresponding value for the given <paramref name="key"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">An element with <paramref name="key"/> already exists in the dictionary.</exception>
        public virtual void Add<T>(T key, TValue value)
        {
            if (ContainsKey(key))
            {
                throw new ArgumentException(string.Format(SR.Argument_AddingDuplicate, key), nameof(key));
            }
            Set(key, value);
        }

        /// <summary>
        /// Returns an unmodifiable <see cref="CharArrayDictionary{TValue}"/>. This allows to provide
        /// unmodifiable views of internal map for "read-only" use.
        /// </summary>
        /// <returns> an new unmodifiable <see cref="CharArrayDictionary{TValue}"/>. </returns>
        // LUCENENET specific - allow .NET-like syntax for creating immutable collections
        public CharArrayDictionary<TValue> AsReadOnly()
        {
            return this is CharArrayDictionary.UnmodifiableCharArrayDictionary<TValue> readOnlyDictionary ?
                readOnlyDictionary :
                new CharArrayDictionary.UnmodifiableCharArrayDictionary<TValue>(this);
        }

        /// <summary>
        /// Clears all entries in this map. This method is supported for reusing, but not 
        /// <see cref="IDictionary{TKey, TValue}.Remove(TKey)"/>. 
        /// </summary>
        public virtual void Clear()
        {
            version++;
            count = 0;
            keys.Fill(null);
            values.Fill(null);
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        bool ICollection<KeyValuePair<string, TValue>>.Contains(KeyValuePair<string, TValue> item)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Copies all items in the current dictionary the <paramref name="array"/> starting at the <paramref name="index"/>.
        /// The array is assumed to already be dimensioned to fit the elements in this dictionary; otherwise a <see cref="ArgumentOutOfRangeException"/>
        /// will be thrown.
        /// </summary>
        /// <param name="array">The array to copy the items into.</param>
        /// <param name="index">A 32-bit integer that represents the index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source is greater
        /// than the available space from <paramref name="index"/> to the end of the destination array.</exception>
        public virtual void CopyTo(KeyValuePair<string, TValue>[] array, int index)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count > array.Length - index)
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);

            using var iter = GetEnumerator();
            for (int i = index; iter.MoveNext(); i++)
            {
                array[i] = new KeyValuePair<string, TValue>(iter.CurrentKeyString, iter.CurrentValue!);
            }
        }

        /// <summary>
        /// Copies all items in the current dictionary the <paramref name="array"/> starting at the <paramref name="index"/>.
        /// The array is assumed to already be dimensioned to fit the elements in this dictionary; otherwise a <see cref="ArgumentOutOfRangeException"/>
        /// will be thrown.
        /// </summary>
        /// <param name="array">The array to copy the items into.</param>
        /// <param name="index">A 32-bit integer that represents the index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source is greater
        /// than the available space from <paramref name="index"/> to the end of the destination array.</exception>
        public virtual void CopyTo(KeyValuePair<char[], TValue>[] array, int index)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count > array.Length - index)
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);

            using var iter = GetEnumerator();
            for (int i = index; iter.MoveNext(); i++)
            {
                array[i] = new KeyValuePair<char[], TValue>((char[])iter.CurrentKey.Clone(), iter.CurrentValue!);
            }
        }

        /// <summary>
        /// Copies all items in the current dictionary the <paramref name="array"/> starting at the <paramref name="index"/>.
        /// The array is assumed to already be dimensioned to fit the elements in this dictionary; otherwise a <see cref="ArgumentOutOfRangeException"/>
        /// will be thrown.
        /// </summary>
        /// <param name="array">The array to copy the items into.</param>
        /// <param name="index">A 32-bit integer that represents the index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source is greater
        /// than the available space from <paramref name="index"/> to the end of the destination array.</exception>
        public virtual void CopyTo(KeyValuePair<ICharSequence, TValue>[] array, int index)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count > array.Length - index)
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);

            using var iter = GetEnumerator();
            for (int i = index; iter.MoveNext(); i++)
            {
                array[i] = new KeyValuePair<ICharSequence, TValue>(((char[])iter.CurrentKey.Clone()).AsCharSequence(), iter.CurrentValue!);
            }
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="length"/> chars of <paramref name="text"/> starting at <paramref name="offset"/>
        /// are in the <see cref="Keys"/> 
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="length"/> is less than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="offset"/> and <paramref name="length"/> refer to a position outside of <paramref name="text"/>.</exception>
        public virtual bool ContainsKey(char[] text, int offset, int length)
        {
            return keys[GetSlot(text, offset, length)] != null;
        }

        /// <summary>
        /// <c>true</c> if the entire <see cref="Keys"/> is the same as the 
        /// <paramref name="text"/> <see cref="T:char[]"/> being passed in; 
        /// otherwise <c>false</c>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool ContainsKey(char[] text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            return keys[GetSlot(text, 0, text.Length)] != null;
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="text"/> <see cref="string"/> is in the <see cref="Keys"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool ContainsKey(string text)
        {
            return keys[GetSlot(text)] != null;
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="text"/> <see cref="ICharSequence"/> is in the <see cref="Keys"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="text"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The <paramref name="text"/>'s <see cref="ICharSequence.HasValue"/> property returns <c>false</c>.
        /// </exception>
        public virtual bool ContainsKey(ICharSequence text)
        {
            if (text is null || !text.HasValue)
                throw new ArgumentNullException(nameof(text));

            if (text is CharArrayCharSequence charArrayCs)
                return ContainsKey(charArrayCs.Value!);
            if (text is StringBuilderCharSequence stringBuilderCs)
                return ContainsKey(stringBuilderCs.Value!.ToString()); // LUCENENET: Indexing into a StringBuilder is slow, so materialize

            return keys[GetSlot(text)] != null;
        }


        /// <summary>
        /// <c>true</c> if the <paramref name="text"/> <see cref="object.ToString()"/> (in the invariant culture)
        /// is in the <see cref="Keys"/>;  otherwise <c>false</c>
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool ContainsKey<T>(T text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)

            if (text is string str)
                return ContainsKey(str);
            if (text is char[] charArray)
                return ContainsKey(charArray, 0, charArray.Length);
            if (text is ICharSequence cs)
                return ContainsKey(cs);

            var returnType = CharArrayDictionary.ConvertObjectToChars(text, out char[] chars, out string s);
            if (returnType == CharArrayDictionary.CharReturnType.String)
                return ContainsKey(s);
            else
                return ContainsKey(chars);
        }

        #region Get

        /// <summary>
        /// Returns the value of the mapping of <paramref name="length"/> chars of <paramref name="text"/>
        /// starting at <paramref name="offset"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="length"/> is less than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="offset"/> and <paramref name="length"/> refer to a position outside of <paramref name="text"/>.</exception>
        /// <exception cref="KeyNotFoundException">The effective key is not found in the dictionary.</exception>
        internal virtual TValue Get(char[] text, int offset, int length, bool throwIfNotFound = true)
        {
            MapValue? value = values[GetSlot(text, offset, length)];
            if (value is not null)
            {
                return value.Value;
            }
            if (throwIfNotFound)
                throw new KeyNotFoundException(string.Format(SR.Arg_KeyNotFoundWithKey, new string(text, offset, length)));
            return default!;
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <paramref name="text"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="KeyNotFoundException"><paramref name="text"/> is not found in the dictionary.</exception>
        internal virtual TValue Get(char[] text, bool throwIfNotFound = true)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            MapValue? value = values[GetSlot(text, 0, text.Length)];
            if (value is not null)
            {
                return value.Value;
            }
            if (throwIfNotFound)
                throw new KeyNotFoundException(string.Format(SR.Arg_KeyNotFoundWithKey, new string(text)));
            return default!;
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <see cref="ICharSequence"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="text"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The <paramref name="text"/>'s <see cref="ICharSequence.HasValue"/> property returns <c>false</c>.
        /// </exception>
        /// <exception cref="KeyNotFoundException"><paramref name="text"/> is not found in the dictionary.</exception>
        internal virtual TValue Get(ICharSequence text, bool throwIfNotFound = true)
        {
            if (text is null || !text.HasValue)
                throw new ArgumentNullException(nameof(text));

            if (text is StringCharSequence strCs)
                return Get(strCs.Value!, throwIfNotFound);
            if (text is CharArrayCharSequence charArrayCs)
                return Get(charArrayCs.Value!, throwIfNotFound);
            if (text is StringBuilderCharSequence stringBuilderCs)
                return Get(stringBuilderCs.Value!.ToString(), throwIfNotFound); // LUCENENET: Indexing into a StringBuilder is slow, so materialize

            var value = values[GetSlot(text)];
            if (value is not null)
            {
                return value.Value;
            }
            if (throwIfNotFound)
                throw new KeyNotFoundException(string.Format(SR.Arg_KeyNotFoundWithKey, text));
            return default!;
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <see cref="string"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="KeyNotFoundException"><paramref name="text"/> is not found in the dictionary.</exception>
        internal virtual TValue Get(string text, bool throwIfNotFound)
        {
            var value = values[GetSlot(text)];
            if (value is not null)
            {
                return value.Value;
            }
            if (throwIfNotFound)
                throw new KeyNotFoundException(string.Format(SR.Arg_KeyNotFoundWithKey, text));
            return default!;
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <see cref="object.ToString()"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="KeyNotFoundException"><paramref name="text"/> is not found in the dictionary.</exception>
        internal virtual TValue Get<T>(T text, bool throwIfNotFound = true)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            // LUCENENET NOTE: Testing for *is* is at least 10x faster
            // than casting using *as* and then checking for null.
            // http://stackoverflow.com/q/1583050/181087
            if (text is string str)
                return Get(str, throwIfNotFound);
            if (text is char[] charArray)
                return Get(charArray, 0, charArray.Length, throwIfNotFound);
            if (text is ICharSequence cs)
                return Get(cs, throwIfNotFound);

            var returnType = CharArrayDictionary.ConvertObjectToChars(text, out char[] chars, out string s);
            if (returnType == CharArrayDictionary.CharReturnType.String)
                return Get(s, throwIfNotFound);
            else
                return Get(chars, throwIfNotFound);
        }

        #endregion Get

        #region GetSlot

        private int GetSlot(char[] text, int offset, int length)
        {
            int code = GetHashCode(text, offset, length);
            int pos = code & (keys.Length - 1);
            char[] text2 = keys[pos];
            if (text2 != null && !Equals(text, offset, length, text2))
            {
                int inc = ((code >> 8) + code) | 1;
                do
                {
                    code += inc;
                    pos = code & (keys.Length - 1);
                    text2 = keys[pos];
                } while (text2 != null && !Equals(text, offset, length, text2));
            }
            return pos;
        }

        /// <summary>
        /// Returns <c>true</c> if the <see cref="ICharSequence"/> is in the set.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="text"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The <paramref name="text"/>'s <see cref="ICharSequence.HasValue"/> property returns <c>false</c>.
        /// </exception>
        private int GetSlot(ICharSequence text)
        {
            int code = GetHashCode(text);
            int pos = code & (keys.Length - 1);
            char[] text2 = keys[pos];
            if (text2 != null && !Equals(text, text2))
            {
                int inc = ((code >> 8) + code) | 1;
                do
                {
                    code += inc;
                    pos = code & (keys.Length - 1);
                    text2 = keys[pos];
                } while (text2 != null && !Equals(text, text2));
            }
            return pos;
        }

        /// <summary>
        /// Returns <c>true</c> if the <see cref="string"/> is in the set.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        private int GetSlot(string text)
        {
            int code = GetHashCode(text);
            int pos = code & (keys.Length - 1);
            char[] text2 = keys[pos];
            if (text2 != null && !Equals(text, text2))
            {
                int inc = ((code >> 8) + code) | 1;
                do
                {
                    code += inc;
                    pos = code & (keys.Length - 1);
                    text2 = keys[pos];
                } while (text2 != null && !Equals(text, text2));
            }
            return pos;
        }

        #endregion GetSlot

        #region Put (value)

        /// <summary>
        /// Add the given mapping.
        /// If ignoreCase is <c>true</c> for this dictionary, the text array will be directly modified.
        /// The user should never modify this text array after calling this method.
        /// <para/>
        /// <b>Note:</b> The <see cref="this[char[]]"/> setter is more efficient than this method if
        /// the <paramref name="previousValue"/> is not required.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value"></param>
        /// <param name="previousValue">The previous value associated with the key, or the default for the type of <paramref name="value"/>
        /// parameter if there was no mapping for <paramref name="text"/>.</param>
        /// <returns><c>true</c> if the mapping was added, <c>false</c> if the key already existed. The <paramref name="previousValue"/>
        /// will be populated if the result is <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool Put(char[] text, TValue value, [MaybeNullWhen(returnValue: true)] out TValue previousValue) // LUCENENET: Refactored to use out value to support value types
        {
            MapValue? oldValue = PutImpl(text, new MapValue(value));
            if (oldValue is not null)
            {
                previousValue = oldValue.Value;
                return false;
            }
            previousValue = default;
            return true;
        }

        /// <summary>
        /// Add the given mapping.
        /// <para/>
        /// <b>Note:</b> The <see cref="this[string]"/> setter is more efficient than this method if
        /// the <paramref name="previousValue"/> is not required.
        /// </summary>
        /// <param name="text">A key with which the specified <paramref name="value"/> is associated.</param>
        /// <param name="value">The value to be associated with the specified <paramref name="text"/>.</param>
        /// <param name="previousValue">The previous value associated with the key, or the default for the type of <paramref name="value"/>
        /// parameter if there was no mapping for <paramref name="text"/>.</param>
        /// <returns><c>true</c> if the mapping was added, <c>false</c> if the key already existed. The <paramref name="previousValue"/>
        /// will be populated if the result is <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool Put(string text, TValue value, [MaybeNullWhen(returnValue: true)] out TValue previousValue) // LUCENENET: Refactored to use out value to support value types
        {
            MapValue? oldValue = PutImpl(text, new MapValue(value));
            if (oldValue is not null)
            {
                previousValue = oldValue.Value;
                return false;
            }
            previousValue = default;
            return true;
        }

        /// <summary>
        /// Add the given mapping.
        /// <para/>
        /// <b>Note:</b> The <see cref="this[ICharSequence]"/> setter is more efficient than this method if
        /// the <paramref name="previousValue"/> is not required.
        /// </summary>
        /// <param name="text">A key with which the specified <paramref name="value"/> is associated.</param>
        /// <param name="value">The value to be associated with the specified <paramref name="text"/>.</param>
        /// <param name="previousValue">The previous value associated with the key, or the default for the type of <paramref name="value"/>
        /// parameter if there was no mapping for <paramref name="text"/>.</param>
        /// <returns><c>true</c> if the mapping was added, <c>false</c> if the key already existed. The <paramref name="previousValue"/>
        /// will be populated if the result is <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="text"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The <paramref name="text"/>'s <see cref="ICharSequence.HasValue"/> property returns <c>false</c>.
        /// </exception>
        public virtual bool Put(ICharSequence text, TValue value, [MaybeNullWhen(returnValue: true)] out TValue previousValue) // LUCENENET: Refactored to use out value to support value types
        {
            MapValue? oldValue = PutImpl(text, new MapValue(value));
            if (oldValue is not null)
            {
                previousValue = oldValue.Value;
                return false;
            }
            previousValue = default;
            return true;
        }

        /// <summary>
        /// Add the given mapping using the <see cref="object.ToString()"/> representation
        /// of <paramref name="text"/> in the <see cref="CultureInfo.InvariantCulture"/>.
        /// <para/>
        /// <b>Note:</b> The <see cref="this[object]"/> setter is more efficient than this method if
        /// the <paramref name="previousValue"/> is not required.
        /// </summary>
        /// <param name="text">A key with which the specified <paramref name="value"/> is associated.</param>
        /// <param name="value">The value to be associated with the specified <paramref name="text"/>.</param>
        /// <param name="previousValue">The previous value associated with the key, or the default for the type of <paramref name="value"/>
        /// parameter if there was no mapping for <paramref name="text"/>.</param>
        /// <returns><c>true</c> if the mapping was added, <c>false</c> if the key already existed. The <paramref name="previousValue"/>
        /// will be populated if the result is <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool Put<T>(T text, TValue value, [MaybeNullWhen(returnValue: true)] out TValue previousValue) // LUCENENET: Refactored to use out value to support value types
        {
            MapValue? oldValue = PutImpl(text, new MapValue(value));
            if (oldValue is not null)
            {
                previousValue = oldValue.Value;
                return false;
            }
            previousValue = default;
            return true;
        }

        #endregion Put (value)

        #region PutImpl

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="text"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The <paramref name="text"/>'s <see cref="ICharSequence.HasValue"/> property returns <c>false</c>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MapValue? PutImpl(ICharSequence text, MapValue value)
        {
            // LUCENENET: Added guard clause
            if (text is null || !text.HasValue)
                throw new ArgumentNullException(nameof(text));

            if (text is CharArrayCharSequence charArrayCs)
                return PutImpl(charArrayCs.Value ?? Arrays.Empty<char>(), value);
            if (text is StringBuilderCharSequence stringBuilderCs) // LUCENENET: Indexing into a StringBuilder is slow, so materialize
            {
                var sb = stringBuilderCs.Value!;
                char[] result = new char[sb.Length];
                sb.CopyTo(sourceIndex: 0, result, destinationIndex: 0, sb.Length);
                return PutImpl(result, value);
            }

            int length = text.Length;
            char[] buffer = new char[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = text[i];
            }
            return PutImpl(buffer, value);
        }

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MapValue? PutImpl<T>(T text, MapValue value)
        {
            // LUCENENET: Added guard clause
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            // LUCENENET NOTE: Testing for *is* is at least 10x faster
            // than casting using *as* and then checking for null.
            // http://stackoverflow.com/q/1583050/181087 
            if (text is string str)
                return PutImpl(str, value);
            if (text is char[] charArray)
                return PutImpl(charArray, value);
            if (text is ICharSequence cs)
                return PutImpl(cs, value);

            var returnType = CharArrayDictionary.ConvertObjectToChars(text, out char[] chars, out string s);
            if (returnType == CharArrayDictionary.CharReturnType.String)
                return PutImpl(s, value);
            else
                return PutImpl(chars, value);
        }

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MapValue? PutImpl(string text, MapValue value)
        {
            // LUCENENET: Added guard clause
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            // LUCENENET specific - only allocate char array if it is required.
            if (ignoreCase)
            {
                return PutImpl(text.ToCharArray(), value);
            }
            version++;
            int slot = GetSlot(text);
            if (keys[slot] != null)
            {
                MapValue oldValue = values[slot];
                values[slot] = value;
                return oldValue;
            }
            keys[slot] = text.ToCharArray();
            values[slot] = value;
            count++;

            if (count + (count >> 2) > keys.Length)
            {
                Rehash();
            }

            return null;
        }

        /// <summary>
        /// LUCENENET specific. Centralizes the logic between Put()
        /// implementations that accept a value and those that don't. This value is
        /// so we know whether or not the value was set, since we can't reliably do
        /// a check for <c>null</c> on the <typeparamref name="TValue"/> type.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MapValue? PutImpl(char[] text, MapValue value)
        {
            // LUCENENET: Added guard clause
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            version++;

            if (ignoreCase)
            {
                charUtils.ToLower(text, 0, text.Length);
            }
            int slot = GetSlot(text, 0, text.Length);
            if (keys[slot] != null)
            {
                MapValue oldValue = values[slot];
                values[slot] = value;
                return oldValue;
            }
            keys[slot] = text;
            values[slot] = value;
            count++;

            if (count + (count >> 2) > keys.Length)
            {
                Rehash();
            }

            return null;
        }

        #endregion PutImpl

        #region Set

        /// <summary>
        /// Sets the value of the mapping of the chars inside this <paramref name="text"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void Set(char[] text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            SetImpl(text, PLACEHOLDER);
        }

        /// <summary>
        /// Sets the value of the mapping of the chars inside this <see cref="ICharSequence"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void Set(ICharSequence text)
        {
            if (text is null || !text.HasValue)
                throw new ArgumentNullException(nameof(text));

            SetImpl(text, PLACEHOLDER);
        }

        /// <summary>
        /// Sets the value of the mapping of the chars inside this <see cref="string"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void Set(string text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            SetImpl(text, PLACEHOLDER);
        }

        /// <summary>
        /// Sets the value of the mapping of the chars inside this <see cref="object.ToString()"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        internal virtual void Set<T>(T text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            // LUCENENET NOTE: Testing for *is* is at least 10x faster
            // than casting using *as* and then checking for null.
            // http://stackoverflow.com/q/1583050/181087
            if (text is string str)
            {
                Set(str);
                return;
            }
            if (text is char[] charArray)
            {
                Set(charArray);
                return;
            }
            if (text is ICharSequence cs)
            {
                Set(cs);
                return;
            }

            var returnType = CharArrayDictionary.ConvertObjectToChars(text, out char[] chars, out string s);
            if (returnType == CharArrayDictionary.CharReturnType.String)
            {
                Set(s);
                return;
            }

            Set(chars);
        }

        void ICharArrayDictionary.Set(char[] text) => Set(text);
        void ICharArrayDictionary.Set(ICharSequence text) => Set(text);
        void ICharArrayDictionary.Set<T>(T text) => Set(text);
        void ICharArrayDictionary.Set(string text) => Set(text);

        #endregion Set

        #region Set (value)

        /// <summary>
        /// Sets the value of the mapping of <paramref name="length"/> chars of <paramref name="text"/>
        /// starting at <paramref name="offset"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="length"/> is less than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="offset"/> and <paramref name="length"/> refer to a position outside of <paramref name="text"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void Set(char[] text, int offset, int length, TValue? value)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            SetImpl(text, offset, length, new MapValue(value));
        }

        /// <summary>
        /// Sets the value of the mapping of the chars inside this <paramref name="text"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void Set(char[] text, TValue? value)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            SetImpl(text, new MapValue(value));
        }

        /// <summary>
        /// Sets the value of the mapping of the chars inside this <see cref="ICharSequence"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="text"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The <paramref name="text"/>'s <see cref="ICharSequence.HasValue"/> property returns <c>false</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void Set(ICharSequence text, TValue? value)
        {
            if (text is null || !text.HasValue)
                throw new ArgumentNullException(nameof(text));

            SetImpl(text, new MapValue(value));
        }

        /// <summary>
        /// Sets the value of the mapping of the chars inside this <see cref="string"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void Set(string text, TValue? value)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            SetImpl(text, new MapValue(value));
        }

        /// <summary>
        /// Sets the value of the mapping of the chars inside this <see cref="object.ToString()"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void Set<T>(T text, TValue? value)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            // LUCENENET NOTE: Testing for *is* is at least 10x faster
            // than casting using *as* and then checking for null.
            // http://stackoverflow.com/q/1583050/181087
            if (text is string str)
            {
                Set(str, value);
                return;
            }
            if (text is char[] charArray)
            {
                Set(charArray, 0, charArray.Length, value);
                return;
            }
            if (text is ICharSequence cs)
            {
                Set(cs, value);
                return;
            }

            var returnType = CharArrayDictionary.ConvertObjectToChars(text, out char[] chars, out string s);
            if (returnType == CharArrayDictionary.CharReturnType.String)
            {
                Set(s, value);
                return;
            }

            Set(chars, value);
        }

        #endregion Set (value)

        #region SetImpl

        /// <summary>
        /// LUCENENET specific. Like PutImpl, but doesn't have a return value or lookup to get the old value.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="text"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The <paramref name="text"/>'s <see cref="ICharSequence.HasValue"/> property returns <c>false</c>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetImpl(ICharSequence text, MapValue value)
        {
            // LUCENENET: Added guard clause
            if (text is null || !text.HasValue)
                throw new ArgumentNullException(nameof(text));

            if (text is CharArrayCharSequence charArrayCs)
            {
                SetImpl(charArrayCs.Value ?? Arrays.Empty<char>(), value);
                return;
            }
            if (text is StringBuilderCharSequence stringBuilderCs) // LUCENENET: Indexing into a StringBuilder is slow, so materialize
            {
                var sb = stringBuilderCs.Value!;
                char[] result = new char[sb.Length];
                sb.CopyTo(sourceIndex: 0, result, destinationIndex: 0, sb.Length);
                SetImpl(result, value);
                return;
            }

            int length = text.Length;
            char[] buffer = new char[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = text[i];
            }

            SetImpl(buffer, value);
        }

        /// <summary>
        /// LUCENENET specific. Like PutImpl, but doesn't have a return value or lookup to get the old value.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetImpl(string text, MapValue value)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            // LUCENENET specific - only allocate char array if it is required.
            if (ignoreCase)
            {
                SetImpl(text.ToCharArray(), value);
                return;
            }
            version++;
            int slot = GetSlot(text);
            if (keys[slot] != null)
            {
                values[slot] = value;
                return;
            }
            keys[slot] = text.ToCharArray();
            values[slot] = value;
            count++;

            if (count + (count >> 2) > keys.Length)
            {
                Rehash();
            }
        }

        /// <summary>
        /// LUCENENET specific. Like PutImpl, but doesn't have a return value or lookup to get the old value.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetImpl(char[] text, MapValue value)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            SetImpl(text, 0, text.Length, value);
        }

        /// <summary>
        /// LUCENENET specific. Like PutImpl, but doesn't have a return value or lookup to get the old value.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="length"/> is less than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="offset"/> and <paramref name="length"/> refer to a position outside of <paramref name="text"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetImpl(char[] text, int offset, int length, MapValue value)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            version++;
            if (ignoreCase)
            {
                charUtils.ToLower(text, offset, length);
            }
            int slot = GetSlot(text, offset, length);
            if (keys[slot] != null)
            {
                values[slot] = value;
                return;
            }
            keys[slot] = text;
            values[slot] = value;
            count++;

            if (count + (count >> 2) > keys.Length)
            {
                Rehash();
            }
        }

        #endregion SetImpl

        #region PutAll

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IDictionary{char[],TValue}"/>'s
        /// entries, and calls this map's <see cref="Set(char[], TValue?)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">A dictionary of values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// An element in the collection is <c>null</c>.
        /// </exception>
        public virtual void PutAll(IDictionary<char[], TValue> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                Set(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IDictionary{string,TValue}"/>'s
        /// entries, and calls this map's <see cref="Set(string, TValue?)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">A dictionary of values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// An element in the collection is <c>null</c>.
        /// </exception>
        public virtual void PutAll(IDictionary<string, TValue> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                Set(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IDictionary{ICharSequence,TValue}"/>'s
        /// entries, and calls this map's <see cref="Set(ICharSequence, TValue?)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">A dictionary of values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// An element in the collection has a <c>null</c> key.
        /// <para/>
        /// -or-
        /// <para/>
        /// The key's <see cref="ICharSequence.HasValue"/> property for a given element in the collection returns <c>false</c>.
        /// </exception>
        public virtual void PutAll(IDictionary<ICharSequence, TValue> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                Set(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IDictionary{T,TValue}"/>'s
        /// entries, and calls this map's <see cref="Set{T}(T, TValue?)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">A dictionary of values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// An element in the collection is <c>null</c>.
        /// </exception>
        public virtual void PutAll<T>(IDictionary<T, TValue> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                Set(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IEnumerable{KeyValuePair{char[],TValue}}"/>'s
        /// entries, and calls this map's <see cref="Set(char[], TValue?)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">The values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// An element in the collection is <c>null</c>.
        /// </exception>
        public virtual void PutAll(IEnumerable<KeyValuePair<char[], TValue>> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                Set(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IEnumerable{KeyValuePair{string,TValue}}"/>'s
        /// entries, and calls this map's <see cref="Set(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">The values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// An element in the collection is <c>null</c>.
        /// </exception>
        public virtual void PutAll(IEnumerable<KeyValuePair<string, TValue>> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                Set(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IEnumerable{KeyValuePair{ICharSequence,TValue}}"/>'s
        /// entries, and calls this map's <see cref="Set(ICharSequence, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">The values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// An element in the collection has a <c>null</c> key.
        /// <para/>
        /// -or-
        /// <para/>
        /// The key's <see cref="ICharSequence.HasValue"/> property for a given element in the collection returns <c>false</c>.
        /// </exception>
        public virtual void PutAll(IEnumerable<KeyValuePair<ICharSequence, TValue>> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                Set(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IEnumerable{KeyValuePair{TKey,TValue}}"/>'s
        /// entries, and calls this map's <see cref="Set{T}(T, TValue?)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">The values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// An element in the collection is <c>null</c>.
        /// </exception>
        public virtual void PutAll<T>(IEnumerable<KeyValuePair<T, TValue>> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

#if FEATURE_SPANFORMATTABLE
            Span<char> buffer = stackalloc char[256];
#else
            Span<char> buffer = stackalloc char[1];
#endif

            foreach (var kvp in collection)
            {
                // Convert the item to chars in the invariant culture
                var returnType = CharArrayDictionary.ConvertObjectToChars(kvp.Key, out char[] chars, out string s, buffer);
                if (returnType == CharArrayDictionary.CharReturnType.String)
                    Set(s, kvp.Value);
                else
                    Set(chars, kvp.Value);
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Rehash()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(keys.Length == values.Length);
            int newSize = 2 * keys.Length;
            char[][] oldkeys = keys;
            MapValue[] oldvalues = values;
            keys = new char[newSize][];
            values = new MapValue[newSize];

            for (int i = 0; i < oldkeys.Length; i++)
            {
                char[] text = oldkeys[i];
                if (text != null)
                {
                    // todo: could be faster... no need to compare strings on collision
                    int slot = GetSlot(text, 0, text.Length);
                    keys[slot] = text;
                    values[slot] = oldvalues[i];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Equals(char[] text1, int offset, int length, char[] text2)
        {
            if (length != text2.Length)
            {
                return false;
            }
            int limit = offset + length;
            if (ignoreCase)
            {
                for (int i = 0; i < length;)
                {
                    var codePointAt = charUtils.CodePointAt(text1, offset + i, limit);
                    if (Character.ToLower(codePointAt, CultureInfo.InvariantCulture) != charUtils.CodePointAt(text2, i, text2.Length)) // LUCENENET specific - need to use invariant culture to match Java
                    {
                        return false;
                    }
                    i += Character.CharCount(codePointAt);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    if (text1[offset + i] != text2[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Equals(ICharSequence text1, char[] text2)
        {
            int length = text1.Length;
            if (length != text2.Length)
            {
                return false;
            }
            if (ignoreCase)
            {
                for (int i = 0; i < length;)
                {
                    int codePointAt = charUtils.CodePointAt(text1, i);
                    if (Character.ToLower(codePointAt, CultureInfo.InvariantCulture) != charUtils.CodePointAt(text2, i, text2.Length)) // LUCENENET specific - need to use invariant culture to match Java
                    {
                        return false;
                    }
                    i += Character.CharCount(codePointAt);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    if (text1[i] != text2[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Equals(string text1, char[] text2)
        {
            int length = text1.Length;
            if (length != text2.Length)
            {
                return false;
            }
            if (ignoreCase)
            {
                for (int i = 0; i < length;)
                {
                    int codePointAt = charUtils.CodePointAt(text1, i);
                    if (Character.ToLower(codePointAt, CultureInfo.InvariantCulture) != charUtils.CodePointAt(text2, i, text2.Length)) // LUCENENET specific - need to use invariant culture to match Java
                    {
                        return false;
                    }
                    i += Character.CharCount(codePointAt);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    if (text1[i] != text2[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// LUCENENET Specific - test for value equality similar to how it is done in Java
        /// </summary>
        /// <param name="obj">Another dictionary to test the values of</param>
        /// <returns><c>true</c> if the given object is an <see cref="T:IDictionary{object, V}"/> that contains
        /// the same key value pairs as the current map</returns>
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            if (obj is not IDictionary<string, TValue> other)
                return false;
            if (this.Count != other.Count)
                return false;

            if (obj is CharArrayDictionary<TValue> charArrayDictionary)
            {
                using var iter = charArrayDictionary.GetEnumerator();
                while (iter.MoveNext())
                {
                    if (!this.TryGetValue(iter.CurrentKey, out TValue? value))
                        return false;

                    if (!JCG.EqualityComparer<TValue>.Default.Equals(value!, iter.Current.Value))
                        return false;
                }
            }
            else
            {
                using var iter = other.GetEnumerator();
                while (iter.MoveNext())
                {
                    if (!this.TryGetValue(iter.Current.Key, out TValue? value))
                        return false;

                    if (!JCG.EqualityComparer<TValue>.Default.Equals(value!, iter.Current.Value))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// LUCENENET Specific - override required by .NET because we override Equals
        /// to simulate Java's value equality checking.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            const int PRIME = 31; // arbitrary prime
            int hash = PRIME;
            using (var iter = GetEnumerator())
            {
                while (iter.MoveNext())
                {
                    hash = (hash * PRIME) ^ iter.CurrentKeyString.GetHashCode();
                    TValue? value = iter.CurrentValue;
                    hash = (hash * PRIME) ^ (value is null ? 0 : JCG.EqualityComparer<TValue>.Default.GetHashCode(value));
                }
            }
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetHashCode(char[] text, int offset, int length)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (offset > text.Length - length) // Checks for int overflow
                throw new ArgumentException(SR.ArgumentOutOfRange_IndexLength);

            int code = 0;
            int stop = offset + length;
            if (ignoreCase)
            {
                for (int i = offset; i < stop;)
                {
                    int codePointAt = charUtils.CodePointAt(text, i, stop);
                    code = code * 31 + Character.ToLower(codePointAt, CultureInfo.InvariantCulture); // LUCENENET specific - need to use invariant culture to match Java
                    i += Character.CharCount(codePointAt);
                }
            }
            else
            {
                for (int i = offset; i < stop; i++)
                {
                    code = code * 31 + text[i];
                }
            }
            return code;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetHashCode(ICharSequence text)
        {
            if (text is null || !text.HasValue)
                throw new ArgumentNullException(nameof(text)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)

            int code = 0;
            int length = text.Length;
            if (ignoreCase)
            {
                for (int i = 0; i < length;)
                {
                    int codePointAt = charUtils.CodePointAt(text, i);
                    code = code * 31 + Character.ToLower(codePointAt, CultureInfo.InvariantCulture); // LUCENENET specific - need to use invariant culture to match Java
                    i += Character.CharCount(codePointAt);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    code = code * 31 + text[i];
                }
            }
            return code;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetHashCode(string text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)

            int code = 0;
            int length = text.Length;
            if (ignoreCase)
            {
                for (int i = 0; i < length;)
                {
                    int codePointAt = charUtils.CodePointAt(text, i);
                    code = code * 31 + Character.ToLower(codePointAt, CultureInfo.InvariantCulture); // LUCENENET specific - need to use invariant culture to match Java
                    i += Character.CharCount(codePointAt);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    code = code * 31 + text[i];
                }
            }
            return code;
        }

        #region For .NET Support LUCENENET

        /// <summary>
        /// The Lucene version corresponding to the compatibility behavior 
        /// that this instance emulates
        /// </summary>
        public virtual LuceneVersion MatchVersion => matchVersion;

        /// <summary>
        /// Adds a placeholder with the given <paramref name="text"/> as the key.
        /// Primarily for internal use by <see cref="CharArraySet"/>.
        /// </summary>
        /// <returns><c>true</c> if the key was added, <c>false</c> if the key already existed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool Put(char[] text)
        {
            return PutImpl(text, PLACEHOLDER) is null;
        }

        /// <summary>
        /// Adds a placeholder with the given <paramref name="text"/> as the key.
        /// Primarily for internal use by <see cref="CharArraySet"/>.
        /// </summary>
        /// <returns><c>true</c> if the key was added, <c>false</c> if the key already existed.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="text"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The <paramref name="text"/>'s <see cref="ICharSequence.HasValue"/> property returns <c>false</c>.</exception>
        public virtual bool Put(ICharSequence text)
        {
            return PutImpl(text, PLACEHOLDER) is null;
        }

        /// <summary>
        /// Adds a placeholder with the given <paramref name="text"/> as the key.
        /// Primarily for internal use by <see cref="CharArraySet"/>.
        /// </summary>
        /// <returns><c>true</c> if the key was added, <c>false</c> if the key already existed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool Put(string text)
        {
            return PutImpl(text, PLACEHOLDER) is null;
        }

        /// <summary>
        /// Adds a placeholder with the given <paramref name="text"/> as the key.
        /// Primarily for internal use by <see cref="CharArraySet"/>.
        /// </summary>
        /// <returns><c>true</c> if the key was added, <c>false</c> if the key already existed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool Put<T>(T text)
        {
            return PutImpl(text, PLACEHOLDER) is null;
        }

        /// <summary>
        /// Returns a copy of the current <see cref="CharArrayDictionary{TValue}"/> as a new instance of <see cref="CharArrayDictionary{TValue}"/>.
        /// Preserves the value of <c>matchVersion</c> and <c>ignoreCase</c> from the current instance.
        /// </summary>
        /// <returns> A copy of the current <see cref="CharArrayDictionary{TValue}"/> as a <see cref="CharArrayDictionary{TValue}"/>. </returns>
        // LUCENENET specific - allow .NET-like syntax for copying CharArrayDictionary
        public virtual CharArrayDictionary<TValue> ToCharArrayDictionary()
        {
            return CharArrayDictionary.Copy(this.matchVersion, (IDictionary<string, TValue>)this);
        }

        /// <summary>
        /// Returns a copy of the current <see cref="CharArrayDictionary{TValue}"/> as a new instance of <see cref="CharArrayDictionary{TValue}"/>
        /// using the specified <paramref name="matchVersion"/> value. Preserves the value of <c>ignoreCase</c> from the current instance.
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. </param>
        /// <returns> A copy of the current <see cref="CharArrayDictionary{TValue}"/> as a <see cref="CharArrayDictionary{TValue}"/>. </returns>
        // LUCENENET specific - allow .NET-like syntax for copying CharArrayDictionary
        public virtual CharArrayDictionary<TValue> ToCharArrayDictionary(LuceneVersion matchVersion)
        {
            return CharArrayDictionary.Copy(matchVersion, (IDictionary<string, TValue>)this);
        }

        /// <summary>
        /// Returns a copy of the current <see cref="CharArrayDictionary{TValue}"/> as a new instance of <see cref="CharArrayDictionary{TValue}"/>
        /// using the specified <paramref name="matchVersion"/> and <paramref name="ignoreCase"/> values.
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. </param>
        /// <param name="ignoreCase"><c>false</c> if and only if the set should be case sensitive otherwise <c>true</c>.</param>
        /// <returns> A copy of the current <see cref="CharArrayDictionary{TValue}"/> as a <see cref="CharArrayDictionary{TValue}"/>. </returns>
        // LUCENENET specific - allow .NET-like syntax for copying CharArrayDictionary
        public virtual CharArrayDictionary<TValue> ToCharArrayDictionary(LuceneVersion matchVersion, bool ignoreCase)
        {
            return new CharArrayDictionary<TValue>(matchVersion, this, ignoreCase);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="offset">The position of the <paramref name="key"/> where the target key begins.</param>
        /// <param name="length">The total length of the <paramref name="key"/>.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="length"/> is less than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="offset"/> and <paramref name="length"/> refer to a position outside of <paramref name="key"/>.</exception>
        public virtual bool TryGetValue(char[] key, int offset, int length, [MaybeNullWhen(returnValue: false)] out TValue value)
        {
            var val = values[GetSlot(key, offset, length)];
            if (val != null)
            {
                value = val.Value;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public virtual bool TryGetValue(char[] key, [MaybeNullWhen(returnValue: false)] out TValue value)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            var val = values[GetSlot(key, 0, key.Length)];
            if (val != null)
            {
                value = val.Value;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The <paramref name="key"/>'s <see cref="ICharSequence.HasValue"/> property returns <c>false</c>.
        /// </exception>
        public virtual bool TryGetValue(ICharSequence key, [MaybeNullWhen(returnValue: false)] out TValue value)
        {
            if (key is null || !key.HasValue)
                throw new ArgumentNullException(nameof(key));

            if (key is StringCharSequence strCs)
                return TryGetValue(strCs.Value!, out value);
            if (key is CharArrayCharSequence charArrayCs)
                return TryGetValue(charArrayCs.Value!, out value);
            if (key is StringBuilderCharSequence stringBuilderCs) // LUCENENET: Indexing into a StringBuilder is slow, so materialize
                return TryGetValue(stringBuilderCs.Value!.ToString(), out value);

            var val = values[GetSlot(key)];
            if (val != null)
            {
                value = val.Value;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public virtual bool TryGetValue(string key, [NotNullWhen(returnValue: false)] out TValue value)
        {
            var val = values[GetSlot(key)];
            if (val != null)
            {
                value = val.Value!;
                return true;
            }
            value = default!;
            return false;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public virtual bool TryGetValue<T>(T key, [MaybeNullWhen(returnValue: false)] out TValue value)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            // LUCENENET NOTE: Testing for *is* is at least 10x faster
            // than casting using *as* and then checking for null.
            // http://stackoverflow.com/q/1583050/181087
            if (key is string str)
                return TryGetValue(str, out value);
            if (key is char[] charArray)
                return TryGetValue(charArray, 0, charArray.Length, out value);
            if (key is ICharSequence cs)
                return TryGetValue(cs, out value);

            var returnType = CharArrayDictionary.ConvertObjectToChars(key, out char[] chars, out string s);
            if (returnType == CharArrayDictionary.CharReturnType.String)
                return TryGetValue(s, out value);
            else
                return TryGetValue(chars, out value);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <param name="offset">The position of the <paramref name="key"/> where the target key begins.</param>
        /// <param name="length">The total length of the <paramref name="key"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="length"/> is less than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="offset"/> and <paramref name="length"/> refer to a position outside of <paramref name="key"/>.</exception>
        public virtual TValue this[char[] key, int offset, int length]
        {
            get => Get(key, offset, length, throwIfNotFound: true);
            set => Set(key, offset, length, value);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public virtual TValue this[char[] key]
        {
            get => Get(key, throwIfNotFound: true);
            set => Set(key, value);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The <paramref name="key"/>'s <see cref="ICharSequence.HasValue"/> property returns <c>false</c>.
        /// </exception>
        public virtual TValue this[ICharSequence key]
        {
            get => Get(key, throwIfNotFound: true);
            set => Set(key, value);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public virtual TValue this[string key]
        {
            get => Get(key, throwIfNotFound: true);
            set => Set(key, value);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public virtual TValue this[object key]
        {
            get => Get(key, throwIfNotFound: true);
            set => Set(key, value);
        }

        /// <summary>
        /// Gets a collection containing the keys in the <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        public virtual CharArraySet Keys => KeySet;

        ICollection<string> IDictionary<string, TValue>.Keys => KeySet;

        IEnumerable<string> IReadOnlyDictionary<string, TValue>.Keys => KeySet;


        private volatile ValueCollection? valueSet;

        /// <summary>
        /// Gets a collection containing the values in the <see cref="CharArrayDictionary{TValue}"/>.
        /// This specialized collection can be enumerated in order to read its values and 
        /// overrides <see cref="object.ToString()"/> in order to display a string 
        /// representation of the values.
        /// </summary>
        public ValueCollection Values
        {
            get
            {
                if (valueSet is null)
                {
                    valueSet = new ValueCollection(this);
                }
                return valueSet;
            }
        }

        ICollection<TValue> IDictionary<string, TValue>.Values => Values;

        IEnumerable<TValue> IReadOnlyDictionary<string, TValue>.Values => Values;

        #region Nested Class: ValueCollection

        /// <summary>
        /// Class that represents the values in the <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        // LUCENENET specific
        [DebuggerDisplay("Count = {Count}, Values = {ToString()}")]
        public sealed class ValueCollection : ICollection<TValue>, ICollection, IReadOnlyCollection<TValue>
        {
            private readonly CharArrayDictionary<TValue> dictionary;

            public ValueCollection(CharArrayDictionary<TValue> dictionary)
            {
                this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            }

            public int Count => dictionary.Count;

            bool ICollection<TValue>.IsReadOnly => true;

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)dictionary).SyncRoot;

            void ICollection<TValue>.Add(TValue item)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ValueCollectionSet);
            }

            void ICollection<TValue>.Clear()
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ValueCollectionSet);
            }

            [SuppressMessage("Style", "IDE0002:Name can be simplified", Justification = "This is a false warning.")]
            public bool Contains(TValue item)
            {
                for (int i = 0; i < dictionary.values.Length; i++)
                {
                    var value = dictionary.values[i];
                    if (JCG.EqualityComparer<TValue>.Equals(value, item))
                        return true;
                }
                return false;
            }

            public void CopyTo(TValue[] array, int index)
            {
                if (array is null)
                    throw new ArgumentNullException(nameof(array));
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
                if (index > array.Length || dictionary.Count > array.Length - index)
                    throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);

                foreach (var entry in this)
                    array[index++] = entry!;
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array is null)
                    throw new ArgumentNullException(nameof(array));
                if (array.Rank != 1)
                    throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
                if (array.GetLowerBound(0) != 0)
                    throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
                if (array.Length - index < dictionary.Count)
                    throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);

                if (array is TValue[] values)
                {
                    CopyTo(values, index);
                }
                else
                {
                    try
                    {
                        object?[] objects = (object?[])array;
                        foreach (var entry in this)
                            objects[index++] = entry;
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                    }
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            bool ICollection<TValue>.Remove(TValue item)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ValueCollectionSet);
            }

            public override string ToString()
            {
                using var i = GetEnumerator();
                if (!i.HasNext)
                    return "[]";

                StringBuilder sb = new StringBuilder();
                sb.Append('[');
                while (i.MoveNext())
                {
                    TValue? value = i.Current;
                    if (sb.Length > 1)
                    {
                        sb.Append(',').Append(' ');
                    }
                    if (value is not null)
                        sb.Append(value.ToString());
                    else
                        sb.Append("null");
                }

                return sb.Append(']').ToString();
            }

            #region Nested Struct: Enumerator

            /// <summary>
            /// Struct to enumerate the values in the <see cref="ValueCollection"/>.
            /// </summary>
            // LUCENENET specific
            public readonly struct Enumerator : IEnumerator<TValue>, IEnumerator
            {
                private readonly CharArrayDictionary<TValue>.Enumerator entryIterator;

                internal Enumerator(CharArrayDictionary<TValue> dictionary) // LUCENENET specific - marked internal to match .NET collections
                {
                    this.entryIterator = dictionary.GetEnumerator();
                }

                public TValue Current => entryIterator.CurrentValue!;

                object IEnumerator.Current => entryIterator.CurrentValue!;

                public void Dispose()
                {
                    entryIterator.Dispose();
                }

                public bool MoveNext()
                {
                    return entryIterator.MoveNext();
                }

                private void Reset()
                {
                    ((IEnumerator)entryIterator).Reset();
                }

                void IEnumerator.Reset() => Reset();

                internal bool HasNext => entryIterator.HasNext;
            }

            #endregion
        }

        #endregion Nested Class: ValueCollection

        /// <summary>
        /// <c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> is read-only; otherwise <c>false</c>.
        /// </summary>
        public virtual bool IsReadOnly => false;

        #endregion For .NET Support LUCENENET

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }

        IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        ICharArrayDictionaryEnumerator ICharArrayDictionary.GetEnumerator() => GetEnumerator();

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new Enumerator(this, Enumerator.DictEntry);
        }

        void IDictionary.Remove(object key)
        {
            throw UnsupportedOperationException.Create();
        }

        bool IDictionary<string, TValue>.Remove(string key)
        {
            throw UnsupportedOperationException.Create();
        }

        bool ICollection<KeyValuePair<string, TValue>>.Remove(KeyValuePair<string, TValue> item)
        {
            throw UnsupportedOperationException.Create();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
            if (array.GetLowerBound(0) != 0)
                throw new ArgumentException(SR.Arg_NonZeroLowerBound);
            if (index < 0 || index > array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (array.Length - index < Count)
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);

            if (array is KeyValuePair<string, TValue>[] strings)
            {
                CopyTo(strings, index);
            }
            else if (array is KeyValuePair<char[], TValue>[] chars)
            {
                CopyTo(chars, index);
            }
            else if (array is KeyValuePair<ICharSequence, TValue>[] charSequences)
            {
                CopyTo(charSequences, index);
            }
            else if (array is DictionaryEntry[] dictEntryArray)
            {
                foreach (var item in this)
                    dictEntryArray[index++] = new DictionaryEntry(item.Key, item.Value);
            }
            else
            {
                if (!(array is object[] objects))
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType);
                }
                try
                {
                    foreach (var item in this)
                        objects[index++] = item;
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType);
                }
            }
        }

        bool IDictionary.IsFixedSize => false;

        bool IDictionary.IsReadOnly => IsReadOnly;

        ICollection IDictionary.Keys => Keys;

        ICollection IDictionary.Values => Values;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        object? IDictionary.this[object key]
        {
            get => Get(key, throwIfNotFound: false);
            set
            {
                if (key is null)
                    throw new ArgumentNullException(nameof(key));

                if (value is null && default(TValue) != null)
                    throw new ArgumentNullException(nameof(value));

                TValue val;
                try
                {
                    val = (TValue)value!;
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException(string.Format(SR.Arg_WrongType, value, typeof(TValue)), nameof(value));
                }
                var returnType = CharArrayDictionary.ConvertObjectToChars(key, out char[] chars, out string s);
                if (returnType == CharArrayDictionary.CharReturnType.String)
                {
                    Set(s, val);
                }
                else
                {
                    Set(chars, val);
                }
            }
        }

        void IDictionary.Add(object key, object? value)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            TValue val;
            try
            {
                val = (TValue)value!;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(string.Format(SR.Arg_WrongType, value, typeof(TValue)), nameof(value));
            }
            var returnType = CharArrayDictionary.ConvertObjectToChars(key, out char[] chars, out string s);
            if (returnType == CharArrayDictionary.CharReturnType.String)
            {
                Add(s, val);
            }
            else
            {
                Add(chars, val);
            }
        }

        bool IDictionary.Contains(object key) => ContainsKey(key);

        /// <summary>
        /// Gets the number of key/value pairs contained in the <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        public virtual int Count => count;

        /// <summary>
        /// Returns a string that represents the current object. (Inherited from <see cref="object"/>.)
        /// </summary>
        public override string ToString()
        {
            if (count == 0)
                return "{}";

            var sb = new StringBuilder("{");

            using (var iter1 = this.GetEnumerator())
            {
                while (iter1.MoveNext())
                {
                    if (sb.Length > 1)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(iter1.CurrentKey);
                    sb.Append('=');
                    if (iter1.CurrentValue is not null)
                        sb.Append(iter1.CurrentValue);
                    else
                        sb.Append("null");
                }
            }

            return sb.Append('}').ToString();
        }

   

        // LUCENENET: Removed entrySet because in .NET we use the collection itself as the IEnumerable
        private CharArraySet? keySet = null;

        // LUCENENET: Removed entrySet(), createEntrySet() because in .NET we use the collection itself as the IEnumerable

        // LUCENENET: Removed originalKeySet() because we fixed infinite recursion
        // by adding a custom enumerator for KeyCollection.

        /// <summary>
        /// Returns an <see cref="CharArraySet"/> view on the map's keys.
        /// The set will use the same <see cref="matchVersion"/> as this map. 
        /// </summary>
        private CharArraySet KeySet
        {
            get
            {
                if (keySet is null)
                {
                    // prevent adding of entries
                    keySet = new KeyCollection(this);
                }
                return keySet;
            }
        }

        #region Nested Class: KeyCollection

        // LUCENENET: This was an anonymous class in Java
        [DebuggerDisplay("Count = {Count}, Values = {ToString()}")]
        private sealed class KeyCollection : CharArraySet
        {
            internal KeyCollection(CharArrayDictionary<TValue> map)
                : base(map)
            {
            }

            public override bool IsReadOnly => true;

            public override bool Add<T>(T text)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_KeyCollectionSet);
            }
            public override bool Add(ICharSequence text)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_KeyCollectionSet);
            }
            public override bool Add(string text)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_KeyCollectionSet);
            }
            public override bool Add(char[] text)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_KeyCollectionSet);
            }
        }

        #endregion Nested Class: KeyCollection

        #region Nested Class: Enumerator

        /// <summary>
        /// Public enumerator class so efficient properties are exposed to users.
        /// </summary>
        // LUCENENET: An attempt was made to make this into a struct, but since it has mutable state that didn't work. So, this should remain a class.
        public sealed class Enumerator : IEnumerator<KeyValuePair<string, TValue>>, IDictionaryEnumerator, ICharArrayDictionaryEnumerator
        {
            private readonly CharArrayDictionary<TValue> dictionary;
            private readonly int getEnumeratorRetType;  // What should Enumerator.Current return?

            internal const int KeyValuePair = 1;
            internal const int DictEntry = 2;

            internal int pos = -1;
            internal int lastPos;
            internal readonly bool allowModify;

            private int version; // LUCENENET specific - track when the enumerator is broken by mutating the state of the original collection
            private bool notStartedOrEnded; // LUCENENET specific

            internal Enumerator(CharArrayDictionary<TValue> dictionary, int getEnumeratorRetType)
            {
                this.dictionary = dictionary;
                this.getEnumeratorRetType = getEnumeratorRetType;
                this.version = dictionary.version;
                this.allowModify = !dictionary.IsReadOnly;
                this.notStartedOrEnded = true;
                GoNext();
            }

            private void GoNext()
            {
                lastPos = pos;
                pos++;
                while (pos < dictionary.keys.Length && dictionary.keys[pos] is null)
                {
                    pos++;
                }
            }

            internal bool HasNext => pos < dictionary.keys.Length;

            /// <summary>
            /// Gets the current key as a <see cref="CharArrayCharSequence"/>.
            /// </summary>
            // LUCENENET specific - quick access to ICharSequence interface
            public ICharSequence CurrentKeyCharSequence
            {
                get
                {
                    if (notStartedOrEnded)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);

                    char[] key = dictionary.keys[lastPos];
                    if (key is null)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    return key.AsCharSequence();
                }
            }

            /// <summary>
            /// Gets the current key... do not modify the returned char[]
            /// </summary>
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            [WritableArray]
            public char[] CurrentKey
            {
                get
                {
                    if (notStartedOrEnded)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);

                    char[] key = dictionary.keys[lastPos];
                    if (key is null)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    return key;
                }
            }

            /// <summary>
            /// Gets the current key as a newly created <see cref="string"/> object.
            /// </summary>
            public string CurrentKeyString
            {
                get
                {
                    if (notStartedOrEnded)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);

                    char[] key = dictionary.keys[lastPos];
                    if (key is null)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    return new string(key);
                }
            }

            /// <summary>
            /// Gets the value associated with the current key.
            /// </summary>
            [MaybeNull]
            public TValue CurrentValue
            {
                get
                {
                    if (notStartedOrEnded)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    char[] key = dictionary.keys[lastPos];
                    if (key is null)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);

                    var val = dictionary.values[lastPos];
                    return val != null ? val.Value : default;
                }
            }

            /// <summary>
            /// Sets the value associated with the current key.
            /// </summary>
            /// <returns>Returns the value prior to the update.</returns>
            [return: MaybeNull]
            public TValue SetValue(TValue value)
            {
                if (notStartedOrEnded)
                    throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                if (!allowModify)
                    throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);

                MapValue current = dictionary.values[lastPos];
                if (current is null)
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);

                TValue? old = current.Value;
                // LUCENENET specific - increment the versions of both this enumerator
                // and the original collection so only this enumerator instance isn't broken.
                dictionary.version++;
                version++;
                current.Value = value;
                return old;
            }

            // LUCENENET: Next() and Remove() methods eliminated here

            #region Added for better .NET support LUCENENET
            public void Dispose()
            {
                // nothing to do
            }

            public bool MoveNext()
            {
                if (version != dictionary.version)
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);

                if (!HasNext)
                {
                    notStartedOrEnded = true;
                    return false;
                }
                notStartedOrEnded = false;
                GoNext();
                return true;
            }

            private void Reset()
            {
                if (version != dictionary.version)
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);

                pos = -1;
                notStartedOrEnded = true;
                GoNext();
            }

            void IEnumerator.Reset() => Reset();

            void ICharArrayDictionaryEnumerator.Reset() => Reset();

            public KeyValuePair<string, TValue> Current
            {
                get
                {
                    if (notStartedOrEnded)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);

                    char[] key = dictionary.keys[lastPos];
                    if (key is null)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    MapValue value = dictionary.values[lastPos];
                    return new KeyValuePair<string, TValue>(new string(key), value.Value!);
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (notStartedOrEnded)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);

                    char[] key = dictionary.keys[lastPos];
                    if (key is null)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    MapValue value = dictionary.values[lastPos];

                    if (getEnumeratorRetType == DictEntry)
                    {
                        return new DictionaryEntry(new string(key), value.Value);
                    }
                    else
                    {
                        return new KeyValuePair<string, TValue>(new string(key), value.Value!);
                    }
                }
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (notStartedOrEnded)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);

                    return Current.Key;
                }
            }

            object? IDictionaryEnumerator.Value
            {
                get
                {
                    if (notStartedOrEnded)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);

                    return Current.Value;
                }
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (notStartedOrEnded)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);

                    return new DictionaryEntry(Current.Key, Current.Value);
                }
            }

            bool ICharArrayDictionaryEnumerator.NotStartedOrEnded => notStartedOrEnded;

            #endregion
        }

        #endregion Nested Class: Enumerator

        // LUCENENET NOTE: The Java Lucene type MapEntry was removed here because it is not possible 
        // to inherit the value type KeyValuePair{TKey, TValue} in .NET.

        // LUCENENET: EntrySet class removed because in .NET we get the entries by calling GetEnumerator() on the dictionary.

        // LUCENENET: Moved UnmodifiableMap static methods to CharArrayDictionary class

        // LUCENENET: Moved Copy static methods to CharArrayDictionary class

        // LUCENENET: Removed EmptyMap() - use Empty instead

        // LUCENENET: Moved UnmodifiableCharArraymap to CharArrayDictionary class

        // LUCENENET: Moved EmptyCharArrayDictionary to CharArrayDictionary class
    }

    /// <summary>
    /// LUCENENET specific interface used so <see cref="CharArraySet"/>
    /// can hold a reference to <see cref="CharArrayDictionary{TValue}"/> without
    /// knowing its generic closing type for TValue.
    /// </summary>
    internal interface ICharArrayDictionary
    {
        void Clear();
        bool ContainsKey(char[] text, int offset, int length);
        bool ContainsKey(char[] text);
        bool ContainsKey<T>(T text);
        bool ContainsKey(string text);
        bool ContainsKey(ICharSequence text);
        int Count { get; }
        bool IgnoreCase { get; }
        bool IsReadOnly { get; }
        LuceneVersion MatchVersion { get; }
        bool Put(char[] text);
        bool Put(ICharSequence text);
        bool Put<T>(T text);
        bool Put(string text);
        void Set(char[] text);
        void Set(ICharSequence text);
        void Set<T>(T text);
        void Set(string text);
        ICharArrayDictionaryEnumerator GetEnumerator();
    }

    /// <summary>
    /// LUCENENET specific interface used so <see cref="CharArraySet"/> can
    /// iterate the keys of <see cref="CharArrayDictionary{TValue}"/> without
    /// knowing its generic closing type for TValue.
    /// </summary>
    internal interface ICharArrayDictionaryEnumerator : IDisposable
    {
        bool NotStartedOrEnded { get; }
        bool MoveNext();
        ICharSequence CurrentKeyCharSequence { get; }
        string CurrentKeyString { get; }
        char[] CurrentKey { get; }
        void Reset();
    }

    public static class CharArrayDictionary // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        /// <summary>
        /// Returns a copy of the given map as a <see cref="CharArrayDictionary{TValue}"/>. If the given map
        /// is a <see cref="CharArrayDictionary{TValue}"/> the ignoreCase property will be preserved.
        /// <para>
        /// <b>Note:</b> If you intend to create a copy of another <see cref="CharArrayDictionary{TValue}"/> where
        /// the <see cref="LuceneVersion"/> of the source map differs from its copy
        /// <see cref="CharArrayDictionary{TValue}.CharArrayDictionary(LuceneVersion, IDictionary{string, TValue}, bool)"/> should be used instead.
        /// The <see cref="Copy{TValue}(LuceneVersion, IDictionary{string, TValue})"/> will preserve the <see cref="LuceneVersion"/> of the
        /// source map if it is an instance of <see cref="CharArrayDictionary{TValue}"/>.
        /// </para>
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. This argument will be ignored if the
        ///          given map is a <see cref="CharArrayDictionary{TValue}"/>. </param>
        /// <param name="map">
        ///          a map to copy </param>
        /// <returns> a copy of the given map as a <see cref="CharArrayDictionary{TValue}"/>. If the given map
        ///         is a <see cref="CharArrayDictionary{TValue}"/> the ignoreCase property as well as the
        ///         <paramref name="matchVersion"/> will be of the given map will be preserved. </returns>
        public static CharArrayDictionary<TValue> Copy<TValue>(LuceneVersion matchVersion, IDictionary<string, TValue> map)
        {
            if (map == CharArrayDictionary<TValue>.Empty)
            {
                return CharArrayDictionary<TValue>.Empty;
            }

            if (map is CharArrayDictionary<TValue> m)
            {
                // use fast path instead of iterating all values
                // this is even on very small sets ~10 times faster than iterating
                var keys = new char[m.keys.Length][];
                m.keys.AsSpan().CopyTo(keys.AsSpan());
                var values = new CharArrayDictionary<TValue>.MapValue[m.values.Length];
                m.values.AsSpan().CopyTo(values.AsSpan());
                m = new CharArrayDictionary<TValue>(m) { keys = keys, values = values };
                return m;
            }
            return new CharArrayDictionary<TValue>(matchVersion, map, false);
        }

        /// <summary>
        /// Used by <see cref="CharArraySet"/> to copy <see cref="CharArrayDictionary{TValue}"/> without knowing 
        /// its generic type.
        /// </summary>
        internal static CharArrayDictionary<TValue> Copy<TValue>(LuceneVersion matchVersion, [DisallowNull] ICharArrayDictionary map)
        {
            return Copy(matchVersion, (IDictionary<string, TValue>)map);
        }

        /// <summary>
        /// Returns an unmodifiable <see cref="CharArrayDictionary{TValue}"/>. This allows to provide
        /// unmodifiable views of internal map for "read-only" use.
        /// </summary>
        /// <param name="map">
        ///          a map for which the unmodifiable map is returned. </param>
        /// <returns> an new unmodifiable <see cref="CharArrayDictionary{TValue}"/>. </returns>
        /// <exception cref="ArgumentException">
        ///           if the given map is <c>null</c>. </exception>
        [Obsolete("Use the CharArrayDictionary<TValue>.AsReadOnly() instance method instead. This method will be removed in 4.8.0 release candidate."), EditorBrowsable(EditorBrowsableState.Never)]
        public static CharArrayDictionary<TValue> UnmodifiableMap<TValue>(CharArrayDictionary<TValue> map)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            if (map == CharArrayDictionary<TValue>.Empty || map.Count == 0)
            {
                return CharArrayDictionary<TValue>.Empty;
            }
            if (map is CharArrayDictionary.UnmodifiableCharArrayDictionary<TValue>)
            {
                return map;
            }
            return new CharArrayDictionary.UnmodifiableCharArrayDictionary<TValue>(map);
        }

        /// <summary>
        /// Used by <see cref="CharArraySet"/> to create an <see cref="UnmodifiableCharArrayDictionary{TValue}"/> instance
        /// without knowing the type of <typeparamref name="TValue"/>.
        /// </summary>
        internal static ICharArrayDictionary UnmodifiableMap<TValue>(ICharArrayDictionary map)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            if (map == CharArrayDictionary<TValue>.Empty || map.Count == 0)
            {
                return CharArrayDictionary<TValue>.Empty;
            }
            if (map is CharArrayDictionary.UnmodifiableCharArrayDictionary<TValue>)
            {
                return map;
            }
            return new CharArrayDictionary.UnmodifiableCharArrayDictionary<TValue>(map);
        }

        #region Nested Class: UnmodifiableCharArrayDictionary<TValue>

        // package private CharArraySet instanceof check in CharArraySet
        internal class UnmodifiableCharArrayDictionary<TValue> : CharArrayDictionary<TValue>
        {
            public UnmodifiableCharArrayDictionary([DisallowNull] CharArrayDictionary<TValue> map)
                : base(map)
            { }

            public UnmodifiableCharArrayDictionary([DisallowNull] ICharArrayDictionary map)
                : base((CharArrayDictionary<TValue>)map)
            { }

            public override void Clear()
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override bool Put(char[] text, TValue value, [MaybeNullWhen(true)] out TValue previousValue)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override bool Put(ICharSequence text, TValue val, [MaybeNullWhen(true)] out TValue previousValue)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override bool Put(string text, TValue val, [MaybeNullWhen(true)] out TValue previousValue)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override bool Put<T>(T text, TValue val, [MaybeNullWhen(true)] out TValue previousValue)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override bool Put(char[] text)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override bool Put(ICharSequence text)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override bool Put(string text)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override bool Put<T>(T text)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            // LUCENENET: Removed CreateEntrySet() method - we use IsReadOnly to control whether it can be written to

            #region Added for better .NET support LUCENENET

            public override bool IsReadOnly => true;

            public override void Add(string key, TValue value)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }
            public override void Add(KeyValuePair<string, TValue> item)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override TValue this[char[] key, int offset, int length]
            {
                get => base[key, offset, length];
                set => throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override TValue this[char[] key]
            {
                get => base[key];
                set => throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override TValue this[ICharSequence key]
            {
                get => base[key];
                set => throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override TValue this[string key]
            {
                get => base[key];
                set => throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override TValue this[object key]
            {
                get => base[key];
                set => throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override void PutAll(IDictionary<string, TValue> collection)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override void PutAll(IDictionary<char[], TValue> collection)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override void PutAll(IDictionary<ICharSequence, TValue> collection)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override void PutAll<T>(IDictionary<T, TValue> collection)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override void PutAll(IEnumerable<KeyValuePair<string, TValue>> collection)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override void PutAll(IEnumerable<KeyValuePair<char[], TValue>> collection)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override void PutAll(IEnumerable<KeyValuePair<ICharSequence, TValue>> collection)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            public override void PutAll<T>(IEnumerable<KeyValuePair<T, TValue>> collection)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            internal override void Set(char[] text)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            internal override void Set(char[] text, int offset, int length, TValue? value)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            internal override void Set(char[] text, TValue? value)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            internal override void Set(ICharSequence text)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            internal override void Set(ICharSequence text, TValue? value)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            internal override void Set(string text)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            internal override void Set(string text, TValue? value)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            internal override void Set<T>(T text)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            internal override void Set<T>(T text, TValue? value)
            {
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
            }

            #endregion
        }

        #endregion

        #region Nested Class: EmptyCharArrayDictionary<V>

        /// <summary>
        /// Empty <see cref="UnmodifiableCharArrayDictionary{V}"/> optimized for speed.
        /// Contains checks will always return <c>false</c> or throw
        /// NPE if necessary.
        /// </summary>
        internal class EmptyCharArrayDictionary<V> : UnmodifiableCharArrayDictionary<V>
        {
            public EmptyCharArrayDictionary()
#pragma warning disable 612, 618
                : base(new CharArrayDictionary<V>(LuceneVersion.LUCENE_CURRENT, 0, false))
#pragma warning restore 612, 618
            {
            }

            public override bool ContainsKey(char[] text, int offset, int length)
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(text));

                return false;
            }

            public override bool ContainsKey(char[] text)
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(text));

                return false;
            }

            public override bool ContainsKey(ICharSequence text)
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(text));

                return false;
            }

            public override bool ContainsKey<T>(T text)
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(text));

                return false;
            }

            internal override V Get(char[] text, int offset, int length, bool throwIfNotfound = true)
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(text));

                if (throwIfNotfound)
                    throw new KeyNotFoundException(string.Format(SR.Arg_KeyNotFoundWithKey, new string(text, offset, length)));
                return default!;
            }

            internal override V Get(char[] text, bool throwIfNotfound = true)
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(text));

                if (throwIfNotfound)
                    throw new KeyNotFoundException(string.Format(SR.Arg_KeyNotFoundWithKey, new string(text)));
                return default!;
            }

            internal override V Get(ICharSequence text, bool throwIfNotfound = true)
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(text));

                if (throwIfNotfound)
                    throw new KeyNotFoundException(string.Format(SR.Arg_KeyNotFoundWithKey, text));
                return default!;
            }

            internal override V Get<T>(T text, bool throwIfNotfound = true)
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(text));

                if (throwIfNotfound)
                    throw new KeyNotFoundException(string.Format(SR.Arg_KeyNotFoundWithKey, text));
                return default!;
            }
        }

        #endregion

        private readonly static CultureInfo invariant = CultureInfo.InvariantCulture;
        private const string TrueString = "true";
        private const string FalseString = "false";


        internal enum CharReturnType
        {
            String = 0,
            CharArray = 1,
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static CharReturnType ConvertObjectToChars<T>(T key, out char[] chars, out string str)
        {
#if FEATURE_SPANFORMATTABLE
            Span<char> buffer = stackalloc char[256];
#else
            Span<char> buffer = stackalloc char[1];
#endif
            return ConvertObjectToChars(key, out chars, out str, buffer);
        }


        // LUCENENET: We need value types to be represented using the invariant
        // culture, so it is consistent regardless of the current culture.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static CharReturnType ConvertObjectToChars<T>(T key, out char[] chars, out string str, Span<char> reuse)
        {
            chars = Arrays.Empty<char>();
            str = string.Empty;

            if (key is null)
            {
                return CharReturnType.CharArray;
            }

            // Handle special cases
            if (key is string strResult)
            {
                str = strResult;
                return CharReturnType.String;
            }
            else if (key is char[] charArray)
            {
                chars = charArray;
                return CharReturnType.CharArray;
            }
            else if (key is IList<char> charList)
            {
                char[] result = new char[charList.Count];
                charList.CopyTo(result, arrayIndex: 0);
                chars = result;
                return CharReturnType.CharArray;
            }
            else if (key is StringBuilder stringBuilder)
            {
                char[] result = new char[stringBuilder.Length];
                stringBuilder.CopyTo(sourceIndex: 0, result, destinationIndex: 0, stringBuilder.Length);
                chars = result;
                return CharReturnType.CharArray;
            }

            // ICharSequence types
            else if (key is StringCharSequence strCs)
            {
                str = strCs.Value ?? string.Empty;
                return CharReturnType.String;
            }
            else if (key is CharArrayCharSequence charArrayCs)
            {
                chars = charArrayCs.Value ?? Arrays.Empty<char>();
                return CharReturnType.CharArray;
            }
            else if (key is StringBuilderCharSequence stringBuilderCs && stringBuilderCs.HasValue)
            {
                var sb = stringBuilderCs.Value!;
                char[] result = new char[sb.Length];
                sb.CopyTo(sourceIndex: 0, result, destinationIndex: 0, sb.Length);
                chars = result;
                return CharReturnType.CharArray;
            }
            else if (key is ICharSequence cs && cs.HasValue)
            {
                int length = cs.Length;
                char[] result = new char[length];
                for (int i = 0; i < length; i++)
                {
                    result[i] = cs[i];
                }
                chars = result;
                return CharReturnType.CharArray;
            }

            // These must be done prior to checking ISpanFormattable and IFormattable
            else if (key is bool b)
            {
                str = b ? TrueString : FalseString;
                return CharReturnType.String;
            }
            else if (key is double d)
            {
                str = J2N.Numerics.Double.ToString(d, invariant);
                return CharReturnType.String;
            }
            else if (key is float f)
            {
                str = J2N.Numerics.Single.ToString(f, invariant);
                return CharReturnType.String;
            }
            else if (key is J2N.Numerics.Number number)
            {
                str = number.ToString(invariant);
                return CharReturnType.String;
            }

#if FEATURE_SPANFORMATTABLE
            else if (key is ISpanFormattable spanFormattable &&
                spanFormattable.TryFormat(reuse, out int charsWritten, string.Empty.AsSpan(), invariant))
            {
                chars = reuse.Slice(0, charsWritten).ToArray();
                return CharReturnType.CharArray;
            }
#endif
            else if (key is IFormattable formattable)
            {
                str = formattable.ToString(string.Empty, invariant);
                return CharReturnType.String;
            }

            using var context = new CultureContext(invariant);
            str = key.ToString() ?? string.Empty;
            return CharReturnType.String;
        }
    }

    /// <summary>
    /// Extensions to <see cref="IDictionary{TKey, TValue}"/> for <see cref="CharArrayDictionary{TValue}"/>.
    /// </summary>
    // LUCENENET specific - allow .NET-like syntax for copying CharArrayDictionary
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Returns a copy of the current <see cref="IDictionary{TKey, TValue}"/> as a <see cref="CharArrayDictionary{TValue}"/>
        /// using the specified <paramref name="matchVersion"/> value.
        /// </summary>
        /// <typeparam name="TValue">The type of dictionary value.</typeparam>
        /// <param name="dictionary">
        ///          A <see cref="IDictionary{TKey, TValue}"/> to copy. </param>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. </param>
        /// <returns> A copy of the current dictionary as a <see cref="CharArrayDictionary{TValue}"/>. </returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        public static CharArrayDictionary<TValue> ToCharArrayDictionary<TValue>(this IDictionary<string, TValue> dictionary, LuceneVersion matchVersion)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return CharArrayDictionary.Copy<TValue>(matchVersion, dictionary);
        }

        /// <summary>
        /// Returns a copy of the current <see cref="IDictionary{TKey, TValue}"/> as a <see cref="CharArrayDictionary{TValue}"/>
        /// using the specified <paramref name="matchVersion"/> and <paramref name="ignoreCase"/> values.
        /// </summary>
        /// <typeparam name="TValue">The type of dictionary value.</typeparam>
        /// <param name="dictionary">
        ///          A <see cref="IDictionary{TKey, TValue}"/> to copy. </param>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. </param>
        /// <param name="ignoreCase"><c>false</c> if and only if the set should be case sensitive otherwise <c>true</c>.</param>
        /// <returns> A copy of the current dictionary as a <see cref="CharArrayDictionary{TValue}"/>. </returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        public static CharArrayDictionary<TValue> ToCharArrayDictionary<TValue>(this IDictionary<string, TValue> dictionary, LuceneVersion matchVersion, bool ignoreCase)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return new CharArrayDictionary<TValue>(matchVersion, dictionary.Count, ignoreCase);
        }
    }

    /// <summary>
    /// LUCENENET specific. Just a class to make error messages easier to manage in one place.
    /// Ideally, these would be in resources so they can be localized (eventually), but at least
    /// this half-measure will make that somewhat easier to do and is guaranteed not to cause
    /// performance issues.
    /// </summary>
    internal static class SR
    {
        public const string Arg_ArrayPlusOffTooSmall = "Destination array is not long enough to copy all the items in the collection. Check array index and length.";
        public const string Arg_KeyNotFoundWithKey = "The given key '{0}' was not present in the dictionary.";
        public const string Arg_NonZeroLowerBound = "The lower bound of target array must be zero.";
        public const string Arg_RankMultiDimNotSupported = "Only single dimensional arrays are supported for the requested action.";
        public const string Arg_WrongType = "The value '{0}' is not of type '{1}' and cannot be used in this generic collection.";

        public const string Argument_AddingDuplicate = "An item with the same key has already been added. Key: {0}";
        public const string Argument_InvalidArrayType = "Target array type is not compatible with the type of items in the collection.";

        public const string ArgumentOutOfRange_IndexLength = "Index and length must refer to a location within the string.";
        public const string ArgumentOutOfRange_NeedNonNegNum = "Non-negative number required.";

        public const string InvalidOperation_EnumFailedVersion = "Collection was modified after the enumerator was instantiated.";
        public const string InvalidOperation_EnumOpCantHappen = "Enumeration has either not started or has already finished.";

        public const string NotSupported_KeyCollectionSet = "Mutating a key collection derived from a dictionary is not allowed.";
        public const string NotSupported_ReadOnlyCollection = "Collection is read-only.";
        public const string NotSupported_ValueCollectionSet = "Mutating a value collection derived from a dictionary is not allowed.";
    }
}
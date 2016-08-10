﻿using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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
    /// A simple class that stores key Strings as char[]'s in a
    /// hash table. Note that this is not a general purpose
    /// class.  For example, it cannot remove items from the
    /// map, nor does it resize its hash table to be smaller,
    /// etc.  It is designed to be quick to retrieve items
    /// by char[] keys without the necessity of converting
    /// to a String first.
    /// 
    /// <a name="version"></a>
    /// <para>You must specify the required <seealso cref="Version"/>
    /// compatibility when creating <seealso cref="CharArrayMap"/>:
    /// <ul>
    ///   <li> As of 3.1, supplementary characters are
    ///       properly lowercased.</li>
    /// </ul>
    /// Before 3.1 supplementary characters could not be
    /// lowercased correctly due to the lack of Unicode 4
    /// support in JDK 1.4. To use instances of
    /// <seealso cref="CharArrayMap"/> with the behavior before Lucene
    /// 3.1 pass a <seealso cref="Version"/> &lt; 3.1 to the constructors.
    /// </para>
    /// </summary>
    public class CharArrayMap<V> : IDictionary<object, V>
    {
        private const int INIT_SIZE = 8;
        private readonly CharacterUtils charUtils;
        private readonly bool ignoreCase;
        private int count;
        internal readonly LuceneVersion matchVersion; // package private because used in CharArraySet
        internal char[][] keys; // package private because used in CharArraySet's non Set-conform CharArraySetIterator
        internal V[] values; // package private because used in CharArraySet's non Set-conform CharArraySetIterator

        /// <summary>
        /// LUCENENET: Added in .NET to prevent infinite recursion when accessing the Keys collection
        /// </summary>
        internal IDictionary<object, V> innerDictionary = new Dictionary<object, V>();

        /// <summary>
        /// Create map with enough capacity to hold startSize terms
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. </param>
        /// <param name="startSize">
        ///          the initial capacity </param>
        /// <param name="ignoreCase">
        ///          <code>false</code> if and only if the set should be case sensitive
        ///          otherwise <code>true</code>. </param>
        public CharArrayMap(LuceneVersion matchVersion, int startSize, bool ignoreCase)
        {
            this.ignoreCase = ignoreCase;
            var size = INIT_SIZE;
            while (startSize + (startSize >> 2) > size)
            {
                size <<= 1;
            }
            keys = new char[size][];
            values = new V[size];
            this.charUtils = CharacterUtils.GetInstance(matchVersion);
            this.matchVersion = matchVersion;
        }

        /// <summary>
        /// Creates a map from the mappings in another map. 
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. </param>
        /// <param name="c">
        ///          a map whose mappings to be copied </param>
        /// <param name="ignoreCase">
        ///          <code>false</code> if and only if the set should be case sensitive
        ///          otherwise <code>true</code>. </param>
        public CharArrayMap(LuceneVersion matchVersion, IDictionary<object, V> c, bool ignoreCase)
            : this(matchVersion, c.Count, ignoreCase)
        {
            foreach (var v in c)
            {
                Add(v);
            }
        }

        /// <summary>
        /// LUCENENET specific, in order to support the KeySet functionality.
        /// </summary>
        /// <param name="keys"></param>
        private CharArrayMap(char[][] keys, V[] values, bool ignoreCase, int count, CharacterUtils charUtils, LuceneVersion matchVersion, Dictionary<object, V> innerDictionary)
        {
            this.keys = keys;
            this.values = values;
            this.ignoreCase = ignoreCase;
            this.count = count;
            this.charUtils = charUtils;
            this.matchVersion = matchVersion;

            foreach (var kvp in innerDictionary)
            {
                this.innerDictionary[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Create set from the supplied map (used internally for readonly maps...) </summary>
        internal CharArrayMap(CharArrayMap<V> toCopy)
        {
            this.keys = toCopy.keys;
            this.values = toCopy.values;
            this.ignoreCase = toCopy.ignoreCase;
            this.count = toCopy.count;
            this.charUtils = toCopy.charUtils;
            this.matchVersion = toCopy.matchVersion;

            foreach (var kvp in toCopy.innerDictionary)
            {
                this.innerDictionary[kvp.Key] = kvp.Value;
            }
        }

        public virtual void Add(KeyValuePair<object, V> item)
        {
            Put(item.Key, item.Value);
        }

        public virtual void Add(object key, V value)
        {
            Put(key, value);
        }

        /// <summary>
        /// Clears all entries in this map. This method is supported for reusing, but not <seealso cref="Map#remove"/>. </summary>
        public virtual void Clear()
        {
            count = 0;
            Arrays.Fill(keys, null);
            Arrays.Fill(values, default(V));
            innerDictionary.Clear();
        }

        public virtual bool Contains(KeyValuePair<object, V> item)
        {
            throw new NotImplementedException();
        }

        public virtual void CopyTo(KeyValuePair<object, V>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// true if the <code>len</code> chars of <code>text</code> starting at <code>off</code>
        /// are in the <seealso cref="#keySet()"/> 
        /// </summary>
        public virtual bool ContainsKey(char[] text, int off, int len)
        {
            return keys[GetSlot(text, off, len)] != null;
        }

        /// <summary>
        /// true if the <code>CharSequence</code> is in the <seealso cref="#keySet()"/> </summary>
        public virtual bool ContainsKey(string cs)
        {
            return keys[GetSlot(cs)] != null;
        }

        public virtual bool ContainsKey(object o)
        {
            if (o == null)
            {
                throw new ArgumentException("o can't be null", "o");
            }

            var c = o as char[];
            if (c != null)
            {
                var text = c;
                return ContainsKey(text, 0, text.Length);
            }
            return ContainsKey(o.ToString());
        }

        /// <summary>
        /// returns the value of the mapping of <code>len</code> chars of <code>text</code>
        /// starting at <code>off</code> 
        /// </summary>
        public virtual V Get(char[] text, int off, int len)
        {
            return values[GetSlot(text, off, len)];
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this {@code CharSequence} </summary>
        public virtual V Get(string cs)
        {
            return values[GetSlot(cs)];
        }

        public virtual V Get(object o)
        {
            var text = o as char[];
            if (text != null)
            {
                return Get(text, 0, text.Length);
            }
            return Get(o.ToString());
        }

        private int GetSlot(char[] text, int off, int len)
        {
            int code = GetHashCode(text, off, len);
            int pos = code & (keys.Length - 1);
            char[] text2 = keys[pos];
            if (text2 != null && !Equals(text, off, len, text2))
            {
                int inc = ((code >> 8) + code) | 1;
                do
                {
                    code += inc;
                    pos = code & (keys.Length - 1);
                    text2 = keys[pos];
                } while (text2 != null && !Equals(text, off, len, text2));
            }
            return pos;
        }

        /// <summary>
        /// Returns true if the String is in the set </summary>
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

        /// <summary>
        /// Add the given mapping. </summary>
        public virtual V Put(ICharSequence text, V value)
        {
            return Put(text.ToString(), value); // could be more efficient
        }

        public virtual V Put(object o, V value)
        {
            var c = o as char[];
            if (c != null)
            {
                return Put(c, value);
            }
            return Put(o.ToString(), value);
        }

        /// <summary>
        /// Add the given mapping. </summary>
        public virtual V Put(string text, V value)
        {
            return Put(text.ToCharArray(), value);
        }

        /// <summary>
        /// Add the given mapping.
        /// If ignoreCase is true for this Set, the text array will be directly modified.
        /// The user should never modify this text array after calling this method.
        /// </summary>
        public virtual V Put(char[] text, V value)
        {
            if (ignoreCase)
            {
                charUtils.ToLower(text, 0, text.Length);
            }
            int slot = GetSlot(text, 0, text.Length);
            if (keys[slot] != null)
            {
                V oldValue = values[slot];
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

            innerDictionary[text] = value;

            return default(V);
        }

        private void Rehash()
        {
            Debug.Assert(keys.Length == values.Length);

            innerDictionary.Clear();

            int newSize = 2 * keys.Length;
            char[][] oldkeys = keys;
            V[] oldvalues = values;
            keys = new char[newSize][];
            values = new V[newSize];

            for (int i = 0; i < oldkeys.Length; i++)
            {
                char[] text = oldkeys[i];
                if (text != null)
                {
                    // todo: could be faster... no need to compare strings on collision
                    int slot = GetSlot(text, 0, text.Length);
                    keys[slot] = text;
                    values[slot] = oldvalues[i];

                    innerDictionary.Add(text, oldvalues[i]);
                }
            }
        }

        private bool Equals(char[] text1, int off, int len, char[] text2)
        {
            if (len != text2.Length)
            {
                return false;
            }
            int limit = off + len;
            if (ignoreCase)
            {
                for (int i = 0; i < len;)
                {
                    var codePointAt = charUtils.CodePointAt(text1, off + i, limit);
                    if (Character.ToLowerCase(codePointAt) != charUtils.CodePointAt(text2, i, text2.Length))
                    {
                        return false;
                    }
                    i += Character.CharCount(codePointAt);
                }
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    if (text1[off + i] != text2[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool Equals(string text1, char[] text2)
        {
            int len = text1.Length;
            if (len != text2.Length)
            {
                return false;
            }
            if (ignoreCase)
            {
                for (int i = 0; i < len;)
                {
                    int codePointAt = charUtils.CodePointAt(text1, i);
                    if (Character.ToLowerCase(codePointAt) != charUtils.CodePointAt(text2, i, text2.Length))
                    {
                        return false;
                    }
                    i += Character.CharCount(codePointAt);
                }
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    if (text1[i] != text2[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private int GetHashCode(char[] text, int offset, int len)
        {
            if (text == null)
            {
                throw new ArgumentException("text can't be null", "text");
            }
            int code = 0;
            int stop = offset + len;
            if (ignoreCase)
            {
                for (int i = offset; i < stop;)
                {
                    int codePointAt = charUtils.CodePointAt(text, i, stop);
                    code = code * 31 + Character.ToLowerCase(codePointAt);
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

        private int GetHashCode(string text)
        {
            if (text == null)
            {
                throw new ArgumentException("text can't be null", "text");
            }

            int code = 0;
            int len = text.Length;
            if (ignoreCase)
            {
                for (int i = 0; i < len;)
                {
                    int codePointAt = charUtils.CodePointAt(text, i);
                    code = code * 31 + Character.ToLowerCase(codePointAt);
                    i += Character.CharCount(codePointAt);
                }
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    code = code * 31 + text[i];
                }
            }
            return code;
        }

        #region For .NET Support LUCENENET

        public virtual bool TryGetValue(object key, out V value)
        {
            throw new NotImplementedException();
        }

        public virtual V this[object key]
        {
            get { return Get(key); }
            set { Put(key, value); }
        }

        public virtual ICollection<object> Keys { get { return KeySet(); } }

        public ICollection<V> Values { get { return values; } }

        #endregion

        public virtual bool IsReadOnly { get; private set; }

        public virtual IEnumerator<KeyValuePair<object, V>> GetEnumerator()
        {
            return new EntryIterator(this, false);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        public virtual bool Remove(object key)
        {
            throw new NotSupportedException();
        }

        public virtual bool Remove(KeyValuePair<object, V> item)
        {
            throw new NotSupportedException();
        }

        public virtual int Count
        {
            get { return count; }
        }

        public override string ToString()
        {
            var sb = new StringBuilder("{");

            IEnumerator<KeyValuePair<object, V>> iter1 = IDictionaryExtensions.EntrySet(this).GetEnumerator();
            while (iter1.MoveNext())
            {
                KeyValuePair<object, V> entry = iter1.Current;
                if (sb.Length > 1)
                {
                    sb.Append(", ");
                }
                if (entry.Key.GetType().Equals(typeof(char[])))
                {
                    sb.Append(new string((char[])entry.Key));
                }
                else
                {
                    sb.Append(entry.Key);
                }
                sb.Append("=");
                sb.Append(entry.Value);
            }

            return sb.Append('}').ToString();
        }

        private EntrySet_ entrySet = null;
        private CharArraySet keySet = null;

        internal virtual EntrySet_ CreateEntrySet()
        {
            return new EntrySet_(this, true);
        }

        public EntrySet_ EntrySet()
        {
            if (entrySet == null)
            {
                entrySet = CreateEntrySet();
            }
            return entrySet;
        }

        // helper for CharArraySet to not produce endless recursion
        internal IEnumerable<object> OriginalKeySet()
        {
            return innerDictionary.Keys;
        }

        /// <summary>
        /// Returns an <seealso cref="CharArraySet"/> view on the map's keys.
        /// The set will use the same {@code matchVersion} as this map. 
        /// </summary>
        private CharArraySet KeySet()
        {
            if (keySet == null)
            {
                // prevent adding of entries

                // LUCENENET TODO: Should find a way to pass a direct reference to this object to
                // CharArraySet's constructor. The only known side effect this causes is to make
                // the ToString test fail when calling it on the Keys property after changing
                // the main set, so perhaps this is not a huge issue.

                // Then again, any derived classes of this one could have serious issues without
                // the direct reference.

                // A possible solution is to create an interface with no generics that this class 
                // implements to be passed into the CharArraySet's constructor. Another possible 
                // solution is to make the CharArraySet class generic so the inner type can be 
                // inferred at compile time.
                var innerDictionaryObj = new Dictionary<object, object>();
                foreach (var kvp in this.innerDictionary)
                {
                    innerDictionaryObj.Add(kvp.Key, kvp.Value);
                }
                var copy = new CharArrayMap<object>(this.keys, this.values.Cast<object>().ToArray(), 
                    this.ignoreCase, this.count, this.charUtils, this.matchVersion, innerDictionaryObj);
                keySet = new UnmodifiableCharArraySet(copy);
            }
            return keySet;
        }

        private sealed class UnmodifiableCharArraySet : CharArraySet
        {
            internal UnmodifiableCharArraySet(CharArrayMap<object> map) : base(map)
            {
            }

            public override bool Add(object o)
            {
                throw new NotSupportedException();
            }
            public bool Add(ICharSequence text)
            {
                throw new NotSupportedException();
            }
            public override bool Add(string text)
            {
                throw new NotSupportedException();
            }
            public override bool Add(char[] text)
            {
                throw new NotSupportedException();
            }
            public override void Clear()
            {
                throw new NotSupportedException();
            }
            public override bool Remove(object item)
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// public iterator class so efficient methods are exposed to users
        /// </summary>
        public class EntryIterator : IEnumerator<KeyValuePair<object, V>>
        {
            private readonly CharArrayMap<V> outerInstance;

            internal int pos = -1;
            internal int lastPos;
            internal readonly bool allowModify;

            internal EntryIterator(CharArrayMap<V> outerInstance, bool allowModify)
            {
                this.outerInstance = outerInstance;
                this.allowModify = allowModify;
                GoNext();
            }

            internal void GoNext()
            {
                lastPos = pos;
                pos++;
                while (pos < outerInstance.keys.Length && outerInstance.keys[pos] == null)
                {
                    pos++;
                }
            }

            public virtual bool HasNext()
            {
                return pos < outerInstance.keys.Length;
            }

            /// <summary>
            /// gets the next key... do not modify the returned char[] </summary>
            public virtual char[] NextKey()
            {
                GoNext();
                return outerInstance.keys[lastPos];
            }

            /// <summary>
            /// gets the next key as a newly created String object </summary>
            public virtual string NextKeyString()
            {
                return new string(NextKey());
            }

            /// <summary>
            /// returns the value associated with the last key returned </summary>
            public virtual V CurrentValue()
            {
                return outerInstance.values[lastPos];
            }

            /// <summary>
            /// sets the value associated with the last key returned </summary>
            public virtual V SetValue(V value)
            {
                if (!allowModify)
                {
                    throw new System.NotSupportedException();
                }
                V old = outerInstance.values[lastPos];
                outerInstance.values[lastPos] = value;
                return old;
            }

            /// <summary>
            /// use nextCharArray() + currentValue() for better efficiency.
            /// </summary>
            //public virtual KeyValuePair<object, V> Next()
            //{
            //    GoNext();
            //    //return new KeyValuePair<object, V>();
            //    return new MapEntry(outerInstance, lastPos, allowModify);
            //}

            public virtual void Remove()
            {
                throw new NotSupportedException();
            }

            #region Added for better .NET support LUCENENET
            public virtual void Dispose()
            {
            }

            public virtual bool MoveNext()
            {
                if (!HasNext()) return false;
                GoNext();
                return true;
            }

            public virtual void Reset()
            {
                pos = -1;
                GoNext();
            }

            public virtual KeyValuePair<object, V> Current { get { return new KeyValuePair<object, V>(outerInstance.keys[lastPos], outerInstance.values[lastPos]); } private set { } }

            object IEnumerator.Current
            {
                get { return CurrentValue(); }
            }

            #endregion
        }

        // LUCENENET NOTE: The Java Lucene type MapEntry was removed here because it is not possible 
        // to inherit the value type KeyValuePair in .NET.

        /// <summary>
        /// public EntrySet_ class so efficient methods are exposed to users
        /// 
        /// NOTE: In .NET this was renamed to EntrySet_ because it conflicted with the
        /// method EntrySet(). Since there is also an extension method named IDictionary<K,V>.EntrySet()
        /// that this class needs to override, changing the name of the method was not
        /// possible because the extension method would produce incorrect results if it were
        /// inadvertently called.
        /// </summary>
        public sealed class EntrySet_ : ISet<KeyValuePair<object, V>>
        {
            private readonly CharArrayMap<V> outerInstance;

            internal readonly bool allowModify;

            internal EntrySet_(CharArrayMap<V> outerInstance, bool allowModify)
            {
                this.outerInstance = outerInstance;
                this.allowModify = allowModify;
            }

            public IEnumerator GetEnumerator()
            {
                return new EntryIterator(outerInstance, allowModify);
            }

            IEnumerator<KeyValuePair<object, V>> IEnumerable<KeyValuePair<object, V>>.GetEnumerator()
            {
                return (IEnumerator<KeyValuePair<object, V>>)GetEnumerator();
            }

            public bool Contains(object o)
            {
                if (!(o is DictionaryEntry))
                {
                    return false;
                }
                var e = (KeyValuePair<object, V>)o;
                object key = e.Key;
                object val = e.Value;
                object v = outerInstance.Get(key);
                return v == null ? val == null : v.Equals(val);
            }

            public bool Remove(KeyValuePair<object, V> item)
            {
                throw new NotSupportedException();
            }

            public int Count { get { return outerInstance.count; } private set { throw new NotSupportedException(); } }
            
            public void Clear()
            {
                if (!allowModify)
                {
                    throw new NotSupportedException();
                }
                outerInstance.Clear();
            }

            #region Added for better .NET support LUCENENET

            #region Not implemented members

            // TODO: Either implement these (and write tests for them) or throw
            // NotSupportedException.
            public void CopyTo(KeyValuePair<object, V>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public bool Contains(KeyValuePair<object, V> item)
            {
                throw new NotImplementedException();
            }

            public void Add(KeyValuePair<object, V> item)
            {
                throw new NotImplementedException();
            }

            public void UnionWith(IEnumerable<KeyValuePair<object, V>> other)
            {
                throw new NotImplementedException();
            }

            public void IntersectWith(IEnumerable<KeyValuePair<object, V>> other)
            {
                throw new NotImplementedException();
            }

            public void ExceptWith(IEnumerable<KeyValuePair<object, V>> other)
            {
                throw new NotImplementedException();
            }

            public void SymmetricExceptWith(IEnumerable<KeyValuePair<object, V>> other)
            {
                throw new NotImplementedException();
            }

            public bool IsSubsetOf(IEnumerable<KeyValuePair<object, V>> other)
            {
                throw new NotImplementedException();
            }

            public bool IsSupersetOf(IEnumerable<KeyValuePair<object, V>> other)
            {
                throw new NotImplementedException();
            }

            public bool IsProperSupersetOf(IEnumerable<KeyValuePair<object, V>> other)
            {
                throw new NotImplementedException();
            }

            public bool IsProperSubsetOf(IEnumerable<KeyValuePair<object, V>> other)
            {
                throw new NotImplementedException();
            }

            public bool Overlaps(IEnumerable<KeyValuePair<object, V>> other)
            {
                throw new NotImplementedException();
            }

            public bool SetEquals(IEnumerable<KeyValuePair<object, V>> other)
            {
                throw new NotImplementedException();
            }

            bool ISet<KeyValuePair<object, V>>.Add(KeyValuePair<object, V> item)
            {
                throw new NotImplementedException();
            }
            #endregion

            public bool IsReadOnly { get { return !allowModify; } }

            public override string ToString()
            {
                var sb = new StringBuilder("[");

                IEnumerator<KeyValuePair<object, V>> iter1 = new EntryIterator(this.outerInstance, false);
                while (iter1.MoveNext())
                {
                    KeyValuePair<object, V> entry = iter1.Current;
                    if (sb.Length > 1)
                    {
                        sb.Append(", ");
                    }
                    if (entry.Key.GetType().Equals(typeof(char[])))
                    {
                        sb.Append(new string((char[])entry.Key));
                    }
                    else
                    {
                        sb.Append(entry.Key);
                    }
                    sb.Append("=");
                    sb.Append(entry.Value);
                }

                return sb.Append(']').ToString();
            }
            #endregion
        }
    }

    /// <summary>
    /// LUCENENET: Added this type so a similar syntax that was used in Java to access the static
    /// methods can be used in .NET (CharArrayMap.Copy(something) rather than CharArrayMap{String}.Copy(something)).
    /// </summary>
    public static class CharArrayMap
    {
        /// <summary>
        /// Returns an unmodifiable <seealso cref="CharArrayMap"/>. This allows to provide
        /// unmodifiable views of internal map for "read-only" use.
        /// </summary>
        /// <param name="map">
        ///          a map for which the unmodifiable map is returned. </param>
        /// <returns> an new unmodifiable <seealso cref="CharArrayMap"/>. </returns>
        /// <exception cref="NullPointerException">
        ///           if the given map is <code>null</code>. </exception>
        public static CharArrayMap<V> UnmodifiableMap<V>(CharArrayMap<V> map)
        {
            if (map == null)
            {
                throw new ArgumentException("Given map is null", "map");
            }
            if (map == EmptyMap<V>() || map.Count == 0)
            {
                return EmptyMap<V>();
            }
            if (map is UnmodifiableCharArrayMap<V>)
            {
                return map;
            }
            return new UnmodifiableCharArrayMap<V>(map);
        }

        /// <summary>
        /// Returns a copy of the given map as a <seealso cref="CharArrayMap"/>. If the given map
        /// is a <seealso cref="CharArrayMap"/> the ignoreCase property will be preserved.
        /// <para>
        /// <b>Note:</b> If you intend to create a copy of another <seealso cref="CharArrayMap"/> where
        /// the <seealso cref="Version"/> of the source map differs from its copy
        /// <seealso cref="#CharArrayMap(Version, Map, boolean)"/> should be used instead.
        /// The <seealso cref="#copy(Version, Map)"/> will preserve the <seealso cref="Version"/> of the
        /// source map it is an instance of <seealso cref="CharArrayMap"/>.
        /// </para>
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. This argument will be ignored if the
        ///          given map is a <seealso cref="CharArrayMap"/>. </param>
        /// <param name="map">
        ///          a map to copy </param>
        /// <returns> a copy of the given map as a <seealso cref="CharArrayMap"/>. If the given map
        ///         is a <seealso cref="CharArrayMap"/> the ignoreCase property as well as the
        ///         matchVersion will be of the given map will be preserved. </returns>
        public static CharArrayMap<V> Copy<V>(LuceneVersion matchVersion, IDictionary<object, V> map)
        {
            if (map == EmptyMap<V>())
            {
                return EmptyMap<V>();
            }

            var vs = map as CharArrayMap<V>;
            if (vs != null)
            {
                var m = vs;
                // use fast path instead of iterating all values
                // this is even on very small sets ~10 times faster than iterating
                var keys = new char[m.keys.Length][];
                Array.Copy(m.keys, 0, keys, 0, keys.Length);
                var values = new V[m.values.Length];
                Array.Copy(m.values, 0, values, 0, values.Length);

                IDictionary<object, V> innerDictionaryCopy = new Dictionary<object, V>();
                for (int i = 0; i < keys.Length; i++)
                {
                    if (keys[i] != null)
                        innerDictionaryCopy.Add(keys[i], values[i]);
                }
                m = new CharArrayMap<V>(m) { keys = keys, values = values, innerDictionary = innerDictionaryCopy };
                return m;
            }
            return new CharArrayMap<V>(matchVersion, map, false);
        }

        /// <summary>
        /// Returns an empty, unmodifiable map. </summary>
        public static CharArrayMap<V> EmptyMap<V>()
        {
            return new EmptyCharArrayMap<V>();
            //return EMPTY_MAP;
        }

        // package private CharArraySet instanceof check in CharArraySet
        internal class UnmodifiableCharArrayMap<V> : CharArrayMap<V>
        {

            public UnmodifiableCharArrayMap(CharArrayMap<V> map) : base(map)
            {

            }

            public override void Clear()
            {
                throw new System.NotSupportedException();
            }

            public override V Put(object o, V val)
            {
                throw new System.NotSupportedException();
            }

            public override V Put(char[] text, V val)
            {
                throw new System.NotSupportedException();
            }

            public override V Put(ICharSequence text, V val)
            {
                throw new System.NotSupportedException();
            }

            public override V Put(string text, V val)
            {
                throw new System.NotSupportedException();
            }

            public override bool Remove(object key)
            {
                throw new System.NotSupportedException();
            }

            internal override EntrySet_ CreateEntrySet()
            {
                return new EntrySet_(this, false);
            }

            #region Added for better .NET support LUCENENET
            public override void Add(object key, V value)
            {
                throw new System.NotSupportedException();
            }
            public override void Add(KeyValuePair<object, V> item)
            {
                throw new System.NotSupportedException();
            }
            public override V this[object key]
            {
                get { return base[key]; }
                set { throw new System.NotSupportedException(); }
            }
            public override bool Remove(KeyValuePair<object, V> item)
            {
                throw new System.NotSupportedException();
            }
            #endregion
        }

        /// <summary>
        /// Empty <seealso cref="CharArrayMap{V}.UnmodifiableCharArrayMap"/> optimized for speed.
        /// Contains checks will always return <code>false</code> or throw
        /// NPE if necessary.
        /// </summary>
        private class EmptyCharArrayMap<V> : UnmodifiableCharArrayMap<V>
        {
            public EmptyCharArrayMap()
                : base(new CharArrayMap<V>(LuceneVersion.LUCENE_CURRENT, 0, false))
            {
            }

            public override bool ContainsKey(char[] text, int off, int len)
            {
                if (text == null)
                {
                    throw new NullReferenceException();
                }
                return false;
            }

            public bool ContainsKey(ICharSequence cs)
            {
                if (cs == null)
                {
                    throw new NullReferenceException();
                }
                return false;
            }

            public override bool ContainsKey(object o)
            {
                if (o == null)
                {
                    throw new NullReferenceException();
                }
                return false;
            }

            public override V Get(char[] text, int off, int len)
            {
                if (text == null)
                {
                    throw new NullReferenceException();
                }
                return default(V);
            }

            public V Get(ICharSequence cs)
            {
                if (cs == null)
                {
                    throw new NullReferenceException();
                }
                return default(V);
            }

            public override V Get(object o)
            {
                if (o == null)
                {
                    throw new NullReferenceException();
                }
                return default(V);
            }
        }
    }
}
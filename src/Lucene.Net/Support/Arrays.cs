using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lucene.Net.Support
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

    public static class Arrays
    {
        /// <summary>
        /// Returns a hash code based on the contents of the given array. For any two
        /// <typeparamref name="T"/> arrays <c>a</c> and <c>b</c>, if
        /// <c>Arrays.Equals(b)</c> returns <c>true</c>, it means
        /// that the return value of <c>Arrays.GetHashCode(a)</c> equals <c>Arrays.GetHashCode(b)</c>.
        /// </summary>
        /// <typeparam name="T">The array element type.</typeparam>
        /// <param name="array">The array whose hash code to compute.</param>
        /// <returns>The hash code for <paramref name="array"/>.</returns>
        public static int GetHashCode<T>(T[] array)
        {
            if (array == null)
                return 0;
            int hashCode = 1, elementHashCode;
            foreach (var element in array)
            {
                elementHashCode = element == null ? 0 : element.GetHashCode();
                hashCode = 31 * hashCode + elementHashCode;
            }
            return hashCode;
        }

        /// <summary>
        /// Returns a hash code based on the contents of the given array. For any two
        /// <see cref="string"/> arrays <c>a</c> and <c>b</c>, if
        /// <c>Arrays.Equals(b)</c> returns <c>true</c>, it means
        /// that the return value of <c>Arrays.GetHashCode(a)</c> equals <c>Arrays.GetHashCode(b)</c>.
        /// </summary>
        /// <param name="array">The array whose hash code to compute.</param>
        /// <returns>The hash code for <paramref name="array"/>.</returns>
        public static int GetHashCode(string[] array)
        {
            if (array == null)
                return 0;
            int hashCode = 1, elementHashCode;
            foreach (var element in array)
            {
                elementHashCode = element == null ? 0 : StringComparer.Ordinal.GetHashCode(element);
                hashCode = 31 * hashCode + elementHashCode;
            }
            return hashCode;
        }

        /// <summary>
        /// Returns a hash code based on the contents of the given array. For any two
        /// <see cref="bool"/> arrays <c>a</c> and <c>b</c>, if
        /// <c>Arrays.Equals(b)</c> returns <c>true</c>, it means
        /// that the return value of <c>Arrays.GetHashCode(a)</c> equals <c>Arrays.GetHashCode(b)</c>.
        /// </summary>
        /// <param name="array">The array whose hash code to compute.</param>
        /// <returns>The hash code for <paramref name="array"/>.</returns>
        public static int GetHashCode(bool[] array)
        {
            if (array == null)
                return 0;
            int hashCode = 1;
            foreach (var element in array)
            {
                // 1231, 1237 are hash code values for boolean value
                hashCode = 31 * hashCode + (element ? 1231 : 1237);
            }
            return hashCode;
        }

        /// <summary>
        /// Returns a hash code based on the contents of the given array. For any two
        /// <see cref="byte"/> arrays <c>a</c> and <c>b</c>, if
        /// <c>Arrays.Equals(b)</c> returns <c>true</c>, it means
        /// that the return value of <c>Arrays.GetHashCode(a)</c> equals <c>Arrays.GetHashCode(b)</c>.
        /// </summary>
        /// <param name="array">The array whose hash code to compute.</param>
        /// <returns>The hash code for <paramref name="array"/>.</returns>
        public static int GetHashCode(byte[] array)
        {
            if (array == null)
                return 0;
            int hashCode = 1;
            foreach (var element in array)
            {
                // the hash code value for integer value is integer value itself
                hashCode = 31 * hashCode + element;
            }
            return hashCode;
        }

        /// <summary>
        /// Returns a hash code based on the contents of the given array. For any two
        /// <see cref="sbyte"/> arrays <c>a</c> and <c>b</c>, if
        /// <c>Arrays.Equals(b)</c> returns <c>true</c>, it means
        /// that the return value of <c>Arrays.GetHashCode(a)</c> equals <c>Arrays.GetHashCode(b)</c>.
        /// </summary>
        /// <param name="array">The array whose hash code to compute.</param>
        /// <returns>The hash code for <paramref name="array"/>.</returns>
        [CLSCompliant(false)]
        public static int GetHashCode(sbyte[] array)
        {
            if (array == null)
                return 0;
            int hashCode = 1;
            foreach (var element in array)
            {
                // the hash code value for integer value is integer value itself
                hashCode = 31 * hashCode + element;
            }
            return hashCode;
        }

        /// <summary>
        /// Returns a hash code based on the contents of the given array. For any two
        /// <see cref="byte"/> arrays <c>a</c> and <c>b</c>, if
        /// <c>Arrays.Equals(b)</c> returns <c>true</c>, it means
        /// that the return value of <c>Arrays.GetHashCode(a)</c> equals <c>Arrays.GetHashCode(b)</c>.
        /// </summary>
        /// <param name="array">The array whose hash code to compute.</param>
        /// <returns>The hash code for <paramref name="array"/>.</returns>
        public static int GetHashCode(char[] array)
        {
            if (array == null)
                return 0;
            int hashCode = 1;
            foreach (var element in array)
            {
                // the hash code value for integer value is integer value itself
                hashCode = 31 * hashCode + element;
            }
            return hashCode;
        }

        /// <summary>
        /// Returns a hash code based on the contents of the given array. For any two
        /// <see cref="short"/> arrays <c>a</c> and <c>b</c>, if
        /// <c>Arrays.Equals(b)</c> returns <c>true</c>, it means
        /// that the return value of <c>Arrays.GetHashCode(a)</c> equals <c>Arrays.GetHashCode(b)</c>.
        /// </summary>
        /// <param name="array">The array whose hash code to compute.</param>
        /// <returns>The hash code for <paramref name="array"/>.</returns>
        public static int GetHashCode(short[] array)
        {
            if (array == null)
                return 0;
            int hashCode = 1;
            foreach (var element in array)
            {
                // the hash code value for integer value is integer value itself
                hashCode = 31 * hashCode + element;
            }
            return hashCode;
        }

        /// <summary>
        /// Returns a hash code based on the contents of the given array. For any two
        /// <see cref="int"/> arrays <c>a</c> and <c>b</c>, if
        /// <c>Arrays.Equals(b)</c> returns <c>true</c>, it means
        /// that the return value of <c>Arrays.GetHashCode(a)</c> equals <c>Arrays.GetHashCode(b)</c>.
        /// </summary>
        /// <param name="array">The array whose hash code to compute.</param>
        /// <returns>The hash code for <paramref name="array"/>.</returns>
        public static int GetHashCode(int[] array)
        {
            if (array == null)
                return 0;
            int hashCode = 1;
            foreach (var element in array)
            {
                // the hash code value for integer value is integer value itself
                hashCode = 31 * hashCode + element;
            }
            return hashCode;
        }

        /// <summary>
        /// Returns a hash code based on the contents of the given array. For any two
        /// <see cref="long"/> arrays <c>a</c> and <c>b</c>, if
        /// <c>Arrays.Equals(b)</c> returns <c>true</c>, it means
        /// that the return value of <c>Arrays.GetHashCode(a)</c> equals <c>Arrays.GetHashCode(b)</c>.
        /// </summary>
        /// <param name="array">The array whose hash code to compute.</param>
        /// <returns>The hash code for <paramref name="array"/>.</returns>
        public static int GetHashCode(long[] array)
        {
            if (array == null)
                return 0;
            int hashCode = 1;
            foreach (var element in array)
            {
                // the hash code value for integer value is integer value itself
                hashCode = 31 * hashCode + (int)(element ^ (long)((ulong)element >> 32));
            }
            return hashCode;
        }

        /// <summary>
        /// Returns a hash code based on the contents of the given array. For any two
        /// <see cref="ushort"/> arrays <c>a</c> and <c>b</c>, if
        /// <c>Arrays.Equals(b)</c> returns <c>true</c>, it means
        /// that the return value of <c>Arrays.GetHashCode(a)</c> equals <c>Arrays.GetHashCode(b)</c>.
        /// </summary>
        /// <param name="array">The array whose hash code to compute.</param>
        /// <returns>The hash code for <paramref name="array"/>.</returns>
        [CLSCompliant(false)]
        public static int GetHashCode(ushort[] array)
        {
            if (array == null)
                return 0;
            int hashCode = 1;
            foreach (var element in array)
            {
                // the hash code value for integer value is integer value itself
                hashCode = 31 * hashCode + element;
            }
            return hashCode;
        }

        /// <summary>
        /// Returns a hash code based on the contents of the given array. For any two
        /// <see cref="uint"/> arrays <c>a</c> and <c>b</c>, if
        /// <c>Arrays.Equals(b)</c> returns <c>true</c>, it means
        /// that the return value of <c>Arrays.GetHashCode(a)</c> equals <c>Arrays.GetHashCode(b)</c>.
        /// </summary>
        /// <param name="array">The array whose hash code to compute.</param>
        /// <returns>The hash code for <paramref name="array"/>.</returns>
        [CLSCompliant(false)]
        public static int GetHashCode(uint[] array)
        {
            if (array == null)
                return 0;
            int hashCode = 1;
            foreach (var element in array)
            {
                // the hash code value for integer value is integer value itself
                hashCode = 31 * hashCode + (int)element;
            }
            return hashCode;
        }

        /// <summary>
        /// Returns a hash code based on the contents of the given array. For any two
        /// <see cref="ulong"/> arrays <c>a</c> and <c>b</c>, if
        /// <c>Arrays.Equals(b)</c> returns <c>true</c>, it means
        /// that the return value of <c>Arrays.GetHashCode(a)</c> equals <c>Arrays.GetHashCode(b)</c>.
        /// </summary>
        /// <param name="array">The array whose hash code to compute.</param>
        /// <returns>The hash code for <paramref name="array"/>.</returns>
        [CLSCompliant(false)]
        public static int GetHashCode(ulong[] array)
        {
            if (array == null)
                return 0;
            int hashCode = 1;
            foreach (var element in array)
            {
                // the hash code value for integer value is integer value itself
                hashCode = 31 * hashCode + (int)(element ^ (element >> 32));
            }
            return hashCode;
        }

        /// <summary>
        /// Returns a hash code based on the contents of the given array. For any two
        /// <see cref="float"/> arrays <c>a</c> and <c>b</c>, if
        /// <c>Arrays.Equals(b)</c> returns <c>true</c>, it means
        /// that the return value of <c>Arrays.GetHashCode(a)</c> equals <c>Arrays.GetHashCode(b)</c>.
        /// </summary>
        /// <param name="array">The array whose hash code to compute.</param>
        /// <returns>The hash code for <paramref name="array"/>.</returns>
        public static int GetHashCode(float[] array)
        {
            if (array == null)
                return 0;
            int hashCode = 1;
            foreach (var element in array)
            {
                // the hash code value for float value is
                // Number.SingleToInt32Bits(value)
                hashCode = 31 * hashCode + Number.SingleToInt32Bits(element);
            }
            return hashCode;
        }

        /// <summary>
        /// Returns a hash code based on the contents of the given array. For any two
        /// <see cref="double"/> arrays <c>a</c> and <c>b</c>, if
        /// <c>Arrays.Equals(b)</c> returns <c>true</c>, it means
        /// that the return value of <c>Arrays.GetHashCode(a)</c> equals <c>Arrays.GetHashCode(b)</c>.
        /// </summary>
        /// <param name="array">The array whose hash code to compute.</param>
        /// <returns>The hash code for <paramref name="array"/>.</returns>
        public static int GetHashCode(double[] array)
        {
            if (array == null)
                return 0;
            int hashCode = 1;
            long v;
            foreach (var element in array)
            {
                v = Number.DoubleToInt64Bits(element);
                // the hash code value for double value is (int) (v ^ (v >>> 32))
                // where v = Number.DoubleToInt64Bits(value)
                hashCode = 31 * hashCode + (int)(v ^ (long)((ulong)v >> 32));
            }
            return hashCode;
        }


        /// <summary>
        /// Assigns the specified value to each element of the specified array.
        /// </summary>
        /// <typeparam name="T">the type of the array</typeparam>
        /// <param name="a">the array to be filled</param>
        /// <param name="val">the value to be stored in all elements of the array</param>
        public static void Fill<T>(T[] a, T val)
        {
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = val;
            }
        }

        /// <summary>
        /// Assigns the specified long value to each element of the specified
        /// range of the specified array of longs.  The range to be filled
        /// extends from index <paramref name="fromIndex"/>, inclusive, to index
        /// <paramref name="toIndex"/>, exclusive.  (If <c>fromIndex==toIndex</c>, the
        /// range to be filled is empty.)
        /// </summary>
        /// <typeparam name="T">the type of the array</typeparam>
        /// <param name="a">the array to be filled</param>
        /// <param name="fromIndex">
        /// the index of the first element (inclusive) to be
        /// filled with the specified value
        /// </param>
        /// <param name="toIndex">
        /// the index of the last element (exclusive) to be
        /// filled with the specified value
        /// </param>
        /// <param name="val">the value to be stored in all elements of the array</param>
        /// <exception cref="ArgumentException">if <c>fromIndex &gt; toIndex</c></exception>
        /// <exception cref="ArgumentOutOfRangeException">if <c>fromIndex &lt; 0</c> or <c>toIndex &gt; a.Length</c></exception>
        public static void Fill<T>(T[] a, int fromIndex, int toIndex, T val)
        {
            //Java Arrays.fill exception logic
            if (fromIndex > toIndex)
                throw new ArgumentException("fromIndex(" + fromIndex + ") > toIndex(" + toIndex + ")");
            if (fromIndex < 0)
                throw new ArgumentOutOfRangeException("fromIndex");
            if (toIndex > a.Length)
                throw new ArgumentOutOfRangeException("toIndex");

            for (int i = fromIndex; i < toIndex; i++)
            {
                a[i] = val;
            }
        }

        /// <summary>
        /// Compares the entire members of one array whith the other one.
        /// </summary>
        /// <param name="a">The array to be compared.</param>
        /// <param name="b">The array to be compared with.</param>
        /// <returns>Returns true if the two specified arrays of Objects are equal
        /// to one another. The two arrays are considered equal if both arrays
        /// contain the same number of elements, and all corresponding pairs of
        /// elements in the two arrays are equal. Two objects e1 and e2 are
        /// considered equal if (e1==null ? e2==null : e1.Equals(e2)). In other
        /// words, the two arrays are equal if they contain the same elements in
        /// the same order. Also, two array references are considered equal if
        /// both are null.
        /// <para/>
        /// Note that if the type of <typeparam name="T"/> is a <see cref="IDictionary{TKey, TValue}"/>,
        /// <see cref="IList{T}"/>, or <see cref="ISet{T}"/>, its values and any nested collection values
        /// will be compared for equality as well.
        /// </returns>
        public static bool Equals<T>(T[] a, T[] b)
        {
            if (object.ReferenceEquals(a, b))
            {
                return true;
            }
            bool isValueType = typeof(T).GetTypeInfo().IsValueType;
            if (!isValueType && a == null)
            {
                return b == null;
            }

            int length = a.Length;

            if (b.Length != length)
            {
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                T o1 = a[i];
                T o2 = b[i];
                if (!(isValueType ? o1.Equals(o2) : (o1 == null ? o2 == null : Collections.Equals(o1, o2))))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Compares the two arrays.
        /// </summary>
        /// <param name="array1">The first <see cref="string"/> array.</param>
        /// <param name="array2">The second <see cref="string"/> array.</param>
        /// <returns><c>true</c> if both arrays are <c>null</c> or if the arrays have the
        /// same length and the elements at each index in the two arrays are
        /// equal; otherwise, <c>false</c>.</returns>
        public static bool Equals(string[] array1, string[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return true;
            int arrayLength = array1.Length;
            if (array1 == null || array2 == null || arrayLength != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (!StringComparer.Ordinal.Equals(array1[i], array2[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Compares the two arrays.
        /// </summary>
        /// <param name="array1">The first <see cref="bool"/> array.</param>
        /// <param name="array2">The second <see cref="bool"/> array.</param>
        /// <returns><c>true</c> if both arrays are <c>null</c> or if the arrays have the
        /// same length and the elements at each index in the two arrays are
        /// equal; otherwise, <c>false</c>.</returns>
        public static bool Equals(bool[] array1, bool[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return true;
            int arrayLength = array1.Length;
            if (array1 == null || array2 == null || arrayLength != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Compares the two arrays.
        /// </summary>
        /// <param name="array1">The first <see cref="byte"/> array.</param>
        /// <param name="array2">The second <see cref="byte"/> array.</param>
        /// <returns><c>true</c> if both arrays are <c>null</c> or if the arrays have the
        /// same length and the elements at each index in the two arrays are
        /// equal; otherwise, <c>false</c>.</returns>
        public static bool Equals(byte[] array1, byte[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return true;
            int arrayLength = array1.Length;
            if (array1 == null || array2 == null || arrayLength != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Compares the two arrays.
        /// </summary>
        /// <param name="array1">The first <see cref="sbyte"/> array.</param>
        /// <param name="array2">The second <see cref="sbyte"/> array.</param>
        /// <returns><c>true</c> if both arrays are <c>null</c> or if the arrays have the
        /// same length and the elements at each index in the two arrays are
        /// equal; otherwise, <c>false</c>.</returns>
        [CLSCompliant(false)]
        public static bool Equals(sbyte[] array1, sbyte[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return true;
            int arrayLength = array1.Length;
            if (array1 == null || array2 == null || arrayLength != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Compares the two arrays.
        /// </summary>
        /// <param name="array1">The first <see cref="char"/> array.</param>
        /// <param name="array2">The second <see cref="char"/> array.</param>
        /// <returns><c>true</c> if both arrays are <c>null</c> or if the arrays have the
        /// same length and the elements at each index in the two arrays are
        /// equal; otherwise, <c>false</c>.</returns>
        public static bool Equals(char[] array1, char[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return true;
            int arrayLength = array1.Length;
            if (array1 == null || array2 == null || arrayLength != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Compares the two arrays.
        /// </summary>
        /// <param name="array1">The first <see cref="short"/> array.</param>
        /// <param name="array2">The second <see cref="short"/> array.</param>
        /// <returns><c>true</c> if both arrays are <c>null</c> or if the arrays have the
        /// same length and the elements at each index in the two arrays are
        /// equal; otherwise, <c>false</c>.</returns>
        public static bool Equals(short[] array1, short[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return true;
            int arrayLength = array1.Length;
            if (array1 == null || array2 == null || arrayLength != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Compares the two arrays.
        /// </summary>
        /// <param name="array1">The first <see cref="int"/> array.</param>
        /// <param name="array2">The second <see cref="int"/> array.</param>
        /// <returns><c>true</c> if both arrays are <c>null</c> or if the arrays have the
        /// same length and the elements at each index in the two arrays are
        /// equal; otherwise, <c>false</c>.</returns>
        public static bool Equals(int[] array1, int[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return true;
            int arrayLength = array1.Length;
            if (array1 == null || array2 == null || arrayLength != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Compares the two arrays.
        /// </summary>
        /// <param name="array1">The first <see cref="long"/> array.</param>
        /// <param name="array2">The second <see cref="long"/> array.</param>
        /// <returns><c>true</c> if both arrays are <c>null</c> or if the arrays have the
        /// same length and the elements at each index in the two arrays are
        /// equal; otherwise, <c>false</c>.</returns>
        public static bool Equals(long[] array1, long[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return true;
            int arrayLength = array1.Length;
            if (array1 == null || array2 == null || arrayLength != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Compares the two arrays.
        /// </summary>
        /// <param name="array1">The first <see cref="ushort"/> array.</param>
        /// <param name="array2">The second <see cref="ushort"/> array.</param>
        /// <returns><c>true</c> if both arrays are <c>null</c> or if the arrays have the
        /// same length and the elements at each index in the two arrays are
        /// equal; otherwise, <c>false</c>.</returns>
        [CLSCompliant(false)]
        public static bool Equals(ushort[] array1, ushort[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return true;
            int arrayLength = array1.Length;
            if (array1 == null || array2 == null || arrayLength != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Compares the two arrays.
        /// </summary>
        /// <param name="array1">The first <see cref="uint"/> array.</param>
        /// <param name="array2">The second <see cref="uint"/> array.</param>
        /// <returns><c>true</c> if both arrays are <c>null</c> or if the arrays have the
        /// same length and the elements at each index in the two arrays are
        /// equal; otherwise, <c>false</c>.</returns>
        [CLSCompliant(false)]
        public static bool Equals(uint[] array1, uint[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return true;
            int arrayLength = array1.Length;
            if (array1 == null || array2 == null || arrayLength != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Compares the two arrays.
        /// </summary>
        /// <param name="array1">The first <see cref="ulong"/> array.</param>
        /// <param name="array2">The second <see cref="ulong"/> array.</param>
        /// <returns><c>true</c> if both arrays are <c>null</c> or if the arrays have the
        /// same length and the elements at each index in the two arrays are
        /// equal; otherwise, <c>false</c>.</returns>
        [CLSCompliant(false)]
        public static bool Equals(ulong[] array1, ulong[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return true;
            int arrayLength = array1.Length;
            if (array1 == null || array2 == null || arrayLength != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Compares the two arrays.
        /// </summary>
        /// <param name="array1">The first <see cref="float"/> array.</param>
        /// <param name="array2">The second <see cref="float"/> array.</param>
        /// <returns><c>true</c> if both arrays are <c>null</c> or if the arrays have the
        /// same length and the elements at each index in the two arrays are
        /// equal; otherwise, <c>false</c>.</returns>
        public static bool Equals(float[] array1, float[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return true;
            int arrayLength = array1.Length;
            if (array1 == null || array2 == null || arrayLength != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Compares the two arrays.
        /// </summary>
        /// <param name="array1">The first <see cref="double"/> array.</param>
        /// <param name="array2">The second <see cref="double"/> array.</param>
        /// <returns><c>true</c> if both arrays are <c>null</c> or if the arrays have the
        /// same length and the elements at each index in the two arrays are
        /// equal; otherwise, <c>false</c>.</returns>
        public static bool Equals(double[] array1, double[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return true;
            int arrayLength = array1.Length;
            if (array1 == null || array2 == null || arrayLength != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Compares the two arrays.
        /// </summary>
        /// <param name="array1">The first <see cref="decimal"/> array.</param>
        /// <param name="array2">The second <see cref="decimal"/> array.</param>
        /// <returns><c>true</c> if both arrays are <c>null</c> or if the arrays have the
        /// same length and the elements at each index in the two arrays are
        /// equal; otherwise, <c>false</c>.</returns>
        public static bool Equals(decimal[] array1, decimal[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return true;
            int arrayLength = array1.Length;
            if (array1 == null || array2 == null || arrayLength != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }


        public static T[] CopyOf<T>(T[] original, int newLength)
        {
            T[] newArray = new T[newLength];

            for (int i = 0; i < Math.Min(original.Length, newLength); i++)
            {
                newArray[i] = original[i];
            }

            return newArray;
        }

        public static T[] CopyOfRange<T>(T[] original, int startIndexInc, int endIndexExc)
        {
            int newLength = endIndexExc - startIndexInc;
            T[] newArray = new T[newLength];

            for (int i = startIndexInc, j = 0; i < endIndexExc; i++, j++)
            {
                newArray[j] = original[i];
            }

            return newArray;
        }

        public static string ToString(IEnumerable<string> values)
        {
            if (values == null)
                return string.Empty;

            return string.Join(", ", values);
        }

        public static string ToString<T>(IEnumerable<T> values)
        {
            if (values == null)
                return string.Empty;

            return string.Join(", ", values);
        }

        public static List<T> AsList<T>(params T[] objects)
        {
            return objects.ToList();
        }
    }
}

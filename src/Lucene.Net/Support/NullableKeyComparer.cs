using System;
using System.Collections.Generic;

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

    /// <summary>
    /// A comparer wrapper for use with <see cref="NullableKeyDictionary{TKey, TValue}"/>
    /// when specifying a custom comparer in the backing dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of key. This can be either a value type or a reference type.
    /// For the nullable feature to function, a value type should be specified as nullable.</typeparam>
    public class NullableKeyComparer<TKey> : IEqualityComparer<NullableKey<TKey>>
    {
        private readonly IEqualityComparer<TKey> comparer;
        public NullableKeyComparer(IEqualityComparer<TKey> comparer)
        {
            this.comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        public bool Equals(NullableKey<TKey> x, NullableKey<TKey> y)
        {
            if (!x.HasValue)
                return !y.HasValue;
            if (!y.HasValue)
                return false; // Already checked x above

            return comparer.Equals(x, y);
        }

        public int GetHashCode(NullableKey<TKey> obj)
        {
            return comparer.GetHashCode(obj);
        }
    }
}

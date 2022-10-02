// Lucene version compatibility level 8.2.0
using Lucene.Net.Store;
using Lucene.Net.Support.Util.Fst;
using System;
using System.IO;
using Long = J2N.Numerics.Int64;

namespace Lucene.Net.Analysis.Ko.Dict
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
    /// Binary dictionary implementation for a known-word dictionary model:
    /// Words are encoded into an FST mapping to a list of wordIDs.
    /// </summary>
    public class TokenInfoDictionary : BinaryDictionary
    {
        public const string FST_FILENAME_SUFFIX = "$fst.dat";

        private readonly TokenInfoFST fst;

        private TokenInfoDictionary()
            : base()
        {
            FST<Long> fst = null;
            using (Stream @is = GetResource(FST_FILENAME_SUFFIX))
            {
                fst = new FST<Long>(new InputStreamDataInput(@is), PositiveInt32Outputs.Singleton);
            }
            this.fst = new TokenInfoFST(fst);
        }

        public TokenInfoFST FST => fst;

        public static TokenInfoDictionary Instance => SingletonHolder.INSTANCE;

        private static class SingletonHolder
        {
            internal static readonly TokenInfoDictionary INSTANCE = LoadInstance(); // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            private static TokenInfoDictionary LoadInstance()
            {
                try
                {
                    return new TokenInfoDictionary();
                }
                catch (IOException ioe)
                {
                    throw new Exception("Cannot load TokenInfoDictionary.", ioe);
                }
            }
        }
    }
}

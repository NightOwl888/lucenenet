﻿using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Ckb
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
    /// Light stemmer for Sorani
    /// </summary>
    public class SoraniStemmer
    {
        /// <summary>
        /// Stem an input buffer of Sorani text.
        /// </summary>
        /// <param name="s"> input buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <returns> length of input buffer after normalization </returns>
        public virtual int Stem(char[] s, int len)
        {
            // postposition
            if (len > 5 && StemmerUtil.EndsWith(s, len, "دا"))
            {
                len -= 2;
            }
            else if (len > 4 && StemmerUtil.EndsWith(s, len, "نا"))
            {
                len--;
            }
            else if (len > 6 && StemmerUtil.EndsWith(s, len, "ەوە"))
            {
                len -= 3;
            }

            // possessive pronoun
            if (len > 6 && (StemmerUtil.EndsWith(s, len, "مان") || StemmerUtil.EndsWith(s, len, "یان") || StemmerUtil.EndsWith(s, len, "تان")))
            {
                len -= 3;
            }

            // indefinite singular ezafe
            if (len > 6 && StemmerUtil.EndsWith(s, len, "ێکی"))
            {
                return len - 3;
            }
            else if (len > 7 && StemmerUtil.EndsWith(s, len, "یەکی"))
            {
                return len - 4;
            }
            // indefinite singular
            if (len > 5 && StemmerUtil.EndsWith(s, len, "ێک"))
            {
                return len - 2;
            }
            else if (len > 6 && StemmerUtil.EndsWith(s, len, "یەک"))
            {
                return len - 3;
            }
            // definite singular
            else if (len > 6 && StemmerUtil.EndsWith(s, len, "ەکە"))
            {
                return len - 3;
            }
            else if (len > 5 && StemmerUtil.EndsWith(s, len, "کە"))
            {
                return len - 2;
            }
            // definite plural
            else if (len > 7 && StemmerUtil.EndsWith(s, len, "ەکان"))
            {
                return len - 4;
            }
            else if (len > 6 && StemmerUtil.EndsWith(s, len, "کان"))
            {
                return len - 3;
            }
            // indefinite plural ezafe
            else if (len > 7 && StemmerUtil.EndsWith(s, len, "یانی"))
            {
                return len - 4;
            }
            else if (len > 6 && StemmerUtil.EndsWith(s, len, "انی"))
            {
                return len - 3;
            }
            // indefinite plural
            else if (len > 6 && StemmerUtil.EndsWith(s, len, "یان"))
            {
                return len - 3;
            }
            else if (len > 5 && StemmerUtil.EndsWith(s, len, "ان"))
            {
                return len - 2;
            }
            // demonstrative plural
            else if (len > 7 && StemmerUtil.EndsWith(s, len, "یانە"))
            {
                return len - 4;
            }
            else if (len > 6 && StemmerUtil.EndsWith(s, len, "انە"))
            {
                return len - 3;
            }
            // demonstrative singular
            else if (len > 5 && (StemmerUtil.EndsWith(s, len, "ایە") || StemmerUtil.EndsWith(s, len, "ەیە")))
            {
                return len - 2;
            }
            else if (len > 4 && StemmerUtil.EndsWith(s, len, "ە"))
            {
                return len - 1;
            }
            // absolute singular ezafe
            else if (len > 4 && StemmerUtil.EndsWith(s, len, "ی"))
            {
                return len - 1;
            }
            return len;
        }
    }
}
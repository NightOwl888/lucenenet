// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Ko.Dict;
using Lucene.Net.Util;
using System.Text;

namespace Lucene.Net.Analysis.Ko.TokenAttributes
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
    /// Part of Speech attributes for Korean.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class PartOfSpeechAttribute : Lucene.Net.Util.Attribute, IPartOfSpeechAttribute
#if FEATURE_CLONEABLE
        , ICloneable
#endif
    {
        private Token token;

        public virtual POS.Type? POSType => token?.POSType;

        public virtual POS.Tag? LeftPOS => token?.LeftPOS;

        public virtual POS.Tag? RightPOS => token?.RightPOS;

        public virtual Morpheme[] GetMorphemes()
        {
            return token == null ? null : token.GetMorphemes();
        }

        public virtual void SetToken(Token token)
        {
            this.token = token;
        }

        public override void Clear()
        {
            token = null;
        }


        public override void ReflectWith(IAttributeReflector reflector)
        {
            string posName = POSType == null ? null : POSType.Value.ToString();
            string rightPOS = RightPOS == null ? null : RightPOS.Value.ToString() + "(" + RightPOS.Value.GetDescription() + ")";
            string leftPOS = LeftPOS == null ? null : LeftPOS.Value.ToString() + "(" + LeftPOS.Value.GetDescription() + ")";
            reflector.Reflect(typeof(PartOfSpeechAttribute), "posType", posName);
            reflector.Reflect(typeof(PartOfSpeechAttribute), "leftPOS", leftPOS);
            reflector.Reflect(typeof(PartOfSpeechAttribute), "rightPOS", rightPOS);
            reflector.Reflect(typeof(PartOfSpeechAttribute), "morphemes", DisplayMorphemes(GetMorphemes()));
        }

        private string DisplayMorphemes(Morpheme[] morphemes)
        {
            if (morphemes == null)
            {
                return null;
            }
            StringBuilder builder = new StringBuilder();
            foreach (Morpheme morpheme in morphemes)
            {
                if (builder.Length > 0)
                {
                    builder.Append("+");
                }
                builder.Append(morpheme.surfaceForm).Append('/').Append(morpheme.posTag.ToString()).Append('(').Append(morpheme.posTag.GetDescription()).Append(')');
            }
            return builder.ToString();
        }

        public override void CopyTo(IAttribute target)
        {
            IPartOfSpeechAttribute t = (IPartOfSpeechAttribute)target;
            t.SetToken(token);
        }
    }
}

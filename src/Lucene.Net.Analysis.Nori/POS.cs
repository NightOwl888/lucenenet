// Lucene version compatibility level 8.2.0
using Lucene.Net.Diagnostics;
using System;

namespace Lucene.Net.Analysis.Ko
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

    public static class TagExtensions
    {
        /// <summary>
        /// Returns the code associated with the tag (as defined in pos-id.def).
        /// </summary>
        public static int GetCode(this POS.Tag tag)
        {
            switch (tag)
            {
                /*
                * Verbal endings
                */
                case POS.Tag.E: return 100;

                /*
                 * Interjection
                 */
                case POS.Tag.IC: return 110;

                /*
                 * Ending Particle
                 */
                case POS.Tag.J: return 120;

                /*
                 * General Adverb
                 */
                case POS.Tag.MAG: return 130;

                /*
                 * Conjunctive adverb
                 */
                case POS.Tag.MAJ: return 131;

                /*
                 * Determiner
                 **/
                case POS.Tag.MM: return 140;

                /*
                 * General Noun
                 **/
                case POS.Tag.NNG: return 150;

                /*
                 * Proper Noun
                 **/
                case POS.Tag.NNP: return 151;

                /*
                 * Dependent noun (following nouns)
                 **/
                case POS.Tag.NNB: return 152;

                /*
                 * Dependent noun
                 **/
                case POS.Tag.NNBC: return 153;

                /*
                 * Pronoun
                 **/
                case POS.Tag.NP: return 154;

                /*
                 * Numeral
                 **/
                case POS.Tag.NR: return 155;

                /*
                 * Terminal punctuation (? ! .)
                 **/
                case POS.Tag.SF: return 160;

                /*
                 * Chinese character
                 **/
                case POS.Tag.SH: return 161;

                /*
                 * Foreign language
                 **/
                case POS.Tag.SL: return 162;

                /*
                 * Number
                 **/
                case POS.Tag.SN: return 163;

                /*
                 * Space
                 **/
                case POS.Tag.SP: return 164; //, "Space"),

                /*
                 * Closing brackets
                 **/
                case POS.Tag.SSC: return 165; //, "Closing brackets"),

                /*
                 * Opening brackets
                 **/
                case POS.Tag.SSO: return 166; //, "Opening brackets"),

                /*
                 * Separator (· / :)
                 **/
                case POS.Tag.SC: return 167; //, "Separator"),

                /*
                 * Other symbol
                 **/
                case POS.Tag.SY: return 168; //, "Other symbol"),

                /*
                 * Ellipsis
                 **/
                case POS.Tag.SE: return 169; //, "Ellipsis"),

                /*
                 * Adjective
                 **/
                case POS.Tag.VA: return 170; //, "Adjective"),

                /*
                 * Negative designator
                 **/
                case POS.Tag.VCN: return 171; //, "Negative designator"),

                /*
                 * Positive designator
                 **/
                case POS.Tag.VCP: return 172; //, "Positive designator"),

                /*
                 * Verb
                 **/
                case POS.Tag.VV: return 173; //, "Verb"),

                /*
                 * Auxiliary Verb or Adjective
                 **/
                case POS.Tag.VX: return 174; //, "Auxiliary Verb or Adjective"),

                /*
                 * Prefix
                 **/
                case POS.Tag.XPN: return 181; //, "Prefix"),

                /*
                 * Root
                 **/
                case POS.Tag.XR: return 182; //, "Root"),

                /*
                 * Adjective Suffix
                 **/
                case POS.Tag.XSA: return 183; //, "Adjective Suffix"),

                /*
                 * Noun Suffix
                 **/
                case POS.Tag.XSN: return 184; //, "Noun Suffix"),

                /*
                 * Verb Suffix
                 **/
                case POS.Tag.XSV: return 185; //, "Verb Suffix"),

                /*
                 * Unknown
                 */
                case POS.Tag.UNKNOWN: return 999; //, "Unknown"),

                /*
                 * Unknown
                 */
                case POS.Tag.UNA: return -1; //, "Unknown"),

                /*
                 * Unknown
                 */
                case POS.Tag.NA: return -1; //, "Unknown"),

                /*
                 * Unknown
                 */
                case POS.Tag.VSV: return -1; //, "Unknown");
            }
            throw new ArgumentException($"Not a valid {typeof(POS.Tag)}");
        }

        /// <summary>
        /// Returns the description associated with the tag.
        /// </summary>
        public static string GetDescription(this POS.Tag tag)
        {
            switch (tag)
            {
                /*
                * Verbal endings
                */
                case POS.Tag.E: return "Verbal endings";

                /*
                 * Interjection
                 */
                case POS.Tag.IC: return "Interjection";

                /*
                 * Ending Particle
                 */
                case POS.Tag.J: return "Ending Particle";

                /*
                 * General Adverb
                 */
                case POS.Tag.MAG: return "General Adverb";

                /*
                 * Conjunctive adverb
                 */
                case POS.Tag.MAJ: return "Conjunctive adverb";

                /*
                 * Determiner
                 **/
                case POS.Tag.MM: return "Modifier";

                /*
                 * General Noun
                 **/
                case POS.Tag.NNG: return "General Noun";

                /*
                 * Proper Noun
                 **/
                case POS.Tag.NNP: return "Proper Noun";

                /*
                 * Dependent noun (following nouns)
                 **/
                case POS.Tag.NNB: return "Dependent noun";

                /*
                 * Dependent noun
                 **/
                case POS.Tag.NNBC: return "Dependent noun";

                /*
                 * Pronoun
                 **/
                case POS.Tag.NP: return "Pronoun";

                /*
                 * Numeral
                 **/
                case POS.Tag.NR: return "Numeral";

                /*
                 * Terminal punctuation (? ! .)
                 **/
                case POS.Tag.SF: return "Terminal punctuation";

                /*
                 * Chinese character
                 **/
                case POS.Tag.SH: return "Chinese Characeter";

                /*
                 * Foreign language
                 **/
                case POS.Tag.SL: return "Foreign language";

                /*
                 * Number
                 **/
                case POS.Tag.SN: return "Number";

                /*
                 * Space
                 **/
                case POS.Tag.SP: return "Space";

                /*
                 * Closing brackets
                 **/
                case POS.Tag.SSC: return "Closing brackets";

                /*
                 * Opening brackets
                 **/
                case POS.Tag.SSO: return "Opening brackets";

                /*
                 * Separator (· / :)
                 **/
                case POS.Tag.SC: return "Separator";

                /*
                 * Other symbol
                 **/
                case POS.Tag.SY: return "Other symbol";

                /*
                 * Ellipsis
                 **/
                case POS.Tag.SE: return "Ellipsis";

                /*
                 * Adjective
                 **/
                case POS.Tag.VA: return "Adjective";

                /*
                 * Negative designator
                 **/
                case POS.Tag.VCN: return "Negative designator";

                /*
                 * Positive designator
                 **/
                case POS.Tag.VCP: return "Positive designator";

                /*
                 * Verb
                 **/
                case POS.Tag.VV: return "Verb";

                /*
                 * Auxiliary Verb or Adjective
                 **/
                case POS.Tag.VX: return "Auxiliary Verb or Adjective";

                /*
                 * Prefix
                 **/
                case POS.Tag.XPN: return "Prefix";

                /*
                 * Root
                 **/
                case POS.Tag.XR: return "Root";

                /*
                 * Adjective Suffix
                 **/
                case POS.Tag.XSA: return "Adjective Suffix";

                /*
                 * Noun Suffix
                 **/
                case POS.Tag.XSN: return "Noun Suffix";

                /*
                 * Verb Suffix
                 **/
                case POS.Tag.XSV: return "Verb Suffix";

                /*
                 * Unknown
                 */
                case POS.Tag.UNKNOWN: return "Unknown";

                /*
                 * Unknown
                 */
                case POS.Tag.UNA: return "Unknown";

                /*
                 * Unknown
                 */
                case POS.Tag.NA: return "Unknown";

                /*
                 * Unknown
                 */
                case POS.Tag.VSV: return "Unknown";
            }
            throw new ArgumentException($"Not a valid {typeof(POS.Tag)}");
        }
    }

    /// <summary>
    /// Part of speech classification for Korean based on Sejong corpus classification.
    /// The list of tags and their meanings is available here:
    /// <a href="https://docs.google.com/spreadsheets/d/1-9blXKjtjeKZqsf4NzHeYJCrr49-nXeRF6D80udfcwY">https://docs.google.com/spreadsheets/d/1-9blXKjtjeKZqsf4NzHeYJCrr49-nXeRF6D80udfcwY</a>
    /// </summary>
    public class POS
    {
        /// <summary>
        /// The type of the token.
        /// </summary>
        public enum Type : byte
        {
            /// <summary>
            /// A simple morpheme.
            /// </summary>
            MORPHEME,

            /// <summary>
            /// Compound noun.
            /// </summary>
            COMPOUND,

            /// <summary>
            /// Inflected token.
            /// </summary>
            INFLECT,

            /// <summary>
            /// Pre-analysis token.
            /// </summary>
            PREANALYSIS,
        }

        /// <summary>
        /// Part of speech tag for Korean based on Sejong corpus classification.
        /// </summary>
        public enum Tag : byte
        {
            /// <summary>
            /// Verbal endings
            /// </summary>
            E,

            /// <summary>
            /// Interjection
            /// </summary>
            IC,

            /// <summary>
            /// Ending Particle
            /// </summary>
            J,

            /// <summary>
            /// General Adverb
            /// </summary>
            MAG,

            /// <summary>
            /// Conjunctive adverb
            /// </summary>
            MAJ,

            /// <summary>
            /// Determiner
            /// </summary>
            MM,

            /// <summary>
            /// General Noun
            /// </summary>
            NNG,

            /// <summary>
            /// Proper Noun
            /// </summary>
            NNP,

            /// <summary>
            /// Dependent noun (following nouns)
            /// </summary>
            NNB,

            /// <summary>
            /// Dependent noun
            /// </summary>
            NNBC,

            /// <summary>
            /// Pronoun
            /// </summary>
            NP,

            /// <summary>
            /// Numeral
            /// </summary>
            NR,

            /// <summary>
            /// Terminal punctuation (? ! .)
            /// </summary>
            SF,

            /// <summary>
            /// Chinese character
            /// </summary>
            SH,

            /// <summary>
            /// Foreign language
            /// </summary>
            SL,

            /// <summary>
            /// Number
            /// </summary>
            SN,

            /// <summary>
            /// Space
            /// </summary>
            SP,

            /// <summary>
            /// Closing brackets
            /// </summary>
            SSC,

            /// <summary>
            /// Opening brackets
            /// </summary>
            SSO,

            /// <summary>
            /// Separator (· / :)
            /// </summary>
            SC,

            /// <summary>
            /// Other symbol
            /// </summary>
            SY,

            /// <summary>
            /// Ellipsis
            /// </summary>
            SE,

            /// <summary>
            /// Adjective
            /// </summary>
            VA,

            /// <summary>
            /// Negative designator
            /// </summary>
            VCN,

            /// <summary>
            /// Positive designator
            /// </summary>
            VCP,

            /// <summary>
            /// Verb
            /// </summary>
            VV,

            /// <summary>
            /// Auxiliary Verb or Adjective
            /// </summary>
            VX,

            /// <summary>
            /// Prefix
            /// </summary>
            XPN,

            /// <summary>
            /// Root
            /// </summary>
            XR,

            /// <summary>
            /// Adjective Suffix
            /// </summary>
            XSA,

            /// <summary>
            /// Noun Suffix
            /// </summary>
            XSN,

            /// <summary>
            /// Verb Suffix
            /// </summary>
            XSV,

            /// <summary>
            /// Unknown
            /// </summary>
            UNKNOWN,

            /// <summary>
            /// Unknown
            /// </summary>
            UNA,

            /// <summary>
            /// Unknown
            /// </summary>
            NA,

            /// <summary>
            /// Unknown
            /// </summary>
            VSV
        }

        /// <summary>
        /// Returns the <see cref="Tag"/> of the provided <paramref name="name"/>.
        /// </summary>
        public static Tag ResolveTag(string name)
        {
            string tagUpper = name.ToUpperInvariant();
            if (tagUpper.StartsWith("J", StringComparison.Ordinal))
            {
                return Tag.J;
            }
            else if (tagUpper.StartsWith("E", StringComparison.Ordinal))
            {
                return Tag.E;
            }
            else
            {
                //return Tag.valueOf(tagUpper);
                return (Tag)Enum.Parse(typeof(Tag), tagUpper);
            }
        }

        /// <summary>
        /// Returns the <see cref="Tag"/> of the provided <paramref name="tag"/>.
        /// </summary>
        public static Tag ResolveTag(byte tag)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(tag < Enum.GetValues(typeof(Tag)).Length);
            //return Tag.values()[tag];
            return (Tag)tag;
        }

        /// <summary>
        /// Returns the <see cref="Type"/> of the provided <paramref name="name"/>.
        /// </summary>
        public static Type ResolveType(string name)
        {
            if ("*".Equals(name))
            {
                return Type.MORPHEME;
            }
            // return Type.valueOf(name.toUpperCase(Locale.ENGLISH));
            return (Type)Enum.Parse(typeof(Type), name, true);
        }

        /// <summary>
        /// Returns the <see cref="Type"/> of the provided <paramref name="type"/>.
        /// </summary>
        public static Type ResolveType(byte type)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(type < Enum.GetValues(typeof(Type)).Length);// Type.values().length;
                                                                     //return Type.values()[type];
            return (Type)type;
        }
    }
}

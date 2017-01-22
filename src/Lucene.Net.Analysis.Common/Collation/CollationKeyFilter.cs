﻿using Icu.Collation;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Collation
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
	/// <para>
	///   Converts each token into its <seealso cref="java.text.CollationKey"/>, and then
	///   encodes the CollationKey with <seealso cref="IndexableBinaryStringTools"/>, to allow 
	///   it to be stored as an index term.
	/// </para>
	/// <para>
	///   <strong>WARNING:</strong> Make sure you use exactly the same Collator at
	///   index and query time -- CollationKeys are only comparable when produced by
	///   the same Collator.  Since <seealso cref="java.text.RuleBasedCollator"/>s are not
	///   independently versioned, it is unsafe to search against stored
	///   CollationKeys unless the following are exactly the same (best practice is
	///   to store this information with the index and check that they remain the
	///   same at query time):
	/// </para>
	/// <ol>
	///   <li>JVM vendor</li>
	///   <li>JVM version, including patch version</li>
	///   <li>
	///     The language (and country and variant, if specified) of the Locale
	///     used when constructing the collator via
	///     <seealso cref="Collator#getInstance(CultureInfo)"/>.
	///   </li>
	///   <li>
	///     The collation strength used - see <seealso cref="Collator#setStrength(int)"/>
	///   </li>
	/// </ol> 
	/// <para>
	///   The <code>ICUCollationKeyFilter</code> in the analysis-icu package 
	///   uses ICU4J's Collator, which makes its
	///   version available, thus allowing collation to be versioned independently
	///   from the JVM.  ICUCollationKeyFilter is also significantly faster and
	///   generates significantly shorter keys than CollationKeyFilter.  See
	///   <a href="http://site.icu-project.org/charts/collation-icu4j-sun"
	///   >http://site.icu-project.org/charts/collation-icu4j-sun</a> for key
	///   generation timing and key length comparisons between ICU4J and
	///   java.text.Collator over several languages.
	/// </para>
	/// <para>
	///   CollationKeys generated by java.text.Collators are not compatible
	///   with those those generated by ICU Collators.  Specifically, if you use 
	///   CollationKeyFilter to generate index terms, do not use
	///   ICUCollationKeyFilter on the query side, or vice versa.
	/// </para> </summary>
	/// @deprecated Use <seealso cref="CollationAttributeFactory"/> instead, which encodes
	///  terms directly as bytes. This filter will be removed in Lucene 5.0 
	[Obsolete("Use <seealso cref=\"CollationAttributeFactory\"/> instead, which encodes")]
	public sealed class CollationKeyFilter : TokenFilter
	{
		private readonly Collator collator;
		private readonly ICharTermAttribute termAtt;

		/// <param name="input"> Source token stream </param>
		/// <param name="collator"> CollationKey generator </param>
		public CollationKeyFilter(TokenStream input, Collator collator) : base(input)
		{
			this.collator = collator;
			this.termAtt = this.AddAttribute<ICharTermAttribute>();
		}

		public override bool IncrementToken()
		{
			if (this.m_input.IncrementToken())
			{
				var collationKey = this.collator.GetSortKey(this.termAtt.ToString()).KeyData.ToSByteArray();
				var encodedLength = IndexableBinaryStringTools.GetEncodedLength(collationKey, 0, collationKey.Length);
				this.termAtt.ResizeBuffer(encodedLength);
				this.termAtt.Length = encodedLength;

				IndexableBinaryStringTools.Encode(collationKey, 0, collationKey.Length, this.termAtt.Buffer, 0, encodedLength);
				return true;
			}
			else
			{
				return false;
			}
		}
	}

	internal static class ByteArrayExtentions
	{
		internal static byte[] ToByteArray(this sbyte[] arr)
		{
			var unsigned = new byte[arr.Length];
			System.Buffer.BlockCopy(arr, 0, unsigned, 0, arr.Length);
			return unsigned;
		}

		internal static sbyte[] ToSByteArray(this byte[] arr)
		{
			var unsigned = new sbyte[arr.Length];
			System.Buffer.BlockCopy(arr, 0, unsigned, 0, arr.Length);
			return unsigned;
		}
	}
}
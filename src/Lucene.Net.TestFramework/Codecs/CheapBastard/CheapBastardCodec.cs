/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using Lucene.Net.Codecs.DiskDV;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Codecs.Lucene46;

namespace Lucene.Net.Codecs.CheapBastard
{
    /// <summary>
    /// Codec that tries to use as little ram as possible because he spent all his money on beer
    /// </summary>
    // TODO: better name :) 
    // but if we named it "LowMemory" in codecs/ package, it would be irresistible like optimize()!
    public class CheapBastardCodec : FilterCodec
    {
        // TODO: would be better to have no terms index at all and bsearch a terms dict
        private readonly PostingsFormat postings;
        // uncompressing versions, waste lots of disk but no ram
        private readonly StoredFieldsFormat storedFields = new Lucene40StoredFieldsFormat();
        private readonly TermVectorsFormat termVectors = new Lucene40TermVectorsFormat();
        // these go to disk for all docvalues/norms datastructures
        private readonly DocValuesFormat docValues;
        private readonly NormsFormat norms = new DiskNormsFormat();

        public CheapBastardCodec(ICodecProvider codecProvider)
            : base(new Lucene46Codec(codecProvider))
        {
            postings = new Lucene41PostingsFormat(codecProvider, 100, 200);
            docValues = new DiskDocValuesFormat(codecProvider);
        }

        public override PostingsFormat PostingsFormat
        {
            get { return postings; }
        }

        public override DocValuesFormat DocValuesFormat
        {
            get { return docValues; }
        }

        public override NormsFormat NormsFormat
        {
            get { return norms; }
        }

        public override StoredFieldsFormat StoredFieldsFormat
        {
            get { return storedFields; }
        }

        public override TermVectorsFormat TermVectorsFormat
        {
            get { return termVectors; }
        }
    }
}

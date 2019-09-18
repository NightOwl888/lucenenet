namespace Lucene.Net.Codecs
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
    /// The default implementation of <see cref="ICodecProvider"/> that simply cascades calls
    /// to the statically registered <see cref="ICodecFactory"/>, <see cref="IDocValuesFormatFactory"/>, and <see cref="IPostingsFormatFactory"/> instances
    /// that are set using <see cref="Codec.SetCodecFactory(ICodecFactory)"/>, <see cref="DocValuesFormat.SetDocValuesFormatFactory(IDocValuesFormatFactory)"/>,
    /// and <see cref="PostingsFormat.SetPostingsFormatFactory(IPostingsFormatFactory)"/> respectively.
    /// </summary>
    public sealed class CodecProvider : ICodecProvider
    {
        private CodecProvider()
        { }

        /// <summary>
        /// The default instance of <see cref="ICodecProvider"/> that can be used to
        /// get cached instances of <see cref="Codecs.Codec"/>, <see cref="Codecs.DocValuesFormat"/>,
        /// and <see cref="Codecs.PostingsFormat"/> instances.
        /// </summary>
        public static ICodecProvider Default { get; } = new CodecProvider();

        /// <summary>
        /// A facade around <see cref="Codecs.Codec"/> that can be used to get instances
        /// of <see cref="Codecs.Codec"/> or set the default codec at runtime.
        /// </summary>
        public ICodec Codec { get; } = new DefaultCodec();

        /// <summary>
        /// A facade around <see cref="Codecs.DocValuesFormat"/> that can be used to get instances
        /// of <see cref="Codecs.DocValuesFormat"/> at runtime.
        /// </summary>
        public IDocValuesFormat DocValuesFormat { get; } = new DefaultDocValuesFormat();

        /// <summary>
        /// A facade around <see cref="Codecs.PostingsFormat"/> that can be used to get instances
        /// of <see cref="Codecs.PostingsFormat"/> at runtime.
        /// </summary>
        public IPostingsFormat PostingsFormat { get; } = new DefaultPostingsFormat();
    }
}

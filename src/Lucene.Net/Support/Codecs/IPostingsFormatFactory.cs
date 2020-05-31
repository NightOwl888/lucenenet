﻿using Lucene.Net.Util;

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
    /// Contract for extending the functionality of <see cref="PostingsFormat"/> implementations so
    /// they can be injected with dependencies.
    /// <para/>
    /// To set the <see cref="IPostingsFormatFactory"/>, call <see cref="PostingsFormat.SetPostingsFormatFactory(IPostingsFormatFactory)"/>.
    /// <para/>
    /// See the <see cref="Lucene.Net.Codecs"/> namespace documentation for some common usage examples.
    /// </summary>
    /// <seealso cref="DefaultPostingsFormatFactory"/>
    /// <seealso cref="IServiceListable"/>
    // LUCENENET specific
    public interface IPostingsFormatFactory
    {
        /// <summary>
        /// Gets the <see cref="PostingsFormat"/> instance from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="PostingsFormat"/> instance to retrieve.</param>
        /// <returns>The <see cref="PostingsFormat"/> instance.</returns>
        PostingsFormat GetPostingsFormat(string name);
    }
}

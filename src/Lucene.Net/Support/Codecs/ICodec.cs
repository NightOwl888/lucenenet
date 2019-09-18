using System.Collections.Generic;

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
    /// Facade interface to provide a seam where the static <see cref="Codec"/> methods can
    /// be swapped with another implementation.
    /// <para/>
    /// Note that <see cref="Codec"/> doesn't acutally implement <see cref="ICodec"/>, this interface
    /// only matches its static members so they are not used directly by any <see cref="Codec"/> derived types.
    /// </summary>
    public interface ICodec
    {
        Codec ForName(string name);
        Codec Default { get; set; }

        ICollection<string> AvailableCodecs { get; }
    }
}

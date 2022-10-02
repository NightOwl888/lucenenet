﻿// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Ko.Dict;
using Lucene.Net.Support.Util.Fst;
using System.IO;
using Long = J2N.Numerics.Int64;

namespace Lucene.Net.Analysis.Ko.Util
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

    public class TokenInfoDictionaryWriter : BinaryDictionaryWriter
    {
        private FST<Long> fst;

        public TokenInfoDictionaryWriter(int size)
            : base(typeof(TokenInfoDictionary), size)
        {
        }

        public virtual void SetFST(FST<Long> fst)
        {
            this.fst = fst;
        }

        public override void Write(string baseDir)
        {
            base.Write(baseDir);
            WriteFST(GetBaseFileName(baseDir) + TokenInfoDictionary.FST_FILENAME_SUFFIX);
        }

        protected void WriteFST(string filename)
        {
            FileInfo f = new FileInfo(filename);
            if (!f.Directory.Exists) f.Directory.Create();
            fst.Save(f);
            //Path p = Paths.get(filename);
            //Files.createDirectories(p.getParent());
            //fst.save(p);
        }
    }
}

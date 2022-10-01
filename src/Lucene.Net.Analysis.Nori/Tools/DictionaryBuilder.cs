// Lucene version compatibility level 8.2.0
using Console = Lucene.Net.Util.SystemConsole;

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

    public static class DictionaryBuilder
    {
        static DictionaryBuilder()
        {
#if FEATURE_ENCODINGPROVIDERS
            // Support for additional encoding types. See: https://docs.microsoft.com/en-us/dotnet/api/system.text.codepagesencodingprovider?view=netcore-2.0
            var encodingProvider = System.Text.CodePagesEncodingProvider.Instance;
            System.Text.Encoding.RegisterProvider(encodingProvider);
#endif
        }

        public static void Build(string inputDirname, string outputDirname, string encoding, bool normalizeEntry)
        {
            Console.Out.WriteLine("building tokeninfo dict...");
            TokenInfoDictionaryBuilder tokenInfoBuilder = new TokenInfoDictionaryBuilder(encoding, normalizeEntry);
            TokenInfoDictionaryWriter tokenInfoDictionary = tokenInfoBuilder.Build(inputDirname);
            tokenInfoDictionary.Write(outputDirname);
            tokenInfoDictionary = null;
            tokenInfoBuilder = null;
            Console.Out.WriteLine("done");

            Console.Out.WriteLine("building unknown word dict...");
            UnknownDictionaryBuilder unkBuilder = new UnknownDictionaryBuilder(encoding);
            UnknownDictionaryWriter unkDictionary = unkBuilder.Build(inputDirname);
            unkDictionary.Write(outputDirname);
            unkDictionary = null;
            unkBuilder = null;
            Console.Out.WriteLine("done");

            Console.Out.WriteLine("building connection costs...");
            ConnectionCostsWriter connectionCosts
              = ConnectionCostsBuilder.Build(inputDirname + System.IO.Path.DirectorySeparatorChar + "matrix.def");
            connectionCosts.Write(outputDirname);
            Console.Out.WriteLine("done");
        }

        public static void Main(string[] args)
        {
            string inputDirname = args[0];
            string outputDirname = args[1];
            string inputEncoding = args.Length > 2 ? args[2] : "utf-8";
            bool normalizeEntries = args.Length > 3 ? bool.Parse(args[3]) : false;

            Console.Out.WriteLine("dictionary builder");
            Console.Out.WriteLine("");
            Console.Out.WriteLine("input directory: " + inputDirname);
            Console.Out.WriteLine("output directory: " + outputDirname);
            Console.Out.WriteLine("input encoding: " + inputEncoding);
            Console.Out.WriteLine("normalize entries: " + normalizeEntries);
            Console.Out.WriteLine("");
            DictionaryBuilder.Build(inputDirname, outputDirname, inputEncoding, normalizeEntries);
        }
    }
}

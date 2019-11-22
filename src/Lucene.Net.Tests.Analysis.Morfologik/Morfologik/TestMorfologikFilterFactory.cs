﻿using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Morfologik
{
    /// <summary>
    /// Test for <see cref="MorfologikFilterFactory"/>
    /// </summary>
    public class TestMorfologikFilterFactory : BaseTokenStreamTestCase
    {
        private class ForbidResourcesLoader : IResourceLoader
        {
            public Type FindType(string cname)
            {
                throw new NotSupportedException();
            }

            public T NewInstance<T>(string cname)
            {
                throw new NotSupportedException();
            }

            public Stream OpenResource(string resource)
            {
                throw new NotSupportedException();
            }
        }

        [Test]
        public void TestDefaultDictionary()
        {
            StringReader reader = new StringReader("rowery bilety");
            MorfologikFilterFactory factory = new MorfologikFilterFactory(Collections.EmptyMap<String, String>());
            factory.Inform(new ForbidResourcesLoader());
            TokenStream stream = new MockTokenizer(reader); //whitespaceMockTokenizer(reader);
            stream = factory.Create(stream);
            AssertTokenStreamContents(stream, new String[] { "rower", "bilet" });
        }

        [Test]
        public void TestExplicitDictionary()
        {
            IResourceLoader loader = new ClasspathResourceLoader(typeof(TestMorfologikFilterFactory));

            StringReader reader = new StringReader("inflected1 inflected2");
            IDictionary<String, String> @params = new HashMap<string, string>();
            @params[MorfologikFilterFactory.DICTIONARY_ATTRIBUTE] = "custom-dictionary.dict";
            MorfologikFilterFactory factory = new MorfologikFilterFactory(@params);
            factory.Inform(loader);
            TokenStream stream = new MockTokenizer(reader); // whitespaceMockTokenizer(reader);
            stream = factory.Create(stream);
            AssertTokenStreamContents(stream, new String[] { "lemma1", "lemma2" });
        }

        [Test]
        public void TestMissingDictionary()
        {
            IResourceLoader loader = new ClasspathResourceLoader(typeof(TestMorfologikFilterFactory));

            IOException expected = NUnit.Framework.Assert.Throws<IOException>(() =>
            {
                IDictionary<String, String> @params = new HashMap<String, String>();
                @params[MorfologikFilterFactory.DICTIONARY_ATTRIBUTE] = "missing-dictionary-resource.dict";
                MorfologikFilterFactory factory = new MorfologikFilterFactory(@params);
                factory.Inform(loader);
            });

            assertTrue(expected.Message.Contains("Resource not found"));
        }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            ArgumentException expected = NUnit.Framework.Assert.Throws<ArgumentException>(() =>
            {
                HashMap<String, String> @params = new HashMap<String, String>();
                @params["bogusArg"] = "bogusValue";
                new MorfologikFilterFactory(@params);
            });

            assertTrue(expected.Message.Contains("Unknown parameters"));
        }
    }
}

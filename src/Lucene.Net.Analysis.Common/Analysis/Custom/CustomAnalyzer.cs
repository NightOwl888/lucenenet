//// Lucene version compatibility level 8.2.0
//using Lucene.Net.Analysis.Common.Analysis.Miscellaneous;
//using Lucene.Net.Analysis.TokenAttributes;
//using Lucene.Net.Analysis.Util;
//using Lucene.Net.Support;
//using Lucene.Net.Util;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;

//namespace Lucene.Net.Analysis.Common.Analysis.Custom
//{
//    public sealed class CustomAnalyzer : Analyzer
//    {
//        /// <summary>
//        /// Returns a builder for custom analyzers that loads all resources from
//        /// Lucene's classloader. All path names given must be absolute with package prefixes.
//        /// </summary>
//        /// <returns></returns>
//        public static CustomAnalyzerBuilder Builder()
//        {
//            return Builder(new ClasspathResourceLoader(typeof(CustomAnalyzer)));
//        }

//        /// <summary>
//        /// Returns a builder for custom analyzers that loads all resources from the given
//        /// file system base directory. Place, e.g., stop word files there.
//        /// Files that are not in the given directory are loaded from Lucene's classloader.
//        /// </summary>
//        /// <param name="configDir"></param>
//        /// <returns></returns>
//        public static CustomAnalyzerBuilder Builder(DirectoryInfo configDir)
//        {
//            return Builder(new FilesystemResourceLoader(configDir, typeof(CustomAnalyzer).Assembly));
//        }

//        /// <summary>Returns a builder for custom analyzers that loads all resources using the given <paramref name="loader"/>.</summary>
//        public static CustomAnalyzerBuilder Builder(IResourceLoader loader)
//        {
//            return new CustomAnalyzerBuilder(loader);
//        }

//        private readonly CharFilterFactory[] charFilters;
//        private readonly TokenizerFactory tokenizer;
//        private readonly TokenFilterFactory[] tokenFilters;
//        private readonly int? posIncGap, offsetGap;

//        internal CustomAnalyzer(LuceneVersion? defaultMatchVersion, CharFilterFactory[] charFilters, TokenizerFactory tokenizer, TokenFilterFactory[] tokenFilters, int? posIncGap, int? offsetGap)
//        {
//            this.charFilters = charFilters;
//            this.tokenizer = tokenizer;
//            this.tokenFilters = tokenFilters;
//            this.posIncGap = posIncGap;
//            this.offsetGap = offsetGap;
//            //SetVersion(defaultMatchVersion); // LUCENENET TODO: Implementation
//        }

//        protected override TextReader InitReader(string fieldName, TextReader reader)
//        {
//            foreach (CharFilterFactory charFilter in charFilters)
//            {
//                reader = charFilter.Create(reader);
//            }
//            return reader;
//        }

//        // LUCENENET: Not yet available on Lucene 4.8.0
//        //  protected override TextReader InitReaderForNormalization(string fieldName, TextReader reader)
//        //{
//        //    foreach (CharFilterFactory charFilter in charFilters)
//        //    {
//        //        reader = charFilter.Normalize(reader);
//        //    }
//        //    return reader;
//        //}

//        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
//        {
//            Tokenizer tk = tokenizer.Create(AttributeFactory(fieldName));
//            TokenStream ts = tk;
//            foreach (TokenFilterFactory filter in tokenFilters)
//            {
//                ts = filter.Create(ts);
//            }
//            return new TokenStreamComponents(tk, ts);
//        }

//        //  protected override TokenStream Normalize(string fieldName, TokenStream input)
//        //{
//        //    TokenStream result = input;
//        //    foreach (TokenFilterFactory filter in tokenFilters)
//        //    {
//        //        result = filter.Normalize(result);
//        //    }
//        //    return result;
//        //}

//        // LUCENENET: Not yet available on Lucene 4.8.0
//        public override int GetPositionIncrementGap(string fieldName)
//        {
//            // use default from Analyzer base class if null
//            return (posIncGap == null) ? base.GetPositionIncrementGap(fieldName) : posIncGap.Value;
//        }

//        public override int GetOffsetGap(string fieldName)
//        {
//            // use default from Analyzer base class if null
//            return (offsetGap == null) ? base.GetOffsetGap(fieldName) : offsetGap.Value;
//        }

//        /// <summary>Returns the list of char filters that are used in this analyzer.</summary>
//        public IList<CharFilterFactory> CharFilterFactories => Collections.UnmodifiableList(Arrays.AsList(charFilters));


//        /** Returns the tokenizer that is used in this analyzer. */
//        public TokenizerFactory TokenizerFactory => tokenizer;


//        /// <summary>Returns the list of token filters that are used in this analyzer.</summary>
//        public IList<TokenFilterFactory> TokenFilterFactories => Collections.UnmodifiableList(Arrays.AsList(tokenFilters));


//        public override string ToString()
//        {
//            StringBuilder sb = new StringBuilder(GetType().Name).Append('(');
//            foreach (CharFilterFactory filter in charFilters)
//            {
//                sb.Append(filter).Append(',');
//            }
//            sb.Append(tokenizer);
//            foreach (TokenFilterFactory filter in tokenFilters)
//            {
//                sb.Append(',').Append(filter);
//            }
//            return sb.Append(')').ToString();
//        }

//        // LUCENENET specific - de-nested Builder and renamed CustomAnalyzerBuilder

//        // LUCENENET specific - de-nested ConditionBuilde
//    }

//    /// <summary>
//    /// Builder for <see cref="CustomAnalyzer"/>.
//    /// </summary>
//    /// <seealso cref="CustomAnalyzer.Builder()"/>
//    /// <seealso cref="CustomAnalyzer.Builder(DirectoryInfo)"/>
//    /// <seealso cref="CustomAnalyzer.Builder(IResourceLoader)"/>
//    public sealed class CustomAnalyzerBuilder
//    {
//        private readonly IResourceLoader loader;
//        //private readonly SetOnce<LuceneVersion> defaultMatchVersion = new SetOnce<LuceneVersion>();
//        private LuceneVersion? defaultMatchVersion;
//        private readonly IList<CharFilterFactory> charFilters = new List<CharFilterFactory>();
//        private readonly SetOnce<TokenizerFactory> tokenizer = new SetOnce<TokenizerFactory>();
//        private readonly IList<TokenFilterFactory> tokenFilters = new List<TokenFilterFactory>();
//        //private readonly SetOnce<int?> posIncGap = new SetOnce<int?>();
//        //private readonly SetOnce<int?> offsetGap = new SetOnce<int?>();
//        private int? posIncGap;
//        private int? offsetGap;

//        private bool componentsAdded = false;

//        internal CustomAnalyzerBuilder(IResourceLoader loader)
//        {
//            this.loader = loader;
//        }

//        /// <summary>
//        /// This match version is passed as default to all tokenizers or filters. It is used unless you
//        /// pass the parameter <c>luceneMatchVersion</c> explicitly. It defaults to undefined, so the
//        /// underlying factory will (in most cases) use <see cref="LuceneVersion.LUCENE_CURRENT"/>
//        /// </summary>
//        public CustomAnalyzerBuilder WithDefaultMatchVersion(LuceneVersion version)
//        {
//            //Objects.requireNonNull(version, "version may not be null"); // LUCENENET: value types cannot be null in .NET
//            if (componentsAdded)
//            {
//                throw new InvalidOperationException("You may only set the default match version before adding tokenizers, " +
//                    "token filters, or char filters.");
//            }
//            if (this.defaultMatchVersion.HasValue)
//                throw new AlreadySetException();
//            this.defaultMatchVersion = version;
//            //this.defaultMatchVersion.Set(version);
//            return this;
//        }

//        /// <summary>
//        /// Sets the position increment gap of the analyzer.
//        /// The default is defined in the analyzer base class.
//        /// </summary>
//        /// <seealso cref="Analyzer.GetPositionIncrementGap(string)"/>
//        public CustomAnalyzerBuilder WithPositionIncrementGap(int posIncGap)
//        {
//            if (posIncGap < 0)
//            {
//                throw new ArgumentException("posIncGap must be >= 0");
//            }
//            if (this.posIncGap.HasValue)
//                throw new AlreadySetException();
//            this.posIncGap = posIncGap;
//            //this.posIncGap.Set(posIncGap);
//            return this;
//        }

//        /// <summary>
//        /// Sets the offset gap of the analyzer. The default is defined
//        /// in the analyzer base class.
//        /// </summary>
//        /// <seealso cref="Analyzer.GetOffsetGap(string)"/>
//        public CustomAnalyzerBuilder WithOffsetGap(int offsetGap)
//        {
//            if (offsetGap < 0)
//            {
//                throw new ArgumentException("offsetGap must be >= 0");
//            }
//            if (this.offsetGap.HasValue)
//                throw new AlreadySetException();
//            this.offsetGap = offsetGap;
//            //this.offsetGap.Set(offsetGap);
//            return this;
//        }

//        /// <summary>
//        /// Uses the given tokenizer.
//        /// </summary>
//        /// <typeparam name="T"><see cref="Type"/> of class that is used to create the tokenizer.</typeparam>
//        /// <param name="parameters">A list of factory string params as key/value pairs.
//        /// The number of parameters must be an even number, as they are pairs.</param>
//        public CustomAnalyzerBuilder WithTokenizer<T>(params string[] parameters) where T : TokenizerFactory // LUCENENET: Removed unnecessary factory parameter
//        {
//            return WithTokenizer<T>(ParamsToMap(parameters));
//        }

//        /// <summary>
//        /// Uses the given tokenizer.
//        /// </summary>
//        /// <param name="factory"><see cref="Type"/> of class that is used to create the tokenizer.</param>
//        /// <param name="parameters">A list of factory string params as key/value pairs.
//        /// The number of parameters must be an even number, as they are pairs.</param>
//        public CustomAnalyzerBuilder WithTokenizer(Type factory, params string[] parameters) // LUCENENET overload to pass runtime type
//        {
//            return WithTokenizer(factory, ParamsToMap(parameters));
//        }

//        /// <summary>
//        /// Uses the given tokenizer.
//        /// </summary>
//        /// <typeparam name="T"><see cref="Type"/> of class that is used to create the tokenizer.</typeparam>
//        /// <param name="parameters">The dictionary of parameters to be passed to factory. The dictionary must be modifiable.</param>
//        public CustomAnalyzerBuilder WithTokenizer<T>(IDictionary<string, string> parameters) where T : TokenizerFactory // LUCENENET: Removed unnecessary factory parameter
//        {
//            //Objects.requireNonNull(factory, "Tokenizer factory may not be null");
//            tokenizer.Set(ApplyResourceLoader(AnalysisSPILoader<T>.NewFactoryClassInstance<T>(ApplyDefaultParams(parameters))));
//            componentsAdded = true;
//            return this;
//        }

//        /// <summary>
//        /// Uses the given tokenizer.
//        /// </summary>
//        /// <param name="factory"><see cref="Type"/> of class that is used to create the tokenizer.</param>
//        /// <param name="parameters">The dictionary of parameters to be passed to factory. The dictionary must be modifiable.</param>
//        public CustomAnalyzerBuilder WithTokenizer(Type factory, IDictionary<string, string> parameters) // LUCENENET overload to pass runtime type
//        {
//            if (factory == null)
//                throw new ArgumentNullException(nameof(factory), "Tokenizer factory may not be null");
//            //Objects.requireNonNull(factory, "Tokenizer factory may not be null");
//            tokenizer.Set(ApplyResourceLoader(AnalysisSPILoader<TokenizerFactory>.NewFactoryClassInstance(factory, ApplyDefaultParams(parameters))));
//            componentsAdded = true;
//            return this;
//        }

//        /// <summary>
//        /// Uses the given tokenizer.
//        /// </summary>
//        /// <param name="name">Used to look up the factory with <see cref="TokenFilterFactory.ForName(string, IDictionary{string, string})"/>.
//        /// The list of possible names can be looked up with <see cref="TokenFilterFactory.AvailableTokenFilters"/>.</param>
//        /// <param name="parameters">A list of factory string params as key/value pairs.
//        /// The number of parameters must be an even number, as they are pairs.</param>
//        /// <returns></returns>
//        public CustomAnalyzerBuilder WithTokenizer(string name, params string[] parameters)
//        {
//            return WithTokenizer(name, ParamsToMap(parameters));
//        }

//        /// <summary>
//        /// Uses the given tokenizer.
//        /// </summary>
//        /// <param name="name">Used to look up the factory with <see cref="TokenFilterFactory.ForName(string, IDictionary{string, string})"/>.
//        /// The list of possible names can be looked up with <see cref="TokenFilterFactory.AvailableTokenFilters"/>.</param>
//        /// <param name="parameters">The dictionary of parameters to be passed to factory. The dictionary must be modifiable.</param>
//        public CustomAnalyzerBuilder WithTokenizer(string name, IDictionary<string, string> parameters)
//        {
//            //Objects.requireNonNull(name, "Tokenizer name may not be null");
//            if (name == null)
//                throw new ArgumentNullException(nameof(name), "Tokenizer name may not be null");
//            tokenizer.Set(ApplyResourceLoader(TokenizerFactory.ForName(name, ApplyDefaultParams(parameters))));
//            componentsAdded = true;
//            return this;
//        }

//        /// <summary>
//        /// Adds the given token filter.
//        /// </summary>
//        /// <typeparam name="T"><see cref="Type"/> of class that is used to create the token filter.</typeparam>
//        /// <param name="parameters">A list of factory string params as key/value pairs.
//        /// The number of parameters must be an even number, as they are pairs.</param>
//        public CustomAnalyzerBuilder AddTokenFilter<T>(params string[] parameters) where T : TokenFilterFactory // LUCENENET: Removed unnecessary factory parameter
//        {
//            return AddTokenFilter<T>(ParamsToMap(parameters));
//        }

//        /// <summary>
//        /// Adds the given token filter.
//        /// </summary>
//        /// <param name="factory"><see cref="Type"/> of class that is used to create the token filter.</param>
//        /// <param name="parameters">A list of factory string params as key/value pairs.
//        /// The number of parameters must be an even number, as they are pairs.</param>
//        public CustomAnalyzerBuilder AddTokenFilter(Type factory, params string[] parameters) // LUCENENET overload to pass runtime type
//        {
//            return AddTokenFilter(factory, ParamsToMap(parameters));
//        }

//        /// <summary>
//        /// Adds the given token filter.
//        /// </summary>
//        /// <typeparam name="T"><see cref="Type"/> of class that is used to create the token filter.</typeparam>
//        /// <param name="parameters">The dictionary of parameters to be passed to factory. The dictionary must be modifiable.</param>
//        public CustomAnalyzerBuilder AddTokenFilter<T>(IDictionary<string, string> parameters) where T : TokenFilterFactory // LUCENENET: Removed unnecessary factory parameter
//        {
//            //Objects.requireNonNull(factory, "TokenFilter name may not be null");
//            tokenFilters.Add(ApplyResourceLoader(AnalysisSPILoader<T>.NewFactoryClassInstance<T>(ApplyDefaultParams(parameters))));
//            componentsAdded = true;
//            return this;
//        }

//        /// <summary>
//        /// Adds the given token filter.
//        /// </summary>
//        /// <param name="factory"><see cref="Type"/> of class that is used to create the token filter.</param>
//        /// <param name="parameters">The dictionary of parameters to be passed to factory. The dictionary must be modifiable.</param>
//        public CustomAnalyzerBuilder AddTokenFilter(Type factory, IDictionary<string, string> parameters) // LUCENENET overload to pass runtime type
//        {
//            if (factory == null)
//                throw new ArgumentNullException(nameof(factory), "TokenFilter name may not be null");
//            //Objects.requireNonNull(factory, "TokenFilter name may not be null");
//            tokenFilters.Add(ApplyResourceLoader(AnalysisSPILoader<TokenFilterFactory>.NewFactoryClassInstance(factory, ApplyDefaultParams(parameters))));
//            componentsAdded = true;
//            return this;
//        }

//        /// <summary>
//        /// Adds the given token filter.
//        /// </summary>
//        /// <param name="name">Used to look up the factory with <see cref="TokenFilterFactory.ForName(string, IDictionary{string, string})"/>.
//        /// The list of possible names can be looked up with <see cref="TokenFilterFactory.AvailableTokenFilters"/>.</param>
//        /// <param name="parameters">A list of factory string params as key/value pairs.
//        /// The number of parameters must be an even number, as they are pairs.</param>
//        public CustomAnalyzerBuilder AddTokenFilter(string name, params string[] parameters)
//        {
//            return AddTokenFilter(name, ParamsToMap(parameters));
//        }

//        /// <summary>
//        /// Adds the given token filter.
//        /// </summary>
//        /// <param name="name">Used to look up the factory with <see cref="TokenFilterFactory.ForName(string, IDictionary{string, string})"/>.
//        /// The list of possible names can be looked up with <see cref="TokenFilterFactory.AvailableTokenFilters"/>.</param>
//        /// <param name="parameters">The dictionary of parameters to be passed to factory. The dictionary must be modifiable.</param>
//        public CustomAnalyzerBuilder AddTokenFilter(string name, IDictionary<string, string> parameters)
//        {
//            if (name == null)
//                throw new ArgumentNullException(nameof(name), "TokenFilter name may not be null");
//            //Objects.requireNonNull(name, "TokenFilter name may not be null");
//            tokenFilters.Add(ApplyResourceLoader(TokenFilterFactory.ForName(name, ApplyDefaultParams(parameters))));
//            componentsAdded = true;
//            return this;
//        }

//        private CustomAnalyzerBuilder AddTokenFilter(TokenFilterFactory factory)
//        {
//            if (factory == null)
//                throw new ArgumentNullException(nameof(factory), "TokenFilterFactory may not be null");
//            //Objects.requireNonNull(factory, "TokenFilterFactory may not be null");
//            tokenFilters.Add(factory);
//            componentsAdded = true;
//            return this;
//        }

//        /// <summary>
//        /// Adds the given char filter.
//        /// </summary>
//        /// <typeparam name="T"><see cref="Type"/> of class that is used to create the char filter.</typeparam>
//        /// <param name="parameters">A list of factory string params as key/value pairs.
//        /// The number of parameters must be an even number, as they are pairs.</param>
//        public CustomAnalyzerBuilder AddCharFilter<T>(params string[] parameters) where T : CharFilterFactory // LUCENENET: Removed unnecessary factory parameter
//        {
//            return AddCharFilter<T>(ParamsToMap(parameters));
//        }

//        /// <summary>
//        /// Adds the given char filter.
//        /// </summary>
//        /// <param name="factory"><see cref="Type"/> of class that is used to create the char filter.</param>
//        /// <param name="parameters">A list of factory string params as key/value pairs.
//        /// The number of parameters must be an even number, as they are pairs.</param>
//        public CustomAnalyzerBuilder AddCharFilter(Type factory, params string[] parameters) // LUCENENET overload to pass runtime type
//        {
//            return AddCharFilter(factory, ParamsToMap(parameters));
//        }

//        /// <summary>
//        /// Adds the given char filter.
//        /// </summary>
//        /// <typeparam name="T"><see cref="Type"/> of class that is used to create the char filter.</typeparam>
//        /// <param name="parameters">The dictionary of parameters to be passed to factory. The dictionary must be modifiable.</param>
//        public CustomAnalyzerBuilder AddCharFilter<T>(IDictionary<string, string> parameters) where T : CharFilterFactory // LUCENENET: Removed unnecessary factory parameter
//        {
//            //Objects.requireNonNull(factory, "CharFilter name may not be null");
//            charFilters.Add(ApplyResourceLoader(AnalysisSPILoader<T>.NewFactoryClassInstance<T>(ApplyDefaultParams(parameters))));
//            componentsAdded = true;
//            return this;
//        }

//        /// <summary>
//        /// Adds the given char filter.
//        /// </summary>
//        /// <param name="factory"><see cref="Type"/> of class that is used to create the char filter.</param>
//        /// <param name="parameters">The dictionary of parameters to be passed to factory. The dictionary must be modifiable.</param>
//        public CustomAnalyzerBuilder AddCharFilter(Type factory, IDictionary<string, string> parameters) // LUCENENET overload to pass runtime type
//        {
//            if (factory == null)
//                throw new ArgumentNullException(nameof(factory), "CharFilter factory may not be null");
//            //Objects.requireNonNull(factory, "CharFilter name may not be null");
//            charFilters.Add(ApplyResourceLoader(AnalysisSPILoader<CharFilterFactory>.NewFactoryClassInstance(factory, ApplyDefaultParams(parameters))));
//            componentsAdded = true;
//            return this;
//        }

//        /// <summary>
//        /// Adds the given char filter.
//        /// </summary>
//        /// <param name="name">Used to look up the factory with <see cref="TokenFilterFactory.ForName(string, IDictionary{string, string})"/>.</param>
//        /// <param name="parameters">A list of factory string params as key/value pairs.
//        /// The number of parameters must be an even number, as they are pairs.</param>
//        /// <returns></returns>
//        public CustomAnalyzerBuilder AddCharFilter(string name, params string[] parameters)
//        {
//            return AddCharFilter(name, ParamsToMap(parameters));
//        }

//        /// <summary>
//        /// Adds the given char filter.
//        /// </summary>
//        /// <param name="name">Used to look up the factory with <see cref="TokenFilterFactory.ForName(string, IDictionary{string, string})"/>.</param>
//        /// <param name="parameters"></param>
//        /// <returns>The dictionary of parameters to be passed to factory. The dictionary must be modifiable.</returns>
//        public CustomAnalyzerBuilder AddCharFilter(string name, IDictionary<string, string> parameters)
//        {
//            if (name == null)
//                throw new ArgumentNullException(nameof(name), "CharFilter name may not be null");
//            //Objects.requireNonNull(name, "CharFilter name may not be null");
//            charFilters.Add(ApplyResourceLoader(CharFilterFactory.ForName(name, ApplyDefaultParams(parameters))));
//            componentsAdded = true;
//            return this;
//        }

//        /// <summary>
//        /// Add a <see cref="ConditionalTokenFilterFactory"/> to the analysis chain
//        /// <para/>
//        /// TokenFilters added by subsequent calls to <see cref="CustomAnalyzerConditionBuilder.AddTokenFilter(string, string[])"/>
//        /// and related functions will only be used if the current token matches the condition.  Consumers
//        /// must call <see cref="CustomAnalyzerConditionBuilder.EndWhen()"/> to return to the normal tokenfilter
//        /// chain once conditional filters have been added.
//        /// </summary>
//        /// <param name="name">Used to look up the factory with <see cref="TokenFilterFactory.ForName(string, IDictionary{string, string})"/>.</param>
//        /// <param name="parameters">The parameters to be passed to the factory.</param>
//        public CustomAnalyzerConditionBuilder When(string name, params string[] parameters)
//        {
//            return When(name, ParamsToMap(parameters));
//        }

//        /// <summary>
//        /// Add a <see cref="ConditionalTokenFilterFactory"/> to the analysis chain
//        /// <para/>
//        /// TokenFilters added by subsequent calls to <see cref="CustomAnalyzerConditionBuilder.AddTokenFilter(string, string[])"/>
//        /// and related functions will only be used if the current token matches the condition.  Consumers
//        /// must call <see cref="CustomAnalyzerConditionBuilder.EndWhen()"/> to return to the normal tokenfilter
//        /// chain once conditional filters have been added.
//        /// </summary>
//        /// <param name="name">Used to look up the factory with <see cref="TokenFilterFactory.ForName(string, IDictionary{string, string})"/>.</param>
//        /// <param name="parameters">The parameters to be passed to the factory. The dictionary must be modifiable.</param>
//        public CustomAnalyzerConditionBuilder When(string name, IDictionary<string, string> parameters)
//        {
//            Type clazz = TokenFilterFactory.LookupClass(name);
//            if (typeof(ConditionalTokenFilterFactory).IsAssignableFrom(clazz) == false)
//            {
//                throw new ArgumentException("TokenFilterFactory " + name + " is not a ConditionalTokenFilterFactory");
//            }
//            return When(clazz, parameters);
//        }

//        /// <summary>
//        /// Add a <see cref="ConditionalTokenFilterFactory"/> to the analysis chain
//        /// <para/>
//        /// TokenFilters added by subsequent calls to <see cref="CustomAnalyzerConditionBuilder.AddTokenFilter(string, string[])"/>
//        /// and related functions will only be used if the current token matches the condition.  Consumers
//        /// must call <see cref="CustomAnalyzerConditionBuilder.EndWhen()"/> to return to the normal tokenfilter
//        /// chain once conditional filters have been added.
//        /// </summary>
//        /// <typeparam name="T"><see cref="Type"/> of class that is used to create the <see cref="ConditionalTokenFilter"/>.</typeparam>
//        /// <param name="parameters">The parameters to be passed to the factory.</param>
//        public CustomAnalyzerConditionBuilder When<T>(params string[] parameters) where T : ConditionalTokenFilterFactory // LUCENENET: removed unnecessary factory parameter
//        {
//            return When<T>(ParamsToMap(parameters));
//        }

//        /// <summary>
//        /// Add a <see cref="ConditionalTokenFilterFactory"/> to the analysis chain
//        /// <para/>
//        /// TokenFilters added by subsequent calls to <see cref="CustomAnalyzerConditionBuilder.AddTokenFilter(string, string[])"/>
//        /// and related functions will only be used if the current token matches the condition.  Consumers
//        /// must call <see cref="CustomAnalyzerConditionBuilder.EndWhen()"/> to return to the normal tokenfilter
//        /// chain once conditional filters have been added.
//        /// </summary>
//        /// <param name="factory"><see cref="Type"/> of class that is used to create the <see cref="ConditionalTokenFilter"/>.</param>
//        /// <param name="parameters">The parameters to be passed to the factory.</param>
//        public CustomAnalyzerConditionBuilder When(Type factory, params string[] parameters) // LUCENENET overload to pass runtime type
//        {
//            return When(factory, ParamsToMap(parameters));
//        }

//        /// <summary>
//        /// Add a <see cref="ConditionalTokenFilterFactory"/> to the analysis chain
//        /// <para/>
//        /// TokenFilters added by subsequent calls to <see cref="CustomAnalyzerConditionBuilder.AddTokenFilter(string, string[])"/>
//        /// and related functions will only be used if the current token matches the condition.  Consumers
//        /// must call <see cref="CustomAnalyzerConditionBuilder.EndWhen()"/> to return to the normal tokenfilter
//        /// chain once conditional filters have been added.
//        /// </summary>
//        /// <typeparam name="T"><see cref="Type"/> of class that is used to create the <see cref="ConditionalTokenFilter"/>.</typeparam>
//        /// <param name="parameters">The parameters to be passed to the factory. The dictionary must be modifiable.</param>
//        public CustomAnalyzerConditionBuilder When<T>(IDictionary<string, string> parameters) where T : ConditionalTokenFilterFactory // LUCENENET: Removed unnecessary factory param
//        {
//            return When(AnalysisSPILoader<T>.NewFactoryClassInstance<T>(ApplyDefaultParams(parameters)));
//        }

//        /// <summary>
//        /// Add a <see cref="ConditionalTokenFilterFactory"/> to the analysis chain
//        /// <para/>
//        /// TokenFilters added by subsequent calls to <see cref="CustomAnalyzerConditionBuilder.AddTokenFilter(string, string[])"/>
//        /// and related functions will only be used if the current token matches the condition.  Consumers
//        /// must call <see cref="CustomAnalyzerConditionBuilder.EndWhen()"/> to return to the normal tokenfilter
//        /// chain once conditional filters have been added.
//        /// </summary>
//        /// <param name="factory"><see cref="Type"/> of class that is used to create the <see cref="ConditionalTokenFilter"/>.</param>
//        /// <param name="parameters">The parameters to be passed to the factory. The dictionary must be modifiable.</param>
//        public CustomAnalyzerConditionBuilder When(Type factory, IDictionary<string, string> parameters) // LUCENENET overload to pass runtime type
//        {
//            return When((ConditionalTokenFilterFactory)AnalysisSPILoader<ConditionalTokenFilterFactory>.NewFactoryClassInstance(factory, ApplyDefaultParams(parameters)));
//        }

//        /// <summary>
//        /// Add a <see cref="ConditionalTokenFilterFactory"/> to the analysis chain.
//        /// <para/>
//        /// TokenFilters added by subsequent calls to <see cref="CustomAnalyzerConditionBuilder.AddTokenFilter(string, string[])"/>
//        /// and related functions will only be used if the current token matches the condition.  Consumers
//        /// must call <see cref="CustomAnalyzerConditionBuilder.EndWhen()"/> to return to the normal tokenfilter
//        /// chain once conditional filters have been added.
//        /// </summary>
//        public CustomAnalyzerConditionBuilder When(ConditionalTokenFilterFactory factory)
//        {
//            return new CustomAnalyzerConditionBuilder(factory, this);
//        }

//        /// <summary>
//        /// Apply subsequent token filters if the current token's term matches a predicate
//        /// <para/>
//        /// This is the equivalent of:
//        /// <code>
//        /// When(new FilteredConditionalTokenFilterFactory(predicate));
//        /// 
//        /// class FilteredConditionalTokenFilter : ConditionalTokenFilter
//        /// {
//        ///     private readonly ICharTermAttribute termAtt;
//        ///     private readonly Predicate&lt;ICharSequence&gt; predicate;
//        ///     
//        ///     public FilteredConditionalTokenFilter(TokenStream input, Func&lt;TokenStream, TokenStream&gt; inner, Predicate&lt;ICharSequence&gt; predicate)
//        ///         : base(input, inner)
//        ///     {
//        ///         this.predicate = predicate;
//        ///         termAtt = AddAttribute&lt;ICharTermAttribute&gt;();
//        ///     }
//        ///     
//        ///     protected override bool ShouldFilter() => predicate.Invoke(termAtt);
//        /// }
//        /// 
//        /// class FilteredConditionalTokenFilterFactory : ConditionalTokenFilterFactory
//        /// {
//        ///     private readonly Predicate&lt;ICharSequence&gt; predicate;
//        ///     
//        ///     public FilteredConditionalTokenFilterFactory(Predicate&lt;ICharSequence&lt; predicate)
//        ///         : base(new Dictionary&lt;string, string&gt;())
//        ///     {
//        ///         this.predicate = predicate;
//        ///     }
//        ///     
//        ///     protected override ConditionalTokenFilter Create(TokenStream input, Func&lt;TokenStream, TokenStream&gt; inner)
//        ///     {
//        ///         return new FilteredConditionalTokenFilter(input, inner, predicate);
//        ///     }
//        /// }
//        /// </code>
//        /// </summary>
//        private class FilteredConditionalTokenFilterFactory : ConditionalTokenFilterFactory
//        {
//            private readonly Predicate<ICharSequence> predicate;
//            public FilteredConditionalTokenFilterFactory(Predicate<ICharSequence> predicate)
//                : base(Collections.EmptyMap<string, string>())
//            {
//                this.predicate = predicate;
//            }

//            protected override ConditionalTokenFilter Create(TokenStream input, Func<TokenStream, TokenStream> inner)
//            {
//                return new FilteredConditionalTokenFilter(input, inner, predicate);
//            }

//            private class FilteredConditionalTokenFilter : ConditionalTokenFilter
//            {
//                private readonly ICharTermAttribute termAtt;
//                private readonly Predicate<ICharSequence> predicate;
//                public FilteredConditionalTokenFilter(TokenStream input, Func<TokenStream, TokenStream> inner, Predicate<ICharSequence> predicate)
//                    : base(input, inner)
//                {
//                    this.predicate = predicate;
//                    termAtt = AddAttribute<ICharTermAttribute>();
//                }

//                protected override bool ShouldFilter() => predicate.Invoke(termAtt);
//            }
//        }

//        public CustomAnalyzerConditionBuilder WhenTerm(Predicate<ICharSequence> predicate)
//        {
//            return new CustomAnalyzerConditionBuilder(new FilteredConditionalTokenFilterFactory(predicate), this);

//            //    return new ConditionBuilder(new ConditionalTokenFilterFactory(Collections.EmptyMap()) {
//            //        @Override
//            //        protected ConditionalTokenFilter create(TokenStream input, Function<TokenStream, TokenStream> inner)
//            //    {
//            //        return new ConditionalTokenFilter(input, inner) {
//            //            CharTermAttribute termAtt = addAttribute(CharTermAttribute.class);
//            //            @Override
//            //            protected bool shouldFilter()
//            //{
//            //    return predicate.test(termAtt);
//            //}
//            //          };
//            //        }
//            //      }, this);
//        }

//        /// <summary>Builds the analyzer.</summary>
//        public CustomAnalyzer Build()
//        {
//            if (tokenizer.Get() == null)
//            {
//                throw new InvalidOperationException("You have to set at least a tokenizer.");
//            }
//            return new CustomAnalyzer(
//              defaultMatchVersion,
//              charFilters.ToArray(),
//              tokenizer.Get(),
//              tokenFilters.ToArray(),
//              posIncGap,
//              offsetGap
//            );
//        }

//        internal IDictionary<string, string> ApplyDefaultParams(IDictionary<string, string> map)
//        {
//            if (/*defaultMatchVersion.get() != null &&*/ !map.ContainsKey(AbstractAnalysisFactory.LUCENE_MATCH_VERSION_PARAM))
//            {
//                map[AbstractAnalysisFactory.LUCENE_MATCH_VERSION_PARAM] = defaultMatchVersion.ToString();
//            }
//            return map;
//        }

//        internal IDictionary<string, string> ParamsToMap(params string[] parameters)
//        {
//            if (parameters.Length % 2 != 0)
//            {
//                throw new ArgumentException("Key-value pairs expected, so the number of params must be even.");
//            }
//            IDictionary<string, string> map = new HashMap<string, string>();
//            for (int i = 0; i < parameters.Length; i += 2)
//            {
//                if (parameters[i] == null)
//                    throw new ArgumentNullException("", "Key of param may not be null.");
//                //Objects.requireNonNull(parameters[i], "Key of param may not be null.");
//                map[parameters[i]] = parameters[i + 1];
//            }
//            return map;
//        }

//        internal T ApplyResourceLoader<T>(T factory)
//        {
//            if (factory is IResourceLoaderAware)
//            {
//                ((IResourceLoaderAware)factory).Inform(loader);
//            }
//            return factory;
//        }
//    }

//    /// <summary>
//    /// Factory class for a <see cref="ConditionalTokenFilter"/>
//    /// </summary>
//    public class CustomAnalyzerConditionBuilder
//    {

//        private readonly IList<TokenFilterFactory> innerFilters = new List<TokenFilterFactory>();
//        private readonly ConditionalTokenFilterFactory factory;
//        private readonly CustomAnalyzerBuilder parent;

//        internal CustomAnalyzerConditionBuilder(ConditionalTokenFilterFactory factory, CustomAnalyzerBuilder parent)
//        {
//            this.factory = factory;
//            this.parent = parent;
//        }

//        /// <summary>
//        /// Adds the given token filter.
//        /// </summary>
//        /// <param name="name">Used to look up the factory with <see cref="TokenFilterFactory.ForName(string, IDictionary{string, string})"/>.
//        /// The list of possible names can be looked up with <see cref="TokenFilterFactory.AvailableTokenFilters"/>.</param>
//        /// <param name="parameters">The dictionary of parameters to be passed to factory. The dictionary must be modifiable.</param>
//        public CustomAnalyzerConditionBuilder AddTokenFilter(string name, IDictionary<string, string> parameters)
//        {
//            innerFilters.Add(TokenFilterFactory.ForName(name, parent.ApplyDefaultParams(parameters)));
//            return this;
//        }

//        /// <summary>
//        /// Adds the given token filter.
//        /// </summary>
//        /// <param name="name">Used to look up the factory with <see cref="TokenFilterFactory.ForName(string, IDictionary{string, string})"/>.
//        /// The list of possible names can be looked up with <see cref="TokenFilterFactory.AvailableTokenFilters"/>.</param>
//        /// <param name="parameters">The map of parameters to be passed to factory. The map must be modifiable.</param>
//        public CustomAnalyzerConditionBuilder AddTokenFilter(string name, params string[] parameters)
//        {
//            return AddTokenFilter(name, parent.ParamsToMap(parameters));
//        }

//        /// <summary>
//        /// Adds the given token filter.
//        /// </summary>
//        /// <typeparam name="T">The type of <see cref="TokenFilterFactory"/>.</typeparam>
//        /// <param name="parameters">The dictionary of parameters to be passed to factory. The dictionary must be modifiable.</param>
//        /// <returns></returns>
//        public CustomAnalyzerConditionBuilder AddTokenFilter<T>(IDictionary<string, string> parameters) where T : TokenFilterFactory
//        {
//            innerFilters.Add(AnalysisSPILoader<T>.NewFactoryClassInstance<T>(parent.ApplyDefaultParams(parameters)));
//            return this;
//        }

//        /// <summary>
//        /// Adds the given token filter.
//        /// </summary>
//        /// <param name="factory">Type that is used to create the token filter.</param>
//        /// <param name="parameters">The dictionary of parameters to be passed to factory. The dictionary must be modifiable.</param>
//        /// <returns></returns>
//        public CustomAnalyzerConditionBuilder AddTokenFilter(Type factory, IDictionary<string, string> parameters) // LUCENENET overload to pass runtime type
//        {
//            innerFilters.Add((TokenFilterFactory)AnalysisSPILoader<TokenFilterFactory>.NewFactoryClassInstance(factory, parent.ApplyDefaultParams(parameters)));
//            return this;
//        }

//        /// <summary>
//        /// Adds the given token filter.
//        /// </summary>
//        /// <typeparam name="T">The type of <see cref="TokenFilterFactory"/>.</typeparam>
//        /// <param name="parameters">The dictionary of parameters to be passed to factory. The dictionary must be modifiable.</param>
//        public CustomAnalyzerConditionBuilder AddTokenFilter<T>(params string[] parameters) where T : TokenFilterFactory
//        {
//            return AddTokenFilter<T>(parent.ParamsToMap(parameters));
//        }

//        /// <summary>
//        /// Adds the given token filter.
//        /// </summary>
//        /// <param name="factory">Type that is used to create the token filter.</param>
//        /// <param name="parameters">The dictionary of parameters to be passed to factory. The dictionary must be modifiable.</param>
//        public CustomAnalyzerConditionBuilder AddTokenFilter(Type factory, params string[] parameters) // LUCENENET overload to pass runtime type
//        {
//            return AddTokenFilter(factory, parent.ParamsToMap(parameters));
//        }

//        /// <summary>
//        /// Close the branch and return to the main analysis chain
//        /// </summary>
//        public CustomAnalyzerBuilder EndWhen()
//        {
//            factory.InnerFilters = innerFilters;
//            parent.ApplyResourceLoader(factory);
//            parent.AddTokenFilter(factory.GetType());
//            return parent;
//        }
//    }
//}

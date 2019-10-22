// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Common.Analysis.Miscellaneous
{
    /// <summary>
    /// Abstract parent class for analysis factories that create <see cref="ConditionalTokenFilter"/> instances
    /// </summary>
    /// <since>7.4.0</since>
    public abstract class ConditionalTokenFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        /// <summary>
        /// Gets or sets the inner filter factories to produce the <see cref="TokenFilter"/>s that will be
        /// wrapped by the <see cref="ConditionalTokenFilter"/>.
        /// </summary>
        public IList<TokenFilterFactory> InnerFilters { get; set; }

        protected ConditionalTokenFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
        }

        ///**
        // * Set the inner filter factories to produce the {@link TokenFilter}s that will be
        // * wrapped by the {@link ConditionalTokenFilter}
        // */
        //public void setInnerFilters(List<TokenFilterFactory> innerFilters)
        //{
        //    this.InnerFilters = innerFilters;
        //}

        public override TokenStream Create(TokenStream input)
        {
            if (InnerFilters == null || InnerFilters.Count == 0)
            {
                return input;
            }
            Func<TokenStream, TokenStream> innerStream = ts => {
                foreach (TokenFilterFactory factory in InnerFilters)
                {
                    ts = factory.Create(ts);
                }
                return ts;
            };
            return Create(input, innerStream);
        }

        public void Inform(IResourceLoader loader)
        {
            if (InnerFilters == null)
                return;
            foreach (TokenFilterFactory factory in InnerFilters)
            {
                if (factory is IResourceLoaderAware)
                {
                    ((IResourceLoaderAware)factory).Inform(loader);
                }
            }
            DoInform(loader);
        }

        /// <summary>
        /// Initializes this component with the corresponding <see cref="IResourceLoader"/>
        /// </summary>
        protected void DoInform(IResourceLoader loader) { }

        /// <summary>
        /// Modify the incoming <see cref="TokenStream"/> with a <see cref="ConditionalTokenFilter"/>
        /// </summary>
        protected abstract ConditionalTokenFilter Create(TokenStream input, Func<TokenStream, TokenStream> inner);
    }
}

using Lucene.Net.Support;
using org.apache.uima.analysis_engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Analysis.Uima.Ae
{
    /// <summary>
    /// Singleton factory class responsible of <see cref="IAEProvider"/>s' creation
    /// </summary>
    public class AEProviderFactory
    {
        private static readonly AEProviderFactory instance = new AEProviderFactory();

        private readonly IDictionary<string, IAEProvider> providerCache = new Dictionary<string, IAEProvider>();

        private AEProviderFactory()
        {
            // Singleton
        }

        public static AEProviderFactory Instance
        {
            get { return instance; }
        }

        /**
         * @param keyPrefix         a prefix of the key used to cache the AEProvider
         * @param aePath            the AnalysisEngine descriptor path
         * @param runtimeParameters map of runtime parameters to configure inside the AnalysisEngine
         * @return AEProvider
         */
        public virtual IAEProvider GetAEProvider(string keyPrefix, string aePath, IDictionary<string, object> runtimeParameters)
        {
            lock (this)
            {
                string key = new StringBuilder(keyPrefix != null ? keyPrefix : "").Append(aePath).Append(runtimeParameters != null ?
                    Collections.ToString(runtimeParameters) : "").ToString();
                IAEProvider aeProvider;
                if (!providerCache.TryGetValue(key, out aeProvider) || aeProvider == null)
                {
                    if (runtimeParameters != null)
                        aeProvider = new OverridingParamsAEProvider(aePath, runtimeParameters);
                    else
                        aeProvider = new BasicAEProvider(aePath);
                    providerCache[key] = aeProvider;
                }
                return aeProvider;
            }
        }

        /**
         * @param aePath the AnalysisEngine descriptor path
         * @return AEProvider
         */
        public virtual IAEProvider GetAEProvider(string aePath)
        {
            lock (this)
            {
                return GetAEProvider(null, aePath, null);
            }
        }

        /**
         * @param aePath            the AnalysisEngine descriptor path
         * @param runtimeParameters map of runtime parameters to configure inside the AnalysisEngine
         * @return AEProvider
         */
        public virtual IAEProvider GetAEProvider(string aePath, IDictionary<string, object> runtimeParameters)
        {
            lock (this)
            {
                return GetAEProvider(null, aePath, runtimeParameters);
            }
        }
    }
}

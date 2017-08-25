using org.apache.uima.analysis_engine;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lucene.Net.Analysis.Uima.Ae
{
    /// <summary>
    /// <see cref="IAEProvider"/> implementation that creates an Aggregate AE from the given path, also
    /// injecting runtime parameters defined in the solrconfig.xml Solr configuration file and assigning
    /// them as overriding parameters in the aggregate AE.
    /// </summary>
    public class OverridingParamsAEProvider : BasicAEProvider
    {
        private readonly IDictionary<string, object> runtimeParameters;

        public OverridingParamsAEProvider(string aePath, IDictionary<string, object> runtimeParameters)
            : base(aePath)
        {
            this.runtimeParameters = runtimeParameters;
        }

        protected override void ConfigureDescription(AnalysisEngineDescription description)
        {
            foreach (string attributeName in runtimeParameters.Keys)
            {
                object val = GetRuntimeValue(description, attributeName);
                description.getAnalysisEngineMetaData().getConfigurationParameterSettings().setParameterValue(
                    attributeName, val);
            }
        }

        /* create the value to inject in the runtime parameter depending on its declared type */
        private object GetRuntimeValue(AnalysisEngineDescription desc, string attributeName)
        {
            string type = desc.getAnalysisEngineMetaData().getConfigurationParameterDeclarations().
                getConfigurationParameter(null, attributeName).getType();
            // TODO : do it via reflection ? i.e. Class paramType = Class.forName(type)...
            object val = null;
            object runtimeValue;
            if (runtimeParameters.TryGetValue(attributeName, out runtimeValue) && runtimeValue != null)
            {
                if ("String".Equals(type))
                {
                    val = Convert.ToString(runtimeValue);
                }
                else if ("Integer".Equals(type))
                {
                    val = Convert.ToInt32(runtimeValue.ToString(), CultureInfo.InvariantCulture);
                }
                else if ("Boolean".Equals(type))
                {
                    val = Convert.ToBoolean(runtimeValue.ToString(), CultureInfo.InvariantCulture);
                }
                else if ("Float".Equals(type))
                {
                    val = Convert.ToSingle(runtimeValue.ToString(), CultureInfo.InvariantCulture);
                }
            }

            return val;
        }
    }
}

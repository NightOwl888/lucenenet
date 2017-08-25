using org.apache.uima.analysis_component;
using org.apache.uima.cas;
using org.apache.uima.jcas;
using org.apache.uima.jcas.tcas;
using System.Globalization;

namespace Lucene.Net.Analysis.Uima.An
{
    /// <summary>
    /// Dummy implementation of a PoS tagger to add part of speech as token types
    /// </summary>
    public class SamplePoSTagger : JCasAnnotator_ImplBase
    {
        private static readonly string NUM = "NUM";
        private static readonly string WORD = "WORD";
        private static readonly string TYPE_NAME = "org.apache.lucene.uima.ts.TokenAnnotation";
        private static readonly string FEATURE_NAME = "pos";

        public override void process(JCas jcas) //throws AnalysisEngineProcessException
        {
            org.apache.uima.cas.Type type = jcas.getCas().getTypeSystem().getType(TYPE_NAME);
            Feature posFeature = type.getFeatureByBaseName(FEATURE_NAME);

            var iter = jcas.getAnnotationIndex(type).iterator();
            while (iter.hasNext())
            {
                Annotation annotation = (Annotation)iter.next();
                string text = annotation.getCoveredText();
                string pos = ExtractPoS(text);
                annotation.setStringValue(posFeature, pos);
            }
        }

        private string ExtractPoS(string text)
        {
            double result;
            if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result))
            {
                return NUM;
            }
            return WORD;
        }
    }
}

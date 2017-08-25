using org.apache.uima;
using org.apache.uima.analysis_component;
using org.apache.uima.cas;
using org.apache.uima.cas.text;
using org.apache.uima.jcas;
using org.apache.uima.jcas.tcas;

namespace Lucene.Net.Analysis.Uima.An
{
    /// <summary>
    /// Dummy implementation of an entity annotator to tag tokens as certain types of entities
    /// </summary>
    public class SampleEntityAnnotator : JCasAnnotator_ImplBase
    {
        private static readonly string NP = "np";
        private static readonly string NPS = "nps";
        private static readonly string TYPE_NAME = "org.apache.lucene.analysis.uima.ts.EntityAnnotation";
        private static readonly string ENTITY_FEATURE = "entity";
        private static readonly string NAME_FEATURE = "entity";

        public override void process(JCas jcas) // throws AnalysisEngineProcessException
        {
            org.apache.uima.cas.Type type = jcas.getCas().getTypeSystem().getType(TYPE_NAME);
            Feature entityFeature = type.getFeatureByBaseName(ENTITY_FEATURE);
            Feature nameFeature = type.getFeatureByBaseName(NAME_FEATURE);

            var iter = jcas.getAnnotationIndex(TokenAnnotation.type).iterator();

            while (iter.hasNext())
            {
                Annotation annotation = (Annotation)iter.next();

                string tokenPOS = ((TokenAnnotation)annotation).getPosTag();

                if (NP.equals(tokenPOS) || NPS.equals(tokenPOS))
                {
                    AnnotationFS entityAnnotation = jcas.getCas().createAnnotation(type, annotation.getBegin(), annotation.getEnd());

                    entityAnnotation.setStringValue(entityFeature, annotation.getCoveredText());

                    string name = "OTHER"; // "OTHER" makes no sense. In practice, "PERSON", "COUNTRY", "E-MAIL", etc.
                    if (annotation.getCoveredText().equals("Apache"))
                        name = "ORGANIZATION";
                    entityAnnotation.setStringValue(nameFeature, name);

                    jcas.addFsToIndexes(entityAnnotation);
                }
            }
        }
    }
}

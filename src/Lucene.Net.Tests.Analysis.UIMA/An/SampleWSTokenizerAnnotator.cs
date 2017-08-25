using org.apache.uima;
using org.apache.uima.analysis_component;
using org.apache.uima.cas.text;
using org.apache.uima.jcas;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Type = org.apache.uima.cas.Type;

namespace Lucene.Net.Analysis.Uima.An
{
    /// <summary>
    /// Dummy implementation of a UIMA based whitespace tokenizer.
    /// </summary>
    public class SampleWSTokenizerAnnotator : JCasAnnotator_ImplBase
    {
        private readonly static string TOKEN_TYPE = "org.apache.lucene.uima.ts.TokenAnnotation";
        private readonly static string SENTENCE_TYPE = "org.apache.lucene.uima.ts.SentenceAnnotation";
        private String lineEnd;
        private static readonly string WHITESPACE = " ";


        public override void initialize(UimaContext aContext) //throws ResourceInitializationException
        {
            base.initialize(aContext);
            lineEnd = Convert.ToString(aContext.getConfigParameterValue("line-end"), CultureInfo.InvariantCulture);
        }


        public override void process(JCas jCas) //throws AnalysisEngineProcessException
        {
            Type sentenceType = jCas.getCas().getTypeSystem().getType(SENTENCE_TYPE);
            Type tokenType = jCas.getCas().getTypeSystem().getType(TOKEN_TYPE);
            int i = 0;
            foreach (string sentenceString in Regex.Split(jCas.getDocumentText(), lineEnd))
            {
                // add the sentence
                AnnotationFS sentenceAnnotation = jCas.getCas().createAnnotation(sentenceType, i, sentenceString.Length);
                jCas.addFsToIndexes(sentenceAnnotation);
                i += sentenceString.Length;
            }

            // get tokens
            int j = 0;
            foreach (string tokenString in Regex.Split(jCas.getDocumentText(), WHITESPACE))
            {
                int tokenLength = tokenString.Length;
                AnnotationFS tokenAnnotation = jCas.getCas().createAnnotation(tokenType, j, j + tokenLength);
                jCas.addFsToIndexes(tokenAnnotation);
                j += tokenLength;
            }
        }
    }
}

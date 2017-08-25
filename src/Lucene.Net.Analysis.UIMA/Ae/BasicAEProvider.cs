using Lucene.Net.Support;
using Lucene.Net.Util;
using org.apache.uima;
using org.apache.uima.analysis_engine;
using org.apache.uima.resource;
using org.apache.uima.util;
using System;
using System.IO;
using System.Security;

namespace Lucene.Net.Analysis.Uima.Ae
{
    /// <summary>
    /// Basic <see cref="IAEProvider"/> which just instantiates a UIMA <see cref="AnalysisEngine"/> with no additional metadata,
    /// parameters or resources
    /// </summary>
    public class BasicAEProvider : IAEProvider
    {
        private readonly string aePath;
        private AnalysisEngineDescription cachedDescription;

        public BasicAEProvider(string aePath)
        {
            this.aePath = System.IO.Path.Combine(DATA_DIR, aePath);
        }


        public virtual AnalysisEngine GetAE() //throws ResourceInitializationException
        {
            lock (this)
            {
                if (cachedDescription == null)
                {
                    XMLInputSource input = null;
                    bool success = false;
                    try
                    {
                        // get Resource Specifier from XML file
                        input = GetInputSource();

                        // get AE description
                        cachedDescription = UIMAFramework.getXMLParser()
                            .parseAnalysisEngineDescription(input);
                        ConfigureDescription(cachedDescription);
                        success = true;
                    }
                    catch (Exception e)
                    {
                        throw new ResourceInitializationException(e);
                    }
                    finally
                    {
                        if (success)
                        {
                            try
                            {
                                IOUtils.Dispose(input.getInputStream());
                            }
                            catch (IOException e)
                            {
                                throw new ResourceInitializationException(e);
                            }
                        }
                        else if (input != null)
                        {
                            IOUtils.DisposeWhileHandlingException(input.getInputStream());
                        }
                    }
                }
            }

            return UIMAFramework.produceAnalysisEngine(cachedDescription);
        }

        protected virtual void ConfigureDescription(AnalysisEngineDescription description)
        {
            // no configuration
        }

        private XMLInputSource GetInputSource() //throws IOException
        {
            try
            {
                return new XMLInputSource(aePath);
            }
            catch (IOException e)
            {
                // LUCENENET TODO: Find a way to load resources from outside of this assembly
                using (var stream = GetType().Assembly.FindAndGetManifestResourceStream(GetType(), System.IO.Path.GetFileName(aePath)))
                {
                    return new XMLInputSource(new ikvm.io.InputStreamWrapper(stream), new java.io.File(aePath));
                }
                //return new XMLInputSource(GetType().getResource(aePath));
            }
        }

        // LUCENENET specific - initialize DATA_DIR
        static BasicAEProvider()
        {
            Init();
        }

        internal static string DATA_DIR = "";

        // LUCENENET specific: We base the directory off of the
        // current path. We start at the current directory and 
        // check to the root directory for a subdirectory named uima.
        private static void Init()
        {
            string dirName = "uima";

            DATA_DIR = SystemProperties.GetProperty("uima.data.dir", "");
            if (DATA_DIR.Length != 0)
                return;

#if NETSTANDARD1_5
            string currentPath = System.AppContext.BaseDirectory;
#else
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
#endif

            // If a matching directory path is found, set our DATA_DIR static
            // variable. If it is null or empty after this process, we need to
            // load the embedded files.
            string candidatePath = System.IO.Path.Combine(currentPath, dirName);
            if (System.IO.Directory.Exists(candidatePath))
            {
                DATA_DIR = candidatePath;
                return;
            }

            while (new DirectoryInfo(currentPath).Parent != null)
            {
                try
                {
                    candidatePath = System.IO.Path.Combine(new DirectoryInfo(currentPath).Parent.FullName, dirName);
                    if (System.IO.Directory.Exists(candidatePath))
                    {
                        DATA_DIR = candidatePath;
                        return;
                    }
                    currentPath = new DirectoryInfo(currentPath).Parent.FullName;
                }
                catch (SecurityException)
                {
                    // ignore security errors
                }
            }
        }
    }
}

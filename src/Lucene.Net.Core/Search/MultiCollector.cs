namespace Lucene.Net.Search
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;

    /// <summary>
    /// A <seealso cref="ICollector"/> which allows running a search with several
    /// <seealso cref="ICollector"/>s. It offers a static <seealso cref="#wrap"/> method which accepts a
    /// list of collectors and wraps them with <seealso cref="MultiCollector"/>, while
    /// filtering out the <code>null</code> null ones.
    /// </summary>
    public class MultiCollector : ICollector
    {
        /// <summary>
        /// Wraps a list of <seealso cref="ICollector"/>s with a <seealso cref="MultiCollector"/>. this
        /// method works as follows:
        /// <ul>
        /// <li>Filters out the <code>null</code> collectors, so they are not used
        /// during search time.
        /// <li>If the input contains 1 real collector (i.e. non-<code>null</code> ),
        /// it is returned.
        /// <li>Otherwise the method returns a <seealso cref="MultiCollector"/> which wraps the
        /// non-<code>null</code> ones.
        /// </ul>
        /// </summary>
        /// <exception cref="IllegalArgumentException">
        ///           if either 0 collectors were input, or all collectors are
        ///           <code>null</code>. </exception>
        public static ICollector Wrap(params ICollector[] collectors)
        {
            // For the user's convenience, we allow null collectors to be passed.
            // However, to improve performance, these null collectors are found
            // and dropped from the array we save for actual collection time.
            int n = 0;
            foreach (ICollector c in collectors)
            {
                if (c != null)
                {
                    n++;
                }
            }

            if (n == 0)
            {
                throw new System.ArgumentException("At least 1 collector must not be null");
            }
            else if (n == 1)
            {
                // only 1 Collector - return it.
                ICollector col = null;
                foreach (ICollector c in collectors)
                {
                    if (c != null)
                    {
                        col = c;
                        break;
                    }
                }
                return col;
            }
            else if (n == collectors.Length)
            {
                return new MultiCollector(collectors);
            }
            else
            {
                ICollector[] colls = new ICollector[n];
                n = 0;
                foreach (ICollector c in collectors)
                {
                    if (c != null)
                    {
                        colls[n++] = c;
                    }
                }
                return new MultiCollector(colls);
            }
        }

        private readonly ICollector[] collectors;

        private MultiCollector(params ICollector[] collectors)
        {
            this.collectors = collectors;
        }

        public virtual bool AcceptsDocsOutOfOrder
        {
            get
            {
                foreach (ICollector c in collectors)
                {
                    if (!c.AcceptsDocsOutOfOrder)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public virtual void Collect(int doc)
        {
            foreach (ICollector c in collectors)
            {
                c.Collect(doc);
            }
        }

        public virtual void SetNextReader(AtomicReaderContext context)
        {
            foreach (ICollector c in collectors)
            {
                c.SetNextReader(context);
            }
        }
        
        public virtual void SetScorer(Scorer scorer)
        {
            foreach (ICollector c in collectors)
            {
                c.SetScorer(scorer);
            }
        }
    }
}
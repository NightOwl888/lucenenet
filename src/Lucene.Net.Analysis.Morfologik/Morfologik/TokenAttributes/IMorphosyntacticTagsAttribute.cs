using Lucene.Net.Util;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Analysis.Morfologik.TokenAttributes
{
    /// <summary>
    /// Morfologik provides morphosyntactic annotations for
    /// surface forms. For the exact format and description of these,
    /// see the project's documentation.
    /// </summary>
    public interface IMorphosyntacticTagsAttribute : IAttribute
    {
        /// <summary>
        /// Gets or sets the POS tag of the term. A single word may have multiple POS tags,
        /// depending on the interpretation (context disambiguation is typically needed
        /// to determine which particular tag is appropriate).
        /// <para/>
        /// The default value (no-value) is null. Returns a list of POS tags corresponding to current lemma.
        /// </summary>
        IList<StringBuilder> Tags { get; set; }

        /// <summary>Clear to default value.</summary>
        void Clear();
    }
}

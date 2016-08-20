using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support.Collation
{
    public interface IAvailableLanguageTags
    {
        /// <summary>
        /// Returns a set of available language tags
        /// </summary>
        IEnumerable<string> AvailableLanguageTags { get; }
    }
}

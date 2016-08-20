using System.Collections.Generic;
using System.Globalization;

namespace Lucene.Net.Support.Collation
{
    /// <summary>
    /// Mimics the Java CollatorProvider abstract class.
    /// </summary>
    public interface ICollatorProvider
    {
        /// <summary>
        /// Returns a new <seealso cref="Collator"/> instance for the specified locale.
        /// </summary>
        /// <param name="locale">the desired locale.</param>
        /// <returns>the <seealso cref="Collator"/> for the desired locale.</returns>
        Collator GetInstance(CultureInfo locale);


        // From LocaleServiceProvider
        IEnumerable<CultureInfo> AvailableLocales { get; }

        bool IsSupportedLocale(CultureInfo locale);
    }
}

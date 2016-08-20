using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support.Collation
{
    /// <summary>
    /// Concrete implementation of the <seealso cref="ICollatorProvider"/>.
    /// </summary>
    public class CollatorProvider : ICollatorProvider, IAvailableLanguageTags
    {
        private readonly IEnumerable<string> langTags;
        private readonly IEnumerable<CultureInfo> availableLocales;

        public CollatorProvider(IEnumerable<string> langTags)
        {
            if (langTags == null)
            {
                throw new ArgumentNullException("langTags");
            }
            this.langTags = langTags;
            ISet<CultureInfo> available = new HashSet<CultureInfo>();
            foreach (var langTag in langTags)
            {
                available.Add(new CultureInfo(langTag));
            }
            this.availableLocales = available;
        }

        public IEnumerable<CultureInfo> AvailableLocales
        {
            get
            {
                return this.availableLocales;
            }
        }

        //internal class AllAvailableLocales
        //{
        //    internal static readonly CultureInfo[] allAvailableLocales;

        //    static AllAvailableLocales()
        //    {
        //        ISet<CultureInfo> all = new HashSet<CultureInfo>(CultureInfo.GetCultures(CultureTypes.AllCultures));


        //        //foreach (var c in CultureInfo.GetCultures(CultureTypes.AllCultures))
        //        //    {
        //        //    LocaleServiceProviderPool pool =
        //        //        LocaleServiceProviderPool.getPool(c);
        //        //all.AddAll(pool.getAvailableLocaleSet());
        //        //}

        //        allAvailableLocales = all.ToArray();
        //    }

        //    // No instantiation
        //    private AllAvailableLocales()
        //    {
        //    }
        //}


        public Collator GetInstance(CultureInfo locale)
        {
            if (locale == null)
            {
                throw new ArgumentNullException("locale");
            }

            Collator result = null;

            // LUCENENET TODO: Finish implementation

            return result;
        }

        public bool IsSupportedLocale(CultureInfo locale)
        {
            return false; // LUCENENET TODO: Implement
        }

        public IEnumerable<string> AvailableLanguageTags
        {
            get
            {
                return langTags;
            }
        }


    }
}

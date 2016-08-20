using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support.Collation
{
    public sealed class NormalizerBase : ICloneable
    {
        //-------------------------------------------------------------------------
        // Private data
        //-------------------------------------------------------------------------
        private char[] buffer = new char[100];
        private int bufferStart = 0;
        private int bufferPos = 0;
        private int bufferLimit = 0;

        // The input text and our position in it
        private UCharacterIterator text;
        private Mode mode = NFC;
        private int options = 0;
        private int currentIndex;
        private int nextIndex;

        /// <summary>
        /// Options bit set value to select Unicode 3.2 normalization
        /// (except NormalizationCorrections).
        /// At most one Unicode version can be selected at a time.
        /// stable ICU 2.6
        /// </summary>
        public static readonly int UNICODE_3_2 = 0x20;

        /// <summary>
        /// Constant indicating that the end of the iteration has been reached.
        /// This is guaranteed to have the same value as {@link UCharacterIterator#DONE}.
        /// @stable ICU 2.8
        /// </summary>
        public static readonly int DONE = UCharacterIterator.DONE;



        public object Clone()
        {
            throw new NotImplementedException();
        }
    }
}

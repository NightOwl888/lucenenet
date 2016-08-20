using System;

namespace Lucene.Net.Support.Collation
{
    public sealed class UCompactIntArray : ICloneable
    {
        /// <summary>
        /// Default constructor for UCompactIntArray, the default value of the
        /// compact array is 0.
        /// </summary>
        public UCompactIntArray()
        {
            values = new int[16][];
            indices = new short[16][];
            blockTouched = new bool[16][];
            planeTouched = new bool[16];
        }

        public UCompactIntArray(int defaultValue)
            : this()
        {
            this.defaultValue = defaultValue;
        }

        /// <summary>
        /// Get the mapped value of a Unicode character.
        /// </summary>
        /// <param name="index">the character to get the mapped value with</param>
        /// <returns>the mapped value of the given character</returns>
        public int ElementAt(int index)
        {
            int plane = (index & PLANEMASK) >> PLANESHIFT;
            if (!planeTouched[plane])
            {
                return defaultValue;
            }
            index &= CODEPOINTMASK;
            return values[plane][(indices[plane][index >> BLOCKSHIFT] & 0xFFFF)
                           + (index & BLOCKMASK)];
        }

        /// <summary>
        /// Set a new value for a Unicode character.
        /// Set automatically expands the array if it is compacted.
        /// </summary>
        /// <param name="index">the character to set the mapped value with</param>
        /// <param name="value">the new mapped value</param>
        public void SetElementAt(int index, int value)
        {
            if (isCompact)
            {
                Expand();
            }
            int plane = (index & PLANEMASK) >> PLANESHIFT;
            if (!planeTouched[plane])
            {
                InitPlane(plane);
            }
            index &= CODEPOINTMASK;
            values[plane][index] = value;
            blockTouched[plane][index >> BLOCKSHIFT] = true;
        }

        /// <summary>
        /// Compact the array.
        /// </summary>
        public void Compact()
        {
            if (isCompact)
            {
                return;
            }
            for (int plane = 0; plane < PLANECOUNT; plane++)
            {
                if (!planeTouched[plane])
                {
                    continue;
                }
                int limitCompacted = 0;
                int iBlockStart = 0;
                short iUntouched = -1;

                for (int i = 0; i < indices[plane].Length; ++i, iBlockStart += BLOCKCOUNT)
                {
                    indices[plane][i] = -1;
                    if (!blockTouched[plane][i] && iUntouched != -1)
                    {
                        // If no values in this block were set, we can just set its
                        // index to be the same as some other block with no values
                        // set, assuming we've seen one yet.
                        indices[plane][i] = iUntouched;
                    }
                    else
                    {
                        int jBlockStart = limitCompacted * BLOCKCOUNT;
                        if (i > limitCompacted)
                        {
                            System.Array.Copy(values[plane], iBlockStart,
                                             values[plane], jBlockStart, BLOCKCOUNT);
                        }
                        if (!blockTouched[plane][i])
                        {
                            // If this is the first untouched block we've seen, remember it.
                            iUntouched = (short)jBlockStart;
                        }
                        indices[plane][i] = (short)jBlockStart;
                        limitCompacted++;
                    }
                }

                // we are done compacting, so now make the array shorter
                int newSize = limitCompacted * BLOCKCOUNT;
                int[] result = new int[newSize];
                System.Array.Copy(values[plane], 0, result, 0, newSize);
                values[plane] = result;
                blockTouched[plane] = null;
            }
            isCompact = true;
        }

        // --------------------------------------------------------------
        // private
        // --------------------------------------------------------------

        private void Expand()
        {
            int i;
            if (isCompact)
            {
                int[] tempArray;
                for (int plane = 0; plane < PLANECOUNT; plane++)
                {
                    if (!planeTouched[plane])
                    {
                        continue;
                    }
                    blockTouched[plane] = new bool[INDEXCOUNT];
                    tempArray = new int[UNICODECOUNT];
                    for (i = 0; i < UNICODECOUNT; ++i)
                    {
                        tempArray[i] = values[plane][indices[plane][i >> BLOCKSHIFT]
                                                    & 0xffff + (i & BLOCKMASK)];
                        blockTouched[plane][i >> BLOCKSHIFT] = true;
                    }
                    for (i = 0; i < INDEXCOUNT; ++i)
                    {
                        indices[plane][i] = (short)(i << BLOCKSHIFT);
                    }
                    values[plane] = tempArray;
                }
                isCompact = false;
            }
        }

        private void InitPlane(int plane)
        {
            values[plane] = new int[UNICODECOUNT];
            indices[plane] = new short[INDEXCOUNT];
            blockTouched[plane] = new bool[INDEXCOUNT];
            planeTouched[plane] = true;

            if (planeTouched[0] && plane != 0)
            {
                System.Array.Copy(indices[0], 0, indices[plane], 0, INDEXCOUNT);
            }
            else
            {
                for (int i = 0; i < INDEXCOUNT; ++i)
                {
                    indices[plane][i] = (short)(i << BLOCKSHIFT);
                }
            }
            for (int i = 0; i < UNICODECOUNT; ++i)
            {
                values[plane][i] = defaultValue;
            }
        }

        public int GetKSize()
        {
            int size = 0;
            for (int plane = 0; plane < PLANECOUNT; plane++)
            {
                if (planeTouched[plane])
                {
                    size += (values[plane].Length * 4 + indices[plane].Length * 2);
                }
            }
            return size / 1024;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        private const int PLANEMASK = 0x30000;
        private const int PLANESHIFT = 16;
        private const int PLANECOUNT = 0x10;
        private const int CODEPOINTMASK = 0xffff;

        private const int UNICODECOUNT = 0x10000;
        private const int BLOCKSHIFT = 7;
        private const int BLOCKCOUNT = (1 << BLOCKSHIFT);
        private const int INDEXSHIFT = (16 - BLOCKSHIFT);
        private const int INDEXCOUNT = (1 << INDEXSHIFT);
        private const int BLOCKMASK = BLOCKCOUNT - 1;

        private int defaultValue;
        private int[][] values;
        private short[][] indices;
        private bool isCompact;
        private bool[][] blockTouched;
        private bool[] planeTouched;
    }
}

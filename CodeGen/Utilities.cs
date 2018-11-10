using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGen
{
    internal static class Utilities
    {
        public static uint NextPowerOfTwo(uint v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;

            return v;
        }

        public static uint CountBitsSet(uint v)
        {
            v = v - ((v >> 1) & 0x55555555);
            v = (v & 0x33333333) + ((v >> 2) & 0x33333333);
            return (((v + (v >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        public static int GetMinimumBitsForInt(int n)
        {
            if (n <= 2)
            {
                return 1;
            }

            var nextPowerOfTwo = NextPowerOfTwo((uint)(n - 2));
            return (int)CountBitsSet(nextPowerOfTwo - 1);
        }

        public static int AddWithAlignment(int blockSize, int itemSize, int itemAlignment)
        {
            if (blockSize == 0)
            {
                return itemSize;
            }

            var padding = itemAlignment - (blockSize % itemAlignment);
            return blockSize + padding + itemSize;
        }
    }
}

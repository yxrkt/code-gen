using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeGen
{
    public static class EnumerableExtensions
    {
        public static (T[] items, int size)[] BinPack<T>(
            this IEnumerable<T> self,
            int binSize,
            Func<T, int> sizeSelector)
        {
            var bins = new List<(List<T> items, int size)>();
            foreach (var item in self.OrderByDescending(i => sizeSelector(i)))
            {
                var itemSize = sizeSelector(item);
                var binIndex = bins.FindIndex(b => b.size + itemSize <= binSize);
                if (binIndex != -1)
                {
                    var bin = bins[binIndex];
                    bin.items.Add(item);
                    bin.size += itemSize;
                }
                else
                {
                    binIndex = bins.Count;
                    bins.Add((new List<T> { item }, itemSize));
                }
            }

            return bins.Select(bin => (bin.items.ToArray(), bin.size)).ToArray();
        }
    }
}

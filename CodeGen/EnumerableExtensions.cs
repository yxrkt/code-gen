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
            return self.BinPack(binSize, sizeSelector, (bin, item) => bin + sizeSelector(item));
        }

        public static (T[] items, int size)[] BinPack<T>(
            this IEnumerable<T> self,
            int binSize,
            Func<T, int> sizeSelector,
            Func<int, T, int> sumSelector)
        {
            var bins = new List<(List<T> items, int size)>();

            foreach (var item in self.OrderByDescending(item => sizeSelector(item)))
            {
                var availableBinsQuery =
                    from bin in bins
                    let binSizeWithItem = sumSelector(bin.size, item)
                    where binSizeWithItem <= binSize
                    select (bin: bin, newSize: binSizeWithItem);

                (var availableBin, int newSize) = availableBinsQuery.FirstOrDefault();
                if (availableBin.items != null)
                {
                    availableBin.items.Add(item);
                    availableBin.size = newSize;
                }
                else
                {
                    bins.Add((items: new List<T> { item }, size: sumSelector(0, item)));
                }
            }

            return bins.Select(bin => (bin.items.ToArray(), bin.size)).ToArray();
        }
    }
}

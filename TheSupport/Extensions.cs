using System;
using System.Collections.Generic;
using System.Linq;

namespace TheSupport
{
    public static class Extensions
    {
        // Credits: Jon Skeet and Thijs http://stackoverflow.com/a/489421
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var seenKeys = new HashSet<TKey>();
            return source.Where(element => seenKeys.Add(keySelector(element)));
        }
    }
}

using System.Collections.Generic;
using System.Linq;

namespace SkinChanger
{
    public static class Extensions
    {
        public static IEnumerable<T> Unique<T>(this IEnumerable<T> list)
        {
            var uniqueList = new List<T>();

            foreach (var entry in list.Where(entry => uniqueList.All(e => !e.Equals(entry))))
            {
                uniqueList.Add(entry);
            }

            return uniqueList;
        }
    }
}

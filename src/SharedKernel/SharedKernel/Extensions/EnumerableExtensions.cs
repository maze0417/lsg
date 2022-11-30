using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LSG.SharedKernel.Extensions
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            using var en = enumerable.GetEnumerator();
            while (en.MoveNext())
            {
                action(en.Current);
            }
        }

        public static async Task ForEachAsync<T>(this IEnumerable<T> enumerable, Func<T, Task> functionAsync)
        {
            using var en = enumerable.GetEnumerator();
            while (en.MoveNext())
            {
                await functionAsync(en.Current);
            }
        }

        public static void EachWithIndex<T>(this IEnumerable<T> enumerable, Action<T, int> handler)
        {
            var idx = 0;
            foreach (var item in enumerable)
                handler(item, idx++);
        }
    }
}
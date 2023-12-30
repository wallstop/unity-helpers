namespace Core.Extension
{
    using System.Collections.Generic;

    public static class HashSetExtensions
    {
        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> elements)
        {
            set.UnionWith(elements);
        }
    }
}

namespace UnityHelpers.Core.Helper
{
    using System.Collections.Generic;

    public static class Enumerables
    {
        public static IEnumerable<T> Of<T>(T element)
        {
            return new[] { element };
        }

        public static IEnumerable<T> Of<T>(params T[] elements)
        {
            return elements;
        }
    }
}

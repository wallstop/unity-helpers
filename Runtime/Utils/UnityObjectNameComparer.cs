namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;

    public sealed class UnityObjectNameComparer<T> : IComparer<T>
        where T : UnityEngine.Object
    {
        public static readonly UnityObjectNameComparer<T> Instance = new();

        private UnityObjectNameComparer() { }

        public int Compare(T x, T y)
        {
            if (x == y)
            {
                return 0;
            }

            if (y == null)
            {
                return 1;
            }

            if (x == null)
            {
                return -1;
            }

            return string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase);
        }
    }
}

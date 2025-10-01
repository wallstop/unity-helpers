namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Collections.Generic;

    public sealed class FuncBasedComparer<T> : IComparer<T>
    {
        private readonly Func<T, T, int> _comparer;

        public FuncBasedComparer(Func<T, T, int> comparer)
        {
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        public int Compare(T x, T y)
        {
            return _comparer(x, y);
        }
    }
}

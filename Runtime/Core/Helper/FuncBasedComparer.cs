namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A comparer powered by a user-provided comparison function.
    /// </summary>
    public sealed class FuncBasedComparer<T> : IComparer<T>
    {
        private readonly Func<T, T, int> _comparer;

        /// <summary>
        /// Creates a comparer from a comparison delegate.
        /// </summary>
        public FuncBasedComparer(Func<T, T, int> comparer)
        {
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        /// <summary>
        /// Compares two values using the provided delegate.
        /// </summary>
        public int Compare(T x, T y)
        {
            return _comparer(x, y);
        }
    }
}

// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System.Collections.Generic;

    /// <summary>
    /// Wraps an existing comparer and reverses its ordering (descending if original was ascending).
    /// </summary>
    public sealed class ReverseComparer<TElement> : IComparer<TElement>
    {
        /// <summary>
        /// A default instance that uses <see cref="Comparer{T}.Default"/>.
        /// </summary>
        public static readonly ReverseComparer<TElement> Instance = new();

        private readonly IComparer<TElement> _baseComparer;

        /// <summary>
        /// Creates a reversed comparer from a base comparer.
        /// </summary>
        public ReverseComparer(IComparer<TElement> baseComparer = null)
        {
            _baseComparer = baseComparer ?? Comparer<TElement>.Default;
        }

        /// <summary>
        /// Compares two values with reversed ordering.
        /// </summary>
        public int Compare(TElement x, TElement y)
        {
            return _baseComparer.Compare(y, x);
        }
    }
}

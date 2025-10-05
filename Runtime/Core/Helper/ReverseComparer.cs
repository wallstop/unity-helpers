namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System.Collections.Generic;

    public sealed class ReverseComparer<TElement> : IComparer<TElement>
    {
        public static readonly ReverseComparer<TElement> Instance = new();

        private readonly IComparer<TElement> _baseComparer;

        public ReverseComparer(IComparer<TElement> baseComparer = null)
        {
            _baseComparer = baseComparer ?? Comparer<TElement>.Default;
        }

        public int Compare(TElement x, TElement y)
        {
            return _baseComparer.Compare(y, x);
        }
    }
}

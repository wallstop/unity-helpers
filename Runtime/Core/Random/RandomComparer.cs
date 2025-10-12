namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System.Collections.Generic;

    public sealed class RandomComparer<T> : IComparer<T>
    {
        private readonly IRandom _random;

        private readonly Dictionary<T, int> _cachedValues;

        public RandomComparer(IRandom random)
        {
            _random = random;
            _cachedValues = new Dictionary<T, int>();
        }

        public int Compare(T x, T y)
        {
            if (!_cachedValues.TryGetValue(x, out int xValue))
            {
                xValue = _random.Next();
                _cachedValues[x] = xValue;
            }

            if (!_cachedValues.TryGetValue(y, out int yValue))
            {
                yValue = _random.Next();
                _cachedValues[y] = yValue;
            }

            return xValue.CompareTo(yValue);
        }
    }
}

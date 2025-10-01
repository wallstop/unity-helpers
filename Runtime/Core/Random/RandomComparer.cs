namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System.Collections.Generic;

    public readonly struct RandomComparer<T> : IComparer<T>
    {
        private readonly IRandom _random;

        public RandomComparer(IRandom random)
        {
            _random = random;
        }

        public int Compare(T x, T y)
        {
            return _random.Next();
        }
    }
}

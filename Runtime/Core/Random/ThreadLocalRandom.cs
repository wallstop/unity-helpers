namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System.Threading;

    public static class ThreadLocalRandom<T>
        where T : IRandom, new()
    {
#if SINGLE_THREADED
        public static readonly T Instance = new T();
#else
        private static readonly ThreadLocal<T> RandomCache = new(() => new T());
        public static T Instance => RandomCache.Value;
#endif
    }
}

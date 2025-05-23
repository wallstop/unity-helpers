﻿namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System.Threading;

    public static class ThreadLocalRandom<T>
        where T : IRandom, new()
    {
        private static readonly ThreadLocal<T> RandomCache = new(() => new T());

        public static T Instance => RandomCache.Value;
    }
}

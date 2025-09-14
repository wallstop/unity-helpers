namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using UnityEngine;

    public static class Buffers
    {
        public const int BufferSize = 10_000;

        public static readonly Collider2D[] Colliders = new Collider2D[BufferSize];
        public static readonly RaycastHit2D[] RaycastHits = new RaycastHit2D[BufferSize];

        /*
            Note: Only use with CONSTANT time values, otherwise this is a memory leak.
            DO NOT USE with random values.
         */
        public static readonly Dictionary<float, WaitForSeconds> WaitForSeconds = new();
        public static readonly Dictionary<float, WaitForSecondsRealtime> WaitForSecondsRealtime =
            new();
        public static readonly System.Random Random = new();
        public static readonly WaitForFixedUpdate WaitForFixedUpdate = new();
        public static readonly WaitForEndOfFrame WaitForEndOfFrame = new();

        public static readonly StringBuilder StringBuilder = new();
    }

    public static class Buffers<T>
    {
        public static readonly List<T> List = new();
        public static readonly HashSet<T> HashSet = new();
        public static readonly Queue<T> Queue = new();
        public static readonly Stack<T> Stack = new();
    }

    public static class WallstopGenericPool<T>
        where T : new()
    {
        private static readonly List<T> _pool = new();
        private static readonly Action<T> _clearAction = GetClearAction();
        private static readonly Action<T> _onDispose = Release;

        public static PooledResource<T> Get()
        {
            if (_pool.Count == 0)
            {
                return new PooledResource<T>(new T(), _onDispose);
            }

            int lastIndex = _pool.Count - 1;
            T instance = _pool[lastIndex];
            _pool.RemoveAt(lastIndex);
            return new PooledResource<T>(instance, _onDispose);
        }

        private static Action<T> GetClearAction()
        {
            Type type = typeof(T);
            foreach (
                MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            )
            {
                if (
                    string.Equals(method.Name, "Clear", StringComparison.Ordinal)
                    && method.GetParameters().Length == 0
                )
                {
                    return (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), method);
                }
            }

            return _ => { };
        }

        private static void Release(T resource)
        {
            _clearAction.Invoke(resource);
            _pool.Add(resource);
        }
    }

    public static class WallstopArrayPool<T>
    {
        private static readonly Dictionary<int, List<T[]>> _pool = new();
        private static readonly Action<T[]> _onDispose = Release;

        public static PooledResource<T[]> Get(int size)
        {
            switch (size)
            {
                case < 0:
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(size),
                        size,
                        "Must be non-negative."
                    );
                }
                case 0:
                {
                    return new PooledResource<T[]>(Array.Empty<T>(), _ => { });
                }
            }

            if (!_pool.TryGetValue(size, out List<T[]> pool))
            {
                pool = new List<T[]>();
                _pool[size] = pool;
            }

            if (pool.Count == 0)
            {
                return new PooledResource<T[]>(new T[size], _onDispose);
            }

            int lastIndex = pool.Count - 1;
            T[] instance = pool[lastIndex];
            pool.RemoveAt(lastIndex);
            return new PooledResource<T[]>(instance, _onDispose);
        }

        private static void Release(T[] resource)
        {
            int length = resource.Length;
            Array.Clear(resource, 0, length);
            if (!_pool.TryGetValue(length, out List<T[]> pool))
            {
                pool = new List<T[]>();
                _pool[resource.Length] = pool;
            }
            pool.Add(resource);
        }
    }

    public readonly struct PooledResource<T> : IDisposable
    {
        public readonly T resource;
        private readonly Action<T> _onDispose;

        internal PooledResource(T resource, Action<T> onDispose)
        {
            this.resource = resource;
            _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
        }

        public void Dispose()
        {
            _onDispose(resource);
        }
    }
}

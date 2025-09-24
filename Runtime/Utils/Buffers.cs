namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;
#if !SINGLE_THREADED
    using System.Threading;
    using System.Collections.Concurrent;
#endif

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

#if SINGLE_THREADED
    public sealed class WallstopGenericPool<T> : IDisposable
    {
        private readonly Func<T> _producer;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDispose;

        private readonly Stack<T> _pool = new();

        public WallstopGenericPool(
            Func<T> producer,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDisposal = null
        )
        {
            _producer = producer ?? throw new ArgumentNullException(nameof(producer));
            _onGet = onGet;
            _onRelease = onRelease ?? (_ => { });
            _onRelease += _pool.Push;
            _onDispose = onDisposal;
        }

        public PooledResource<T> Get()
        {
            if (!_pool.TryPop(out T value))
            {
                value = _producer();
            }

            _onGet?.Invoke(value);
            return new PooledResource<T>(value, _onRelease);
        }

        public void Dispose()
        {
            if (_onDispose == null)
            {
                _pool.Clear();
                return;
            }

            while (_pool.TryPop(out T value))
            {
                _onDispose(value);
            }
        }
    }
#else
    public sealed class WallstopGenericPool<T> : IDisposable
    {
        private readonly Func<T> _producer;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDispose;

        private readonly ConcurrentStack<T> _pool = new();

        public WallstopGenericPool(
            Func<T> producer,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDisposal = null
        )
        {
            _producer = producer ?? throw new ArgumentNullException(nameof(producer));
            _onGet = onGet;
            _onRelease = onRelease ?? (_ => { });
            _onRelease += _pool.Push;
            _onDispose = onDisposal;
        }

        public PooledResource<T> Get()
        {
            if (!_pool.TryPop(out T value))
            {
                value = _producer();
            }

            _onGet?.Invoke(value);
            return new PooledResource<T>(value, _onRelease);
        }

        public void Dispose()
        {
            if (_onDispose == null)
            {
                _pool.Clear();
                return;
            }

            while (_pool.TryPop(out T value))
            {
                _onDispose(value);
            }
        }
    }
#endif

#if SINGLE_THREADED
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
#else
    public static class WallstopArrayPool<T>
    {
        private static readonly ConcurrentDictionary<int, ConcurrentStack<T[]>> _pool = new();
        private static readonly Action<T[]> _onRelease = Release;

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

            ConcurrentStack<T[]> result = _pool.GetOrAdd(size, _ => new ConcurrentStack<T[]>());
            if (!result.TryPop(out T[] array))
            {
                array = new T[size];
            }

            return new PooledResource<T[]>(array, _onRelease);
        }

        private static void Release(T[] resource)
        {
            int length = resource.Length;
            Array.Clear(resource, 0, length);
            ConcurrentStack<T[]> result = _pool.GetOrAdd(length, _ => new ConcurrentStack<T[]>());
            result.Push(resource);
        }
    }
#endif

#if SINGLE_THREADED
    public static class WallstopFastArrayPool<T>
    {
        private static readonly List<Stack<T[]>> _pool = new();
        private static readonly Action<T[]> _onRelease = Release;

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

            while (_pool.Count <= size)
            {
                _pool.Add(null);
            }

            Stack<T[]> pool = _pool[size];
            if (pool == null)
            {
                pool = new Stack<T[]>();
                _pool[size] = pool;
            }

            if (!pool.TryPop(out T[] instance))
            {
                instance = new T[size];
            }

            return new PooledResource<T[]>(instance, _onRelease);
        }

        private static void Release(T[] resource)
        {
            _pool[resource.Length].Push(resource);
        }
    }
#else
    public static class WallstopFastArrayPool<T>
    {
        private static readonly ReaderWriterLockSlim _lock = new();
        private static readonly List<ConcurrentStack<T[]>> _pool = new();
        private static readonly Action<T[]> _onRelease = Release;

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

            bool withinRange;
            ConcurrentStack<T[]> pool = null;
            _lock.EnterReadLock();
            try
            {
                withinRange = size < _pool.Count;
                if (withinRange)
                {
                    pool = _pool[size];
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (withinRange)
            {
                if (pool == null)
                {
                    _lock.EnterUpgradeableReadLock();
                    try
                    {
                        pool = _pool[size];
                        if (pool == null)
                        {
                            _lock.EnterWriteLock();
                            try
                            {
                                pool = _pool[size];
                                if (pool == null)
                                {
                                    pool = new ConcurrentStack<T[]>();
                                    _pool[size] = pool;
                                }
                            }
                            finally
                            {
                                _lock.ExitWriteLock();
                            }
                        }
                    }
                    finally
                    {
                        _lock.ExitUpgradeableReadLock();
                    }
                }
            }
            else
            {
                _lock.EnterUpgradeableReadLock();
                try
                {
                    if (size < _pool.Count)
                    {
                        pool = _pool[size];
                        if (pool == null)
                        {
                            _lock.EnterWriteLock();
                            try
                            {
                                pool = _pool[size];
                                if (pool == null)
                                {
                                    pool = new ConcurrentStack<T[]>();
                                    _pool[size] = pool;
                                }
                            }
                            finally
                            {
                                _lock.ExitWriteLock();
                            }
                        }
                    }
                    else
                    {
                        _lock.EnterWriteLock();
                        try
                        {
                            while (_pool.Count <= size)
                            {
                                _pool.Add(null);
                            }
                            pool = _pool[size];
                            if (pool == null)
                            {
                                pool = new ConcurrentStack<T[]>();
                                _pool[size] = pool;
                            }
                        }
                        finally
                        {
                            _lock.ExitWriteLock();
                        }
                    }
                }
                finally
                {
                    _lock.ExitUpgradeableReadLock();
                }
            }

            if (!pool.TryPop(out T[] instance))
            {
                instance = new T[size];
            }

            return new PooledResource<T[]>(instance, _onRelease);
        }

        private static void Release(T[] resource)
        {
            _pool[resource.Length].Push(resource);
        }
    }
#endif

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

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("WallstopStudios.UnityHelpers.Tests.Runtime")]

namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Text.Json;
    using UnityEngine;
#if !SINGLE_THREADED
    using System.Threading;
    using System.Collections.Concurrent;
#else
    using WallstopStudios.UnityHelpers.Core.Extension;
#endif
    public static class Buffers
    {
#if SINGLE_THREADED
        private static readonly Dictionary<float, WaitForSeconds> WaitForSeconds = new();
        private static readonly Dictionary<float, WaitForSecondsRealtime> WaitForSecondsRealtime =
            new();
#else
        private static readonly ConcurrentDictionary<float, WaitForSeconds> WaitForSeconds = new();
        private static readonly ConcurrentDictionary<
            float,
            WaitForSecondsRealtime
        > WaitForSecondsRealtime = new();
#endif

        public static readonly WaitForFixedUpdate WaitForFixedUpdate = new();
        public static readonly WaitForEndOfFrame WaitForEndOfFrame = new();

        public static readonly WallstopGenericPool<StringBuilder> StringBuilder = new(
            () => new StringBuilder(),
            onRelease: builder => builder.Clear()
        );

        /*
            Note: Only use with CONSTANT time values, otherwise this is a memory leak.
            DO NOT USE with random values.
         */
        public static WaitForSeconds GetWaitForSeconds(float seconds)
        {
            return WaitForSeconds.GetOrAdd(seconds, value => new WaitForSeconds(value));
        }

        /*
            Note: Only use with CONSTANT time values, otherwise this is a memory leak.
            DO NOT USE with random values.
         */
        public static WaitForSecondsRealtime GetWaitForSecondsRealTime(float seconds)
        {
            return WaitForSecondsRealtime.GetOrAdd(
                seconds,
                value => new WaitForSecondsRealtime(value)
            );
        }
    }

    public static class Buffers<T>
    {
        public static readonly WallstopGenericPool<List<T>> List = new(
            () => new List<T>(),
            onRelease: list => list.Clear()
        );

        public static readonly WallstopGenericPool<HashSet<T>> HashSet = new(
            () => new HashSet<T>(),
            onRelease: set => set.Clear()
        );

        public static readonly WallstopGenericPool<Queue<T>> Queue = new(
            () => new Queue<T>(),
            onRelease: queue => queue.Clear()
        );

        public static readonly WallstopGenericPool<Stack<T>> Stack = new(
            () => new Stack<T>(),
            onRelease: stack => stack.Clear()
        );

        public static readonly WallstopGenericPool<SortedSet<T>> SortedSet = new(
            () => new SortedSet<T>(),
            onRelease: set => set.Clear()
        );
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
            int preWarmCount = 0,
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
            for (int i = 0; i < preWarmCount; ++i)
            {
                T value = _producer();
                _onGet?.Invoke(value);
                _onRelease(value);
            }
        }

        public PooledResource<T> Get()
        {
            return Get(out _);
        }

        public PooledResource<T> Get(out T value)
        {
            if (!_pool.TryPop(out value))
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
        internal int Count => _pool.Count;

        private readonly Func<T> _producer;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDispose;

        private readonly ConcurrentStack<T> _pool = new();

        public WallstopGenericPool(
            Func<T> producer,
            int preWarmCount = 0,
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
            for (int i = 0; i < preWarmCount; ++i)
            {
                T value = _producer();
                _onGet?.Invoke(value);
                _onRelease(value);
            }
        }

        public PooledResource<T> Get()
        {
            return Get(out _);
        }

        public PooledResource<T> Get(out T value)
        {
            if (!_pool.TryPop(out value))
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
            return Get(size, out _);
        }

        public static PooledResource<T[]> Get(int size, out T[] value)
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
                    value = Array.Empty<T>();
                    return new PooledResource<T[]>(value, _ => { });
                }
            }

            if (!_pool.TryGetValue(size, out List<T[]> pool))
            {
                pool = new List<T[]>();
                _pool[size] = pool;
            }

            if (pool.Count == 0)
            {
                value = new T[size];
                return new PooledResource<T[]>(value, _onDispose);
            }

            int lastIndex = pool.Count - 1;
            value = pool[lastIndex];
            pool.RemoveAt(lastIndex);
            return new PooledResource<T[]>(value, _onDispose);
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
            return Get(size, out _);
        }

        public static PooledResource<T[]> Get(int size, out T[] value)
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
                    value = Array.Empty<T>();
                    return new PooledResource<T[]>(value, _ => { });
                }
            }

            ConcurrentStack<T[]> result = _pool.GetOrAdd(size, _ => new ConcurrentStack<T[]>());
            if (!result.TryPop(out value))
            {
                value = new T[size];
            }

            return new PooledResource<T[]>(value, _onRelease);
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
            return Get(size, out _);
        }

        public static PooledResource<T[]> Get(int size, out T[] value)
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
                    value = Array.Empty<T>();
                    return new PooledResource<T[]>(value, _ => { });
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

            if (!pool.TryPop(out value))
            {
                value = new T[size];
            }

            return new PooledResource<T[]>(value, _onRelease);
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
            return Get(size, out _);
        }

        public static PooledResource<T[]> Get(int size, out T[] value)
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
                    value = Array.Empty<T>();
                    return new PooledResource<T[]>(value, _ => { });
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

            if (!pool.TryPop(out value))
            {
                value = new T[size];
            }

            return new PooledResource<T[]>(value, _onRelease);
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
        private readonly bool _initialized;

        internal PooledResource(T resource, Action<T> onDispose)
        {
            _initialized = true;
            this.resource = resource;
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (!_initialized)
            {
                return;
            }

            _onDispose(resource);
        }
    }
}

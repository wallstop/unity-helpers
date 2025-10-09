namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using Helper;
    using Random;
    using UnityEngine;

    /// <summary>
    /// A lightweight time-based cache that recomputes a value after a TTL expires.
    /// </summary>
    /// <typeparam name="T">Value type produced by the cache factory.</typeparam>
    /// <remarks>
    /// Use for expensive computations that can be reused for a short period (e.g., path costs, counts, queries).
    /// Optionally introduces a one-time jitter to spread refreshes across frames when many caches exist.
    /// </remarks>
    public sealed class TimedCache<T>
    {
        /// <summary>
        /// Gets the cached value, recomputing if the TTL (plus optional jitter) has elapsed.
        /// </summary>
        public T Value
        {
            get
            {
                if (!_lastRead.HasValue)
                {
                    Reset();
                }
                else if (
                    Helpers.HasEnoughTimePassed(
                        _lastRead.Value,
                        _cacheTtl + (_shouldUseJitter && !_usedJitter ? _jitterAmount : 0f)
                    )
                )
                {
                    if (_shouldUseJitter)
                    {
                        _usedJitter = true;
                    }
                    Reset();
                }

                return _value;
            }
        }

        private readonly Func<T> _valueProducer;
        private readonly float _cacheTtl;

        private float? _lastRead;
        private T _value;

        private bool _usedJitter;
        private readonly bool _shouldUseJitter;
        private readonly float _jitterAmount;

        /// <summary>
        /// Creates a time-based cache.
        /// </summary>
        /// <param name="valueProducer">Factory invoked to recompute the value.</param>
        /// <param name="cacheTtl">Time to live, in seconds.</param>
        /// <param name="useJitter">If true, applies a single randomized offset up to <paramref name="cacheTtl"/> to the first refresh.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="valueProducer"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="cacheTtl"/> is negative.</exception>
        public TimedCache(Func<T> valueProducer, float cacheTtl, bool useJitter = false)
        {
            _valueProducer =
                valueProducer ?? throw new ArgumentNullException(nameof(valueProducer));
            if (cacheTtl < 0)
            {
                throw new ArgumentException(nameof(cacheTtl));
            }

            _cacheTtl = cacheTtl;
            _shouldUseJitter = useJitter;
            _jitterAmount = useJitter ? PRNG.Instance.NextFloat(0f, cacheTtl) : 0f;
        }

        /// <summary>
        /// Forces the cache to recompute the value and resets the TTL timer.
        /// </summary>
        public void Reset()
        {
            _value = _valueProducer();
            _lastRead = Time.time;
        }
    }
}

namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using Helper;
    using Random;
    using UnityEngine;

    public sealed class TimedCache<T>
    {
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

        public void Reset()
        {
            _value = _valueProducer();
            _lastRead = Time.time;
        }
    }
}

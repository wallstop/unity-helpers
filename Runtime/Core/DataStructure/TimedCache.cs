namespace UnityHelpers.Core.DataStructure
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
                if (!_lastRead.HasValue || Helpers.HasEnoughTimePassed(_lastRead.Value, _cacheTtl))
                {
                    Reset();
                }

                return _value;
            }
        }

        private readonly Func<T> _valueProducer;
        private readonly float _cacheTtl;

        private float? _lastRead;
        private T _value;

        public TimedCache(Func<T> valueProducer, float cacheTtl, bool useJitter = false)
        {
            _valueProducer = valueProducer ?? throw new ArgumentNullException(nameof(valueProducer));
            if (cacheTtl < 0)
            {
                throw new ArgumentException(nameof(cacheTtl));
            }

            _cacheTtl = cacheTtl;
            if (useJitter && 0 <_cacheTtl)
            {
                _cacheTtl += PcgRandom.Instance.NextFloat(_cacheTtl);
            }
        }

        public void Reset()
        {
            _value = _valueProducer();
            _lastRead = Time.time;
        }
    }
}

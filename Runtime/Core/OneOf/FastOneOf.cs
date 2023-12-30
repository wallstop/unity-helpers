namespace Core.OneOf
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Helper;

    // Like OneOf, except that Equals doesn't allocate
    public readonly struct FastOneOf<T0, T1, T2> : IEquatable<FastOneOf<T0, T1, T2>>
    {
        private static readonly EqualityComparer<T0> _t0Comparer = EqualityComparer<T0>.Default;
        private static readonly EqualityComparer<T1> _t1Comparer = EqualityComparer<T1>.Default;
        private static readonly EqualityComparer<T2> _t2Comparer = EqualityComparer<T2>.Default;

        private readonly T0 _value0;
        private readonly T1 _value1;
        private readonly T2 _value2;
        private readonly int _index;

        public int Index => _index;

        public bool IsT0
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _index == 0;
        }

        public bool IsT1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _index == 1;
        }

        public bool IsT2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _index == 2;
        }

        public T0 AsT0
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_index != 0)
                {
                    throw new InvalidOperationException($"Cannot return as T0 as result is T{_index}");
                }

                return _value0;
            }
        }


        public T1 AsT1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_index != 1)
                {
                    throw new InvalidOperationException($"Cannot return as T1 as result is T{_index}");
                }

                return _value1;
            }
        }

        public T2 AsT2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_index != 2)
                {
                    throw new InvalidOperationException($"Cannot return as T2 as result is T{_index}");
                }

                return _value2;
            }
        }

        private FastOneOf(int index, T0 value0 = default, T1 value1 = default, T2 value2 = default)
        {
            _index = index;
            _value0 = value0;
            _value1 = value1;
            _value2 = value2;
        }

        public static implicit operator FastOneOf<T0, T1, T2>(T0 value) => new(0, value0: value);
        public static implicit operator FastOneOf<T0, T1, T2>(T1 value) => new(1, value1: value);
        public static implicit operator FastOneOf<T0, T1, T2>(T2 value) => new(2, value2: value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FastOneOf<T0, T1, T2> other)
        {
            if (_index != other._index)
            {
                return false;
            }

            switch (_index)
            {
                case 0:
                    return _t0Comparer.Equals(_value0, other._value0);
                case 1:
                    return _t1Comparer.Equals(_value1, other._value1);
                case 2:
                    return _t2Comparer.Equals(_value2, other._value2);
                default:
                    throw new ArgumentException($"Index {_index} out of range");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Switch(Action<T0> f0, Action<T1> f1, Action<T2> f2)
        {
            switch (_index)
            {
                case 0:
                    f0?.Invoke(_value0);
                    return;
                case 1:
                    f1?.Invoke(_value1);
                    return;
                case 2:
                    f2?.Invoke(_value2);
                    return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is FastOneOf<T0, T1, T2> other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Objects.HashCode(_value0, _value1, _value2, _index);
        }
    }
}

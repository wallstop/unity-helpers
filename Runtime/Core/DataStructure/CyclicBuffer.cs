namespace UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Helper;

    public enum BufferOverflowBehavior
    {
        Prepend,
        Append,
    }

    [Serializable]
    public sealed class CyclicBuffer<T> : IReadOnlyList<T>
    {
        public int Count { get; private set; }
        public readonly BufferOverflowBehavior overflowBehavior;
        public readonly int capacity;

        private readonly T[] _buffer;
        private int _position;

        public T this[int index]
        {
            get
            {
                BoundsCheck(index);
                return _buffer[AdjustedIndexFor(index)];
            }
            set
            {
                BoundsCheck(index);
                _buffer[AdjustedIndexFor(index)] = value;
            }
        }

        public CyclicBuffer(
            int capacity,
            BufferOverflowBehavior overflowBehavior = BufferOverflowBehavior.Prepend
        )
        {
            if (capacity < 0)
            {
                throw new ArgumentException(nameof(capacity));
            }
            this.overflowBehavior = overflowBehavior;
            this.capacity = capacity;
            _position = 0;
            _buffer = new T[capacity];
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            _buffer[_position] = item;
            _position = _position.WrappedIncrement(capacity);
            if (Count < capacity)
            {
                ++Count;
            }
        }

        public void Clear()
        {
            /* Simply reset state */
            Count = 0;
            _position = 0;
        }

        public bool Peek(out T value)
        {
            if (InBounds(0))
            {
                value = this[0];
                return true;
            }
            value = default(T);
            return false;
        }

        private int AdjustedIndexFor(int index)
        {
            switch (overflowBehavior)
            {
                case BufferOverflowBehavior.Prepend:
                {
                    return (_position - 1 + capacity - index) % capacity;
                }
                case BufferOverflowBehavior.Append:
                {
                    return index;
                }
                default:
                {
                    throw new InvalidEnumArgumentException(
                        nameof(overflowBehavior),
                        (int)overflowBehavior,
                        typeof(BufferOverflowBehavior)
                    );
                }
            }
        }

        private void BoundsCheck(int index)
        {
            if (!InBounds(index))
            {
                throw new IndexOutOfRangeException($"{index} is outside of bounds [0, {Count})");
            }
        }

        private bool InBounds(int index)
        {
            return !(Count <= index || index < 0);
        }
    }
}

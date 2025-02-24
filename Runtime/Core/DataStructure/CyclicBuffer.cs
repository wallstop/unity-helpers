namespace UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Helper;

    [Serializable]
    public sealed class CyclicBuffer<T> : IReadOnlyList<T>
    {
        public int Count { get; private set; }
        public readonly int capacity;

        private readonly List<T> _buffer;
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

        public CyclicBuffer(int capacity, IEnumerable<T> initialContents = null)
        {
            if (capacity < 0)
            {
                throw new ArgumentException(nameof(capacity));
            }

            this.capacity = capacity;
            _position = 0;
            Count = 0;
            _buffer = new List<T>();
            foreach (T item in initialContents ?? Enumerable.Empty<T>())
            {
                Add(item);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
            {
                // No need for bounds check, we're safe
                yield return _buffer[AdjustedIndexFor(i)];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            if (capacity == 0)
            {
                return;
            }

            if (_position < _buffer.Count)
            {
                _buffer[_position] = item;
            }
            else
            {
                _buffer.Add(item);
            }

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
            const int firstIndex = 0;
            if (InBounds(firstIndex))
            {
                value = _buffer[AdjustedIndexFor(firstIndex)];
                return true;
            }

            value = default;
            return false;
        }

        private int AdjustedIndexFor(int index)
        {
            if (index < _buffer.Count)
            {
                return index;
            }
            return (_position - 1 + capacity - index) % capacity;
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
            return 0 <= index && index < Count;
        }
    }
}

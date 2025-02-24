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
        public bool IsReadOnly => false;

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
            _buffer.Clear();
        }

        public bool Contains(T item)
        {
            return _buffer.Contains(item);
        }

        private int AdjustedIndexFor(int index)
        {
            long longCapacity = capacity;
            unchecked
            {
                int adjustedIndex = (int)(
                    (_position - 1L + longCapacity - (_buffer.Count - 1 - index)) % longCapacity
                );
                return adjustedIndex;
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
            return 0 <= index && index < Count;
        }
    }
}

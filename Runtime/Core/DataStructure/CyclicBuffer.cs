namespace UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Extension;
    using Helper;

    [Serializable]
    public sealed class CyclicBuffer<T> : IReadOnlyList<T>
    {
        public int Capacity { get; private set; }
        public int Count { get; private set; }

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

            Capacity = capacity;
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
                // No need for bound check, we're safe
                yield return _buffer[AdjustedIndexFor(i)];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            if (Capacity == 0)
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

            _position = _position.WrappedIncrement(Capacity);
            if (Count < Capacity)
            {
                ++Count;
            }
        }

        public void Clear()
        {
            Count = 0;
            _position = 0;
            _buffer.Clear();
        }

        public void Resize(int newCapacity)
        {
            if (newCapacity == Capacity)
            {
                return;
            }

            if (newCapacity < 0)
            {
                throw new ArgumentException(nameof(newCapacity));
            }

            int oldCapacity = Capacity;
            Capacity = newCapacity;
            _buffer.Shift(-_position);
            if (newCapacity < _buffer.Count)
            {
                _buffer.RemoveRange(newCapacity, _buffer.Count - newCapacity);
            }

            _position =
                newCapacity < oldCapacity && newCapacity <= _buffer.Count ? 0 : _buffer.Count;
            Count = Math.Min(newCapacity, Count);
        }

        public bool Contains(T item)
        {
            return _buffer.Contains(item);
        }

        private int AdjustedIndexFor(int index)
        {
            long longCapacity = Capacity;
            if (longCapacity == 0L)
            {
                return 0;
            }
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

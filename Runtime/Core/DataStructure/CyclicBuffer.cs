namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Extension;
    using Helper;
    using WallstopStudios.UnityHelpers.Utils;

    [Serializable]
    public sealed class CyclicBuffer<T> : IReadOnlyList<T>
    {
        public struct CyclicBufferEnumerator : IEnumerator<T>
        {
            private readonly CyclicBuffer<T> _buffer;

            private int _index;
            private T _current;

            internal CyclicBufferEnumerator(CyclicBuffer<T> buffer)
            {
                _buffer = buffer;
                _index = -1;
                _current = default;
            }

            public bool MoveNext()
            {
                if (++_index < _buffer.Count)
                {
                    _current = _buffer._buffer[_buffer.AdjustedIndexFor(_index)];
                    return true;
                }

                _current = default;
                return false;
            }

            public T Current => _current;

            object IEnumerator.Current => Current;

            public void Reset()
            {
                _index = -1;
                _current = default;
            }

            public void Dispose() { }
        }

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
            if (initialContents != null)
            {
                foreach (T item in initialContents)
                {
                    Add(item);
                }
            }
        }

        public CyclicBufferEnumerator GetEnumerator()
        {
            return new CyclicBufferEnumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
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

        public bool Remove(T element, IEqualityComparer<T> comparer = null)
        {
            if (Count == 0)
            {
                return false;
            }

            comparer ??= EqualityComparer<T>.Default;

            using PooledResource<List<T>> listResource = Buffers<T>.List.Get(out List<T> temp);
            for (int i = 0; i < Count; ++i)
            {
                temp.Add(_buffer[AdjustedIndexFor(i)]);
            }

            bool removed = false;
            for (int i = 0; i < temp.Count; ++i)
            {
                if (comparer.Equals(temp[i], element))
                {
                    temp.RemoveAt(i);
                    removed = true;
                    break;
                }
            }

            if (!removed)
            {
                return false;
            }

            RebuildFromCache(temp);
            return true;
        }

        public int RemoveAll(Predicate<T> predicate)
        {
            if (Count == 0)
            {
                return 0;
            }

            using PooledResource<List<T>> listResource = Buffers<T>.List.Get(out List<T> temp);
            for (int i = 0; i < Count; ++i)
            {
                temp.Add(_buffer[AdjustedIndexFor(i)]);
            }

            int removedCount = temp.RemoveAll(predicate);
            if (removedCount == 0)
            {
                return 0;
            }

            RebuildFromCache(temp);
            return removedCount;
        }

        private void RebuildFromCache(List<T> temp)
        {
            _buffer.Clear();
            _buffer.AddRange(temp);
            Count = temp.Count;
            _position = Count < Capacity ? Count : 0;
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

namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Extension;
    using Helper;
    using ProtoBuf;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    [Serializable]
    [ProtoContract(IgnoreListHandling = true)]
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

        [ProtoMember(1)]
        [field: SerializeField]
        public int Capacity { get; private set; }

        [ProtoMember(2)]
        [field: SerializeField]
        public int Count { get; private set; }

        [ProtoMember(3)]
        private List<T> _serializedItems;

        [SerializeField]
        [ProtoIgnore]
        private List<T> _buffer;

        [SerializeField]
        [ProtoMember(4)]
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

        private CyclicBuffer()
        {
            Capacity = 0;
            _position = 0;
            Count = 0;
            _buffer = new List<T>();
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

        /// <summary>
        /// Attempts to remove and return the element at the front of the buffer in O(1) time.
        /// </summary>
        /// <param name="result">The element at the front if the buffer is not empty.</param>
        /// <returns>True if an element was removed, false if the buffer is empty.</returns>
        public bool TryPopFront(out T result)
        {
            if (Count == 0)
            {
                result = default;
                return false;
            }

            int frontIndex = GetHeadIndex();
            result = _buffer[frontIndex];
            _buffer[frontIndex] = default; // Clear reference for GC

            Count--;
            if (Count == 0)
            {
                _position = 0;
            }

            return true;
        }

        /// <summary>
        /// Attempts to remove and return the element at the back of the buffer in O(1) time.
        /// </summary>
        /// <param name="result">The element at the back if the buffer is not empty.</param>
        /// <returns>True if an element was removed, false if the buffer is empty.</returns>
        public bool TryPopBack(out T result)
        {
            if (Count == 0)
            {
                result = default;
                return false;
            }

            int backIndex = AdjustedIndexFor(Count - 1);
            result = _buffer[backIndex];
            _buffer[backIndex] = default; // Clear reference for GC

            Count--;
            _position = Count == 0 ? 0 : backIndex;

            return true;
        }

        [ProtoBeforeSerialization]
        private void OnProtoSerialize()
        {
            if (Count == 0)
            {
                _serializedItems = null;
                return;
            }

            List<T> buffer = _serializedItems;
            if (buffer == null)
            {
                buffer = new List<T>(Count);
            }
            else
            {
                buffer.Clear();
            }

            for (int i = 0; i < Count; i++)
            {
                buffer.Add(_buffer[AdjustedIndexFor(i)]);
            }

            _serializedItems = buffer;
        }

        [ProtoAfterSerialization]
        private void OnProtoSerialized()
        {
            _serializedItems = null;
        }

        [ProtoAfterDeserialization]
        private void OnProtoDeserialized()
        {
            int itemCount = _serializedItems?.Count ?? 0;
            int capacity = Capacity;

            if (capacity < itemCount)
            {
                capacity = itemCount;
            }

            Capacity = capacity;
            if (_buffer == null)
            {
                _buffer = new List<T>();
            }
            else
            {
                _buffer.Clear();
            }

            if (itemCount > 0)
            {
                _buffer.AddRange(_serializedItems);
            }

            Count = itemCount;
            _position = Count < Capacity ? Count : 0;
            _serializedItems = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetHeadIndex()
        {
            int capacity = Capacity;
            if (capacity == 0 || Count == 0)
            {
                return 0;
            }

            return (_position - Count).PositiveMod(capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AdjustedIndexFor(int index)
        {
            int capacity = Capacity;
            if (capacity == 0 || Count == 0)
            {
                return 0;
            }

            int adjustedIndex = GetHeadIndex() + index;
            if (adjustedIndex >= capacity)
            {
                adjustedIndex -= capacity;
            }

            return adjustedIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BoundsCheck(int index)
        {
            if (!InBounds(index))
            {
                throw new IndexOutOfRangeException($"{index} is outside of bounds [0, {Count})");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool InBounds(int index)
        {
            return 0 <= index && index < Count;
        }
    }
}

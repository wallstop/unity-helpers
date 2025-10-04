namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// A highly optimized double-ended queue (deque) implemented with a circular array.
    /// Supports efficient O(1) insertion and removal from both front and back.
    /// Ideal for BFS algorithms, undo/redo systems, and sliding window problems.
    /// </summary>
    /// <typeparam name="T">The type of elements in the deque.</typeparam>
    public sealed class Deque<T> : IReadOnlyList<T>
    {
        public struct DequeEnumerator : IEnumerator<T>
        {
            private readonly T[] _items;
            private readonly int _head;
            private readonly int _count;
            private readonly int _capacity;
            private int _index;
            private T _current;

            internal DequeEnumerator(T[] items, int head, int count, int capacity)
            {
                _items = items;
                _head = head;
                _count = count;
                _capacity = capacity;
                _index = -1;
                _current = default;
            }

            public bool MoveNext()
            {
                if (++_index < _count)
                {
                    int actualIndex = (_head + _index) % _capacity;
                    _current = _items[actualIndex];
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

        private const int DefaultCapacity = 16;
        private const int MinimumGrowth = 4;

        private T[] _items;
        private int _head;
        private int _tail;
        private int _count;

        /// <summary>
        /// Gets the number of elements in the deque.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Gets whether the deque is empty.
        /// </summary>
        public bool IsEmpty => _count == 0;

        /// <summary>
        /// Gets the current capacity of the underlying array.
        /// </summary>
        public int Capacity => _items.Length;

        /// <summary>
        /// Gets the element at the specified index (0 is front, Count-1 is back).
        /// </summary>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                {
                    throw new IndexOutOfRangeException(
                        $"{index} is outside of bounds [0, {_count})"
                    );
                }
                int actualIndex = (_head + index) % _items.Length;
                return _items[actualIndex];
            }
            set
            {
                if (index < 0 || index >= _count)
                {
                    throw new IndexOutOfRangeException(
                        $"{index} is outside of bounds [0, {_count})"
                    );
                }
                int actualIndex = (_head + index) % _items.Length;
                _items[actualIndex] = value;
            }
        }

        /// <summary>
        /// Constructs an empty deque with the specified capacity.
        /// </summary>
        public Deque(int capacity = DefaultCapacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity must be positive."
                );
            }
            _items = new T[capacity];
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        /// <summary>
        /// Constructs a deque from an existing collection.
        /// </summary>
        public Deque(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            int capacity = DefaultCapacity;
            if (collection is IReadOnlyCollection<T> roc)
            {
                capacity = Math.Max(DefaultCapacity, roc.Count);
            }
            else if (collection is ICollection<T> col)
            {
                capacity = Math.Max(DefaultCapacity, col.Count);
            }

            _items = new T[capacity];
            _head = 0;
            _tail = 0;
            _count = 0;

            foreach (T item in collection)
            {
                PushBack(item);
            }
        }

        /// <summary>
        /// Adds an element to the front of the deque in O(1) time.
        /// </summary>
        public void PushFront(T item)
        {
            if (_count == _items.Length)
            {
                Resize(ComputeGrowth(_items.Length));
            }

            _head = (_head - 1 + _items.Length) % _items.Length;
            _items[_head] = item;
            _count++;
        }

        /// <summary>
        /// Adds an element to the back of the deque in O(1) time.
        /// </summary>
        public void PushBack(T item)
        {
            if (_count == _items.Length)
            {
                Resize(ComputeGrowth(_items.Length));
            }

            _items[_tail] = item;
            _tail = (_tail + 1) % _items.Length;
            _count++;
        }

        /// <summary>
        /// Attempts to remove and return the element at the front of the deque.
        /// </summary>
        /// <returns>True if an element was removed, false if empty.</returns>
        public bool TryPopFront(out T result)
        {
            if (_count == 0)
            {
                result = default;
                return false;
            }

            result = _items[_head];
            _items[_head] = default;
            _head = (_head + 1) % _items.Length;
            _count--;
            return true;
        }

        /// <summary>
        /// Attempts to remove and return the element at the back of the deque.
        /// </summary>
        /// <returns>True if an element was removed, false if empty.</returns>
        public bool TryPopBack(out T result)
        {
            if (_count == 0)
            {
                result = default;
                return false;
            }

            _tail = (_tail - 1 + _items.Length) % _items.Length;
            result = _items[_tail];
            _items[_tail] = default;
            _count--;
            return true;
        }

        /// <summary>
        /// Attempts to peek at the front element without removing it.
        /// </summary>
        /// <returns>True if the deque is not empty, false otherwise.</returns>
        public bool TryPeekFront(out T result)
        {
            if (_count == 0)
            {
                result = default;
                return false;
            }
            result = _items[_head];
            return true;
        }

        /// <summary>
        /// Attempts to peek at the back element without removing it.
        /// </summary>
        /// <returns>True if the deque is not empty, false otherwise.</returns>
        public bool TryPeekBack(out T result)
        {
            if (_count == 0)
            {
                result = default;
                return false;
            }
            int backIndex = (_tail - 1 + _items.Length) % _items.Length;
            result = _items[backIndex];
            return true;
        }

        /// <summary>
        /// Removes all elements from the deque.
        /// </summary>
        public void Clear()
        {
            if (_count > 0)
            {
                if (_head < _tail)
                {
                    Array.Clear(_items, _head, _count);
                }
                else
                {
                    Array.Clear(_items, _head, _items.Length - _head);
                    Array.Clear(_items, 0, _tail);
                }
            }
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        /// <summary>
        /// Checks if the deque contains a specific element in O(n) time.
        /// </summary>
        public bool Contains(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < _count; i++)
            {
                int actualIndex = (_head + i) % _items.Length;
                if (comparer.Equals(_items[actualIndex], item))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Copies the deque elements to an array.
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }
            if (array.Length - arrayIndex < _count)
            {
                throw new ArgumentException("Destination array is not large enough.");
            }

            for (int i = 0; i < _count; i++)
            {
                int actualIndex = (_head + i) % _items.Length;
                array[arrayIndex + i] = _items[actualIndex];
            }
        }

        /// <summary>
        /// Returns an array containing all elements in order.
        /// </summary>
        public T[] ToArray()
        {
            T[] result = new T[_count];
            CopyTo(result, 0);
            return result;
        }

        /// <summary>
        /// Trims excess capacity to match the current count.
        /// </summary>
        public void TrimExcess()
        {
            int threshold = (int)(_items.Length * 0.9);
            if (_count < threshold)
            {
                int newCapacity = Math.Max(DefaultCapacity, _count);
                if (newCapacity < _items.Length)
                {
                    Resize(newCapacity);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ComputeGrowth(int currentCapacity)
        {
            int growth = currentCapacity + (currentCapacity >> 1);
            int newCapacity = currentCapacity + Math.Max(growth - currentCapacity, MinimumGrowth);

            if ((uint)newCapacity > 0X7FFFFFC7)
            {
                newCapacity = 0X7FFFFFC7;
            }

            return newCapacity;
        }

        private void Resize(int newCapacity)
        {
            T[] newItems = new T[newCapacity];

            if (_count > 0)
            {
                if (_head < _tail)
                {
                    Array.Copy(_items, _head, newItems, 0, _count);
                }
                else
                {
                    int headToEnd = _items.Length - _head;
                    Array.Copy(_items, _head, newItems, 0, headToEnd);
                    Array.Copy(_items, 0, newItems, headToEnd, _tail);
                }
            }

            _items = newItems;
            _head = 0;
            _tail = _count;
        }

        public DequeEnumerator GetEnumerator()
        {
            return new DequeEnumerator(_items, _head, _count, _items.Length);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

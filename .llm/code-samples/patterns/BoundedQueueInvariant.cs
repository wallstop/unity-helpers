// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// BoundedQueue with invariant maintenance
// Always ensure internal state remains valid regardless of input

namespace WallstopStudios.UnityHelpers.Examples
{
    using UnityEngine;

    public sealed class BoundedQueue<T>
    {
        private readonly T[] _buffer;
        private int _head;
        private int _tail;
        private int _count;
        private readonly int _capacity;

        public BoundedQueue(int capacity)
        {
            _capacity = capacity;
            _buffer = new T[capacity];
        }

        public int Count => _count;

        public void Enqueue(T item)
        {
            // Maintain invariant: count never exceeds capacity
            if (_count >= _capacity)
            {
                // Overwrite oldest (circular buffer behavior)
                _head = (_head + 1) % _capacity;
            }
            else
            {
                _count++;
            }

            _buffer[_tail] = item;
            _tail = (_tail + 1) % _capacity;
        }

        public bool TryDequeue(out T result)
        {
            if (_count == 0)
            {
                result = default;
                return false;
            }

            result = _buffer[_head];
            _buffer[_head] = default; // Clear reference to allow GC
            _head = (_head + 1) % _capacity;
            _count--;

            // Invariant assertions (debug only)
            Debug.Assert(_head >= 0 && _head < _capacity, "Head index out of bounds");
            Debug.Assert(_tail >= 0 && _tail < _capacity, "Tail index out of bounds");
            Debug.Assert(_count >= 0 && _count <= _capacity, "Count out of bounds");

            return true;
        }

        public bool TryPeek(out T result)
        {
            if (_count == 0)
            {
                result = default;
                return false;
            }

            result = _buffer[_head];
            return true;
        }

        public void Clear()
        {
            // Clear all references for GC
            for (int i = 0; i < _buffer.Length; i++)
            {
                _buffer[i] = default;
            }
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }
}

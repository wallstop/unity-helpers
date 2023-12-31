namespace UnityHelpers.Core.DataStructure
{
    using Helper;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;

    public enum BufferAddMethod
    {
        Prepend,
        Append
    }

    [Serializable]
    public sealed class CyclicBuffer<T> : IEnumerable<T>
    {
        private readonly T[] _buffer;

        private int _position;

        public readonly BufferAddMethod AddMethod;

        public readonly int Capacity;

        public int Count { get; private set; }

        public readonly bool IsReadOnly;

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

        public CyclicBuffer(int capacity, BufferAddMethod addMethod = BufferAddMethod.Prepend)
        {
            if (capacity < 0)
            {
                throw new ArgumentException(nameof(capacity));
            }
            AddMethod = addMethod;
            Capacity = capacity;
            _position = 0;
            _buffer = new T[capacity];
            IsReadOnly = false;
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
            _position = WallMath.WrappedIncrement(_position, Capacity);
            if (Count < Capacity)
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
            switch (AddMethod)
            {
                case BufferAddMethod.Prepend:
                    {
                        return (_position - 1 + Capacity - index) % Capacity;
                    }
                case BufferAddMethod.Append:
                    {
                        return index;
                    }
                default:
                    {
                        throw new InvalidEnumArgumentException("Unexpected AddMethod: " + AddMethod);
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

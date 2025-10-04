namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// A compact, dynamically resizable bit set data structure for storing boolean flags.
    /// Uses a single bit per element for memory efficiency. Perfect for entity states,
    /// visibility masks, collision layers, and any scenario requiring dense boolean storage.
    /// Supports fast O(1) set, clear, and test operations with dynamic growth and shrinking.
    /// </summary>
    public sealed class BitSet : IEnumerable<bool>
    {
        private const int BitsPerLong = 64;
        private const int BitsPerLongShift = 6; // log2(64)
        private const int BitsPerLongMask = 63; // 64 - 1
        private const int DefaultCapacity = 64;

        private ulong[] _bits;
        private int _capacity;

        public struct BitEnumerator : IEnumerator<bool>
        {
            private readonly BitSet _bitSet;
            private int _index;
            private bool _current;

            internal BitEnumerator(BitSet bitSet)
            {
                _bitSet = bitSet;
                _index = -1;
                _current = false;
            }

            public bool MoveNext()
            {
                if (++_index < _bitSet._capacity)
                {
                    _bitSet.TryGet(_index, out _current);
                    return true;
                }
                _current = false;
                return false;
            }

            public bool Current => _current;

            object IEnumerator.Current => Current;

            public void Reset()
            {
                _index = -1;
                _current = false;
            }

            public void Dispose() { }
        }

        /// <summary>
        /// Gets the current capacity (maximum number of bits that can be stored without resizing).
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Gets or sets the bit at the specified index.
        /// Automatically expands if setting a bit beyond current capacity.
        /// </summary>
        public bool this[int index]
        {
            get => TryGet(index, out bool value) && value;
            set
            {
                if (value)
                {
                    TrySet(index);
                }
                else
                {
                    TryClear(index);
                }
            }
        }

        /// <summary>
        /// Constructs a bit set with the specified initial capacity.
        /// </summary>
        public BitSet(int initialCapacity = DefaultCapacity)
        {
            if (initialCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialCapacity),
                    "Initial capacity must be positive."
                );
            }

            _capacity = initialCapacity;
            int arraySize = (initialCapacity + BitsPerLong - 1) >> BitsPerLongShift;
            _bits = new ulong[arraySize];
        }

        /// <summary>
        /// Attempts to set the bit at the specified index to 1.
        /// Automatically expands capacity if needed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySet(int index)
        {
            if (index < 0)
            {
                return false;
            }

            if (index >= _capacity)
            {
                EnsureCapacity(index + 1);
            }

            int arrayIndex = index >> BitsPerLongShift;
            int bitIndex = index & BitsPerLongMask;
            _bits[arrayIndex] |= 1UL << bitIndex;
            return true;
        }

        /// <summary>
        /// Attempts to clear the bit at the specified index to 0.
        /// Returns false if index is negative or beyond current capacity (does not auto-expand).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryClear(int index)
        {
            if (index < 0 || index >= _capacity)
            {
                return false;
            }

            int arrayIndex = index >> BitsPerLongShift;
            int bitIndex = index & BitsPerLongMask;
            _bits[arrayIndex] &= ~(1UL << bitIndex);
            return true;
        }

        /// <summary>
        /// Attempts to toggle the bit at the specified index.
        /// Automatically expands capacity if needed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFlip(int index)
        {
            if (index < 0)
            {
                return false;
            }

            if (index >= _capacity)
            {
                EnsureCapacity(index + 1);
            }

            int arrayIndex = index >> BitsPerLongShift;
            int bitIndex = index & BitsPerLongMask;
            _bits[arrayIndex] ^= 1UL << bitIndex;
            return true;
        }

        /// <summary>
        /// Attempts to get the bit at the specified index.
        /// Returns false (out parameter) for indices beyond capacity.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(int index, out bool value)
        {
            if (index < 0 || index >= _capacity)
            {
                value = false;
                return false;
            }

            int arrayIndex = index >> BitsPerLongShift;
            int bitIndex = index & BitsPerLongMask;
            value = (_bits[arrayIndex] & (1UL << bitIndex)) != 0;
            return true;
        }

        /// <summary>
        /// Ensures the bit set can hold at least the specified capacity.
        /// </summary>
        public void EnsureCapacity(int minCapacity)
        {
            if (minCapacity <= _capacity)
            {
                return;
            }

            int newCapacity = _capacity;
            while (newCapacity < minCapacity)
            {
                newCapacity = newCapacity < 256 ? newCapacity * 2 : newCapacity + (newCapacity / 2);
            }

            Resize(newCapacity);
        }

        /// <summary>
        /// Resizes the bit set to the specified capacity.
        /// If shrinking, bits beyond the new capacity are lost.
        /// </summary>
        public void Resize(int newCapacity)
        {
            if (newCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(newCapacity),
                    "Capacity must be positive."
                );
            }

            if (newCapacity == _capacity)
            {
                return;
            }

            int newArraySize = (newCapacity + BitsPerLong - 1) >> BitsPerLongShift;
            ulong[] newBits = new ulong[newArraySize];

            int copyLength = Math.Min(_bits.Length, newArraySize);
            Array.Copy(_bits, 0, newBits, 0, copyLength);

            _bits = newBits;
            _capacity = newCapacity;

            // Clear any bits beyond new capacity in the last element
            int remainingBits = newCapacity & BitsPerLongMask;
            if (remainingBits != 0 && newArraySize > 0)
            {
                ulong mask = (1UL << remainingBits) - 1;
                _bits[newArraySize - 1] &= mask;
            }
        }

        /// <summary>
        /// Shrinks the capacity to fit the highest set bit, or to a minimum capacity.
        /// </summary>
        public void TrimExcess(int minimumCapacity = DefaultCapacity)
        {
            int highestSetBit = -1;
            for (int i = _capacity - 1; i >= 0; i--)
            {
                if (TryGet(i, out bool value) && value)
                {
                    highestSetBit = i;
                    break;
                }
            }

            int targetCapacity = Math.Max(minimumCapacity, highestSetBit + 1);
            if (targetCapacity < _capacity)
            {
                Resize(targetCapacity);
            }
        }

        /// <summary>
        /// Sets all bits to 1 within the current capacity.
        /// </summary>
        public void SetAll()
        {
            for (int i = 0; i < _bits.Length; i++)
            {
                _bits[i] = ulong.MaxValue;
            }

            // Clear any bits beyond capacity in the last element
            int remainingBits = _capacity & BitsPerLongMask;
            if (remainingBits != 0 && _bits.Length > 0)
            {
                ulong mask = (1UL << remainingBits) - 1;
                _bits[_bits.Length - 1] &= mask;
            }
        }

        /// <summary>
        /// Sets all bits to 0.
        /// </summary>
        public void ClearAll()
        {
            Array.Clear(_bits, 0, _bits.Length);
        }

        /// <summary>
        /// Flips all bits (0 becomes 1, 1 becomes 0) within the current capacity.
        /// </summary>
        public void FlipAll()
        {
            for (int i = 0; i < _bits.Length; i++)
            {
                _bits[i] = ~_bits[i];
            }

            // Clear any bits beyond capacity in the last element
            int remainingBits = _capacity & BitsPerLongMask;
            if (remainingBits != 0 && _bits.Length > 0)
            {
                ulong mask = (1UL << remainingBits) - 1;
                _bits[_bits.Length - 1] &= mask;
            }
        }

        /// <summary>
        /// Inverts all bits (same as FlipAll).
        /// </summary>
        public void Not()
        {
            FlipAll();
        }

        /// <summary>
        /// Shifts all bits left by the specified amount.
        /// Bits shifted beyond capacity are lost, zeros fill from the right.
        /// </summary>
        public void LeftShift(int shift)
        {
            if (shift <= 0)
            {
                return;
            }

            if (shift >= _capacity)
            {
                ClearAll();
                return;
            }

            // Shift bit by bit for correctness
            for (int i = _capacity - 1; i >= shift; i--)
            {
                TryGet(i - shift, out bool value);
                if (value)
                {
                    TrySet(i);
                }
                else
                {
                    TryClear(i);
                }
            }

            // Clear the lower bits
            for (int i = 0; i < shift; i++)
            {
                TryClear(i);
            }
        }

        /// <summary>
        /// Shifts all bits right by the specified amount.
        /// Bits shifted out are lost, zeros fill from the left.
        /// </summary>
        public void RightShift(int shift)
        {
            if (shift <= 0)
            {
                return;
            }

            if (shift >= _capacity)
            {
                ClearAll();
                return;
            }

            // Shift bit by bit for correctness
            for (int i = 0; i < _capacity - shift; i++)
            {
                TryGet(i + shift, out bool value);
                if (value)
                {
                    TrySet(i);
                }
                else
                {
                    TryClear(i);
                }
            }

            // Clear the upper bits
            for (int i = _capacity - shift; i < _capacity; i++)
            {
                TryClear(i);
            }
        }

        /// <summary>
        /// Counts the number of bits set to 1.
        /// </summary>
        public int CountSetBits()
        {
            int count = 0;
            for (int i = 0; i < _bits.Length; i++)
            {
                count += PopCount(_bits[i]);
            }
            return count;
        }

        /// <summary>
        /// Checks if any bits are set to 1.
        /// </summary>
        public bool Any()
        {
            for (int i = 0; i < _bits.Length; i++)
            {
                if (_bits[i] != 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if all bits are set to 0.
        /// </summary>
        public bool None()
        {
            return !Any();
        }

        /// <summary>
        /// Checks if all bits (within capacity) are set to 1.
        /// </summary>
        public bool All()
        {
            for (int i = 0; i < _bits.Length - 1; i++)
            {
                if (_bits[i] != ulong.MaxValue)
                {
                    return false;
                }
            }

            // Check last element considering capacity
            if (_bits.Length > 0)
            {
                int remainingBits = _capacity & BitsPerLongMask;
                if (remainingBits == 0)
                {
                    return _bits[_bits.Length - 1] == ulong.MaxValue;
                }
                else
                {
                    ulong mask = (1UL << remainingBits) - 1;
                    return (_bits[_bits.Length - 1] & mask) == mask;
                }
            }

            return false;
        }

        /// <summary>
        /// Performs a bitwise AND operation with another BitSet.
        /// Resizes this BitSet to match the other if needed.
        /// </summary>
        public bool TryAnd(BitSet other)
        {
            if (other == null)
            {
                return false;
            }

            if (other._capacity != _capacity)
            {
                Resize(other._capacity);
            }

            for (int i = 0; i < _bits.Length; i++)
            {
                _bits[i] &= other._bits[i];
            }
            return true;
        }

        /// <summary>
        /// Performs a bitwise OR operation with another BitSet.
        /// Resizes this BitSet to match the other if needed.
        /// </summary>
        public bool TryOr(BitSet other)
        {
            if (other == null)
            {
                return false;
            }

            if (other._capacity > _capacity)
            {
                Resize(other._capacity);
            }

            int minLength = Math.Min(_bits.Length, other._bits.Length);
            for (int i = 0; i < minLength; i++)
            {
                _bits[i] |= other._bits[i];
            }
            return true;
        }

        /// <summary>
        /// Performs a bitwise XOR operation with another BitSet.
        /// Resizes this BitSet to match the other if needed.
        /// </summary>
        public bool TryXor(BitSet other)
        {
            if (other == null)
            {
                return false;
            }

            if (other._capacity > _capacity)
            {
                Resize(other._capacity);
            }

            int minLength = Math.Min(_bits.Length, other._bits.Length);
            for (int i = 0; i < minLength; i++)
            {
                _bits[i] ^= other._bits[i];
            }
            return true;
        }

        /// <summary>
        /// Populates the provided list with indices of all set bits.
        /// Clears the list before adding. Returns the same list for chaining.
        /// </summary>
        public List<int> GetSetBits(List<int> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            for (int i = 0; i < _capacity; i++)
            {
                if (TryGet(i, out bool value) && value)
                {
                    results.Add(i);
                }
            }
            return results;
        }

        /// <summary>
        /// Enumerates indices of all set bits (where value is 1).
        /// Use this for efficient iteration over sparse bitsets.
        /// </summary>
        public IEnumerable<int> EnumerateSetIndices()
        {
            for (int i = 0; i < _capacity; i++)
            {
                if (TryGet(i, out bool value) && value)
                {
                    yield return i;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PopCount(ulong value)
        {
            // Brian Kernighan's algorithm
            int count = 0;
            while (value != 0)
            {
                value &= value - 1;
                count++;
            }
            return count;
        }

        /// <summary>
        /// Returns an enumerator that iterates through all bit values (true/false) in order.
        /// Use foreach to iterate over all bits.
        /// </summary>
        public BitEnumerator GetEnumerator()
        {
            return new BitEnumerator(this);
        }

        IEnumerator<bool> IEnumerable<bool>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

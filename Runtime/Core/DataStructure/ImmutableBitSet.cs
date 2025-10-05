namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using ProtoBuf;

    /// <summary>
    /// An immutable value-type variant of BitSet that provides read-only access to bit data.
    /// Uses a single bit per element for memory efficiency. Perfect for passing bit flags
    /// without risk of modification, or for creating snapshots of BitSet state.
    /// Supports fast O(1) read operations and implements value semantics.
    /// </summary>
    [Serializable]
    [ProtoContract(IgnoreListHandling = true)]
    public readonly struct ImmutableBitSet : IReadOnlyList<bool>, IEquatable<ImmutableBitSet>
    {
        private const int BitsPerLong = 64;
        private const int BitsPerLongShift = 6; // log2(64)
        private const int BitsPerLongMask = 63; // 64 - 1

        [ProtoMember(1, IsPacked = true)]
        private readonly ulong[] _bits;

        [ProtoMember(2)]
        private readonly int _capacity;

        /// <summary>
        /// Gets the current capacity (number of bits stored).
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Gets the number of bits in this bit set.
        /// </summary>
        public int Count => _capacity;

        /// <summary>
        /// Gets the bit at the specified index.
        /// Returns false for indices beyond capacity.
        /// </summary>
        public bool this[int index]
        {
            get => TryGet(index, out bool value) && value;
        }

        /// <summary>
        /// Constructs an immutable bit set with the specified capacity and bit data.
        /// </summary>
        internal ImmutableBitSet(ulong[] bits, int capacity)
        {
            _bits = bits ?? Array.Empty<ulong>();
            _capacity = capacity;
        }

        /// <summary>
        /// Gets a copy of the internal bits array for serialization purposes.
        /// </summary>
        internal ulong[] GetBitsArrayCopy()
        {
            if (_bits == null)
            {
                return Array.Empty<ulong>();
            }
            ulong[] copy = new ulong[_bits.Length];
            Array.Copy(_bits, copy, _bits.Length);
            return copy;
        }

        /// <summary>
        /// Attempts to get the bit at the specified index.
        /// Returns false if the index is out of range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(int index, out bool value)
        {
            if (index < 0 || index >= _capacity || _bits == null)
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
        /// Counts the number of bits set to 1.
        /// </summary>
        public int CountSetBits()
        {
            if (_bits == null)
            {
                return 0;
            }
            int count = 0;
            foreach (ulong bit in _bits)
            {
                count += PopCount(bit);
            }
            return count;
        }

        /// <summary>
        /// Checks if any bits are set to 1.
        /// </summary>
        public bool Any()
        {
            if (_bits == null)
            {
                return false;
            }
            return Array.Exists(_bits, bit => bit != 0);
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
            if (_capacity <= 0 || _bits == null)
            {
                return false;
            }
            int fullSegments = _capacity >> BitsPerLongShift;
            for (int i = 0; i < fullSegments; i++)
            {
                if (_bits[i] != ulong.MaxValue)
                {
                    return false;
                }
            }
            int remainingBits = _capacity & BitsPerLongMask;
            if (remainingBits == 0)
            {
                return true;
            }
            if (_bits.Length <= fullSegments)
            {
                return false;
            }
            ulong mask = (1UL << remainingBits) - 1;
            return (_bits[fullSegments] & mask) == mask;
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

        /// <summary>
        /// Converts this immutable bit set to a mutable BitSet.
        /// Creates a new BitSet with a copy of the bit data.
        /// </summary>
        public BitSet ToBitSet()
        {
            BitSet result = new BitSet(_capacity > 0 ? _capacity : 64);
            if (_bits != null && _capacity > 0)
            {
                // Copy bits from this immutable set to the new mutable set
                for (int i = 0; i < _capacity; i++)
                {
                    if (TryGet(i, out bool value) && value)
                    {
                        result.TrySet(i);
                    }
                }
            }
            return result;
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

        /// <summary>
        /// Checks if this ImmutableBitSet is equal to another.
        /// </summary>
        public bool Equals(ImmutableBitSet other)
        {
            if (_capacity != other._capacity)
            {
                return false;
            }
            if (_bits == null && other._bits == null)
            {
                return true;
            }
            if (_bits == null || other._bits == null)
            {
                return false;
            }
            if (_bits.Length != other._bits.Length)
            {
                return false;
            }
            for (int i = 0; i < _bits.Length; i++)
            {
                if (_bits[i] != other._bits[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is ImmutableBitSet other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = _capacity.GetHashCode();
                if (_bits != null)
                {
                    foreach (ulong bit in _bits)
                    {
                        hash = (hash * 397) ^ bit.GetHashCode();
                    }
                }
                return hash;
            }
        }

        public static bool operator ==(ImmutableBitSet left, ImmutableBitSet right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ImmutableBitSet left, ImmutableBitSet right)
        {
            return !left.Equals(right);
        }

        public struct BitEnumerator : IEnumerator<bool>
        {
            private readonly ImmutableBitSet _bitSet;
            private int _index;
            private bool _current;

            internal BitEnumerator(ImmutableBitSet bitSet)
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
    }
}

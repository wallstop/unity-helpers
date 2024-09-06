namespace UnityHelpers.Core.Math
{
    using Extension;
    using Helper;
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///     Inclusive Range [min,max]
    /// </summary>
    [Serializable]
    [DataContract]
    public struct Range<T> : IEquatable<Range<T>> where T : IEquatable<T>, IComparable<T>
    {
        [DataMember]
        public T min;

        [DataMember]
        public T max;

        [DataMember]
        public bool startInclusive;

        [DataMember]
        public bool endInclusive;

        public Range(T min, T max, bool startInclusive = true, bool endInclusive = true)
        {
            this.min = min;
            this.max = max;
            this.startInclusive = startInclusive;
            this.endInclusive = endInclusive;
        }

        public bool Equals(Range<T> other)
        {
            return min.Equals(other.min) &&
                   max.Equals(other.max) &&
                   startInclusive == other.startInclusive &&
                   endInclusive == other.endInclusive;
        }

        public override bool Equals(object obj)
        {
            return obj is Range<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Objects.HashCode(min, max, startInclusive, endInclusive);
        }

        public override string ToString()
        {
            return this.ToJson();
        }

        public bool WithinRange(T value)
        {
            int comparison = value.CompareTo(min);
            if (comparison < 0)
            {
                return false;
            }

            if (!startInclusive && comparison == 0)
            {
                return false;
            }

            comparison = value.CompareTo(max);
            if (0 < comparison)
            {
                return false;
            }

            if (!endInclusive && comparison == 0)
            {
                return false;
            }

            return true;
        }
    }
}
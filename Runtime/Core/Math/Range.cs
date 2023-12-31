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
    public struct Range<T> : IEquatable<Range<T>> where T: IEquatable<T>, IComparable<T>
    {
        [DataMember]
        public T min;
        [DataMember]
        public T max;

        public Range(T min, T max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Equals(Range<T> other)
        {
            return Equals(min, other.min) && Equals(max, other.max);
        }

        public override bool Equals(object obj)
        {
            return obj is Range<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Objects.HashCode(min, max);
        }

        public override string ToString()
        {
            return this.ToJson();
        }

        public bool WithinRange(T value)
        {
            if (value.CompareTo(min) < 0)
            {
                return false;
            }

            return value.CompareTo(max) <= 0;
        }
    }
}

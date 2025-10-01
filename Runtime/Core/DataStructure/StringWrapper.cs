namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
#if !SINGLE_THREADED
    using System.Collections.Concurrent;
#else
    using WallstopStudios.UnityHelpers.Core.Extension;
    using System.Collections.Generic;
#endif

    /*
        Used to cache strings, meant to be used in place of strings as keys for when a Dictionary
        has a known set of values
     */
    [Serializable]
    public sealed class StringWrapper
        : IEquatable<StringWrapper>,
            IComparable<StringWrapper>,
            IDisposable
    {
#if SINGLE_THREADED
        private static readonly Dictionary<string, StringWrapper> Cache = new();
#else
        private static readonly ConcurrentDictionary<string, StringWrapper> Cache = new();
#endif

        public readonly string value;

        private readonly int _hashCode;

        private StringWrapper(string value)
        {
            this.value = value;
            _hashCode = value.GetHashCode();
        }

        public static StringWrapper Get(string value)
        {
            return Cache.GetOrAdd(value, key => new StringWrapper(key));
        }

        public static bool Remove(string value)
        {
            return Cache.TryRemove(value, out _);
        }

        public bool Equals(StringWrapper other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (_hashCode != other._hashCode)
            {
                return false;
            }

            return string.Equals(value, other.value, StringComparison.Ordinal);
        }

        public int CompareTo(StringWrapper other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (ReferenceEquals(other, null))
            {
                return -1;
            }

            int comparison = _hashCode.CompareTo(other._hashCode);
            if (comparison != 0)
            {
                return comparison;
            }

            return string.Compare(value, other.value, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override bool Equals(object other)
        {
            return Equals(other as StringWrapper);
        }

        public override string ToString()
        {
            return value;
        }

        public void Dispose()
        {
            Remove(value);
        }
    }
}

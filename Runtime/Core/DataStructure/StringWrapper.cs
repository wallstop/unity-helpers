namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
#if !SINGLE_THREADED
    using System.Collections.Concurrent;
#else
    using WallstopStudios.UnityHelpers.Core.Extension;
    using System.Collections.Generic;
#endif

    /// <summary>
    /// Flyweight cache that interns frequently reused strings to reduce allocations and dictionary lookups.
    /// Useful when you have a known set of keys and want reference equality semantics without hitting <see cref="string.Intern(string)"/>.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// StringWrapper key = StringWrapper.Get("Enemy/State/Alert");
    /// dictionary[key] = value;
    /// key.Dispose(); // optional – returns wrapper to cache when no longer needed
    /// ]]></code>
    /// </example>
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

        public static int Clear()
        {
            int count = Cache.Count;
            Cache.Clear();
            return count;
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

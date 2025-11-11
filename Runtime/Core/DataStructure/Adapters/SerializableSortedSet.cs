namespace WallstopStudios.UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using ProtoBuf;

    /// <summary>
    /// Sorted set with Unity/ProtoBuf/System.Text.Json serialization support.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class SerializableSortedSet<T> : SerializableSetBase<T, SortedSet<T>>
        where T : IComparable<T>
    {
        private sealed class StorageSet : SortedSet<T>
        {
            public StorageSet() { }

            public StorageSet(IEnumerable<T> collection)
                : base(collection ?? Array.Empty<T>()) { }

            public StorageSet(SerializationInfo info, StreamingContext context)
                : base(info, context) { }
        }

        public SerializableSortedSet()
            : base(new StorageSet()) { }

        public SerializableSortedSet(IEnumerable<T> collection)
            : base(new StorageSet(collection)) { }

        protected SerializableSortedSet(SerializationInfo info, StreamingContext context)
            : base(
                info,
                context,
                (serializationInfo, streamingContext) =>
                    new StorageSet(serializationInfo, streamingContext)
            ) { }

        /// <summary>
        /// Creates a new <see cref="global::System.Collections.Generic.SortedSet{T}"/> populated with this set's contents.
        /// </summary>
        /// <returns>A copy of the sorted set's current state.</returns>
        public global::System.Collections.Generic.SortedSet<T> ToSortedSet()
        {
            global::System.Collections.Generic.SortedSet<T> copy =
                new global::System.Collections.Generic.SortedSet<T>(Set, Set.Comparer);
            return copy;
        }

        public SortedSet<T>.Enumerator GetEnumerator()
        {
            return Set.GetEnumerator();
        }

        public T Min => Set.Min;

        public T Max => Set.Max;

        public IEnumerable<T> Reverse()
        {
            return Set.Reverse();
        }

        public SortedSet<T> GetViewBetween(T lowerValue, T upperValue)
        {
            return Set.GetViewBetween(lowerValue, upperValue);
        }

        protected override int RemoveWhereInternal(Predicate<T> match)
        {
            return Set.RemoveWhere(match);
        }

        protected override bool TryGetValueCore(T equalValue, out T actualValue)
        {
            return Set.TryGetValue(equalValue, out actualValue);
        }

        protected override bool SupportsSorting => true;
    }
}

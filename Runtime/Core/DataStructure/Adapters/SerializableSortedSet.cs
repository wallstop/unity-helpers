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
    {
        private sealed class StorageSet : SortedSet<T>
        {
            public StorageSet() { }

            public StorageSet(IComparer<T> comparer)
                : base(comparer) { }

            public StorageSet(IEnumerable<T> collection, IComparer<T> comparer)
                : base(collection, comparer) { }

            public StorageSet(SerializationInfo info, StreamingContext context)
                : base(info, context) { }
        }

        public SerializableSortedSet()
            : base(new StorageSet()) { }

        public SerializableSortedSet(IComparer<T> comparer)
            : base(new StorageSet(comparer ?? Comparer<T>.Default)) { }

        public SerializableSortedSet(IEnumerable<T> collection)
            : base(new StorageSet(collection ?? Array.Empty<T>(), Comparer<T>.Default)) { }

        public SerializableSortedSet(IEnumerable<T> collection, IComparer<T> comparer)
            : base(new StorageSet(collection ?? Array.Empty<T>(), comparer ?? Comparer<T>.Default))
        { }

        protected SerializableSortedSet(SerializationInfo info, StreamingContext context)
            : base(
                info,
                context,
                (serializationInfo, streamingContext) =>
                    new StorageSet(serializationInfo, streamingContext)
            ) { }

        public IComparer<T> Comparer => Set.Comparer;

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
    }
}

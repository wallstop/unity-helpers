namespace WallstopStudios.UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using ProtoBuf;

    /// <summary>
    /// Sorted set wrapper that keeps ordering intact across Unity, ProtoBuf, and JSON serialization.
    /// Ideal for deterministic gameplay systems that need sorted iteration but still want to save or inspect data.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// SerializableSortedSet<int> highscores = new SerializableSortedSet<int>(new[] { 100, 250, 420 });
    /// highscores.Add(360);
    /// foreach (int score in highscores)
    /// {
    ///     Debug.Log(score);
    /// }
    /// ]]></code>
    /// </example>
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

        /// <summary>
        /// Initializes an empty sorted set that can participate in Unity and ProtoBuf serialization.
        /// </summary>
        public SerializableSortedSet()
            : base(new StorageSet()) { }

        /// <summary>
        /// Initializes the set with the provided items.
        /// </summary>
        /// <param name="collection">Sequence whose elements are copied into the set.</param>
        public SerializableSortedSet(IEnumerable<T> collection)
            : base(new StorageSet(collection)) { }

        /// <summary>
        /// Initializes the set during custom serialization scenarios.
        /// </summary>
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
        public SortedSet<T> ToSortedSet()
        {
            SortedSet<T> copy = new(Set, Set.Comparer);
            return copy;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the set in sorted order.
        /// </summary>
        public SortedSet<T>.Enumerator GetEnumerator()
        {
            return Set.GetEnumerator();
        }

        /// <summary>
        /// Gets the smallest value stored in the set.
        /// </summary>
        public T Min => Set.Min;

        /// <summary>
        /// Gets the largest value stored in the set.
        /// </summary>
        public T Max => Set.Max;

        /// <summary>
        /// Enumerates the set in descending order without allocating a copy.
        /// </summary>
        /// <returns>Lazy enumerable that yields items from greatest to least.</returns>
        public IEnumerable<T> Reverse()
        {
            return Set.Reverse();
        }

        /// <summary>
        /// Produces a view of the set constrained between the provided lower and upper bounds.
        /// </summary>
        /// <param name="lowerValue">Inclusive lower bound.</param>
        /// <param name="upperValue">Inclusive upper bound.</param>
        /// <returns>A view that reflects mutations made to the underlying set.</returns>
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

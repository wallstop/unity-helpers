namespace WallstopStudios.UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using ProtoBuf;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;

    /// <summary>
    /// Shared infrastructure for Unity-friendly serialized sets.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public abstract class SerializableSetBase<T, TSet>
        : ISet<T>,
            ICollection<T>,
            IEnumerable<T>,
            IEnumerable,
            IReadOnlyCollection<T>,
            ISerializationCallbackReceiver,
            IDeserializationCallback,
            ISerializable
        where TSet : class, ISet<T>
    {
        [SerializeField]
        [ProtoMember(1, OverwriteList = true)]
        internal T[] _items;

        [ProtoIgnore]
        [JsonIgnore]
        private readonly TSet _set;

        [NonSerialized]
        private bool _preserveSerializedEntries;

        protected SerializableSetBase(TSet set)
        {
            if (set == null)
            {
                throw new ArgumentNullException(nameof(set));
            }

            _set = set;
        }

        protected SerializableSetBase(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext,
            Func<SerializationInfo, StreamingContext, TSet> factory
        )
        {
            if (serializationInfo == null)
            {
                throw new ArgumentNullException(nameof(serializationInfo));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _set = factory(serializationInfo, streamingContext);
        }

        protected TSet Set => _set;

        /// <summary>
        /// Unity inspector helper for identifying serialized array property names.
        /// </summary>
        internal static class SerializedPropertyNames
        {
            private sealed class NameHolder : SerializableHashSet<T>
            {
                public static readonly string ItemsName = nameof(_items);
            }

            internal static readonly string ItemsName = NameHolder.ItemsName;
        }

        public int Count => _set.Count;

        bool ICollection<T>.IsReadOnly => ((ICollection<T>)_set).IsReadOnly;

        public bool Add(T item)
        {
            bool added = _set.Add(item);
            if (added)
            {
                MarkSerializationCacheDirty();
            }

            return added;
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            _set.UnionWith(other);
            MarkSerializationCacheDirty();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            _set.IntersectWith(other);
            MarkSerializationCacheDirty();
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            _set.ExceptWith(other);
            MarkSerializationCacheDirty();
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            _set.SymmetricExceptWith(other);
            MarkSerializationCacheDirty();
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return _set.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return _set.IsSupersetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return _set.IsProperSupersetOf(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return _set.IsProperSubsetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return _set.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return _set.SetEquals(other);
        }

        public void Clear()
        {
            if (_set.Count == 0)
            {
                return;
            }

            _set.Clear();
            MarkSerializationCacheDirty();
        }

        public bool Contains(T item)
        {
            return _set.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _set.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            bool removed = _set.Remove(item);
            if (removed)
            {
                MarkSerializationCacheDirty();
            }

            return removed;
        }

        public int RemoveWhere(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            int removed = RemoveWhereInternal(match);
            if (removed > 0)
            {
                MarkSerializationCacheDirty();
            }

            return removed;
        }

        protected virtual int RemoveWhereInternal(Predicate<T> match)
        {
            List<T> buffer = new List<T>();
            foreach (T value in _set)
            {
                if (match(value))
                {
                    buffer.Add(value);
                }
            }

            foreach (T value in buffer)
            {
                _set.Remove(value);
            }

            return buffer.Count;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_set.GetEnumerator());
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_set).GetEnumerator();
        }

        public void OnBeforeSerialize()
        {
            if (_preserveSerializedEntries && _items != null)
            {
                return;
            }

            int count = _set.Count;
            _items = new T[count];
            _set.CopyTo(_items, 0);
            _preserveSerializedEntries = false;
        }

        public void OnAfterDeserialize()
        {
            if (_items == null)
            {
                _preserveSerializedEntries = false;
                _set.Clear();
                return;
            }

            _set.Clear();
            bool hasDuplicates = false;
            for (int index = 0; index < _items.Length; index++)
            {
                T value = _items[index];
                if (!_set.Add(value) && !hasDuplicates)
                {
                    hasDuplicates = true;
                }
            }

            _preserveSerializedEntries = hasDuplicates;

            if (!hasDuplicates)
            {
                _items = null;
            }
        }

        [ProtoBeforeSerialization]
        private void OnProtoBeforeSerialization()
        {
            OnBeforeSerialize();
        }

        [ProtoAfterSerialization]
        private void OnProtoAfterSerialization()
        {
            if (_preserveSerializedEntries)
            {
                return;
            }

            _items = null;
        }

        [ProtoAfterDeserialization]
        private void OnProtoAfterDeserialization()
        {
            OnAfterDeserialize();
        }

        public void OnDeserialization(object sender)
        {
            if (_set is IDeserializationCallback callback)
            {
                callback.OnDeserialization(sender);
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (_set is ISerializable serializable)
            {
                serializable.GetObjectData(info, context);
            }
        }

        protected void MarkSerializationCacheDirty()
        {
            _preserveSerializedEntries = false;
            _items = null;
        }

        public override string ToString()
        {
            return this.ToJson();
        }

        public struct Enumerator : IEnumerator<T>
        {
            private IEnumerator<T> _enumerator;

            internal Enumerator(IEnumerator<T> enumerator)
            {
                _enumerator = enumerator;
            }

            public T Current => _enumerator.Current;

            object IEnumerator.Current => _enumerator.Current;

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException("Reset is not supported.");
            }
        }
    }

    /// <summary>
    /// Hash set that can be serialized by Unity, ProtoBuf, and System.Text.Json.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class SerializableHashSet<T> : SerializableSetBase<T, HashSet<T>>
    {
        private sealed class StorageSet : HashSet<T>
        {
            public StorageSet() { }

            public StorageSet(IEqualityComparer<T> comparer)
                : base(comparer) { }

            public StorageSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
                : base(collection, comparer) { }

            public StorageSet(SerializationInfo info, StreamingContext context)
                : base(info, context) { }
        }

        public SerializableHashSet()
            : base(new StorageSet()) { }

        public SerializableHashSet(IEqualityComparer<T> comparer)
            : base(new StorageSet(comparer ?? EqualityComparer<T>.Default)) { }

        public SerializableHashSet(IEnumerable<T> collection)
            : base(new StorageSet(collection ?? Array.Empty<T>(), EqualityComparer<T>.Default)) { }

        public SerializableHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
            : base(
                new StorageSet(
                    collection ?? Array.Empty<T>(),
                    comparer ?? EqualityComparer<T>.Default
                )
            ) { }

        protected SerializableHashSet(SerializationInfo info, StreamingContext context)
            : base(
                info,
                context,
                (serializationInfo, streamingContext) =>
                    new StorageSet(serializationInfo, streamingContext)
            ) { }

        public IEqualityComparer<T> Comparer => Set.Comparer;

        protected override int RemoveWhereInternal(Predicate<T> match)
        {
            return Set.RemoveWhere(match);
        }
    }

    internal static class SerializableHashSetSerializedPropertyNames
    {
        internal static readonly string Items = SerializableHashSet<int>
            .SerializedPropertyNames
            .ItemsName;
    }
}

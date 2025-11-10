namespace WallstopStudios.UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using ProtoBuf;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    internal interface ISerializableSetInspector
    {
        Type ElementType { get; }

        int UniqueCount { get; }

        int SerializedCount { get; }

        bool TryAddElement(object value, out object normalizedValue);

        bool ContainsElement(object value);

        bool RemoveElement(object value);

        void ClearElements();

        Array GetSerializedItemsSnapshot();

        void SetSerializedItemsSnapshot(Array values, bool preserveSerializedEntries);

        void SynchronizeSerializedState();
    }

    internal interface ISerializableSetEditorSync
    {
        void EditorAfterDeserialize();
    }

    /// <summary>
    /// Shared infrastructure for Unity-friendly serialized sets.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public abstract class SerializableSetBase<T, TSet>
        : ISet<T>,
            IReadOnlyCollection<T>,
            ISerializationCallbackReceiver,
            IDeserializationCallback,
            ISerializable,
            ISerializableSetInspector,
            ISerializableSetEditorSync
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

        internal bool PreserveSerializedEntries => _preserveSerializedEntries;

        protected SerializableSetBase(TSet set)
        {
            _set = set ?? throw new ArgumentNullException(nameof(set));
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
                public const string ItemsName = nameof(_items);
            }

            internal const string ItemsName = NameHolder.ItemsName;
        }

        public int Count => _set.Count;

        bool ICollection<T>.IsReadOnly => _set.IsReadOnly;

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

        public bool TryGetValue(T equalValue, out T actualValue)
        {
            if (TryGetValueCore(equalValue, out T resolved))
            {
                actualValue = resolved;
                return true;
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            foreach (T value in _set)
            {
                if (comparer.Equals(value, equalValue))
                {
                    actualValue = value;
                    return true;
                }
            }

            actualValue = default;
            return false;
        }

        protected virtual bool TryGetValueCore(T equalValue, out T actualValue)
        {
            actualValue = default;
            return false;
        }

        public void CopyTo(T[] array)
        {
            CopyTo(array, 0);
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
            List<T> buffer = new();
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
            OnAfterDeserializeInternal(suppressWarnings: false);
        }

        void ISerializableSetEditorSync.EditorAfterDeserialize()
        {
            OnAfterDeserializeInternal(suppressWarnings: true);
        }

        private void OnAfterDeserializeInternal(bool suppressWarnings)
        {
            if (_items == null)
            {
                _preserveSerializedEntries = false;
                _set.Clear();
                return;
            }

            bool preserveExistingOrder = _preserveSerializedEntries;
            _set.Clear();
            bool hasDuplicates = false;
            bool encounteredNullReference = false;
            bool supportsNullCheck = TypeSupportsNullReferences(typeof(T));
            for (int index = 0; index < _items.Length; index++)
            {
                T value = _items[index];
                if (supportsNullCheck && ReferenceEquals(value, null))
                {
                    encounteredNullReference = true;
                    if (!suppressWarnings)
                    {
                        LogNullEntrySkip(index);
                    }
                    continue;
                }

                if (!_set.Add(value) && !hasDuplicates)
                {
                    hasDuplicates = true;
                }
            }

            _preserveSerializedEntries =
                preserveExistingOrder || hasDuplicates || encounteredNullReference;

            if (!_preserveSerializedEntries)
            {
                _items = null;
            }
        }

        private static bool TypeSupportsNullReferences(Type type)
        {
            return type != null
                && (!type.IsValueType || typeof(UnityEngine.Object).IsAssignableFrom(type));
        }

        private static void LogNullEntrySkip(int index)
        {
#if UNITY_EDITOR
            if (!EditorShouldLog())
            {
                return;
            }
#endif
            Debug.LogError(
                $"SerializableSet<{typeof(T).FullName}> skipped serialized entry at index {index} because the value reference was null."
            );
        }

#if UNITY_EDITOR
        private static bool EditorShouldLog()
        {
            try
            {
                return EditorApplication.isPlayingOrWillChangePlaymode;
            }
            catch (UnityException)
            {
                return false;
            }
        }
#endif

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

        Type ISerializableSetInspector.ElementType => typeof(T);

        int ISerializableSetInspector.UniqueCount => _set.Count;

        int ISerializableSetInspector.SerializedCount => _items?.Length ?? _set.Count;

        bool ISerializableSetInspector.TryAddElement(object value, out object normalizedValue)
        {
            if (!TryConvertToElement(value, out T converted))
            {
                normalizedValue = default(T);
                return false;
            }

            bool added = _set.Add(converted);
            if (added)
            {
                MarkSerializationCacheDirty();
            }

            normalizedValue = converted;
            return added;
        }

        bool ISerializableSetInspector.ContainsElement(object value)
        {
            if (!TryConvertToElement(value, out T converted))
            {
                return false;
            }

            return _set.Contains(converted);
        }

        bool ISerializableSetInspector.RemoveElement(object value)
        {
            if (!TryConvertToElement(value, out T converted))
            {
                return false;
            }

            bool removed = _set.Remove(converted);
            if (removed)
            {
                MarkSerializationCacheDirty();
            }

            return removed;
        }

        void ISerializableSetInspector.ClearElements()
        {
            if (_set.Count == 0 && (_items == null || _items.Length == 0))
            {
                return;
            }

            _set.Clear();
            _items = null;
            _preserveSerializedEntries = false;
        }

        Array ISerializableSetInspector.GetSerializedItemsSnapshot()
        {
            if (_items != null && _items.Length > 0)
            {
                return (T[])_items.Clone();
            }

            if (_set.Count == 0)
            {
                return Array.Empty<T>();
            }

            T[] snapshot = new T[_set.Count];
            _set.CopyTo(snapshot, 0);
            return snapshot;
        }

        void ISerializableSetInspector.SetSerializedItemsSnapshot(
            Array values,
            bool preserveSerializedEntries
        )
        {
            if (values == null || values.Length == 0)
            {
                _items = null;
                _preserveSerializedEntries = false;
                _set.Clear();
                return;
            }

            int length = values.Length;
            T[] convertedItems = new T[length];
            for (int index = 0; index < length; index++)
            {
                object raw = values.GetValue(index);
                if (!TryConvertToElement(raw, out T converted))
                {
                    converted = default;
                }

                convertedItems[index] = converted;
            }

            _items = convertedItems;
            _preserveSerializedEntries = preserveSerializedEntries;

            _set.Clear();
            for (int index = 0; index < convertedItems.Length; index++)
            {
                _set.Add(convertedItems[index]);
            }
        }

        void ISerializableSetInspector.SynchronizeSerializedState()
        {
            OnBeforeSerialize();
        }

        private bool TryConvertToElement(object value, out T result)
        {
            if (value is T typedValue)
            {
                result = typedValue;
                return true;
            }

            if (value == null)
            {
                if (default(T) == null)
                {
                    result = default;
                    return true;
                }

                result = default;
                return false;
            }

            Type elementType = typeof(T);

            if (elementType.IsInstanceOfType(value))
            {
                result = (T)value;
                return true;
            }

            try
            {
                if (elementType.IsEnum)
                {
                    if (value is string enumName)
                    {
                        result = (T)Enum.Parse(elementType, enumName);
                        return true;
                    }

                    object enumValue = Enum.ToObject(elementType, value);
                    result = (T)enumValue;
                    return true;
                }

                if (value is IConvertible)
                {
                    object converted = Convert.ChangeType(
                        value,
                        elementType,
                        CultureInfo.InvariantCulture
                    );
                    result = (T)converted;
                    return true;
                }
            }
            catch
            {
                // Fallback handled below.
            }

            result = default;
            return false;
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

        public HashSet<T>.Enumerator GetEnumerator()
        {
            return Set.GetEnumerator();
        }

        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            Set.CopyTo(array, arrayIndex, count);
        }

        public void TrimExcess()
        {
            Set.TrimExcess();
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

    internal static class SerializableHashSetSerializedPropertyNames
    {
        internal static readonly string Items = SerializableHashSet<int>
            .SerializedPropertyNames
            .ItemsName;
    }
}

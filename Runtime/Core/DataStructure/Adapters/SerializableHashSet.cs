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
    using WallstopStudios.UnityHelpers.Utils;
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

        bool SupportsSorting { get; }
    }

    internal interface ISerializableSetEditorSync
    {
        void EditorAfterDeserialize();
    }

    /// <summary>
    /// Shared infrastructure for Unity-friendly serialized sets.
    /// Synchronizes the serialized element array with a backing <see cref="ISet{T}"/> so Unity, ProtoBuf, and JSON stay in step with runtime mutations.
    /// Extend this class to build custom set types with specialized equality logic or editor behavior.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// [Serializable]
    /// public sealed class CaseInsensitiveTagSet : SerializableSetBase<string, HashSet<string>>
    /// {
    ///     public CaseInsensitiveTagSet()
    ///         : base(new HashSet<string>(StringComparer.OrdinalIgnoreCase))
    ///     {
    ///     }
    ///
    ///     protected override bool TryGetValueCore(string equalValue, out string actualValue)
    ///     {
    ///         foreach (string value in Set)
    ///         {
    ///             if (string.Equals(value, equalValue, StringComparison.OrdinalIgnoreCase))
    ///             {
    ///                 actualValue = value;
    ///                 return true;
    ///             }
    ///         }
    ///
    ///         actualValue = equalValue;
    ///         return false;
    ///     }
    /// }
    /// ]]></code>
    /// </example>
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

        internal T[] SerializedItems => _items;

        protected virtual bool SupportsSorting => false;

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

            internal const string ItemsNameInternal = NameHolder.ItemsName;
        }

        public int Count => _set.Count;

        bool ICollection<T>.IsReadOnly => _set.IsReadOnly;

        /// <summary>
        /// Adds an element to the set and updates the serialized cache when the value was not already present.
        /// </summary>
        /// <param name="item">The element to insert.</param>
        /// <returns><c>true</c> when the value was added.</returns>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> abilities = new SerializableHashSet<string>();
        /// bool added = abilities.Add("Dash");
        /// ]]></code>
        /// </example>
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

        /// <summary>
        /// Adds all values from the provided sequence to the set.
        /// </summary>
        /// <param name="other">Values to union into this set.</param>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> abilities = new SerializableHashSet<string>();
        /// string[] unlocks = new string[] { "Dash", "Grapple" };
        /// abilities.UnionWith(unlocks);
        /// ]]></code>
        /// </example>
        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            _set.UnionWith(other);
            MarkSerializationCacheDirty();
        }

        /// <summary>
        /// Removes any element that is not contained in the provided sequence.
        /// </summary>
        /// <param name="other">Sequence that defines the intersection.</param>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> abilities = new SerializableHashSet<string>();
        /// string[] allowed = new string[] { "Dash" };
        /// abilities.IntersectWith(allowed);
        /// ]]></code>
        /// </example>
        public void IntersectWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            _set.IntersectWith(other);
            MarkSerializationCacheDirty();
        }

        /// <summary>
        /// Removes all elements that appear in the provided sequence.
        /// </summary>
        /// <param name="other">Sequence whose members should be removed.</param>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> abilities = new SerializableHashSet<string>();
        /// string[] deprecated = new string[] { "Dash" };
        /// abilities.ExceptWith(deprecated);
        /// ]]></code>
        /// </example>
        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            _set.ExceptWith(other);
            MarkSerializationCacheDirty();
        }

        /// <summary>
        /// Modifies the set so it contains elements that appear in exactly one of the sequences.
        /// </summary>
        /// <param name="other">Sequence whose elements are compared against the current set.</param>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> first = new SerializableHashSet<string>();
        /// first.Add("Dash");
        /// string[] second = new string[] { "Dash", "Grapple" };
        /// first.SymmetricExceptWith(second);
        /// ]]></code>
        /// </example>
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            _set.SymmetricExceptWith(other);
            MarkSerializationCacheDirty();
        }

        /// <summary>
        /// Determines whether the set is a subset of the provided sequence.
        /// </summary>
        /// <param name="other">Sequence to compare against.</param>
        /// <returns><c>true</c> when every element exists in the other sequence.</returns>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> storyUnlocks = new SerializableHashSet<string>();
        /// bool subset = storyUnlocks.IsSubsetOf(new string[] { "Dash", "DoubleJump" });
        /// ]]></code>
        /// </example>
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return _set.IsSubsetOf(other);
        }

        /// <summary>
        /// Determines whether the set contains all values found in the provided sequence.
        /// </summary>
        /// <param name="other">Sequence that must be contained in the set.</param>
        /// <returns><c>true</c> when the set is a superset.</returns>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> storyUnlocks = new SerializableHashSet<string>();
        /// bool superset = storyUnlocks.IsSupersetOf(new string[] { "Dash" });
        /// ]]></code>
        /// </example>
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return _set.IsSupersetOf(other);
        }

        /// <summary>
        /// Determines whether the set strictly contains all values from the other sequence and has additional elements.
        /// </summary>
        /// <param name="other">Sequence that must be contained in the set.</param>
        /// <returns><c>true</c> when the set is a proper superset.</returns>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> storyUnlocks = new SerializableHashSet<string>();
        /// bool properSuperset = storyUnlocks.IsProperSupersetOf(new string[] { "Dash" });
        /// ]]></code>
        /// </example>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return _set.IsProperSupersetOf(other);
        }

        /// <summary>
        /// Determines whether the set is strictly contained inside the provided sequence.
        /// </summary>
        /// <param name="other">Sequence that must contain every element plus at least one additional element.</param>
        /// <returns><c>true</c> when the set is a proper subset.</returns>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> storyUnlocks = new SerializableHashSet<string>();
        /// bool properSubset = storyUnlocks.IsProperSubsetOf(new string[] { "Dash", "Grapple" });
        /// ]]></code>
        /// </example>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return _set.IsProperSubsetOf(other);
        }

        /// <summary>
        /// Determines whether the set shares any element with the provided sequence.
        /// </summary>
        /// <param name="other">Sequence to compare to.</param>
        /// <returns><c>true</c> when at least one value is shared.</returns>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> storyUnlocks = new SerializableHashSet<string>();
        /// bool overlaps = storyUnlocks.Overlaps(new string[] { "Dash" });
        /// ]]></code>
        /// </example>
        public bool Overlaps(IEnumerable<T> other)
        {
            return _set.Overlaps(other);
        }

        /// <summary>
        /// Determines whether this set and the provided sequence contain the exact same elements.
        /// </summary>
        /// <param name="other">Sequence to compare against.</param>
        /// <returns><c>true</c> when both contain identical members.</returns>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> storyUnlocks = new SerializableHashSet<string>();
        /// bool matches = storyUnlocks.SetEquals(new string[] { "Dash", "DoubleJump" });
        /// ]]></code>
        /// </example>
        public bool SetEquals(IEnumerable<T> other)
        {
            return _set.SetEquals(other);
        }

        /// <summary>
        /// Removes every element from the set and clears the serialized cache.
        /// </summary>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> storyUnlocks = new SerializableHashSet<string>();
        /// storyUnlocks.Clear();
        /// ]]></code>
        /// </example>
        public void Clear()
        {
            if (_set.Count == 0)
            {
                return;
            }

            _set.Clear();
            MarkSerializationCacheDirty();
        }

        /// <summary>
        /// Determines whether the set contains the specified element.
        /// </summary>
        /// <param name="item">The element to look up.</param>
        /// <returns><c>true</c> when the element exists.</returns>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> storyUnlocks = new SerializableHashSet<string>();
        /// bool hasDash = storyUnlocks.Contains("Dash");
        /// ]]></code>
        /// </example>
        public bool Contains(T item)
        {
            return _set.Contains(item);
        }

        /// <summary>
        /// Retrieves the stored value that compares equal to the supplied value.
        /// </summary>
        /// <param name="equalValue">The candidate value.</param>
        /// <param name="actualValue">Receives the canonical value from the set.</param>
        /// <returns><c>true</c> when a matching element is found.</returns>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> storyUnlocks = new SerializableHashSet<string>();
        /// string normalized;
        /// bool found = storyUnlocks.TryGetValue("Dash", out normalized);
        /// ]]></code>
        /// </example>
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

        /// <summary>
        /// Allows derived types to substitute custom lookup behavior (for example, when values are wrapped).
        /// </summary>
        /// <param name="equalValue">The candidate value.</param>
        /// <param name="actualValue">Receives the resolved value.</param>
        /// <returns><c>true</c> when the derived type resolved a match.</returns>
        /// <example>
        /// <code><![CDATA[
        /// protected override bool TryGetValueCore(string equalValue, out string actualValue)
        /// {
        ///     foreach (string stored in SerializedItems)
        ///     {
        ///         if (string.Equals(stored, equalValue, StringComparison.OrdinalIgnoreCase))
        ///         {
        ///             actualValue = stored;
        ///             return true;
        ///         }
        ///     }
        ///     actualValue = null;
        ///     return false;
        /// }
        /// ]]></code>
        /// </example>
        protected virtual bool TryGetValueCore(T equalValue, out T actualValue)
        {
            actualValue = default;
            return false;
        }

        /// <summary>
        /// Copies the elements into the provided array starting at index zero.
        /// </summary>
        /// <param name="array">Destination array.</param>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> storyUnlocks = new SerializableHashSet<string>();
        /// string[] snapshot = new string[storyUnlocks.Count];
        /// storyUnlocks.CopyTo(snapshot);
        /// ]]></code>
        /// </example>
        public void CopyTo(T[] array)
        {
            CopyTo(array, 0);
        }

        /// <summary>
        /// Copies the elements into the provided array starting at the given index.
        /// </summary>
        /// <param name="array">Destination array.</param>
        /// <param name="arrayIndex">Index in <paramref name="array"/> where copying begins.</param>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> storyUnlocks = new SerializableHashSet<string>();
        /// string[] snapshot = new string[storyUnlocks.Count + 2];
        /// storyUnlocks.CopyTo(snapshot, 1);
        /// ]]></code>
        /// </example>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _set.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes a single element from the set and updates the serialized cache.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns><c>true</c> when the value existed.</returns>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> storyUnlocks = new SerializableHashSet<string>();
        /// bool removed = storyUnlocks.Remove("Dash");
        /// ]]></code>
        /// </example>
        public bool Remove(T item)
        {
            bool removed = _set.Remove(item);
            if (removed)
            {
                MarkSerializationCacheDirty();
            }

            return removed;
        }

        /// <summary>
        /// Removes every element that satisfies the provided predicate.
        /// </summary>
        /// <param name="match">Condition that determines which elements are removed.</param>
        /// <returns>The number of elements removed.</returns>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> storyUnlocks = new SerializableHashSet<string>();
        /// int removed = storyUnlocks.RemoveWhere(id => id.Contains("Beta"));
        /// ]]></code>
        /// </example>
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

        /// <summary>
        /// Allows derived types to customize how batch removals are handled.
        /// </summary>
        /// <param name="match">The predicate describing which elements to remove.</param>
        /// <returns>The number of removed elements.</returns>
        protected virtual int RemoveWhereInternal(Predicate<T> match)
        {
            using PooledResource<List<T>> bufferResource = Buffers<T>.List.Get(out List<T> buffer);
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

        /// <inheritdoc />
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_set).GetEnumerator();
        }

        /// <summary>
        /// Copies the live set contents into the serialized backing array before Unity or ProtoBuf serialization.
        /// </summary>
        /// <example>
        /// <code>
        /// hashSet.OnBeforeSerialize();
        /// var snapshot = hashSet.SerializedItems;
        /// </code>
        /// </example>
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

        /// <summary>
        /// Reconstructs the live set from the serialized array after Unity or ProtoBuf deserialization.
        /// </summary>
        /// <example>
        /// <code>
        /// hashSet.OnAfterDeserialize();
        /// bool contains = hashSet.Contains(item);
        /// </code>
        /// </example>
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

        /// <summary>
        /// Completes deserialization after <see cref="SerializationInfo"/> data has been applied.
        /// </summary>
        /// <param name="sender">Reserved for future use.</param>
        public void OnDeserialization(object sender)
        {
            if (_set is IDeserializationCallback callback)
            {
                callback.OnDeserialization(sender);
            }
        }

        /// <summary>
        /// Writes the serialized representation of the set into a <see cref="SerializationInfo"/> instance.
        /// </summary>
        /// <param name="info">The serialization store to populate.</param>
        /// <param name="context">Context for the serialization process.</param>
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

        /// <summary>
        /// Returns a JSON string describing the serialized items for quick debugging.
        /// </summary>
        /// <returns>A JSON representation of the set.</returns>
        /// <example>
        /// <code><![CDATA[
        /// SerializableHashSet<string> storyUnlocks = new SerializableHashSet<string>();
        /// string snapshot = storyUnlocks.ToString();
        /// ]]></code>
        /// </example>
        public override string ToString()
        {
            return this.ToJson();
        }

        Type ISerializableSetInspector.ElementType => typeof(T);

        int ISerializableSetInspector.UniqueCount => _set.Count;

        int ISerializableSetInspector.SerializedCount => _items?.Length ?? _set.Count;

        bool ISerializableSetInspector.SupportsSorting => SupportsSorting;

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
            if (_items is { Length: > 0 })
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
            foreach (T convertedItem in convertedItems)
            {
                _set.Add(convertedItem);
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
    /// Unity-serializable hash set that keeps elements deduplicated while remaining compatible with ProtoBuf and System.Text.Json.
    /// Perfect for authoring unlock lists, feature flags, and other boolean membership data in the inspector.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// [Serializable]
    /// public sealed class UnlockState : MonoBehaviour
    /// {
    ///     [SerializeField]
    ///     private SerializableHashSet<string> unlockedLevels = new SerializableHashSet<string>();
    ///
    ///     public bool HasUnlocked(string levelId)
    ///     {
    ///         return unlockedLevels.Contains(levelId);
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    [Serializable]
    [ProtoContract]
    public class SerializableHashSet<T> : SerializableSetBase<T, HashSet<T>>
    {
        private sealed class StorageSet : HashSet<T>
        {
            /// <summary>
            /// Initializes an empty storage set using the default comparer.
            /// </summary>
            public StorageSet() { }

            /// <summary>
            /// Initializes an empty storage set that uses the provided comparer.
            /// </summary>
            /// <param name="comparer">Comparer passed to <see cref="HashSet{T}.HashSet(IEqualityComparer{T})"/>.</param>
            public StorageSet(IEqualityComparer<T> comparer)
                : base(comparer) { }

            /// <summary>
            /// Initializes the storage set with the supplied elements and comparer.
            /// </summary>
            /// <param name="collection">Elements to copy into the backing set.</param>
            /// <param name="comparer">Comparer used to determine uniqueness.</param>
            public StorageSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
                : base(collection, comparer) { }

            /// <summary>
            /// Deserialization constructor used by <see cref="ISerializable"/>.
            /// </summary>
            /// <param name="info">Serialized data describing the set.</param>
            /// <param name="context">Context describing the serialization source.</param>
            public StorageSet(SerializationInfo info, StreamingContext context)
                : base(info, context) { }
        }

        /// <summary>
        /// Initializes an empty hash set compatible with Unity and ProtoBuf serialization.
        /// </summary>
        public SerializableHashSet()
            : base(new StorageSet()) { }

        /// <summary>
        /// Initializes an empty hash set using the supplied equality comparer.
        /// </summary>
        /// <param name="comparer">Comparer used to evaluate set membership. Defaults to <see cref="EqualityComparer{T}.Default"/> when <c>null</c>.</param>
        public SerializableHashSet(IEqualityComparer<T> comparer)
            : base(new StorageSet(comparer ?? EqualityComparer<T>.Default)) { }

        /// <summary>
        /// Initializes the set with elements copied from the provided collection.
        /// </summary>
        /// <param name="collection">Sequence whose elements are added to the set. <see cref="Array.Empty{T}"/> is used when <c>null</c>.</param>
        public SerializableHashSet(IEnumerable<T> collection)
            : base(new StorageSet(collection ?? Array.Empty<T>(), EqualityComparer<T>.Default)) { }

        /// <summary>
        /// Initializes the set with elements copied from the provided collection and comparer.
        /// </summary>
        /// <param name="collection">Sequence whose elements are added to the set. <see cref="Array.Empty{T}"/> is used when <c>null</c>.</param>
        /// <param name="comparer">Comparer used to evaluate set membership. Defaults to <see cref="EqualityComparer{T}.Default"/> when <c>null</c>.</param>
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

        /// <summary>
        /// Gets the equality comparer used by the underlying hash set.
        /// </summary>
        public IEqualityComparer<T> Comparer => Set.Comparer;

        /// <summary>
        /// Creates a new <see cref="global::System.Collections.Generic.HashSet{T}"/> populated with this set's contents.
        /// </summary>
        /// <returns>A copy of the hash set's current state.</returns>
        public HashSet<T> ToHashSet()
        {
            HashSet<T> copy = new(Set, Set.Comparer);
            return copy;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the set.
        /// </summary>
        public HashSet<T>.Enumerator GetEnumerator()
        {
            return Set.GetEnumerator();
        }

        /// <summary>
        /// Copies a subset of elements into the provided array starting at the specified index.
        /// </summary>
        /// <param name="array">Destination array that receives items.</param>
        /// <param name="arrayIndex">Zero-based index indicating where copying starts.</param>
        /// <param name="count">Number of elements to copy.</param>
        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            Set.CopyTo(array, arrayIndex, count);
        }

        /// <summary>
        /// Resizes the underlying hash set to remove unused capacity.
        /// </summary>
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
            .ItemsNameInternal;
    }
}

namespace WallstopStudios.UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using ProtoBuf;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    /// <summary>
    /// Sorted dictionary wrapper that keeps key ordering intact across Unity, JSON, and ProtoBuf serialization.
    /// Ideal when deterministic iteration order matters for gameplay logic or editor tooling.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// SerializableSortedDictionary<int, string> loot = new SerializableSortedDictionary<int, string>();
    /// loot.Add(50, "Gold");
    /// loot.Add(100, "Potion");
    /// foreach (KeyValuePair<int, string> entry in loot)
    /// {
    ///     Debug.Log($"{entry.Key} -> {entry.Value}");
    /// }
    /// ]]></code>
    /// </example>
    [Serializable]
    [ProtoContract]
    public abstract class SerializableSortedDictionaryBase<TKey, TValue, TValueCache>
        : IDictionary<TKey, TValue>,
            IDictionary,
            IReadOnlyDictionary<TKey, TValue>,
            ISerializationCallbackReceiver,
            IDeserializationCallback,
            ISerializable
        where TKey : IComparable<TKey>
    {
        private const string KeysSerializationName = "Keys";
        private const string ValuesSerializationName = "Values";

        [SerializeField]
        [ProtoMember(1, OverwriteList = true)]
        internal TKey[] _keys;

        [SerializeField]
        [ProtoMember(2, OverwriteList = true)]
        internal TValueCache[] _values;

        [ProtoIgnore]
        [JsonIgnore]
        private SortedDictionary<TKey, TValue> _dictionary;

        [NonSerialized]
        private bool _preserveSerializedEntries;

        [NonSerialized]
        private bool _arraysDirty = true;

        internal bool PreserveSerializedEntries => _preserveSerializedEntries;

        internal TKey[] SerializedKeys => _keys;

        internal TValueCache[] SerializedValues => _values;

        internal bool SerializationArraysDirty => _arraysDirty;

        protected SerializableSortedDictionaryBase()
        {
            _dictionary = new SortedDictionary<TKey, TValue>();
        }

        protected SerializableSortedDictionaryBase(IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            _dictionary = new SortedDictionary<TKey, TValue>();
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                _dictionary[pair.Key] = pair.Value;
            }
        }

        protected SerializableSortedDictionaryBase(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            _dictionary = new SortedDictionary<TKey, TValue>();

            _keys = (TKey[])info.GetValue(KeysSerializationName, typeof(TKey[]));
            _values = (TValueCache[])info.GetValue(ValuesSerializationName, typeof(TValueCache[]));
            OnAfterDeserialize();
        }

        protected abstract TValue GetValue(TValueCache[] cache, int index);

        protected abstract void SetValue(TValueCache[] cache, int index, TValue value);

        public int Count => _dictionary.Count;

        public bool IsReadOnly => ((IDictionary<TKey, TValue>)_dictionary).IsReadOnly;

        public ICollection<TKey> Keys => _dictionary.Keys;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _dictionary.Keys;

        public ICollection<TValue> Values => _dictionary.Values;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _dictionary.Values;

        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set
            {
                _dictionary[key] = value;
                MarkSerializationCacheDirty();
            }
        }

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
            MarkSerializationCacheDirty();
        }

        public bool TryAdd(TKey key, TValue value)
        {
            bool added = _dictionary.TryAdd(key, value);
            if (added)
            {
                MarkSerializationCacheDirty();
            }

            return added;
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            bool removed = _dictionary.Remove(key);
            if (removed)
            {
                MarkSerializationCacheDirty();
            }

            return removed;
        }

        public bool Remove(TKey key, out TValue value)
        {
            bool removed = _dictionary.Remove(key, out value);
            if (removed)
            {
                MarkSerializationCacheDirty();
            }

            return removed;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public void Clear()
        {
            _dictionary.Clear();
            MarkSerializationCacheDirty();
        }

        public void CopyFrom(IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            _dictionary.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                _dictionary[pair.Key] = pair.Value;
            }

            MarkSerializationCacheDirty();
        }

        /// <summary>
        /// Creates a new <see cref="global::System.Collections.Generic.SortedDictionary{TKey, TValue}"/> populated with this dictionary's contents.
        /// </summary>
        /// <returns>A copy of the sorted dictionary's current state.</returns>
        public SortedDictionary<TKey, TValue> ToSortedDictionary()
        {
            SortedDictionary<TKey, TValue> copy = new(_dictionary, _dictionary.Comparer);
            return copy;
        }

        /// <summary>
        /// Flushes the in-memory dictionary into the serialized key/value arrays prior to Unity or ProtoBuf serialization.
        /// </summary>
        /// <example>
        /// <code>
        /// sortedDictionary.OnBeforeSerialize();
        /// var keys = sortedDictionary.SerializedKeys;
        /// </code>
        /// </example>
        public void OnBeforeSerialize()
        {
            bool arraysIntact = _keys != null && _values != null && _keys.Length == _values.Length;

            if ((_preserveSerializedEntries || !_arraysDirty) && arraysIntact)
            {
                return;
            }

            int count = _dictionary.Count;
            _keys = new TKey[count];
            _values = new TValueCache[count];

            int index = 0;
            foreach (KeyValuePair<TKey, TValue> pair in _dictionary)
            {
                _keys[index] = pair.Key;
                SetValue(_values, index, pair.Value);
                index++;
            }

            _preserveSerializedEntries = false;
            _arraysDirty = false;
        }

        /// <summary>
        /// Rehydrates the sorted dictionary from the serialized key/value arrays after Unity or ProtoBuf deserialization.
        /// </summary>
        /// <example>
        /// <code>
        /// sortedDictionary.OnAfterDeserialize();
        /// TValue value = sortedDictionary[key];
        /// </code>
        /// </example>
        public void OnAfterDeserialize()
        {
            bool keysAndValuesPresent =
                _keys != null && _values != null && _keys.Length == _values.Length;

            _preserveSerializedEntries = false;
            _arraysDirty = false;

            if (!keysAndValuesPresent)
            {
                _keys = null;
                _values = null;
                _arraysDirty = true;
                return;
            }

            _dictionary.Clear();
            bool hasDuplicateKeys = false;
            bool encounteredNullReference = false;
            bool keySupportsNullCheck = TypeSupportsNullReferences(typeof(TKey));
            int length = _keys.Length;

            for (int index = 0; index < length; index++)
            {
                TKey key = _keys[index];
                TValue value = GetValue(_values, index);

                if (keySupportsNullCheck && ReferenceEquals(key, null))
                {
                    encounteredNullReference = true;
                    LogNullReferenceSkip("key", index);
                    continue;
                }

                if (!hasDuplicateKeys && _dictionary.ContainsKey(key))
                {
                    hasDuplicateKeys = true;
                }

                _dictionary[key] = value;
            }

            _preserveSerializedEntries = hasDuplicateKeys || encounteredNullReference;
            if (!_preserveSerializedEntries)
            {
                _keys = null;
                _values = null;
            }
            else
            {
                _arraysDirty = false;
            }
        }

        private static bool TypeSupportsNullReferences(Type type)
        {
            return type != null
                && (!type.IsValueType || typeof(UnityEngine.Object).IsAssignableFrom(type));
        }

        private static void LogNullReferenceSkip(string component, int index)
        {
#if UNITY_EDITOR
            if (!EditorShouldLog())
            {
                return;
            }
#endif
            Debug.LogError(
                $"SerializableSortedDictionary<{typeof(TKey).FullName}, {typeof(TValue).FullName}> skipped serialized entry at index {index} because the {component} reference was null."
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

            _keys = null;
            _values = null;
        }

        [ProtoAfterDeserialization]
        private void OnProtoAfterDeserialization()
        {
            OnAfterDeserialize();
        }

        /// <summary>
        /// Writes the serialized representation of the dictionary into a <see cref="SerializationInfo"/> instance.
        /// </summary>
        /// <param name="info">The serialization store to populate.</param>
        /// <param name="context">Context for the serialization process.</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            OnBeforeSerialize();
            info.AddValue(KeysSerializationName, _keys, typeof(TKey[]));
            info.AddValue(ValuesSerializationName, _values, typeof(TValueCache[]));
        }

        /// <summary>
        /// Finalizes deserialization after <see cref="SerializationInfo"/> data has been applied.
        /// </summary>
        /// <param name="sender">Reserved for future use.</param>
        public void OnDeserialization(object sender)
        {
            // No additional action required. The serialization constructor already
            // reconstructed the sorted dictionary from the serialized key/value arrays.
        }

        private void MarkSerializationCacheDirty()
        {
            _preserveSerializedEntries = false;
            _keys = null;
            _values = null;
            _arraysDirty = true;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_dictionary.GetEnumerator());
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<
            KeyValuePair<TKey, TValue>
        >.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>)_dictionary).Add(item);
            MarkSerializationCacheDirty();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)_dictionary).Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>)_dictionary).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            bool removed = ((IDictionary<TKey, TValue>)_dictionary).Remove(item);
            if (removed)
            {
                MarkSerializationCacheDirty();
            }

            return removed;
        }

        public bool IsFixedSize => ((IDictionary)_dictionary).IsFixedSize;

        ICollection IDictionary.Keys => _dictionary.Keys;

        ICollection IDictionary.Values => _dictionary.Values;

        public bool IsSynchronized => ((IDictionary)_dictionary).IsSynchronized;

        public object SyncRoot => ((IDictionary)_dictionary).SyncRoot;

        public object this[object key]
        {
            get => ((IDictionary)_dictionary)[key];
            set
            {
                ((IDictionary)_dictionary)[key] = value;
                MarkSerializationCacheDirty();
            }
        }

        public void Add(object key, object value)
        {
            ((IDictionary)_dictionary).Add(key, value);
            MarkSerializationCacheDirty();
        }

        public bool Contains(object key)
        {
            return ((IDictionary)_dictionary).Contains(key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        public void Remove(object key)
        {
            IDictionary dictionary = _dictionary;
            bool existed = dictionary.Contains(key);
            dictionary.Remove(key);
            if (existed)
            {
                MarkSerializationCacheDirty();
            }
        }

        public void CopyTo(Array array, int index)
        {
            ((IDictionary)_dictionary).CopyTo(array, index);
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private SortedDictionary<TKey, TValue>.Enumerator _enumerator;

            internal Enumerator(SortedDictionary<TKey, TValue>.Enumerator enumerator)
            {
                _enumerator = enumerator;
            }

            public KeyValuePair<TKey, TValue> Current => _enumerator.Current;

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
    /// Concrete sorted dictionary implementation that saves both keys and values through Unity, ProtoBuf, and JSON serialization.
    /// Use this when you need deterministic iteration order plus inspector support for key/value data.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// SerializableSortedDictionary<string, int> scoreboard = new SerializableSortedDictionary<string, int>(StringComparer.Ordinal);
    /// scoreboard.Add("Alice", 1200);
    /// scoreboard.Add("Bob", 900);
    /// foreach (KeyValuePair<string, int> entry in scoreboard)
    /// {
    ///     Debug.Log($"{entry.Key}: {entry.Value}");
    /// }
    /// ]]></code>
    /// </example>
    [Serializable]
    [ProtoContract]
    public class SerializableSortedDictionary<TKey, TValue>
        : SerializableSortedDictionaryBase<TKey, TValue, TValue>
        where TKey : IComparable<TKey>
    {
        /// <summary>
        /// Initializes an empty sorted dictionary compatible with Unity and ProtoBuf serialization.
        /// </summary>
        public SerializableSortedDictionary() { }

        /// <summary>
        /// Initializes the dictionary by copying entries from an existing map.
        /// </summary>
        /// <param name="dictionary">Source dictionary whose contents are copied into the new instance.</param>
        public SerializableSortedDictionary(IDictionary<TKey, TValue> dictionary)
            : base(dictionary) { }

        /// <summary>
        /// Initializes the dictionary during custom serialization scenarios.
        /// </summary>
        protected SerializableSortedDictionary(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        protected override TValue GetValue(TValue[] cache, int index)
        {
            return cache[index];
        }

        protected override void SetValue(TValue[] cache, int index, TValue value)
        {
            cache[index] = value;
        }
    }

    /// <summary>
    /// Sorted dictionary variant that stores each value in a cache object so complex data can be serialized safely.
    /// Extend this when values require bespoke serialization, such as types containing Unity objects or unmanaged resources.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// [Serializable]
    /// public sealed class RichValue
    /// {
    ///     public string Name;
    ///     public int Cost;
    /// }
    ///
    /// [Serializable]
    /// public sealed class RichValueCache : SerializableDictionary.Cache<RichValue>
    /// {
    /// }
    ///
    /// SerializableSortedDictionary<int, RichValue, RichValueCache> catalog =
    ///     new SerializableSortedDictionary<int, RichValue, RichValueCache>();
    /// catalog[1] = new RichValue { Name = "HealthPotion", Cost = 50 };
    /// ]]></code>
    /// </example>
    [Serializable]
    [ProtoContract]
    public class SerializableSortedDictionary<TKey, TValue, TValueCache>
        : SerializableSortedDictionaryBase<TKey, TValue, TValueCache>
        where TKey : IComparable<TKey>
        where TValueCache : SerializableDictionary.Cache<TValue>, new()
    {
        /// <summary>
        /// Initializes an empty sorted dictionary whose values are stored through cache entries.
        /// </summary>
        public SerializableSortedDictionary() { }

        /// <summary>
        /// Initializes the dictionary by copying entries from an existing map.
        /// </summary>
        /// <param name="dictionary">Source dictionary whose contents are copied into the new instance.</param>
        public SerializableSortedDictionary(IDictionary<TKey, TValue> dictionary)
            : base(dictionary) { }

        /// <summary>
        /// Initializes the dictionary during custom serialization scenarios.
        /// </summary>
        protected SerializableSortedDictionary(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        protected override TValue GetValue(TValueCache[] cache, int index)
        {
            return cache[index].Data;
        }

        protected override void SetValue(TValueCache[] cache, int index, TValue value)
        {
            cache[index] = new TValueCache { Data = value };
        }
    }
}

// Portions of this file are adapted from JDSherbert's Unity-Serializable-Dictionary (MIT License):
// https://github.com/JDSherbert/Unity-Serializable-Dictionary

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
    /// Provides the shared infrastructure for serializable dictionary implementations.
    /// </summary>
    [Serializable]
    public abstract class SerializableDictionaryBase
    {
        protected class Dictionary<TKey, TValue>
            : System.Collections.Generic.Dictionary<TKey, TValue>
        {
            public Dictionary() { }

            public Dictionary(IDictionary<TKey, TValue> dictionary)
                : base(dictionary) { }

            public Dictionary(
                SerializationInfo serializationInfo,
                StreamingContext streamingContext
            )
                : base(serializationInfo, streamingContext) { }
        }

        [Serializable]
        public abstract class Cache { }

        public override string ToString()
        {
            return this.ToJson();
        }

        internal abstract void EditorAfterDeserialize();
    }

    /// <summary>
    /// Shared implementation for Unity serializable dictionaries.
    /// </summary>
    /// <typeparam name="TKey">Dictionary key type.</typeparam>
    /// <typeparam name="TValue">Dictionary value type.</typeparam>
    /// <typeparam name="TValueCache">Serialized value cache type.</typeparam>
    [Serializable]
    [ProtoContract]
    public abstract class SerializableDictionaryBase<TKey, TValue, TValueCache>
        : SerializableDictionaryBase,
            IDictionary<TKey, TValue>,
            IDictionary,
            ISerializationCallbackReceiver,
            IDeserializationCallback,
            ISerializable,
            IReadOnlyDictionary<TKey, TValue>
    {
        [ProtoIgnore]
        [JsonIgnore]
        private Dictionary<TKey, TValue> _dictionary;

        [SerializeField]
        [ProtoMember(1, OverwriteList = true)]
        internal TKey[] _keys;

        [SerializeField]
        [ProtoMember(2, OverwriteList = true)]
        internal TValueCache[] _values;

        [NonSerialized]
        private bool _preserveSerializedEntries;

        protected SerializableDictionaryBase()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }

        protected SerializableDictionaryBase(IDictionary<TKey, TValue> dictionary)
        {
            _dictionary = new Dictionary<TKey, TValue>(dictionary);
        }

        protected SerializableDictionaryBase(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext
        )
        {
            _dictionary = new Dictionary<TKey, TValue>(serializationInfo, streamingContext);
        }

        internal static class SerializedPropertyNames
        {
            private sealed class NameHolder : SerializableDictionary<TKey, TValue>
            {
                public static readonly string KeysName = nameof(_keys);
                public static readonly string ValuesName = nameof(_values);
            }

            internal static readonly string KeysName = NameHolder.KeysName;
            internal static readonly string ValuesName = NameHolder.ValuesName;
        }

        public void OnAfterDeserialize()
        {
            OnAfterDeserializeInternal(suppressWarnings: false);
        }

        internal override void EditorAfterDeserialize()
        {
            OnAfterDeserializeInternal(suppressWarnings: true);
        }

        private void OnAfterDeserializeInternal(bool suppressWarnings)
        {
            bool keysAndValuesPresent =
                _keys != null && _values != null && _keys.Length == _values.Length;

            _preserveSerializedEntries = false;

            if (!keysAndValuesPresent)
            {
                _keys = null;
                _values = null;
                return;
            }

            _dictionary.Clear();
            HashSet<TKey> observedKeys = new();
            bool hasDuplicateKeys = false;
            bool encounteredNullReference = false;
            bool keySupportsNullCheck = TypeSupportsNullReferences(typeof(TKey));
            bool valueSupportsNullCheck = TypeSupportsNullReferences(typeof(TValue));
            int length = _keys.Length;

            for (int index = 0; index < length; index++)
            {
                TKey key = _keys[index];
                TValue value = GetValue(_values, index);

                if (keySupportsNullCheck && ReferenceEquals(key, null))
                {
                    encounteredNullReference = true;
                    if (!suppressWarnings)
                    {
                        LogNullReferenceSkip("key", index);
                    }
                    continue;
                }

                if (valueSupportsNullCheck && ReferenceEquals(value, null))
                {
                    encounteredNullReference = true;
                    if (!suppressWarnings)
                    {
                        LogNullReferenceSkip("value", index);
                    }
                    continue;
                }

                if (!hasDuplicateKeys && !observedKeys.Add(key))
                {
                    hasDuplicateKeys = true;
                }

                _dictionary[key] = value;
            }

            _preserveSerializedEntries = hasDuplicateKeys || encounteredNullReference;

            if (!hasDuplicateKeys && !encounteredNullReference)
            {
                _keys = null;
                _values = null;
            }
        }

        private static bool TypeSupportsNullReferences(Type type)
        {
            return type != null
                && (!type.IsValueType || typeof(UnityEngine.Object).IsAssignableFrom(type));
        }

        private static void LogNullReferenceSkip(string component, int index)
        {
            Debug.LogError(
                $"SerializableDictionary<{typeof(TKey).FullName}, {typeof(TValue).FullName}> skipped serialized entry at index {index} because the {component} reference was null."
            );
        }

        public void OnBeforeSerialize()
        {
            bool arraysIntact = _keys != null && _values != null && _keys.Length == _values.Length;

            if (_preserveSerializedEntries && arraysIntact)
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

            _keys = null;
            _values = null;
        }

        [ProtoAfterDeserialization]
        private void OnProtoAfterDeserialization()
        {
            OnAfterDeserializeInternal(suppressWarnings: false);
        }

        protected abstract void SetValue(TValueCache[] cache, int index, TValue value);

        protected abstract TValue GetValue(TValueCache[] cache, int index);

        /// <summary>
        /// Replaces this dictionary's contents with another dictionary.
        /// </summary>
        /// <param name="dictionary">Source dictionary.</param>
        public void CopyFrom(IDictionary<TKey, TValue> dictionary)
        {
            _dictionary.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                _dictionary[pair.Key] = pair.Value;
            }

            MarkSerializationCacheDirty();
        }

        private void MarkSerializationCacheDirty()
        {
            _preserveSerializedEntries = false;
            _keys = null;
            _values = null;
        }

        public ICollection<TKey> Keys => _dictionary.Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        public ICollection<TValue> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public bool IsReadOnly => ((IDictionary<TKey, TValue>)_dictionary).IsReadOnly;

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

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>)_dictionary).Add(item);
            MarkSerializationCacheDirty();
        }

        public void Clear()
        {
            _dictionary.Clear();
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

        public void OnDeserialization(object sender)
        {
            ((IDeserializationCallback)_dictionary).OnDeserialization(sender);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            _dictionary.GetObjectData(info, context);
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private Dictionary<TKey, TValue>.Enumerator _enumerator;

            internal Enumerator(Dictionary<TKey, TValue>.Enumerator enumerator)
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
    /// Factory and cache helpers for serializable dictionaries.
    /// </summary>
    public static class SerializableDictionary
    {
        [Serializable]
        [ProtoContract]
        public class Cache<T> : SerializableDictionaryBase.Cache
        {
            [ProtoMember(1)]
            public T Data;
        }
    }

    /// <summary>
    /// Serializable dictionary with values serialized directly.
    /// </summary>
    /// <typeparam name="TKey">Dictionary key type.</typeparam>
    /// <typeparam name="TValue">Dictionary value type.</typeparam>
    [Serializable]
    [ProtoContract]
    public class SerializableDictionary<TKey, TValue>
        : SerializableDictionaryBase<TKey, TValue, TValue>
    {
        public SerializableDictionary() { }

        public SerializableDictionary(IDictionary<TKey, TValue> dictionary)
            : base(dictionary) { }

        protected SerializableDictionary(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext
        )
            : base(serializationInfo, streamingContext) { }

        protected override TValue GetValue(TValue[] cache, int index)
        {
            return cache[index];
        }

        protected override void SetValue(TValue[] cache, int index, TValue value)
        {
            cache[index] = value;
        }
    }

    internal static class SerializableDictionarySerializedPropertyNames
    {
        internal static readonly string Keys = SerializableDictionary<int, int>
            .SerializedPropertyNames
            .KeysName;
        internal static readonly string Values = SerializableDictionary<int, int>
            .SerializedPropertyNames
            .ValuesName;
    }

    /// <summary>
    /// Serializable dictionary that uses cache objects for value serialization.
    /// </summary>
    /// <typeparam name="TKey">Dictionary key type.</typeparam>
    /// <typeparam name="TValue">Dictionary value type.</typeparam>
    /// <typeparam name="TValueCache">Serialized value cache type.</typeparam>
    [Serializable]
    [ProtoContract]
    public class SerializableDictionary<TKey, TValue, TValueCache>
        : SerializableDictionaryBase<TKey, TValue, TValueCache>
        where TValueCache : SerializableDictionary.Cache<TValue>, new()
    {
        public SerializableDictionary() { }

        public SerializableDictionary(IDictionary<TKey, TValue> dictionary)
            : base(dictionary) { }

        protected SerializableDictionary(
            SerializationInfo serializationInfo,
            StreamingContext streamingContext
        )
            : base(serializationInfo, streamingContext) { }

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

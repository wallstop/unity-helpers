namespace WallstopStudios.UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using ProtoBuf;
    using UnityEngine;

    /// <summary>
    /// Sorted dictionary that supports Unity, JSON, and ProtoBuf serialisation.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public abstract class SerializableSortedDictionaryBase<TKey, TValue, TValueCache>
        : IDictionary<TKey, TValue>,
            IDictionary,
            IReadOnlyDictionary<TKey, TValue>,
            ISerializationCallbackReceiver,
            IDeserializationCallback,
            ISerializable
    {
        private const string KeysSerializationName = "Keys";
        private const string ValuesSerializationName = "Values";
        private const string ComparerSerializationName = "Comparer";

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

        protected SerializableSortedDictionaryBase()
            : this((IComparer<TKey>)null) { }

        protected SerializableSortedDictionaryBase(IComparer<TKey> comparer)
        {
            _dictionary = new SortedDictionary<TKey, TValue>(comparer ?? Comparer<TKey>.Default);
        }

        protected SerializableSortedDictionaryBase(IDictionary<TKey, TValue> dictionary)
            : this(dictionary, null) { }

        protected SerializableSortedDictionaryBase(
            IDictionary<TKey, TValue> dictionary,
            IComparer<TKey> comparer
        )
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            _dictionary = new SortedDictionary<TKey, TValue>(
                dictionary,
                comparer ?? Comparer<TKey>.Default
            );
        }

        protected SerializableSortedDictionaryBase(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            IComparer<TKey> comparer =
                (IComparer<TKey>)info.GetValue(ComparerSerializationName, typeof(IComparer<TKey>));
            _dictionary = new SortedDictionary<TKey, TValue>(comparer ?? Comparer<TKey>.Default);

            _keys = (TKey[])info.GetValue(KeysSerializationName, typeof(TKey[]));
            _values = (TValueCache[])info.GetValue(ValuesSerializationName, typeof(TValueCache[]));
            OnAfterDeserialize();
        }

        protected SerializableSortedDictionaryBase(
            SerializationInfo info,
            StreamingContext context,
            IComparer<TKey> comparer
        )
            : this(info, context)
        {
            if (comparer != null && comparer != _dictionary.Comparer)
            {
                SortedDictionary<TKey, TValue> replacement = new SortedDictionary<TKey, TValue>(
                    comparer
                );
                foreach (KeyValuePair<TKey, TValue> pair in _dictionary)
                {
                    replacement[pair.Key] = pair.Value;
                }

                _dictionary = replacement;
            }
        }

        public IComparer<TKey> Comparer => _dictionary.Comparer;

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
            int length = _keys.Length;

            for (int index = 0; index < length; index++)
            {
                TKey key = _keys[index];
                TValue value = GetValue(_values, index);

                if (!hasDuplicateKeys && _dictionary.ContainsKey(key))
                {
                    hasDuplicateKeys = true;
                }

                _dictionary[key] = value;
            }

            _preserveSerializedEntries = hasDuplicateKeys;
            _arraysDirty = false;
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
            OnAfterDeserialize();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            OnBeforeSerialize();
            info.AddValue(KeysSerializationName, _keys, typeof(TKey[]));
            info.AddValue(ValuesSerializationName, _values, typeof(TValueCache[]));
            info.AddValue(ComparerSerializationName, _dictionary.Comparer, typeof(IComparer<TKey>));
        }

        public void OnDeserialization(object sender)
        {
            // No additional action required. The serialization constructor already
            // reconstructed the sorted dictionary using the stored comparer and
            // serialized key/value arrays.
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

    [Serializable]
    [ProtoContract]
    public class SerializableSortedDictionary<TKey, TValue>
        : SerializableSortedDictionaryBase<TKey, TValue, TValue>
    {
        public SerializableSortedDictionary() { }

        public SerializableSortedDictionary(IComparer<TKey> comparer)
            : base(comparer) { }

        public SerializableSortedDictionary(IDictionary<TKey, TValue> dictionary)
            : base(dictionary) { }

        public SerializableSortedDictionary(
            IDictionary<TKey, TValue> dictionary,
            IComparer<TKey> comparer
        )
            : base(dictionary, comparer) { }

        protected SerializableSortedDictionary(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        protected SerializableSortedDictionary(
            SerializationInfo info,
            StreamingContext context,
            IComparer<TKey> comparer
        )
            : base(info, context, comparer) { }

        protected override TValue GetValue(TValue[] cache, int index)
        {
            return cache[index];
        }

        protected override void SetValue(TValue[] cache, int index, TValue value)
        {
            cache[index] = value;
        }
    }

    [Serializable]
    [ProtoContract]
    public class SerializableSortedDictionary<TKey, TValue, TValueCache>
        : SerializableSortedDictionaryBase<TKey, TValue, TValueCache>
        where TValueCache : SerializableDictionary.Cache<TValue>, new()
    {
        public SerializableSortedDictionary() { }

        public SerializableSortedDictionary(IComparer<TKey> comparer)
            : base(comparer) { }

        public SerializableSortedDictionary(IDictionary<TKey, TValue> dictionary)
            : base(dictionary) { }

        public SerializableSortedDictionary(
            IDictionary<TKey, TValue> dictionary,
            IComparer<TKey> comparer
        )
            : base(dictionary, comparer) { }

        protected SerializableSortedDictionary(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        protected SerializableSortedDictionary(
            SerializationInfo info,
            StreamingContext context,
            IComparer<TKey> comparer
        )
            : base(info, context, comparer) { }

        protected override TValue GetValue(TValueCache[] cache, int index)
        {
            return cache[index].Data;
        }

        protected override void SetValue(TValueCache[] cache, int index, TValue value)
        {
            cache[index] = new TValueCache();
            cache[index].Data = value;
        }
    }
}

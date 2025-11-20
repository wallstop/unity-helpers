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
#if UNITY_EDITOR
    using UnityEditor;
#endif

    /// <summary>
    /// Provides the shared infrastructure for Unity-friendly serializable dictionary implementations.
    /// Manages the synchronized key and value arrays that Unity, ProtoBuf, and JSON rely on,
    /// while exposing a runtime dictionary for fast lookups and mutations.
    /// Derive from the generic base to create strongly typed dictionaries that stay editable in the inspector.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// [Serializable]
    /// public sealed class WeaponDefinition
    /// {
    ///     public string DisplayName;
    /// }
    ///
    /// [Serializable]
    /// public sealed class WeaponDefinitionCache : SerializableDictionary.Cache<WeaponDefinition>
    /// {
    /// }
    ///
    /// [Serializable]
    /// public sealed class WeaponDictionary
    ///     : SerializableDictionaryBase<int, WeaponDefinition, WeaponDefinitionCache>
    /// {
    ///     protected override WeaponDefinition GetValue(WeaponDefinitionCache[] cache, int index)
    ///     {
    ///         return cache[index].Data;
    ///     }
    ///
    ///     protected override void SetValue(WeaponDefinitionCache[] cache, int index, WeaponDefinition value)
    ///     {
    ///         cache[index] = new WeaponDefinitionCache { Data = value };
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    [Serializable]
    public abstract class SerializableDictionaryBase
    {
        protected class Dictionary<TKey, TValue>
            : System.Collections.Generic.Dictionary<TKey, TValue>
        {
            /// <summary>
            /// Creates an empty runtime dictionary that uses the default equality comparer.
            /// </summary>
            /// <example>
            /// <code><![CDATA[
            /// protected sealed class AbilityDictionary
            ///     : SerializableDictionaryBase<string, AbilityDefinition, AbilityCache>
            /// {
            ///     public AbilityDictionary()
            ///         : base(new Dictionary<string, AbilityDefinition>())
            ///     {
            ///     }
            /// }
            /// ]]></code>
            /// </example>
            public Dictionary() { }

            /// <summary>
            /// Creates a runtime dictionary pre-populated with entries from another dictionary.
            /// </summary>
            /// <param name="dictionary">The source collection to copy.</param>
            /// <example>
            /// <code><![CDATA[
            /// IDictionary<string, AbilityDefinition> seed = new Dictionary<string, AbilityDefinition>();
            /// Dictionary<string, AbilityDefinition> runtimeDictionary =
            ///     new Dictionary<string, AbilityDefinition>(seed);
            /// ]]></code>
            /// </example>
            public Dictionary(IDictionary<TKey, TValue> dictionary)
                : base(dictionary) { }

            /// <summary>
            /// Rehydrates the dictionary from a <see cref="SerializationInfo"/> payload.
            /// </summary>
            /// <param name="serializationInfo">Serialized data describing the dictionary.</param>
            /// <param name="streamingContext">Context about the serialization source or destination.</param>
            /// <example>
            /// <code><![CDATA[
            /// SerializationInfo info = new SerializationInfo(typeof(Dictionary<string, AbilityDefinition>), new FormatterConverter());
            /// StreamingContext context = new StreamingContext(StreamingContextStates.File);
            /// Dictionary<string, AbilityDefinition> runtimeDictionary = new Dictionary<string, AbilityDefinition>(info, context);
            /// ]]></code>
            /// </example>
            public Dictionary(
                SerializationInfo serializationInfo,
                StreamingContext streamingContext
            )
                : base(serializationInfo, streamingContext) { }
        }

        [Serializable]
        public abstract class Cache { }

        /// <summary>
        /// Produces a JSON string that mirrors the serialized key and value arrays, which is useful for debugging.
        /// </summary>
        /// <returns>A JSON representation of the dictionary contents.</returns>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilityLookup = new AbilityDictionary();
        /// abilityLookup["Dash"] = dashDefinition;
        /// string preview = abilityLookup.ToString();
        /// Debug.Log(preview);
        /// ]]></code>
        /// </example>
        public override string ToString()
        {
            return this.ToJson();
        }

        internal abstract void EditorAfterDeserialize();
    }

    /// <summary>
    /// Shared implementation for Unity serializable dictionaries that keeps serialized key/value arrays synchronized with the runtime <see cref="Dictionary{TKey,TValue}"/>.
    /// Override <see cref="SetValue"/> and <see cref="GetValue"/> to control how values move between serialized caches and the live map.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// [Serializable]
    /// public sealed class AbilityDefinition : ScriptableObject
    /// {
    ///     public string Id;
    /// }
    ///
    /// [Serializable]
    /// public sealed class AbilityCache : SerializableDictionary.Cache<AbilityDefinition>
    /// {
    /// }
    ///
    /// [Serializable]
    /// public sealed class AbilityDictionary
    ///     : SerializableDictionaryBase<string, AbilityDefinition, AbilityCache>
    /// {
    ///     public AbilityDictionary()
    ///         : base(new Dictionary<string, AbilityDefinition>(StringComparer.OrdinalIgnoreCase))
    ///     {
    ///     }
    ///
    ///     protected override AbilityDefinition GetValue(AbilityCache[] cache, int index)
    ///     {
    ///         return cache[index].Data;
    ///     }
    ///
    ///     protected override void SetValue(AbilityCache[] cache, int index, AbilityDefinition value)
    ///     {
    ///         cache[index] = new AbilityCache { Data = value };
    ///     }
    /// }
    /// ]]></code>
    /// </example>
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

        internal bool PreserveSerializedEntries => _preserveSerializedEntries;

        internal TKey[] SerializedKeys => _keys;

        internal TValueCache[] SerializedValues => _values;

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
                public const string KeysName = nameof(_keys);
                public const string ValuesName = nameof(_values);
            }

            internal const string KeysNameInternal = NameHolder.KeysName;
            internal const string ValuesNameInternal = NameHolder.ValuesName;
        }

        /// <summary>
        /// Rebuilds the runtime dictionary from the serialized key/value arrays after Unity or ProtoBuf deserialization.
        /// </summary>
        /// <remarks>
        /// Invoked automatically by Unity; call it manually only when deserializing outside of Unity's pipeline.
        /// </remarks>
        /// <example>
        /// <code><![CDATA[
        /// dictionary.OnAfterDeserialize();
        /// IReadOnlyDictionary<TKey, TValue> restored = dictionary;
        /// ]]></code>
        /// </example>
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
#if UNITY_EDITOR
            if (!EditorShouldLog())
            {
                return;
            }
#endif

            Debug.LogError(
                $"SerializableDictionary<{typeof(TKey).FullName}, {typeof(TValue).FullName}> skipped serialized entry at index {index} because the {component} reference was null."
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

        /// <summary>
        /// Packs the runtime dictionary contents into the serialized key/value arrays prior to Unity or ProtoBuf serialization.
        /// </summary>
        /// <remarks>
        /// This method is invoked automatically by Unity's serialization pipeline; call it manually only when integrating with custom serializers.
        /// </remarks>
        /// <example>
        /// <code>
        /// dictionary.OnBeforeSerialize();
        /// var keys = dictionary.SerializedKeys;
        /// </code>
        /// </example>
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

        /// <summary>
        /// Creates a new <see cref="global::System.Collections.Generic.Dictionary{TKey, TValue}"/> populated with this dictionary's contents.
        /// </summary>
        /// <returns>A copy of the dictionary's current state.</returns>
        public global::System.Collections.Generic.Dictionary<TKey, TValue> ToDictionary()
        {
            global::System.Collections.Generic.Dictionary<TKey, TValue> copy = new(
                _dictionary,
                _dictionary.Comparer
            );
            return copy;
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

        /// <summary>
        /// Gets or sets a value associated with the provided key in the runtime dictionary.
        /// Updates invalidate the serialized cache so Unity and ProtoBuf can pick up the changes.
        /// </summary>
        /// <param name="key">The key of the entry to access.</param>
        /// <returns>The stored value.</returns>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilityLookup = new AbilityDictionary();
        /// abilityLookup["Dash"] = dashDefinition;
        /// AbilityDefinition dash = abilityLookup["Dash"];
        /// ]]></code>
        /// </example>
        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set
            {
                _dictionary[key] = value;
                MarkSerializationCacheDirty();
            }
        }

        /// <summary>
        /// Adds a new key/value pair to the runtime dictionary and marks the serialized cache as dirty so Unity can persist the change.
        /// </summary>
        /// <param name="key">The key to insert.</param>
        /// <param name="value">The value associated with the key.</param>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// abilities.Add("Dash", dashDefinition);
        /// ]]></code>
        /// </example>
        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
            MarkSerializationCacheDirty();
        }

        /// <summary>
        /// Attempts to add a new entry without throwing when the key already exists.
        /// </summary>
        /// <param name="key">The key to insert.</param>
        /// <param name="value">The value associated with the key.</param>
        /// <returns><c>true</c> when the entry was added.</returns>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// bool added = abilities.TryAdd("Dash", dashDefinition);
        /// ]]></code>
        /// </example>
        public bool TryAdd(TKey key, TValue value)
        {
            bool added = _dictionary.TryAdd(key, value);
            if (added)
            {
                MarkSerializationCacheDirty();
            }

            return added;
        }

        /// <summary>
        /// Determines whether the runtime dictionary already contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns><c>true</c> when the key exists.</returns>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// bool hasDash = abilities.ContainsKey("Dash");
        /// ]]></code>
        /// </example>
        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Removes an entry by key and invalidates the serialized cache if the key existed.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns><c>true</c> when an entry was removed.</returns>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// bool removed = abilities.Remove("Dash");
        /// ]]></code>
        /// </example>
        public bool Remove(TKey key)
        {
            bool removed = _dictionary.Remove(key);
            if (removed)
            {
                MarkSerializationCacheDirty();
            }

            return removed;
        }

        /// <summary>
        /// Removes an entry and outputs the value that was previously stored.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <param name="value">Receives the removed value when successful.</param>
        /// <returns><c>true</c> when the key was found.</returns>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// AbilityDefinition removed;
        /// bool removedEntry = abilities.Remove("Dash", out removed);
        /// ]]></code>
        /// </example>
        public bool Remove(TKey key, out TValue value)
        {
            bool removed = _dictionary.Remove(key, out value);
            if (removed)
            {
                MarkSerializationCacheDirty();
            }

            return removed;
        }

        /// <summary>
        /// Retrieves a value without throwing when the key is missing.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <param name="value">Outputs the located value.</param>
        /// <returns><c>true</c> when the key exists.</returns>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// AbilityDefinition dash;
        /// if (abilities.TryGetValue("Dash", out dash))
        /// {
        ///     dash.Activate();
        /// }
        /// ]]></code>
        /// </example>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Adds a <see cref="KeyValuePair{TKey, TValue}"/> to the dictionary via the <see cref="ICollection{T}"/> interface.
        /// </summary>
        /// <param name="item">The entry to add.</param>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// KeyValuePair<string, AbilityDefinition> entry = new KeyValuePair<string, AbilityDefinition>("Dash", dashDefinition);
        /// abilities.Add(entry);
        /// ]]></code>
        /// </example>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>)_dictionary).Add(item);
            MarkSerializationCacheDirty();
        }

        /// <summary>
        /// Removes all entries from the dictionary and clears the serialized arrays.
        /// </summary>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// abilities.Clear();
        /// ]]></code>
        /// </example>
        public void Clear()
        {
            _dictionary.Clear();
            MarkSerializationCacheDirty();
        }

        /// <summary>
        /// Determines whether the dictionary contains the provided key/value pair.
        /// </summary>
        /// <param name="item">The entry to look for.</param>
        /// <returns><c>true</c> when both the key and value match.</returns>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// KeyValuePair<string, AbilityDefinition> entry = new KeyValuePair<string, AbilityDefinition>("Dash", dashDefinition);
        /// bool present = abilities.Contains(entry);
        /// ]]></code>
        /// </example>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)_dictionary).Contains(item);
        }

        /// <summary>
        /// Copies the contents of the dictionary into the provided array, which is useful when interoperating with legacy APIs.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The index to start copying into.</param>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// KeyValuePair<string, AbilityDefinition>[] snapshot = new KeyValuePair<string, AbilityDefinition>[abilities.Count];
        /// abilities.CopyTo(snapshot, 0);
        /// ]]></code>
        /// </example>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>)_dictionary).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the specified key/value pair only when both components match the stored entry.
        /// </summary>
        /// <param name="item">The entry to remove.</param>
        /// <returns><c>true</c> when the pair existed.</returns>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// KeyValuePair<string, AbilityDefinition> entry = new KeyValuePair<string, AbilityDefinition>("Dash", dashDefinition);
        /// bool removed = abilities.Remove(entry);
        /// ]]></code>
        /// </example>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            bool removed = ((IDictionary<TKey, TValue>)_dictionary).Remove(item);
            if (removed)
            {
                MarkSerializationCacheDirty();
            }

            return removed;
        }

        /// <summary>
        /// Returns a struct enumerator that iterates over key/value pairs without allocations.
        /// </summary>
        /// <returns>An enumerator positioned before the first entry.</returns>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// foreach (KeyValuePair<string, AbilityDefinition> entry in abilities)
        /// {
        ///     Debug.Log(entry.Key);
        /// }
        /// ]]></code>
        /// </example>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_dictionary.GetEnumerator());
        }

        /// <inheritdoc />
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<
            KeyValuePair<TKey, TValue>
        >.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        /// <summary>
        /// Indicates whether the non-generic <see cref="IDictionary"/> wrapper has a fixed size (it does not).
        /// </summary>
        public bool IsFixedSize => ((IDictionary)_dictionary).IsFixedSize;

        ICollection IDictionary.Keys => _dictionary.Keys;

        ICollection IDictionary.Values => _dictionary.Values;

        /// <summary>
        /// Indicates whether access to the dictionary is synchronized (thread-safe).
        /// </summary>
        public bool IsSynchronized => ((IDictionary)_dictionary).IsSynchronized;

        /// <summary>
        /// Provides an object that can be used to synchronize access when required by legacy APIs.
        /// </summary>
        public object SyncRoot => ((IDictionary)_dictionary).SyncRoot;

        /// <summary>
        /// Gets or sets entries through the non-generic <see cref="IDictionary"/> interface.
        /// </summary>
        /// <param name="key">The boxed key.</param>
        /// <returns>The boxed value associated with the key.</returns>
        public object this[object key]
        {
            get => ((IDictionary)_dictionary)[key];
            set
            {
                ((IDictionary)_dictionary)[key] = value;
                MarkSerializationCacheDirty();
            }
        }

        /// <summary>
        /// Adds a boxed key/value pair through the non-generic interface.
        /// </summary>
        /// <param name="key">The boxed key.</param>
        /// <param name="value">The boxed value.</param>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// IDictionary boxed = abilities;
        /// boxed.Add((object)"Dash", dashDefinition);
        /// ]]></code>
        /// </example>
        public void Add(object key, object value)
        {
            ((IDictionary)_dictionary).Add(key, value);
            MarkSerializationCacheDirty();
        }

        /// <summary>
        /// Determines whether the dictionary contains the provided boxed key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns><c>true</c> when the key exists.</returns>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// IDictionary boxed = abilities;
        /// bool hasDash = boxed.Contains((object)"Dash");
        /// ]]></code>
        /// </example>
        public bool Contains(object key)
        {
            return ((IDictionary)_dictionary).Contains(key);
        }

        /// <inheritdoc />
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        /// <summary>
        /// Removes a boxed entry and marks the serialized cache as dirty when something is deleted.
        /// </summary>
        /// <param name="key">The boxed key to remove.</param>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// IDictionary boxed = abilities;
        /// boxed.Remove((object)"Dash");
        /// ]]></code>
        /// </example>
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

        /// <summary>
        /// Copies the dictionary contents into a non-generic array, matching <see cref="IDictionary.CopyTo"/>.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="index">The starting index inside <paramref name="array"/>.</param>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// DictionaryEntry[] entries = new DictionaryEntry[abilities.Count];
        /// IDictionary boxed = abilities;
        /// boxed.CopyTo(entries, 0);
        /// ]]></code>
        /// </example>
        public void CopyTo(Array array, int index)
        {
            ((IDictionary)_dictionary).CopyTo(array, index);
        }

        /// <summary>
        /// Completes deserialization after <see cref="SerializationInfo"/> data has been applied.
        /// </summary>
        /// <param name="sender">Reserved for future use.</param>
        public void OnDeserialization(object sender)
        {
            ((IDeserializationCallback)_dictionary).OnDeserialization(sender);
        }

        /// <summary>
        /// Writes the serialized representation of the dictionary into a <see cref="SerializationInfo"/> instance.
        /// </summary>
        /// <param name="info">The serialization store to populate.</param>
        /// <param name="context">Context for the serialization process.</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            _dictionary.GetObjectData(info, context);
        }

        /// <summary>
        /// Allocation-free enumerator used by <see cref="SerializableDictionaryBase{TKey, TValue, TValueCache}"/>.
        /// </summary>
        /// <example>
        /// <code><![CDATA[
        /// AbilityDictionary abilities = new AbilityDictionary();
        /// SerializableDictionary<string, AbilityDefinition>.Enumerator enumerator = abilities.GetEnumerator();
        /// while (enumerator.MoveNext())
        /// {
        ///     KeyValuePair<string, AbilityDefinition> entry = enumerator.Current;
        /// }
        /// ]]></code>
        /// </example>
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private Dictionary<TKey, TValue>.Enumerator _enumerator;

            internal Enumerator(Dictionary<TKey, TValue>.Enumerator enumerator)
            {
                _enumerator = enumerator;
            }

            public KeyValuePair<TKey, TValue> Current => _enumerator.Current;

            object IEnumerator.Current => _enumerator.Current;

            /// <summary>
            /// Advances the enumerator to the next entry.
            /// </summary>
            /// <returns><c>true</c> when another item is available.</returns>
            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            /// <summary>
            /// Disposes of the underlying dictionary enumerator.
            /// </summary>
            public void Dispose()
            {
                _enumerator.Dispose();
            }

            /// <summary>
            /// Reset is not supported because Unity serializable dictionaries mirror <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/> semantics.
            /// </summary>
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
    /// Unity-friendly dictionary that keeps keys and values serialized for editor, ProtoBuf, and JSON pipelines.
    /// Use this when you need deterministic order, inspector editing, and runtime dictionary semantics without custom wrappers.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// [Serializable]
    /// public sealed class LootTable : MonoBehaviour
    /// {
    ///     [SerializeField]
    ///     private SerializableDictionary<string, int> weights = new SerializableDictionary<string, int>();
    ///
    ///     private void Awake()
    ///     {
    ///         weights["Common"] = 80;
    ///         weights["Rare"] = 15;
    ///         weights["Legendary"] = 5;
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    /// <typeparam name="TKey">Dictionary key type.</typeparam>
    /// <typeparam name="TValue">Dictionary value type.</typeparam>
    [Serializable]
    [ProtoContract]
    public class SerializableDictionary<TKey, TValue>
        : SerializableDictionaryBase<TKey, TValue, TValue>
    {
        /// <summary>
        /// Initializes an empty serializable dictionary whose values can be written directly to Unity serialization.
        /// </summary>
        /// <example>
        /// <code><![CDATA[
        /// SerializableDictionary<string, int> weights = new SerializableDictionary<string, int>();
        /// weights["Common"] = 42;
        /// ]]></code>
        /// </example>
        public SerializableDictionary() { }

        /// <summary>
        /// Initializes the serializable dictionary with items copied from an existing dictionary.
        /// </summary>
        /// <param name="dictionary">The source entries to clone.</param>
        /// <example>
        /// <code><![CDATA[
        /// Dictionary<string, int> seed = new Dictionary<string, int>();
        /// seed["Common"] = 42;
        /// SerializableDictionary<string, int> weights = new SerializableDictionary<string, int>(seed);
        /// ]]></code>
        /// </example>
        public SerializableDictionary(IDictionary<TKey, TValue> dictionary)
            : base(dictionary) { }

        /// <summary>
        /// Deserialization constructor used by <see cref="ISerializable"/> pipelines.
        /// </summary>
        /// <param name="serializationInfo">Serialized key/value data.</param>
        /// <param name="streamingContext">Information about the serialization source.</param>
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
        internal const string Keys = SerializableDictionary<int, int>
            .SerializedPropertyNames
            .KeysNameInternal;

        internal const string Values = SerializableDictionary<int, int>
            .SerializedPropertyNames
            .ValuesNameInternal;
    }

    /// <summary>
    /// Serializable dictionary that stores value data inside cache objects so complex or non-serializable runtime types can participate in Unity serialization.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// [Serializable]
    /// [Serializable]
    /// public sealed class ComplexValue
    /// {
    ///     public int Score;
    /// }
    ///
    /// [Serializable]
    /// public sealed class ComplexValueCache : SerializableDictionary.Cache<ComplexValue>
    /// {
    /// }
    ///
    /// [Serializable]
    /// public sealed class ComplexValueDictionary
    ///     : SerializableDictionary<string, ComplexValue, ComplexValueCache>
    /// {
    /// }
    /// ]]></code>
    /// </example>
    /// <typeparam name="TKey">Dictionary key type.</typeparam>
    /// <typeparam name="TValue">Dictionary value type.</typeparam>
    /// <typeparam name="TValueCache">Serialized value cache type.</typeparam>
    [Serializable]
    [ProtoContract]
    public class SerializableDictionary<TKey, TValue, TValueCache>
        : SerializableDictionaryBase<TKey, TValue, TValueCache>
        where TValueCache : SerializableDictionary.Cache<TValue>, new()
    {
        /// <summary>
        /// Initializes an empty serializable dictionary whose values are stored in cache objects.
        /// </summary>
        /// <example>
        /// <code><![CDATA[
        /// SerializableDictionary<string, ComplexValue, ComplexValueCache> cache =
        ///     new SerializableDictionary<string, ComplexValue, ComplexValueCache>();
        /// ]]></code>
        /// </example>
        public SerializableDictionary() { }

        /// <summary>
        /// Initializes the dictionary with entries copied from an existing runtime dictionary.
        /// </summary>
        /// <param name="dictionary">Entries to seed the serializable dictionary with.</param>
        /// <example>
        /// <code><![CDATA[
        /// Dictionary<string, ComplexValue> seed = new Dictionary<string, ComplexValue>();
        /// SerializableDictionary<string, ComplexValue, ComplexValueCache> cache =
        ///     new SerializableDictionary<string, ComplexValue, ComplexValueCache>(seed);
        /// ]]></code>
        /// </example>
        public SerializableDictionary(IDictionary<TKey, TValue> dictionary)
            : base(dictionary) { }

        /// <summary>
        /// Deserialization constructor required by the <see cref="ISerializable"/> contract.
        /// </summary>
        /// <param name="serializationInfo">Serialized representation of the dictionary.</param>
        /// <param name="streamingContext">Context describing the serialization environment.</param>
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

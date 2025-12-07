namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// Non-generic helper for ScriptableObjectSingleton initialization state tracking.
    /// </summary>
    internal static class ScriptableObjectSingletonInitState
    {
#if UNITY_EDITOR
        private static bool _initialEnsureCompleted;

        /// <summary>
        /// Indicates whether the initial singleton asset creation pass has completed globally.
        /// When false, metadata-related warnings are suppressed since assets may not exist yet.
        /// </summary>
        internal static bool InitialEnsureCompleted
        {
            get => _initialEnsureCompleted;
            set => _initialEnsureCompleted = value;
        }
#endif
    }

    [Serializable]
    internal sealed class ScriptableObjectSingletonMetadata : ScriptableObject
    {
        public const string ResourcePath =
            "Wallstop Studios/Unity Helpers/ScriptableObjectSingletonMetadata";
        public const string AssetPath =
            "Assets/Resources/Wallstop Studios/Unity Helpers/ScriptableObjectSingletonMetadata.asset";

        /// <summary>
        /// Legacy path for migration from older versions.
        /// </summary>
        internal const string LegacyAssetPath =
            "Assets/Resources/ScriptableObjectSingletonMetadata.asset";

        [Serializable]
        public struct Entry
        {
            public string assemblyQualifiedTypeName;
            public string resourcesLoadPath;
            public string resourcesPath;

            // ReSharper disable once NotAccessedField.Global
            public string assetGuid;
        }

        [FormerlySerializedAs("entries")]
        [SerializeField]
        private List<Entry> _entries = new();

        public bool TryGetEntry(Type type, out Entry entry)
        {
            if (type is null)
            {
                entry = default;
                return false;
            }

            string key = type.AssemblyQualifiedName;
            if (string.IsNullOrEmpty(key) || _entries == null || _entries.Count == 0)
            {
                entry = default;
                return false;
            }

            foreach (Entry candidate in _entries)
            {
                if (
                    string.Equals(
                        candidate.assemblyQualifiedTypeName,
                        key,
                        StringComparison.Ordinal
                    )
                )
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = default;
            return false;
        }

#if UNITY_EDITOR
        public void SetOrUpdateEntry(Entry entry)
        {
            _entries ??= new List<Entry>();

            string key = entry.assemblyQualifiedTypeName;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (
                    string.Equals(
                        _entries[i].assemblyQualifiedTypeName,
                        key,
                        StringComparison.Ordinal
                    )
                )
                {
                    _entries[i] = entry;
                    return;
                }
            }

            _entries.Add(entry);
        }

        /// <summary>
        /// Removes an entry for the specified type from the metadata.
        /// </summary>
        /// <param name="assemblyQualifiedTypeName">The assembly-qualified type name to remove.</param>
        /// <returns>True if an entry was removed; false otherwise.</returns>
        public bool RemoveEntry(string assemblyQualifiedTypeName)
        {
            if (string.IsNullOrEmpty(assemblyQualifiedTypeName) || _entries == null)
            {
                return false;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                if (
                    string.Equals(
                        _entries[i].assemblyQualifiedTypeName,
                        assemblyQualifiedTypeName,
                        StringComparison.Ordinal
                    )
                )
                {
                    _entries.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all entries in the metadata. For editor tooling use only.
        /// </summary>
        public IReadOnlyList<Entry> GetAllEntries()
        {
            return _entries ?? (IReadOnlyList<Entry>)Array.Empty<Entry>();
        }
#endif
    }
}

namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    internal sealed class ScriptableObjectSingletonMetadata : ScriptableObject
    {
        public const string ResourcePath = "ScriptableObjectSingletonMetadata";
        public const string AssetPath = "Assets/Resources/ScriptableObjectSingletonMetadata.asset";

        [Serializable]
        public struct Entry
        {
            public string assemblyQualifiedTypeName;
            public string resourcesLoadPath;
            public string resourcesPath;
            public string assetGuid;
        }

        [SerializeField]
        private List<Entry> entries = new();

        public bool TryGetEntry(Type type, out Entry entry)
        {
            if (type is null)
            {
                entry = default;
                return false;
            }

            string key = type.AssemblyQualifiedName;
            if (string.IsNullOrEmpty(key) || entries == null || entries.Count == 0)
            {
                entry = default;
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                Entry candidate = entries[i];
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
            if (entries == null)
            {
                entries = new List<Entry>();
            }

            string key = entry.assemblyQualifiedTypeName;
            for (int i = 0; i < entries.Count; i++)
            {
                if (
                    string.Equals(
                        entries[i].assemblyQualifiedTypeName,
                        key,
                        StringComparison.Ordinal
                    )
                )
                {
                    entries[i] = entry;
                    return;
                }
            }

            entries.Add(entry);
        }
#endif
    }
}

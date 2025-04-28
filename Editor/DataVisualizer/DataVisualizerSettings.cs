namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer
{
    using System.Collections.Generic;
    using Data;
    using UnityEngine;
    using UnityEngine.Serialization;

    [CreateAssetMenu(
        fileName = "DataVisualizerSettings",
        menuName = "DataVisualizer/Data Visualizer Settings",
        order = 1
    )]
    public sealed class DataVisualizerSettings : ScriptableObject
    {
        public const string DefaultDataFolderPath = "Assets/Data";

        public string DataFolderPath => _dataFolderPath;

        [Tooltip(
            "Path relative to the project root (e.g., Assets/Data) where DataObject assets might be located or created."
        )]
        [SerializeField]
        internal string _dataFolderPath = DefaultDataFolderPath;

        [FormerlySerializedAs("UseEditorPrefsForState")]
        [Tooltip(
            "If true, window state (selection, order, collapse) is saved globally in EditorPrefs. If false, state is saved within this settings asset file."
        )]
        public bool PersistStateInSettingsAsset;

        [
            Header("Saved State (Internal - Use only if EditorPrefs is disabled)"),
            SerializeField,
            HideInInspector
        ]
        internal string InternalLastSelectedNamespaceKey;

        [SerializeField]
        [HideInInspector]
        internal string InternalLastSelectedTypeName;

        [SerializeField, HideInInspector]
        internal List<LastObjectSelectionEntry> InternalLastObjectSelections =
            new List<LastObjectSelectionEntry>();

        [SerializeField]
        [HideInInspector]
        internal List<string> InternalNamespaceOrder = new List<string>();

        // Helper class for storing type order per namespace
        [System.Serializable]
        internal class NamespaceTypeOrder
        {
            public string NamespaceKey; // The namespace this order applies to
            public List<string> TypeNames = new(); // Ordered list of type names
        }

        [SerializeField]
        [HideInInspector]
        internal List<NamespaceTypeOrder> InternalTypeOrders = new();

        [System.Serializable]
        internal class NamespaceCollapseState
        {
            public string NamespaceKey;
            public bool IsCollapsed;
        }

        [SerializeField]
        [HideInInspector]
        internal List<NamespaceCollapseState> InternalNamespaceCollapseStates = new();

        private void OnValidate()
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                if (string.IsNullOrWhiteSpace(_dataFolderPath))
                {
                    _dataFolderPath = DefaultDataFolderPath;
                }

                _dataFolderPath = _dataFolderPath.Replace('\\', '/');
            }
        }

        internal void SetLastObjectForType(string typeName, string guid)
        {
            if (string.IsNullOrEmpty(typeName))
                return;
            // Remove existing entry for this type first
            // Add new entry only if guid is valid
            if (!string.IsNullOrEmpty(guid))
            {
                InternalLastObjectSelections.RemoveAll(e => e.TypeName == typeName);
                InternalLastObjectSelections.Add(
                    new LastObjectSelectionEntry { TypeName = typeName, ObjectGuid = guid }
                );
            }
        }

        internal string GetLastObjectForType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;
            return InternalLastObjectSelections.Find(e => e.TypeName == typeName)?.ObjectGuid;
        }

        internal List<string> GetOrCreateTypeOrderList(string namespaceKey)
        {
            NamespaceTypeOrder entry = InternalTypeOrders.Find(o => o.NamespaceKey == namespaceKey);
            if (entry == null)
            {
                entry = new NamespaceTypeOrder { NamespaceKey = namespaceKey };
                InternalTypeOrders.Add(entry);
            }
            return entry.TypeNames;
        }

        // Helper method to find or create collapse state entry
        internal NamespaceCollapseState GetOrCreateCollapseState(string namespaceKey)
        {
            NamespaceCollapseState entry = InternalNamespaceCollapseStates.Find(o =>
                o.NamespaceKey == namespaceKey
            );
            if (entry == null)
            {
                entry = new NamespaceCollapseState
                {
                    NamespaceKey = namespaceKey,
                    IsCollapsed = false,
                }; // Default expanded
                InternalNamespaceCollapseStates.Add(entry);
            }
            return entry;
        }
    }
}

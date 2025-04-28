namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer
{
    using System;
    using System.Collections.Generic;
    using Data;

    [Serializable]
    public class UserStateTypeOrder
    {
        public string NamespaceKey;
        public List<string> TypeNames = new List<string>();
    }

    [Serializable]
    public class UserStateNamespaceCollapseState
    {
        public string NamespaceKey;
        public bool IsCollapsed;
    }

    [Serializable] // Make the main container serializable
    public class DataVisualizerUserState
    {
        // --- Versioning (Optional but recommended for future changes) ---
        public int Version = 1;

        // --- Last Selection ---
        public string LastSelectedNamespaceKey;
        public string LastSelectedTypeName;
        public string LastSelectedObjectGuid;

        // --- Ordering ---
        public List<string> NamespaceOrder = new List<string>();
        public List<UserStateTypeOrder> TypeOrders = new List<UserStateTypeOrder>();

        public List<LastObjectSelectionEntry> LastObjectSelections =
            new List<LastObjectSelectionEntry>();

        // --- UI State ---
        public List<UserStateNamespaceCollapseState> NamespaceCollapseStates =
            new List<UserStateNamespaceCollapseState>();

        // --- Helper Methods (similar to Settings object) ---

        public void SetLastObjectForType(string typeName, string guid)
        {
            if (string.IsNullOrEmpty(typeName))
                return;
            if (!string.IsNullOrEmpty(guid))
            {
                LastObjectSelections.RemoveAll(e => e.TypeName == typeName);
                LastObjectSelections.Add(
                    new LastObjectSelectionEntry { TypeName = typeName, ObjectGuid = guid }
                );
            }
        }

        public string GetLastObjectForType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;
            return LastObjectSelections.Find(e => e.TypeName == typeName)?.ObjectGuid;
        }

        public List<string> GetOrCreateTypeOrderList(string namespaceKey)
        {
            UserStateTypeOrder entry = TypeOrders.Find(o => o.NamespaceKey == namespaceKey);
            if (entry == null)
            {
                entry = new UserStateTypeOrder { NamespaceKey = namespaceKey };
                TypeOrders.Add(entry);
            }
            return entry.TypeNames;
        }

        public UserStateNamespaceCollapseState GetOrCreateCollapseState(string namespaceKey)
        {
            UserStateNamespaceCollapseState entry = NamespaceCollapseStates.Find(o =>
                o.NamespaceKey == namespaceKey
            );
            if (entry == null)
            {
                entry = new UserStateNamespaceCollapseState
                {
                    NamespaceKey = namespaceKey,
                    IsCollapsed = false,
                }; // Default expanded
                NamespaceCollapseStates.Add(entry);
            }
            return entry;
        }
    }
}

namespace UnityHelpers.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Attributes;
    using Core.DataVisualizer;
    using Core.Extension;
    using Core.Helper;
    using Core.Serialization;
    using UnityEditor;
    using UnityEngine;

#if UNITY_EDITOR
    public sealed class DataVisualizer : EditorWindow
    {
        private const string CustomTypeOrderKey =
            "WallstopStudios.UnityHelpers.DataVisualizer.CustomTypeOrder";
        private const string CustomNamespaceOrderKey =
            "WallstopStudios.UnityHelpers.DataVisualizer.CustomNamespaceOrder";

        private Vector2 _scrollPosition;

        private readonly List<(string key, List<Type> types)> _scriptableObjectTypes = new();

        [MenuItem("Tools/Unity Helpers/Data Visualizer")]
        public static void ShowWindow()
        {
            GetWindow<DataVisualizer>("Data Visualizer");
        }

        private void OnEnable()
        {
            LoadScriptableObjectTypes();
        }

        private void LoadScriptableObjectTypes()
        {
            if (0 < _scriptableObjectTypes.Count)
            {
                return;
            }

            foreach (Type type in TypeCache.GetTypesDerivedFrom<BaseDataObject>())
            {
                if (type.IsAbstract)
                {
                    continue;
                }

                string key = null;
                if (type.IsAttributeDefined(out DataVisualizerCustomPropertiesAttribute attribute))
                {
                    if (!string.IsNullOrWhiteSpace(attribute.Namespace))
                    {
                        key = attribute.Namespace;
                    }
                }

                if (string.IsNullOrWhiteSpace(key))
                {
                    key = type.Namespace?.Split(".").Last() ?? "No Namespace";
                }

                List<Type> types;
                int index = _scriptableObjectTypes.FindIndex(kvp =>
                    string.Equals(key, kvp.key, StringComparison.OrdinalIgnoreCase)
                );
                if (index < 0)
                {
                    types = new List<Type>();
                    _scriptableObjectTypes.Add((key, types));
                }
                else
                {
                    types = _scriptableObjectTypes[index].types;
                }
                types.Add(type);
            }

            List<string> customNamespaceOrder;
            try
            {
                string customOrderJson = EditorPrefs.GetString(CustomNamespaceOrderKey, "[]");
                customNamespaceOrder = Serializer.JsonDeserialize<List<string>>(customOrderJson);
            }
            catch (Exception e)
            {
                this.LogError($"Failed to load custom namespace order. Using default order.", e);
                customNamespaceOrder = new List<string>();
            }

            _scriptableObjectTypes.Sort(
                (lhs, rhs) =>
                {
                    int lhsIndex = customNamespaceOrder.IndexOf(lhs.key);
                    int rhsIndex = customNamespaceOrder.IndexOf(rhs.key);
                    if (0 <= lhsIndex && 0 <= rhsIndex)
                    {
                        return lhsIndex.CompareTo(rhsIndex);
                    }

                    return string.Compare(lhs.key, rhs.key, StringComparison.OrdinalIgnoreCase);
                }
            );

            foreach ((string key, List<Type> types) in _scriptableObjectTypes)
            {
                List<Type> customTypeOrder;
                try
                {
                    string customOrderJson = EditorPrefs.GetString(
                        CustomTypeOrderKey + $".{key}",
                        "[]"
                    );
                    customTypeOrder = Serializer.JsonDeserialize<List<Type>>(customOrderJson);
                }
                catch (Exception e)
                {
                    this.LogError($"Failed to load custom type order. Using default order.", e);
                    customTypeOrder = new List<Type>();
                }
                types.Sort(
                    (lhs, rhs) =>
                    {
                        int lhsIndex = customTypeOrder.IndexOf(lhs);
                        int rhsIndex = customTypeOrder.IndexOf(rhs);
                        if (0 <= lhsIndex && 0 <= rhsIndex)
                        {
                            return lhsIndex.CompareTo(rhsIndex);
                        }

                        return string.Compare(
                            lhs.Name,
                            rhs.Name,
                            StringComparison.OrdinalIgnoreCase
                        );
                    }
                );
            }
        }
    }
#endif
}

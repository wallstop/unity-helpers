namespace UnityHelpers.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Attributes;
    using Core.DataVisualizer;
    using Core.Extension;
    using Core.Helper;
    using UnityEditor;
    using UnityEngine;
    using UnityHelpers.Utils;

#if UNITY_EDITOR
    public sealed class DataVisualizer : EditorWindow
    {
        private Vector2 _scrollPosition;

        private readonly Dictionary<string, List<Type>> _scriptableObjectTypes = new();

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

                key ??= type.Namespace?.Split(".").Last() ?? "No Namespace";
                List<Type> types = _scriptableObjectTypes.GetOrAdd(key);
                types.Add(type);
            }

            foreach (List<Type> types in _scriptableObjectTypes.Values)
            {
                types.Sort(TypeNameSorter.Instance);
            }

            // TODO
        }
    }
#endif
}

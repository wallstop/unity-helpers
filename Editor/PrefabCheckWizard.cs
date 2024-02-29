namespace UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Core.Attributes;
    using UnityEditor;
    using UnityEngine;
    using Core.Extension;
    using Core.Helper;
    using Utils;
    using Object = UnityEngine.Object;

    public sealed class PrefabCheckWizard : ScriptableWizard
    {
        private static readonly Dictionary<Type, List<FieldInfo>> FieldsByType = new();

        [Tooltip("Drag a folder from Unity here to validate all prefabs under it. Defaults to Assets/Prefabs and Assets/Resources if none specified.")]
        public List<Object> assetPaths;

        [MenuItem("Tools/Unity Helpers/Prefab Check Wizard")]
        public static void CreatePrefabCheckWizard()
        {
            _ = DisplayWizard<PrefabCheckWizard>("Prefab sanity check", "Run");
        }

        private void OnWizardCreate()
        {
            List<string> parsedAssetPaths;
            if (assetPaths is { Count: > 0 })
            {
                parsedAssetPaths = assetPaths.Select(AssetDatabase.GetAssetPath).ToList();
                parsedAssetPaths.RemoveAll(string.IsNullOrEmpty);
                if (parsedAssetPaths.Count <= 0)
                {
                    parsedAssetPaths = null;
                }
            }
            else
            {
                parsedAssetPaths = null;
            }

            foreach (GameObject prefab in Helpers.EnumeratePrefabs(parsedAssetPaths))
            {
                List<MonoBehaviour> componentBuffer = Buffers<MonoBehaviour>.List;
                prefab.GetComponentsInChildren(true, componentBuffer);
                foreach (MonoBehaviour script in componentBuffer)
                {
                    if (!script)
                    {
                        Type scriptType = script?.GetType();
                        if (scriptType == null)
                        {
                            prefab.LogError("Detected missing script.");
                        }
                        else
                        {
                            prefab.LogError("Detected missing script for script type {0}.", scriptType);
                        }

                        continue;
                    }
                    ValidateNoNullsInLists(script);
                }
            }
        }

        private static void ValidateNoNullsInLists(Object component)
        {
            foreach (FieldInfo field in FieldsByType.GetOrAdd(
                         component.GetType(), type => type
                             .GetFields(BindingFlags.Instance | BindingFlags.Public)
                             .Concat(
                                 type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(
                                     field => field.GetCustomAttributes(typeof(SerializeField)).Any() ||
                                              field.GetCustomAttributes(typeof(ValidateAssignmentAttribute)).Any()))
                             .Where(field => typeof(IEnumerable).IsAssignableFrom(field.FieldType) || field.FieldType.IsArray)
                             .Where(field => !typeof(Transform).IsAssignableFrom(field.FieldType))
                             .Where(field => !typeof(Object).IsAssignableFrom(field.FieldType))
                             .ToList()))
            {
                bool LogIfNull(object thing, int? position = null)
                {
                    if (thing == null || (thing is Object unityThing && !unityThing))
                    {
                        if (position == null)
                        {
                            component.LogError("Field {0} has a null element in it.", field.Name);
                        }
                        else
                        {
                            component.LogError("Field {0} has a null element at position {1}.", field.Name, position);
                        }

                        return true;
                    }

                    return false;
                }

                object fieldValue = field.GetValue(component);

                if (field.FieldType.IsArray)
                {
                    if (fieldValue == null)
                    {
                        component.LogError("Field {0} (array) was null.", field.Name);
                        continue;
                    }

                    Array array = (Array)fieldValue;
                    foreach (object thing in array)
                    {
                        bool nullElement = LogIfNull(thing);
                        if (nullElement)
                        {
                            break;
                        }
                    }

                    continue;
                }

                if (fieldValue is not IEnumerable list)
                {
                    continue;
                }

                int position = 0;
                foreach (object thing in list)
                {
                    _ = LogIfNull(thing, position++);
                }
            }
        }
    }
#endif
}

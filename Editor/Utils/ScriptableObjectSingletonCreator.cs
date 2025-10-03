namespace WallstopStudios.UnityHelpers.Editor.Utils
{
#if UNITY_EDITOR
    using System;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [InitializeOnLoad]
    public static class ScriptableObjectSingletonCreator
    {
        private static bool IsTestAssembly(Type type)
        {
            string assemblyName = type.Assembly.GetName().Name;
            return !string.IsNullOrWhiteSpace(assemblyName)
                && assemblyName.Contains("Test", StringComparison.OrdinalIgnoreCase);
        }

        static ScriptableObjectSingletonCreator()
        {
            bool anyCreated = false;
            foreach (
                Type derivedType in TypeCache.GetTypesDerivedFrom(
                    typeof(UnityHelpers.Utils.ScriptableObjectSingleton<>)
                )
            )
            {
                if (
                    !derivedType.IsAbstract
                    && !derivedType.IsGenericType
                    && !IsTestAssembly(derivedType)
                )
                {
                    Object[] existing = Resources.LoadAll(string.Empty, derivedType);
                    if (existing.Length != 0)
                    {
                        continue;
                    }

                    if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    {
                        AssetDatabase.CreateFolder("Assets", "Resources");
                    }

                    ScriptableObject instance = ScriptableObject.CreateInstance(derivedType);
                    string assetPathName = AssetDatabase.GenerateUniqueAssetPath(
                        "Assets/Resources/" + derivedType.Name + ".asset"
                    );
                    AssetDatabase.CreateAsset(instance, assetPathName);
                    Debug.Log($"Creating missing singleton for type {derivedType.Name}.");
                    anyCreated = true;
                }
            }

            if (anyCreated)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
#endif
}

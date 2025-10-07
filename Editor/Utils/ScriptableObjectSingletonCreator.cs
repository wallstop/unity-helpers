namespace WallstopStudios.UnityHelpers.Editor.Utils
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using Object = UnityEngine.Object;

    [InitializeOnLoad]
    public static class ScriptableObjectSingletonCreator
    {
        private const string ResourcesRoot = "Assets/Resources";

        internal static bool IncludeTestAssemblies { get; set; }

        static ScriptableObjectSingletonCreator()
        {
            EnsureSingletonAssets();
        }

        internal static void EnsureSingletonAssets()
        {
            bool anyChanges = false;
            foreach (
                Type derivedType in TypeCache.GetTypesDerivedFrom(
                    typeof(UnityHelpers.Utils.ScriptableObjectSingleton<>)
                )
            )
            {
                if (
                    !derivedType.IsAbstract
                    && !derivedType.IsGenericType
                    && (IncludeTestAssemblies || !TestAssemblyHelper.IsTestType(derivedType))
                )
                {
                    EnsureFolderExists(ResourcesRoot);

                    string resourcesSubFolder = GetResourcesSubFolder(derivedType);
                    string targetFolder = CombinePaths(ResourcesRoot, resourcesSubFolder);
                    EnsureFolderExists(targetFolder);

                    string targetAssetPath = CombinePaths(
                        targetFolder,
                        derivedType.Name + ".asset"
                    );

                    Object assetAtTarget = AssetDatabase.LoadAssetAtPath(
                        targetAssetPath,
                        derivedType
                    );

                    if (assetAtTarget == null)
                    {
                        assetAtTarget = MoveExistingAssetIfNeeded(
                            derivedType,
                            targetAssetPath,
                            ref anyChanges
                        );
                    }

                    if (assetAtTarget != null)
                    {
                        continue;
                    }

                    ScriptableObject instance = ScriptableObject.CreateInstance(derivedType);
                    AssetDatabase.CreateAsset(instance, targetAssetPath);
                    Debug.Log(
                        $"Creating missing singleton for type {derivedType.Name} at {targetAssetPath}."
                    );
                    anyChanges = true;
                }
            }

            if (anyChanges)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static string GetResourcesSubFolder(Type type)
        {
            ScriptableSingletonPathAttribute attribute =
                type.GetCustomAttribute<ScriptableSingletonPathAttribute>();
            if (attribute == null)
            {
                return string.Empty;
            }

            string path = attribute.resourcesPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return path.SanitizePath().Trim('/');
        }

        private static Object MoveExistingAssetIfNeeded(
            Type type,
            string targetAssetPath,
            ref bool anyChanges
        )
        {
            string normalizedTarget = NormalizePath(targetAssetPath);
            string assetName = Path.GetFileName(targetAssetPath);

            HashSet<string> seenPaths = new(StringComparer.OrdinalIgnoreCase);
            List<string> candidatePaths = new();

            string[] guids = AssetDatabase.FindAssets("t:" + type.Name);
            if (guids != null && guids.Length != 0)
            {
                foreach (string guid in guids)
                {
                    AddCandidate(AssetDatabase.GUIDToAssetPath(guid));
                }
            }

            Object[] resourceInstances = Resources.LoadAll(string.Empty, type);
            if (resourceInstances != null)
            {
                foreach (Object instance in resourceInstances)
                {
                    if (instance == null)
                    {
                        continue;
                    }

                    string assetPath = AssetDatabase.GetAssetPath(instance);
                    AddCandidate(assetPath);
                }
            }

            if (seenPaths.Contains(normalizedTarget))
            {
                return AssetDatabase.LoadAssetAtPath(normalizedTarget, type);
            }

            foreach (string alternatePath in candidatePaths)
            {
                if (
                    string.Equals(
                        alternatePath,
                        normalizedTarget,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    continue;
                }

                Object asset = AssetDatabase.LoadAssetAtPath(alternatePath, type);
                if (asset == null)
                {
                    continue;
                }

                string moveResult = AssetDatabase.MoveAsset(alternatePath, normalizedTarget);
                if (string.IsNullOrEmpty(moveResult))
                {
                    Debug.Log(
                        $"Relocated singleton asset for type {type.Name} from {alternatePath} to {normalizedTarget}."
                    );
                    anyChanges = true;
                    return asset;
                }

                Debug.LogWarning(
                    $"Failed to move singleton asset {assetName} for type {type.Name} from {alternatePath}: {moveResult}"
                );
            }

            return null;

            void AddCandidate(string rawPath)
            {
                if (string.IsNullOrWhiteSpace(rawPath))
                {
                    return;
                }

                string normalized = NormalizePath(rawPath);
                if (seenPaths.Add(normalized))
                {
                    candidatePaths.Add(normalized);
                }
            }
        }

        private static string CombinePaths(string left, string right)
        {
            if (string.IsNullOrEmpty(right))
            {
                return NormalizePath(left);
            }

            return NormalizePath(Path.Combine(left, right));
        }

        private static string NormalizePath(string path)
        {
            return PathHelper.Sanitize(path);
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            folderPath = NormalizePath(folderPath);
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return;
            }

            string current = parts[0];
            if (!string.Equals(current, "Assets", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning(
                    $"Unable to ensure folder for path '{folderPath}' because it does not start with 'Assets'."
                );
                return;
            }

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
#endif
}

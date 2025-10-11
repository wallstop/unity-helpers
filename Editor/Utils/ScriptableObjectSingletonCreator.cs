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

        // Prevents re-entrant execution during domain reloads/asset refreshes
        private static bool _isEnsuring;

        // Controls whether informational logs are emitted. Warnings still always log.
        internal static bool VerboseLogging { get; set; }

        internal static bool IncludeTestAssemblies { get; set; }

        static ScriptableObjectSingletonCreator()
        {
            EnsureSingletonAssets();
        }

        internal static void EnsureSingletonAssets()
        {
            if (_isEnsuring)
            {
                LogVerbose(
                    "ScriptableObjectSingletonCreator: EnsureSingletonAssets re-entrancy prevented."
                );
                return;
            }

            _isEnsuring = true;
            AssetDatabase.StartAssetEditing();
            bool anyChanges = false;
            try
            {
                // Collect candidate types once and detect simple name collisions (same class name, different namespaces)
                List<Type> allCandidates = new();
                foreach (
                    Type t in WallstopStudios.UnityHelpers.Core.Helper.ReflectionHelpers.GetTypesDerivedFrom(
                        typeof(UnityHelpers.Utils.ScriptableObjectSingleton<>),
                        includeAbstract: false
                    )
                )
                {
                    if (
                        !t.IsGenericType
                        && (IncludeTestAssemblies || !TestAssemblyHelper.IsTestType(t))
                    )
                    {
                        allCandidates.Add(t);
                    }
                }

                // Build collision map by simple type name
                Dictionary<string, List<Type>> byName = new(StringComparer.OrdinalIgnoreCase);
                foreach (Type t in allCandidates)
                {
                    if (!byName.TryGetValue(t.Name, out List<Type> list))
                    {
                        list = new List<Type>();
                        byName[t.Name] = list;
                    }
                    list.Add(t);
                }

                HashSet<string> collisionLogged = new(StringComparer.OrdinalIgnoreCase);

                foreach (Type derivedType in allCandidates)
                {
                    // Skip name-collision types to avoid creating overlapping assets like TypeName.asset
                    if (
                        byName.TryGetValue(derivedType.Name, out List<Type> group)
                        && group.Count > 1
                    )
                    {
                        if (collisionLogged.Add(derivedType.Name))
                        {
                            Debug.LogWarning(
                                $"ScriptableObjectSingletonCreator: Type name collision detected for '{derivedType.Name}'. Conflicting types: {string.Join(", ", group.ConvertAll(x => x.FullName))}. Skipping auto-creation. Consider adding [ScriptableSingletonPath] to disambiguate."
                            );
                        }
                        continue;
                    }

                    EnsureFolderExists(ResourcesRoot);

                    string resourcesSubFolder = GetResourcesSubFolder(derivedType);
                    string targetFolder = CombinePaths(ResourcesRoot, resourcesSubFolder);
                    EnsureFolderExists(targetFolder);

                    string targetAssetPath = CombinePaths(
                        targetFolder,
                        derivedType.Name + ".asset"
                    );

                    // Extra safety: if any asset exists at the exact path, do not create a duplicate.
                    // Prefer to use/move existing assets rather than generating unique names.
                    string existingGuid = null;
                    Object existingAtPath = AssetDatabase.LoadAssetAtPath<Object>(targetAssetPath);
                    if (existingAtPath != null)
                    {
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                            existingAtPath,
                            out existingGuid,
                            out long _
                        );
                    }

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

                    // If something already exists at the target path (even of another type), avoid creating another asset.
                    if (!string.IsNullOrEmpty(existingGuid))
                    {
                        Debug.LogWarning(
                            $"ScriptableObjectSingletonCreator: Singleton target path already occupied at {targetAssetPath}. Skipping creation for {derivedType.FullName}."
                        );
                        continue;
                    }

                    ScriptableObject instance = ScriptableObject.CreateInstance(derivedType);
                    AssetDatabase.CreateAsset(instance, targetAssetPath);
                    LogVerbose(
                        $"ScriptableObjectSingletonCreator: Created missing singleton for type {derivedType.FullName} at {targetAssetPath}."
                    );
                    anyChanges = true;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                _isEnsuring = false;

                if (anyChanges)
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
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

            return path.SanitizePath()?.Trim().Trim('/');
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
                    LogVerbose(
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
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            // Trim whitespace and normalize separators
            string normalized = PathHelper.Sanitize(path.Trim());

            // Collapse duplicate separators
            while (normalized.Contains("//"))
            {
                normalized = normalized.Replace("//", "/");
            }

            // Remove trailing separator (except for bare "Assets")
            if (
                normalized.EndsWith("/")
                && !string.Equals(normalized, "Assets", StringComparison.Ordinal)
            )
            {
                normalized = normalized.TrimEnd('/');
            }

            return normalized;
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

            // Always anchor to Unity's "Assets" root with correct casing
            string current = "Assets";
            if (!string.Equals(parts[0], current, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning(
                    $"Unable to ensure folder for path '{folderPath}' because it does not start with 'Assets'."
                );
                return;
            }

            for (int i = 1; i < parts.Length; i++)
            {
                string desiredName = parts[i];

                // Find an existing subfolder that matches the desired segment, ignoring case
                string[] subFolders = AssetDatabase.GetSubFolders(current);
                string matchedExisting = null;
                if (subFolders != null)
                {
                    foreach (string sub in subFolders)
                    {
                        // sub is like "Assets/Resources" — compare only the terminal name
                        int lastSlash = sub.LastIndexOf('/', sub.Length - 1);
                        string terminal = lastSlash >= 0 ? sub.Substring(lastSlash + 1) : sub;
                        if (
                            string.Equals(terminal, desiredName, StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            matchedExisting = sub;
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(matchedExisting))
                {
                    AssetDatabase.CreateFolder(current, desiredName);
                    current = current + "/" + desiredName;
                    LogVerbose($"ScriptableObjectSingletonCreator: Created folder '{current}'.");
                }
                else
                {
                    if (
                        !string.Equals(
                            matchedExisting,
                            current + "/" + desiredName,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        LogVerbose(
                            $"ScriptableObjectSingletonCreator: Reusing existing folder '{matchedExisting}' for requested segment '{desiredName}'."
                        );
                    }
                    current = matchedExisting; // reuse existing folder even if case differs
                }
            }
        }

        private static void LogVerbose(string message)
        {
            if (VerboseLogging)
            {
                Debug.Log(message);
            }
        }
    }
#endif
}

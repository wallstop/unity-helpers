namespace WallstopStudios.UnityHelpers.Editor.Utils
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
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
        private static int _assetEditingScopeDepth;

        // Controls whether informational logs are emitted. Warnings still always log.
        internal static bool VerboseLogging { get; set; }

        internal static bool IncludeTestAssemblies { get; set; }

        // Optional hook so tests can restrict the candidate singleton types that auto-creation processes.
        internal static Func<Type, bool> TypeFilter { get; set; }

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
            _assetEditingScopeDepth++;
            bool anyChanges = false;
            try
            {
                // Collect candidate types once and detect simple name collisions (same class name, different namespaces)
                List<Type> allCandidates = new();
                foreach (
                    Type t in ReflectionHelpers.GetTypesDerivedFrom(
                        typeof(UnityHelpers.Utils.ScriptableObjectSingleton<>),
                        includeAbstract: false
                    )
                )
                {
                    if (
                        !t.IsGenericType
                        && (IncludeTestAssemblies || !TestAssemblyHelper.IsTestType(t))
                        && (TypeFilter == null || TypeFilter(t))
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

                    string resolvedResourcesRoot = EnsureAndResolveFolderPath(ResourcesRoot);
                    if (string.IsNullOrWhiteSpace(resolvedResourcesRoot))
                    {
                        Debug.LogError(
                            "ScriptableObjectSingletonCreator: Unable to resolve required Resources root folder. Aborting singleton auto-creation."
                        );
                        break;
                    }

                    string resourcesSubFolder = GetResourcesSubFolder(derivedType);
                    string targetFolderRequested = CombinePaths(ResourcesRoot, resourcesSubFolder);
                    string targetFolder = EnsureAndResolveFolderPath(targetFolderRequested);
                    if (string.IsNullOrWhiteSpace(targetFolder))
                    {
                        Debug.LogError(
                            $"ScriptableObjectSingletonCreator: Unable to ensure folder '{targetFolderRequested}' for singleton {derivedType.FullName}. Skipping asset creation."
                        );
                        continue;
                    }

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
                if (_assetEditingScopeDepth > 0)
                {
                    _assetEditingScopeDepth--;
                }
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
            if (
                !ReflectionHelpers.TryGetAttributeSafe<ScriptableSingletonPathAttribute>(
                    type,
                    out ScriptableSingletonPathAttribute attribute,
                    inherit: false
                )
            )
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

            // Ensure the target parent folder exists and use its exact-cased path
            string targetParent = Path.GetDirectoryName(normalizedTarget)?.SanitizePath();
            if (!string.IsNullOrWhiteSpace(targetParent))
            {
                string resolvedParent = EnsureAndResolveFolderPath(targetParent);
                if (!string.IsNullOrWhiteSpace(resolvedParent))
                {
                    string rebuilt = CombinePaths(resolvedParent, assetName);
                    normalizedTarget = rebuilt;
                }
            }

            HashSet<string> seenPaths = new(StringComparer.OrdinalIgnoreCase);
            List<string> candidatePaths = new();

            // Try finding by script name (won't work for nested/private classes but harmless to try)
            string[] guids = AssetDatabase.FindAssets("t:" + type.Name);
            if (guids != null && guids.Length != 0)
            {
                foreach (string guid in guids)
                {
                    AddCandidate(AssetDatabase.GUIDToAssetPath(guid));
                }
            }

            // Load from Resources - this works for already-indexed assets
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

            // Search only within Assets/Resources folder for ScriptableObject assets of this type
            // This is more targeted than searching all assets and catches newly created test assets
            string[] resourceGuids = AssetDatabase.FindAssets(
                "t:ScriptableObject",
                new[] { ResourcesRoot }
            );
            if (resourceGuids != null)
            {
                foreach (string guid in resourceGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }

                    Object obj = AssetDatabase.LoadAssetAtPath(path, type);
                    if (obj != null)
                    {
                        AddCandidate(path);
                    }
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

                // Final guard: ensure parent exists just before moving
                string parent = Path.GetDirectoryName(normalizedTarget)?.SanitizePath();
                if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
                {
                    string ensured = EnsureAndResolveFolderPath(parent);
                    if (!string.IsNullOrWhiteSpace(ensured))
                    {
                        normalizedTarget = CombinePaths(ensured, assetName);
                    }
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

                // Retry after ensuring parent and performing save/refresh if parent folder may not yet be registered
                string parentDir = Path.GetDirectoryName(normalizedTarget)?.SanitizePath();
                bool retried = false;
                if (!string.IsNullOrWhiteSpace(parentDir))
                {
                    // Ensure parent folder exists and get its resolved path
                    string resolvedParent = EnsureAndResolveFolderPath(parentDir);
                    if (!string.IsNullOrWhiteSpace(resolvedParent))
                    {
                        normalizedTarget = CombinePaths(resolvedParent, assetName);
                        parentDir = resolvedParent;
                    }

                    // Temporarily stop asset editing to save and refresh
                    AssetDatabase.StopAssetEditing();
                    try
                    {
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                    finally
                    {
                        AssetDatabase.StartAssetEditing();
                    }

                    // Verify parent folder is now valid in AssetDatabase
                    if (AssetDatabase.IsValidFolder(parentDir))
                    {
                        string retry = AssetDatabase.MoveAsset(alternatePath, normalizedTarget);
                        retried = true;
                        if (string.IsNullOrEmpty(retry))
                        {
                            LogVerbose(
                                $"Relocated singleton asset for type {type.Name} from {alternatePath} to {normalizedTarget} after refresh."
                            );
                            anyChanges = true;
                            return asset;
                        }
                        else
                        {
                            moveResult = retry;
                        }
                    }
                    else
                    {
                        retried = true;
                        moveResult = $"Parent directory is not in asset database (after retry)";
                    }
                }

                Debug.LogWarning(
                    $"Failed to move singleton asset {assetName} for type {type.Name} from {alternatePath}: {moveResult}{(retried ? " (after retry)" : string.Empty)}"
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

        private static string EnsureAndResolveFolderPath(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return string.Empty;
            }

            folderPath = NormalizePath(folderPath);
            string[] parts = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return string.Empty;
            }

            // If the whole folder already exists, return the exact casing Unity knows
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                // Resolve to exact-cased path by walking down from Assets
                return ResolveExistingFolderPath(folderPath);
            }

            // Always anchor to Unity's "Assets" root with correct casing
            string current = "Assets";
            if (!string.Equals(parts[0], current, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning(
                    $"Unable to ensure folder for path '{folderPath}' because it does not start with 'Assets'."
                );
                return folderPath;
            }

            for (int i = 1; i < parts.Length; i++)
            {
                string desiredName = parts[i];

                string matchedExisting = FindMatchingSubfolder(current, desiredName);

                if (string.IsNullOrEmpty(matchedExisting))
                {
                    string intendedPath = current + "/" + desiredName;
                    string createdGuid = AssetDatabase.CreateFolder(current, desiredName);
                    string createdPath = string.Empty;
                    if (!string.IsNullOrEmpty(createdGuid))
                    {
                        createdPath = NormalizePath(AssetDatabase.GUIDToAssetPath(createdGuid));
                    }

                    string actualPath = FindMatchingSubfolder(current, desiredName);
                    if (string.IsNullOrEmpty(actualPath))
                    {
                        actualPath = createdPath;
                    }

                    bool intendedValid = AssetDatabase.IsValidFolder(intendedPath);
                    bool actualValid =
                        !string.IsNullOrEmpty(actualPath)
                        && AssetDatabase.IsValidFolder(actualPath);

                    if (!intendedValid && !actualValid)
                    {
                        bool directoryExists =
                            Directory.Exists(intendedPath)
                            || (!string.IsNullOrEmpty(actualPath) && Directory.Exists(actualPath));
                        if (directoryExists || !string.IsNullOrEmpty(createdGuid))
                        {
                            ForceAssetDatabaseSync();
                        }

                        intendedValid = AssetDatabase.IsValidFolder(intendedPath);
                        if (!intendedValid)
                        {
                            actualPath = FindMatchingSubfolder(current, desiredName);
                            if (
                                string.IsNullOrEmpty(actualPath)
                                && !string.IsNullOrEmpty(createdGuid)
                            )
                            {
                                actualPath = NormalizePath(
                                    AssetDatabase.GUIDToAssetPath(createdGuid)
                                );
                            }

                            actualValid =
                                !string.IsNullOrEmpty(actualPath)
                                && AssetDatabase.IsValidFolder(actualPath);
                        }
                        else
                        {
                            actualPath = intendedPath;
                            actualValid = true;
                        }
                    }

                    if (intendedValid)
                    {
                        current = ResolveExistingFolderPath(intendedPath);
                        LogVerbose(
                            $"ScriptableObjectSingletonCreator: Created folder '{current}'."
                        );
                        continue;
                    }

                    if (
                        actualValid
                        && string.Equals(
                            actualPath,
                            intendedPath,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        string renameError = AssetDatabase.MoveAsset(actualPath, intendedPath);
                        if (string.IsNullOrEmpty(renameError))
                        {
                            LogVerbose(
                                $"ScriptableObjectSingletonCreator: Renamed folder '{actualPath}' to '{intendedPath}' to correct casing."
                            );
                            current = ResolveExistingFolderPath(intendedPath);
                            continue;
                        }

                        LogVerbose(
                            $"ScriptableObjectSingletonCreator: Reusing newly created folder '{actualPath}' when casing correction to '{intendedPath}' failed: {renameError}."
                        );
                        current = ResolveExistingFolderPath(actualPath);
                        continue;
                    }

                    if (actualValid && AssetDatabase.IsValidFolder(actualPath))
                    {
                        bool deleted = AssetDatabase.DeleteAsset(actualPath);
                        if (!deleted)
                        {
                            Debug.LogWarning(
                                $"ScriptableObjectSingletonCreator: Unexpected folder '{actualPath}' was created while attempting to create '{intendedPath}', but it could not be removed."
                            );
                        }

                        Debug.LogError(
                            $"ScriptableObjectSingletonCreator: Expected to create folder '{intendedPath}', but Unity created '{actualPath}'. Aborting to avoid duplicate folders."
                        );
                        return string.Empty;
                    }

                    Debug.LogError(
                        $"ScriptableObjectSingletonCreator: Failed to create folder '{intendedPath}'."
                    );
                    return string.Empty;
                }
                else
                {
                    string intendedPath = current + "/" + desiredName;
                    if (string.Equals(matchedExisting, intendedPath, StringComparison.Ordinal))
                    {
                        // Exact match, just continue
                        current = matchedExisting;
                    }
                    else
                    {
                        // Case-insensitive match with different casing. Attempt to rename to the intended casing
                        string renameError = AssetDatabase.MoveAsset(matchedExisting, intendedPath);
                        if (string.IsNullOrEmpty(renameError))
                        {
                            LogVerbose(
                                $"ScriptableObjectSingletonCreator: Renamed folder '{matchedExisting}' to '{intendedPath}' to correct casing."
                            );
                            current = intendedPath;
                        }
                        else
                        {
                            // Some platforms/filesystems require a two-step rename to change only casing
                            // Attempt rename -> temp -> intended when paths differ only by case
                            string currentTerminal = matchedExisting;
                            int ls = currentTerminal.LastIndexOf('/', currentTerminal.Length - 1);
                            currentTerminal =
                                ls >= 0 ? currentTerminal.Substring(ls + 1) : currentTerminal;

                            if (
                                string.Equals(
                                    currentTerminal,
                                    desiredName,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            {
                                string tempName = desiredName + "__CaseFix__";
                                string tempPath = current + "/" + tempName;
                                string toTempErr = AssetDatabase.MoveAsset(
                                    matchedExisting,
                                    tempPath
                                );
                                if (string.IsNullOrEmpty(toTempErr))
                                {
                                    string toFinalErr = AssetDatabase.MoveAsset(
                                        tempPath,
                                        intendedPath
                                    );
                                    if (string.IsNullOrEmpty(toFinalErr))
                                    {
                                        LogVerbose(
                                            $"ScriptableObjectSingletonCreator: Renamed folder '{matchedExisting}' to '{intendedPath}' via temporary '{tempPath}' to correct casing."
                                        );
                                        current = intendedPath;
                                    }
                                    else
                                    {
                                        LogVerbose(
                                            $"ScriptableObjectSingletonCreator: Reusing existing folder '{matchedExisting}' for requested segment '{desiredName}' (final case-fix rename failed: {toFinalErr})."
                                        );
                                        current = matchedExisting;
                                    }
                                }
                                else
                                {
                                    LogVerbose(
                                        $"ScriptableObjectSingletonCreator: Reusing existing folder '{matchedExisting}' for requested segment '{desiredName}' (case-fix temp rename failed: {toTempErr})."
                                    );
                                    current = matchedExisting;
                                }
                            }
                            else
                            {
                                // If mismatch isn't purely casing, fall back to using existing
                                LogVerbose(
                                    $"ScriptableObjectSingletonCreator: Reusing existing folder '{matchedExisting}' for requested segment '{desiredName}' (rename failed: {renameError})."
                                );
                                current = matchedExisting;
                            }
                        }
                    }
                }
            }

            return current;
        }

        private static string FindMatchingSubfolder(string parent, string desiredName)
        {
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(desiredName))
            {
                return null;
            }

            string[] subFolders = AssetDatabase.GetSubFolders(parent);
            if (subFolders == null || subFolders.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < subFolders.Length; i++)
            {
                string sub = subFolders[i];
                int lastSlash = sub.LastIndexOf('/', sub.Length - 1);
                string terminal = lastSlash >= 0 ? sub.Substring(lastSlash + 1) : sub;
                if (string.Equals(terminal, desiredName, StringComparison.OrdinalIgnoreCase))
                {
                    return sub;
                }
            }

            return null;
        }

        private static string ResolveExistingFolderPath(string intended)
        {
            if (string.IsNullOrWhiteSpace(intended))
            {
                return string.Empty;
            }

            intended = NormalizePath(intended);
            string[] parts = intended.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return string.Empty;
            }

            string current = parts[0];
            if (!string.Equals(current, "Assets", StringComparison.OrdinalIgnoreCase))
            {
                return intended;
            }

            for (int i = 1; i < parts.Length; i++)
            {
                string desired = parts[i];
                string next = current + "/" + desired;
                if (AssetDatabase.IsValidFolder(next))
                {
                    current = next;
                    continue;
                }

                string[] subs = AssetDatabase.GetSubFolders(current);
                if (subs == null || subs.Length == 0)
                {
                    return intended;
                }

                string match = null;
                for (int s = 0; s < subs.Length; s++)
                {
                    string sub = subs[s];
                    int last = sub.LastIndexOf('/', sub.Length - 1);
                    string name = last >= 0 ? sub.Substring(last + 1) : sub;
                    if (string.Equals(name, desired, StringComparison.OrdinalIgnoreCase))
                    {
                        match = sub;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(match))
                {
                    return intended;
                }

                current = match;
            }

            return current;
        }

        private static void ForceAssetDatabaseSync()
        {
            if (_assetEditingScopeDepth > 0)
            {
                AssetDatabase.StopAssetEditing();
                try
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                finally
                {
                    AssetDatabase.StartAssetEditing();
                }
            }
            else
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
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

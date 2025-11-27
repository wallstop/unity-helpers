namespace WallstopStudios.UnityHelpers.Editor.Utils
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;
    using Debug = UnityEngine.Debug;
    using Object = UnityEngine.Object;

    [InitializeOnLoad]
    public static class ScriptableObjectSingletonCreator
    {
        private const string ResourcesRoot = "Assets/Resources";
        private const string AssetImportWorkerEnvVar = "UNITY_ASSET_IMPORT_WORKER";
        private const string LegacyAssetImportWorkerEnvVar = "UNITY_ASSETIMPORT_WORKER";
        private const int MaxRetryAttempts = 5;

        // Prevents re-entrant execution during domain reloads/asset refreshes
        private static bool _isEnsuring;
        private static int _assetEditingScopeDepth;
        private static bool _ensureScheduled;
        private static int _retryAttempts;
        private static bool? _assetImportWorkerEnvCachedValue;
        private static Func<bool> _defaultAssetImportWorkerDetector;
        private static bool _mainThreadConfirmed;
        private static bool _mainThreadConfirmationPending;
        private static int _capturedMainThreadId;

        // Controls whether informational logs are emitted. Warnings still always log.
        internal static bool VerboseLogging { get; set; }

        internal static bool IncludeTestAssemblies { get; set; }
        internal static bool DisableAutomaticRetries { get; set; }
        internal static Func<bool> AssetImportWorkerProcessCheck { get; set; }

        // Optional hook so tests can restrict the candidate singleton types that auto-creation processes.
        internal static Func<Type, bool> TypeFilter { get; set; }

        static ScriptableObjectSingletonCreator()
        {
            EnsureSingletonAssets();
        }

        internal static void EnsureSingletonAssets()
        {
            CancelScheduledEnsureInvocation();

            if (IsRunningInsideAssetImportWorkerProcess())
            {
                if (_mainThreadConfirmationPending)
                {
                    ScheduleEnsureSingletonAssets();
                    return;
                }

                LogVerbose(
                    "ScriptableObjectSingletonCreator: Skipping ensure while running inside asset import worker process."
                );
                return;
            }

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
            bool retryRequested = false;
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
                        retryRequested = true;
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
                        retryRequested = true;
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
                        if (UpdateSingletonMetadataEntry(derivedType, targetAssetPath))
                        {
                            anyChanges = true;
                        }
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
                    if (UpdateSingletonMetadataEntry(derivedType, targetAssetPath))
                    {
                        anyChanges = true;
                    }
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

                if (retryRequested && !DisableAutomaticRetries)
                {
                    ScheduleEnsureSingletonAssets();
                }
                else
                {
                    _retryAttempts = 0;
                }
            }
        }

        private static void ScheduleEnsureSingletonAssets()
        {
            if (_ensureScheduled)
            {
                return;
            }

            if (_retryAttempts >= MaxRetryAttempts)
            {
                Debug.LogWarning(
                    "ScriptableObjectSingletonCreator: Maximum automatic retry attempts reached. Further retries are suppressed to avoid infinite loops."
                );
                return;
            }

            _retryAttempts++;

            _ensureScheduled = true;
            EditorApplication.delayCall += RunScheduledEnsure;
        }

        private static void RunScheduledEnsure()
        {
            EditorApplication.delayCall -= RunScheduledEnsure;
            _ensureScheduled = false;
            EnsureSingletonAssets();
        }

        private static void CancelScheduledEnsureInvocation()
        {
            if (!_ensureScheduled)
            {
                return;
            }

            EditorApplication.delayCall -= RunScheduledEnsure;
            _ensureScheduled = false;

            if (_retryAttempts > 0)
            {
                _retryAttempts--;
            }
        }

        private static string GetResourcesSubFolder(Type type)
        {
            if (
                !ReflectionHelpers.TryGetAttributeSafe(
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

                        moveResult = retry;
                    }
                    else
                    {
                        retried = true;
                        moveResult = "Parent directory is not in asset database (after retry)";
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

        private static bool UpdateSingletonMetadataEntry(Type type, string assetPath)
        {
            string loadPath = ToResourcesLoadPath(assetPath);
            if (string.IsNullOrEmpty(loadPath))
            {
                return false;
            }

            string resourcesFolder = GetResourcesFolderFromLoadPath(loadPath);
            string guid = AssetDatabase.AssetPathToGUID(assetPath) ?? string.Empty;
            ScriptableObjectSingletonMetadataUtility.UpdateEntry(
                type,
                loadPath,
                resourcesFolder,
                guid
            );
            return true;
        }

        private static string ToResourcesLoadPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return null;
            }

            string normalized = NormalizePath(assetPath);
            if (!normalized.StartsWith(ResourcesRoot, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string relative = normalized.Substring(ResourcesRoot.Length).TrimStart('/');
            if (string.IsNullOrWhiteSpace(relative))
            {
                return null;
            }

            if (relative.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                relative = relative.Substring(0, relative.Length - ".asset".Length);
            }

            return relative.Replace("\\", "/");
        }

        private static string GetResourcesFolderFromLoadPath(string loadPath)
        {
            if (string.IsNullOrWhiteSpace(loadPath))
            {
                return string.Empty;
            }

            string directory = Path.GetDirectoryName(loadPath);
            if (string.IsNullOrEmpty(directory))
            {
                return string.Empty;
            }

            return directory.Replace("\\", "/");
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

        private static string TryGetAbsoluteAssetsPath(string assetsRelativePath)
        {
            if (string.IsNullOrWhiteSpace(assetsRelativePath))
            {
                return string.Empty;
            }

            string normalized = NormalizePath(assetsRelativePath);
            if (!normalized.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
            {
                return string.Empty;
            }

            string combined = Path.Combine(projectRoot, normalized);
            return Path.GetFullPath(combined);
        }

        private static bool ProjectDirectoryExists(string assetsRelativePath)
        {
            string absolutePath = TryGetAbsoluteAssetsPath(assetsRelativePath);
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return false;
            }

            return Directory.Exists(absolutePath);
        }

        private static bool EnsureFolderExistsOnDisk(string assetsRelativePath)
        {
            string absolutePath = TryGetAbsoluteAssetsPath(assetsRelativePath);
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(absolutePath);
                if (!Directory.Exists(absolutePath))
                {
                    return false;
                }

                return RegisterFolderWithAssetDatabase(assetsRelativePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"ScriptableObjectSingletonCreator: Directory.CreateDirectory fallback failed for '{assetsRelativePath}': {ex.Message}"
                );
                return false;
            }
        }

        private static bool RegisterFolderWithAssetDatabase(string assetsRelativePath)
        {
            string normalized = NormalizePath(assetsRelativePath);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            if (AssetDatabase.IsValidFolder(normalized))
            {
                return true;
            }

            bool restartEditing = false;
            if (_assetEditingScopeDepth > 0)
            {
                AssetDatabase.StopAssetEditing();
                restartEditing = true;
            }

            try
            {
                AssetDatabase.ImportAsset(normalized, ImportAssetOptions.ForceUpdate);
                if (AssetDatabase.IsValidFolder(normalized))
                {
                    return true;
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"ScriptableObjectSingletonCreator: Failed to register folder '{normalized}' with AssetDatabase: {ex.Message}"
                );
            }
            finally
            {
                if (restartEditing)
                {
                    AssetDatabase.StartAssetEditing();
                }
            }

            return AssetDatabase.IsValidFolder(normalized);
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
                    if (ProjectDirectoryExists(intendedPath))
                    {
                        if (EnsureFolderExistsOnDisk(intendedPath))
                        {
                            current = ResolveExistingFolderPath(intendedPath);
                            LogVerbose(
                                $"ScriptableObjectSingletonCreator: Registered existing folder '{current}'."
                            );
                            continue;
                        }
                    }

                    string createdGuid = string.Empty;
                    string createdPath = string.Empty;
                    try
                    {
                        createdGuid = AssetDatabase.CreateFolder(current, desiredName);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            $"ScriptableObjectSingletonCreator: AssetDatabase.CreateFolder threw while ensuring '{intendedPath}'. Falling back to disk creation. {ex.Message}"
                        );
                    }

                    if (!string.IsNullOrEmpty(createdGuid))
                    {
                        createdPath = NormalizePath(AssetDatabase.GUIDToAssetPath(createdGuid));
                    }
                    else if (EnsureFolderExistsOnDisk(intendedPath))
                    {
                        createdPath = intendedPath;
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
                            ProjectDirectoryExists(intendedPath)
                            || (
                                !string.IsNullOrEmpty(actualPath)
                                && ProjectDirectoryExists(actualPath)
                            );
                        if (directoryExists)
                        {
                            RegisterFolderWithAssetDatabase(intendedPath);
                            if (!string.IsNullOrEmpty(actualPath))
                            {
                                RegisterFolderWithAssetDatabase(actualPath);
                            }
                        }
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

                            if (string.IsNullOrEmpty(actualPath) && directoryExists)
                            {
                                actualPath = intendedPath;
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

                        string lastError = renameError;
                        string currentTerminal = actualPath;
                        int ls = currentTerminal.LastIndexOf('/', currentTerminal.Length - 1);
                        currentTerminal =
                            ls >= 0 ? currentTerminal.Substring(ls + 1) : currentTerminal;
                        string desiredTerminal = desiredName;

                        if (
                            string.Equals(
                                currentTerminal,
                                desiredTerminal,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            string tempName = desiredTerminal + "__CaseFix__";
                            string tempPath = current + "/" + tempName;
                            string toTempErr = AssetDatabase.MoveAsset(actualPath, tempPath);
                            if (string.IsNullOrEmpty(toTempErr))
                            {
                                string toFinalErr = AssetDatabase.MoveAsset(tempPath, intendedPath);
                                if (string.IsNullOrEmpty(toFinalErr))
                                {
                                    LogVerbose(
                                        $"ScriptableObjectSingletonCreator: Renamed folder '{actualPath}' to '{intendedPath}' via temporary '{tempPath}' to correct casing."
                                    );
                                    current = ResolveExistingFolderPath(intendedPath);
                                    continue;
                                }

                                lastError = toFinalErr;
                            }
                            else
                            {
                                lastError = toTempErr;
                            }
                        }

                        DeleteCreatedFolder(actualPath, createdGuid);
                        Debug.LogError(
                            $"ScriptableObjectSingletonCreator: Unable to correct folder casing from '{actualPath}' to '{intendedPath}' (last error: {lastError})."
                        );
                        return string.Empty;
                    }

                    if (actualValid && AssetDatabase.IsValidFolder(actualPath))
                    {
                        DeleteCreatedFolder(actualPath, createdGuid);
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

            foreach (string sub in subFolders)
            {
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
                foreach (string sub in subs)
                {
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
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
                finally
                {
                    AssetDatabase.StartAssetEditing();
                }
            }
            else
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

        private static void DeleteCreatedFolder(string candidatePath, string createdGuid)
        {
            string path = candidatePath;
            if (string.IsNullOrWhiteSpace(path) && !string.IsNullOrEmpty(createdGuid))
            {
                path = NormalizePath(AssetDatabase.GUIDToAssetPath(createdGuid));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            if (!AssetDatabase.DeleteAsset(path))
            {
                Debug.LogWarning(
                    $"ScriptableObjectSingletonCreator: Temporary folder '{path}' was created while attempting to ensure a target path, but it could not be removed."
                );
            }
        }

        private static bool IsRunningInsideAssetImportWorkerProcess()
        {
            Func<bool> detectorOverride = AssetImportWorkerProcessCheck;
            if (detectorOverride != null)
            {
                _mainThreadConfirmationPending = false;
                return InvokeDetector(detectorOverride, assumeWorkerOnFailure: false);
            }

            if (IsAssetImportWorkerProcessViaEnvironment())
            {
                _mainThreadConfirmationPending = false;
                return true;
            }

            if (!TryConfirmEditorMainThread())
            {
                LogVerbose(
                    "ScriptableObjectSingletonCreator: Main thread not yet confirmed; deferring singleton ensure."
                );
                _mainThreadConfirmationPending = true;
                return true;
            }

            _mainThreadConfirmationPending = false;
            _defaultAssetImportWorkerDetector ??= AssetDatabase.IsAssetImportWorkerProcess;
            return InvokeDetector(_defaultAssetImportWorkerDetector, assumeWorkerOnFailure: false);
        }

        private static bool InvokeDetector(Func<bool> detector, bool assumeWorkerOnFailure)
        {
            try
            {
                return detector();
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"ScriptableObjectSingletonCreator: Asset import worker detector threw {ex.GetType().Name}: {ex.Message}. {(assumeWorkerOnFailure ? "Assuming import worker context." : "Assuming main editor process.")}"
                );
                return assumeWorkerOnFailure;
            }
        }

        private static bool TryConfirmEditorMainThread()
        {
            if (_mainThreadConfirmed)
            {
                return Thread.CurrentThread.ManagedThreadId == _capturedMainThreadId;
            }

            if (!UnityMainThreadGuard.IsMainThread)
            {
                return false;
            }

            _capturedMainThreadId = Thread.CurrentThread.ManagedThreadId;
            _mainThreadConfirmed = true;
            return true;
        }

        private static bool IsAssetImportWorkerProcessViaEnvironment()
        {
            if (_assetImportWorkerEnvCachedValue.HasValue)
            {
                return _assetImportWorkerEnvCachedValue.Value;
            }

            if (IsTruthy(Environment.GetEnvironmentVariable(AssetImportWorkerEnvVar)))
            {
                _assetImportWorkerEnvCachedValue = true;
                return true;
            }

            if (IsTruthy(Environment.GetEnvironmentVariable(LegacyAssetImportWorkerEnvVar)))
            {
                _assetImportWorkerEnvCachedValue = true;
                return true;
            }

            IDictionary variables = null;
            try
            {
                variables = Environment.GetEnvironmentVariables();
            }
            catch (Exception ex)
            {
                LogVerbose(
                    $"ScriptableObjectSingletonCreator: Unable to enumerate environment variables for worker detection: {ex.Message}"
                );
            }

            if (variables != null)
            {
                foreach (DictionaryEntry entry in variables)
                {
                    if (
                        entry.Key is not string key
                        || key.IndexOf(
                            "UNITY_ASSET_IMPORT_WORKER",
                            StringComparison.OrdinalIgnoreCase
                        ) < 0
                        || entry.Value is not string candidateValue
                    )
                    {
                        continue;
                    }

                    if (IsTruthy(candidateValue))
                    {
                        _assetImportWorkerEnvCachedValue = true;
                        return true;
                    }
                }
            }

            _assetImportWorkerEnvCachedValue = false;
            return false;

            static bool IsTruthy(string candidate)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    return false;
                }

                string normalized = candidate.Trim();
                return !string.Equals(normalized, "0", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(normalized, "false", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static void LogVerbose(string message)
        {
            if (VerboseLogging)
            {
                Debug.Log(message);
            }
        }

        [Conditional("UNITY_INCLUDE_TESTS")]
        internal static void ResetAssetImportWorkerDetectionStateForTests()
        {
            _assetImportWorkerEnvCachedValue = null;
            _defaultAssetImportWorkerDetector = null;
            _mainThreadConfirmed = false;
            _mainThreadConfirmationPending = false;
            _capturedMainThreadId = 0;
        }
    }
#endif
}

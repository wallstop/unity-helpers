namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine.Serialization;
    using WallstopStudios.UnityHelpers.Core.Helper;

    [Serializable]
    public sealed class DirectoryUsageData
    {
        public string path;
        public int count;
        public long lastUsedTicks;

        public DirectoryUsageData(string p)
        {
            path = p;
            count = 0;
            lastUsedTicks = DateTime.UtcNow.Ticks;
        }

        public void MarkUsed()
        {
            count++;
            lastUsedTicks = DateTime.UtcNow.Ticks;
        }
    }

    [Serializable]
    public sealed class ContextHistory
    {
        public string contextKey;
        public List<DirectoryUsageData> directories = new();

        public ContextHistory() { }

        public ContextHistory(string key)
        {
            contextKey = key;
        }

        public DirectoryUsageData GetOrAddDirectory(string path)
        {
            DirectoryUsageData dirData = directories.Find(directoryData =>
                string.Equals(directoryData.path, path, StringComparison.Ordinal)
            );
            if (dirData != null)
            {
                return dirData;
            }

            dirData = new DirectoryUsageData(path);
            directories.Add(dirData);
            return dirData;
        }
    }

    [Serializable]
    public sealed class ToolHistory
    {
        public string toolName;
        public List<ContextHistory> contexts = new();

        public ToolHistory() { }

        public ToolHistory(string name)
        {
            toolName = name;
        }

        public ContextHistory GetOrAddContext(string contextKey)
        {
            ContextHistory context = contexts.Find(c =>
                string.Equals(c.contextKey, contextKey, StringComparison.Ordinal)
            );
            if (context != null)
            {
                return context;
            }

            context = new ContextHistory(contextKey);
            contexts.Add(context);
            return context;
        }
    }

    [WallstopStudios.UnityHelpers.Core.Attributes.ScriptableSingletonPath(
        "Wallstop Studios/Editor"
    )]
    public sealed class PersistentDirectorySettings
        : WallstopStudios.UnityHelpers.Utils.ScriptableObjectSingleton<PersistentDirectorySettings>
    {
        [FormerlySerializedAs("allToolHistories")]
        [SerializeField]
        private List<ToolHistory> _allToolHistories = new();

        // Automatic migration and consolidation to Resources/Wallstop Studios/Editor
        [InitializeOnLoadMethod]
        private static void EnsureSingletonAndMigrate()
        {
            try
            {
                string resourcesRoot = "Assets/Resources";
                string subPath = "Wallstop Studios/Editor";
                string targetFolder = SanitizePath(Path.Combine(resourcesRoot, subPath));
                string targetAssetPath = SanitizePath(
                    Path.Combine(targetFolder, nameof(PersistentDirectorySettings) + ".asset")
                );

                EnsureFolderExists(resourcesRoot);
                EnsureFolderExists(targetFolder);

                // Find all existing assets of this type
                string[] guids = AssetDatabase.FindAssets(
                    "t:" + nameof(PersistentDirectorySettings)
                );
                List<string> candidatePaths = new();
                using (
                    WallstopStudios
                        .UnityHelpers.Utils.SetBuffers<string>.GetHashSetPool(
                            StringComparer.OrdinalIgnoreCase
                        )
                        .Get(out HashSet<string> seen)
                )
                {
                    for (int i = 0; i < guids.Length; i++)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                        if (string.IsNullOrWhiteSpace(path))
                        {
                            continue;
                        }
                        string sanitized = SanitizePath(path);
                        if (seen.Add(sanitized))
                        {
                            candidatePaths.Add(sanitized);
                        }
                    }
                }

                PersistentDirectorySettings target =
                    AssetDatabase.LoadAssetAtPath<PersistentDirectorySettings>(targetAssetPath);

                if (target == null)
                {
                    if (candidatePaths.Count == 0)
                    {
                        // Create a fresh asset if none exist anywhere
                        target = CreateInstance<PersistentDirectorySettings>();
                        AssetDatabase.CreateAsset(target, targetAssetPath);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        // Take the first found as primary and move it to target
                        string primaryPath = candidatePaths[0];
                        target = AssetDatabase.LoadAssetAtPath<PersistentDirectorySettings>(
                            primaryPath
                        );
                        if (target == null)
                        {
                            // Fallback to create if unexpected null
                            target = CreateInstance<PersistentDirectorySettings>();
                            AssetDatabase.CreateAsset(target, targetAssetPath);
                        }
                        else if (!PathsEqual(primaryPath, targetAssetPath))
                        {
                            string moveResult = AssetDatabase.MoveAsset(
                                primaryPath,
                                targetAssetPath
                            );
                            if (!string.IsNullOrEmpty(moveResult))
                            {
                                Debug.LogWarning(
                                    $"Failed to move {nameof(PersistentDirectorySettings)} from {primaryPath} to {targetAssetPath}: {moveResult}. Will create new and merge."
                                );
                                // Create new target and merge below
                                PersistentDirectorySettings newTarget =
                                    CreateInstance<PersistentDirectorySettings>();
                                AssetDatabase.CreateAsset(newTarget, targetAssetPath);
                                target = newTarget;
                            }
                            else
                            {
                                TryDeleteEmptyParentFolders(primaryPath);
                            }
                        }
                    }
                }

                // Merge any remaining duplicates into target, then delete them
                if (target != null && candidatePaths.Count > 0)
                {
                    foreach (string path in candidatePaths)
                    {
                        if (PathsEqual(path, targetAssetPath))
                        {
                            continue;
                        }

                        PersistentDirectorySettings other =
                            AssetDatabase.LoadAssetAtPath<PersistentDirectorySettings>(path);
                        if (other == null)
                        {
                            continue;
                        }

                        // Merge data
                        MergeSettings(target, other);
                        EditorUtility.SetDirty(target);

                        // Delete old asset and clean empty folders
                        if (AssetDatabase.DeleteAsset(path))
                        {
                            TryDeleteEmptyParentFolders(path);
                        }
                    }

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    $"{nameof(PersistentDirectorySettings)} migration encountered an issue: {e.Message}\n{e}"
                );
            }
        }

        private ToolHistory GetOrAddToolHistory(string toolName)
        {
            ToolHistory toolHistory = _allToolHistories.Find(toolHistory =>
                string.Equals(toolHistory.toolName, toolName, StringComparison.Ordinal)
            );
            if (toolHistory != null)
            {
                return toolHistory;
            }

            toolHistory = new ToolHistory(toolName);
            _allToolHistories.Add(toolHistory);

            return toolHistory;
        }

        public void RecordPath(string toolName, string contextKey, string path)
        {
            if (
                string.IsNullOrWhiteSpace(toolName)
                || string.IsNullOrWhiteSpace(contextKey)
                || string.IsNullOrWhiteSpace(path)
            )
            {
                Debug.LogWarning("RecordPath: toolName, contextKey, or path cannot be empty.");
                return;
            }

            string sanitizedPath = PathHelper.Sanitize(path);
            if (
                !sanitizedPath.StartsWith("Assets/", StringComparison.Ordinal)
                || !AssetDatabase.IsValidFolder(sanitizedPath)
            )
            {
                if (
                    !Path.IsPathRooted(sanitizedPath)
                    && !sanitizedPath.StartsWith("Assets/", StringComparison.Ordinal)
                )
                {
                    Debug.LogWarning(
                        $"Recording path '{sanitizedPath}' that is not an 'Assets/' relative path or an absolute path. This might be intentional."
                    );
                }
            }

            ToolHistory tool = GetOrAddToolHistory(toolName);
            ContextHistory context = tool.GetOrAddContext(contextKey);
            DirectoryUsageData dirData = context.GetOrAddDirectory(sanitizedPath);
            dirData.MarkUsed();
            EditorUtility.SetDirty(this);
        }

        public DirectoryUsageData[] GetPaths(
            string toolName,
            string contextKey,
            bool topOnly = false,
            int topN = 5
        )
        {
            ToolHistory tool = _allToolHistories.Find(th =>
                string.Equals(th.toolName, toolName, StringComparison.Ordinal)
            );
            if (tool == null)
            {
                return Array.Empty<DirectoryUsageData>();
            }

            ContextHistory context = tool.contexts.Find(c =>
                string.Equals(c.contextKey, contextKey, StringComparison.Ordinal)
            );
            if (context == null)
            {
                return Array.Empty<DirectoryUsageData>();
            }

            List<DirectoryUsageData> list = context.directories;
            if (list == null || list.Count == 0)
            {
                return Array.Empty<DirectoryUsageData>();
            }

            DirectoryUsageData[] sortedDirectories = new DirectoryUsageData[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                sortedDirectories[i] = list[i];
            }
            Array.Sort(
                sortedDirectories,
                static (a, b) =>
                {
                    int cmp = b.count.CompareTo(a.count);
                    if (cmp != 0)
                    {
                        return cmp;
                    }
                    return b.lastUsedTicks.CompareTo(a.lastUsedTicks);
                }
            );

            if (topOnly)
            {
                int n =
                    topN < 0
                        ? 0
                        : (topN > sortedDirectories.Length ? sortedDirectories.Length : topN);
                if (n == sortedDirectories.Length)
                {
                    return sortedDirectories;
                }
                DirectoryUsageData[] result = new DirectoryUsageData[n];
                for (int i = 0; i < n; i++)
                {
                    result[i] = sortedDirectories[i];
                }
                return result;
            }
            return sortedDirectories;
        }

        private static void MergeSettings(
            PersistentDirectorySettings target,
            PersistentDirectorySettings other
        )
        {
            if (other == null || target == null || ReferenceEquals(target, other))
            {
                return;
            }

            foreach (ToolHistory otherTool in other._allToolHistories)
            {
                if (otherTool == null || string.IsNullOrWhiteSpace(otherTool.toolName))
                {
                    continue;
                }

                ToolHistory targetTool = target._allToolHistories.Find(t =>
                    string.Equals(t.toolName, otherTool.toolName, StringComparison.Ordinal)
                );
                if (targetTool == null)
                {
                    // Deep copy to avoid shared references
                    ToolHistory copyTool = new(otherTool.toolName)
                    {
                        contexts = new List<ContextHistory>(),
                    };

                    foreach (ContextHistory oc in otherTool.contexts)
                    {
                        if (oc == null || string.IsNullOrWhiteSpace(oc.contextKey))
                        {
                            continue;
                        }
                        ContextHistory cc = new(oc.contextKey)
                        {
                            directories = new List<DirectoryUsageData>(),
                        };
                        foreach (DirectoryUsageData od in oc.directories)
                        {
                            if (od == null || string.IsNullOrWhiteSpace(od.path))
                            {
                                continue;
                            }
                            cc.directories.Add(
                                new DirectoryUsageData(od.path)
                                {
                                    count = od.count,
                                    lastUsedTicks = od.lastUsedTicks,
                                }
                            );
                        }
                        copyTool.contexts.Add(cc);
                    }

                    target._allToolHistories.Add(copyTool);
                    continue;
                }

                // Merge contexts
                foreach (ContextHistory otherContext in otherTool.contexts)
                {
                    if (otherContext == null || string.IsNullOrWhiteSpace(otherContext.contextKey))
                    {
                        continue;
                    }

                    ContextHistory targetContext = targetTool.contexts.Find(c =>
                        string.Equals(
                            c.contextKey,
                            otherContext.contextKey,
                            StringComparison.Ordinal
                        )
                    );
                    if (targetContext == null)
                    {
                        ContextHistory cc = new(otherContext.contextKey)
                        {
                            directories = new List<DirectoryUsageData>(),
                        };
                        foreach (DirectoryUsageData od in otherContext.directories)
                        {
                            if (od == null || string.IsNullOrWhiteSpace(od.path))
                            {
                                continue;
                            }
                            cc.directories.Add(
                                new DirectoryUsageData(od.path)
                                {
                                    count = od.count,
                                    lastUsedTicks = od.lastUsedTicks,
                                }
                            );
                        }
                        targetTool.contexts.Add(cc);
                        continue;
                    }

                    // Merge directories
                    foreach (DirectoryUsageData od in otherContext.directories)
                    {
                        if (od == null || string.IsNullOrWhiteSpace(od.path))
                        {
                            continue;
                        }
                        DirectoryUsageData td = targetContext.directories.Find(d =>
                            string.Equals(d.path, od.path, StringComparison.Ordinal)
                        );
                        if (td == null)
                        {
                            targetContext.directories.Add(
                                new DirectoryUsageData(od.path)
                                {
                                    count = od.count,
                                    lastUsedTicks = od.lastUsedTicks,
                                }
                            );
                        }
                        else
                        {
                            long maxTicks = Math.Max(td.lastUsedTicks, od.lastUsedTicks);
                            long sumCount = (long)td.count + od.count;
                            td.count = sumCount > int.MaxValue ? int.MaxValue : (int)sumCount;
                            td.lastUsedTicks = maxTicks;
                        }
                    }
                }
            }
        }

        private static string SanitizePath(string path)
        {
            return PathHelper.Sanitize(path);
        }

        private static bool PathsEqual(string a, string b)
        {
            return string.Equals(
                SanitizePath(a),
                SanitizePath(b),
                StringComparison.OrdinalIgnoreCase
            );
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            folderPath = SanitizePath(folderPath);
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

        private static void TryDeleteEmptyParentFolders(string assetOrFolderPath)
        {
            try
            {
                string path = SanitizePath(assetOrFolderPath);
                string folder = AssetDatabase.IsValidFolder(path)
                    ? path
                    : Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(folder))
                {
                    return;
                }

                folder = SanitizePath(folder);
                while (
                    !string.IsNullOrWhiteSpace(folder)
                    && !string.Equals(folder, "Assets", StringComparison.OrdinalIgnoreCase)
                )
                {
                    if (!AssetDatabase.IsValidFolder(folder))
                    {
                        folder = Path.GetDirectoryName(folder);
                        folder = SanitizePath(folder);
                        continue;
                    }

                    string[] contents = AssetDatabase.FindAssets(string.Empty, new[] { folder });
                    if (contents == null || contents.Length == 0)
                    {
                        if (AssetDatabase.DeleteAsset(folder))
                        {
                            string parent = Path.GetDirectoryName(folder);
                            folder = SanitizePath(parent);
                            continue;
                        }
                    }
                    break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete {assetOrFolderPath} with error: {e}.");
            }
        }
    }
#endif
}

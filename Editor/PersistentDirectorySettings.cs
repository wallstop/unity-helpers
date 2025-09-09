namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using Core.Helper;
    using UnityEngine.Serialization;

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

    public sealed class PersistentDirectorySettings : ScriptableObject
    {
        private const string DefaultAssetPath = "Assets/Editor/PersistentDirectorySettings.asset";

        [FormerlySerializedAs("allToolHistories")]
        [SerializeField]
        private List<ToolHistory> _allToolHistories = new();

        private static PersistentDirectorySettings _instance;

        public static PersistentDirectorySettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    PersistentDirectorySettings[] settings = AssetDatabase
                        .FindAssets($"t:{nameof(PersistentDirectorySettings)}")
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<PersistentDirectorySettings>)
                        .Where(Objects.NotNull)
                        .ToArray();

                    if (settings.Length > 0)
                    {
                        if (settings.Length > 1)
                        {
                            Debug.LogWarning(
                                $"Multiple instances of {nameof(PersistentDirectorySettings)} found. Using the first one at: {AssetDatabase.GetAssetPath(settings[0])}. Please ensure only one instance exists for consistent behavior."
                            );
                        }

                        _instance = settings[0];
                    }
                    else
                    {
                        if (_instance == null)
                        {
                            Debug.Log(
                                $"No instance of {nameof(PersistentDirectorySettings)} found. Creating a new one at {DefaultAssetPath}."
                            );
                            _instance = CreateInstance<PersistentDirectorySettings>();

                            string directoryPath = Path.GetDirectoryName(DefaultAssetPath);
                            if (
                                !string.IsNullOrWhiteSpace(directoryPath)
                                && !Directory.Exists(directoryPath)
                            )
                            {
                                Directory.CreateDirectory(directoryPath);
                            }

                            AssetDatabase.CreateAsset(_instance, DefaultAssetPath);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                            EditorUtility.FocusProjectWindow();
                            Selection.activeObject = _instance;
                        }
                    }
                }

                if (_instance == null)
                {
                    Debug.LogError(
                        $"Failed to find or create {nameof(PersistentDirectorySettings)}. Directory persistence will not work."
                    );
                }

                return _instance;
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

            string sanitizedPath = path.SanitizePath();
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

            DirectoryUsageData[] sortedDirectories = context
                .directories.OrderByDescending(directoryData => directoryData.count)
                .ThenByDescending(directoryData => directoryData.lastUsedTicks)
                .ToArray();

            return topOnly ? sortedDirectories.Take(topN).ToArray() : sortedDirectories;
        }
    }
#endif
}

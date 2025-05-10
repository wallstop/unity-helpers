namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using Core.Helper; // For Path.GetDirectoryName, Directory.CreateDirectory

    [Serializable]
    public sealed class DirectoryUsageData
    {
        public string path;
        public int count;
        public long lastUsedTicks; // For secondary sorting (most recent if counts are equal)

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
    public class ContextHistory
    {
        public string contextKey;
        public List<DirectoryUsageData> directories = new List<DirectoryUsageData>();

        public ContextHistory() { } // For serialization

        public ContextHistory(string key)
        {
            contextKey = key;
        }

        public DirectoryUsageData GetOrAddDirectory(string path)
        {
            DirectoryUsageData dirData = directories.FirstOrDefault(d => d.path == path);
            if (dirData == null)
            {
                dirData = new DirectoryUsageData(path);
                directories.Add(dirData);
            }
            return dirData;
        }
    }

    [Serializable]
    public class ToolHistory
    {
        public string toolName;
        public List<ContextHistory> contexts = new List<ContextHistory>();

        public ToolHistory() { } // For serialization

        public ToolHistory(string name)
        {
            toolName = name;
        }

        public ContextHistory GetOrAddContext(string contextKey)
        {
            ContextHistory context = contexts.FirstOrDefault(c => c.contextKey == contextKey);
            if (context == null)
            {
                context = new ContextHistory(contextKey);
                contexts.Add(context);
            }
            return context;
        }
    }

    // Not using CreateAssetMenu as it's intended to be a singleton managed by code.
    public class PersistentDirectorySettings : ScriptableObject
    {
        private const string DEFAULT_ASSET_PATH = "Assets/Editor/PersistentDirectorySettings.asset";

        [SerializeField]
        private List<ToolHistory> allToolHistories = new List<ToolHistory>();

        private static PersistentDirectorySettings _instance;

        public static PersistentDirectorySettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    string[] guids = AssetDatabase.FindAssets(
                        $"t:{nameof(PersistentDirectorySettings)}"
                    );
                    if (guids.Length > 0)
                    {
                        if (guids.Length > 1)
                        {
                            Debug.LogWarning(
                                $"Multiple instances of {nameof(PersistentDirectorySettings)} found. Using the first one at: {AssetDatabase.GUIDToAssetPath(guids[0])}. Please ensure only one instance exists for consistent behavior."
                            );
                        }

                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        _instance = AssetDatabase.LoadAssetAtPath<PersistentDirectorySettings>(
                            path
                        );
                    }
                    else
                    {
                        // Try to load from a Resources/Editor folder (less ideal for editor-only assets but a fallback)
                        // _instance = Resources.Load<PersistentDirectorySettings>(Path.Combine("Editor", RESOURCE_LOAD_PATH));
                        // Unity doesn't really support Resources/Editor loading well.
                        // Stick to AssetDatabase.FindAssets and create if not found.

                        if (_instance == null)
                        {
                            Debug.Log(
                                $"No instance of {nameof(PersistentDirectorySettings)} found. Creating a new one at {DEFAULT_ASSET_PATH}."
                            );
                            _instance = CreateInstance<PersistentDirectorySettings>();

                            string dirPath = Path.GetDirectoryName(DEFAULT_ASSET_PATH);
                            if (!Directory.Exists(dirPath))
                            {
                                Directory.CreateDirectory(dirPath);
                            }

                            AssetDatabase.CreateAsset(_instance, DEFAULT_ASSET_PATH);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                            EditorUtility.FocusProjectWindow();
                            Selection.activeObject = _instance;
                        }
                    }
                }

                if (_instance == null) // Should only happen if creation failed catastrophically
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
            ToolHistory toolHistory = allToolHistories.FirstOrDefault(th =>
                th.toolName == toolName
            );
            if (toolHistory == null)
            {
                toolHistory = new ToolHistory(toolName);
                allToolHistories.Add(toolHistory);
            }

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

            string sanitizedPath = path.SanitizePath(); // Assuming your StringPathExtensions
            if (!sanitizedPath.StartsWith("Assets/") || !AssetDatabase.IsValidFolder(sanitizedPath))
            {
                // Allow non-asset paths too, but maybe log if they are not absolute
                // For now, let's primarily focus on Asset paths, but be flexible
                if (!Path.IsPathRooted(sanitizedPath) && !sanitizedPath.StartsWith("Assets/"))
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

            EditorUtility.SetDirty(this); // Mark for saving
            // AssetDatabase.SaveAssets(); // Saving too frequently can be slow. Let Unity's auto-save or user save handle it.
        }

        public DirectoryUsageData[] GetPaths(
            string toolName,
            string contextKey,
            bool topOnly = false,
            int topN = 5
        )
        {
            ToolHistory tool = allToolHistories.FirstOrDefault(th =>
                string.Equals(th.toolName, toolName, StringComparison.Ordinal)
            );
            if (tool == null)
            {
                return Array.Empty<DirectoryUsageData>();
            }

            ContextHistory context = tool.contexts.FirstOrDefault(c =>
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

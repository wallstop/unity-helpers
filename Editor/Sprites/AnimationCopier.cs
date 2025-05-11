namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using UnityEditor;
    using UnityEngine;
    using Core.Extension;
    using Core.Helper;
    using CustomEditors;

    public sealed class AnimationCopierWindow : EditorWindow
    {
        [SerializeField]
        private string _animationSourcePathRelative = "Assets/Sprites";

        [SerializeField]
        private string _animationDestinationPathRelative = "Assets/Animations";
        private string _fullSourcePath = "";
        private string _fullDestinationPath = "";

        private bool _analysisNeeded = true;
        private bool _isAnalyzing;
        private bool _isCopying;
        private bool _isDeleting;

        private SerializedObject _serializedObject;
        private SerializedProperty _animationSourcesPathProperty;
        private SerializedProperty _animationDestinationPathProperty;

        private readonly List<AnimationFileInfo> _sourceAnimations = new();
        private readonly List<AnimationFileInfo> _newAnimations = new();
        private readonly List<AnimationFileInfo> _changedAnimations = new();
        private readonly List<AnimationFileInfo> _unchangedAnimations = new();

        private enum AnimationStatus
        {
            Unknown,
            New,
            Changed,
            Unchanged,
        }

        private enum CopyMode
        {
            All,
            Changed,
            New,
        }

        private class AnimationFileInfo
        {
            public string RelativePath { get; set; }
            public string FullPath { get; set; }
            public string FileName { get; set; }
            public string RelativeDirectory { get; set; }
            public string Hash { get; set; }
            public AnimationStatus Status { get; set; } = AnimationStatus.Unknown;
            public string DestinationRelativePath { get; set; }
        }

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Animation Copier", priority = -2)]
        public static void ShowWindow()
        {
            GetWindow<AnimationCopierWindow>("Animation Copier");
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _animationSourcesPathProperty = _serializedObject.FindProperty(
                nameof(_animationSourcePathRelative)
            );
            _animationDestinationPathProperty = _serializedObject.FindProperty(
                nameof(_animationDestinationPathRelative)
            );
            ValidatePaths();
            _analysisNeeded = true;
            this.Log($"Animation Copier Window opened.");
        }

        private void OnGUI()
        {
            _serializedObject.Update();
            bool operationInProgress = _isAnalyzing || _isCopying || _isDeleting;

            if (operationInProgress)
            {
                string status =
                    _isAnalyzing ? "Analyzing..."
                    : _isCopying ? "Copying..."
                    : "Deleting...";
                EditorGUILayout.LabelField(status, EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUI.BeginDisabledGroup(operationInProgress);

            PersistentDirectoryGUI.PathSelectorString(
                _animationSourcesPathProperty,
                nameof(AnimationCopierWindow),
                "Source Path",
                new GUIContent("Source Path")
            );
            EditorGUILayout.Separator();
            PersistentDirectoryGUI.PathSelectorString(
                _animationDestinationPathProperty,
                nameof(AnimationCopierWindow),
                "Destination Path",
                new GUIContent("Destination Path")
            );
            EditorGUILayout.Separator();

            DrawAnalysisSection();
            EditorGUILayout.Separator();
            DrawCopySection();
            EditorGUILayout.Separator();
            DrawCleanupSection();

            EditorGUI.EndDisabledGroup();

            if (!operationInProgress && _analysisNeeded && Event.current.type == EventType.Layout)
            {
                if (ArePathsValid())
                {
                    AnalyzeAnimations();
                }
                else
                {
                    ClearAnalysisResults();
                }
                _analysisNeeded = false;
                Repaint();
            }
        }

        private void DrawAnalysisSection()
        {
            EditorGUILayout.LabelField("Analysis:", EditorStyles.boldLabel);

            if (GUILayout.Button("Analyze Source & Destination"))
            {
                if (ArePathsValid())
                {
                    AnalyzeAnimations();
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        "Source or Destination path is not set or invalid.",
                        "OK"
                    );
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(
                "Source Animations Found:",
                _sourceAnimations.Count.ToString()
            );
            EditorGUILayout.LabelField("- New:", _newAnimations.Count.ToString());
            EditorGUILayout.LabelField("- Changed:", _changedAnimations.Count.ToString());
            EditorGUILayout.LabelField(
                "- Unchanged (Duplicates):",
                _unchangedAnimations.Count.ToString()
            );
        }

        private void DrawCopySection()
        {
            EditorGUILayout.LabelField("Copy Actions:", EditorStyles.boldLabel);

            bool canAnalyze = ArePathsValid();
            bool analysisDone = !_analysisNeeded;

            bool canCopyNew = canAnalyze && analysisDone && _newAnimations.Any();
            bool canCopyChanged = canAnalyze && analysisDone && _changedAnimations.Any();
            bool canCopyAll = canAnalyze && analysisDone && _sourceAnimations.Any();

            EditorGUI.BeginDisabledGroup(!canCopyNew);
            if (GUILayout.Button($"Copy New ({_newAnimations.Count})"))
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Confirm Copy New",
                        $"Copy {_newAnimations.Count} new animations from '{_animationSourcePathRelative}' to '{_animationDestinationPathRelative}'?",
                        "Yes, Copy New",
                        "Cancel"
                    )
                )
                {
                    CopyAnimationsInternal(CopyMode.New);
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!canCopyChanged);
            if (GUILayout.Button($"Copy Changed ({_changedAnimations.Count})"))
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Confirm Copy Changed",
                        $"Copy {_changedAnimations.Count} changed animations from '{_animationSourcePathRelative}' to '{_animationDestinationPathRelative}', overwriting existing files?",
                        "Yes, Copy Changed",
                        "Cancel"
                    )
                )
                {
                    CopyAnimationsInternal(CopyMode.Changed);
                }
            }
            EditorGUI.EndDisabledGroup();

            int totalToCopyAll =
                _newAnimations.Count + _changedAnimations.Count + _unchangedAnimations.Count;
            EditorGUI.BeginDisabledGroup(!canCopyAll);
            if (GUILayout.Button($"Copy All ({totalToCopyAll})"))
            {
                string overwriteWarning =
                    _changedAnimations.Count + _unchangedAnimations.Count > 0
                        ? $" This will overwrite {_changedAnimations.Count + _unchangedAnimations.Count} existing files."
                        : "";
                if (
                    EditorUtility.DisplayDialog(
                        "Confirm Copy All",
                        $"Copy {totalToCopyAll} animations from '{_animationSourcePathRelative}' to '{_animationDestinationPathRelative}'?{overwriteWarning}",
                        "Yes, Copy All",
                        "Cancel"
                    )
                )
                {
                    CopyAnimationsInternal(CopyMode.All);
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawCleanupSection()
        {
            EditorGUILayout.LabelField("Cleanup Actions:", EditorStyles.boldLabel);

            bool canAnalyze = ArePathsValid();
            bool analysisDone = !_analysisNeeded;
            bool hasUnchanged = _unchangedAnimations.Any();

            if (canAnalyze && analysisDone && hasUnchanged)
            {
                Color originalColor = GUI.color;
                GUI.color = Color.red;

                string buttonText =
                    $"Delete {_unchangedAnimations.Count} Unchanged Source Duplicates";

                if (GUILayout.Button(buttonText))
                {
                    DeleteUnchangedSourceAnimations();
                }

                GUI.color = originalColor;
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button("Delete Unchanged Source Duplicates (None found)");
                EditorGUI.EndDisabledGroup();
            }
        }

        private void ValidatePaths()
        {
            _fullSourcePath = GetFullPathFromRelative(_animationSourcePathRelative);
            _fullDestinationPath = GetFullPathFromRelative(_animationDestinationPathRelative);

            if (_fullSourcePath == null || !Directory.Exists(_fullSourcePath))
            {
                this.LogWarn(
                    $"Source path '{_animationSourcePathRelative}' is invalid or outside the project. Please set a valid path within Assets."
                );
                _fullSourcePath = null;
                _analysisNeeded = true;
                ClearAnalysisResults();
            }
            if (_fullDestinationPath == null)
            {
                this.LogWarn(
                    $"Destination path '{_animationDestinationPathRelative}' is invalid or outside the project. Please set a valid path within Assets."
                );
                _analysisNeeded = true;
                ClearAnalysisResults();
            }
            else
            {
                string parentDir = Path.GetDirectoryName(_fullDestinationPath);
                if (!Directory.Exists(parentDir))
                {
                    this.LogWarn(
                        $"The parent directory for the destination path '{_animationDestinationPathRelative}' does not exist ('{parentDir}'). Copy operations may fail to create folders."
                    );
                }
            }
        }

        private bool ArePathsValid()
        {
            return !string.IsNullOrWhiteSpace(_animationSourcePathRelative)
                && !string.IsNullOrWhiteSpace(_animationDestinationPathRelative)
                && _fullSourcePath != null
                && _fullDestinationPath != null
                && !string.Equals(
                    _animationSourcePathRelative,
                    _animationDestinationPathRelative,
                    StringComparison.Ordinal
                );
        }

        private void AnalyzeAnimations()
        {
            if (!ArePathsValid())
            {
                this.LogError($"Cannot analyze: Paths are invalid.");
                ClearAnalysisResults();
                _analysisNeeded = false;
                Repaint();
                return;
            }

            if (_isAnalyzing || _isCopying || _isDeleting)
            {
                return;
            }

            this.Log($"Starting animation analysis...");
            _isAnalyzing = true;
            ClearAnalysisResults();
            Repaint();

            try
            {
                string[] sourceGuids = AssetDatabase.FindAssets(
                    "t:AnimationClip",
                    new[] { _animationSourcePathRelative }
                );
                _sourceAnimations.Clear();

                float total = sourceGuids.Length * 2;
                int current = 0;

                EditorUtility.DisplayProgressBar(
                    "Analyzing Animations",
                    "Gathering source files...",
                    0f
                );

                foreach (string guid in sourceGuids)
                {
                    current++;
                    string sourceRelPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (
                        string.IsNullOrWhiteSpace(sourceRelPath)
                        || !sourceRelPath.StartsWith(
                            _animationSourcePathRelative,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        continue;
                    }

                    string sourceFullPath = GetFullPathFromRelative(sourceRelPath);
                    if (sourceFullPath == null || !File.Exists(sourceFullPath))
                    {
                        continue;
                    }

                    string directoryName = Path.GetDirectoryName(sourceRelPath);
                    if (string.IsNullOrWhiteSpace(directoryName))
                    {
                        continue;
                    }
                    AnimationFileInfo fileInfo = new()
                    {
                        RelativePath = sourceRelPath,
                        FullPath = sourceFullPath,
                        FileName = Path.GetFileName(sourceRelPath),
                        RelativeDirectory = GetRelativeSubPath(
                            _animationSourcePathRelative,
                            directoryName.SanitizePath()
                        ),
                        Hash = CalculateFileHash(sourceFullPath),
                    };
                    fileInfo.DestinationRelativePath = Path.Combine(
                            _animationDestinationPathRelative,
                            fileInfo.RelativeDirectory,
                            fileInfo.FileName
                        )
                        .SanitizePath();
                    _sourceAnimations.Add(fileInfo);

                    EditorUtility.DisplayProgressBar(
                        "Analyzing Animations",
                        $"Hashing: {fileInfo.FileName}",
                        current / total
                    );
                }

                this.Log(
                    $"Found {_sourceAnimations.Count} animations in source. Comparing with destination..."
                );

                for (int i = 0; i < _sourceAnimations.Count; i++)
                {
                    AnimationFileInfo sourceInfo = _sourceAnimations[i];
                    current++;
                    EditorUtility.DisplayProgressBar(
                        "Analyzing Animations",
                        $"Comparing: {sourceInfo.FileName}",
                        current / total
                    );

                    string destRelPath = sourceInfo.DestinationRelativePath;
                    string destFullPath = GetFullPathFromRelative(destRelPath);
                    bool destExists = destFullPath != null && File.Exists(destFullPath);

                    if (!destExists)
                    {
                        sourceInfo.Status = AnimationStatus.New;
                        _newAnimations.Add(sourceInfo);
                    }
                    else
                    {
                        string destHash = CalculateFileHash(destFullPath);
                        if (
                            string.IsNullOrWhiteSpace(sourceInfo.Hash)
                            || string.IsNullOrWhiteSpace(destHash)
                        )
                        {
                            this.LogWarn(
                                $"Could not compare '{sourceInfo.FileName}' due to hashing error. Treating as 'Changed'."
                            );
                            sourceInfo.Status = AnimationStatus.Changed;
                            _changedAnimations.Add(sourceInfo);
                        }
                        else if (
                            sourceInfo.Hash.Equals(destHash, StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            sourceInfo.Status = AnimationStatus.Unchanged;
                            _unchangedAnimations.Add(sourceInfo);
                        }
                        else
                        {
                            sourceInfo.Status = AnimationStatus.Changed;
                            _changedAnimations.Add(sourceInfo);
                        }
                    }
                }

                this.Log(
                    $"Analysis complete: {_newAnimations.Count} New, {_changedAnimations.Count} Changed, {_unchangedAnimations.Count} Unchanged."
                );
            }
            catch (Exception ex)
            {
                this.LogError($"Error during analysis: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog(
                    "Analysis Error",
                    $"An error occurred during analysis: {ex.Message}",
                    "OK"
                );
                ClearAnalysisResults();
            }
            finally
            {
                _isAnalyzing = false;
                _analysisNeeded = false;
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private void CopyAnimationsInternal(CopyMode mode)
        {
            if (!ArePathsValid() || _isAnalyzing || _isCopying || _isDeleting)
            {
                return;
            }

            List<AnimationFileInfo> animationsToCopy = new();
            switch (mode)
            {
                case CopyMode.All:
                    animationsToCopy.AddRange(_newAnimations);
                    animationsToCopy.AddRange(_changedAnimations);
                    animationsToCopy.AddRange(_unchangedAnimations);
                    break;
                case CopyMode.Changed:
                    animationsToCopy.AddRange(_changedAnimations);
                    break;
                case CopyMode.New:
                    animationsToCopy.AddRange(_newAnimations);
                    break;
            }

            if (animationsToCopy.Count == 0)
            {
                this.Log($"No animations to copy for the selected mode.");
                EditorUtility.DisplayDialog(
                    "Nothing to Copy",
                    "There are no animations matching the selected criteria.",
                    "OK"
                );
                return;
            }

            this.Log(
                $"Starting copy operation (Mode: {mode}) for {animationsToCopy.Count} animations..."
            );
            _isCopying = true;
            Repaint();

            int successCount = 0;
            int errorCount = 0;
            foreach (AnimationFileInfo animInfo in animationsToCopy)
            {
                string destinationAssetPath = animInfo.DestinationRelativePath;
                string destDirectory = Path.GetDirectoryName(destinationAssetPath).SanitizePath();

                if (
                    string.IsNullOrWhiteSpace(destDirectory)
                    || AssetDatabase.IsValidFolder(destDirectory)
                )
                {
                    continue;
                }

                try
                {
                    DirectoryHelper.EnsureDirectoryExists(destDirectory);
                }
                catch (Exception ex)
                {
                    this.LogError(
                        $"Failed to create destination directory '{destDirectory}' for animation '{animInfo.FileName}'. Error: {ex.Message}. Skipping."
                    );
                }
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < animationsToCopy.Count; i++)
                {
                    AnimationFileInfo animInfo = animationsToCopy[i];
                    float progress = (float)(i + 1) / animationsToCopy.Count;
                    bool userCancelled = EditorUtility.DisplayCancelableProgressBar(
                        $"Copying Animations ({mode})",
                        $"Copying: {animInfo.FileName} ({i + 1}/{animationsToCopy.Count})",
                        progress
                    );

                    if (userCancelled)
                    {
                        this.LogWarn($"Copy operation cancelled by user.");
                        break;
                    }

                    string sourceAssetPath = animInfo.RelativePath;
                    string destinationAssetPath = animInfo.DestinationRelativePath;
                    bool copySuccessful = AssetDatabase.CopyAsset(
                        sourceAssetPath,
                        destinationAssetPath
                    );

                    if (copySuccessful)
                    {
                        successCount++;
                    }
                    else
                    {
                        this.LogError(
                            $"Failed to copy animation from '{sourceAssetPath}' to '{destinationAssetPath}'."
                        );
                        errorCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                this.LogError(
                    $"An unexpected error occurred during the copy process: {ex.Message}\n{ex.StackTrace}"
                );
                errorCount = animationsToCopy.Count - successCount;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
                _isCopying = false;
                this.Log(
                    $"Copy operation finished. Mode: {mode}. Success: {successCount}, Errors: {errorCount}."
                );

                EditorUtility.DisplayDialog(
                    "Copy Complete",
                    $"Copy operation finished.\nMode: {mode}\nSuccessfully copied: {successCount}\nErrors: {errorCount}\n\nSee console log for details.",
                    "OK"
                );

                _analysisNeeded = true;
                Repaint();
            }
        }

        private void DeleteUnchangedSourceAnimations()
        {
            if (!ArePathsValid() || _isAnalyzing || _isCopying || _isDeleting)
            {
                return;
            }

            List<AnimationFileInfo> animationsToDelete = _unchangedAnimations.ToList();

            if (animationsToDelete.Count == 0)
            {
                this.Log($"No unchanged source animations to delete.");

                return;
            }

            this.Log(
                $"Starting delete operation for {animationsToDelete.Count} unchanged source animations..."
            );
            _isDeleting = true;
            Repaint();

            int successCount = 0;
            int errorCount = 0;
            AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = 0; i < animationsToDelete.Count; i++)
                {
                    AnimationFileInfo animInfo = animationsToDelete[i];
                    float progress = (float)(i + 1) / animationsToDelete.Count;
                    bool userCancelled = EditorUtility.DisplayCancelableProgressBar(
                        "Deleting Source Duplicates",
                        $"Deleting: {animInfo.FileName} ({i + 1}/{animationsToDelete.Count})",
                        progress
                    );

                    if (userCancelled)
                    {
                        this.LogWarn($"Delete operation cancelled by user.");
                        break;
                    }

                    string sourceAssetPath = animInfo.RelativePath;

                    bool deleteSuccessful = AssetDatabase.DeleteAsset(sourceAssetPath);

                    if (deleteSuccessful)
                    {
                        successCount++;
                    }
                    else
                    {
                        this.LogError(
                            $"Failed to delete source duplicate: '{sourceAssetPath}'. It might have been moved or deleted already."
                        );
                        errorCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                this.LogError(
                    $"An unexpected error occurred during the delete process: {ex.Message}\n{ex.StackTrace}"
                );
                errorCount = animationsToDelete.Count - successCount;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();

                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
                _isDeleting = false;
                this.Log(
                    $"Delete operation finished. Successfully deleted: {successCount}, Errors: {errorCount}."
                );

                _analysisNeeded = true;
                Repaint();
            }
        }

        private void ClearAnalysisResults()
        {
            _sourceAnimations.Clear();
            _newAnimations.Clear();
            _changedAnimations.Clear();
            _unchangedAnimations.Clear();

            Repaint();
        }

        private static string GetRelativeAssetPath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }

            fullPath = fullPath.SanitizePath();
            if (
                fullPath.EndsWith("/Assets", StringComparison.OrdinalIgnoreCase)
                && Path.GetFileName(fullPath).Equals("Assets", StringComparison.OrdinalIgnoreCase)
            )
            {
                return "Assets";
            }

            string assetsPath = Application.dataPath.SanitizePath();
            if (fullPath.StartsWith(assetsPath, StringComparison.OrdinalIgnoreCase))
            {
                if (fullPath.Length == assetsPath.Length)
                {
                    return "Assets";
                }

                int startIndex = assetsPath.Length;
                if (fullPath.Length > startIndex && fullPath[startIndex] == '/')
                {
                    startIndex++;
                }

                return "Assets/" + fullPath.Substring(startIndex);
            }

            int assetIndex = fullPath.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
            if (assetIndex >= 0)
            {
                return fullPath.Substring(assetIndex + 1);
            }

            return null;
        }

        private static string GetFullPathFromRelative(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            if (relativePath.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return Application.dataPath.SanitizePath();
            }

            if (relativePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                string projectRoot = Application.dataPath.Substring(
                    0,
                    Application.dataPath.Length - "Assets".Length
                );
                return (projectRoot + relativePath).SanitizePath();
            }
            return null;
        }

        private string GetRelativeSubPath(string basePath, string fullPath)
        {
            string normalizedBasePath = basePath.TrimEnd('/') + "/";
            string normalizedFullPath = fullPath.TrimEnd('/') + "/";

            if (
                normalizedFullPath.StartsWith(
                    normalizedBasePath,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                string subPath = normalizedFullPath
                    .Substring(normalizedBasePath.Length)
                    .TrimEnd('/');
                return subPath;
            }

            this.LogWarn(
                $"Path '{fullPath}' did not start with expected base '{basePath}'. Could not determine relative sub-path."
            );
            return string.Empty;
        }

        private string CalculateFileHash(string filePath)
        {
            try
            {
                using MD5 md5 = MD5.Create();
                using FileStream stream = File.OpenRead(filePath);
                byte[] hashBytes = md5.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
            catch (IOException ioEx)
            {
                this.LogError(
                    $"[AnimationCopierWindow] IO Error calculating hash for {filePath}.",
                    ioEx
                );
                return string.Empty;
            }
            catch (Exception ex)
            {
                this.LogError(
                    $"[AnimationCopierWindow] Error calculating hash for {filePath}.",
                    ex
                );
                return string.Empty;
            }
        }
    }
#endif
}

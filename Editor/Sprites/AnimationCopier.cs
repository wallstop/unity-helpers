namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;
    using CustomEditors;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class AnimationCopierWindow : EditorWindow
    {
        // Test-friendly: allow suppressing modal prompts and progress UI
        internal static bool SuppressUserPrompts { get; set; }

        static AnimationCopierWindow()
        {
            try
            {
                if (Application.isBatchMode || IsInvokedByTestRunner())
                {
                    SuppressUserPrompts = true;
                }
            }
            catch { }
        }

        private static bool IsInvokedByTestRunner()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; ++i)
            {
                string a = args[i];
                if (
                    a.IndexOf("runTests", StringComparison.OrdinalIgnoreCase) >= 0
                    || a.IndexOf("testResults", StringComparison.OrdinalIgnoreCase) >= 0
                    || a.IndexOf("testPlatform", StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    return true;
                }
            }
            return false;
        }

        internal string AnimationSourcePathRelative
        {
            get => _animationSourcePathRelative;
            set
            {
                _animationSourcePathRelative = value;
                // Quiet validation when changed programmatically (e.g., tests)
                ValidatePaths(false);
                _analysisNeeded = true;
            }
        }

        internal string AnimationDestinationPathRelative
        {
            get => _animationDestinationPathRelative;
            set
            {
                _animationDestinationPathRelative = value;
                // Quiet validation when changed programmatically (e.g., tests)
                ValidatePaths(false);
                _analysisNeeded = true;
            }
        }

        internal bool DryRun
        {
            get => _dryRun;
            set => _dryRun = value;
        }

        internal int NewCount => _newAnimations.Count;
        internal int ChangedCount => _changedAnimations.Count;
        internal int UnchangedCount => _unchangedAnimations.Count;
        internal int OrphansCount => _destinationOrphans.Count;

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
        private readonly List<AnimationFileInfo> _destinationOrphans = new();

        // Preview and options
        [SerializeField]
        private bool _dryRun;

        [SerializeField]
        private bool _includeUnchangedInCopyAll;
        private bool _previewFoldout;
        private bool _newFoldout = true;
        private bool _changedFoldout = true;
        private bool _unchangedFoldout;
        private bool _orphansFoldout;
        private Vector2 _previewScroll;
        private string _filterText = string.Empty;
        private bool _filterUseRegex;
        private bool _sortAscending = true;

        private enum AnimationStatus
        {
            Unknown,
            New,
            Changed,
            Unchanged,
        }

        internal enum CopyMode
        {
            All,
            Changed,
            New,
        }

        private sealed class AnimationFileInfo
        {
            public string RelativePath { get; set; }
            public string FullPath { get; set; }
            public string FileName { get; set; }
            public string RelativeDirectory { get; set; }
            public string Hash { get; set; }
            public AnimationStatus Status { get; set; } = AnimationStatus.Unknown;
            public string DestinationRelativePath { get; set; }
            public bool Selected { get; set; } = true;
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
            // Avoid noisy logs during editor reloads or tests
            ValidatePaths(false);
            _analysisNeeded = true;
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

            // Detect path changes to revalidate and trigger re-analysis
            EditorGUI.BeginChangeCheck();
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
            if (EditorGUI.EndChangeCheck())
            {
                // User-initiated change: allow warnings to surface
                ValidatePaths(true);
                _analysisNeeded = true;
                ClearAnalysisResults();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Source Folder", GUILayout.Width(160)))
                {
                    RevealFolder(_animationSourcePathRelative);
                }
                if (GUILayout.Button("Open Destination Folder", GUILayout.Width(180)))
                {
                    RevealFolder(_animationDestinationPathRelative);
                }
            }
            EditorGUILayout.Separator();

            DrawAnalysisSection();
            EditorGUILayout.Separator();
            DrawPreviewSection();
            EditorGUILayout.Separator();
            DrawCopySection();
            EditorGUILayout.Separator();
            DrawCleanupSection();

            EditorGUI.EndDisabledGroup();

            // Always keep paths validated and analyze when needed (quietly)
            ValidatePaths(false);
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
                    Info("Error", "Source or Destination path is not set or invalid.");
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

            _dryRun = EditorGUILayout.ToggleLeft("Dry Run (no changes)", _dryRun);
            _includeUnchangedInCopyAll = EditorGUILayout.ToggleLeft(
                "Include Unchanged in Copy All (force replace)",
                _includeUnchangedInCopyAll
            );

            bool canAnalyze = ArePathsValid();
            bool analysisDone = !_analysisNeeded;

            int selectedNew = _newAnimations.Count(a => a.Selected);
            int selectedChanged = _changedAnimations.Count(a => a.Selected);
            int selectedAll =
                selectedNew
                + selectedChanged
                + (_includeUnchangedInCopyAll ? _unchangedAnimations.Count(a => a.Selected) : 0);

            bool canCopyNew = canAnalyze && analysisDone && selectedNew > 0;
            bool canCopyChanged = canAnalyze && analysisDone && selectedChanged > 0;
            bool canCopyAll = canAnalyze && analysisDone && selectedAll > 0;

            EditorGUI.BeginDisabledGroup(!canCopyNew);
            if (GUILayout.Button($"Copy New ({selectedNew})"))
            {
                if (
                    Confirm(
                        "Confirm Copy New",
                        $"Copy {selectedNew} new animation(s) from '{_animationSourcePathRelative}' to '{_animationDestinationPathRelative}'{(_dryRun ? " (dry run)" : string.Empty)}?",
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
            if (GUILayout.Button($"Copy Changed ({selectedChanged})"))
            {
                if (
                    Confirm(
                        "Confirm Copy Changed",
                        $"Copy {selectedChanged} changed animation(s) from '{_animationSourcePathRelative}' to '{_animationDestinationPathRelative}', overwriting existing files{(_dryRun ? " (dry run)" : string.Empty)}?",
                        "Yes, Copy Changed",
                        "Cancel"
                    )
                )
                {
                    CopyAnimationsInternal(CopyMode.Changed);
                }
            }
            EditorGUI.EndDisabledGroup();

            // Copy All will optionally include unchanged if requested
            int totalToCopyAll = selectedAll;
            EditorGUI.BeginDisabledGroup(!canCopyAll);
            if (GUILayout.Button($"Copy All ({totalToCopyAll})"))
            {
                string overwriteWarning =
                    selectedChanged
                        + (
                            _includeUnchangedInCopyAll
                                ? _unchangedAnimations.Count(a => a.Selected)
                                : 0
                        )
                    > 0
                        ? $" This will overwrite {selectedChanged + (_includeUnchangedInCopyAll ? _unchangedAnimations.Count(a => a.Selected) : 0)} existing files."
                        : "";
                if (
                    Confirm(
                        "Confirm Copy All",
                        $"Copy {totalToCopyAll} animation(s) from '{_animationSourcePathRelative}' to '{_animationDestinationPathRelative}'?{overwriteWarning}{(_dryRun ? " (dry run)" : string.Empty)}",
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
            bool hasOrphans = _destinationOrphans.Any();

            _dryRun = EditorGUILayout.ToggleLeft("Dry Run (no changes)", _dryRun);

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

            EditorGUILayout.Space();
            // Mirror delete destination-only clips
            if (canAnalyze && analysisDone && hasOrphans)
            {
                Color originalColor = GUI.color;
                GUI.color = new Color(1f, 0.5f, 0f); // orange

                string buttonText =
                    $"Mirror Delete Destination Orphans ({_destinationOrphans.Count})";
                if (GUILayout.Button(buttonText))
                {
                    MirrorDeleteDestinationAnimations();
                }

                GUI.color = originalColor;
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button("Mirror Delete Destination Orphans (None found)");
                EditorGUI.EndDisabledGroup();
            }
        }

        private void ValidatePaths(bool logWarnings = false)
        {
            _fullSourcePath = GetFullPathFromRelative(_animationSourcePathRelative);
            _fullDestinationPath = GetFullPathFromRelative(_animationDestinationPathRelative);

            if (_fullSourcePath == null || !Directory.Exists(_fullSourcePath))
            {
                if (logWarnings && !SuppressUserPrompts)
                {
                    this.LogWarn(
                        $"Source path '{_animationSourcePathRelative}' is invalid or outside the project. Please set a valid path within Assets."
                    );
                }
                _fullSourcePath = null;
                _analysisNeeded = true;
                ClearAnalysisResults();
            }
            if (_fullDestinationPath == null)
            {
                if (logWarnings && !SuppressUserPrompts)
                {
                    this.LogWarn(
                        $"Destination path '{_animationDestinationPathRelative}' is invalid or outside the project. Please set a valid path within Assets."
                    );
                }
                _analysisNeeded = true;
                ClearAnalysisResults();
            }
            else
            {
                string parentDir = Path.GetDirectoryName(_fullDestinationPath);
                if (!Directory.Exists(parentDir))
                {
                    if (logWarnings && !SuppressUserPrompts)
                    {
                        this.LogWarn(
                            $"The parent directory for the destination path '{_animationDestinationPathRelative}' does not exist ('{parentDir}'). Copy operations may fail to create folders."
                        );
                    }
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

        internal void AnalyzeAnimations()
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
                // Pre-size to reduce reallocations
                if (sourceGuids != null)
                {
                    _sourceAnimations.Capacity = Math.Max(
                        _sourceAnimations.Capacity,
                        sourceGuids.Length
                    );
                    _newAnimations.Capacity = Math.Max(_newAnimations.Capacity, sourceGuids.Length);
                    _changedAnimations.Capacity = Math.Max(
                        _changedAnimations.Capacity,
                        sourceGuids.Length
                    );
                    _unchangedAnimations.Capacity = Math.Max(
                        _unchangedAnimations.Capacity,
                        sourceGuids.Length
                    );
                }

                if (sourceGuids != null)
                {
                    float total = sourceGuids.Length * 2;
                    int current = 0;

                    ShowProgress("Analyzing Animations", "Gathering source files...", 0f);

                    int throttleCounter = 0;
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
                            Hash = GetDependencyHashString(sourceRelPath),
                        };
                        fileInfo.DestinationRelativePath = Path.Combine(
                                _animationDestinationPathRelative,
                                fileInfo.RelativeDirectory,
                                fileInfo.FileName
                            )
                            .SanitizePath();
                        _sourceAnimations.Add(fileInfo);

                        // Throttle progress updates
                        if (++throttleCounter % 10 == 0)
                        {
                            ShowProgress(
                                "Analyzing Animations",
                                $"Hashing: {fileInfo.FileName}",
                                current / total
                            );
                        }
                    }

                    this.Log(
                        $"Found {_sourceAnimations.Count} animations in source. Comparing with destination..."
                    );

                    for (int i = 0; i < _sourceAnimations.Count; i++)
                    {
                        AnimationFileInfo sourceInfo = _sourceAnimations[i];
                        current++;
                        if (i % 10 == 0 || i == _sourceAnimations.Count - 1)
                        {
                            ShowProgress(
                                "Analyzing Animations",
                                $"Comparing: {sourceInfo.FileName}",
                                current / total
                            );
                        }

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
                            string destHash = GetDependencyHashString(destRelPath);
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

                    // Compute destination-only (orphans) for mirror delete
                    try
                    {
                        _destinationOrphans.Clear();
                        HashSet<string> expectedDestPaths = new(StringComparer.OrdinalIgnoreCase);
                        foreach (AnimationFileInfo info in _sourceAnimations)
                        {
                            if (!string.IsNullOrWhiteSpace(info.DestinationRelativePath))
                            {
                                expectedDestPaths.Add(info.DestinationRelativePath);
                            }
                        }

                        string[] destGuids = AssetDatabase.FindAssets(
                            "t:AnimationClip",
                            new[] { _animationDestinationPathRelative }
                        );
                        int local = 0;
                        foreach (string dGuid in destGuids)
                        {
                            local++;
                            string destRelPath = AssetDatabase.GUIDToAssetPath(dGuid);
                            if (string.IsNullOrWhiteSpace(destRelPath))
                            {
                                continue;
                            }
                            if (expectedDestPaths.Contains(destRelPath))
                            {
                                continue;
                            }
                            string destFullPath = GetFullPathFromRelative(destRelPath);
                            AnimationFileInfo orphan = new()
                            {
                                RelativePath = null,
                                FullPath = destFullPath,
                                FileName = Path.GetFileName(destRelPath),
                                RelativeDirectory = GetRelativeSubPath(
                                    _animationDestinationPathRelative,
                                    Path.GetDirectoryName(destRelPath).SanitizePath()
                                ),
                                Hash = GetDependencyHashString(destRelPath),
                                Status = AnimationStatus.Unknown,
                                DestinationRelativePath = destRelPath,
                                Selected = true,
                            };
                            _destinationOrphans.Add(orphan);

                            if (local % 20 == 0)
                            {
                                ShowProgress(
                                    "Analyzing Animations",
                                    $"Scanning destination: {orphan.FileName}",
                                    current / total
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.LogError(
                            $"Error while scanning destination for orphans: {ex.Message}"
                        );
                    }
                }

                this.Log(
                    $"Analysis complete: {_newAnimations.Count} New, {_changedAnimations.Count} Changed, {_unchangedAnimations.Count} Unchanged, {_destinationOrphans.Count} Orphans."
                );
            }
            catch (Exception ex)
            {
                this.LogError($"Error during analysis: {ex.Message}\n{ex.StackTrace}");
                Info("Analysis Error", $"An error occurred during analysis: {ex.Message}");
                ClearAnalysisResults();
            }
            finally
            {
                _isAnalyzing = false;
                _analysisNeeded = false;
                ClearProgress();
                Repaint();
            }
        }

        internal void CopyAnimationsInternal(CopyMode mode)
        {
            if (!ArePathsValid() || _isAnalyzing || _isCopying || _isDeleting)
            {
                return;
            }

            using PooledResource<List<AnimationFileInfo>> pooled =
                Buffers<AnimationFileInfo>.List.Get(out List<AnimationFileInfo> animationsToCopy);
            switch (mode)
            {
                case CopyMode.All:
                    // Skip unchanged by default to avoid needless errors
                    animationsToCopy.AddRange(_newAnimations);
                    animationsToCopy.AddRange(_changedAnimations);
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
                Info("Nothing to Copy", "There are no animations matching the selected criteria.");
                return;
            }

            // Respect user selection
            animationsToCopy.RemoveAll(info => info == null || !info.Selected);
            if (animationsToCopy.Count == 0)
            {
                Info("Nothing Selected", "No animations are selected for the operation.");
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
                    bool userCancelled = false;
                    if (i == 0 || i % 10 == 0 || i == animationsToCopy.Count - 1)
                    {
                        userCancelled = CancelableProgress(
                            $"Copying Animations ({mode})",
                            $"Copying: {animInfo.FileName} ({i + 1}/{animationsToCopy.Count})",
                            progress
                        );
                    }

                    if (userCancelled)
                    {
                        this.LogWarn($"Copy operation cancelled by user.");
                        break;
                    }

                    string sourceAssetPath = animInfo.RelativePath;
                    string destinationAssetPath = animInfo.DestinationRelativePath;
                    bool operationSuccessful = false;
                    try
                    {
                        string destFullPath = GetFullPathFromRelative(destinationAssetPath);
                        bool destExists =
                            !string.IsNullOrWhiteSpace(destFullPath) && File.Exists(destFullPath);

                        if (_dryRun)
                        {
                            // Simulate
                            operationSuccessful = true;
                        }
                        else if (!destExists || animInfo.Status == AnimationStatus.New)
                        {
                            operationSuccessful = AssetDatabase.CopyAsset(
                                sourceAssetPath,
                                destinationAssetPath
                            );
                        }
                        else if (animInfo.Status == AnimationStatus.Changed)
                        {
                            // Preserve GUID: replace file on disk and reimport
                            string sourceFullPath = animInfo.FullPath;
                            if (
                                !string.IsNullOrWhiteSpace(sourceFullPath)
                                && !string.IsNullOrWhiteSpace(destFullPath)
                            )
                            {
                                FileUtil.ReplaceFile(sourceFullPath, destFullPath);
                                AssetDatabase.ImportAsset(
                                    destinationAssetPath,
                                    ImportAssetOptions.ForceUpdate
                                );
                                operationSuccessful = true;
                            }
                        }
                        else
                        {
                            // Unchanged
                            if (_includeUnchangedInCopyAll && mode == CopyMode.All)
                            {
                                string sourceFullPath = animInfo.FullPath;
                                if (
                                    !string.IsNullOrWhiteSpace(sourceFullPath)
                                    && !string.IsNullOrWhiteSpace(destFullPath)
                                )
                                {
                                    FileUtil.ReplaceFile(sourceFullPath, destFullPath);
                                    AssetDatabase.ImportAsset(
                                        destinationAssetPath,
                                        ImportAssetOptions.ForceUpdate
                                    );
                                    operationSuccessful = true;
                                }
                            }
                            else
                            {
                                // skip unchanged by default
                                operationSuccessful = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.LogError(
                            $"Failed to copy/replace '{sourceAssetPath}' -> '{destinationAssetPath}'. {ex.Message}"
                        );
                        operationSuccessful = false;
                    }

                    if (operationSuccessful)
                    {
                        successCount++;
                    }
                    else
                    {
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
                if (!_dryRun)
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                ClearProgress();
                _isCopying = false;
                this.Log(
                    $"Copy operation finished{(_dryRun ? " (dry run)" : string.Empty)}. Mode: {mode}. Success: {successCount}, Errors: {errorCount}."
                );

                Info(
                    "Copy Complete",
                    $"Copy operation finished{(_dryRun ? " (dry run)" : string.Empty)}.\nMode: {mode}\nItems processed: {successCount + errorCount}\nSuccessful: {successCount}\nErrors: {errorCount}\n\nSee console log for details."
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

            List<AnimationFileInfo> animationsToDelete = _unchangedAnimations
                .Where(a => a is { Selected: true })
                .ToList();

            if (animationsToDelete.Count == 0)
            {
                this.Log($"No unchanged source animations to delete.");

                return;
            }

            bool confirm = Confirm(
                "Confirm Delete Unchanged",
                $"Delete {animationsToDelete.Count} unchanged source animation(s) from '{_animationSourcePathRelative}'?\n\nThese files are duplicates of the destination and will be moved to Trash.{(_dryRun ? "\n\nDry run is ON: no files will be changed." : string.Empty)}",
                _dryRun ? "OK" : "Yes, Delete",
                "Cancel"
            );
            if (!confirm)
            {
                return;
            }

            this.Log(
                $"Starting delete operation for {animationsToDelete.Count} unchanged source animations..."
            );
            _isDeleting = true;
            Repaint();

            int successCount = 0;
            int errorCount = 0;
            if (!_dryRun)
            {
                AssetDatabase.StartAssetEditing();
            }

            try
            {
                for (int i = 0; i < animationsToDelete.Count; i++)
                {
                    AnimationFileInfo animInfo = animationsToDelete[i];
                    float progress = (float)(i + 1) / animationsToDelete.Count;
                    bool userCancelled = false;
                    if (i == 0 || i % 10 == 0 || i == animationsToDelete.Count - 1)
                    {
                        userCancelled = CancelableProgress(
                            "Deleting Source Duplicates",
                            $"Deleting: {animInfo.FileName} ({i + 1}/{animationsToDelete.Count})",
                            progress
                        );
                    }

                    if (userCancelled)
                    {
                        this.LogWarn($"Delete operation cancelled by user.");
                        break;
                    }

                    bool deleteSuccessful = true;
                    string sourceAssetPath = animInfo.RelativePath;
                    if (!_dryRun)
                    {
                        deleteSuccessful = AssetDatabase.DeleteAsset(sourceAssetPath);
                    }

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
                if (!_dryRun)
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.Refresh();
                }
                ClearProgress();
                _isDeleting = false;
                this.Log(
                    $"Delete operation finished{(_dryRun ? " (dry run)" : string.Empty)}. Successfully processed: {successCount}, Errors: {errorCount}."
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
            _destinationOrphans.Clear();

            Repaint();
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

        private string GetDependencyHashString(string assetPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    return string.Empty;
                }
                Hash128 hash = AssetDatabase.GetAssetDependencyHash(assetPath);
                return hash.ToString();
            }
            catch (Exception ex)
            {
                this.LogError(
                    $"[AnimationCopierWindow] Error getting dependency hash for {assetPath}.",
                    ex
                );
                return string.Empty;
            }
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            _dryRun = EditorGUILayout.ToggleLeft("Dry Run (no changes)", _dryRun);
            using (new EditorGUILayout.HorizontalScope())
            {
                _filterText = EditorGUILayout.TextField(new GUIContent("Filter"), _filterText);
                _filterUseRegex = EditorGUILayout.ToggleLeft(
                    "Regex",
                    _filterUseRegex,
                    GUILayout.Width(60)
                );
                _sortAscending = EditorGUILayout.ToggleLeft(
                    "Sort Asc",
                    _sortAscending,
                    GUILayout.Width(80)
                );
                if (GUILayout.Button("Export Preview Report", GUILayout.Width(180)))
                {
                    ExportPreviewReport();
                }
            }
            _previewFoldout = EditorGUILayout.Foldout(_previewFoldout, "Show Preview Lists", true);
            if (!_previewFoldout)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(GUILayout.Height(250)))
            {
                _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll);

                DrawPreviewGroup(ref _newFoldout, "New", _newAnimations, useSourcePath: true);
                DrawPreviewGroup(
                    ref _changedFoldout,
                    "Changed",
                    _changedAnimations,
                    useSourcePath: true
                );
                DrawPreviewGroup(
                    ref _unchangedFoldout,
                    "Unchanged (Source Duplicates)",
                    _unchangedAnimations,
                    useSourcePath: true
                );
                DrawPreviewGroup(
                    ref _orphansFoldout,
                    "Destination Orphans",
                    _destinationOrphans,
                    useSourcePath: false
                );

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawPreviewGroup(
            ref bool foldout,
            string inputTitle,
            List<AnimationFileInfo> items,
            bool useSourcePath
        )
        {
            if (items == null)
            {
                return;
            }
            IEnumerable<AnimationFileInfo> filtered = ApplyFilterAndSort(items);
            AnimationFileInfo[] animationFileInfos =
                filtered as AnimationFileInfo[] ?? filtered.ToArray();
            int count = animationFileInfos.Length;
            if (count == 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField($"{inputTitle}: None", EditorStyles.miniLabel);
                }
                return;
            }

            foldout = EditorGUILayout.Foldout(foldout, $"{inputTitle} ({count})", true);
            if (!foldout)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Select All", GUILayout.Width(100)))
                    {
                        foreach (AnimationFileInfo info in animationFileInfos)
                        {
                            info.Selected = true;
                        }
                    }
                    if (GUILayout.Button("Select None", GUILayout.Width(100)))
                    {
                        foreach (AnimationFileInfo info in animationFileInfos)
                        {
                            info.Selected = false;
                        }
                    }
                    if (GUILayout.Button("Select Filtered", GUILayout.Width(120)))
                    {
                        foreach (AnimationFileInfo info in animationFileInfos)
                        {
                            info.Selected = true;
                        }
                    }
                    if (GUILayout.Button("Clear Filtered", GUILayout.Width(120)))
                    {
                        foreach (AnimationFileInfo info in animationFileInfos)
                        {
                            info.Selected = false;
                        }
                    }
                }

                foreach (AnimationFileInfo info in animationFileInfos)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        info.Selected = EditorGUILayout.Toggle(info.Selected, GUILayout.Width(20));
                        EditorGUILayout.LabelField(info.FileName, GUILayout.MinWidth(100));
                        if (GUILayout.Button("Ping", GUILayout.Width(60)))
                        {
                            string assetPath = useSourcePath
                                ? info.RelativePath
                                : info.DestinationRelativePath;
                            if (!string.IsNullOrWhiteSpace(assetPath))
                            {
                                UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(
                                    assetPath
                                );
                                if (obj != null)
                                {
                                    EditorGUIUtility.PingObject(obj);
                                }
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<AnimationFileInfo> ApplyFilterAndSort(List<AnimationFileInfo> items)
        {
            IEnumerable<AnimationFileInfo> query = items;
            if (!string.IsNullOrWhiteSpace(_filterText))
            {
                if (_filterUseRegex)
                {
                    try
                    {
                        Regex rx = new(_filterText, RegexOptions.IgnoreCase);
                        query = query.Where(i =>
                            i is { FileName: not null } && rx.IsMatch(i.FileName)
                        );
                    }
                    catch (Exception ex)
                    {
                        this.LogWarn($"Invalid regex '{_filterText}': {ex.Message}");
                        query = Enumerable.Empty<AnimationFileInfo>();
                    }
                }
                else
                {
                    query = query.Where(i =>
                        i is { FileName: not null }
                        && i.FileName.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0
                    );
                }
            }
            query = _sortAscending
                ? query.OrderBy(i => i.FileName, StringComparer.OrdinalIgnoreCase)
                : query.OrderByDescending(i => i.FileName, StringComparer.OrdinalIgnoreCase);
            return query;
        }

        internal void MirrorDeleteDestinationAnimations()
        {
            if (!ArePathsValid() || _isAnalyzing || _isCopying || _isDeleting)
            {
                return;
            }

            List<AnimationFileInfo> toDelete = _destinationOrphans
                .Where(a => a is { Selected: true })
                .ToList();
            if (toDelete.Count == 0)
            {
                Info("Nothing to Delete", "No destination orphans are selected.");
                return;
            }

            bool confirm = Confirm(
                "Confirm Mirror Delete",
                $"Delete {toDelete.Count} destination-only animation(s) from '{_animationDestinationPathRelative}'.{(_dryRun ? "\n\nDry run is ON: no files will be changed." : string.Empty)}",
                _dryRun ? "OK" : "Yes, Delete",
                "Cancel"
            );
            if (!confirm)
            {
                return;
            }

            this.Log($"Starting mirror delete for {toDelete.Count} orphan animations...");
            _isDeleting = true;
            Repaint();

            int success = 0;
            int errors = 0;
            if (!_dryRun)
            {
                AssetDatabase.StartAssetEditing();
            }
            try
            {
                for (int i = 0; i < toDelete.Count; i++)
                {
                    AnimationFileInfo info = toDelete[i];
                    float progress = (float)(i + 1) / toDelete.Count;
                    bool userCancelled = false;
                    if (i == 0 || i % 10 == 0 || i == toDelete.Count - 1)
                    {
                        userCancelled = CancelableProgress(
                            "Mirror Deleting Destination Orphans",
                            $"Deleting: {info.FileName} ({i + 1}/{toDelete.Count})",
                            progress
                        );
                    }
                    if (userCancelled)
                    {
                        this.LogWarn($"Mirror delete cancelled by user.");
                        break;
                    }

                    bool ok = true;
                    if (!_dryRun)
                    {
                        ok = AssetDatabase.DeleteAsset(info.DestinationRelativePath);
                    }
                    if (ok)
                    {
                        success++;
                    }
                    else
                    {
                        errors++;
                    }
                }
            }
            catch (Exception ex)
            {
                this.LogError($"Error during mirror delete: {ex.Message}\n{ex.StackTrace}");
                errors = toDelete.Count - success;
            }
            finally
            {
                if (!_dryRun)
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.Refresh();
                }
                ClearProgress();
                _isDeleting = false;
                this.Log(
                    $"Mirror delete finished{(_dryRun ? " (dry run)" : string.Empty)}. Success: {success}, Errors: {errors}."
                );
                _analysisNeeded = true;
                Repaint();
            }
        }

        // Convenience wrappers for tests
        internal void CopyChanged() => CopyAnimationsInternal(CopyMode.Changed);

        internal void CopyNew() => CopyAnimationsInternal(CopyMode.New);

        internal void CopyAll() => CopyAnimationsInternal(CopyMode.All);

        private static bool Confirm(string title, string message, string ok, string cancel)
        {
            return Utils.EditorUi.Confirm(title, message, ok, cancel, defaultWhenSuppressed: true);
        }

        private static void Info(string title, string message)
        {
            Utils.EditorUi.Info(title, message);
        }

        private static void ShowProgress(string title, string info, float progress)
        {
            Utils.EditorUi.ShowProgress(title, info, progress);
        }

        private static bool CancelableProgress(string title, string info, float progress)
        {
            return Utils.EditorUi.CancelableProgress(title, info, progress);
        }

        private static void ClearProgress()
        {
            Utils.EditorUi.ClearProgress();
        }

        private void ExportPreviewReport()
        {
            try
            {
                StringBuilder sb = new(4096);
                sb.AppendLine(
                    $"Animation Copier Preview Report - {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                );
                sb.AppendLine($"Source: {_animationSourcePathRelative}");
                sb.AppendLine($"Destination: {_animationDestinationPathRelative}");
                sb.AppendLine($"Dry Run: {_dryRun}");
                sb.AppendLine($"Include Unchanged in Copy All: {_includeUnchangedInCopyAll}");
                sb.AppendLine(
                    $"Filter: '{_filterText}' (Regex={_filterUseRegex}) SortAsc={_sortAscending}"
                );
                sb.AppendLine();

                void DumpGroup(
                    string inputTitle,
                    IEnumerable<AnimationFileInfo> list,
                    bool useSource
                )
                {
                    AnimationFileInfo[] arr = ApplyFilterAndSort(list.ToList()).ToArray();
                    sb.AppendLine($"== {inputTitle} ({arr.Length}) ==");
                    foreach (AnimationFileInfo info in arr)
                    {
                        string path = useSource ? info.RelativePath : info.DestinationRelativePath;
                        sb.AppendLine(
                            $"[{(info.Selected ? 'x' : ' ')}] {info.FileName}  ->  {path}"
                        );
                    }
                    sb.AppendLine();
                }

                DumpGroup("New", _newAnimations, true);
                DumpGroup("Changed", _changedAnimations, true);
                DumpGroup("Unchanged (Source Duplicates)", _unchangedAnimations, true);
                DumpGroup("Destination Orphans", _destinationOrphans, false);

                string savePath = EditorUtility.SaveFilePanel(
                    "Export Preview Report",
                    Application.dataPath,
                    "AnimationCopierReport.txt",
                    "txt"
                );
                if (!string.IsNullOrWhiteSpace(savePath))
                {
                    File.WriteAllText(savePath, sb.ToString());
                    EditorUtility.RevealInFinder(savePath);
                }
            }
            catch (Exception ex)
            {
                this.LogError($"Failed to export preview report: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void RevealFolder(string relativeAssetsPath)
        {
            try
            {
                string full = GetFullPathFromRelative(relativeAssetsPath);
                if (
                    !string.IsNullOrWhiteSpace(full)
                    && (Directory.Exists(full) || File.Exists(full))
                )
                {
                    EditorUtility.RevealInFinder(full);
                }
                else
                {
                    this.LogWarn($"Cannot open folder: '{relativeAssetsPath}'");
                }
            }
            catch (Exception ex)
            {
                this.LogError($"Failed to open folder '{relativeAssetsPath}': {ex.Message}");
            }
        }
    }
#endif
}

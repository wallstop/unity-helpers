namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;
    using CustomEditors;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Utils;
    using Object = UnityEngine.Object;

    public enum FitMode
    {
        GrowAndShrink = 0,
        GrowOnly = 1,
        ShrinkOnly = 2,
        RoundToNearest = 3,
    }

    public sealed class FitTextureSizeWindow : EditorWindow
    {
        private static bool SuppressUserPrompts { get; set; }

        static FitTextureSizeWindow()
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
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; ++i)
            {
                string a = args[i];
                if (
                    a.IndexOf("runTests", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || a.IndexOf("testResults", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || a.IndexOf("testPlatform", System.StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    return true;
                }
            }
            return false;
        }

        internal FitMode _fitMode = FitMode.GrowAndShrink;

        [SerializeField]
        internal List<Object> _textureSourcePaths = new();
        private Vector2 _scrollPosition = Vector2.zero;
        private SerializedObject _serializedObject;
        private SerializedProperty _textureSourcePathsProperty;
        private int _potentialChangeCount = -1;
        private int _potentialGrowCount = 0;
        private int _potentialShrinkCount = 0;
        private int _potentialUnchangedCount = 0;

        [SerializeField]
        internal bool _useSelectionOnly = false;

        [SerializeField]
        internal bool _onlySprites = false;

        [SerializeField]
        internal int _minAllowedTextureSize = 32;

        [SerializeField]
        internal int _maxAllowedTextureSize = 8192;

        [SerializeField]
        internal bool _applyToStandalone = false;

        [SerializeField]
        internal bool _applyToAndroid = false;

        [SerializeField]
        internal bool _applyToiOS = false;

        [SerializeField]
        internal string _nameFilter = string.Empty;

        [SerializeField]
        internal bool _useRegexForName = false;

        [SerializeField]
        internal bool _caseSensitiveNameFilter = false;

        [SerializeField]
        internal string _labelFilterCsv = string.Empty;

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Fit Texture Size", priority = -1)]
        public static void ShowWindow()
        {
            GetWindow<FitTextureSizeWindow>("Fit Texture Size");
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _textureSourcePathsProperty = _serializedObject.FindProperty(
                nameof(_textureSourcePaths)
            );

            if (_textureSourcePaths is { Count: > 0 })
            {
                return;
            }

            _textureSourcePaths = new List<Object>();
            Object defaultFolder = AssetDatabase.LoadAssetAtPath<Object>("Assets/Sprites");
            if (defaultFolder == null)
            {
                return;
            }

            _textureSourcePaths.Add(defaultFolder);
            _serializedObject.Update();
        }

        private void OnGUI()
        {
            _serializedObject.Update();
            bool beganScroll = false;
            try
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                beganScroll = true;

                EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
                _fitMode = (FitMode)
                    EditorGUILayout.EnumPopup(
                        new GUIContent(
                            "Fit Mode",
                            "GrowAndShrink: increase or decrease to closest POT bound around size.\nGrowOnly: increase up to next POT if needed, never shrink.\nShrinkOnly: decrease down to tightest POT <= size, never grow.\nRoundToNearest: choose nearest POT to source size (ties round up)."
                        ),
                        _fitMode
                    );

                // Inline help for current mode
                string modeHelp = _fitMode switch
                {
                    FitMode.GrowAndShrink => "Grow or shrink to bound POT around the source size.",
                    FitMode.GrowOnly =>
                        "Only increase max size to the next POT if the source exceeds it.",
                    FitMode.ShrinkOnly =>
                        "Only decrease to the tightest POT that still fits the source.",
                    FitMode.RoundToNearest =>
                        "Choose the nearest power-of-two to the source size (ties up).",
                    _ => string.Empty,
                };
                if (!string.IsNullOrEmpty(modeHelp))
                {
                    EditorGUILayout.HelpBox(modeHelp, MessageType.None);
                }

                _useSelectionOnly = EditorGUILayout.Toggle(
                    new GUIContent(
                        "Only Current Selection",
                        "When enabled, only process assets and folders currently selected in the Project window."
                    ),
                    _useSelectionOnly
                );
                _onlySprites = EditorGUILayout.Toggle(
                    new GUIContent(
                        "Only Sprites",
                        "When enabled, only process textures whose importer type is Sprite."
                    ),
                    _onlySprites
                );

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
                _nameFilter = EditorGUILayout.TextField(
                    new GUIContent(
                        "Name Filter",
                        "Filter textures by filename (without extension). Leave empty for no name filtering."
                    ),
                    _nameFilter
                );
                _useRegexForName = EditorGUILayout.Toggle(
                    new GUIContent("Use Regex", "Interpret Name Filter as a regular expression."),
                    _useRegexForName
                );
                _caseSensitiveNameFilter = EditorGUILayout.Toggle(
                    new GUIContent(
                        "Case Sensitive",
                        "Apply case-sensitive matching for the Name Filter."
                    ),
                    _caseSensitiveNameFilter
                );
                _labelFilterCsv = EditorGUILayout.TextField(
                    new GUIContent(
                        "Label Filter (CSV)",
                        "Comma-separated list of asset labels. When provided, only assets containing at least one of these labels are processed."
                    ),
                    _labelFilterCsv
                );

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Bounds", EditorStyles.boldLabel);
                _minAllowedTextureSize = Mathf.Clamp(
                    EditorGUILayout.IntField(
                        new GUIContent(
                            "Min Allowed Size",
                            "Lower clamp for computed maxTextureSize. Final applied size will not be less than this value."
                        ),
                        _minAllowedTextureSize
                    ),
                    1,
                    16384
                );
                _maxAllowedTextureSize = Mathf.Clamp(
                    EditorGUILayout.IntField(
                        new GUIContent(
                            "Max Allowed Size",
                            "Upper clamp for computed maxTextureSize. Final applied size will not exceed this value."
                        ),
                        _maxAllowedTextureSize
                    ),
                    _minAllowedTextureSize,
                    16384
                );

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Platform Overrides", EditorStyles.boldLabel);
                _applyToStandalone = EditorGUILayout.Toggle(
                    new GUIContent(
                        "Apply to Standalone",
                        "Also apply computed max size to Standalone platform override."
                    ),
                    _applyToStandalone
                );
                _applyToAndroid = EditorGUILayout.Toggle(
                    new GUIContent(
                        "Apply to Android",
                        "Also apply computed max size to Android platform override."
                    ),
                    _applyToAndroid
                );
                _applyToiOS = EditorGUILayout.Toggle(
                    new GUIContent(
                        "Apply to iOS",
                        "Also apply computed max size to iOS platform override."
                    ),
                    _applyToiOS
                );

                EditorGUILayout.Space();
                PersistentDirectoryGUI.PathSelectorObjectArray(
                    _textureSourcePathsProperty,
                    nameof(FitTextureSizeWindow)
                );
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

                if (GUILayout.Button("Calculate Potential Changes"))
                {
                    _potentialChangeCount = CalculateTextureChanges(applyChanges: false);
                    string message =
                        _potentialChangeCount >= 0
                            ? $"Calculation complete. {_potentialChangeCount} textures would be modified."
                            : "Calculation failed.";
                    this.Log($"{message}");
                }

                if (_potentialChangeCount >= 0)
                {
                    EditorGUILayout.HelpBox(
                        $"{_potentialChangeCount} textures would be modified with the current settings. Grows: {_potentialGrowCount}, Shrinks: {_potentialShrinkCount}, Unchanged: {_potentialUnchangedCount}.",
                        MessageType.Info
                    );
                    if (GUILayout.Button("Copy Summary"))
                    {
                        string summary =
                            $"Fit Texture Size Summary\nMode: {_fitMode}\nOnly Selection: {_useSelectionOnly}\nOnly Sprites: {_onlySprites}\nMin: {_minAllowedTextureSize}, Max: {_maxAllowedTextureSize}\nTotal Changes: {_potentialChangeCount}\nGrows: {_potentialGrowCount}, Shrinks: {_potentialShrinkCount}, Unchanged: {_potentialUnchangedCount}";
                        EditorGUIUtility.systemCopyBuffer = summary;
                        this.Log($"Summary copied to clipboard.");
                    }
                }

                if (GUILayout.Button("Run Fit Texture Size"))
                {
                    int actualChanges = CalculateTextureChanges(applyChanges: true);
                    _potentialChangeCount = -1;
                    string message =
                        actualChanges >= 0
                            ? $"Operation complete. {actualChanges} textures were modified."
                            : "Operation failed.";
                    this.Log($"{message}");
                }
            }
            finally
            {
                if (beganScroll)
                {
                    EditorGUILayout.EndScrollView();
                }
            }
            _serializedObject.ApplyModifiedProperties();
        }

        private List<string> CollectAssetGuids()
        {
            _textureSourcePaths ??= new List<Object>();

            using PooledResource<HashSet<string>> uniqRes = SetBuffers<string>.HashSet.Get(
                out HashSet<string> uniqueAssetPaths
            );
            using PooledResource<List<string>> searchRes = Buffers<string>.List.Get(
                out List<string> searchPaths
            );
            using PooledResource<HashSet<string>> guidSetRes = SetBuffers<string>.HashSet.Get(
                out HashSet<string> guidSet
            );
            using PooledResource<List<string>> resultRes = Buffers<string>.List.Get(
                out List<string> result
            );

            if (_useSelectionOnly)
            {
                string[] selGuids = Selection.assetGUIDs;
                foreach (string guid in selGuids)
                {
                    string selPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrWhiteSpace(selPath))
                    {
                        continue;
                    }

                    if (AssetDatabase.IsValidFolder(selPath))
                    {
                        _ = uniqueAssetPaths.Add(selPath);
                    }
                    else
                    {
                        // Include selected assets directly; filtering is applied later.
                        _ = guidSet.Add(guid);
                    }
                }
            }
            else
            {
                foreach (Object sourceObject in _textureSourcePaths)
                {
                    if (sourceObject == null)
                    {
                        continue;
                    }

                    string assetPath = AssetDatabase.GetAssetPath(sourceObject);
                    if (string.IsNullOrWhiteSpace(assetPath))
                    {
                        continue;
                    }

                    if (AssetDatabase.IsValidFolder(assetPath))
                    {
                        _ = uniqueAssetPaths.Add(assetPath);
                    }
                    else
                    {
                        this.LogWarn($"Skipping non-folder object: '{assetPath}'");
                    }
                }
            }

            if (!uniqueAssetPaths.Any())
            {
                if (_useSelectionOnly)
                {
                    // Selection-only mode with no folders selected: rely on direct GUIDs only.
                }
                else if (_textureSourcePaths.Any(o => o != null))
                {
                    this.LogWarn($"No valid source folders found in the list.");
                }
                else
                {
                    this.Log($"No source folders specified. Searching entire 'Assets' folder.");
                    searchPaths.Add("Assets");
                }
            }
            else
            {
                searchPaths.AddRange(uniqueAssetPaths);
            }

            if (searchPaths.Count > 0)
            {
                // Query all textures; specific filtering is performed later against importers.
                string[] guids = AssetDatabase.FindAssets("t:texture2D", searchPaths.ToArray());
                for (int i = 0; i < guids.Length; i++)
                {
                    _ = guidSet.Add(guids[i]);
                }
            }

            // Output consolidated GUID list.
            foreach (string g in guidSet)
            {
                result.Add(g);
            }

            return new List<string>(result);
        }

        internal int CalculateTextureChanges(bool applyChanges)
        {
            List<string> textureGuids = CollectAssetGuids();

            if (textureGuids.Count <= 0)
            {
                this.Log($"No textures found in the specified paths.");
                return 0;
            }

            int changedCount = 0;
            int growCount = 0;
            int shrinkCount = 0;
            int unchangedCount = 0;
            // Prepare filters
            Regex nameRegex = null;
            if (!string.IsNullOrWhiteSpace(_nameFilter) && _useRegexForName)
            {
                RegexOptions opts = _caseSensitiveNameFilter
                    ? RegexOptions.None
                    : RegexOptions.IgnoreCase;
                try
                {
                    nameRegex = new Regex(_nameFilter, opts);
                }
                catch (System.Exception ex)
                {
                    this.LogError($"Invalid name regex '{_nameFilter}': {ex.Message}");
                    nameRegex = null;
                }
            }
            bool hasNameFilter = !string.IsNullOrWhiteSpace(_nameFilter);
            PooledResource<HashSet<string>> labelSetRes = default;
            HashSet<string> labelSet = null;
            string[] parsedLabels = null;
            if (!string.IsNullOrWhiteSpace(_labelFilterCsv))
            {
                parsedLabels = _labelFilterCsv
                    .Split(new[] { ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToArray();
                if (parsedLabels.Length > 0)
                {
                    labelSetRes = SetBuffers<string>.HashSet.Get(out labelSet);
                    for (int i = 0; i < parsedLabels.Length; i++)
                    {
                        _ = labelSet.Add(
                            _caseSensitiveNameFilter
                                ? parsedLabels[i]
                                : parsedLabels[i].ToLowerInvariant()
                        );
                    }
                }
            }
            if (applyChanges)
            {
                AssetDatabase.StartAssetEditing();
            }
            try
            {
                float totalAssets = textureGuids.Count;
                for (int i = 0; i < textureGuids.Count; i++)
                {
                    string guid = textureGuids[i];
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    float progress = (i + 1) / totalAssets;
                    string progressBarTitle = applyChanges
                        ? "Fitting Texture Size"
                        : "Calculating Changes";
                    bool cancel = false;
                    if (!SuppressUserPrompts)
                    {
                        // Throttle progress updates to reduce GC and UI overhead
                        if ((i % 32) == 0 || i == textureGuids.Count - 1)
                        {
                            cancel = EditorUtility.DisplayCancelableProgressBar(
                                progressBarTitle,
                                $"Checking: {Path.GetFileName(assetPath)} ({i + 1}/{textureGuids.Count})",
                                progress
                            );
                        }
                    }

                    if (cancel)
                    {
                        this.LogWarn($"Operation cancelled by user.");
                        return -1;
                    }

                    if (string.IsNullOrWhiteSpace(assetPath))
                    {
                        continue;
                    }

                    TextureImporter textureImporter =
                        AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (textureImporter == null)
                    {
                        continue;
                    }

                    if (_onlySprites && textureImporter.textureType != TextureImporterType.Sprite)
                    {
                        // Skip non-sprite textures when filtering by sprites only
                        continue;
                    }

                    // Name filter
                    if (hasNameFilter)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(assetPath);
                        bool nameMatch = false;
                        if (nameRegex != null)
                        {
                            nameMatch = nameRegex.IsMatch(fileName);
                        }
                        else
                        {
                            var comp = _caseSensitiveNameFilter
                                ? System.StringComparison.Ordinal
                                : System.StringComparison.OrdinalIgnoreCase;
                            nameMatch = fileName.IndexOf(_nameFilter, comp) >= 0;
                        }
                        if (!nameMatch)
                        {
                            continue;
                        }
                    }

                    // Label filter (load asset only when labels are requested)
                    if (labelSet != null)
                    {
                        Object main = AssetDatabase.LoadMainAssetAtPath(assetPath);
                        if (main == null)
                        {
                            continue;
                        }
                        string[] labels = AssetDatabase.GetLabels(main);
                        bool any = false;
                        for (int li = 0; li < labels.Length; li++)
                        {
                            string lab = _caseSensitiveNameFilter
                                ? labels[li]
                                : labels[li].ToLowerInvariant();
                            if (labelSet.Contains(lab))
                            {
                                any = true;
                                break;
                            }
                        }
                        if (!any)
                        {
                            continue;
                        }
                    }

                    textureImporter.GetSourceTextureWidthAndHeight(out int width, out int height);

                    float size = Mathf.Max(width, height);
                    int currentTextureSize = textureImporter.maxTextureSize;
                    int targetTextureSize = currentTextureSize;
                    bool needsChange = false;
                    bool grew = false;
                    bool shrank = false;

                    if (_fitMode == FitMode.RoundToNearest)
                    {
                        int largest = Mathf.Max(width, height);
                        int upper = Mathf.NextPowerOfTwo(Mathf.Max(largest, 1));
                        int lower = upper == largest ? upper : (upper >> 1);
                        int diffDown = largest - lower;
                        int diffUp = upper - largest;
                        int nearest = diffDown < diffUp ? lower : upper;
                        if (nearest != targetTextureSize)
                        {
                            targetTextureSize = nearest;
                            needsChange = true;
                        }
                    }
                    else if (_fitMode is FitMode.GrowAndShrink or FitMode.GrowOnly)
                    {
                        int tempSize = targetTextureSize;
                        while (tempSize < size)
                        {
                            tempSize <<= 1;
                        }
                        if (tempSize != targetTextureSize)
                        {
                            targetTextureSize = tempSize;
                            needsChange = true;
                        }
                    }

                    if (_fitMode is FitMode.GrowAndShrink or FitMode.ShrinkOnly)
                    {
                        int tempSize = targetTextureSize;
                        // Shrink to the tightest power-of-two that is <= the largest dimension (size),
                        // without dipping below 1. This matches test expectation for ShrinkOnly.
                        while (tempSize > 1 && tempSize > size)
                        {
                            tempSize >>= 1;
                        }
                        if (tempSize != targetTextureSize)
                        {
                            targetTextureSize = tempSize;
                            needsChange = true;
                        }
                        else if (!needsChange && tempSize != currentTextureSize)
                        {
                            targetTextureSize = tempSize;
                            needsChange = true;
                        }
                    }

                    // Clamp to allowed bounds (commonly Unity caps at 8192, expose as user setting)
                    if (targetTextureSize < _minAllowedTextureSize)
                    {
                        targetTextureSize = _minAllowedTextureSize;
                        needsChange = needsChange || (currentTextureSize != targetTextureSize);
                    }
                    if (targetTextureSize > _maxAllowedTextureSize)
                    {
                        targetTextureSize = _maxAllowedTextureSize;
                        needsChange = needsChange || (currentTextureSize != targetTextureSize);
                    }

                    // After clamping, determine net direction of change for counts
                    if (needsChange)
                    {
                        grew = targetTextureSize > currentTextureSize;
                        shrank = targetTextureSize < currentTextureSize;
                    }

                    if (!needsChange || currentTextureSize == targetTextureSize)
                    {
                        unchangedCount++;
                        continue;
                    }

                    changedCount++;
                    if (grew)
                    {
                        growCount++;
                    }
                    if (shrank)
                    {
                        shrinkCount++;
                    }
                    if (!applyChanges)
                    {
                        continue;
                    }

                    textureImporter.maxTextureSize = targetTextureSize;
                    // Apply platform overrides if requested
                    ApplyPlatformOverride(textureImporter, "Standalone", targetTextureSize);
                    ApplyPlatformOverride(textureImporter, "Android", targetTextureSize);
                    ApplyPlatformOverride(textureImporter, "iPhone", targetTextureSize);

                    // Persist only if dirty; avoid extra allocations and tracking
                    AssetDatabase.WriteImportSettingsIfDirty(assetPath);
                }
            }
            finally
            {
                if (labelSetRes.resource != null)
                {
                    labelSetRes.Dispose();
                }
                if (applyChanges)
                {
                    AssetDatabase.StopAssetEditing();
                }
                EditorUtility.ClearProgressBar();

                if (applyChanges)
                {
                    if (changedCount != 0)
                    {
                        this.Log($"Updated {changedCount} textures.");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        this.Log($"No textures updated.");
                    }
                }
            }
            // Store counts for UI preview
            _potentialGrowCount = growCount;
            _potentialShrinkCount = shrinkCount;
            _potentialUnchangedCount = unchangedCount;
            return changedCount;
        }

        private void ApplyPlatformOverride(TextureImporter importer, string platform, int target)
        {
            bool enabled =
                platform == "Standalone" ? _applyToStandalone
                : platform == "Android" ? _applyToAndroid
                : platform == "iPhone" ? _applyToiOS
                : false;
            if (!enabled)
            {
                return;
            }

            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(
                platform
            );
            settings.overridden = true;
            settings.maxTextureSize = Mathf.Clamp(
                target,
                _minAllowedTextureSize,
                _maxAllowedTextureSize
            );
            importer.SetPlatformTextureSettings(settings);
        }
    }
#endif
}

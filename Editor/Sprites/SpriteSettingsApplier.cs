// ReSharper disable CompareOfFloatsByEqualityOperator
namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using CustomEditors;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Utils;
    using System.Text.RegularExpressions;
    using Object = UnityEngine.Object;

    [Serializable]
    public sealed class SpriteSettings
    {
        public enum MatchMode
        {
            [Obsolete("Default is invalid. Choose a specific match mode.", false)]
            None = 0,
            Any = 1,
            NameContains = 2,
            PathContains = 3,
            Regex = 4,
            Extension = 5,
        }

        public MatchMode matchBy = MatchMode.Any;
        public string matchPattern = string.Empty;
        public int priority;

        public bool applyPixelsPerUnit;

        [WShowIf(nameof(applyPixelsPerUnit))]
        public int pixelsPerUnit = 100;

        public bool applyPivot;

        [WShowIf(nameof(applyPivot))]
        public Vector2 pivot = new(0.5f, 0.5f);

        public bool applySpriteMode;

        [WShowIf(nameof(applySpriteMode))]
        public SpriteImportMode spriteMode = SpriteImportMode.Single;

        public bool applyGenerateMipMaps;

        [WShowIf(nameof(applyGenerateMipMaps))]
        public bool generateMipMaps;

        public bool applyAlphaIsTransparency;

        [WShowIf(nameof(applyAlphaIsTransparency))]
        public bool alphaIsTransparency = true;

        public bool applyReadWriteEnabled;

        [WShowIf(nameof(applyReadWriteEnabled))]
        public bool readWriteEnabled = true;

        public bool applyExtrudeEdges;

        [WShowIf(nameof(applyExtrudeEdges))]
        [Range(0, 32)]
        public uint extrudeEdges = 1;

        public bool applyWrapMode;

        [WShowIf(nameof(applyWrapMode))]
        public TextureWrapMode wrapMode = TextureWrapMode.Clamp;

        public bool applyFilterMode;

        [WShowIf(nameof(applyFilterMode))]
        public FilterMode filterMode = FilterMode.Point;

        public bool applyCrunchCompression;

        [WShowIf(nameof(applyCrunchCompression))]
        public bool useCrunchCompression;

        public bool applyCompression;

        [WShowIf(nameof(applyCompression))]
        [SerializeField]
        public TextureImporterCompression compressionLevel = TextureImporterCompression.Compressed;

        public string name = string.Empty;

        public bool applyTextureType;

        [WShowIf(nameof(applyTextureType))]
        public TextureImporterType textureType = TextureImporterType.Sprite;
    }

    [CustomPropertyDrawer(typeof(SpriteSettings))]
    public sealed class SpriteSettingsDrawer : PropertyDrawer
    {
        private const float CheckboxWidth = 18f;
        private const float HorizontalSpacing = 5f;

        private readonly (string apply, string val, string label)[] _settingPairs =
        {
            (
                nameof(SpriteSettings.applyPixelsPerUnit),
                nameof(SpriteSettings.pixelsPerUnit),
                "Pixels Per Unit"
            ),
            (nameof(SpriteSettings.applyPivot), nameof(SpriteSettings.pivot), "Pivot"),
            (
                nameof(SpriteSettings.applySpriteMode),
                nameof(SpriteSettings.spriteMode),
                "Sprite Mode"
            ),
            (
                nameof(SpriteSettings.applyGenerateMipMaps),
                nameof(SpriteSettings.generateMipMaps),
                "Generate Mip Maps"
            ),
            (
                nameof(SpriteSettings.applyAlphaIsTransparency),
                nameof(SpriteSettings.alphaIsTransparency),
                "Alpha Is Transparency"
            ),
            (
                nameof(SpriteSettings.applyReadWriteEnabled),
                nameof(SpriteSettings.readWriteEnabled),
                "Read/Write Enabled"
            ),
            (
                nameof(SpriteSettings.applyExtrudeEdges),
                nameof(SpriteSettings.extrudeEdges),
                "Extrude Edges"
            ),
            (nameof(SpriteSettings.applyWrapMode), nameof(SpriteSettings.wrapMode), "Wrap Mode"),
            (
                nameof(SpriteSettings.applyFilterMode),
                nameof(SpriteSettings.filterMode),
                "Filter Mode"
            ),
            (
                nameof(SpriteSettings.applyCrunchCompression),
                nameof(SpriteSettings.useCrunchCompression),
                "Use Crunch Compression"
            ),
            (
                nameof(SpriteSettings.applyCompression),
                nameof(SpriteSettings.compressionLevel),
                "Compression Level"
            ),
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty nameProp = property.FindPropertyRelative(
                nameof(SpriteSettings.name)
            );

            Rect currentRect = new(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight
            );
            EditorGUI.PropertyField(
                currentRect,
                nameProp,
                new GUIContent("Profile Name (Optional)")
            );

            currentRect.y +=
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Matching UI
            SerializedProperty matchByProp = property.FindPropertyRelative(
                nameof(SpriteSettings.matchBy)
            );
            SerializedProperty matchPatternProp = property.FindPropertyRelative(
                nameof(SpriteSettings.matchPattern)
            );
            SerializedProperty priorityProp = property.FindPropertyRelative(
                nameof(SpriteSettings.priority)
            );

            EditorGUI.LabelField(currentRect, "Matching", EditorStyles.boldLabel);
            currentRect.y +=
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            Rect rowRect = new(
                position.x,
                currentRect.y,
                position.width,
                EditorGUIUtility.singleLineHeight
            );
            Rect matchByRect = new(rowRect.x, rowRect.y, rowRect.width * 0.35f, rowRect.height);
            // Draw a compact label+field for Match By to prevent label width from eating field space
            const float matchByLabelWidth = 70f;
            Rect matchByLabelRect = new(
                matchByRect.x,
                matchByRect.y,
                matchByLabelWidth,
                matchByRect.height
            );
            Rect matchByFieldRect = new(
                matchByLabelRect.x + matchByLabelRect.width + HorizontalSpacing,
                matchByRect.y,
                Mathf.Max(0f, matchByRect.width - matchByLabelWidth - HorizontalSpacing),
                matchByRect.height
            );
            EditorGUI.LabelField(matchByLabelRect, "Match By");
            EditorGUI.PropertyField(matchByFieldRect, matchByProp, GUIContent.none);
            SpriteSettings.MatchMode mode = (SpriteSettings.MatchMode)matchByProp.enumValueIndex;
#pragma warning disable CS0618 // Type or member is obsolete
            if (mode != SpriteSettings.MatchMode.Any && mode != SpriteSettings.MatchMode.None)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Rect patternRect = new(
                    matchByRect.x + matchByRect.width + HorizontalSpacing,
                    rowRect.y,
                    rowRect.width - matchByRect.width - HorizontalSpacing - 80f,
                    rowRect.height
                );
                EditorGUI.PropertyField(patternRect, matchPatternProp, new GUIContent("Pattern"));
            }
            Rect priorityRect = new(
                rowRect.x + rowRect.width - 80f,
                rowRect.y,
                80f,
                rowRect.height
            );
            // Priority area is tight: draw label+field manually to avoid label consuming all width
            const float priorityLabelWidth = 50f;
            Rect priorityLabelRect = new(
                priorityRect.x,
                priorityRect.y,
                priorityLabelWidth,
                priorityRect.height
            );
            Rect priorityFieldRect = new(
                priorityLabelRect.x + priorityLabelRect.width + HorizontalSpacing,
                priorityRect.y,
                Mathf.Max(0f, priorityRect.width - priorityLabelWidth - HorizontalSpacing),
                priorityRect.height
            );
            EditorGUI.LabelField(priorityLabelRect, "Priority");
            EditorGUI.PropertyField(priorityFieldRect, priorityProp, GUIContent.none);
            currentRect.y +=
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            foreach ((string apply, string val, string label) pair in _settingPairs)
            {
                SerializedProperty applyProp = property.FindPropertyRelative(pair.apply);
                SerializedProperty valueProp = property.FindPropertyRelative(pair.val);

                if (applyProp == null || valueProp == null)
                {
                    EditorGUI.LabelField(currentRect, $"Error finding properties for {pair.label}");
                    currentRect.y +=
                        EditorGUIUtility.singleLineHeight
                        + EditorGUIUtility.standardVerticalSpacing;
                    continue;
                }

                float labelLineHeight = EditorGUIUtility.singleLineHeight;
                Rect labelLineRect = new(
                    position.x,
                    currentRect.y,
                    position.width,
                    labelLineHeight
                );
                Rect checkboxRect = new(
                    labelLineRect.x + labelLineRect.width - CheckboxWidth,
                    labelLineRect.y,
                    CheckboxWidth,
                    labelLineHeight
                );
                Rect labelRect = new(
                    labelLineRect.x,
                    labelLineRect.y,
                    labelLineRect.width - CheckboxWidth - HorizontalSpacing,
                    labelLineHeight
                );

                EditorGUI.LabelField(labelRect, pair.label);
                EditorGUI.PropertyField(checkboxRect, applyProp, GUIContent.none);
                currentRect.y += labelLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if (applyProp.boolValue)
                {
                    float valuePropHeight = EditorGUI.GetPropertyHeight(valueProp, true);
                    Rect valueRect = new(
                        position.x,
                        currentRect.y,
                        position.width,
                        valuePropHeight
                    );

                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none, true);
                    }

                    currentRect.y += valueRect.height + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            // Enforce Texture Type UI
            SerializedProperty applyTextureTypeProp = property.FindPropertyRelative(
                nameof(SpriteSettings.applyTextureType)
            );
            SerializedProperty textureTypeProp = property.FindPropertyRelative(
                nameof(SpriteSettings.textureType)
            );

            float labelLineHeight2 = EditorGUIUtility.singleLineHeight;
            Rect labelLineRect2 = new(position.x, currentRect.y, position.width, labelLineHeight2);
            Rect checkboxRect2 = new(
                labelLineRect2.x + labelLineRect2.width - CheckboxWidth,
                labelLineRect2.y,
                CheckboxWidth,
                labelLineHeight2
            );
            Rect labelRect2 = new(
                labelLineRect2.x,
                labelLineRect2.y,
                labelLineRect2.width - CheckboxWidth - HorizontalSpacing,
                labelLineHeight2
            );
            EditorGUI.LabelField(labelRect2, "Enforce Texture Type");
            EditorGUI.PropertyField(checkboxRect2, applyTextureTypeProp, GUIContent.none);
            currentRect.y += labelLineHeight2 + EditorGUIUtility.standardVerticalSpacing;
            if (applyTextureTypeProp.boolValue)
            {
                float valuePropHeight = EditorGUI.GetPropertyHeight(textureTypeProp, true);
                Rect valueRect = new(position.x, currentRect.y, position.width, valuePropHeight);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUI.PropertyField(valueRect, textureTypeProp, GUIContent.none, true);
                }
                currentRect.y += valueRect.height + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = 0f;

            SerializedProperty nameProp = property.FindPropertyRelative(
                nameof(SpriteSettings.name)
            );
            if (nameProp != null)
            {
                totalHeight +=
                    EditorGUI.GetPropertyHeight(nameProp)
                    + EditorGUIUtility.standardVerticalSpacing;
            }

            // Matching header + row (matchBy, pattern, priority)
            totalHeight +=
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // header
            totalHeight +=
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // row

            foreach ((string apply, string val, string label) pair in _settingPairs)
            {
                SerializedProperty applyProp = property.FindPropertyRelative(pair.apply);
                SerializedProperty valueProp = property.FindPropertyRelative(pair.val);

                if (applyProp == null || valueProp == null)
                {
                    continue;
                }

                totalHeight +=
                    EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if (applyProp.boolValue)
                {
                    totalHeight +=
                        EditorGUI.GetPropertyHeight(valueProp, true)
                        + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            // Texture type enforcement height
            SerializedProperty applyTextureTypeProp = property.FindPropertyRelative(
                nameof(SpriteSettings.applyTextureType)
            );
            SerializedProperty textureTypeProp = property.FindPropertyRelative(
                nameof(SpriteSettings.textureType)
            );
            totalHeight +=
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (applyTextureTypeProp != null && applyTextureTypeProp.boolValue)
            {
                totalHeight +=
                    EditorGUI.GetPropertyHeight(textureTypeProp, true)
                    + EditorGUIUtility.standardVerticalSpacing;
            }

            if (totalHeight > 0)
            {
                totalHeight -= EditorGUIUtility.standardVerticalSpacing;
            }

            return totalHeight;
        }
    }

    /// <summary>
    /// Batch-applies configurable sprite importer settings to selected sprites and/or recursively
    /// through selected directories using prioritized profiles with multiple match modes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Problems this solves: keeping large sets of sprites consistent (PPU, pivot, mode, filter,
    /// wrap, compression, etc.) without manual per-asset editing.
    /// </para>
    /// <para>
    /// How it works: define one or more <c>SpriteSettings</c> profiles with match mode
    /// (Any/NameContains/PathContains/Regex/Extension) and an optional priority. Calculate stats to
    /// preview which assets will be affected, then apply settings in one pass.
    /// </para>
    /// <para>
    /// Usage: add sprites and/or directories; configure profiles; click "Calculate Stats" to see
    /// impact and preview up to 200 paths; then "Apply Settings" to write importer changes.
    /// </para>
    /// <para>
    /// Caveats: importer changes trigger reimports; ensure regex patterns are correct; for very
    /// large trees prefer running in batches.
    /// </para>
    /// </remarks>
    public sealed class SpriteSettingsApplierWindow : EditorWindow
    {
        public List<Sprite> sprites = new();
        public List<string> spriteFileExtensions = new() { ".png" };
        public List<SpriteSettings> spriteSettings = new() { new SpriteSettings() };
        public List<Object> directories = new();

        private SerializedObject _serializedObject;
        private SerializedProperty _spritesProp;
        private SerializedProperty _spriteFileExtensionsProp;
        private SerializedProperty _spriteSettingsProp;
        private SerializedProperty _directoriesProp;

        private Vector2 _scrollPosition;
        private int _totalSpritesToProcess = -1;
        private int _spritesThatWillChange = -1;
        private bool _showPreviewOfChanges;
        private readonly List<string> _assetsThatWillChange = new();
        private bool _applyCanceled;
        private readonly TextureImporterSettings _settingsBuffer = new();

        private sealed class PreparedProfile
        {
            public SpriteSettings settings;
            public SpriteSettings.MatchMode mode;
            public string nameLower;
            public string patternLower;
            public string extWithDot;
            public Regex regex;
            public int priority;
        }

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Sprite Settings Applier", priority = -2)]
        public static void ShowWindow()
        {
            SpriteSettingsApplierWindow window = GetWindow<SpriteSettingsApplierWindow>(
                "Sprite Settings Applier"
            );
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _spritesProp = _serializedObject.FindProperty(nameof(sprites));
            _spriteFileExtensionsProp = _serializedObject.FindProperty(
                nameof(spriteFileExtensions)
            );
            _spriteSettingsProp = _serializedObject.FindProperty(nameof(spriteSettings));
            _directoriesProp = _serializedObject.FindProperty(nameof(directories));
        }

        private void OnGUI()
        {
            _serializedObject.Update();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Sprite Sources", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_spritesProp, new GUIContent("Specific Sprites"), true);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Directory Sources", EditorStyles.boldLabel);
            PersistentDirectoryGUI.PathSelectorObjectArray(
                _directoriesProp,
                nameof(SpriteSettingsApplierWindow)
            );
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _spriteFileExtensionsProp,
                new GUIContent("Sprite File Extensions"),
                true
            );
            EditorGUILayout.PropertyField(
                _spriteSettingsProp,
                new GUIContent("Sprite Settings Profiles"),
                true
            );
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Calculate Stats"))
            {
                CalculateStats();
            }

            if (_totalSpritesToProcess >= 0 && _spritesThatWillChange >= 0)
            {
                EditorGUILayout.LabelField($"Sprites to process: {_totalSpritesToProcess}");
                EditorGUILayout.LabelField($"Sprites that will change: {_spritesThatWillChange}");
                _showPreviewOfChanges = EditorGUILayout.Foldout(
                    _showPreviewOfChanges,
                    $"Preview ({_assetsThatWillChange.Count})"
                );
                if (_showPreviewOfChanges)
                {
                    int toShow = Mathf.Min(_assetsThatWillChange.Count, 200);
                    for (int i = 0; i < toShow; i++)
                    {
                        EditorGUILayout.LabelField(_assetsThatWillChange[i]);
                    }
                    if (_assetsThatWillChange.Count > 200)
                    {
                        EditorGUILayout.LabelField(
                            $"...and {_assetsThatWillChange.Count - 200} more"
                        );
                    }
                    if (GUILayout.Button("Copy List"))
                    {
                        EditorGUIUtility.systemCopyBuffer = string.Join(
                            "\n",
                            _assetsThatWillChange
                        );
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("Press 'Calculate Stats' to see processing details.");
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Settings to Sprites"))
            {
                ApplySettings();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Profiles", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Profiles Asset"))
            {
                SaveProfilesAsset();
            }
            if (GUILayout.Button("Load Profiles Asset"))
            {
                LoadProfilesAsset();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            _serializedObject.ApplyModifiedProperties();
        }

        private List<(string fullFilePath, string relativePath)> GetTargetSpritePaths()
        {
            List<(string fullFilePath, string relativePath)> filePaths = new();
            HashSet<string> uniqueRelativePaths = new(StringComparer.OrdinalIgnoreCase);

            // Collect folder asset paths from user selection
            List<string> folderAssetPaths = new();
            for (int i = 0; i < _directoriesProp.arraySize; i++)
            {
                Object dir = _directoriesProp.GetArrayElementAtIndex(i).objectReferenceValue;
                if (dir == null)
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(dir);
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }

                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    folderAssetPaths.Add(assetPath);
                }
                else
                {
                    this.LogWarn($"Item '{assetPath}' is not a valid directory. Skipping.");
                }
            }

            // Build allowed extension set
            HashSet<string> allowedExtensions = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _spriteFileExtensionsProp.arraySize; i++)
            {
                string ext = _spriteFileExtensionsProp.GetArrayElementAtIndex(i).stringValue;
                if (string.IsNullOrWhiteSpace(ext))
                {
                    continue;
                }

                if (!ext.StartsWith("."))
                {
                    ext = "." + ext;
                }

                allowedExtensions.Add(ext);
            }

            // Search in folders via AssetDatabase
            if (folderAssetPaths.Count > 0)
            {
                using PooledResource<string[]> bufferResource = WallstopFastArrayPool<string>.Get(
                    folderAssetPaths.Count,
                    out string[] folders
                );

                for (int i = 0; i < folderAssetPaths.Count; i++)
                {
                    folders[i] = folderAssetPaths[i];
                }

                string[] guids = AssetDatabase.FindAssets("t:Texture2D", folders);
                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.IsNullOrWhiteSpace(assetPath))
                    {
                        continue;
                    }

                    string ext = Path.GetExtension(assetPath);
                    if (allowedExtensions.Count > 0 && !allowedExtensions.Contains(ext))
                    {
                        continue;
                    }

                    if (uniqueRelativePaths.Add(assetPath))
                    {
                        filePaths.Add((string.Empty, assetPath));
                    }
                }
            }

            // Add explicitly selected sprites
            for (int i = 0; i < _spritesProp.arraySize; i++)
            {
                Sprite sprite =
                    _spritesProp.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                if (sprite == null)
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(sprite);
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }

                string ext = Path.GetExtension(assetPath);
                if (allowedExtensions.Count > 0 && !allowedExtensions.Contains(ext))
                {
                    continue;
                }

                if (uniqueRelativePaths.Add(assetPath))
                {
                    filePaths.Add((string.Empty, assetPath));
                }
            }

            return filePaths;
        }

        private void CalculateStats()
        {
            _totalSpritesToProcess = 0;
            _spritesThatWillChange = 0;
            List<(string fullFilePath, string relativePath)> targetFiles = GetTargetSpritePaths();

            _totalSpritesToProcess = targetFiles.Count;

            List<SpriteSettings> currentSettings;
            if (_serializedObject.targetObject is SpriteSettingsApplierWindow windowInstance)
            {
                currentSettings = windowInstance.spriteSettings;
            }
            else
            {
                this.LogError(
                    $"Cannot access spriteSettings list from target object. Aborting stats."
                );
                return;
            }

            _assetsThatWillChange.Clear();
            if (_assetsThatWillChange.Capacity < targetFiles.Count)
            {
                _assetsThatWillChange.Capacity = targetFiles.Count;
            }

            // Prepare matchers once
            List<PreparedProfile> prepared = PrepareProfiles(currentSettings);

            double lastUpdateTime = EditorApplication.timeSinceStartup;
            for (int i = 0; i < targetFiles.Count; i++)
            {
                (string _, string relativePath) = targetFiles[i];
                // Throttle progress bar updates to reduce overhead
                double now = EditorApplication.timeSinceStartup;
                if (
                    i == 0
                    || i == targetFiles.Count - 1
                    || i % 50 == 0
                    || now - lastUpdateTime > 0.2
                )
                {
                    WallstopStudios.UnityHelpers.Editor.Utils.EditorUi.ShowProgress(
                        "Calculating Stats",
                        $"Checking '{Path.GetFileName(relativePath)}' ({i + 1}/{_totalSpritesToProcess})",
                        (float)(i + 1) / _totalSpritesToProcess
                    );
                    lastUpdateTime = now;
                }

                if (WillTextureSettingsChangePrepared(relativePath, prepared))
                {
                    _spritesThatWillChange++;
                    _assetsThatWillChange.Add(relativePath);
                }
            }

            WallstopStudios.UnityHelpers.Editor.Utils.EditorUi.ClearProgress();
            this.Log(
                $"Calculation complete. Sprites to process: {_totalSpritesToProcess}, Sprites that will change: {_spritesThatWillChange}"
            );
        }

        private void ApplySettings()
        {
            List<(string fullFilePath, string relativePath)> targetFiles = GetTargetSpritePaths();
            int spriteCount = 0;
            List<TextureImporter> updatedImporters = new(targetFiles.Count);
            _applyCanceled = false;

            List<SpriteSettings> currentSettings;
            if (_serializedObject.targetObject is SpriteSettingsApplierWindow windowInstance)
            {
                currentSettings = windowInstance.spriteSettings;
            }
            else
            {
                this.LogError(
                    $"Cannot access spriteSettings list from target object. Aborting apply."
                );
                return;
            }

            if (targetFiles.Count == 0)
            {
                this.LogWarn($"No sprites found to process based on current configuration.");
                return;
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                // Prepare profile matchers once
                List<PreparedProfile> prepared = PrepareProfiles(currentSettings);
                double lastUpdateTime = EditorApplication.timeSinceStartup;

                for (int i = 0; i < targetFiles.Count; i++)
                {
                    string filePath = targetFiles[i].relativePath;
                    double now = EditorApplication.timeSinceStartup;
                    bool shouldUpdate =
                        i == 0
                        || i == targetFiles.Count - 1
                        || i % 50 == 0
                        || now - lastUpdateTime > 0.2;
                    if (
                        shouldUpdate
                        && WallstopStudios.UnityHelpers.Editor.Utils.EditorUi.CancelableProgress(
                            "Applying Sprite Settings",
                            $"Processing '{Path.GetFileName(filePath)}' ({i + 1}/{targetFiles.Count})",
                            (float)(i + 1) / targetFiles.Count
                        )
                    )
                    {
                        _applyCanceled = true;
                        break;
                    }
                    if (shouldUpdate)
                    {
                        lastUpdateTime = now;
                    }

                    if (
                        TryUpdateTextureSettingsPrepared(
                            filePath,
                            prepared,
                            out TextureImporter textureImporter
                        )
                    )
                    {
                        if (textureImporter != null)
                        {
                            updatedImporters.Add(textureImporter);
                            ++spriteCount;
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                WallstopStudios.UnityHelpers.Editor.Utils.EditorUi.ClearProgress();
                foreach (TextureImporter importer in updatedImporters)
                {
                    importer.SaveAndReimport();
                }

                if (_applyCanceled)
                {
                    this.Log($"Canceled. Processed {spriteCount} sprites before cancel.");
                }
                else
                {
                    this.Log($"Processed {spriteCount} sprites.");
                }
                if (0 < spriteCount)
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    this.Log($"Asset database saved and refreshed.");
                }
                else
                {
                    this.Log($"No sprites required changes.");
                }

                _totalSpritesToProcess = -1;
                _spritesThatWillChange = -1;
            }
        }

        private static List<PreparedProfile> PrepareProfiles(List<SpriteSettings> profiles)
        {
            List<PreparedProfile> result = new(profiles?.Count ?? 0);
            if (profiles == null)
            {
                return result;
            }

            for (int i = 0; i < profiles.Count; i++)
            {
                SpriteSettings s = profiles[i];
                if (s == null)
                {
                    continue;
                }

                PreparedProfile p = new()
                {
                    settings = s,
                    mode = s.matchBy,
                    nameLower = string.IsNullOrEmpty(s.name) ? null : s.name.ToLowerInvariant(),
                    patternLower = string.IsNullOrEmpty(s.matchPattern)
                        ? null
                        : s.matchPattern.ToLowerInvariant(),
                    extWithDot =
                        string.IsNullOrEmpty(s.matchPattern) ? null
                        : s.matchPattern.StartsWith(".") ? s.matchPattern
                        : "." + s.matchPattern,
                    priority = s.priority,
                };
                if (
                    s.matchBy == SpriteSettings.MatchMode.Regex
                    && !string.IsNullOrEmpty(s.matchPattern)
                )
                {
                    try
                    {
                        p.regex = new Regex(
                            s.matchPattern,
                            RegexOptions.IgnoreCase | RegexOptions.Compiled
                        );
                    }
                    catch
                    {
                        p.regex = null;
                    }
                }
                result.Add(p);
            }
            return result;
        }

        private static SpriteSettings FindMatchingSettingsPrepared(
            string filePath,
            List<PreparedProfile> prepared
        )
        {
            if (prepared == null || prepared.Count == 0)
            {
                return null;
            }

            string fileName = Path.GetFileName(filePath);
            string fileNameLower = fileName.ToLowerInvariant();
            string pathLower = filePath.ToLowerInvariant();
            string ext = Path.GetExtension(filePath);

            SpriteSettings best = null;
            int bestPriority = int.MinValue;
            for (int i = 0; i < prepared.Count; i++)
            {
                PreparedProfile p = prepared[i];
                bool matches = false;
                switch (p.mode)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    case SpriteSettings.MatchMode.None:
#pragma warning restore CS0618 // Type or member is obsolete
                        break;
                    case SpriteSettings.MatchMode.Any:
                        if (!string.IsNullOrEmpty(p.nameLower))
                        {
                            matches = fileNameLower.Contains(p.nameLower);
                        }
                        else
                        {
                            matches = true;
                        }
                        break;
                    case SpriteSettings.MatchMode.NameContains:
                        if (!string.IsNullOrEmpty(p.patternLower))
                        {
                            matches = fileNameLower.Contains(p.patternLower);
                        }
                        break;
                    case SpriteSettings.MatchMode.PathContains:
                        if (!string.IsNullOrEmpty(p.patternLower))
                        {
                            matches = pathLower.Contains(p.patternLower);
                        }
                        break;
                    case SpriteSettings.MatchMode.Extension:
                        if (!string.IsNullOrEmpty(p.extWithDot))
                        {
                            matches = string.Equals(
                                ext,
                                p.extWithDot,
                                StringComparison.OrdinalIgnoreCase
                            );
                        }
                        break;
                    case SpriteSettings.MatchMode.Regex:
                        if (p.regex != null)
                        {
                            matches = p.regex.IsMatch(filePath);
                        }
                        break;
                }
                if (!matches)
                {
                    continue;
                }

                if (best == null || p.priority > bestPriority)
                {
                    best = p.settings;
                    bestPriority = p.priority;
                }
            }
            return best;
        }

        private bool WillTextureSettingsChangePrepared(
            string filePath,
            List<PreparedProfile> prepared
        )
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            TextureImporter textureImporter = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (textureImporter == null)
            {
                this.LogWarn($"Could not get TextureImporter for asset: {filePath}");
                return false;
            }
            SpriteSettings spriteData = FindMatchingSettingsPrepared(filePath, prepared);
            if (spriteData == null)
            {
                this.LogWarn($"No matching SpriteSettings profile found for: {filePath}");
                return false;
            }

            bool changed = false;
            if (spriteData.applyPixelsPerUnit)
            {
                changed |= textureImporter.spritePixelsPerUnit != spriteData.pixelsPerUnit;
            }
            if (spriteData.applyPivot)
            {
                changed |= textureImporter.spritePivot != spriteData.pivot;
            }
            if (spriteData.applyGenerateMipMaps)
            {
                changed |= textureImporter.mipmapEnabled != spriteData.generateMipMaps;
            }
            if (spriteData.applyCrunchCompression)
            {
                changed |= textureImporter.crunchedCompression != spriteData.useCrunchCompression;
            }
            if (spriteData.applyCompression)
            {
                changed |= textureImporter.textureCompression != spriteData.compressionLevel;
            }

            textureImporter.ReadTextureSettings(_settingsBuffer);
            if (spriteData.applyTextureType)
            {
                changed |= textureImporter.textureType != spriteData.textureType;
            }
            if (spriteData.applyPivot)
            {
                changed |= _settingsBuffer.spriteAlignment != (int)SpriteAlignment.Custom;
            }
            if (spriteData.applyAlphaIsTransparency)
            {
                changed |= _settingsBuffer.alphaIsTransparency != spriteData.alphaIsTransparency;
            }
            if (spriteData.applyReadWriteEnabled)
            {
                changed |= _settingsBuffer.readable != spriteData.readWriteEnabled;
            }
            if (spriteData.applySpriteMode)
            {
                changed |= _settingsBuffer.spriteMode != (int)spriteData.spriteMode;
            }
            if (spriteData.applyExtrudeEdges)
            {
                changed |= _settingsBuffer.spriteExtrude != spriteData.extrudeEdges;
            }
            if (spriteData.applyWrapMode)
            {
                changed |= _settingsBuffer.wrapMode != spriteData.wrapMode;
            }
            if (spriteData.applyFilterMode)
            {
                changed |= _settingsBuffer.filterMode != spriteData.filterMode;
            }
            return changed;
        }

        private bool TryUpdateTextureSettingsPrepared(
            string filePath,
            List<PreparedProfile> prepared,
            out TextureImporter textureImporter
        )
        {
            textureImporter = default;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            textureImporter = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (textureImporter == null)
            {
                this.LogWarn($"Could not get TextureImporter for asset: {filePath}");
                return false;
            }
            SpriteSettings spriteData = FindMatchingSettingsPrepared(filePath, prepared);
            if (spriteData == null)
            {
                this.LogWarn($"No matching SpriteSettings profile found for: {filePath}");
                return false;
            }

            bool changed = false;
            bool settingsChanged = false;
            textureImporter.ReadTextureSettings(_settingsBuffer);

            if (spriteData.applyTextureType)
            {
                if (textureImporter.textureType != spriteData.textureType)
                {
                    textureImporter.textureType = spriteData.textureType;
                    changed = true;
                }
            }
            else if (
                (
                    spriteData.applyPivot
                    || spriteData.applySpriteMode
                    || spriteData.applyPixelsPerUnit
                )
                && textureImporter.textureType != TextureImporterType.Sprite
            )
            {
                this.LogWarn(
                    $"Applying sprite settings on a non-Sprite texture type for: {filePath}"
                );
            }

            if (spriteData.applySpriteMode)
            {
                if (textureImporter.spriteImportMode != spriteData.spriteMode)
                {
                    textureImporter.spriteImportMode = spriteData.spriteMode;
                    changed = true;
                }
                if (_settingsBuffer.spriteMode != (int)spriteData.spriteMode)
                {
                    _settingsBuffer.spriteMode = (int)spriteData.spriteMode;
                    settingsChanged = true;
                }
            }
            if (spriteData.applyPixelsPerUnit)
            {
                if (textureImporter.spritePixelsPerUnit != spriteData.pixelsPerUnit)
                {
                    textureImporter.spritePixelsPerUnit = spriteData.pixelsPerUnit;
                    changed = true;
                }
                if (_settingsBuffer.spritePixelsPerUnit != spriteData.pixelsPerUnit)
                {
                    _settingsBuffer.spritePixelsPerUnit = spriteData.pixelsPerUnit;
                    settingsChanged = true;
                }
            }
            if (spriteData.applyPivot)
            {
                if (textureImporter.spritePivot != spriteData.pivot)
                {
                    textureImporter.spritePivot = spriteData.pivot;
                    changed = true;
                }
                if (_settingsBuffer.spriteAlignment != (int)SpriteAlignment.Custom)
                {
                    _settingsBuffer.spriteAlignment = (int)SpriteAlignment.Custom;
                    settingsChanged = true;
                }
                if (_settingsBuffer.spritePivot != spriteData.pivot)
                {
                    _settingsBuffer.spritePivot = spriteData.pivot;
                    settingsChanged = true;
                }
            }
            if (spriteData.applyGenerateMipMaps)
            {
                if (textureImporter.mipmapEnabled != spriteData.generateMipMaps)
                {
                    textureImporter.mipmapEnabled = spriteData.generateMipMaps;
                    changed = true;
                }
                if (_settingsBuffer.mipmapEnabled != spriteData.generateMipMaps)
                {
                    _settingsBuffer.mipmapEnabled = spriteData.generateMipMaps;
                    settingsChanged = true;
                }
            }
            if (spriteData.applyCrunchCompression)
            {
                if (textureImporter.crunchedCompression != spriteData.useCrunchCompression)
                {
                    textureImporter.crunchedCompression = spriteData.useCrunchCompression;
                    changed = true;
                }
            }
            if (spriteData.applyCompression)
            {
                if (textureImporter.textureCompression != spriteData.compressionLevel)
                {
                    textureImporter.textureCompression = spriteData.compressionLevel;
                    changed = true;
                }
            }
            if (spriteData.applyAlphaIsTransparency)
            {
                if (textureImporter.alphaIsTransparency != spriteData.alphaIsTransparency)
                {
                    textureImporter.alphaIsTransparency = spriteData.alphaIsTransparency;
                    changed = true;
                }
                if (_settingsBuffer.alphaIsTransparency != spriteData.alphaIsTransparency)
                {
                    _settingsBuffer.alphaIsTransparency = spriteData.alphaIsTransparency;
                    settingsChanged = true;
                }
            }
            if (spriteData.applyReadWriteEnabled)
            {
                if (textureImporter.isReadable != spriteData.readWriteEnabled)
                {
                    textureImporter.isReadable = spriteData.readWriteEnabled;
                    changed = true;
                }
                if (_settingsBuffer.readable != spriteData.readWriteEnabled)
                {
                    _settingsBuffer.readable = spriteData.readWriteEnabled;
                    settingsChanged = true;
                }
            }
            if (spriteData.applyExtrudeEdges)
            {
                if (_settingsBuffer.spriteExtrude != spriteData.extrudeEdges)
                {
                    _settingsBuffer.spriteExtrude = spriteData.extrudeEdges;
                    settingsChanged = true;
                }
            }
            if (spriteData.applyWrapMode)
            {
                if (textureImporter.wrapMode != spriteData.wrapMode)
                {
                    textureImporter.wrapMode = spriteData.wrapMode;
                    changed = true;
                }
                if (_settingsBuffer.wrapMode != spriteData.wrapMode)
                {
                    _settingsBuffer.wrapMode = spriteData.wrapMode;
                    settingsChanged = true;
                }
            }
            if (spriteData.applyFilterMode)
            {
                if (textureImporter.filterMode != spriteData.filterMode)
                {
                    textureImporter.filterMode = spriteData.filterMode;
                    changed = true;
                }
                if (_settingsBuffer.filterMode != spriteData.filterMode)
                {
                    _settingsBuffer.filterMode = spriteData.filterMode;
                    settingsChanged = true;
                }
            }

            if (settingsChanged)
            {
                textureImporter.SetTextureSettings(_settingsBuffer);
            }

            return changed || settingsChanged;
        }

        // Profiles persistence helpers
        private void SaveProfilesAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Sprite Settings Profiles",
                "SpriteSettingsProfiles",
                "asset",
                "Choose location to save the profiles asset"
            );
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            SpriteSettingsProfileCollection asset =
                CreateInstance<SpriteSettingsProfileCollection>();
            asset.profiles = new List<SpriteSettings>(spriteSettings.Count);
            for (int i = 0; i < spriteSettings.Count; i++)
            {
                string json = JsonUtility.ToJson(spriteSettings[i]);
                asset.profiles.Add(JsonUtility.FromJson<SpriteSettings>(json));
            }
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            this.Log($"Saved profiles to {path}");
        }

        private void LoadProfilesAsset()
        {
            string path = EditorUtility.OpenFilePanel(
                "Load Sprite Settings Profiles",
                "Assets",
                "asset"
            );
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string projectRelative = path;
            if (path.StartsWith(Application.dataPath))
            {
                projectRelative = "Assets" + path.Substring(Application.dataPath.Length);
            }
            else if (path.Contains("/Assets/"))
            {
                int idx = path.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
                projectRelative = path.Substring(idx + 1);
            }

            SpriteSettingsProfileCollection asset =
                AssetDatabase.LoadAssetAtPath<SpriteSettingsProfileCollection>(projectRelative);
            if (asset == null)
            {
                this.LogWarn($"Could not load profiles asset at: {projectRelative}");
                return;
            }

            spriteSettings = new List<SpriteSettings>(asset.profiles.Count);
            for (int i = 0; i < asset.profiles.Count; i++)
            {
                string json = JsonUtility.ToJson(asset.profiles[i]);
                spriteSettings.Add(JsonUtility.FromJson<SpriteSettings>(json));
            }
            this.Log($"Loaded {spriteSettings.Count} profiles from {projectRelative}");
        }
    }
#endif
}

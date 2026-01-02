// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using CustomEditors;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Utils;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Extracts individual sprites from sprite sheet textures (textures with SpriteImportMode.Multiple)
    /// and saves them as separate PNG files. Provides preview GUI with reordering, bulk operations,
    /// and optional reference replacement.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Problems this solves: splitting sprite sheets into individual assets for easier management,
    /// creating separate sprites for animation systems that expect individual files, preparing
    /// assets for different build targets.
    /// </para>
    /// <para>
    /// How it works: scans input directories for textures with SpriteImportMode.Multiple, reads
    /// sprite metadata (rects, names, pivots, borders), extracts pixel data for each sprite,
    /// and writes individual PNG files. Optionally updates references in prefabs and scenes.
    /// </para>
    /// <para>
    /// Usage:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Open via menu: Tools/Wallstop Studios/Unity Helpers/Sprite Sheet Extractor.</description></item>
    /// <item><description>Select input folders and optional regex filter.</description></item>
    /// <item><description>Choose output directory and extraction options.</description></item>
    /// <item><description>Preview sprites and adjust selection/naming as needed.</description></item>
    /// <item><description>Click Extract to generate individual sprite files.</description></item>
    /// </list>
    /// <para>
    /// Pros: batch processing, preserves import settings, preview before extraction, undo support
    /// for reference replacement. Caveats: extraction is one-way; reference replacement is
    /// potentially destructive—use VCS.
    /// </para>
    /// </remarks>
    public sealed class SpriteSheetExtractor : EditorWindow
    {
        private const string Name = "Sprite Sheet Extractor";

        /// <summary>
        /// Minimum score threshold for boundary transparency detection.
        /// Lowered from 0.5 to 0.15 to handle sprite sheets with thin transparent gutters.
        /// </summary>
        private const float MinimumBoundaryScore = 0.15f;

        private static readonly string[] ImageFileExtensions =
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".bmp",
            ".tga",
            ".psd",
            ".gif",
        };

        /// <summary>
        /// Common sprite cell sizes for grid detection candidate generation.
        /// Avoids allocation during DetectOptimalGridFromTransparency calls.
        /// </summary>
        private static readonly int[] CommonCellSizes =
        {
            8,
            16,
            24,
            32,
            48,
            64,
            96,
            128,
            256,
            512,
        };

        /// <summary>
        /// Represents a discovered sprite sheet with its metadata.
        /// </summary>
        internal sealed class SpriteSheetEntry
        {
            internal string _assetPath;
            internal Texture2D _texture;
            internal TextureImporter _importer;
            internal SpriteImportMode _importMode;
            internal List<SpriteEntryData> _sprites;
            internal bool _isExpanded;
            internal bool _isSelected;

            internal bool _useGlobalSettings = true;
            internal bool _perSheetSettingsFoldout;
            internal ExtractionMode? _extractionModeOverride;
            internal GridSizeMode? _gridSizeModeOverride;
            internal int? _gridColumnsOverride;
            internal int? _gridRowsOverride;
            internal int? _cellWidthOverride;
            internal int? _cellHeightOverride;
            internal int? _paddingLeftOverride;
            internal int? _paddingRightOverride;
            internal int? _paddingTopOverride;
            internal int? _paddingBottomOverride;
            internal float? _alphaThresholdOverride;
            internal bool? _showGridOverlayOverride;
            internal bool _sourcePreviewExpanded;
        }

        /// <summary>
        /// Represents an individual sprite within a sprite sheet.
        /// </summary>
        internal sealed class SpriteEntryData
        {
            internal string _originalName;
            internal string _outputName;
            internal Rect _rect;
            internal Vector2 _pivot;
            internal Vector4 _border;
            internal int _sortIndex;
            internal bool _isSelected;
            internal Texture2D _previewTexture;
        }

        public enum SortMode
        {
            [Obsolete("Use a specific SortMode value instead of None.")]
            None = 0,
            Original = 1,
            ByName = 2,
            ByPositionTopLeft = 3,
            ByPositionBottomLeft = 4,
            Reversed = 5,
        }

        /// <summary>
        /// Determines how sprites are discovered and extracted from sprite sheets.
        /// </summary>
        public enum ExtractionMode
        {
            [Obsolete("Use a specific ExtractionMode value instead of None.")]
            None = 0,
            FromMetadata = 1,
            GridBased = 2,
            AlphaDetection = 3,
            PaddedGrid = 4,
        }

        /// <summary>
        /// Determines whether grid dimensions are calculated automatically or manually specified.
        /// </summary>
        public enum GridSizeMode
        {
            [Obsolete("Use a specific GridSizeMode value instead of None.")]
            None = 0,
            Auto = 1,
            Manual = 2,
        }

        /// <summary>
        /// Determines the size of sprite preview thumbnails.
        /// </summary>
        public enum PreviewSizeMode
        {
            [Obsolete("Use a specific PreviewSizeMode value instead of None.")]
            None = 0,
            Size24 = 1,
            Size32 = 2,
            Size64 = 3,
            RealSize = 4,
        }

        [SerializeField]
        internal List<Object> _inputDirectories = new();

        [SerializeField]
        internal string _spriteNameRegex = ".*";

        [SerializeField]
        internal Object _outputDirectory;

        [SerializeField]
        internal string _namingPrefix = "";

        [SerializeField]
        internal bool _preserveImportSettings = true;

        [SerializeField]
        internal bool _overwriteExisting;

        [SerializeField]
        internal bool _dryRun;

        [SerializeField]
        internal SortMode _sortMode = SortMode.Original;

        [SerializeField]
        internal ExtractionMode _extractionMode = ExtractionMode.FromMetadata;

        [SerializeField]
        internal GridSizeMode _gridSizeMode = GridSizeMode.Auto;

        [SerializeField]
        internal PreviewSizeMode _previewSizeMode = PreviewSizeMode.Size32;

        [SerializeField]
        internal int _gridColumns = 4;

        [SerializeField]
        internal int _gridRows = 4;

        [SerializeField]
        internal int _cellWidth = 32;

        [SerializeField]
        internal int _cellHeight = 32;

        [SerializeField]
        internal int _paddingLeft;

        [SerializeField]
        internal int _paddingRight;

        [SerializeField]
        internal int _paddingTop;

        [SerializeField]
        internal int _paddingBottom;

        [SerializeField]
        internal float _alphaThreshold = 0.01f;

        [SerializeField]
        internal bool _showGridOverlay;

        [SerializeField]
        internal Color _gridLineColor = new Color(1f, 0f, 0f, 0.5f);

        [SerializeField]
        internal bool _sourcePreviewFoldout;

        [SerializeField]
        internal bool _dangerZoneFoldout;

        // Intentionally not serialized - users must re-acknowledge danger each session
        private bool _ackDanger;

        private SerializedObject _serializedObject;
        private SerializedProperty _inputDirectoriesProperty;
        private SerializedProperty _spriteNameRegexProperty;
        private SerializedProperty _outputDirectoryProperty;
        private SerializedProperty _namingPrefixProperty;
        private SerializedProperty _preserveImportSettingsProperty;
        private SerializedProperty _overwriteExistingProperty;
        private SerializedProperty _dryRunProperty;
        private SerializedProperty _sortModeProperty;
        private SerializedProperty _extractionModeProperty;
        private SerializedProperty _gridSizeModeProperty;
        private SerializedProperty _previewSizeModeProperty;
        private SerializedProperty _gridColumnsProperty;
        private SerializedProperty _gridRowsProperty;
        private SerializedProperty _cellWidthProperty;
        private SerializedProperty _cellHeightProperty;
        private SerializedProperty _paddingLeftProperty;
        private SerializedProperty _paddingRightProperty;
        private SerializedProperty _paddingTopProperty;
        private SerializedProperty _paddingBottomProperty;
        private SerializedProperty _alphaThresholdProperty;
        private SerializedProperty _showGridOverlayProperty;
        private SerializedProperty _gridLineColorProperty;
        private SerializedProperty _sourcePreviewFoldoutProperty;
        private SerializedProperty _dangerZoneFoldoutProperty;

        private Regex _regex;
        private string _regexError;
        private string _lastValidatedRegex;

        private SortMode _lastSortMode;
        private List<SpriteEntryData> _cachedSortedSprites;
        private List<SpriteEntryData> _lastSpritesSource;

        private PreviewSizeMode _lastPreviewSizeMode;
        private ExtractionMode _lastExtractionMode;
        internal bool _previewRegenerationScheduled;
        private bool _regenerationInProgress;

        internal List<SpriteSheetEntry> _discoveredSheets;
        private Vector2 _scrollPosition;

        private int _lastExtractedCount;
        private int _lastSkippedCount;
        private int _lastErrorCount;

        internal static bool SuppressUserPrompts { get; set; }

        static SpriteSheetExtractor()
        {
            try
            {
                if (Application.isBatchMode || Utils.EditorUi.Suppress)
                {
                    SuppressUserPrompts = true;
                }
            }
            catch
            {
                // Ignore environment probing failures
            }
        }

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/" + Name)]
        private static void ShowWindow() => GetWindow<SpriteSheetExtractor>(Name);

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _inputDirectoriesProperty = _serializedObject.FindProperty(nameof(_inputDirectories));
            _spriteNameRegexProperty = _serializedObject.FindProperty(nameof(_spriteNameRegex));
            _outputDirectoryProperty = _serializedObject.FindProperty(nameof(_outputDirectory));
            _namingPrefixProperty = _serializedObject.FindProperty(nameof(_namingPrefix));
            _preserveImportSettingsProperty = _serializedObject.FindProperty(
                nameof(_preserveImportSettings)
            );
            _overwriteExistingProperty = _serializedObject.FindProperty(nameof(_overwriteExisting));
            _dryRunProperty = _serializedObject.FindProperty(nameof(_dryRun));
            _sortModeProperty = _serializedObject.FindProperty(nameof(_sortMode));
            _extractionModeProperty = _serializedObject.FindProperty(nameof(_extractionMode));
            _gridSizeModeProperty = _serializedObject.FindProperty(nameof(_gridSizeMode));
            _previewSizeModeProperty = _serializedObject.FindProperty(nameof(_previewSizeMode));
            _gridColumnsProperty = _serializedObject.FindProperty(nameof(_gridColumns));
            _gridRowsProperty = _serializedObject.FindProperty(nameof(_gridRows));
            _cellWidthProperty = _serializedObject.FindProperty(nameof(_cellWidth));
            _cellHeightProperty = _serializedObject.FindProperty(nameof(_cellHeight));
            _paddingLeftProperty = _serializedObject.FindProperty(nameof(_paddingLeft));
            _paddingRightProperty = _serializedObject.FindProperty(nameof(_paddingRight));
            _paddingTopProperty = _serializedObject.FindProperty(nameof(_paddingTop));
            _paddingBottomProperty = _serializedObject.FindProperty(nameof(_paddingBottom));
            _alphaThresholdProperty = _serializedObject.FindProperty(nameof(_alphaThreshold));
            _showGridOverlayProperty = _serializedObject.FindProperty(nameof(_showGridOverlay));
            _gridLineColorProperty = _serializedObject.FindProperty(nameof(_gridLineColor));
            _sourcePreviewFoldoutProperty = _serializedObject.FindProperty(
                nameof(_sourcePreviewFoldout)
            );
            _dangerZoneFoldoutProperty = _serializedObject.FindProperty(nameof(_dangerZoneFoldout));

            _lastPreviewSizeMode = _previewSizeMode;
            _lastExtractionMode = _extractionMode;

            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            CleanupPreviewTextures();
            _cachedSortedSprites = null;
            _lastSpritesSource = null;
        }

        /// <summary>
        /// Editor update callback to force continuous repainting while preview regeneration is in progress.
        /// </summary>
        private void OnEditorUpdate()
        {
            if (_regenerationInProgress)
            {
                Repaint();
            }
        }

        private void CleanupPreviewTextures()
        {
            if (_discoveredSheets == null)
            {
                return;
            }

            for (int i = 0; i < _discoveredSheets.Count; ++i)
            {
                SpriteSheetEntry entry = _discoveredSheets[i];
                if (entry?._sprites == null)
                {
                    continue;
                }

                for (int j = 0; j < entry._sprites.Count; ++j)
                {
                    SpriteEntryData sprite = entry._sprites[j];
                    if (sprite?._previewTexture != null)
                    {
                        DestroyImmediate(sprite._previewTexture);
                        sprite._previewTexture = null;
                    }
                }
            }
        }

        private static bool IsTextureFormatSupportedForGetPixels(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.Alpha8:
                case TextureFormat.ARGB4444:
                case TextureFormat.RGB24:
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                case TextureFormat.RGB565:
                case TextureFormat.R16:
                case TextureFormat.RGBA4444:
                case TextureFormat.BGRA32:
                case TextureFormat.RHalf:
                case TextureFormat.RGHalf:
                case TextureFormat.RGBAHalf:
                case TextureFormat.RFloat:
                case TextureFormat.RGFloat:
                case TextureFormat.RGBAFloat:
                case TextureFormat.R8:
                case TextureFormat.RG16:
                case TextureFormat.RG32:
                case TextureFormat.RGB48:
                case TextureFormat.RGBA64:
                    return true;
                default:
                    return false;
            }
        }

        private void OnGUI()
        {
            _serializedObject.Update();

            DrawInputSection();
            EditorGUILayout.Space();
            DrawOutputSection();
            EditorGUILayout.Space();
            DrawDiscoverySection();
            EditorGUILayout.Space();
            DrawPreviewSection();
            EditorGUILayout.Space();
            DrawExtractionSection();
            EditorGUILayout.Space();
            DrawDangerZone();

            _serializedObject.ApplyModifiedProperties();
        }

        private void DrawInputSection()
        {
            EditorGUILayout.LabelField("Input Directories", EditorStyles.boldLabel);
            PersistentDirectoryGUI.PathSelectorObjectArray(
                _inputDirectoriesProperty,
                nameof(SpriteSheetExtractor)
            );

            EditorGUILayout.PropertyField(
                _spriteNameRegexProperty,
                new GUIContent(
                    "Sprite Name Regex",
                    "Optional .NET regex to filter sprite sheets by file name (no extension)."
                )
            );

            ValidateRegex();
            if (!string.IsNullOrEmpty(_regexError))
            {
                EditorGUILayout.HelpBox($"Invalid regex: {_regexError}", MessageType.Error);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Extraction Mode", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                _extractionModeProperty,
                new GUIContent(
                    "Mode",
                    "FromMetadata: Use existing sprite metadata.\n"
                        + "GridBased: Slice texture by grid.\n"
                        + "AlphaDetection: Detect sprites by alpha boundaries.\n"
                        + "PaddedGrid: Grid with padding between cells."
                )
            );

            DrawExtractionModeOptions();
        }

        private void DrawExtractionModeOptions()
        {
            bool showGridOptions =
                _extractionMode == ExtractionMode.GridBased
                || _extractionMode == ExtractionMode.PaddedGrid;
            bool showPaddingOptions = _extractionMode == ExtractionMode.PaddedGrid;
            bool showAlphaOptions = _extractionMode == ExtractionMode.AlphaDetection;

            if (showGridOptions)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(
                        _gridSizeModeProperty,
                        new GUIContent(
                            "Grid Size Mode",
                            "Auto: Calculate grid from texture dimensions.\n"
                                + "Manual: Specify columns/rows or cell dimensions."
                        )
                    );

                    if (_gridSizeMode == GridSizeMode.Manual)
                    {
                        EditorGUILayout.PropertyField(
                            _gridColumnsProperty,
                            new GUIContent("Columns", "Number of columns in the grid.")
                        );
                        EditorGUILayout.PropertyField(
                            _gridRowsProperty,
                            new GUIContent("Rows", "Number of rows in the grid.")
                        );
                        EditorGUILayout.PropertyField(
                            _cellWidthProperty,
                            new GUIContent(
                                "Cell Width",
                                "Width of each grid cell in pixels (0 = auto)."
                            )
                        );
                        EditorGUILayout.PropertyField(
                            _cellHeightProperty,
                            new GUIContent(
                                "Cell Height",
                                "Height of each grid cell in pixels (0 = auto)."
                            )
                        );

                        _gridColumns = Mathf.Max(1, _gridColumns);
                        _gridRows = Mathf.Max(1, _gridRows);
                        _cellWidth = Mathf.Max(0, _cellWidth);
                        _cellHeight = Mathf.Max(0, _cellHeight);
                    }
                }
            }

            if (showPaddingOptions)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField("Padding", EditorStyles.miniBoldLabel);

                    EditorGUILayout.PropertyField(
                        _paddingLeftProperty,
                        new GUIContent("Left", "Padding from left edge of each cell.")
                    );
                    EditorGUILayout.PropertyField(
                        _paddingRightProperty,
                        new GUIContent("Right", "Padding from right edge of each cell.")
                    );
                    EditorGUILayout.PropertyField(
                        _paddingTopProperty,
                        new GUIContent("Top", "Padding from top edge of each cell.")
                    );
                    EditorGUILayout.PropertyField(
                        _paddingBottomProperty,
                        new GUIContent("Bottom", "Padding from bottom edge of each cell.")
                    );

                    _paddingLeft = Mathf.Max(0, _paddingLeft);
                    _paddingRight = Mathf.Max(0, _paddingRight);
                    _paddingTop = Mathf.Max(0, _paddingTop);
                    _paddingBottom = Mathf.Max(0, _paddingBottom);
                }
            }

            if (showAlphaOptions)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(
                        _alphaThresholdProperty,
                        new GUIContent(
                            "Alpha Threshold",
                            "Pixels with alpha above this value are considered opaque. (0.0-1.0)"
                        )
                    );
                    _alphaThreshold = Mathf.Clamp01(_alphaThreshold);
                }
            }
        }

        private void ValidateRegex()
        {
            if (string.Equals(_spriteNameRegex, _lastValidatedRegex, StringComparison.Ordinal))
            {
                return;
            }

            _lastValidatedRegex = _spriteNameRegex;
            if (string.IsNullOrWhiteSpace(_spriteNameRegex))
            {
                _regexError = null;
                return;
            }

            try
            {
                _ = new Regex(_spriteNameRegex, RegexOptions.CultureInvariant);
                _regexError = null;
            }
            catch (ArgumentException ex)
            {
                _regexError = ex.Message;
            }
        }

        private void DrawOutputSection()
        {
            EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                _outputDirectoryProperty,
                new GUIContent(
                    "Output Directory",
                    "Directory where extracted sprites will be saved. Required."
                )
            );

            EditorGUILayout.PropertyField(
                _namingPrefixProperty,
                new GUIContent(
                    "Naming Prefix Override",
                    "Optional prefix for output filenames. If empty, uses original texture name."
                )
            );

            EditorGUILayout.PropertyField(
                _preserveImportSettingsProperty,
                new GUIContent(
                    "Preserve Import Settings",
                    "Copy PPU, pivot, borders, and other settings from original sprite sheet."
                )
            );

            EditorGUILayout.PropertyField(
                _overwriteExistingProperty,
                new GUIContent(
                    "Overwrite Existing Files",
                    "If enabled, existing files in output directory will be overwritten."
                )
            );
            if (_overwriteExisting)
            {
                EditorGUILayout.HelpBox(
                    "Warning: Existing files will be overwritten without confirmation.",
                    MessageType.Warning
                );
            }

            EditorGUILayout.PropertyField(
                _dryRunProperty,
                new GUIContent(
                    "Dry Run",
                    "Simulate extraction without writing files. Shows what would be created."
                )
            );
        }

        private void DrawDiscoverySection()
        {
            EditorGUILayout.LabelField("Discovery", EditorStyles.boldLabel);

            if (GUILayout.Button("Find Sprite Sheets"))
            {
                if (!string.IsNullOrEmpty(_regexError))
                {
                    ShowNotification(new GUIContent("Invalid regex. Fix it before searching."));
                    return;
                }

                DiscoverSpriteSheets();
            }
        }

        private void DrawPreviewSection()
        {
            if (_discoveredSheets == null || _discoveredSheets.Count == 0)
            {
                if (_discoveredSheets != null)
                {
                    EditorGUILayout.LabelField(
                        "No sprite sheets found in selected directories.",
                        EditorStyles.label
                    );
                }
                return;
            }

            EditorGUILayout.LabelField(
                $"Found {_discoveredSheets.Count} sprite sheet(s)",
                EditorStyles.boldLabel
            );

            DrawBulkActions();

            EditorGUILayout.PropertyField(
                _sortModeProperty,
                new GUIContent("Sort Mode", "How to sort sprites within each sheet for extraction.")
            );

            EditorGUILayout.PropertyField(
                _previewSizeModeProperty,
                new GUIContent(
                    "Preview Size",
                    "Size of sprite preview thumbnails. RealSize uses actual sprite dimensions."
                )
            );

            bool previewSizeModeChanged = _lastPreviewSizeMode != _previewSizeMode;
            bool extractionModeChanged = _lastExtractionMode != _extractionMode;

            if ((previewSizeModeChanged || extractionModeChanged) && !_previewRegenerationScheduled)
            {
                _lastPreviewSizeMode = _previewSizeMode;
                _lastExtractionMode = _extractionMode;
                _previewRegenerationScheduled = true;
                EditorApplication.delayCall += RegenerateAllPreviewTextures;
            }

            bool showGridOverlayOption = AnySheetUsesGridBasedExtraction();
            if (showGridOverlayOption)
            {
                EditorGUILayout.PropertyField(
                    _showGridOverlayProperty,
                    new GUIContent(
                        "Show Grid Overlay",
                        "Display grid lines on source texture previews."
                    )
                );
                if (_showGridOverlay)
                {
                    EditorGUILayout.PropertyField(
                        _gridLineColorProperty,
                        new GUIContent("Grid Line Color", "Color of the grid overlay lines.")
                    );
                }
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(
                _scrollPosition,
                GUILayout.MaxHeight(400)
            );
            try
            {
                for (int i = 0; i < _discoveredSheets.Count; ++i)
                {
                    DrawSpriteSheetEntry(_discoveredSheets[i]);
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawBulkActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select All"))
                {
                    SetAllSelection(true);
                }
                if (GUILayout.Button("Select None"))
                {
                    SetAllSelection(false);
                }
                if (GUILayout.Button("Expand All"))
                {
                    SetAllExpanded(true);
                }
                if (GUILayout.Button("Collapse All"))
                {
                    SetAllExpanded(false);
                }
                if (GUILayout.Button("Apply Global to All"))
                {
                    ApplyGlobalSettingsToAll();
                }
            }
        }

        internal void ApplyGlobalSettingsToAll()
        {
            if (_discoveredSheets == null)
            {
                return;
            }

            for (int i = 0; i < _discoveredSheets.Count; ++i)
            {
                SpriteSheetEntry entry = _discoveredSheets[i];
                entry._useGlobalSettings = false;
                entry._extractionModeOverride = _extractionMode;
                entry._gridSizeModeOverride = _gridSizeMode;
                entry._gridColumnsOverride = _gridColumns;
                entry._gridRowsOverride = _gridRows;
                entry._cellWidthOverride = _cellWidth;
                entry._cellHeightOverride = _cellHeight;
                entry._paddingLeftOverride = _paddingLeft;
                entry._paddingRightOverride = _paddingRight;
                entry._paddingTopOverride = _paddingTop;
                entry._paddingBottomOverride = _paddingBottom;
                entry._alphaThresholdOverride = _alphaThreshold;
                entry._showGridOverlayOverride = _showGridOverlay;
            }

            Repaint();
        }

        private void SetAllSelection(bool selected)
        {
            if (_discoveredSheets == null)
            {
                return;
            }

            for (int i = 0; i < _discoveredSheets.Count; ++i)
            {
                SpriteSheetEntry entry = _discoveredSheets[i];
                entry._isSelected = selected;
                if (entry._sprites != null)
                {
                    for (int j = 0; j < entry._sprites.Count; ++j)
                    {
                        entry._sprites[j]._isSelected = selected;
                    }
                }
            }
            Repaint();
        }

        private void SetAllExpanded(bool expanded)
        {
            if (_discoveredSheets == null)
            {
                return;
            }

            for (int i = 0; i < _discoveredSheets.Count; ++i)
            {
                _discoveredSheets[i]._isExpanded = expanded;
            }
            Repaint();
        }

        private void DrawSpriteSheetEntry(SpriteSheetEntry entry)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                int spriteCount = entry._sprites != null ? entry._sprites.Count : 0;

                using (new EditorGUILayout.HorizontalScope())
                {
                    entry._isSelected = EditorGUILayout.Toggle(
                        entry._isSelected,
                        GUILayout.Width(20)
                    );

                    entry._isExpanded = EditorGUILayout.Foldout(
                        entry._isExpanded,
                        $"{Path.GetFileName(entry._assetPath)} ({spriteCount} sprites)",
                        true
                    );

                    if (entry._importMode == SpriteImportMode.Single)
                    {
                        GUIContent warningIcon = EditorGUIUtility.IconContent(
                            "console.warnicon.sml"
                        );
                        warningIcon.tooltip =
                            "Single sprite mode - only one sprite will be extracted.";
                        GUILayout.Label(warningIcon, GUILayout.Width(20));
                    }
                }

                if (entry._isExpanded)
                {
                    DrawPerSheetSettings(entry);
                }

                DrawGridValidationWarnings(entry);

                if (entry._isExpanded)
                {
                    ExtractionMode effectiveMode = GetEffectiveExtractionMode(entry);
                    bool isGridBased =
                        effectiveMode == ExtractionMode.GridBased
                        || effectiveMode == ExtractionMode.PaddedGrid;

                    using (new EditorGUI.IndentLevelScope())
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(EditorGUI.indentLevel * 15f);
                            if (GUILayout.Button("Preview Slicing", GUILayout.Width(120)))
                            {
                                entry._sourcePreviewExpanded = true;
                                if (isGridBased)
                                {
                                    if (entry._useGlobalSettings)
                                    {
                                        _showGridOverlayProperty.boolValue = true;
                                    }
                                    else
                                    {
                                        entry._showGridOverlayOverride = true;
                                    }
                                }
                                Repaint();
                            }

                            if (isGridBased)
                            {
                                bool currentOverlay = GetEffectiveShowGridOverlay(entry);
                                bool newOverlay = GUILayout.Toggle(
                                    currentOverlay,
                                    "Show Grid Overlay"
                                );
                                if (newOverlay != currentOverlay)
                                {
                                    if (entry._useGlobalSettings)
                                    {
                                        _showGridOverlayProperty.boolValue = newOverlay;
                                    }
                                    else
                                    {
                                        entry._showGridOverlayOverride = newOverlay;
                                    }
                                }
                            }
                        }
                    }
                }

                if (entry._isExpanded && entry._sprites != null)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        DrawSourceTexturePreview(entry);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Select All", GUILayout.Width(80)))
                            {
                                for (int i = 0; i < entry._sprites.Count; ++i)
                                {
                                    entry._sprites[i]._isSelected = true;
                                }
                            }
                            if (GUILayout.Button("Select None", GUILayout.Width(80)))
                            {
                                for (int i = 0; i < entry._sprites.Count; ++i)
                                {
                                    entry._sprites[i]._isSelected = false;
                                }
                            }
                        }

                        List<SpriteEntryData> sortedSprites = GetSortedSprites(entry._sprites);
                        for (int i = 0; i < sortedSprites.Count; ++i)
                        {
                            SpriteEntryData sprite = sortedSprites[i];
                            DrawSpriteEntry(entry, sprite, i);
                        }
                    }
                }
            }
        }

        private void DrawGridValidationWarnings(SpriteSheetEntry entry)
        {
            ExtractionMode effectiveExtractionMode = GetEffectiveExtractionMode(entry);
            if (
                effectiveExtractionMode != ExtractionMode.GridBased
                && effectiveExtractionMode != ExtractionMode.PaddedGrid
            )
            {
                return;
            }

            if (entry._texture == null)
            {
                return;
            }

            int textureWidth = entry._texture.width;
            int textureHeight = entry._texture.height;

            int effectiveCellWidth;
            int effectiveCellHeight;
            int effectiveColumns;
            int effectiveRows;

            CalculateGridDimensions(
                textureWidth,
                textureHeight,
                entry,
                out effectiveColumns,
                out effectiveRows,
                out effectiveCellWidth,
                out effectiveCellHeight
            );

            bool widthMismatch = (effectiveCellWidth * effectiveColumns) != textureWidth;
            bool heightMismatch = (effectiveCellHeight * effectiveRows) != textureHeight;

            if (widthMismatch || heightMismatch)
            {
                string warning = "Grid dimensions do not evenly divide texture:";
                if (widthMismatch)
                {
                    warning +=
                        $" Width {textureWidth} / {effectiveColumns} cols = {(float)textureWidth / effectiveColumns:F2} pixels/cell.";
                }
                if (heightMismatch)
                {
                    warning +=
                        $" Height {textureHeight} / {effectiveRows} rows = {(float)textureHeight / effectiveRows:F2} pixels/cell.";
                }
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
        }

        private void DrawPerSheetSettings(SpriteSheetEntry entry)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                entry._perSheetSettingsFoldout = EditorGUILayout.Foldout(
                    entry._perSheetSettingsFoldout,
                    "Per-Sheet Settings",
                    true
                );

                if (!entry._perSheetSettingsFoldout)
                {
                    return;
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    bool previousUseGlobal = entry._useGlobalSettings;
                    entry._useGlobalSettings = EditorGUILayout.Toggle(
                        new GUIContent(
                            "Use Global Settings",
                            "When enabled, this sheet uses the global extraction settings."
                        ),
                        entry._useGlobalSettings
                    );

                    if (!entry._useGlobalSettings)
                    {
                        DrawPerSheetOverrideFields(entry);

                        DrawCopySettingsFromButton(entry);
                    }

                    if (previousUseGlobal != entry._useGlobalSettings)
                    {
                        SchedulePreviewRegenerationForEntry(entry);
                    }
                }
            }
        }

        private void DrawPerSheetOverrideFields(SpriteSheetEntry entry)
        {
            ExtractionMode previousExtractionMode =
                entry._extractionModeOverride ?? _extractionMode;
            entry._extractionModeOverride = (ExtractionMode)
                EditorGUILayout.EnumPopup(
                    new GUIContent("Extraction Mode", "How sprites are extracted from this sheet."),
                    entry._extractionModeOverride ?? _extractionMode
                );

            ExtractionMode effectiveMode = entry._extractionModeOverride.Value;
            bool showGridOptions =
                effectiveMode == ExtractionMode.GridBased
                || effectiveMode == ExtractionMode.PaddedGrid;
            bool showPaddingOptions = effectiveMode == ExtractionMode.PaddedGrid;
            bool showAlphaOptions = effectiveMode == ExtractionMode.AlphaDetection;

            if (showGridOptions)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    entry._gridSizeModeOverride = (GridSizeMode)
                        EditorGUILayout.EnumPopup(
                            new GUIContent(
                                "Grid Size Mode",
                                "Auto: Calculate grid from texture dimensions.\nManual: Specify columns/rows or cell dimensions."
                            ),
                            entry._gridSizeModeOverride ?? _gridSizeMode
                        );

                    GridSizeMode effectiveGridMode = entry._gridSizeModeOverride.Value;
                    if (effectiveGridMode == GridSizeMode.Manual)
                    {
                        int gridColumnsValue = entry._gridColumnsOverride ?? _gridColumns;
                        gridColumnsValue = EditorGUILayout.IntField(
                            new GUIContent("Columns", "Number of columns in the grid."),
                            gridColumnsValue
                        );
                        entry._gridColumnsOverride = Mathf.Max(1, gridColumnsValue);

                        int gridRowsValue = entry._gridRowsOverride ?? _gridRows;
                        gridRowsValue = EditorGUILayout.IntField(
                            new GUIContent("Rows", "Number of rows in the grid."),
                            gridRowsValue
                        );
                        entry._gridRowsOverride = Mathf.Max(1, gridRowsValue);

                        int cellWidthValue = entry._cellWidthOverride ?? _cellWidth;
                        cellWidthValue = EditorGUILayout.IntField(
                            new GUIContent(
                                "Cell Width",
                                "Width of each grid cell in pixels (0 = auto)."
                            ),
                            cellWidthValue
                        );
                        entry._cellWidthOverride = Mathf.Max(0, cellWidthValue);

                        int cellHeightValue = entry._cellHeightOverride ?? _cellHeight;
                        cellHeightValue = EditorGUILayout.IntField(
                            new GUIContent(
                                "Cell Height",
                                "Height of each grid cell in pixels (0 = auto)."
                            ),
                            cellHeightValue
                        );
                        entry._cellHeightOverride = Mathf.Max(0, cellHeightValue);
                    }
                }
            }

            if (showPaddingOptions)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField("Padding", EditorStyles.miniBoldLabel);

                    int paddingLeftValue = entry._paddingLeftOverride ?? _paddingLeft;
                    paddingLeftValue = EditorGUILayout.IntField(
                        new GUIContent("Left", "Padding from left edge of each cell."),
                        paddingLeftValue
                    );
                    entry._paddingLeftOverride = Mathf.Max(0, paddingLeftValue);

                    int paddingRightValue = entry._paddingRightOverride ?? _paddingRight;
                    paddingRightValue = EditorGUILayout.IntField(
                        new GUIContent("Right", "Padding from right edge of each cell."),
                        paddingRightValue
                    );
                    entry._paddingRightOverride = Mathf.Max(0, paddingRightValue);

                    int paddingTopValue = entry._paddingTopOverride ?? _paddingTop;
                    paddingTopValue = EditorGUILayout.IntField(
                        new GUIContent("Top", "Padding from top edge of each cell."),
                        paddingTopValue
                    );
                    entry._paddingTopOverride = Mathf.Max(0, paddingTopValue);

                    int paddingBottomValue = entry._paddingBottomOverride ?? _paddingBottom;
                    paddingBottomValue = EditorGUILayout.IntField(
                        new GUIContent("Bottom", "Padding from bottom edge of each cell."),
                        paddingBottomValue
                    );
                    entry._paddingBottomOverride = Mathf.Max(0, paddingBottomValue);
                }
            }

            if (showAlphaOptions)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    float alphaThresholdValue = entry._alphaThresholdOverride ?? _alphaThreshold;
                    alphaThresholdValue = EditorGUILayout.Slider(
                        new GUIContent(
                            "Alpha Threshold",
                            "Pixels with alpha above this value are considered opaque. (0.0-1.0)"
                        ),
                        alphaThresholdValue,
                        0f,
                        1f
                    );
                    entry._alphaThresholdOverride = alphaThresholdValue;
                }
            }

            if (showGridOptions)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    bool currentOverlayValue = entry._showGridOverlayOverride ?? _showGridOverlay;
                    bool newOverlayValue = EditorGUILayout.Toggle(
                        new GUIContent(
                            "Show Grid Overlay",
                            "Display grid lines on the source texture preview for this sheet."
                        ),
                        currentOverlayValue
                    );
                    entry._showGridOverlayOverride = newOverlayValue;
                }
            }

            if (previousExtractionMode != entry._extractionModeOverride)
            {
                SchedulePreviewRegenerationForEntry(entry);
            }
        }

        private void DrawCopySettingsFromButton(SpriteSheetEntry entry)
        {
            if (_discoveredSheets == null || _discoveredSheets.Count < 2)
            {
                return;
            }

            if (GUILayout.Button("Copy Settings From..."))
            {
                GenericMenu menu = new GenericMenu();

                for (int i = 0; i < _discoveredSheets.Count; ++i)
                {
                    SpriteSheetEntry sourceEntry = _discoveredSheets[i];
                    if (sourceEntry == entry)
                    {
                        continue;
                    }

                    string entryName = Path.GetFileName(sourceEntry._assetPath);
                    int capturedIndex = i;
                    menu.AddItem(
                        new GUIContent(entryName),
                        false,
                        () => CopySettingsFromEntry(_discoveredSheets[capturedIndex], entry)
                    );
                }

                menu.ShowAsContext();
            }
        }

        internal void CopySettingsFromEntry(SpriteSheetEntry source, SpriteSheetEntry target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target._useGlobalSettings = source._useGlobalSettings;
            target._extractionModeOverride = source._extractionModeOverride;
            target._gridSizeModeOverride = source._gridSizeModeOverride;
            target._gridColumnsOverride = source._gridColumnsOverride;
            target._gridRowsOverride = source._gridRowsOverride;
            target._cellWidthOverride = source._cellWidthOverride;
            target._cellHeightOverride = source._cellHeightOverride;
            target._paddingLeftOverride = source._paddingLeftOverride;
            target._paddingRightOverride = source._paddingRightOverride;
            target._paddingTopOverride = source._paddingTopOverride;
            target._paddingBottomOverride = source._paddingBottomOverride;
            target._alphaThresholdOverride = source._alphaThresholdOverride;
            target._showGridOverlayOverride = source._showGridOverlayOverride;

            SchedulePreviewRegenerationForEntry(target);
        }

        private void SchedulePreviewRegenerationForEntry(SpriteSheetEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            _regenerationInProgress = true;
            // Store preview textures from current sprites, keyed by rect for transfer
            using PooledResource<Dictionary<Rect, Texture2D>> rectToPreviewLease = DictionaryBuffer<
                Rect,
                Texture2D
            >.Dictionary.Get(out Dictionary<Rect, Texture2D> rectToPreview);
            try
            {
                // Keep the old sprites list reference - it will remain visible during repopulation
                // to prevent grey question marks if Unity repaints during AssetDatabase operations
                List<SpriteEntryData> oldSprites = entry._sprites;

                // Store preview textures from old sprites, keyed by rect for transfer
                // IMPORTANT: Do NOT clear _previewTexture from old sprites yet - they need to remain
                // visible if Unity repaints during population
                if (oldSprites != null)
                {
                    for (int i = 0; i < oldSprites.Count; ++i)
                    {
                        SpriteEntryData sprite = oldSprites[i];
                        if (sprite != null && sprite._previewTexture != null)
                        {
                            // If duplicate rect exists, destroy the old texture to prevent memory leak
                            if (
                                rectToPreview.TryGetValue(sprite._rect, out Texture2D existing)
                                && existing != null
                            )
                            {
                                DestroyImmediate(existing);
                            }
                            // Use rect as key to identify matching sprites after repopulation
                            // Keep the preview in the old sprite for now - it will be displayed if repaint occurs
                            rectToPreview[sprite._rect] = sprite._previewTexture;
                        }
                    }
                }

                // Ensure texture and importer are fresh before repopulation
                // This handles cases where the asset may have been reimported or modified
                if (entry._texture == null || !entry._texture)
                {
                    entry._texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry._assetPath);
                }
                if (entry._importer == null || !entry._importer)
                {
                    entry._importer = AssetImporter.GetAtPath(entry._assetPath) as TextureImporter;
                }

                // Create a new list for the new sprites - don't modify entry._sprites yet
                // This keeps old sprites visible if Unity repaints during AssetDatabase operations
                List<SpriteEntryData> newSprites = new List<SpriteEntryData>();

                // Repopulate sprites into the new list
                RepopulateSpritesForEntryIntoList(entry, newSprites);

                // Transfer existing preview textures to new sprites with matching rects
                // This prevents grey question marks during the transition
                for (int i = 0; i < newSprites.Count; ++i)
                {
                    SpriteEntryData sprite = newSprites[i];
                    if (
                        sprite != null
                        && rectToPreview.TryGetValue(sprite._rect, out Texture2D existingPreview)
                    )
                    {
                        sprite._previewTexture = existingPreview;
                        // Remove from dictionary to mark as transferred (not orphaned)
                        rectToPreview.Remove(sprite._rect);
                    }
                }

                // Swap the sprites list FIRST - this is the key fix
                // The new sprites have transferred previews, so they're ready to display
                // Old sprites still have their preview references (not yet cleared)
                entry._sprites = newSprites;

                // NOW clear preview references from old sprites AFTER the swap
                // This prevents double-free issues since the textures are now owned by new sprites or rectToPreview
                // It's safe to clear now because entry._sprites points to newSprites
                if (oldSprites != null)
                {
                    for (int i = 0; i < oldSprites.Count; ++i)
                    {
                        SpriteEntryData sprite = oldSprites[i];
                        if (sprite != null)
                        {
                            sprite._previewTexture = null;
                        }
                    }
                }

                // Reload texture again if it became null during repopulation
                if (entry._texture == null || !entry._texture)
                {
                    entry._texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry._assetPath);
                }

                // Generate new previews if we have a valid texture
                if (entry._texture != null && entry._sprites != null && entry._sprites.Count > 0)
                {
                    using PooledResource<List<SpriteSheetEntry>> singleEntryLease =
                        Buffers<SpriteSheetEntry>.List.Get(
                            out List<SpriteSheetEntry> singleEntryList
                        );
                    singleEntryList.Add(entry);
                    GenerateAllPreviewTexturesInBatch(singleEntryList);
                }

                Repaint();

                // Schedule an additional delayed repaint to ensure the UI updates after any
                // async operations (like texture reimport) complete
                SpriteSheetExtractor windowRef = this;
                EditorApplication.delayCall += () =>
                {
                    if (windowRef)
                    {
                        windowRef.Repaint();
                    }
                };
            }
            finally
            {
                // Destroy orphaned textures (those not transferred to new sprites) even if an exception occurred
                // Use struct enumerator with using statement to properly dispose and avoid allocation
                using PooledResource<List<Texture2D>> orphanedTexturesLease =
                    Buffers<Texture2D>.List.Get(out List<Texture2D> orphanedTextures);
                using (
                    Dictionary<Rect, Texture2D>.Enumerator enumerator =
                        rectToPreview.GetEnumerator()
                )
                {
                    while (enumerator.MoveNext())
                    {
                        KeyValuePair<Rect, Texture2D> kvp = enumerator.Current;
                        if (kvp.Value != null)
                        {
                            orphanedTextures.Add(kvp.Value);
                        }
                    }
                }
                for (int i = 0; i < orphanedTextures.Count; ++i)
                {
                    DestroyImmediate(orphanedTextures[i]);
                }
                _regenerationInProgress = false;
            }
        }

        internal void RepopulateSpritesForEntry(SpriteSheetEntry entry)
        {
            if (entry == null || entry._texture == null)
            {
                return;
            }

            if (entry._sprites != null)
            {
                entry._sprites.Clear();
            }
            else
            {
                entry._sprites = new List<SpriteEntryData>();
            }

            ExtractionMode effectiveMode = GetEffectiveExtractionMode(entry);

            switch (effectiveMode)
            {
                case ExtractionMode.GridBased:
                    PopulateSpritesFromGrid(entry, entry._texture);
                    break;
                case ExtractionMode.PaddedGrid:
                    PopulateSpritesFromPaddedGrid(entry, entry._texture);
                    break;
                case ExtractionMode.AlphaDetection:
                    PopulateSpritesFromAlphaDetection(entry, entry._texture);
                    break;
                case ExtractionMode.FromMetadata:
                default:
                    PopulateSpritesFromMetadata(entry, entry._assetPath, entry._importer);
                    break;
            }
        }

        /// <summary>
        /// Repopulates sprites for a sprite sheet entry into a provided target list.
        /// IMPORTANT: This method does NOT modify entry._sprites at all, ensuring that old sprites
        /// remain visible if Unity repaints during AssetDatabase operations.
        /// </summary>
        /// <param name="entry">The sprite sheet entry to get configuration from.</param>
        /// <param name="targetList">The list to populate sprites into.</param>
        internal void RepopulateSpritesForEntryIntoList(
            SpriteSheetEntry entry,
            List<SpriteEntryData> targetList
        )
        {
            if (entry == null || entry._texture == null || targetList == null)
            {
                return;
            }

            ExtractionMode effectiveMode = GetEffectiveExtractionMode(entry);

            switch (effectiveMode)
            {
                case ExtractionMode.GridBased:
                    PopulateSpritesFromGridIntoList(entry, entry._texture, targetList);
                    break;
                case ExtractionMode.PaddedGrid:
                    PopulateSpritesFromPaddedGridIntoList(entry, entry._texture, targetList);
                    break;
                case ExtractionMode.AlphaDetection:
                    PopulateSpritesFromAlphaDetectionIntoList(entry, entry._texture, targetList);
                    break;
                case ExtractionMode.FromMetadata:
                default:
                    PopulateSpritesFromMetadataIntoList(
                        entry,
                        entry._assetPath,
                        entry._importer,
                        targetList
                    );
                    break;
            }
        }

        private void DrawSourceTexturePreview(SpriteSheetEntry entry)
        {
            entry._sourcePreviewExpanded = EditorGUILayout.Foldout(
                entry._sourcePreviewExpanded,
                "Source Texture Preview",
                true
            );

            if (!entry._sourcePreviewExpanded)
            {
                return;
            }

            if (entry._texture == null)
            {
                EditorGUILayout.LabelField("Texture not available.", EditorStyles.miniLabel);
                return;
            }

            int textureWidth = entry._texture.width;
            int textureHeight = entry._texture.height;

            float maxPreviewWidth = EditorGUIUtility.currentViewWidth - 50;
            float maxPreviewHeight = 200;
            float scale = Mathf.Min(
                maxPreviewWidth / textureWidth,
                maxPreviewHeight / textureHeight
            );
            scale = Mathf.Min(scale, 1f);

            float displayWidth = textureWidth * scale;
            float displayHeight = textureHeight * scale;

            Rect previewRect = GUILayoutUtility.GetRect(displayWidth, displayHeight);

            GUI.DrawTexture(previewRect, entry._texture, ScaleMode.ScaleToFit);

            ExtractionMode effectiveExtractionMode = GetEffectiveExtractionMode(entry);
            bool effectiveShowGridOverlay = GetEffectiveShowGridOverlay(entry);
            bool shouldDrawGrid =
                effectiveShowGridOverlay
                && (
                    effectiveExtractionMode == ExtractionMode.GridBased
                    || effectiveExtractionMode == ExtractionMode.PaddedGrid
                );
            if (shouldDrawGrid)
            {
                DrawGridOverlay(
                    previewRect,
                    entry._texture.width,
                    entry._texture.height,
                    scale,
                    entry
                );
            }

            // For non-grid modes, draw sprite rectangles if source preview is expanded
            if (!shouldDrawGrid && entry._sprites != null && entry._sprites.Count > 0)
            {
                DrawSpriteRectsOverlay(
                    previewRect,
                    entry._texture.width,
                    entry._texture.height,
                    scale,
                    entry
                );
            }
        }

        internal void CalculateGridDimensions(
            int textureWidth,
            int textureHeight,
            out int columns,
            out int rows,
            out int cellWidth,
            out int cellHeight
        )
        {
            CalculateGridDimensions(
                textureWidth,
                textureHeight,
                null,
                out columns,
                out rows,
                out cellWidth,
                out cellHeight
            );
        }

        internal void CalculateGridDimensions(
            int textureWidth,
            int textureHeight,
            SpriteSheetEntry entry,
            out int columns,
            out int rows,
            out int cellWidth,
            out int cellHeight
        )
        {
            CalculateGridDimensions(
                textureWidth,
                textureHeight,
                entry,
                null,
                out columns,
                out rows,
                out cellWidth,
                out cellHeight
            );
        }

        internal void CalculateGridDimensions(
            int textureWidth,
            int textureHeight,
            SpriteSheetEntry entry,
            Color32[] pixels,
            out int columns,
            out int rows,
            out int cellWidth,
            out int cellHeight
        )
        {
            GridSizeMode effectiveGridSizeMode = GetEffectiveGridSizeMode(entry);
            int effectiveGridColumns = GetEffectiveGridColumns(entry);
            int effectiveGridRows = GetEffectiveGridRows(entry);
            int effectiveCellWidth = GetEffectiveCellWidth(entry);
            int effectiveCellHeight = GetEffectiveCellHeight(entry);

            if (effectiveGridSizeMode == GridSizeMode.Manual)
            {
                columns = Mathf.Max(1, effectiveGridColumns);
                rows = Mathf.Max(1, effectiveGridRows);

                if (effectiveCellWidth > 0 && effectiveCellHeight > 0)
                {
                    cellWidth = effectiveCellWidth;
                    cellHeight = effectiveCellHeight;
                }
                else
                {
                    cellWidth = textureWidth / columns;
                    cellHeight = textureHeight / rows;
                }
            }
            else
            {
                float effectiveAlphaThreshold = GetEffectiveAlphaThreshold(entry);
                bool detectedFromTransparency = false;
                cellWidth = 0;
                cellHeight = 0;

                if (pixels != null && pixels.Length == textureWidth * textureHeight)
                {
                    detectedFromTransparency = DetectOptimalGridFromTransparency(
                        pixels,
                        textureWidth,
                        textureHeight,
                        effectiveAlphaThreshold,
                        out cellWidth,
                        out cellHeight
                    );
                }

                if (!detectedFromTransparency)
                {
                    // Use smarter fallback that prefers common sprite sizes over GCD
                    cellWidth = FindSmallestReasonableDivisor(textureWidth);
                    cellHeight = FindSmallestReasonableDivisor(textureHeight);

                    // If both return the full dimension, try using GCD as a fallback
                    if (cellWidth == textureWidth && cellHeight == textureHeight)
                    {
                        int gcd = CalculateGCD(textureWidth, textureHeight);
                        if (gcd >= 8 && gcd < textureWidth && gcd < textureHeight)
                        {
                            cellWidth = gcd;
                            cellHeight = gcd;
                        }
                    }
                }

                columns = Mathf.Max(1, textureWidth / cellWidth);
                rows = Mathf.Max(1, textureHeight / cellHeight);

                cellWidth = textureWidth / columns;
                cellHeight = textureHeight / rows;
            }
        }

        private static int CalculateGCD(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        /// <summary>
        /// Finds the smallest reasonable divisor for a dimension, preferring common sprite sizes.
        /// This produces better results than GCD for typical sprite sheets.
        /// </summary>
        /// <param name="dimension">The texture dimension to find a divisor for.</param>
        /// <returns>A divisor that produces at least 2 cells if possible, or the dimension itself.</returns>
        internal static int FindSmallestReasonableDivisor(int dimension)
        {
            // First try common sprite sizes that produce at least 2 cells
            for (int i = 0; i < CommonCellSizes.Length; ++i)
            {
                int size = CommonCellSizes[i];
                if (size >= 8 && size <= dimension && dimension % size == 0)
                {
                    int cellCount = dimension / size;
                    if (cellCount >= 2)
                    {
                        return size;
                    }
                }
            }

            // If no common size works, try all divisors starting from 8
            for (int divisor = 8; divisor <= dimension / 2; ++divisor)
            {
                if (dimension % divisor == 0)
                {
                    return divisor;
                }
            }

            // Last resort: return the full dimension (1 cell)
            return dimension;
        }

        /// <summary>
        /// Detects optimal grid dimensions by analyzing transparent boundaries in the texture.
        /// Uses a multi-candidate scoring approach that evaluates various cell sizes and picks
        /// the one with the highest transparency score along its grid boundaries.
        /// </summary>
        /// <param name="pixels">The texture pixel data in Color32 format.</param>
        /// <param name="textureWidth">Width of the texture in pixels.</param>
        /// <param name="textureHeight">Height of the texture in pixels.</param>
        /// <param name="alphaThreshold">Alpha value (0-1) below which a pixel is considered transparent.
        /// Must be less than 1.0; a value of 1.0 or greater returns false immediately.</param>
        /// <param name="cellWidth">Output: Detected cell width, or 0 if no clear grid was detected.</param>
        /// <param name="cellHeight">Output: Detected cell height, or 0 if no clear grid was detected.</param>
        /// <returns>True if a valid grid was detected, false otherwise.</returns>
        internal static bool DetectOptimalGridFromTransparency(
            Color32[] pixels,
            int textureWidth,
            int textureHeight,
            float alphaThreshold,
            out int cellWidth,
            out int cellHeight
        )
        {
            cellWidth = 0;
            cellHeight = 0;

            if (pixels == null || pixels.Length == 0)
            {
                return false;
            }

            if (textureWidth < 4 || textureHeight < 4)
            {
                return false;
            }

            if (pixels.Length != textureWidth * textureHeight)
            {
                return false;
            }

            if (alphaThreshold >= 1f)
            {
                return false;
            }

            byte alphaThresholdByte = (byte)(alphaThreshold * 255f);

            using PooledArray<int> columnTransparencyLease = SystemArrayPool<int>.Get(
                textureWidth,
                out int[] columnTransparencyCount
            );
            using PooledArray<int> rowTransparencyLease = SystemArrayPool<int>.Get(
                textureHeight,
                out int[] rowTransparencyCount
            );

            Array.Clear(columnTransparencyCount, 0, textureWidth);
            Array.Clear(rowTransparencyCount, 0, textureHeight);

            for (int y = 0; y < textureHeight; ++y)
            {
                int rowOffset = y * textureWidth;
                for (int x = 0; x < textureWidth; ++x)
                {
                    if (pixels[rowOffset + x].a <= alphaThresholdByte)
                    {
                        ++columnTransparencyCount[x];
                        ++rowTransparencyCount[y];
                    }
                }
            }

            using PooledResource<List<int>> widthCandidatesLease = Buffers<int>.List.Get(
                out List<int> widthCandidates
            );
            using PooledResource<List<int>> heightCandidatesLease = Buffers<int>.List.Get(
                out List<int> heightCandidates
            );

            GenerateCandidateCellSizes(textureWidth, widthCandidates);
            GenerateCandidateCellSizes(textureHeight, heightCandidates);

            int bestWidth = 0;
            int bestHeight = 0;
            float bestScore = -1f;

            for (int wi = 0; wi < widthCandidates.Count; ++wi)
            {
                int candidateWidth = widthCandidates[wi];
                float widthScore = ScoreCellSizeForDimension(
                    columnTransparencyCount,
                    textureWidth,
                    textureHeight,
                    candidateWidth
                );

                if (widthScore < MinimumBoundaryScore)
                {
                    continue;
                }

                for (int hi = 0; hi < heightCandidates.Count; ++hi)
                {
                    int candidateHeight = heightCandidates[hi];
                    float heightScore = ScoreCellSizeForDimension(
                        rowTransparencyCount,
                        textureHeight,
                        textureWidth,
                        candidateHeight
                    );

                    if (heightScore < MinimumBoundaryScore)
                    {
                        continue;
                    }

                    float combinedScore = (widthScore + heightScore) * 0.5f;

                    int columns = textureWidth / candidateWidth;
                    int rows = textureHeight / candidateHeight;

                    // Stronger bonus for producing multiple cells in both dimensions
                    if (columns >= 2 && rows >= 2)
                    {
                        combinedScore += 0.15f;
                    }
                    else if (columns >= 2 || rows >= 2)
                    {
                        combinedScore += 0.08f;
                    }

                    if (IsPowerOfTwo(candidateWidth) && IsPowerOfTwo(candidateHeight))
                    {
                        combinedScore += 0.05f;
                    }

                    if (candidateWidth == candidateHeight)
                    {
                        combinedScore += 0.02f;
                    }

                    // Bonus for reasonable cell counts (4-64 cells is a sweet spot)
                    int cellCount = columns * rows;
                    if (cellCount >= 4 && cellCount <= 64)
                    {
                        combinedScore += 0.03f;
                    }

                    if (combinedScore > bestScore)
                    {
                        bestScore = combinedScore;
                        bestWidth = candidateWidth;
                        bestHeight = candidateHeight;
                    }
                }
            }

            if (bestScore >= MinimumBoundaryScore && bestWidth > 0 && bestHeight > 0)
            {
                cellWidth = bestWidth;
                cellHeight = bestHeight;
                return true;
            }

            int detectedCellWidth = FindConsistentSpacingFromTransparency(
                columnTransparencyCount,
                textureWidth,
                textureHeight,
                minimumCellSize: 8
            );
            int detectedCellHeight = FindConsistentSpacingFromTransparency(
                rowTransparencyCount,
                textureHeight,
                textureWidth,
                minimumCellSize: 8
            );

            if (detectedCellWidth > 0 && detectedCellHeight > 0)
            {
                cellWidth = detectedCellWidth;
                cellHeight = detectedCellHeight;
                return true;
            }

            if (detectedCellWidth > 0 && detectedCellHeight <= 0)
            {
                cellWidth = detectedCellWidth;
                cellHeight = detectedCellWidth;
                if (textureHeight % cellHeight == 0)
                {
                    return true;
                }
            }

            if (detectedCellHeight > 0 && detectedCellWidth <= 0)
            {
                cellHeight = detectedCellHeight;
                cellWidth = detectedCellHeight;
                if (textureWidth % cellWidth == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Generates a list of candidate cell sizes for a given dimension.
        /// Includes common sprite sizes, power-of-2 sizes, and all divisors >= 8.
        /// </summary>
        private static void GenerateCandidateCellSizes(int dimension, List<int> candidates)
        {
            candidates.Clear();

            for (int i = 0; i < CommonCellSizes.Length; ++i)
            {
                int size = CommonCellSizes[i];
                if (size <= dimension && dimension % size == 0)
                {
                    AddUniqueSorted(candidates, size);
                }
            }

            for (int power = 3; power <= 10; ++power)
            {
                int size = 1 << power;
                if (size > dimension)
                {
                    break;
                }
                if (dimension % size == 0)
                {
                    AddUniqueSorted(candidates, size);
                }
            }

            for (int divisor = 2; divisor * divisor <= dimension; ++divisor)
            {
                if (dimension % divisor == 0)
                {
                    if (divisor >= 8)
                    {
                        AddUniqueSorted(candidates, divisor);
                    }
                    int complement = dimension / divisor;
                    if (complement >= 8 && complement != divisor)
                    {
                        AddUniqueSorted(candidates, complement);
                    }
                }
            }

            if (dimension >= 8)
            {
                AddUniqueSorted(candidates, dimension);
            }
        }

        /// <summary>
        /// Adds a value to a sorted list if not already present, maintaining sort order.
        /// </summary>
        private static void AddUniqueSorted(List<int> list, int value)
        {
            int insertIndex = 0;
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i] == value)
                {
                    return;
                }
                if (list[i] < value)
                {
                    insertIndex = i + 1;
                }
            }
            list.Insert(insertIndex, value);
        }

        /// <summary>
        /// Scores a candidate cell size based on how transparent the grid boundaries would be.
        /// Returns a value between 0.0 (poor match) and 1.0+ (excellent match with bonuses).
        /// </summary>
        private static float ScoreCellSizeForDimension(
            int[] transparencyCount,
            int dimension,
            int crossDimension,
            int cellSize
        )
        {
            if (cellSize <= 0 || dimension % cellSize != 0)
            {
                return 0f;
            }

            int cellCount = dimension / cellSize;
            if (cellCount < 1)
            {
                return 0f;
            }

            if (cellCount == 1)
            {
                return 0.6f;
            }

            int boundaryCount = cellCount - 1;
            float totalScore = 0f;

            for (int i = 1; i < cellCount; ++i)
            {
                int boundaryPosition = i * cellSize;

                if (boundaryPosition >= dimension)
                {
                    continue;
                }

                float boundaryScore = CalculateBoundaryTransparencyScore(
                    transparencyCount,
                    dimension,
                    crossDimension,
                    boundaryPosition
                );

                totalScore += boundaryScore;
            }

            return totalScore / boundaryCount;
        }

        /// <summary>
        /// Calculates a transparency score for a specific boundary position.
        /// Considers both the exact position and nearby positions (within 3 pixels)
        /// to handle slight misalignment in sprite sheets and thin transparent gutters.
        /// </summary>
        private static float CalculateBoundaryTransparencyScore(
            int[] transparencyCount,
            int dimension,
            int crossDimension,
            int position
        )
        {
            if (position < 0 || position >= dimension)
            {
                return 0f;
            }

            float primaryScore = transparencyCount[position] / (float)crossDimension;

            // Track max and average transparency in a 3-pixel radius
            float maxScore = primaryScore;
            float sumNearby = primaryScore;
            int nearbyCount = 1;

            // Expanded from +-2 to +-3 pixels to better detect thin transparent gutters
            for (int offset = -3; offset <= 3; ++offset)
            {
                if (offset == 0)
                {
                    continue;
                }

                int adjacentPos = position + offset;
                if (adjacentPos >= 0 && adjacentPos < dimension)
                {
                    float adjacentScore = transparencyCount[adjacentPos] / (float)crossDimension;
                    if (adjacentScore > maxScore)
                    {
                        maxScore = adjacentScore;
                    }
                    sumNearby += adjacentScore;
                    ++nearbyCount;
                }
            }

            float avgNearby = sumNearby / nearbyCount;

            // Weight max found transparency more heavily to detect thin gutters
            // maxScore helps when transparency is offset by a pixel
            // avgNearby helps when there's a wider transparent region
            // primaryScore gives slight preference to exact boundary position
            return maxScore * 0.6f + avgNearby * 0.25f + primaryScore * 0.15f;
        }

        /// <summary>
        /// Checks if a value is a power of two.
        /// </summary>
        private static bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }

        /// <summary>
        /// Fallback method that finds consistent spacing using the legacy approach.
        /// First identifies highly transparent boundaries, then looks for consistent gaps.
        /// </summary>
        private static int FindConsistentSpacingFromTransparency(
            int[] transparencyCount,
            int dimension,
            int crossDimension,
            int minimumCellSize
        )
        {
            // Lowered from 90% to 70% to handle sprite sheets with partial transparency
            float transparencyRequirement = 0.70f;
            int minTransparentPixels = (int)(crossDimension * transparencyRequirement);

            using PooledResource<List<int>> boundariesLease = Buffers<int>.List.Get(
                out List<int> boundaries
            );

            for (int i = 0; i < dimension; ++i)
            {
                if (transparencyCount[i] >= minTransparentPixels)
                {
                    boundaries.Add(i);
                }
            }

            // If insufficient boundaries found with 70%, try again with 50%
            if (boundaries.Count < 2)
            {
                transparencyRequirement = 0.50f;
                minTransparentPixels = (int)(crossDimension * transparencyRequirement);
                boundaries.Clear();

                for (int i = 0; i < dimension; ++i)
                {
                    if (transparencyCount[i] >= minTransparentPixels)
                    {
                        boundaries.Add(i);
                    }
                }
            }

            if (boundaries.Count < 2)
            {
                return 0;
            }

            return FindConsistentSpacing(boundaries, dimension, minimumCellSize);
        }

        /// <summary>
        /// Finds consistent spacing between transparent boundaries.
        /// Returns the most common spacing if it divides the dimension evenly.
        /// Uses a tolerance of +/- 2 pixels when searching for valid divisors.
        /// </summary>
        private static int FindConsistentSpacing(
            List<int> boundaries,
            int totalDimension,
            int minimumCellSize
        )
        {
            if (boundaries.Count < 2)
            {
                return 0;
            }

            using PooledResource<List<int>> gapsLease = Buffers<int>.List.Get(out List<int> gaps);

            int firstBoundary = boundaries[0];
            if (firstBoundary > 0 && firstBoundary >= minimumCellSize)
            {
                gaps.Add(firstBoundary);
            }

            for (int i = 1; i < boundaries.Count; ++i)
            {
                int gap = boundaries[i] - boundaries[i - 1];
                if (gap >= minimumCellSize)
                {
                    gaps.Add(gap);
                }
            }

            int lastBoundary = boundaries[boundaries.Count - 1];
            int trailingGap = totalDimension - lastBoundary - 1;
            if (trailingGap >= minimumCellSize)
            {
                gaps.Add(trailingGap);
            }

            if (gaps.Count < 2)
            {
                return 0;
            }

            using PooledResource<Dictionary<int, int>> gapCountsLease = DictionaryBuffer<
                int,
                int
            >.Dictionary.Get(out Dictionary<int, int> gapCounts);

            for (int i = 0; i < gaps.Count; ++i)
            {
                int gap = gaps[i];
                if (gapCounts.TryGetValue(gap, out int count))
                {
                    gapCounts[gap] = count + 1;
                }
                else
                {
                    gapCounts[gap] = 1;
                }
            }

            int mostCommonGap = 0;
            int mostCommonCount = 0;

            using PooledResource<List<int>> gapKeysLease = Buffers<int>.List.Get(
                out List<int> gapKeys
            );
            using (Dictionary<int, int>.Enumerator enumerator = gapCounts.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    gapKeys.Add(enumerator.Current.Key);
                }
            }

            for (int i = 0; i < gapKeys.Count; ++i)
            {
                int gap = gapKeys[i];
                int count = gapCounts[gap];
                if (count > mostCommonCount)
                {
                    mostCommonCount = count;
                    mostCommonGap = gap;
                }
            }

            if (mostCommonGap > 0 && totalDimension % mostCommonGap == 0)
            {
                int expectedColumns = totalDimension / mostCommonGap;
                if (expectedColumns >= 2 && mostCommonCount >= expectedColumns - 1)
                {
                    return mostCommonGap;
                }
            }

            for (int tolerance = 1; tolerance <= 2; ++tolerance)
            {
                int candidateLow = mostCommonGap - tolerance;
                if (candidateLow >= minimumCellSize && totalDimension % candidateLow == 0)
                {
                    int expectedColumns = totalDimension / candidateLow;
                    if (expectedColumns >= 2)
                    {
                        return candidateLow;
                    }
                }

                int candidateHigh = mostCommonGap + tolerance;
                if (candidateHigh >= minimumCellSize && totalDimension % candidateHigh == 0)
                {
                    int expectedColumns = totalDimension / candidateHigh;
                    if (expectedColumns >= 2)
                    {
                        return candidateHigh;
                    }
                }
            }

            return 0;
        }

        internal ExtractionMode GetEffectiveExtractionMode(SpriteSheetEntry entry)
        {
            if (
                entry == null
                || entry._useGlobalSettings
                || !entry._extractionModeOverride.HasValue
            )
            {
                return _extractionMode;
            }
            return entry._extractionModeOverride.Value;
        }

        internal GridSizeMode GetEffectiveGridSizeMode(SpriteSheetEntry entry)
        {
            if (entry == null || entry._useGlobalSettings || !entry._gridSizeModeOverride.HasValue)
            {
                return _gridSizeMode;
            }
            return entry._gridSizeModeOverride.Value;
        }

        internal int GetEffectiveGridColumns(SpriteSheetEntry entry)
        {
            if (entry == null || entry._useGlobalSettings || !entry._gridColumnsOverride.HasValue)
            {
                return _gridColumns;
            }
            return entry._gridColumnsOverride.Value;
        }

        internal int GetEffectiveGridRows(SpriteSheetEntry entry)
        {
            if (entry == null || entry._useGlobalSettings || !entry._gridRowsOverride.HasValue)
            {
                return _gridRows;
            }
            return entry._gridRowsOverride.Value;
        }

        internal int GetEffectiveCellWidth(SpriteSheetEntry entry)
        {
            if (entry == null || entry._useGlobalSettings || !entry._cellWidthOverride.HasValue)
            {
                return _cellWidth;
            }
            return entry._cellWidthOverride.Value;
        }

        internal int GetEffectiveCellHeight(SpriteSheetEntry entry)
        {
            if (entry == null || entry._useGlobalSettings || !entry._cellHeightOverride.HasValue)
            {
                return _cellHeight;
            }
            return entry._cellHeightOverride.Value;
        }

        internal int GetEffectivePaddingLeft(SpriteSheetEntry entry)
        {
            if (entry == null || entry._useGlobalSettings || !entry._paddingLeftOverride.HasValue)
            {
                return _paddingLeft;
            }
            return entry._paddingLeftOverride.Value;
        }

        internal int GetEffectivePaddingRight(SpriteSheetEntry entry)
        {
            if (entry == null || entry._useGlobalSettings || !entry._paddingRightOverride.HasValue)
            {
                return _paddingRight;
            }
            return entry._paddingRightOverride.Value;
        }

        internal int GetEffectivePaddingTop(SpriteSheetEntry entry)
        {
            if (entry == null || entry._useGlobalSettings || !entry._paddingTopOverride.HasValue)
            {
                return _paddingTop;
            }
            return entry._paddingTopOverride.Value;
        }

        internal int GetEffectivePaddingBottom(SpriteSheetEntry entry)
        {
            if (entry == null || entry._useGlobalSettings || !entry._paddingBottomOverride.HasValue)
            {
                return _paddingBottom;
            }
            return entry._paddingBottomOverride.Value;
        }

        internal float GetEffectiveAlphaThreshold(SpriteSheetEntry entry)
        {
            if (
                entry == null
                || entry._useGlobalSettings
                || !entry._alphaThresholdOverride.HasValue
            )
            {
                return _alphaThreshold;
            }
            return entry._alphaThresholdOverride.Value;
        }

        /// <summary>
        /// Returns whether the grid overlay should be shown for the given entry.
        /// Uses per-sheet override if set, otherwise falls back to global setting.
        /// </summary>
        /// <param name="entry">The sprite sheet entry to check, or null to use global setting.</param>
        /// <returns>True if the grid overlay should be displayed for this entry.</returns>
        internal bool GetEffectiveShowGridOverlay(SpriteSheetEntry entry)
        {
            if (
                entry == null
                || entry._useGlobalSettings
                || !entry._showGridOverlayOverride.HasValue
            )
            {
                return _showGridOverlay;
            }
            return entry._showGridOverlayOverride.Value;
        }

        /// <summary>
        /// Checks if any discovered sheet uses grid-based extraction mode.
        /// Returns true if the global mode is grid-based, or if any per-sheet override is grid-based.
        /// </summary>
        /// <returns>True if any sheet uses GridBased or PaddedGrid extraction mode.</returns>
        internal bool AnySheetUsesGridBasedExtraction()
        {
            bool globalIsGridBased =
                _extractionMode == ExtractionMode.GridBased
                || _extractionMode == ExtractionMode.PaddedGrid;

            if (globalIsGridBased)
            {
                return true;
            }

            if (_discoveredSheets == null || _discoveredSheets.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < _discoveredSheets.Count; ++i)
            {
                SpriteSheetEntry entry = _discoveredSheets[i];
                if (entry == null || entry._useGlobalSettings)
                {
                    continue;
                }

                ExtractionMode effectiveMode = GetEffectiveExtractionMode(entry);
                if (
                    effectiveMode == ExtractionMode.GridBased
                    || effectiveMode == ExtractionMode.PaddedGrid
                )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Calculates the actual rect where the texture is drawn within the preview area.
        /// This accounts for ScaleMode.ScaleToFit centering behavior.
        /// </summary>
        internal Rect CalculateTextureRectWithinPreview(
            Rect previewRect,
            int textureWidth,
            int textureHeight,
            float scale
        )
        {
            // Defensive check for invalid inputs - fall back to full preview rect
            if (textureWidth <= 0 || textureHeight <= 0 || scale <= 0f)
            {
                return previewRect;
            }

            float scaledWidth = textureWidth * scale;
            float scaledHeight = textureHeight * scale;
            float offsetX = (previewRect.width - scaledWidth) * 0.5f;
            float offsetY = (previewRect.height - scaledHeight) * 0.5f;
            return new Rect(
                previewRect.x + offsetX,
                previewRect.y + offsetY,
                scaledWidth,
                scaledHeight
            );
        }

        internal void DrawGridOverlay(
            Rect previewRect,
            int textureWidth,
            int textureHeight,
            float scale
        )
        {
            DrawGridOverlay(previewRect, textureWidth, textureHeight, scale, null);
        }

        internal void DrawGridOverlay(
            Rect previewRect,
            int textureWidth,
            int textureHeight,
            float scale,
            SpriteSheetEntry entry
        )
        {
            int columns;
            int rows;
            int cellWidth;
            int cellHeight;

            CalculateGridDimensions(
                textureWidth,
                textureHeight,
                entry,
                out columns,
                out rows,
                out cellWidth,
                out cellHeight
            );

            // Calculate actual texture rect to account for ScaleToFit centering
            Rect textureRect = CalculateTextureRectWithinPreview(
                previewRect,
                textureWidth,
                textureHeight,
                scale
            );

            float scaledCellWidth = cellWidth * scale;
            float scaledCellHeight = cellHeight * scale;

            Color previousColor = GUI.color;
            GUI.color = _gridLineColor;

            try
            {
                for (int col = 1; col < columns; ++col)
                {
                    float x = textureRect.x + col * scaledCellWidth;
                    Rect lineRect = new Rect(x, textureRect.y, 1, textureRect.height);
                    EditorGUI.DrawRect(lineRect, _gridLineColor);
                }

                for (int row = 1; row < rows; ++row)
                {
                    float y = textureRect.y + row * scaledCellHeight;
                    Rect lineRect = new Rect(textureRect.x, y, textureRect.width, 1);
                    EditorGUI.DrawRect(lineRect, _gridLineColor);
                }
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        private void DrawSpriteRectsOverlay(
            Rect previewRect,
            int textureWidth,
            int textureHeight,
            float scale,
            SpriteSheetEntry entry
        )
        {
            if (entry._sprites == null || entry._sprites.Count == 0)
            {
                return;
            }

            // Calculate actual texture rect to account for ScaleToFit centering
            Rect textureRect = CalculateTextureRectWithinPreview(
                previewRect,
                textureWidth,
                textureHeight,
                scale
            );

            for (int i = 0; i < entry._sprites.Count; ++i)
            {
                SpriteEntryData sprite = entry._sprites[i];
                if (sprite == null)
                {
                    continue;
                }

                float rectX = textureRect.x + sprite._rect.x * scale;
                float rectY =
                    textureRect.y + (textureHeight - sprite._rect.y - sprite._rect.height) * scale;
                float rectWidth = sprite._rect.width * scale;
                float rectHeight = sprite._rect.height * scale;

                EditorGUI.DrawRect(new Rect(rectX, rectY, rectWidth, 1), _gridLineColor);
                EditorGUI.DrawRect(
                    new Rect(rectX, rectY + rectHeight - 1, rectWidth, 1),
                    _gridLineColor
                );
                EditorGUI.DrawRect(new Rect(rectX, rectY, 1, rectHeight), _gridLineColor);
                EditorGUI.DrawRect(
                    new Rect(rectX + rectWidth - 1, rectY, 1, rectHeight),
                    _gridLineColor
                );
            }
        }

        private List<SpriteEntryData> GetSortedSprites(List<SpriteEntryData> sprites)
        {
            bool needsRefresh =
                _cachedSortedSprites == null
                || _lastSpritesSource != sprites
                || _lastSortMode != _sortMode;

            if (!needsRefresh)
            {
                return _cachedSortedSprites;
            }

            _cachedSortedSprites ??= new List<SpriteEntryData>();
            _cachedSortedSprites.Clear();

            for (int i = 0; i < sprites.Count; ++i)
            {
                _cachedSortedSprites.Add(sprites[i]);
            }

            switch (_sortMode)
            {
                case SortMode.ByName:
                    _cachedSortedSprites.Sort(
                        (a, b) =>
                            string.Compare(
                                a._originalName,
                                b._originalName,
                                StringComparison.Ordinal
                            )
                    );
                    break;
                case SortMode.ByPositionTopLeft:
                    _cachedSortedSprites.Sort(
                        (a, b) =>
                        {
                            int yCompare = b._rect.y.CompareTo(a._rect.y);
                            return yCompare != 0 ? yCompare : a._rect.x.CompareTo(b._rect.x);
                        }
                    );
                    break;
                case SortMode.ByPositionBottomLeft:
                    _cachedSortedSprites.Sort(
                        (a, b) =>
                        {
                            int yCompare = a._rect.y.CompareTo(b._rect.y);
                            return yCompare != 0 ? yCompare : a._rect.x.CompareTo(b._rect.x);
                        }
                    );
                    break;
                case SortMode.Reversed:
                    _cachedSortedSprites.Reverse();
                    break;
                case SortMode.Original:
                default:
                    break;
            }

            _lastSpritesSource = sprites;
            _lastSortMode = _sortMode;
            return _cachedSortedSprites;
        }

        private void DrawSpriteEntry(SpriteSheetEntry sheet, SpriteEntryData sprite, int index)
        {
            int previewSize = GetPreviewSize(sprite);

            using (new EditorGUILayout.HorizontalScope())
            {
                sprite._isSelected = EditorGUILayout.Toggle(
                    sprite._isSelected,
                    GUILayout.Width(20)
                );

                if (sprite._previewTexture != null)
                {
                    GUILayout.Label(
                        sprite._previewTexture,
                        GUILayout.Width(previewSize),
                        GUILayout.Height(previewSize)
                    );
                }
                else
                {
                    using (
                        new EditorGUILayout.VerticalScope(
                            GUILayout.Width(previewSize),
                            GUILayout.Height(previewSize)
                        )
                    )
                    {
                        GUIStyle placeholderStyle = new(EditorStyles.helpBox)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = previewSize / 2,
                        };
                        GUILayout.Label(
                            "?",
                            placeholderStyle,
                            GUILayout.Width(previewSize),
                            GUILayout.Height(previewSize)
                        );
                    }
                }

                string prefix = string.IsNullOrWhiteSpace(_namingPrefix)
                    ? Path.GetFileNameWithoutExtension(sheet._assetPath)
                    : _namingPrefix;
                string previewName = $"{prefix}_{index:D3}.png";

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(
                        $"Original: {sprite._originalName}",
                        EditorStyles.miniLabel
                    );
                    EditorGUILayout.LabelField($"Output: {previewName}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(
                        $"Size: {(int)sprite._rect.width}x{(int)sprite._rect.height}",
                        EditorStyles.miniLabel
                    );
                    EditorGUILayout.LabelField(
                        $"Rect: ({(int)sprite._rect.x}, {(int)sprite._rect.y})",
                        EditorStyles.miniLabel
                    );
                    string previewStatus =
                        sprite._previewTexture != null
                            ? $"Preview: {sprite._previewTexture.width}x{sprite._previewTexture.height}"
                            : "Preview: MISSING";
                    EditorGUILayout.LabelField(previewStatus, EditorStyles.miniLabel);
                }
            }
        }

        internal int GetPreviewSize(SpriteEntryData sprite)
        {
            switch (_previewSizeMode)
            {
                case PreviewSizeMode.Size24:
                    return 24;
                case PreviewSizeMode.Size64:
                    return 64;
                case PreviewSizeMode.RealSize:
                    if (sprite != null)
                    {
                        int maxDim = Mathf.Max((int)sprite._rect.width, (int)sprite._rect.height);
                        return Mathf.Max(16, Mathf.Min(maxDim, 128));
                    }
                    return 32;
                case PreviewSizeMode.Size32:
                default:
                    return 32;
            }
        }

        private void DrawExtractionSection()
        {
            if (_discoveredSheets == null || _discoveredSheets.Count == 0)
            {
                return;
            }

            int selectedSheetCount = 0;
            int selectedSpriteCount = 0;
            for (int i = 0; i < _discoveredSheets.Count; ++i)
            {
                SpriteSheetEntry entry = _discoveredSheets[i];
                if (!entry._isSelected)
                {
                    continue;
                }
                ++selectedSheetCount;
                if (entry._sprites != null)
                {
                    for (int j = 0; j < entry._sprites.Count; ++j)
                    {
                        if (entry._sprites[j]._isSelected)
                        {
                            ++selectedSpriteCount;
                        }
                    }
                }
            }

            EditorGUILayout.LabelField(
                $"Selected: {selectedSheetCount} sheet(s), {selectedSpriteCount} sprite(s)",
                EditorStyles.boldLabel
            );

            if (_outputDirectory == null)
            {
                EditorGUILayout.HelpBox(
                    "Output directory is required for extraction.",
                    MessageType.Info
                );
                return;
            }

            string outputPath = AssetDatabase.GetAssetPath(_outputDirectory);
            if (!AssetDatabase.IsValidFolder(outputPath))
            {
                EditorGUILayout.HelpBox(
                    "Selected output directory is not valid.",
                    MessageType.Error
                );
                return;
            }

            using (new EditorGUI.DisabledScope(selectedSpriteCount == 0))
            {
                string buttonLabel = _dryRun
                    ? $"Dry Run: Preview {selectedSpriteCount} Sprite(s)"
                    : $"Extract {selectedSpriteCount} Sprite(s)";

                if (GUILayout.Button(buttonLabel))
                {
                    ExtractSelectedSprites();
                }
            }

            if (_lastExtractedCount > 0 || _lastSkippedCount > 0 || _lastErrorCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Last extraction: {_lastExtractedCount} extracted, {_lastSkippedCount} skipped, {_lastErrorCount} errors",
                    _lastErrorCount > 0 ? MessageType.Warning : MessageType.Info
                );
            }
        }

        private void DrawDangerZone()
        {
            using (new GUILayout.VerticalScope("box"))
            {
                Color prev = GUI.color;
                GUI.color = Color.red;
                _dangerZoneFoldout = EditorGUILayout.Foldout(
                    _dangerZoneFoldout,
                    "Danger Zone: Reference Replacement",
                    true
                );
                GUI.color = prev;

                if (!_dangerZoneFoldout)
                {
                    return;
                }

                EditorGUILayout.HelpBox(
                    "This will scan prefabs and scenes for references to original sprite sheet sprites "
                        + "and replace them with references to extracted individual sprites. This is potentially "
                        + "destructive. Ensure you have backups/version control.",
                    MessageType.Error
                );

                _ackDanger = EditorGUILayout.ToggleLeft(
                    "I understand the risks and want to proceed.",
                    _ackDanger
                );

                using (new EditorGUI.DisabledScope(!_ackDanger || _discoveredSheets == null))
                {
                    if (GUILayout.Button("Replace Sprite References With Extracted Versions"))
                    {
                        ReplaceSpriteReferences();
                    }
                }
            }
        }

        internal void DiscoverSpriteSheets()
        {
            CleanupPreviewTextures();

            _discoveredSheets ??= new List<SpriteSheetEntry>();
            _discoveredSheets.Clear();

            // Compile regex from _spriteNameRegex field at the start
            _regex = null;
            if (!string.IsNullOrWhiteSpace(_spriteNameRegex))
            {
                try
                {
                    _regex = new Regex(
                        _spriteNameRegex,
                        RegexOptions.Compiled | RegexOptions.CultureInvariant
                    );
                }
                catch (ArgumentException ex)
                {
                    this.LogWarn($"Invalid regex '{_spriteNameRegex}': {ex.Message}");
                    _regexError = ex.Message;
                    Repaint();
                    return;
                }
            }

            if (_inputDirectories == null || _inputDirectories.Count == 0)
            {
                this.LogWarn($"No input directories selected.");
                Repaint();
                return;
            }

            using PooledResource<HashSet<string>> seenLease = SetBuffers<string>
                .GetHashSetPool(StringComparer.OrdinalIgnoreCase)
                .Get(out HashSet<string> seen);

            for (int dirIndex = 0; dirIndex < _inputDirectories.Count; ++dirIndex)
            {
                Object maybeDirectory = _inputDirectories[dirIndex];
                if (maybeDirectory == null)
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(maybeDirectory);
                if (!AssetDatabase.IsValidFolder(assetPath))
                {
                    this.LogWarn($"Skipping invalid path: {assetPath}");
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { assetPath });
                for (int guidIndex = 0; guidIndex < guids.Length; ++guidIndex)
                {
                    string file = AssetDatabase.GUIDToAssetPath(guids[guidIndex]);
                    if (string.IsNullOrEmpty(file))
                    {
                        continue;
                    }

                    bool hasValidExtension = false;
                    for (int extIndex = 0; extIndex < ImageFileExtensions.Length; ++extIndex)
                    {
                        if (
                            file.EndsWith(
                                ImageFileExtensions[extIndex],
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            hasValidExtension = true;
                            break;
                        }
                    }
                    if (!hasValidExtension)
                    {
                        continue;
                    }

                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (_regex != null && !_regex.IsMatch(fileName))
                    {
                        continue;
                    }

                    if (!seen.Add(file))
                    {
                        continue;
                    }

                    if (
                        AssetImporter.GetAtPath(file)
                        is not TextureImporter { textureType: TextureImporterType.Sprite } importer
                    )
                    {
                        continue;
                    }

                    SpriteSheetEntry entry = CreateSpriteSheetEntry(file, importer);
                    if (entry != null)
                    {
                        _discoveredSheets.Add(entry);
                    }
                }
            }

            GenerateAllPreviewTexturesInBatch(_discoveredSheets);

            this.Log($"Discovered {_discoveredSheets.Count} sprite sheet(s).");
            Repaint();
        }

        private SpriteSheetEntry CreateSpriteSheetEntry(string assetPath, TextureImporter importer)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                return null;
            }

            SpriteSheetEntry entry = new()
            {
                _assetPath = assetPath,
                _texture = texture,
                _importer = importer,
                _importMode = importer.spriteImportMode,
                _isExpanded = false,
                _isSelected = true,
                _sprites = new List<SpriteEntryData>(),
            };

            switch (_extractionMode)
            {
                case ExtractionMode.GridBased:
                    PopulateSpritesFromGrid(entry, texture);
                    break;
                case ExtractionMode.PaddedGrid:
                    PopulateSpritesFromPaddedGrid(entry, texture);
                    break;
                case ExtractionMode.AlphaDetection:
                    PopulateSpritesFromAlphaDetection(entry, texture);
                    break;
                case ExtractionMode.FromMetadata:
                default:
                    PopulateSpritesFromMetadata(entry, assetPath, importer);
                    break;
            }

            return entry;
        }

        /// <summary>
        /// Generates preview thumbnails for all sprites in the specified sprite sheet entry.
        /// </summary>
        /// <param name="entry">The sprite sheet entry containing sprites to generate previews for.</param>
        /// <remarks>
        /// Temporarily enables Read/Write on the texture if needed, generates scaled preview
        /// textures for each sprite, then restores the original readable state. Compressed or
        /// unsupported texture formats will skip preview generation with a warning.
        /// </remarks>
        private void GeneratePreviewTexturesForEntry(SpriteSheetEntry entry)
        {
            if (entry?._sprites == null || entry._sprites.Count == 0 || entry._texture == null)
            {
                return;
            }

            bool wasReadable = entry._importer.isReadable;
            bool needsReimport = false;

            try
            {
                if (!wasReadable)
                {
                    entry._importer.isReadable = true;
                    entry._importer.SaveAndReimport();
                    needsReimport = true;
                    entry._texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry._assetPath);
                    if (entry._texture == null)
                    {
                        return;
                    }
                }

                if (!IsTextureFormatSupportedForGetPixels(entry._texture.format))
                {
                    this.LogWarn(
                        $"Texture format '{entry._texture.format}' does not support GetPixels32 for {entry._assetPath}. Preview generation skipped."
                    );
                    return;
                }

                Color32[] sourcePixels = entry._texture.GetPixels32();
                int sourceWidth = entry._texture.width;
                int sourceHeight = entry._texture.height;

                for (int i = 0; i < entry._sprites.Count; ++i)
                {
                    SpriteEntryData sprite = entry._sprites[i];
                    if (sprite == null)
                    {
                        continue;
                    }

                    // Keep old texture until new one is ready to avoid grey question marks
                    Texture2D oldTexture = sprite._previewTexture;
                    Texture2D preview = GenerateSinglePreviewTexture(
                        sourcePixels,
                        sourceWidth,
                        sourceHeight,
                        sprite
                    );
                    sprite._previewTexture = preview;

                    // Destroy old texture after new one is assigned
                    if (oldTexture != null)
                    {
                        DestroyImmediate(oldTexture);
                    }
                }
            }
            catch (Exception ex)
            {
                this.LogWarn($"Failed to generate preview textures for {entry._assetPath}.", ex);
            }
            finally
            {
                if (needsReimport)
                {
                    entry._importer.isReadable = false;
                    entry._importer.SaveAndReimport();
                    entry._texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry._assetPath);
                }
            }
        }

        /// <summary>
        /// Generates preview textures for all sprite sheet entries in a batch operation.
        /// </summary>
        /// <param name="entries">The list of sprite sheet entries to generate previews for.</param>
        /// <remarks>
        /// This method batches texture readability changes to minimize reimport operations.
        /// It uses <see cref="AssetDatabase.StartAssetEditing"/> and <see cref="AssetDatabase.StopAssetEditing"/>
        /// to batch all import changes together, improving performance when processing multiple textures.
        /// </remarks>
        internal void GenerateAllPreviewTexturesInBatch(List<SpriteSheetEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            WallstopGenericPool<Dictionary<string, bool>> originalReadablePool = DictionaryBuffer<
                string,
                bool
            >.GetDictionaryPool(StringComparer.OrdinalIgnoreCase);
            using PooledResource<Dictionary<string, bool>> originalReadableLease =
                originalReadablePool.Get(out Dictionary<string, bool> originalReadable);

            using PooledResource<List<SpriteSheetEntry>> needsReadableLease =
                Buffers<SpriteSheetEntry>.List.Get(out List<SpriteSheetEntry> needsReadable);

            for (int i = 0; i < entries.Count; ++i)
            {
                SpriteSheetEntry entry = entries[i];
                if (entry?._sprites == null || entry._sprites.Count == 0 || entry._texture == null)
                {
                    continue;
                }

                if (entry._importer == null)
                {
                    continue;
                }

                originalReadable[entry._assetPath] = entry._importer.isReadable;

                if (!entry._importer.isReadable)
                {
                    needsReadable.Add(entry);
                }
            }

            if (needsReadable.Count > 0)
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    for (int i = 0; i < needsReadable.Count; ++i)
                    {
                        SpriteSheetEntry entry = needsReadable[i];
                        entry._importer.isReadable = true;
                        entry._importer.SaveAndReimport();
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                for (int i = 0; i < needsReadable.Count; ++i)
                {
                    SpriteSheetEntry entry = needsReadable[i];
                    entry._texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry._assetPath);
                    if (entry._texture == null)
                    {
                        this.LogWarn(
                            $"Failed to reload texture after making readable: {entry._assetPath}"
                        );
                        continue;
                    }
                }
            }

            for (int i = 0; i < entries.Count; ++i)
            {
                SpriteSheetEntry entry = entries[i];
                if (entry?._sprites == null || entry._sprites.Count == 0 || entry._texture == null)
                {
                    continue;
                }

                if (!IsTextureFormatSupportedForGetPixels(entry._texture.format))
                {
                    this.LogWarn(
                        $"Texture format '{entry._texture.format}' does not support GetPixels32 for {entry._assetPath}. Preview generation skipped."
                    );
                    continue;
                }

                try
                {
                    Color32[] sourcePixels = entry._texture.GetPixels32();
                    int sourceWidth = entry._texture.width;
                    int sourceHeight = entry._texture.height;

                    for (int j = 0; j < entry._sprites.Count; ++j)
                    {
                        SpriteEntryData sprite = entry._sprites[j];
                        if (sprite == null)
                        {
                            continue;
                        }

                        // Keep old texture until new one is ready to avoid grey question marks
                        Texture2D oldTexture = sprite._previewTexture;
                        Texture2D preview = GenerateSinglePreviewTexture(
                            sourcePixels,
                            sourceWidth,
                            sourceHeight,
                            sprite
                        );
                        sprite._previewTexture = preview;

                        // Destroy old texture after new one is assigned
                        if (oldTexture != null)
                        {
                            DestroyImmediate(oldTexture);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.LogWarn(
                        $"Failed to generate preview textures for {entry._assetPath}.",
                        ex
                    );
                }
            }

            using PooledResource<List<SpriteSheetEntry>> needsRestoreLease =
                Buffers<SpriteSheetEntry>.List.Get(out List<SpriteSheetEntry> needsRestore);

            for (int i = 0; i < entries.Count; ++i)
            {
                SpriteSheetEntry entry = entries[i];
                if (entry?._importer == null)
                {
                    continue;
                }

                if (
                    originalReadable.TryGetValue(entry._assetPath, out bool wasReadable)
                    && !wasReadable
                )
                {
                    needsRestore.Add(entry);
                }
            }

            if (needsRestore.Count > 0)
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    for (int i = 0; i < needsRestore.Count; ++i)
                    {
                        SpriteSheetEntry entry = needsRestore[i];
                        entry._importer.isReadable = false;
                        entry._importer.SaveAndReimport();
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                for (int i = 0; i < needsRestore.Count; ++i)
                {
                    SpriteSheetEntry entry = needsRestore[i];
                    entry._texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry._assetPath);
                }

                // Schedule repaint after asset database operations complete
                SpriteSheetExtractor windowRef = this;
                EditorApplication.delayCall += () =>
                {
                    // Use implicit bool for Unity object null check
                    if (windowRef)
                    {
                        windowRef.Repaint();
                    }
                };
            }
        }

        /// <summary>
        /// Creates a single preview texture for a sprite by extracting and scaling pixels from the source texture.
        /// </summary>
        /// <param name="sourcePixels">The pixel data from the source texture.</param>
        /// <param name="sourceWidth">The width of the source texture in pixels.</param>
        /// <param name="sourceHeight">The height of the source texture in pixels.</param>
        /// <param name="sprite">The sprite entry containing rect and size information.</param>
        /// <returns>A scaled preview Texture2D, or null if the sprite is invalid or has zero dimensions.</returns>
        /// <remarks>
        /// Uses nearest-neighbor sampling for pixel-perfect scaling. The preview size is determined
        /// by the current <see cref="_previewSizeMode"/> setting. Uses pooled arrays to minimize
        /// GC allocations during preview generation.
        /// </remarks>
        private Texture2D GenerateSinglePreviewTexture(
            Color32[] sourcePixels,
            int sourceWidth,
            int sourceHeight,
            SpriteEntryData sprite
        )
        {
            if (sprite == null)
            {
                this.LogWarn($"GenerateSinglePreviewTexture: sprite is null, skipping preview.");
                return null;
            }

            int spriteX = Mathf.FloorToInt(sprite._rect.x);
            int spriteY = Mathf.FloorToInt(sprite._rect.y);
            int spriteWidth = Mathf.FloorToInt(sprite._rect.width);
            int spriteHeight = Mathf.FloorToInt(sprite._rect.height);

            if (spriteWidth <= 0 || spriteHeight <= 0)
            {
                this.LogWarn(
                    $"GenerateSinglePreviewTexture: Invalid dimensions for '{sprite._originalName}' - width={spriteWidth}, height={spriteHeight}, rect=({sprite._rect.x}, {sprite._rect.y}, {sprite._rect.width}, {sprite._rect.height})"
                );
                return null;
            }

            spriteX = Mathf.Clamp(spriteX, 0, sourceWidth - 1);
            spriteY = Mathf.Clamp(spriteY, 0, sourceHeight - 1);
            spriteWidth = Mathf.Clamp(spriteWidth, 1, sourceWidth - spriteX);
            spriteHeight = Mathf.Clamp(spriteHeight, 1, sourceHeight - spriteY);

            int previewSize = GetPreviewSize(sprite);
            int targetWidth;
            int targetHeight;

            if (_previewSizeMode == PreviewSizeMode.RealSize)
            {
                int maxDim = Mathf.Max(spriteWidth, spriteHeight);
                float scale = Mathf.Min(1f, (float)previewSize / maxDim);
                targetWidth = Mathf.Max(1, Mathf.RoundToInt(spriteWidth * scale));
                targetHeight = Mathf.Max(1, Mathf.RoundToInt(spriteHeight * scale));
            }
            else
            {
                if (spriteWidth >= spriteHeight)
                {
                    targetWidth = previewSize;
                    targetHeight = Mathf.Max(
                        1,
                        Mathf.RoundToInt(previewSize * (float)spriteHeight / spriteWidth)
                    );
                }
                else
                {
                    targetHeight = previewSize;
                    targetWidth = Mathf.Max(
                        1,
                        Mathf.RoundToInt(previewSize * (float)spriteWidth / spriteHeight)
                    );
                }
            }

            int pixelCount = targetWidth * targetHeight;
            // Note: Cannot use pooled arrays here because SetPixels32 requires the array length
            // to exactly match the texture dimensions, but ArrayPool returns arrays that may be
            // larger than requested.
            Color32[] destPixels = new Color32[pixelCount];

            float xRatio = (float)spriteWidth / targetWidth;
            float yRatio = (float)spriteHeight / targetHeight;

            for (int destY = 0; destY < targetHeight; ++destY)
            {
                int srcY = spriteY + Mathf.FloorToInt(destY * yRatio);
                srcY = Mathf.Clamp(srcY, 0, sourceHeight - 1);
                int destRowStart = destY * targetWidth;
                int srcRowStart = srcY * sourceWidth;

                for (int destX = 0; destX < targetWidth; ++destX)
                {
                    int srcX = spriteX + Mathf.FloorToInt(destX * xRatio);
                    srcX = Mathf.Clamp(srcX, 0, sourceWidth - 1);
                    destPixels[destRowStart + destX] = sourcePixels[srcRowStart + srcX];
                }
            }

            Texture2D preview = null;
            try
            {
                preview = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
                preview.SetPixels32(destPixels);
                preview.Apply();
                return preview;
            }
            catch
            {
                if (preview != null)
                {
                    DestroyImmediate(preview);
                }
                throw;
            }
        }

        /// <summary>
        /// Regenerates preview textures for all discovered sprite sheets.
        /// </summary>
        /// <remarks>
        /// Called when preview size mode or extraction mode changes to update all existing
        /// preview thumbnails. Uses batch processing to minimize reimport operations.
        /// </remarks>
        internal void RegenerateAllPreviewTextures()
        {
            _previewRegenerationScheduled = false;

            if (!this || _discoveredSheets == null || _discoveredSheets.Count == 0)
            {
                return;
            }

            _regenerationInProgress = true;

            // Collect old textures to destroy after new ones are generated
            using PooledResource<List<Texture2D>> oldTexturesLease = Buffers<Texture2D>.List.Get(
                out List<Texture2D> oldTextures
            );

            for (int i = 0; i < _discoveredSheets.Count; ++i)
            {
                SpriteSheetEntry entry = _discoveredSheets[i];
                if (entry == null)
                {
                    continue;
                }

                if (entry._sprites != null)
                {
                    for (int j = 0; j < entry._sprites.Count; ++j)
                    {
                        SpriteEntryData sprite = entry._sprites[j];
                        if (sprite != null && sprite._previewTexture != null)
                        {
                            oldTextures.Add(sprite._previewTexture);
                            sprite._previewTexture = null;
                        }
                    }
                }

                RepopulateSpritesForEntry(entry);
            }

            GenerateAllPreviewTexturesInBatch(_discoveredSheets);

            // Destroy old textures after new ones are generated
            for (int i = 0; i < oldTextures.Count; ++i)
            {
                Texture2D oldTexture = oldTextures[i];
                if (oldTexture != null)
                {
                    DestroyImmediate(oldTexture);
                }
            }

            _regenerationInProgress = false;
            Repaint();
        }

        private void PopulateSpritesFromMetadata(
            SpriteSheetEntry entry,
            string assetPath,
            TextureImporter importer
        )
        {
            if (importer.spriteImportMode == SpriteImportMode.Multiple)
            {
                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                int spriteIndex = 0;
                for (int i = 0; i < allAssets.Length; ++i)
                {
                    if (allAssets[i] is not Sprite sprite)
                    {
                        continue;
                    }

                    SpriteEntryData spriteEntry = new()
                    {
                        _originalName = sprite.name,
                        _outputName = sprite.name,
                        _rect = sprite.rect,
                        _pivot = sprite.pivot / sprite.rect.size,
                        _border = sprite.border,
                        _sortIndex = spriteIndex++,
                        _isSelected = true,
                        _previewTexture = null,
                    };
                    entry._sprites.Add(spriteEntry);
                }
            }
            else
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null)
                {
                    SpriteEntryData spriteEntry = new()
                    {
                        _originalName = sprite.name,
                        _outputName = sprite.name,
                        _rect = sprite.rect,
                        _pivot = importer.spritePivot,
                        _border = sprite.border,
                        _sortIndex = 0,
                        _isSelected = true,
                        _previewTexture = null,
                    };
                    entry._sprites.Add(spriteEntry);
                }
            }
        }

        internal void PopulateSpritesFromGrid(SpriteSheetEntry entry, Texture2D texture)
        {
            int columns;
            int rows;
            int cellWidth;
            int cellHeight;

            Color32[] pixels = null;
            if (texture.isReadable)
            {
                pixels = texture.GetPixels32();
            }

            CalculateGridDimensions(
                texture.width,
                texture.height,
                entry,
                pixels,
                out columns,
                out rows,
                out cellWidth,
                out cellHeight
            );

            int spriteIndex = 0;
            for (int row = 0; row < rows; ++row)
            {
                for (int col = 0; col < columns; ++col)
                {
                    int x = col * cellWidth;
                    int y = (rows - 1 - row) * cellHeight;

                    Rect rect = new Rect(x, y, cellWidth, cellHeight);

                    SpriteEntryData spriteEntry = new()
                    {
                        _originalName = $"sprite_{row}_{col}",
                        _outputName = $"sprite_{row}_{col}",
                        _rect = rect,
                        _pivot = new Vector2(0.5f, 0.5f),
                        _border = Vector4.zero,
                        _sortIndex = spriteIndex++,
                        _isSelected = true,
                        _previewTexture = null,
                    };
                    entry._sprites.Add(spriteEntry);
                }
            }
        }

        internal void PopulateSpritesFromPaddedGrid(SpriteSheetEntry entry, Texture2D texture)
        {
            int columns;
            int rows;
            int cellWidth;
            int cellHeight;

            Color32[] pixels = null;
            if (texture.isReadable)
            {
                pixels = texture.GetPixels32();
            }

            CalculateGridDimensions(
                texture.width,
                texture.height,
                entry,
                pixels,
                out columns,
                out rows,
                out cellWidth,
                out cellHeight
            );

            int effectivePaddingLeft = GetEffectivePaddingLeft(entry);
            int effectivePaddingRight = GetEffectivePaddingRight(entry);
            int effectivePaddingTop = GetEffectivePaddingTop(entry);
            int effectivePaddingBottom = GetEffectivePaddingBottom(entry);

            int paddedWidth = cellWidth - effectivePaddingLeft - effectivePaddingRight;
            int paddedHeight = cellHeight - effectivePaddingTop - effectivePaddingBottom;

            if (paddedWidth <= 0 || paddedHeight <= 0)
            {
                this.LogWarn(
                    $"Padding values result in invalid sprite size for texture {entry._assetPath}. Cell: {cellWidth}x{cellHeight}, Padding removes: {effectivePaddingLeft + effectivePaddingRight}x{effectivePaddingTop + effectivePaddingBottom}"
                );
                return;
            }

            int spriteIndex = 0;
            for (int row = 0; row < rows; ++row)
            {
                for (int col = 0; col < columns; ++col)
                {
                    int baseX = col * cellWidth;
                    int baseY = (rows - 1 - row) * cellHeight;

                    int x = baseX + effectivePaddingLeft;
                    int y = baseY + effectivePaddingBottom;

                    Rect rect = new Rect(x, y, paddedWidth, paddedHeight);

                    SpriteEntryData spriteEntry = new()
                    {
                        _originalName = $"sprite_{row}_{col}",
                        _outputName = $"sprite_{row}_{col}",
                        _rect = rect,
                        _pivot = new Vector2(0.5f, 0.5f),
                        _border = Vector4.zero,
                        _sortIndex = spriteIndex++,
                        _isSelected = true,
                        _previewTexture = null,
                    };
                    entry._sprites.Add(spriteEntry);
                }
            }
        }

        internal void PopulateSpritesFromAlphaDetection(SpriteSheetEntry entry, Texture2D texture)
        {
            if (!texture.isReadable)
            {
                this.LogWarn(
                    $"Texture {entry._assetPath} is not readable. Enable 'Read/Write' in import settings for alpha detection."
                );
                return;
            }

            int textureWidth = texture.width;
            int textureHeight = texture.height;
            Color32[] pixels = texture.GetPixels32();

            using PooledResource<List<Rect>> rectsLease = Buffers<Rect>.List.Get(
                out List<Rect> detectedRects
            );

            float effectiveAlphaThreshold = GetEffectiveAlphaThreshold(entry);
            DetectSpriteBoundsByAlpha(
                pixels,
                textureWidth,
                textureHeight,
                effectiveAlphaThreshold,
                detectedRects
            );

            int spriteIndex = 0;
            for (int i = 0; i < detectedRects.Count; ++i)
            {
                Rect rect = detectedRects[i];

                SpriteEntryData spriteEntry = new()
                {
                    _originalName = $"sprite_{spriteIndex}",
                    _outputName = $"sprite_{spriteIndex}",
                    _rect = rect,
                    _pivot = new Vector2(0.5f, 0.5f),
                    _border = Vector4.zero,
                    _sortIndex = spriteIndex,
                    _isSelected = true,
                    _previewTexture = null,
                };
                entry._sprites.Add(spriteEntry);
                ++spriteIndex;
            }
        }

        private void PopulateSpritesFromMetadataIntoList(
            SpriteSheetEntry entry,
            string assetPath,
            TextureImporter importer,
            List<SpriteEntryData> targetList
        )
        {
            if (importer.spriteImportMode == SpriteImportMode.Multiple)
            {
                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                int spriteIndex = 0;
                for (int i = 0; i < allAssets.Length; ++i)
                {
                    if (allAssets[i] is not Sprite sprite)
                    {
                        continue;
                    }

                    SpriteEntryData spriteEntry = new()
                    {
                        _originalName = sprite.name,
                        _outputName = sprite.name,
                        _rect = sprite.rect,
                        _pivot = sprite.pivot / sprite.rect.size,
                        _border = sprite.border,
                        _sortIndex = spriteIndex++,
                        _isSelected = true,
                        _previewTexture = null,
                    };
                    targetList.Add(spriteEntry);
                }
            }
            else
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null)
                {
                    SpriteEntryData spriteEntry = new()
                    {
                        _originalName = sprite.name,
                        _outputName = sprite.name,
                        _rect = sprite.rect,
                        _pivot = importer.spritePivot,
                        _border = sprite.border,
                        _sortIndex = 0,
                        _isSelected = true,
                        _previewTexture = null,
                    };
                    targetList.Add(spriteEntry);
                }
            }
        }

        private void PopulateSpritesFromGridIntoList(
            SpriteSheetEntry entry,
            Texture2D texture,
            List<SpriteEntryData> targetList
        )
        {
            int columns;
            int rows;
            int cellWidth;
            int cellHeight;

            Color32[] pixels = null;
            if (texture.isReadable)
            {
                pixels = texture.GetPixels32();
            }

            CalculateGridDimensions(
                texture.width,
                texture.height,
                entry,
                pixels,
                out columns,
                out rows,
                out cellWidth,
                out cellHeight
            );

            int spriteIndex = 0;
            for (int row = 0; row < rows; ++row)
            {
                for (int col = 0; col < columns; ++col)
                {
                    int x = col * cellWidth;
                    int y = (rows - 1 - row) * cellHeight;

                    Rect rect = new Rect(x, y, cellWidth, cellHeight);

                    SpriteEntryData spriteEntry = new()
                    {
                        _originalName = $"sprite_{row}_{col}",
                        _outputName = $"sprite_{row}_{col}",
                        _rect = rect,
                        _pivot = new Vector2(0.5f, 0.5f),
                        _border = Vector4.zero,
                        _sortIndex = spriteIndex++,
                        _isSelected = true,
                        _previewTexture = null,
                    };
                    targetList.Add(spriteEntry);
                }
            }
        }

        private void PopulateSpritesFromPaddedGridIntoList(
            SpriteSheetEntry entry,
            Texture2D texture,
            List<SpriteEntryData> targetList
        )
        {
            int columns;
            int rows;
            int cellWidth;
            int cellHeight;

            Color32[] pixels = null;
            if (texture.isReadable)
            {
                pixels = texture.GetPixels32();
            }

            CalculateGridDimensions(
                texture.width,
                texture.height,
                entry,
                pixels,
                out columns,
                out rows,
                out cellWidth,
                out cellHeight
            );

            int effectivePaddingLeft = GetEffectivePaddingLeft(entry);
            int effectivePaddingRight = GetEffectivePaddingRight(entry);
            int effectivePaddingTop = GetEffectivePaddingTop(entry);
            int effectivePaddingBottom = GetEffectivePaddingBottom(entry);

            int paddedWidth = cellWidth - effectivePaddingLeft - effectivePaddingRight;
            int paddedHeight = cellHeight - effectivePaddingTop - effectivePaddingBottom;

            if (paddedWidth <= 0 || paddedHeight <= 0)
            {
                this.LogWarn(
                    $"Padding values result in invalid sprite size for texture {entry._assetPath}. Cell: {cellWidth}x{cellHeight}, Padding removes: {effectivePaddingLeft + effectivePaddingRight}x{effectivePaddingTop + effectivePaddingBottom}"
                );
                return;
            }

            int spriteIndex = 0;
            for (int row = 0; row < rows; ++row)
            {
                for (int col = 0; col < columns; ++col)
                {
                    int baseX = col * cellWidth;
                    int baseY = (rows - 1 - row) * cellHeight;

                    int x = baseX + effectivePaddingLeft;
                    int y = baseY + effectivePaddingBottom;

                    Rect rect = new Rect(x, y, paddedWidth, paddedHeight);

                    SpriteEntryData spriteEntry = new()
                    {
                        _originalName = $"sprite_{row}_{col}",
                        _outputName = $"sprite_{row}_{col}",
                        _rect = rect,
                        _pivot = new Vector2(0.5f, 0.5f),
                        _border = Vector4.zero,
                        _sortIndex = spriteIndex++,
                        _isSelected = true,
                        _previewTexture = null,
                    };
                    targetList.Add(spriteEntry);
                }
            }
        }

        private void PopulateSpritesFromAlphaDetectionIntoList(
            SpriteSheetEntry entry,
            Texture2D texture,
            List<SpriteEntryData> targetList
        )
        {
            if (!texture.isReadable)
            {
                this.LogWarn(
                    $"Texture {entry._assetPath} is not readable. Enable 'Read/Write' in import settings for alpha detection."
                );
                return;
            }

            int textureWidth = texture.width;
            int textureHeight = texture.height;
            Color32[] pixels = texture.GetPixels32();

            using PooledResource<List<Rect>> rectsLease = Buffers<Rect>.List.Get(
                out List<Rect> detectedRects
            );

            float effectiveAlphaThreshold = GetEffectiveAlphaThreshold(entry);
            DetectSpriteBoundsByAlpha(
                pixels,
                textureWidth,
                textureHeight,
                effectiveAlphaThreshold,
                detectedRects
            );

            int spriteIndex = 0;
            for (int i = 0; i < detectedRects.Count; ++i)
            {
                Rect rect = detectedRects[i];

                SpriteEntryData spriteEntry = new()
                {
                    _originalName = $"sprite_{spriteIndex}",
                    _outputName = $"sprite_{spriteIndex}",
                    _rect = rect,
                    _pivot = new Vector2(0.5f, 0.5f),
                    _border = Vector4.zero,
                    _sortIndex = spriteIndex,
                    _isSelected = true,
                    _previewTexture = null,
                };
                targetList.Add(spriteEntry);
                ++spriteIndex;
            }
        }

        internal static void DetectSpriteBoundsByAlpha(
            Color32[] pixels,
            int textureWidth,
            int textureHeight,
            float alphaThreshold,
            List<Rect> result
        )
        {
            result.Clear();

            byte alphaThresholdByte = (byte)(alphaThreshold * 255f);
            using PooledArray<bool> visitedLease = SystemArrayPool<bool>.Get(
                pixels.Length,
                out bool[] visited
            );
            Array.Clear(visited, 0, visited.Length);

            for (int y = 0; y < textureHeight; ++y)
            {
                for (int x = 0; x < textureWidth; ++x)
                {
                    int index = y * textureWidth + x;
                    if (visited[index])
                    {
                        continue;
                    }

                    if (pixels[index].a <= alphaThresholdByte)
                    {
                        visited[index] = true;
                        continue;
                    }

                    int minX = x;
                    int maxX = x;
                    int minY = y;
                    int maxY = y;

                    using PooledResource<List<int>> stackLease = Buffers<int>.List.Get(
                        out List<int> stack
                    );
                    stack.Add(index);
                    visited[index] = true;

                    while (stack.Count > 0)
                    {
                        int lastIndex = stack.Count - 1;
                        int current = stack[lastIndex];
                        stack.RemoveAt(lastIndex);

                        int currentX = current % textureWidth;
                        int currentY = current / textureWidth;

                        if (currentX < minX)
                        {
                            minX = currentX;
                        }
                        if (currentX > maxX)
                        {
                            maxX = currentX;
                        }
                        if (currentY < minY)
                        {
                            minY = currentY;
                        }
                        if (currentY > maxY)
                        {
                            maxY = currentY;
                        }

                        if (currentX > 0)
                        {
                            int leftIndex = current - 1;
                            if (!visited[leftIndex] && pixels[leftIndex].a > alphaThresholdByte)
                            {
                                visited[leftIndex] = true;
                                stack.Add(leftIndex);
                            }
                        }
                        if (currentX < textureWidth - 1)
                        {
                            int rightIndex = current + 1;
                            if (!visited[rightIndex] && pixels[rightIndex].a > alphaThresholdByte)
                            {
                                visited[rightIndex] = true;
                                stack.Add(rightIndex);
                            }
                        }
                        if (currentY > 0)
                        {
                            int bottomIndex = current - textureWidth;
                            if (!visited[bottomIndex] && pixels[bottomIndex].a > alphaThresholdByte)
                            {
                                visited[bottomIndex] = true;
                                stack.Add(bottomIndex);
                            }
                        }
                        if (currentY < textureHeight - 1)
                        {
                            int topIndex = current + textureWidth;
                            if (!visited[topIndex] && pixels[topIndex].a > alphaThresholdByte)
                            {
                                visited[topIndex] = true;
                                stack.Add(topIndex);
                            }
                        }
                    }

                    int width = maxX - minX + 1;
                    int height = maxY - minY + 1;

                    if (width >= 2 && height >= 2)
                    {
                        result.Add(new Rect(minX, minY, width, height));
                    }
                }
            }

            if (result.Count > 1)
            {
                result.Sort(
                    (a, b) =>
                    {
                        int yCompare = b.y.CompareTo(a.y);
                        return yCompare != 0 ? yCompare : a.x.CompareTo(b.x);
                    }
                );
            }
        }

        internal void ExtractSelectedSprites()
        {
            if (_discoveredSheets == null || _discoveredSheets.Count == 0)
            {
                this.LogWarn($"No sprite sheets discovered.");
                return;
            }

            string outputPath = AssetDatabase.GetAssetPath(_outputDirectory);
            if (string.IsNullOrWhiteSpace(outputPath) || !AssetDatabase.IsValidFolder(outputPath))
            {
                this.LogError($"Invalid output directory.");
                return;
            }

            _lastExtractedCount = 0;
            _lastSkippedCount = 0;
            _lastErrorCount = 0;

            bool canceled = false;
            int totalSprites = 0;
            int processedSprites = 0;

            for (int i = 0; i < _discoveredSheets.Count; ++i)
            {
                SpriteSheetEntry entry = _discoveredSheets[i];
                if (!entry._isSelected || entry._sprites == null)
                {
                    continue;
                }
                for (int j = 0; j < entry._sprites.Count; ++j)
                {
                    if (entry._sprites[j]._isSelected)
                    {
                        ++totalSprites;
                    }
                }
            }

            if (totalSprites == 0)
            {
                this.LogWarn($"No sprites selected for extraction.");
                return;
            }

            WallstopGenericPool<Dictionary<string, bool>> originalReadablePool = DictionaryBuffer<
                string,
                bool
            >.GetDictionaryPool(StringComparer.OrdinalIgnoreCase);
            using PooledResource<Dictionary<string, bool>> originalReadableLease =
                originalReadablePool.Get(out Dictionary<string, bool> originalReadable);

            try
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    for (int i = 0; i < _discoveredSheets.Count && !canceled; ++i)
                    {
                        SpriteSheetEntry entry = _discoveredSheets[i];
                        if (!entry._isSelected || entry._sprites == null)
                        {
                            continue;
                        }

                        if (
                            Utils.EditorUi.CancelableProgress(
                                Name,
                                $"Making readable: {Path.GetFileName(entry._assetPath)}",
                                (float)i / _discoveredSheets.Count * 0.1f
                            )
                        )
                        {
                            canceled = true;
                            break;
                        }

                        if (!entry._importer.isReadable)
                        {
                            originalReadable[entry._assetPath] = false;
                            entry._importer.isReadable = true;
                            entry._importer.SaveAndReimport();
                        }
                        else
                        {
                            originalReadable[entry._assetPath] = true;
                        }
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                if (!canceled)
                {
                    for (int i = 0; i < _discoveredSheets.Count && !canceled; ++i)
                    {
                        SpriteSheetEntry entry = _discoveredSheets[i];
                        if (!entry._isSelected || entry._sprites == null)
                        {
                            continue;
                        }

                        entry._texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry._assetPath);
                        if (entry._texture == null)
                        {
                            this.LogWarn($"Failed to reload texture: {entry._assetPath}");
                            continue;
                        }

                        List<SpriteEntryData> sortedSprites = GetSortedSprites(entry._sprites);

                        for (int j = 0; j < sortedSprites.Count && !canceled; ++j)
                        {
                            SpriteEntryData sprite = sortedSprites[j];
                            if (!sprite._isSelected)
                            {
                                continue;
                            }

                            if (
                                Utils.EditorUi.CancelableProgress(
                                    Name,
                                    $"Extracting: {sprite._originalName}",
                                    0.1f + (float)processedSprites / totalSprites * 0.9f
                                )
                            )
                            {
                                canceled = true;
                                break;
                            }

                            bool success = ExtractSprite(entry, sprite, outputPath, j);
                            if (success)
                            {
                                ++_lastExtractedCount;
                            }
                            ++processedSprites;
                        }
                    }
                }

                if (originalReadable.Count > 0)
                {
                    using PooledResource<List<string>> keysLease = Buffers<string>.List.Get(
                        out List<string> keys
                    );
                    foreach (KeyValuePair<string, bool> kvp in originalReadable)
                    {
                        keys.Add(kvp.Key);
                    }

                    AssetDatabase.StartAssetEditing();
                    try
                    {
                        for (int keyIndex = 0; keyIndex < keys.Count; ++keyIndex)
                        {
                            string key = keys[keyIndex];
                            if (originalReadable[key])
                            {
                                continue;
                            }

                            if (
                                AssetImporter.GetAtPath(key) is TextureImporter
                                {
                                    textureType: TextureImporterType.Sprite
                                } imp
                            )
                            {
                                imp.isReadable = false;
                                imp.SaveAndReimport();
                            }
                        }
                    }
                    finally
                    {
                        AssetDatabase.StopAssetEditing();
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }

                if (canceled)
                {
                    this.LogWarn($"Extraction canceled by user.");
                }
                else
                {
                    string mode = _dryRun ? "Dry run" : "Extraction";
                    this.Log(
                        $"{mode} complete. Extracted: {_lastExtractedCount}, Skipped: {_lastSkippedCount}, Errors: {_lastErrorCount}"
                    );
                }
            }
            catch (Exception ex)
            {
                this.LogError($"Error during extraction.", ex);
            }
            finally
            {
                Utils.EditorUi.ClearProgress();
            }
        }

        private bool ExtractSprite(
            SpriteSheetEntry sheet,
            SpriteEntryData sprite,
            string outputPath,
            int index
        )
        {
            if (sheet?._texture == null)
            {
                this.LogError($"Cannot extract sprite: texture is null.");
                ++_lastErrorCount;
                return false;
            }

            if (sprite == null)
            {
                ++_lastErrorCount;
                return false;
            }

            try
            {
                string prefix = string.IsNullOrWhiteSpace(_namingPrefix)
                    ? Path.GetFileNameWithoutExtension(sheet._assetPath)
                    : _namingPrefix;

                if (string.IsNullOrWhiteSpace(prefix))
                {
                    this.LogError(
                        $"Cannot extract sprite '{sprite._originalName}': output filename prefix is empty."
                    );
                    ++_lastErrorCount;
                    return false;
                }

                string outputFileName = $"{prefix}_{index:D3}.png";
                string fullOutputPath = Path.Combine(outputPath, outputFileName);

                if (!_overwriteExisting && File.Exists(fullOutputPath))
                {
                    ++_lastSkippedCount;
                    return false;
                }

                if (_dryRun)
                {
                    this.Log($"Would extract: {sprite._originalName} -> {fullOutputPath}");
                    return true;
                }

                int x = Mathf.FloorToInt(sprite._rect.x);
                int y = Mathf.FloorToInt(sprite._rect.y);
                int width = Mathf.FloorToInt(sprite._rect.width);
                int height = Mathf.FloorToInt(sprite._rect.height);

                x = Mathf.Clamp(x, 0, sheet._texture.width - 1);
                y = Mathf.Clamp(y, 0, sheet._texture.height - 1);
                width = Mathf.Clamp(width, 1, sheet._texture.width - x);
                height = Mathf.Clamp(height, 1, sheet._texture.height - y);

                Color32[] pixels = sheet._texture.GetPixels32();
                int srcWidth = sheet._texture.width;
                int pixelCount = width * height;
                // Note: Cannot use pooled arrays here because SetPixels32 requires the array length
                // to exactly match the texture dimensions, but ArrayPool returns arrays that may be
                // larger than requested.
                Color32[] destPixels = new Color32[pixelCount];

                Parallel.For(
                    0,
                    height,
                    destY =>
                    {
                        int srcY = y + destY;
                        int destRowStart = destY * width;
                        int srcRowStart = srcY * srcWidth + x;
                        for (int destX = 0; destX < width; ++destX)
                        {
                            destPixels[destRowStart + destX] = pixels[srcRowStart + destX];
                        }
                    }
                );

                Texture2D extracted = null;
                try
                {
                    extracted = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    extracted.SetPixels32(destPixels);
                    extracted.Apply();

                    byte[] pngBytes = extracted.EncodeToPNG();
                    File.WriteAllBytes(fullOutputPath, pngBytes);
                }
                finally
                {
                    if (extracted != null)
                    {
                        DestroyImmediate(extracted);
                    }
                }

                AssetDatabase.ImportAsset(fullOutputPath);

                if (_preserveImportSettings)
                {
                    ApplyImportSettings(fullOutputPath, sheet._importer, sprite);
                }

                return true;
            }
            catch (Exception ex)
            {
                this.LogError($"Failed to extract sprite '{sprite._originalName}'.", ex);
                ++_lastErrorCount;
                return false;
            }
        }

        private void ApplyImportSettings(
            string outputPath,
            TextureImporter sourceImporter,
            SpriteEntryData sprite
        )
        {
            if (AssetImporter.GetAtPath(outputPath) is not TextureImporter newImporter)
            {
                return;
            }

            newImporter.textureType = TextureImporterType.Sprite;
            newImporter.spriteImportMode = SpriteImportMode.Single;
            newImporter.spritePixelsPerUnit = sourceImporter.spritePixelsPerUnit;
            newImporter.filterMode = sourceImporter.filterMode;
            newImporter.textureCompression = sourceImporter.textureCompression;
            newImporter.wrapMode = sourceImporter.wrapMode;
            newImporter.mipmapEnabled = sourceImporter.mipmapEnabled;
            newImporter.isReadable = sourceImporter.isReadable;

            TextureImporterSettings settings = new();
            newImporter.ReadTextureSettings(settings);
            settings.spritePivot = sprite._pivot;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spriteBorder = sprite._border;
            newImporter.SetTextureSettings(settings);
            newImporter.spritePivot = sprite._pivot;

            try
            {
                TextureImporterPlatformSettings srcDefault =
                    sourceImporter.GetDefaultPlatformTextureSettings();
                if (!string.IsNullOrWhiteSpace(srcDefault.name))
                {
                    newImporter.SetPlatformTextureSettings(srcDefault);
                }
            }
            catch (Exception ex)
            {
                this.LogWarn($"Failed to copy platform settings for '{outputPath}'.", ex);
            }

            newImporter.SaveAndReimport();
        }

        private void ReplaceSpriteReferences()
        {
            if (_discoveredSheets == null || _discoveredSheets.Count == 0)
            {
                this.LogWarn($"No sprite sheets discovered. Run 'Find Sprite Sheets' first.");
                return;
            }

            string outputPath = AssetDatabase.GetAssetPath(_outputDirectory);
            if (string.IsNullOrWhiteSpace(outputPath) || !AssetDatabase.IsValidFolder(outputPath))
            {
                this.LogError($"Invalid output directory for reference replacement.");
                return;
            }

            WallstopGenericPool<Dictionary<Sprite, Sprite>> mappingPool = DictionaryBuffer<
                Sprite,
                Sprite
            >.Dictionary;
            using PooledResource<Dictionary<Sprite, Sprite>> mappingLease = mappingPool.Get(
                out Dictionary<Sprite, Sprite> mapping
            );

            try
            {
                for (int i = 0; i < _discoveredSheets.Count; ++i)
                {
                    SpriteSheetEntry entry = _discoveredSheets[i];
                    if (!entry._isSelected || entry._sprites == null)
                    {
                        continue;
                    }

                    Object[] originalSprites = AssetDatabase.LoadAllAssetsAtPath(entry._assetPath);
                    List<SpriteEntryData> sortedSprites = GetSortedSprites(entry._sprites);

                    for (int j = 0; j < sortedSprites.Count; ++j)
                    {
                        SpriteEntryData spriteData = sortedSprites[j];
                        if (!spriteData._isSelected)
                        {
                            continue;
                        }

                        Sprite originalSprite = null;
                        for (int k = 0; k < originalSprites.Length; ++k)
                        {
                            if (
                                originalSprites[k] is Sprite s
                                && s.name == spriteData._originalName
                            )
                            {
                                originalSprite = s;
                                break;
                            }
                        }

                        if (originalSprite == null)
                        {
                            continue;
                        }

                        string prefix = string.IsNullOrWhiteSpace(_namingPrefix)
                            ? Path.GetFileNameWithoutExtension(entry._assetPath)
                            : _namingPrefix;
                        string extractedFileName = $"{prefix}_{j:D3}.png";
                        string extractedPath = Path.Combine(outputPath, extractedFileName);

                        Sprite extractedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                            extractedPath
                        );
                        if (extractedSprite != null)
                        {
                            mapping[originalSprite] = extractedSprite;
                        }
                    }
                }

                if (mapping.Count == 0)
                {
                    this.LogWarn($"No original->extracted sprite mappings found.");
                    return;
                }

                string[] allAssets = AssetDatabase.GetAllAssetPaths();
                string[] candidateExts =
                {
                    ".prefab",
                    ".unity",
                    ".asset",
                    ".mat",
                    ".anim",
                    ".overrideController",
                };
                int modifiedAssets = 0;

                AssetDatabase.StartAssetEditing();
                try
                {
                    for (int i = 0; i < allAssets.Length; ++i)
                    {
                        string path = allAssets[i];
                        if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        bool hasValidExt = false;
                        for (int extIndex = 0; extIndex < candidateExts.Length; ++extIndex)
                        {
                            if (
                                path.EndsWith(
                                    candidateExts[extIndex],
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            {
                                hasValidExt = true;
                                break;
                            }
                        }
                        if (!hasValidExt)
                        {
                            continue;
                        }

                        if (
                            Utils.EditorUi.CancelableProgress(
                                "Replacing Sprite References",
                                $"Scanning {i + 1}/{allAssets.Length}: {Path.GetFileName(path)}",
                                i / (float)allAssets.Length
                            )
                        )
                        {
                            this.LogWarn($"Reference replacement cancelled by user.");
                            break;
                        }

                        bool assetModified = false;
                        Object[] objs = AssetDatabase.LoadAllAssetsAtPath(path);
                        for (int objIndex = 0; objIndex < objs.Length; ++objIndex)
                        {
                            Object o = objs[objIndex];
                            if (o == null)
                            {
                                continue;
                            }

                            SerializedObject so = new(o);
                            SerializedProperty it = so.GetIterator();
                            bool enter = true;
                            while (it.NextVisible(enter))
                            {
                                enter = false;
                                if (it.propertyType != SerializedPropertyType.ObjectReference)
                                {
                                    continue;
                                }

                                Sprite s = it.objectReferenceValue as Sprite;
                                if (s != null && mapping.TryGetValue(s, out Sprite replacement))
                                {
                                    Undo.RecordObject(o, "Replace sprite reference");
                                    it.objectReferenceValue = replacement;
                                    assetModified = true;
                                    this.Log(
                                        $"Replaced reference in {path}: {s.name} -> {replacement.name}"
                                    );
                                }
                            }
                            if (assetModified)
                            {
                                so.ApplyModifiedPropertiesWithoutUndo();
                                EditorUtility.SetDirty(o);
                            }
                        }
                        if (assetModified)
                        {
                            ++modifiedAssets;
                        }
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Utils.EditorUi.ClearProgress();
                }

                this.Log(
                    $"Reference replacement complete. Modified assets: {modifiedAssets}. Mapped pairs: {mapping.Count}."
                );
            }
            catch (Exception ex)
            {
                this.LogError($"Error during reference replacement.", ex);
            }
        }
    }
#endif
}

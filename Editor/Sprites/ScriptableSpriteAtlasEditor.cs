namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEditor.U2D;
    using UnityEngine;
    using UnityEngine.U2D;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Editor window for managing <c>ScriptableSpriteAtlas</c> configuration assets and generating
    /// corresponding <c>.spriteatlas</c> assets. Supports scanning for adds/removals, bulk generate,
    /// and optional packing after generation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Problems this solves: keeping <c>SpriteAtlas</c> assets in sync with curated rules and sets
    /// of sprites, with visibility into pending additions/removals before writing.
    /// </para>
    /// <para>
    /// How it works: loads all <c>ScriptableSpriteAtlas</c> assets in the project, caches scan
    /// results (to add/remove), and drives generation of the <c>.spriteatlas</c> assets. Optionally
    /// invokes <see cref="SpriteAtlasUtility.PackAllAtlases"/> to repack.
    /// </para>
    /// <para>
    /// Usage: open via menu, refresh config list, create new configs in a target folder, then
    /// Generate/Pack as needed.
    /// </para>
    /// <para>
    /// Caveats: generation modifies/creates assets; ensure correct output paths and VCS.
    /// </para>
    /// </remarks>
    public sealed class ScriptableSpriteAtlasEditor : EditorWindow
    {
        private static bool SuppressUserPrompts { get; set; }

        static ScriptableSpriteAtlasEditor()
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

        private readonly Dictionary<ScriptableSpriteAtlas, SerializedObject> _serializedConfigs =
            new();
        private List<ScriptableSpriteAtlas> _atlasConfigs = new();
        private Vector2 _scrollPosition;
        private bool _packAfterGenerate;

        private sealed class ScanResult
        {
            public List<Sprite> spritesToAdd = new();
            public List<Sprite> spritesToRemove = new();
            public bool hasScanned;
        }

        private readonly Dictionary<ScriptableSpriteAtlas, ScanResult> _scanResultsCache = new();
        private readonly Dictionary<ScriptableSpriteAtlas, bool> _foldoutStates = new();

        private const string NewAtlasConfigDirectory = "Assets/Data";

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Sprite Atlas Generator")]
        public static void ShowWindow()
        {
            GetWindow<ScriptableSpriteAtlasEditor>("Sprite Atlas Generator");
        }

        private void OnEnable()
        {
            LoadAtlasConfigs();
        }

        private void OnProjectChange()
        {
            LoadAtlasConfigs();
        }

        internal void LoadAtlasConfigs()
        {
            _atlasConfigs.Clear();
            Dictionary<ScriptableSpriteAtlas, ScanResult> existingScanCache = new(
                _scanResultsCache
            );
            _serializedConfigs.Clear();
            _scanResultsCache.Clear();

            string[] guids = AssetDatabase.FindAssets("t:ScriptableSpriteAtlas");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }
                ScriptableSpriteAtlas config = AssetDatabase.LoadAssetAtPath<ScriptableSpriteAtlas>(
                    path
                );
                if (config != null)
                {
                    _atlasConfigs.Add(config);
                    if (existingScanCache.TryGetValue(config, out ScanResult cachedResult))
                    {
                        _scanResultsCache[config] = cachedResult;
                    }
                    else
                    {
                        _scanResultsCache.TryAdd(config, _ => new ScanResult());
                    }
                    _serializedConfigs.TryAdd(config, newConfig => new SerializedObject(newConfig));
                    _foldoutStates.TryAdd(config, true);
                }
            }
            _atlasConfigs = _atlasConfigs.OrderBy(c => c.name).ToList();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Sprite Atlas Generation Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Config List", GUILayout.Height(30)))
                {
                    LoadAtlasConfigs();
                }

                if (
                    GUILayout.Button(
                        $"Create New Config in '{NewAtlasConfigDirectory}'",
                        GUILayout.Height(30)
                    )
                )
                {
                    CreateNewScriptableSpriteAtlas();
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                _packAfterGenerate = EditorGUILayout.ToggleLeft(
                    new GUIContent(
                        "Pack after generate",
                        "If enabled, atlases will be packed immediately after generation."
                    ),
                    _packAfterGenerate,
                    GUILayout.Width(180)
                );

                if (
                    GUILayout.Button(
                        "Generate/Update All .spriteatlas Assets",
                        GUILayout.Height(30)
                    )
                )
                {
                    GenerateAllAtlases();
                    if (_packAfterGenerate)
                    {
                        PackAllProjectAtlases();
                    }
                }

                if (GUILayout.Button("Generate + Pack All", GUILayout.Height(30)))
                {
                    GenerateAllAtlases();
                    PackAllProjectAtlases();
                }
            }

            if (GUILayout.Button("Pack All Generated Sprite Atlases", GUILayout.Height(40)))
            {
                PackAllProjectAtlases();
            }
            EditorGUILayout.Space(20);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            if (_atlasConfigs.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No ScriptableSpriteAtlas configurations found. Use the 'Create New Config' button above or create one via Assets > Create > Wallstop Studios > Unity Helpers > Scriptable Sprite Atlas Config.",
                    MessageType.Info
                );
            }

            foreach (ScriptableSpriteAtlas config in _atlasConfigs)
            {
                if (config == null)
                {
                    LoadAtlasConfigs();
                    Repaint();
                    return;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                string foldoutLabel =
                    $"{config.name} (Output: {config.FullOutputPath ?? "Path Not Set"})";
                if (AssetDatabase.Contains(config))
                {
                    foldoutLabel += $" - Path: {AssetDatabase.GetAssetPath(config)}";
                }

                _foldoutStates[config] = EditorGUILayout.Foldout(
                    _foldoutStates[config],
                    foldoutLabel,
                    true,
                    EditorStyles.foldoutHeader
                );

                if (_foldoutStates[config])
                {
                    using EditorGUI.IndentLevelScope indentScope = new();
                    SerializedObject serializedConfig = _serializedConfigs.GetOrAdd(
                        config,
                        newConfig => new SerializedObject(newConfig)
                    );
                    serializedConfig.Update();
                    EditorGUI.BeginChangeCheck();

                    string currentAssetName = config.name;
                    Rect nameRect = EditorGUILayout.GetControlRect();
                    EditorGUI.BeginChangeCheck();
                    string newAssetName = EditorGUI.TextField(
                        new Rect(nameRect.x, nameRect.y, nameRect.width - 60, nameRect.height),
                        "Asset Name",
                        currentAssetName
                    );
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedConfig.ApplyModifiedProperties();
                        if (
                            !string.IsNullOrWhiteSpace(newAssetName)
                            && newAssetName != currentAssetName
                            && AssetDatabase.Contains(config)
                        )
                        {
                            string assetPath = AssetDatabase.GetAssetPath(config);
                            string error = AssetDatabase.RenameAsset(assetPath, newAssetName);
                            if (string.IsNullOrWhiteSpace(error))
                            {
                                LoadAtlasConfigs();
                                GUIUtility.ExitGUI();
                            }
                            else
                            {
                                this.LogError($"Failed to rename asset: {error}");
                            }
                        }
                    }

                    SerializedProperty scriptProperty = serializedConfig.FindProperty("m_Script");
                    if (scriptProperty != null)
                    {
                        GUI.enabled = false;
                        EditorGUILayout.PropertyField(scriptProperty);
                        GUI.enabled = true;
                    }

                    SerializedProperty property = serializedConfig.GetIterator();
                    bool enterChildren = true;
                    while (property.NextVisible(enterChildren))
                    {
                        enterChildren = false;
                        if (string.Equals(property.name, "m_Script", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        EditorGUILayout.PropertyField(property, true);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedConfig.ApplyModifiedProperties();
                    }

                    EditorGUILayout.Space();
                    // Output validation and quick actions
                    string fullOutputPath = config.FullOutputPath;
                    if (
                        string.IsNullOrWhiteSpace(config.outputSpriteAtlasDirectory)
                        || string.IsNullOrWhiteSpace(config.outputSpriteAtlasName)
                    )
                    {
                        EditorGUILayout.HelpBox(
                            "Output directory and file name must be set to generate the atlas.",
                            MessageType.Warning
                        );
                    }
                    else
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Generate Only"))
                            {
                                GenerateSingleAtlas(config);
                                if (_packAfterGenerate)
                                {
                                    PackAllProjectAtlases();
                                }
                            }
                            if (GUILayout.Button("Generate + Pack"))
                            {
                                GenerateSingleAtlas(config);
                                PackAllProjectAtlases();
                            }

                            if (!string.IsNullOrWhiteSpace(fullOutputPath))
                            {
                                SpriteAtlas existing = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(
                                    fullOutputPath
                                );
                                if (existing != null)
                                {
                                    if (GUILayout.Button("Ping Atlas"))
                                    {
                                        PingAtlas(fullOutputPath);
                                    }
                                    if (GUILayout.Button("Reveal In Explorer"))
                                    {
                                        RevealInExplorer(fullOutputPath);
                                    }
                                }
                            }
                        }
                    }
                    if (GUILayout.Button("Add New Source Folder Entry"))
                    {
                        string folderPath = Utils.EditorUi.OpenFolderPanel(
                            "Select Source Folder",
                            Application.dataPath,
                            ""
                        );
                        if (!string.IsNullOrWhiteSpace(folderPath))
                        {
                            if (folderPath.StartsWith(Application.dataPath))
                            {
                                string relativePath =
                                    "Assets" + folderPath.Substring(Application.dataPath.Length);
                                relativePath = relativePath.SanitizePath();
                                SerializedProperty sourceFolderEntriesProp =
                                    serializedConfig.FindProperty(
                                        nameof(ScriptableSpriteAtlas.sourceFolderEntries)
                                    );

                                bool pathExists = false;
                                for (int j = 0; j < sourceFolderEntriesProp.arraySize; j++)
                                {
                                    SerializedProperty entryProp =
                                        sourceFolderEntriesProp.GetArrayElementAtIndex(j);
                                    SerializedProperty pathProp = entryProp.FindPropertyRelative(
                                        "folderPath"
                                    );
                                    if (
                                        string.Equals(
                                            pathProp.stringValue,
                                            relativePath,
                                            StringComparison.Ordinal
                                        )
                                    )
                                    {
                                        pathExists = true;
                                        this.LogWarn(
                                            $"Folder path '{relativePath}' already exists in an entry for '{config.name}'."
                                        );
                                        break;
                                    }
                                }

                                if (!pathExists)
                                {
                                    sourceFolderEntriesProp.InsertArrayElementAtIndex(
                                        sourceFolderEntriesProp.arraySize
                                    );
                                    SerializedProperty newEntryProp =
                                        sourceFolderEntriesProp.GetArrayElementAtIndex(
                                            sourceFolderEntriesProp.arraySize - 1
                                        );
                                    newEntryProp.FindPropertyRelative("folderPath").stringValue =
                                        relativePath;

                                    serializedConfig.ApplyModifiedProperties();
                                    this.Log(
                                        $"Added new source folder entry for '{relativePath}' to '{config.name}'. You can add regexes to it below."
                                    );
                                }
                            }
                            else
                            {
                                Utils.EditorUi.Info(
                                    "Invalid Folder",
                                    "The selected folder must be within the project's 'Assets' directory."
                                );
                            }
                        }
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Analysis & Actions", EditorStyles.boldLabel);

                    if (GUILayout.Button($"Scan Folders for '{config.name}'"))
                    {
                        ScanFoldersForConfig(config);
                    }

                    ScanResult result = _scanResultsCache[config];
                    if (result.hasScanned)
                    {
                        EditorGUILayout.LabelField(
                            $"Current manually added sprites: {config.spritesToPack.Count(s => s != null)}"
                        );
                        EditorGUILayout.LabelField(
                            "Sprites found by scan (not yet added/removed):"
                        );
                        using EditorGUI.IndentLevelScope nextIndentScope = new();

                        if (result.spritesToAdd.Count > 0)
                        {
                            EditorGUILayout.LabelField(
                                $"To Add: {result.spritesToAdd.Count} sprites."
                            );
                            if (
                                GUILayout.Button(
                                    $"Add {result.spritesToAdd.Count} Sprites to '{config.name}' List"
                                )
                            )
                            {
                                AddScannedSprites(config, result);
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField(
                                "To Add: 0 sprites.",
                                EditorStyles.miniLabel
                            );
                        }

                        if (result.spritesToRemove.Count > 0)
                        {
                            EditorGUILayout.LabelField(
                                $"To Remove: {result.spritesToRemove.Count} sprites (currently in list but not found by scan)."
                            );
                            if (
                                GUILayout.Button(
                                    $"Remove {result.spritesToRemove.Count} Sprites from '{config.name}' List"
                                )
                            )
                            {
                                RemoveUnfoundSprites(config, result);
                            }
                            if (
                                GUILayout.Button(
                                    $"Sync List To Scan Result ({result.spritesToAdd.Count} add, {result.spritesToRemove.Count} remove)"
                                )
                            )
                            {
                                SyncListToScanResult(config, result);
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField(
                                "To Remove: 0 sprites.",
                                EditorStyles.miniLabel
                            );
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            "Scan to see potential changes from folder sources.",
                            MessageType.None
                        );
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Source Sprite Utilities", EditorStyles.boldLabel);

                    int validSpriteCount = config.spritesToPack.Count(s => s != null);
                    EditorGUI.BeginDisabledGroup(validSpriteCount == 0);
                    if (
                        GUILayout.Button(
                            $"Force Uncompressed for {validSpriteCount} Source Sprites in '{config.name}'"
                        )
                        && Utils.EditorUi.Confirm(
                            "Force Uncompressed Source Sprites",
                            $"This will modify the import settings of {validSpriteCount} source sprites currently in the '{config.name}' list.\n\n"
                                + "- Crunch compression will be disabled.\n"
                                + "- Texture format for the 'Default' platform will be set to uncompressed (RGBA32 or RGB24).\n\n"
                                + "This action modifies source asset import settings and may require re-packing atlases. Are you sure?",
                            "Yes, Modify Source Sprites",
                            "Cancel",
                            defaultWhenSuppressed: true
                        )
                    )
                    {
                        ForceUncompressedSourceSprites(config);
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.Space();
                    if (
                        GUILayout.Button(
                            $"Generate/Update '{config.outputSpriteAtlasName}.spriteatlas' ONLY"
                        )
                        && Utils.EditorUi.Confirm(
                            $"Generate Atlas: {config.name}",
                            $"This will create or update '{config.outputSpriteAtlasName}.spriteatlas'. Continue?",
                            "Yes",
                            "No",
                            defaultWhenSuppressed: true
                        )
                    )
                    {
                        GenerateSingleAtlas(config);
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndScrollView();
        }

        private void CreateNewScriptableSpriteAtlas()
        {
            DirectoryHelper.EnsureDirectoryExists(NewAtlasConfigDirectory);
            ScriptableSpriteAtlas newAtlasConfig = CreateInstance<ScriptableSpriteAtlas>();
            string path = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(NewAtlasConfigDirectory, "NewScriptableSpriteAtlas.asset")
            );

            AssetDatabase.CreateAsset(newAtlasConfig, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newAtlasConfig;

            this.Log($"Created new ScriptableSpriteAtlas at: {path}");
            LoadAtlasConfigs();
            Repaint();
        }

        private void ScanFoldersForConfig(ScriptableSpriteAtlas config)
        {
            if (config.sourceFolderEntries == null || config.sourceFolderEntries.Count == 0)
            {
                this.LogWarn(
                    $"'{config.name}': No source folder entries defined. Scan will find nothing from folders."
                );
                _scanResultsCache[config] = new ScanResult { hasScanned = true };
                Repaint();
                return;
            }

            ScanResult currentScan = new();
            HashSet<Sprite> foundSpritesInFolders = new();

            foreach (SourceFolderEntry entry in config.sourceFolderEntries)
            {
                if (
                    string.IsNullOrWhiteSpace(entry.folderPath)
                    || !AssetDatabase.IsValidFolder(entry.folderPath)
                )
                {
                    this.LogWarn(
                        $"'{config.name}': Invalid or empty folder path '{entry.folderPath}' in an entry. Skipping this entry."
                    );
                    continue;
                }

                // Build a search set optimized by labels when applicable
                bool useLabels =
                    entry.selectionMode.HasFlagNoAlloc(SpriteSelectionMode.Labels)
                    && entry.labels is { Count: > 0 };
                List<string> guidList = new();
                if (useLabels)
                {
                    switch (entry.labelSelectionMode)
                    {
                        case LabelSelectionMode.All:
                        {
                            string query = "t:Texture2D";
                            foreach (string l in entry.labels)
                            {
                                if (!string.IsNullOrWhiteSpace(l))
                                {
                                    query += $" l:{l}";
                                }
                            }
                            guidList.AddRange(
                                AssetDatabase.FindAssets(query, new[] { entry.folderPath })
                            );
                            break;
                        }
                        case LabelSelectionMode.AnyOf:
                        {
                            using PooledResource<HashSet<string>> setRes =
                                Buffers<string>.HashSet.Get();
                            HashSet<string> set = setRes.resource;
                            foreach (string l in entry.labels)
                            {
                                if (string.IsNullOrWhiteSpace(l))
                                {
                                    continue;
                                }
                                string query = $"t:Texture2D l:{l}";
                                foreach (
                                    string g in AssetDatabase.FindAssets(
                                        query,
                                        new[] { entry.folderPath }
                                    )
                                )
                                {
                                    set.Add(g);
                                }
                            }
                            if (set.Count > 0)
                            {
                                guidList.AddRange(set);
                            }
                            break;
                        }
                        default:
                        {
                            this.LogError(
                                $"'{config.name}', Folder '{entry.folderPath}': Invalid LabelSelectionMode value '{entry.labelSelectionMode}'. Skipping label pre-filter."
                            );
                            break;
                        }
                    }
                }
                else
                {
                    guidList.AddRange(
                        AssetDatabase.FindAssets("t:Texture2D", new[] { entry.folderPath })
                    );
                }

                // Prepare compiled regexes if regex selection is enabled
                bool useRegex = entry.selectionMode.HasFlagNoAlloc(SpriteSelectionMode.Regex);
                List<string> activeRegexPatterns = entry
                    .regexes.Where(r => !string.IsNullOrWhiteSpace(r))
                    .ToList();
                List<Regex> compiledRegexes = null;
                if (useRegex && activeRegexPatterns.Count > 0)
                {
                    compiledRegexes = new List<Regex>(activeRegexPatterns.Count);
                    foreach (string pattern in activeRegexPatterns)
                    {
                        try
                        {
                            compiledRegexes.Add(
                                new Regex(
                                    pattern,
                                    RegexOptions.IgnoreCase
                                        | RegexOptions.CultureInvariant
                                        | RegexOptions.Compiled
                                )
                            );
                        }
                        catch (ArgumentException ex)
                        {
                            this.LogError(
                                $"'{config.name}', Folder '{entry.folderPath}': Invalid Regex pattern '{pattern}': {ex.Message}. This pattern will be ignored."
                            );
                        }
                    }
                }

                // Compile exclusion regexes if provided
                List<Regex> compiledExcludeRegexes = null;
                if (entry.excludeRegexes is { Count: > 0 })
                {
                    compiledExcludeRegexes = new List<Regex>(entry.excludeRegexes.Count);
                    foreach (string pattern in entry.excludeRegexes)
                    {
                        if (string.IsNullOrWhiteSpace(pattern))
                        {
                            continue;
                        }
                        try
                        {
                            compiledExcludeRegexes.Add(
                                new Regex(
                                    pattern,
                                    RegexOptions.IgnoreCase
                                        | RegexOptions.CultureInvariant
                                        | RegexOptions.Compiled
                                )
                            );
                        }
                        catch (ArgumentException ex)
                        {
                            this.LogError(
                                $"'{config.name}', Folder '{entry.folderPath}': Invalid Exclude Regex pattern '{pattern}': {ex.Message}. This pattern will be ignored."
                            );
                        }
                    }
                }

                foreach (string guid in guidList)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = Path.GetFileName(assetPath);

                    bool matchesAllRegexesInEntry = true;
                    if (useRegex && compiledRegexes is { Count: > 0 })
                    {
                        foreach (Regex rx in compiledRegexes)
                        {
                            if (!rx.IsMatch(fileName))
                            {
                                matchesAllRegexesInEntry = false;
                                break;
                            }
                        }
                    }

                    bool allMatch = matchesAllRegexesInEntry;
                    bool matchesAllTagsInEntry = true;
                    if (
                        entry.selectionMode.HasFlagNoAlloc(SpriteSelectionMode.Labels)
                        && entry.labels is { Count: > 0 }
                    )
                    {
                        Object mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                        if (mainAsset != null)
                        {
                            string[] labels = AssetDatabase.GetLabels(mainAsset);
                            using PooledResource<HashSet<string>> entryLabelsRes =
                                Buffers<string>.HashSet.Get();
                            HashSet<string> entryLabels = entryLabelsRes.resource;
                            entryLabels.Clear();
                            entryLabels.UnionWith(entry.labels);

                            using PooledResource<HashSet<string>> assetLabelsRes =
                                Buffers<string>.HashSet.Get();
                            HashSet<string> assetLabels = assetLabelsRes.resource;
                            assetLabels.Clear();
                            assetLabels.UnionWith(labels);
                            switch (entry.labelSelectionMode)
                            {
                                case LabelSelectionMode.All:
                                {
                                    // Asset must contain all required entry labels
                                    matchesAllTagsInEntry = entryLabels.All(assetLabels.Contains);
                                    break;
                                }
                                case LabelSelectionMode.AnyOf:
                                {
                                    matchesAllTagsInEntry = assetLabels.Any(entryLabels.Contains);
                                    break;
                                }
                                default:
                                {
                                    this.LogError(
                                        $"'{config.name}', Folder '{entry.folderPath}': Invalid LabelSelectionMode value '{entry.labelSelectionMode}'. Skipping label filtering for this entry."
                                    );
                                    matchesAllTagsInEntry = false;
                                    break;
                                }
                            }
                        }

                        switch (entry.regexAndTagLogic)
                        {
                            case SpriteSelectionBooleanLogic.And:
                            {
                                allMatch = matchesAllRegexesInEntry && matchesAllTagsInEntry;
                                break;
                            }
                            case SpriteSelectionBooleanLogic.Or:
                            {
                                allMatch = matchesAllRegexesInEntry || matchesAllTagsInEntry;
                                break;
                            }
                            default:
                            {
                                this.LogError(
                                    $"'{config.name}', Folder '{entry.folderPath}': Invalid SpriteSelectionBooleanLogic value '{entry.regexAndTagLogic}'. Defaulting to AND logic."
                                );
                                allMatch = matchesAllRegexesInEntry && matchesAllTagsInEntry;
                                break;
                            }
                        }
                    }

                    if (allMatch)
                    {
                        bool excluded = false;
                        // Exclude by path prefixes (case-insensitive starts-with)
                        if (!excluded && entry.excludePathPrefixes is { Count: > 0 })
                        {
                            string ap = assetPath.SanitizePath();
                            foreach (string prefix in entry.excludePathPrefixes)
                            {
                                if (string.IsNullOrWhiteSpace(prefix))
                                {
                                    continue;
                                }
                                string p = prefix.SanitizePath();
                                if (ap.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                                {
                                    excluded = true;
                                    break;
                                }
                            }
                        }
                        // Exclude by regex (OR semantics)
                        if (!excluded && compiledExcludeRegexes is { Count: > 0 })
                        {
                            foreach (Regex rx in compiledExcludeRegexes)
                            {
                                if (rx.IsMatch(fileName))
                                {
                                    excluded = true;
                                    break;
                                }
                            }
                        }

                        // Exclude by labels
                        if (!excluded && entry.excludeLabels is { Count: > 0 })
                        {
                            Object mainAsset2 = AssetDatabase.LoadMainAssetAtPath(assetPath);
                            if (mainAsset2 != null)
                            {
                                string[] labels2 = AssetDatabase.GetLabels(mainAsset2);
                                using PooledResource<HashSet<string>> exEntryLabelsRes =
                                    Buffers<string>.HashSet.Get();
                                HashSet<string> exEntryLabels = exEntryLabelsRes.resource;
                                exEntryLabels.Clear();
                                exEntryLabels.UnionWith(entry.excludeLabels);

                                using PooledResource<HashSet<string>> assetLabelsRes2 =
                                    Buffers<string>.HashSet.Get();
                                HashSet<string> assetLabels2 = assetLabelsRes2.resource;
                                assetLabels2.Clear();
                                assetLabels2.UnionWith(labels2);

                                switch (entry.excludeLabelSelectionMode)
                                {
                                    case LabelSelectionMode.All:
                                        excluded = exEntryLabels.All(assetLabels2.Contains);
                                        break;
                                    case LabelSelectionMode.AnyOf:
                                        excluded = assetLabels2.Any(exEntryLabels.Contains);
                                        break;
                                }
                            }
                        }

                        if (!excluded)
                        {
                            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                            {
                                if (asset is Sprite spriteAsset && spriteAsset != null)
                                {
                                    foundSpritesInFolders.Add(spriteAsset);
                                }
                            }
                        }
                    }
                }
            }

            List<Sprite> validSpritesInConfigList = config
                .spritesToPack.Where(s => s != null)
                .ToList();

            currentScan.spritesToAdd = foundSpritesInFolders
                .Except(validSpritesInConfigList)
                .ToList();

            currentScan.spritesToRemove = validSpritesInConfigList
                .Except(foundSpritesInFolders)
                .ToList();

            currentScan.hasScanned = true;
            _scanResultsCache[config] = currentScan;
            this.Log(
                $"'{config.name}': Scan complete. Total unique sprites found across all folder entries: {foundSpritesInFolders.Count}. Potential to add: {currentScan.spritesToAdd.Count}, Potential to remove: {currentScan.spritesToRemove.Count}."
            );
            Repaint();
        }

        private void AddScannedSprites(ScriptableSpriteAtlas config, ScanResult result)
        {
            if (result.spritesToAdd.Count <= 0)
            {
                return;
            }

            SerializedObject so = new(config);
            SerializedProperty spritesListProp = so.FindProperty(
                nameof(ScriptableSpriteAtlas.spritesToPack)
            );

            Undo.RecordObject(config, "Add Scanned Sprites to Atlas Config");

            int addedCount = 0;
            foreach (Sprite sprite in result.spritesToAdd)
            {
                bool alreadyExists = false;
                for (int i = 0; i < spritesListProp.arraySize; ++i)
                {
                    if (spritesListProp.GetArrayElementAtIndex(i).objectReferenceValue == sprite)
                    {
                        alreadyExists = true;
                        break;
                    }
                }
                if (!alreadyExists)
                {
                    spritesListProp.InsertArrayElementAtIndex(spritesListProp.arraySize);
                    spritesListProp
                        .GetArrayElementAtIndex(spritesListProp.arraySize - 1)
                        .objectReferenceValue = sprite;
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                so.ApplyModifiedProperties();
                config.spritesToPack.SortByName();
                EditorUtility.SetDirty(config);
                this.Log($"'{config.name}': Added {addedCount} sprites.");
            }
            else
            {
                this.Log(
                    $"'{config.name}': No new sprites to add (all found sprites might already be in the list)."
                );
            }

            result.spritesToAdd.Clear();
            ScanFoldersForConfig(config);
            Repaint();
        }

        private void RemoveUnfoundSprites(ScriptableSpriteAtlas config, ScanResult result)
        {
            if (result.spritesToRemove.Count <= 0)
            {
                return;
            }

            SerializedObject so = new(config);
            SerializedProperty spritesListProp = so.FindProperty("spritesToPack");

            Undo.RecordObject(config, "Remove Unfound Sprites from Atlas Config");

            int countRemoved = 0;
            List<Sprite> spritesActuallyToRemove = new(result.spritesToRemove);

            for (int i = spritesListProp.arraySize - 1; 0 <= i; --i)
            {
                SerializedProperty element = spritesListProp.GetArrayElementAtIndex(i);
                if (
                    element.objectReferenceValue != null
                    && spritesActuallyToRemove.Contains(element.objectReferenceValue as Sprite)
                )
                {
                    element.objectReferenceValue = null;
                    spritesListProp.DeleteArrayElementAtIndex(i);
                    countRemoved++;
                }
            }

            if (countRemoved > 0)
            {
                so.ApplyModifiedProperties();
                this.Log(
                    $"'{config.name}': Removed {countRemoved} sprites that were no longer found by scan."
                );
            }
            result.spritesToRemove.Clear();
            ScanFoldersForConfig(config);
            Repaint();
        }

        internal void GenerateAllAtlases()
        {
            if (_atlasConfigs.Count == 0)
            {
                Utils.EditorUi.Info(
                    "No Configurations",
                    "No ScriptableSpriteAtlas configurations found to generate."
                );
                return;
            }

            int totalConfigs = _atlasConfigs.Count;
            int currentConfig = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (ScriptableSpriteAtlas config in _atlasConfigs)
                {
                    if (config == null)
                    {
                        continue;
                    }

                    currentConfig++;
                    float progress = (float)currentConfig / totalConfigs;
                    Utils.EditorUi.ShowProgress(
                        "Generating Sprite Atlases",
                        $"Processing: {config.name}",
                        progress
                    );
                    GenerateSingleAtlas(config, false);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                Utils.EditorUi.ClearProgress();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void GenerateSingleAtlas(
            ScriptableSpriteAtlas config,
            bool refreshAssetsImmediately = true
        )
        {
            if (config == null)
            {
                this.LogError($"Attempted to generate atlas from a null config.");
                return;
            }
            string outputPath = config.FullOutputPath;
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                this.LogError(
                    $"'{config.name}': Output path or name is not set. Cannot generate atlas."
                );
                return;
            }

            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(outputPath);
            bool newAtlas = false;
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                newAtlas = true;
                this.Log($"'{config.name}': Creating new SpriteAtlas at {outputPath}");
            }
            else
            {
                this.Log($"'{config.name}': Updating existing SpriteAtlas at {outputPath}");
                atlas.Remove(atlas.GetPackables());
            }

            SpriteAtlasPackingSettings packingSettings = atlas.GetPackingSettings();
            packingSettings.enableRotation = config.enableRotation;
            packingSettings.padding = config.padding;
            packingSettings.enableTightPacking = config.enableTightPacking;
            packingSettings.enableAlphaDilation = config.enableAlphaDilation;
            atlas.SetPackingSettings(packingSettings);

            SpriteAtlasTextureSettings textureSettings = atlas.GetTextureSettings();
            textureSettings.readable = config.readWriteEnabled;
            atlas.SetTextureSettings(textureSettings);

            TextureImporterPlatformSettings platformSettings = atlas.GetPlatformSettings(
                "DefaultTexturePlatform"
            );
            if (string.IsNullOrWhiteSpace(platformSettings.name))
            {
                platformSettings.name = "DefaultTexturePlatform";
            }

            platformSettings.overridden = true;
            platformSettings.maxTextureSize = config.maxTextureSize;
            platformSettings.crunchedCompression = config.useCrunchCompression;
            platformSettings.compressionQuality = config.crunchCompressionLevel;
            platformSettings.format = TextureImporterFormat.Automatic;
            platformSettings.textureCompression = config.compression;
            atlas.SetPlatformSettings(platformSettings);

            // Apply per-platform overrides if configured
            if (config.overrideStandalone)
            {
                ApplyPlatformSettings(
                    atlas,
                    "Standalone",
                    config.standaloneMaxTextureSize,
                    config.standaloneCompression,
                    config.standaloneUseCrunchCompression,
                    config.standaloneCrunchCompressionLevel
                );
            }
            if (config.overrideIPhone)
            {
                ApplyPlatformSettings(
                    atlas,
                    "iPhone",
                    config.iPhoneMaxTextureSize,
                    config.iPhoneCompression,
                    config.iPhoneUseCrunchCompression,
                    config.iPhoneCrunchCompressionLevel
                );
            }
            if (config.overrideAndroid)
            {
                ApplyPlatformSettings(
                    atlas,
                    "Android",
                    config.androidMaxTextureSize,
                    config.androidCompression,
                    config.androidUseCrunchCompression,
                    config.androidCrunchCompressionLevel
                );
            }

            // No need to remove null sprites from atlas contents here; we control packables below.

            int removed = config.spritesToPack.RemoveAll(sprite => sprite == null);
            if (removed > 0)
            {
                EditorUtility.SetDirty(config);
            }

            if (config.spritesToPack.Count > 0)
            {
                Object[] spritesToAdd = config.spritesToPack.ToArray<Object>();
                atlas.Add(spritesToAdd);
            }
            else
            {
                this.LogWarn(
                    $"'{config.name}': No sprites in the 'spritesToPack' list. Atlas will be empty."
                );
            }

            if (newAtlas)
            {
                AssetDatabase.CreateAsset(atlas, outputPath);
            }
            else
            {
                EditorUtility.SetDirty(atlas);
            }

            if (refreshAssetsImmediately)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            this.Log(
                $"'{config.name}': Successfully generated/updated at {outputPath}. Sprites included: {config.spritesToPack.Count}."
            );
        }

        private static void ApplyPlatformSettings(
            SpriteAtlas atlas,
            string platformName,
            int maxTextureSize,
            TextureImporterCompression compression,
            bool useCrunch,
            int crunchLevel
        )
        {
            TextureImporterPlatformSettings ps = atlas.GetPlatformSettings(platformName);
            if (string.IsNullOrWhiteSpace(ps.name))
            {
                ps.name = platformName;
            }
            ps.overridden = true;
            ps.maxTextureSize = maxTextureSize;
            ps.textureCompression = compression;
            ps.crunchedCompression = useCrunch;
            ps.compressionQuality = Mathf.Clamp(crunchLevel, 0, 100);
            atlas.SetPlatformSettings(ps);
        }

        internal void PackAllProjectAtlases()
        {
            this.Log(
                $"Starting to pack all Sprite Atlases in the project for target: {EditorUserBuildSettings.activeBuildTarget}"
            );
            SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
            this.Log($"Finished packing all Sprite Atlases.");
            AssetDatabase.Refresh();
        }

        private static void PingAtlas(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return;
            }
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(outputPath);
            if (obj != null)
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
        }

        private static void RevealInExplorer(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return;
            }
            EditorUtility.RevealInFinder(outputPath);
        }

        private void ForceUncompressedSourceSprites(ScriptableSpriteAtlas config)
        {
            if (config == null)
            {
                return;
            }

            List<Sprite> spritesToProcess = config
                .spritesToPack.Where(s => s != null && s.texture != null)
                .ToList();
            if (spritesToProcess.Count == 0)
            {
                this.LogWarn(
                    $"'{config.name}': No valid sprites with textures in the list to modify."
                );
                Utils.EditorUi.Info(
                    "No Sprites",
                    "No valid sprites found in the configuration's list to process."
                );
                return;
            }

            int modifiedCount = 0;
            int errorCount = 0;
            HashSet<string> processedAssetPaths = new();

            List<TextureImporter> importers = new();
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < spritesToProcess.Count; ++i)
                {
                    Sprite sprite = spritesToProcess[i];
                    Utils.EditorUi.ShowProgress(
                        "Modifying Source Sprite Import Settings",
                        $"Processing: {sprite.name} ({i + 1}/{spritesToProcess.Count})",
                        (float)(i + 1) / spritesToProcess.Count
                    );

                    string assetPath = AssetDatabase.GetAssetPath(sprite.texture);
                    if (string.IsNullOrWhiteSpace(assetPath))
                    {
                        this.LogWarn(
                            $"Could not find asset path for sprite's texture: {sprite.name}. Skipping."
                        );
                        errorCount++;
                        continue;
                    }

                    if (!processedAssetPaths.Add(assetPath))
                    {
                        continue;
                    }

                    TextureImporter importer =
                        AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer == null)
                    {
                        this.LogWarn(
                            $"Could not get TextureImporter for asset: {assetPath} (from sprite: {sprite.name}). Skipping."
                        );
                        errorCount++;
                        continue;
                    }

                    bool settingsActuallyModified = false;

                    if (importer.crunchedCompression)
                    {
                        importer.crunchedCompression = false;
                        settingsActuallyModified = true;
                    }

                    if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        settingsActuallyModified = true;
                    }

                    TextureImporterPlatformSettings platformSettings =
                        importer.GetDefaultPlatformTextureSettings();
                    bool platformSettingsChangedThisTime = false;
                    TextureImporterFormat targetFormat = importer.DoesSourceTextureHaveAlpha()
                        ? TextureImporterFormat.RGBA32
                        : TextureImporterFormat.RGB24;

                    if (platformSettings.format != targetFormat)
                    {
                        platformSettings.format = targetFormat;
                        platformSettingsChangedThisTime = true;
                    }
                    if (platformSettings.crunchedCompression)
                    {
                        platformSettings.crunchedCompression = false;
                        platformSettingsChangedThisTime = true;
                    }
                    if (platformSettings.compressionQuality != 100)
                    {
                        platformSettings.compressionQuality = 100;
                        platformSettingsChangedThisTime = true;
                    }

                    if (platformSettingsChangedThisTime || !platformSettings.overridden)
                    {
                        platformSettings.overridden = true;
                        importer.SetPlatformTextureSettings(platformSettings);
                        settingsActuallyModified = true;
                    }

                    if (settingsActuallyModified)
                    {
                        importer.SaveAndReimport();
                        importers.Add(importer);
                        modifiedCount++;
                        this.Log(
                            $"Set import settings for texture: {assetPath} (from sprite: {sprite.name}) to uncompressed ({targetFormat})."
                        );
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                Utils.EditorUi.ClearProgress();
            }

            foreach (TextureImporter importer in importers)
            {
                importer.SaveAndReimport();
            }

            if (modifiedCount > 0 || errorCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            string summaryMessage =
                $"Finished processing source sprite textures for '{config.name}'.\n"
                + $"Successfully modified importers for: {modifiedCount} textures.\n"
                + $"Errors/Skipped duplicates: {errorCount + (spritesToProcess.Count - processedAssetPaths.Count)}.";
            this.Log($"{summaryMessage}");
        }

        private void SyncListToScanResult(ScriptableSpriteAtlas config, ScanResult result)
        {
            if (config == null || result == null || !result.hasScanned)
            {
                return;
            }

            SerializedObject so = new(config);
            SerializedProperty spritesListProp = so.FindProperty(
                nameof(ScriptableSpriteAtlas.spritesToPack)
            );

            Undo.RecordObject(config, "Sync Sprites To Scan Result");

            // Build the target set = (current ∪ toAdd) \ toRemove
            using PooledResource<HashSet<Sprite>> targetSetRes = Buffers<Sprite>.HashSet.Get();
            HashSet<Sprite> targetSet = targetSetRes.resource;

            for (int i = 0; i < spritesListProp.arraySize; ++i)
            {
                Object o = spritesListProp.GetArrayElementAtIndex(i).objectReferenceValue;
                if (o is Sprite s && s != null)
                {
                    targetSet.Add(s);
                }
            }

            foreach (Sprite s in result.spritesToAdd)
            {
                if (s != null)
                {
                    targetSet.Add(s);
                }
            }
            foreach (Sprite s in result.spritesToRemove)
            {
                if (s != null)
                {
                    targetSet.Remove(s);
                }
            }

            // Rewrite list to match target set
            while (spritesListProp.arraySize > 0)
            {
                spritesListProp.DeleteArrayElementAtIndex(spritesListProp.arraySize - 1);
            }
            int index = 0;
            foreach (Sprite s in targetSet)
            {
                spritesListProp.InsertArrayElementAtIndex(index);
                spritesListProp.GetArrayElementAtIndex(index).objectReferenceValue = s;
                index++;
            }

            so.ApplyModifiedProperties();
            config.spritesToPack.SortByName();
            EditorUtility.SetDirty(config);
            this.Log(
                $"'{config.name}': Synchronized sprite list to scan result. Now contains {config.spritesToPack.Count} sprites."
            );

            // Refresh scan to reflect new state
            result.spritesToAdd.Clear();
            result.spritesToRemove.Clear();
            ScanFoldersForConfig(config);
            Repaint();
        }
    }
#endif
}

namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Core.Extension;
    using Core.Helper;
    using UnityEditor;
    using UnityEditor.U2D;
    using UnityEngine;
    using UnityEngine.U2D;
    using Object = UnityEngine.Object;

    public sealed class ScriptableSpriteAtlasEditor : EditorWindow
    {
        private readonly Dictionary<ScriptableSpriteAtlas, SerializedObject> _serializedConfigs =
            new();
        private List<ScriptableSpriteAtlas> _atlasConfigs = new();
        private Vector2 _scrollPosition;

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

        private void LoadAtlasConfigs()
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

            if (GUILayout.Button("Generate/Update All .spriteatlas Assets", GUILayout.Height(40)))
            {
                GenerateAllAtlases();
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
                    if (GUILayout.Button("Add New Source Folder Entry"))
                    {
                        string folderPath = EditorUtility.OpenFolderPanel(
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
                                EditorUtility.DisplayDialog(
                                    "Invalid Folder",
                                    "The selected folder must be within the project's 'Assets' directory.",
                                    "OK"
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
                        && EditorUtility.DisplayDialog(
                            "Force Uncompressed Source Sprites",
                            $"This will modify the import settings of {validSpriteCount} source sprites currently in the '{config.name}' list.\n\n"
                                + "- Crunch compression will be disabled.\n"
                                + "- Texture format for the 'Default' platform will be set to uncompressed (RGBA32 or RGB24).\n\n"
                                + "This action modifies source asset import settings and may require re-packing atlases. Are you sure?",
                            "Yes, Modify Source Sprites",
                            "Cancel"
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
                        && EditorUtility.DisplayDialog(
                            $"Generate Atlas: {config.name}",
                            $"This will create or update '{config.outputSpriteAtlasName}.spriteatlas'. Continue?",
                            "Yes",
                            "No"
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

                string[] textureGuids = AssetDatabase.FindAssets(
                    "t:Texture2D",
                    new[] { entry.folderPath }
                );
                foreach (string guid in textureGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = Path.GetFileName(assetPath);

                    bool matchesAllRegexesInEntry = true;
                    List<string> activeRegexPatterns = entry
                        .regexes.Where(r => !string.IsNullOrWhiteSpace(r))
                        .ToList();

                    if (
                        entry.selectionMode.HasFlagNoAlloc(SpriteSelectionMode.Regex)
                        && activeRegexPatterns is { Count: > 0 }
                    )
                    {
                        foreach (string regexPattern in activeRegexPatterns)
                        {
                            try
                            {
                                if (!Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase))
                                {
                                    matchesAllRegexesInEntry = false;
                                    break;
                                }
                            }
                            catch (ArgumentException ex)
                            {
                                this.LogError(
                                    $"'{config.name}', Folder '{entry.folderPath}': Invalid Regex pattern '{regexPattern}': {ex.Message}. File '{fileName}' will not be matched by this entry due to this error."
                                );
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
                            switch (entry.labelSelectionMode)
                            {
                                case LabelSelectionMode.All:
                                {
                                    matchesAllTagsInEntry = labels.All(label =>
                                        entry.labels.Contains(label)
                                    );
                                    break;
                                }
                                case LabelSelectionMode.AnyOf:
                                {
                                    matchesAllTagsInEntry = labels.Any(label =>
                                        entry.labels.Contains(label)
                                    );
                                    break;
                                }
                                default:
                                {
                                    throw new InvalidEnumArgumentException(
                                        nameof(entry.labelSelectionMode),
                                        (int)entry.labelSelectionMode,
                                        typeof(LabelSelectionMode)
                                    );
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
                                throw new InvalidEnumArgumentException(
                                    nameof(entry.regexAndTagLogic),
                                    (int)entry.regexAndTagLogic,
                                    typeof(SpriteSelectionBooleanLogic)
                                );
                            }
                        }
                    }

                    if (allMatch)
                    {
                        foreach (
                            Object asset in AssetDatabase
                                .LoadAllAssetsAtPath(assetPath)
                                .Concat(AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath))
                        )
                        {
                            if (asset is Sprite spriteAsset && spriteAsset != null)
                            {
                                foundSpritesInFolders.Add(spriteAsset);
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

        private void GenerateAllAtlases()
        {
            if (_atlasConfigs.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Configurations",
                    "No ScriptableSpriteAtlas configurations found to generate.",
                    "OK"
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
                    EditorUtility.DisplayProgressBar(
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
                EditorUtility.ClearProgressBar();
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

            Object[] spritesToAdd = config.spritesToPack.Where(s => s != null).ToArray<Object>();
            if (spritesToAdd.Length > 0)
            {
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
                $"'{config.name}': Successfully generated/updated at {outputPath}. Sprites included: {spritesToAdd.Length}."
            );
        }

        private void PackAllProjectAtlases()
        {
            this.Log(
                $"Starting to pack all Sprite Atlases in the project for target: {EditorUserBuildSettings.activeBuildTarget}"
            );
            SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
            this.Log($"Finished packing all Sprite Atlases.");
            AssetDatabase.Refresh();
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
                EditorUtility.DisplayDialog(
                    "No Sprites",
                    "No valid sprites found in the configuration's list to process.",
                    "OK"
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
                    EditorUtility.DisplayProgressBar(
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
                EditorUtility.ClearProgressBar();
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
    }
#endif
}

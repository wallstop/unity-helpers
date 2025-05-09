// ReSharper disable CompareOfFloatsByEqualityOperator
namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using Core.Attributes;
    using Core.Extension;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using Utils;
    using Object = UnityEngine.Object;

    [Serializable]
    public sealed class SpriteSettings
    {
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
    }

    [CustomPropertyDrawer(typeof(SpriteSettings))]
    public class SpriteSettingsDrawer : PropertyDrawer
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

                    using (new GUIIndentScope())
                    {
                        EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none, true);
                    }

                    currentRect.y += valueRect.height + EditorGUIUtility.standardVerticalSpacing;
                }
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

            if (totalHeight > 0)
            {
                totalHeight -= EditorGUIUtility.standardVerticalSpacing;
            }

            return totalHeight;
        }
    }

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
            EditorGUILayout.PropertyField(
                _directoriesProp,
                new GUIContent("Scan Directories"),
                true
            );
            if (GUILayout.Button("Add Directory via Browser"))
            {
                AddDirectory();
            }

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

            EditorGUILayout.EndScrollView();

            _serializedObject.ApplyModifiedProperties();
        }

        private void AddDirectory()
        {
            string path = EditorUtility.OpenFolderPanel("Select Directory", "Assets", "");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (path.StartsWith(Application.dataPath, StringComparison.Ordinal))
            {
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                Object folderAsset = AssetDatabase.LoadAssetAtPath<Object>(relativePath);
                if (folderAsset != null)
                {
                    _directoriesProp.serializedObject.Update();
                    bool alreadyExists = false;
                    for (int i = 0; i < _directoriesProp.arraySize; i++)
                    {
                        if (
                            _directoriesProp.GetArrayElementAtIndex(i).objectReferenceValue
                            == folderAsset
                        )
                        {
                            alreadyExists = true;
                            break;
                        }
                    }

                    if (!alreadyExists)
                    {
                        int newIndex = _directoriesProp.arraySize;
                        _directoriesProp.InsertArrayElementAtIndex(newIndex);
                        _directoriesProp.GetArrayElementAtIndex(newIndex).objectReferenceValue =
                            folderAsset;
                        this.Log($"Added directory: {relativePath}");
                    }
                    else
                    {
                        this.LogWarn($"Directory already in list: {relativePath}");
                    }
                    _directoriesProp.serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    this.LogError(
                        $"Could not load asset at path: {relativePath}. Is it a valid folder within Assets?"
                    );
                }
            }
            else
            {
                this.LogError(
                    $"Selected folder must be inside the project's Assets folder. Path selected: {path}"
                );
            }
        }

        private List<(string fullFilePath, string relativePath)> GetTargetSpritePaths()
        {
            HashSet<string> uniqueRelativePaths = new(StringComparer.OrdinalIgnoreCase);
            List<Object> validDirectories = new();

            for (int i = 0; i < _directoriesProp.arraySize; i++)
            {
                Object dir = _directoriesProp.GetArrayElementAtIndex(i).objectReferenceValue;
                if (dir != null)
                {
                    validDirectories.Add(dir);
                }
            }

            HashSet<string> uniqueDirectoryPaths = new(StringComparer.OrdinalIgnoreCase);
            foreach (
                string assetPath in validDirectories
                    .Select(AssetDatabase.GetAssetPath)
                    .Where(assetPath => !string.IsNullOrWhiteSpace(assetPath))
            )
            {
                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    _ = uniqueDirectoryPaths.Add(assetPath);
                }
                else
                {
                    this.LogWarn($"Item '{assetPath}' is not a valid directory. Skipping.");
                }
            }

            List<(string fullFilePath, string relativePath)> filePaths = new();
            Queue<string> directoriesToCheck = new(uniqueDirectoryPaths);
            HashSet<string> processedFullPaths = new(StringComparer.OrdinalIgnoreCase);

            while (directoriesToCheck.TryDequeue(out string relativeDirectoryPath))
            {
                string fullDirectoryPath = Path.GetFullPath(relativeDirectoryPath);

                if (!Directory.Exists(fullDirectoryPath))
                {
                    this.LogWarn($"Directory path does not exist: {fullDirectoryPath}. Skipping.");
                    continue;
                }

                try
                {
                    foreach (string fullFilePath in Directory.EnumerateFiles(fullDirectoryPath))
                    {
                        string fileExtension = Path.GetExtension(fullFilePath);
                        bool extensionMatch = false;
                        for (int i = 0; i < _spriteFileExtensionsProp.arraySize; i++)
                        {
                            if (
                                string.Equals(
                                    fileExtension,
                                    _spriteFileExtensionsProp.GetArrayElementAtIndex(i).stringValue,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            {
                                extensionMatch = true;
                                break;
                            }
                        }

                        if (!extensionMatch)
                        {
                            continue;
                        }

                        if (processedFullPaths.Add(fullFilePath))
                        {
                            string relativeFilePath =
                                "Assets"
                                + fullFilePath
                                    .Substring(Application.dataPath.Length)
                                    .Replace("\\", "/");
                            if (uniqueRelativePaths.Add(relativeFilePath))
                            {
                                filePaths.Add((fullFilePath, relativeFilePath));
                            }
                        }
                    }

                    foreach (
                        string subDirectoryFullPath in Directory.EnumerateDirectories(
                            fullDirectoryPath
                        )
                    )
                    {
                        string relativeSubDirectory =
                            "Assets"
                            + subDirectoryFullPath
                                .Substring(Application.dataPath.Length)
                                .Replace("\\", "/");
                        directoriesToCheck.Enqueue(relativeSubDirectory);
                    }
                }
                catch (Exception e)
                {
                    this.LogError(
                        $"Error enumerating directory '{fullDirectoryPath}': {e.Message}"
                    );
                }
            }

            for (int i = 0; i < _spritesProp.arraySize; i++)
            {
                Sprite sprite =
                    _spritesProp.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                if (sprite == null)
                {
                    continue;
                }

                string relativePath = AssetDatabase.GetAssetPath(sprite);
                if (!string.IsNullOrWhiteSpace(relativePath))
                {
                    if (uniqueRelativePaths.Add(relativePath))
                    {
                        string fullPath = Path.GetFullPath(relativePath);
                        if (processedFullPaths.Add(fullPath))
                        {
                            filePaths.Add((fullPath, relativePath));
                        }
                    }
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

            List<SpriteSettings> currentSettings = new();
            for (int i = 0; i < _spriteSettingsProp.arraySize; i++)
            {
                if (_serializedObject.targetObject is SpriteSettingsApplierWindow windowInstance)
                {
                    if (i < windowInstance.spriteSettings.Count)
                    {
                        currentSettings.Add(windowInstance.spriteSettings[i]);
                    }
                }
            }

            if (currentSettings.Count != _spriteSettingsProp.arraySize)
            {
                this.LogWarn(
                    $"Mismatch between SerializedProperty size and actual list size for spriteSettings. Stats might be inaccurate."
                );
            }

            for (int i = 0; i < targetFiles.Count; i++)
            {
                (string _, string relativePath) = targetFiles[i];
                EditorUtility.DisplayProgressBar(
                    "Calculating Stats",
                    $"Checking '{Path.GetFileName(relativePath)}' ({i + 1}/{_totalSpritesToProcess})",
                    (float)(i + 1) / _totalSpritesToProcess
                );

                if (WillTextureSettingsChange(relativePath, currentSettings))
                {
                    _spritesThatWillChange++;
                }
            }

            EditorUtility.ClearProgressBar();
            this.Log(
                $"Calculation complete. Sprites to process: {_totalSpritesToProcess}, Sprites that will change: {_spritesThatWillChange}"
            );
        }

        private void ApplySettings()
        {
            List<(string fullFilePath, string relativePath)> targetFiles = GetTargetSpritePaths();
            int spriteCount = 0;
            List<TextureImporter> updatedImporters = new();

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
                foreach ((string _, string filePath) in targetFiles)
                {
                    if (
                        TryUpdateTextureSettings(
                            filePath,
                            currentSettings,
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

                for (int i = 0; i < updatedImporters.Count; i++)
                {
                    TextureImporter importer = updatedImporters[i];
                    EditorUtility.DisplayProgressBar(
                        "Updating Sprite Settings",
                        $"Processing '{Path.GetFileName(importer.assetPath)}' ({i + 1}/{updatedImporters.Count})",
                        (float)(i + 1) / updatedImporters.Count
                    );
                    try
                    {
                        importer.SaveAndReimport();
                    }
                    catch (Exception ex)
                    {
                        this.LogError(
                            $"Failed to save and reimport asset '{importer.assetPath}': {ex.Message}"
                        );
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                this.Log($"Processed {spriteCount} sprites.");
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

        private static SpriteSettings FindMatchingSettings(
            string filePath,
            List<SpriteSettings> settingsProfiles
        )
        {
            string fileName = Path.GetFileName(filePath);

            foreach (SpriteSettings settings in settingsProfiles)
            {
                if (
                    !string.IsNullOrWhiteSpace(settings.name)
                    && fileName.Contains(settings.name, StringComparison.OrdinalIgnoreCase)
                )
                {
                    return settings;
                }
            }

            foreach (SpriteSettings settings in settingsProfiles)
            {
                if (string.IsNullOrWhiteSpace(settings.name))
                {
                    return settings;
                }
            }
            return null;
        }

        private bool WillTextureSettingsChange(
            string filePath,
            List<SpriteSettings> settingsProfiles
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

            SpriteSettings spriteData = FindMatchingSettings(filePath, settingsProfiles);
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

            TextureImporterSettings settings = new();
            textureImporter.ReadTextureSettings(settings);
            if (spriteData.applyPivot)
            {
                changed |= settings.spriteAlignment != (int)SpriteAlignment.Custom;
            }

            if (spriteData.applyAlphaIsTransparency)
            {
                changed |= settings.alphaIsTransparency != spriteData.alphaIsTransparency;
            }

            if (spriteData.applyReadWriteEnabled)
            {
                changed |= settings.readable != spriteData.readWriteEnabled;
            }

            if (spriteData.applySpriteMode)
            {
                changed |= settings.spriteMode != (int)spriteData.spriteMode;
            }

            if (spriteData.applyExtrudeEdges)
            {
                changed |= settings.spriteExtrude != spriteData.extrudeEdges;
            }

            if (spriteData.applyWrapMode)
            {
                changed |= settings.wrapMode != spriteData.wrapMode;
            }

            if (spriteData.applyFilterMode)
            {
                changed |= settings.filterMode != spriteData.filterMode;
            }

            return changed;
        }

        private bool TryUpdateTextureSettings(
            string filePath,
            List<SpriteSettings> settingsProfiles,
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

            SpriteSettings spriteData = FindMatchingSettings(filePath, settingsProfiles);

            if (spriteData == null)
            {
                this.LogWarn($"No matching SpriteSettings profile found for: {filePath}");
                return false;
            }

            bool changed = false;
            bool settingsChanged = false;
            TextureImporterSettings settings = new();
            textureImporter.ReadTextureSettings(settings);

            if (spriteData.applyPixelsPerUnit)
            {
                if (textureImporter.spritePixelsPerUnit != spriteData.pixelsPerUnit)
                {
                    textureImporter.spritePixelsPerUnit = spriteData.pixelsPerUnit;
                    changed = true;
                }
            }

            if (spriteData.applyPivot)
            {
                if (textureImporter.spritePivot != spriteData.pivot)
                {
                    textureImporter.spritePivot = spriteData.pivot;
                    changed = true;
                }

                if (settings.spriteAlignment != (int)SpriteAlignment.Custom)
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
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
                if (settings.alphaIsTransparency != spriteData.alphaIsTransparency)
                {
                    settings.alphaIsTransparency = spriteData.alphaIsTransparency;
                    settingsChanged = true;
                }
            }

            if (spriteData.applyReadWriteEnabled)
            {
                if (settings.readable != spriteData.readWriteEnabled)
                {
                    settings.readable = spriteData.readWriteEnabled;
                    settingsChanged = true;
                }
            }

            if (spriteData.applySpriteMode)
            {
                if (settings.spriteMode != (int)spriteData.spriteMode)
                {
                    settings.spriteMode = (int)spriteData.spriteMode;
                    settingsChanged = true;
                }
            }

            if (spriteData.applyExtrudeEdges)
            {
                if (settings.spriteExtrude != spriteData.extrudeEdges)
                {
                    settings.spriteExtrude = spriteData.extrudeEdges;
                    settingsChanged = true;
                }
            }

            if (spriteData.applyWrapMode)
            {
                if (settings.wrapMode != spriteData.wrapMode)
                {
                    settings.wrapMode = spriteData.wrapMode;
                    settingsChanged = true;
                }
            }

            if (spriteData.applyFilterMode)
            {
                if (settings.filterMode != spriteData.filterMode)
                {
                    settings.filterMode = spriteData.filterMode;
                    settingsChanged = true;
                }
            }

            if (settingsChanged)
            {
                textureImporter.SetTextureSettings(settings);
            }

            return changed || settingsChanged;
        }
    }
#endif
}

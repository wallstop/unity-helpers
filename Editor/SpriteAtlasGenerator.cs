namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Core.Extension;
    using Extensions;
    using UnityEditor;
    using UnityEditor.U2D;
    using UnityEngine;
    using UnityEngine.U2D;
    using Object = UnityEngine.Object;

    public sealed class SpriteAtlasGenerator : EditorWindow
    {
        private const string Name = "Sprite Atlas Generator";
        private const string DefaultPlatformName = "DefaultTexturePlatform";

        [SerializeField]
        private Object[] _sourceFolders = Array.Empty<Object>();

        [SerializeField]
        private string _nameRegex = ".*";

        [SerializeField]
        private string _outputFolder = "Assets/Sprites/Atlases";

        [SerializeField]
        private int _crunchCompression = -1;

        [SerializeField]
        private TextureImporterCompression _compressionLevel =
            TextureImporterCompression.Compressed;

        private int _matchCount;
        private int _totalCount;

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/" + Name)]
        public static void ShowWindow() => GetWindow<SpriteAtlasGenerator>("Atlas Generator");

        private void OnEnable()
        {
            if (_sourceFolders is { Length: > 0 })
            {
                return;
            }

            Object defaultFolder = AssetDatabase.LoadAssetAtPath<Object>("Assets/Sprites");
            if (defaultFolder != null)
            {
                _sourceFolders = new[] { defaultFolder };
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Source Folders", EditorStyles.boldLabel);
            SerializedObject so = new(this);
            so.Update();
            EditorGUILayout.PropertyField(so.FindProperty(nameof(_sourceFolders)), true);
            so.ApplyModifiedProperties();
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Sprite Name Regex");
            _nameRegex = EditorGUILayout.TextField(_nameRegex);

            GUILayout.Space(4);

            if (GUILayout.Button("Calculate Matches"))
            {
                UpdateMatchCounts();
            }

            EditorGUILayout.LabelField(
                $"Matches: {_matchCount}    Non-matches: {_totalCount - _matchCount}"
            );

            GUILayout.Space(4);
            EditorGUILayout.LabelField("Crunch Compression");
            _crunchCompression = EditorGUILayout.IntField(_crunchCompression);

            GUILayout.Space(4);
            EditorGUILayout.LabelField("Compression Level");
            _compressionLevel = (TextureImporterCompression)
                EditorGUILayout.EnumPopup(_compressionLevel);

            GUILayout.Space(12);
            EditorGUILayout.LabelField("Atlas Output Folder");
            EditorGUILayout.LabelField(_outputFolder, EditorStyles.textField);
            if (GUILayout.Button("Select Output Folder"))
            {
                string absPath = EditorUtility.OpenFolderPanel(
                    "Select Atlas Output Folder",
                    Application.dataPath,
                    ""
                );
                if (!string.IsNullOrEmpty(absPath))
                {
                    if (absPath.StartsWith(Application.dataPath, StringComparison.Ordinal))
                    {
                        _outputFolder = "Assets" + absPath.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(
                            "Invalid Folder",
                            "Please select a folder inside the project's Assets directory.",
                            "OK"
                        );
                    }
                }
            }

            GUILayout.Space(12);
            if (GUILayout.Button("Generate Atlases"))
            {
                GenerateAtlases();
            }
        }

        private void UpdateMatchCounts()
        {
            _totalCount = 0;
            _matchCount = 0;
            Regex regex;
            try
            {
                regex = new Regex(_nameRegex);
            }
            catch (ArgumentException ex)
            {
                this.LogError($"Invalid Regex pattern: '{_nameRegex}'. Error: {ex.Message}");
                Repaint();
                return;
            }

            foreach (Object obj in _sourceFolders)
            {
                if (obj == null)
                {
                    continue;
                }

                string folderPath = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
                {
                    this.LogWarn($"Skipping invalid or null source folder entry.");
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    if (assets == null)
                    {
                        continue;
                    }

                    IEnumerable<Sprite> sprites = assets.OfType<Sprite>();
                    foreach (Sprite sp in sprites)
                    {
                        if (sp == null)
                        {
                            continue;
                        }

                        _totalCount++;
                        if (regex.IsMatch(sp.name))
                        {
                            _matchCount++;
                        }
                    }
                }
            }

            Repaint();
        }

        private void GenerateAtlases()
        {
            List<SpriteAtlas> atlases = new();
            int processed = 0;
            try
            {
                EditorUtility.DisplayProgressBar(Name, "Initializing...", 0f);
                if (string.IsNullOrWhiteSpace(_outputFolder))
                {
                    this.LogError($"Invalid output folder.");
                    EditorUtility.ClearProgressBar();
                    return;
                }

                if (
                    _sourceFolders == null
                    || _sourceFolders.Length == 0
                    || _sourceFolders.All(f => f == null)
                )
                {
                    this.LogError($"No valid source folders specified.");
                    EditorUtility.ClearProgressBar();
                    return;
                }

                if (!AssetDatabase.IsValidFolder(_outputFolder))
                {
                    try
                    {
                        string parent = Path.GetDirectoryName(_outputFolder);
                        string newFolderName = Path.GetFileName(_outputFolder);
                        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(newFolderName))
                        {
                            this.LogError($"Output folder path '{_outputFolder}' is invalid.");
                            EditorUtility.ClearProgressBar();
                            return;
                        }
                        AssetDatabase.CreateFolder(parent, newFolderName);
                        AssetDatabase.Refresh();
                        if (!AssetDatabase.IsValidFolder(_outputFolder))
                        {
                            this.LogError($"Failed to create output folder: '{_outputFolder}'");
                            EditorUtility.ClearProgressBar();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.LogError(
                            $"Error creating output folder '{_outputFolder}': {ex.Message}"
                        );
                        EditorUtility.ClearProgressBar();
                        return;
                    }
                }

                EditorUtility.DisplayProgressBar(Name, "Deleting old atlases...", 0.1f);
                string[] existing = AssetDatabase
                    .FindAssets("t:SpriteAtlas", new[] { _outputFolder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToArray();

                if (existing.Length > 0)
                {
                    List<string> failedPaths = new();
                    AssetDatabase.DeleteAssets(existing, failedPaths);
                    if (failedPaths.Any())
                    {
                        this.LogWarn(
                            $"Failed to delete {failedPaths.Count} atlases:\n{string.Join("\n", failedPaths)}"
                        );
                    }
                    AssetDatabase.Refresh();
                }

                EditorUtility.DisplayProgressBar(Name, "Scanning sprites...", 0.25f);
                Regex regex;
                try
                {
                    regex = new(_nameRegex);
                }
                catch (ArgumentException ex)
                {
                    this.LogError(
                        $"Invalid Regex pattern for generation: '{_nameRegex}'. Error: {ex.Message}"
                    );
                    EditorUtility.ClearProgressBar();
                    return;
                }
                Dictionary<string, List<Sprite>> groups = new(StringComparer.Ordinal);

                float sourceFolderProgressIncrement = 0.15f / _sourceFolders.Length;
                float currentProgress = 0.25f;

                foreach (Object sourceDirectory in _sourceFolders)
                {
                    if (sourceDirectory == null)
                    {
                        continue;
                    }

                    string folderPath = AssetDatabase.GetAssetPath(sourceDirectory);
                    if (!AssetDatabase.IsValidFolder(folderPath))
                    {
                        this.LogWarn(
                            $"Skipping invalid source folder during generation: '{folderPath}'"
                        );
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(
                        Name,
                        $"Scanning folder '{folderPath}'...",
                        currentProgress
                    );

                    foreach (
                        string assetGuid in AssetDatabase.FindAssets(
                            "t:Sprite",
                            new[] { folderPath }
                        )
                    )
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                        if (string.IsNullOrEmpty(assetPath))
                        {
                            continue;
                        }

                        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                        if (allAssets == null)
                        {
                            continue;
                        }

                        foreach (Sprite sub in allAssets.OfType<Sprite>())
                        {
                            if (sub == null)
                            {
                                continue;
                            }

                            string assetName = sub.name;
                            if (!regex.IsMatch(assetName))
                            {
                                continue;
                            }

                            Match match = Regex.Match(assetName, @"^(.+?)(?:_\d+)?$");
                            string key = match.Success ? match.Groups[1].Value : assetName;
                            groups.GetOrAdd(key).Add(sub);
                        }
                    }
                    currentProgress += sourceFolderProgressIncrement;
                }

                const int atlasSize = 8192;
                const long budget = (long)atlasSize * atlasSize;
                int totalChunks = 0;
                Dictionary<string, List<List<Sprite>>> groupChunks = new();

                EditorUtility.DisplayProgressBar(Name, "Calculating chunks...", 0.4f);

                foreach (KeyValuePair<string, List<Sprite>> kv in groups)
                {
                    List<Sprite> sprites = kv
                        .Value.OrderByDescending(s => s.rect.width * s.rect.height)
                        .ToList();
                    List<List<Sprite>> chunks = new();
                    List<Sprite> current = new();
                    long currentArea = 0;
                    foreach (Sprite sprite in sprites)
                    {
                        if (sprite == null || sprite.rect.width <= 0 || sprite.rect.height <= 0)
                        {
                            this.LogWarn(
                                $"Skipping invalid sprite '{sprite?.name ?? "null"}' in group '{kv.Key}'."
                            );
                            continue;
                        }

                        long area = (long)(sprite.rect.width * sprite.rect.height);
                        if (area > budget)
                        {
                            this.LogWarn(
                                $"Sprite '{sprite.name}' ({sprite.rect.width}x{sprite.rect.height}) is larger than max atlas area budget and will be placed in its own atlas chunk."
                            );
                            continue;
                        }
                        if (currentArea + area <= budget && current.Count < 2000)
                        {
                            current.Add(sprite);
                            currentArea += area;
                        }
                        else
                        {
                            if (current.Count > 1)
                            {
                                chunks.Add(current);
                            }
                            current = new List<Sprite> { sprite };
                            currentArea = area;
                        }
                    }

                    if (current.Count > 1)
                    {
                        chunks.Add(current);
                    }

                    if (chunks.Count > 0)
                    {
                        groupChunks[kv.Key] = chunks;
                        totalChunks += chunks.Count;
                    }
                }

                if (totalChunks == 0)
                {
                    this.Log(
                        $"No sprites matched the regex '{_nameRegex}' or formed valid chunks."
                    );
                    EditorUtility.ClearProgressBar();
                    return;
                }

                int chunkIndex = 0;
                float atlasCreationProgressStart = 0.45f;
                float atlasCreationProgressRange = 0.5f;

                foreach ((string prefix, List<List<Sprite>> chunks) in groupChunks)
                {
                    for (int i = 0; i < chunks.Count; i++)
                    {
                        List<Sprite> chunk = chunks[i];
                        if (chunk == null || chunk.Count == 0)
                        {
                            continue;
                        }

                        float progress =
                            atlasCreationProgressStart
                            + atlasCreationProgressRange * (chunkIndex / (float)totalChunks);

                        string atlasName = chunks.Count > 1 ? $"{prefix}_{i}" : prefix;
                        EditorUtility.DisplayProgressBar(
                            Name,
                            $"Creating atlas '{atlasName}' ({i + 1}/{chunks.Count})... Sprites: {chunk.Count}",
                            progress
                        );

                        SpriteAtlas atlas = new();
                        atlases.Add(atlas);

                        SpriteAtlasPackingSettings packingSettings = atlas.GetPackingSettings();
                        packingSettings.enableTightPacking = true;
                        packingSettings.padding = 4;
                        packingSettings.enableRotation = false;
                        atlas.SetPackingSettings(packingSettings);

                        SpriteAtlasTextureSettings textureSettings = atlas.GetTextureSettings();
                        textureSettings.generateMipMaps = false;
                        textureSettings.filterMode = FilterMode.Bilinear;
                        textureSettings.readable = false;
                        atlas.SetTextureSettings(textureSettings);

                        TextureImporterPlatformSettings platformSettings =
                            atlas.GetPlatformSettings(DefaultPlatformName);

                        if (platformSettings == null)
                        {
                            platformSettings = new TextureImporterPlatformSettings();
                            platformSettings.name = DefaultPlatformName;
                            this.LogWarn(
                                $"Could not get default platform settings for {atlasName}. Creating new default."
                            );
                        }

                        platformSettings.overridden = true;
                        platformSettings.maxTextureSize = atlasSize;
                        platformSettings.textureCompression = _compressionLevel;
                        platformSettings.format = TextureImporterFormat.Automatic;

                        if (_crunchCompression is >= 0 and <= 100)
                        {
                            platformSettings.crunchedCompression = true;
                            platformSettings.compressionQuality = _crunchCompression;
                        }
                        else
                        {
                            if (100 < _crunchCompression)
                            {
                                this.LogWarn(
                                    $"Invalid crunch compression: {_crunchCompression}. Using default (off)."
                                );
                            }
                            platformSettings.crunchedCompression = false;
                            platformSettings.compressionQuality = 50;
                        }

                        atlas.SetPlatformSettings(platformSettings);

                        Object[] validSprites = chunk
                            .Where(s => s != null)
                            .Select(sprite => sprite as Object)
                            .ToArray();
                        if (validSprites.Length == 0)
                        {
                            this.LogWarn(
                                $"Skipping atlas '{atlasName}' as it contained no valid sprites after filtering."
                            );
                            atlases.Remove(atlas);
                        }
                        else
                        {
                            atlas.Add(validSprites);
                            atlas.SetIncludeInBuild(true);
                            string path = Path.Combine(_outputFolder, atlasName + ".spriteatlas");
                            path = AssetDatabase.GenerateUniqueAssetPath(path);
                            AssetDatabase.CreateAsset(atlas, path);
                            processed++;
                        }
                        chunkIndex++;
                    }
                }

                if (processed > 0)
                {
                    EditorUtility.DisplayProgressBar(Name, "Saving assets...", 0.95f);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    EditorUtility.DisplayProgressBar(Name, "Packing atlases...", 0.97f);
                    SpriteAtlasUtility.PackAllAtlases(
                        EditorUserBuildSettings.activeBuildTarget,
                        false
                    );

                    bool anyChanged = false;
                    EditorUtility.DisplayProgressBar(Name, "Optimizing atlas sizes...", 0.98f);
                    foreach (
                        SpriteAtlas atlas in atlases
                            .Select(AssetDatabase.GetAssetPath)
                            .Where(p => !string.IsNullOrEmpty(p))
                            .Select(AssetDatabase.LoadAssetAtPath<SpriteAtlas>)
                            .Where(a => a != null)
                    )
                    {
                        Texture2D preview = atlas.GetPreviewTexture();
                        if (preview == null)
                        {
                            continue;
                        }

                        TextureImporterPlatformSettings platformSettings =
                            atlas.GetPlatformSettings(DefaultPlatformName);
                        if (platformSettings is not { overridden: true })
                        {
                            continue;
                        }

                        int actualWidth = preview.width;
                        int actualHeight = preview.height;
                        int newMaxSize = Mathf.Max(
                            Mathf.NextPowerOfTwo(actualWidth),
                            Mathf.NextPowerOfTwo(actualHeight)
                        );
                        newMaxSize = Mathf.Clamp(newMaxSize, 32, atlasSize);

                        if (newMaxSize < platformSettings.maxTextureSize)
                        {
                            this.Log(
                                $"Optimizing atlas '{atlas.name}' max size from {platformSettings.maxTextureSize} to {newMaxSize}"
                            );
                            platformSettings.maxTextureSize = newMaxSize;
                            atlas.SetPlatformSettings(platformSettings);
                            EditorUtility.SetDirty(atlas);
                            anyChanged = true;
                        }
                    }

                    if (anyChanged)
                    {
                        EditorUtility.DisplayProgressBar(Name, "Saving optimizations...", 0.99f);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
            }
            catch (Exception e)
            {
                this.LogError($"An unexpected error occurred during atlas generation.", e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (processed > 0)
            {
                this.Log(
                    $"[SpriteAtlasGenerator] Successfully created or updated {processed} atlases in '{_outputFolder}'."
                );
            }
            else
            {
                this.Log(
                    $"[SpriteAtlasGenerator] No atlases were generated. Check source folders and regex pattern."
                );
            }
        }
    }
#endif
}

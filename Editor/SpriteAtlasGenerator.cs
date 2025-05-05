namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Core.Extension;
    using Core.Helper;
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
            UpdateMatchCounts();
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
            Regex regex = new(_nameRegex);

            foreach (Object obj in _sourceFolders)
            {
                string folderPath = AssetDatabase.GetAssetPath(obj);
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    IEnumerable<Sprite> sprites = AssetDatabase
                        .LoadAllAssetsAtPath(path)
                        .OfType<Sprite>();
                    foreach (Sprite sp in sprites)
                    {
                        _totalCount++;
                        if (regex.IsMatch(sp.name))
                        {
                            _matchCount++;
                        }
                    }
                }
            }
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
                    return;
                }

                if (!AssetDatabase.IsValidFolder(_outputFolder))
                {
                    string parent = Path.GetDirectoryName(_outputFolder);
                    string newFolderName = Path.GetFileName(_outputFolder);
                    AssetDatabase.CreateFolder(parent, newFolderName);
                }

                EditorUtility.DisplayProgressBar(Name, "Deleting old atlases...", 0.1f);
                string[] existing = AssetDatabase
                    .FindAssets("t:SpriteAtlas", new[] { _outputFolder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .ToArray();
                List<string> failedPaths = new();
                AssetDatabase.DeleteAssets(existing, failedPaths);
                if (failedPaths.Any())
                {
                    this.LogWarn(
                        $"Failed to delete {failedPaths.Count} atlases.\n{string.Join("\n", failedPaths)}"
                    );
                }

                EditorUtility.DisplayProgressBar(Name, "Scanning sprites...", 0.25f);
                Regex regex = new(_nameRegex);
                Dictionary<string, List<Sprite>> groups = new(StringComparer.Ordinal);

                foreach (Object sourceDirectory in _sourceFolders)
                {
                    string folderPath = AssetDatabase.GetAssetPath(sourceDirectory);
                    if (!AssetDatabase.IsValidFolder(folderPath))
                    {
                        continue;
                    }

                    foreach (
                        string assetGuid in AssetDatabase.FindAssets(
                            "t:Sprite",
                            new[] { folderPath }
                        )
                    )
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                        foreach (Sprite sub in allAssets.OfType<Sprite>())
                        {
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
                }

                const int atlasSize = 8192;
                const long budget = (long)atlasSize * atlasSize;
                int totalChunks = 0;
                Dictionary<string, List<List<Sprite>>> groupChunks = new();
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
                        long area = (long)(sprite.rect.width * sprite.rect.height);
                        if (area > budget)
                        {
                            List<Sprite> chunk = new() { sprite };
                            chunks.Add(chunk);
                        }
                        else if (currentArea + area <= budget)
                        {
                            current.Add(sprite);
                            currentArea += area;
                        }
                        else
                        {
                            chunks.Add(current);
                            current = new List<Sprite> { sprite };
                            currentArea = area;
                        }
                    }

                    if (current.Count > 0)
                    {
                        chunks.Add(current);
                    }

                    groupChunks[kv.Key] = chunks;
                    totalChunks += chunks.Count;
                }

                // Generate atlases per chunk
                int chunkIndex = 0;
                foreach ((string prefix, List<List<Sprite>> chunks) in groupChunks)
                {
                    for (int i = 0; i < chunks.Count; i++)
                    {
                        List<Sprite> chunk = chunks[i];
                        ++processed;
                        EditorUtility.DisplayProgressBar(
                            Name,
                            $"Creating atlas '{prefix}' ({i + 1}/{chunks.Count})...",
                            0.4f + 0.6f * (chunkIndex++ / (float)totalChunks)
                        );
                        SpriteAtlas atlas = new();
                        atlases.Add(atlas);
                        string atlasName = $"{prefix}_{i}";
                        SpriteAtlasPackingSettings packingSettings = atlas.GetPackingSettings();
                        packingSettings.enableTightPacking = true;
                        packingSettings.padding = 2;
                        packingSettings.enableRotation = false;
                        atlas.SetPackingSettings(packingSettings);

                        SpriteAtlasTextureSettings textureSettings = atlas.GetTextureSettings();
                        textureSettings.generateMipMaps = false;
                        textureSettings.filterMode = FilterMode.Trilinear;
                        textureSettings.readable = true;
                        atlas.SetTextureSettings(textureSettings);

                        TextureImporterPlatformSettings platformSettings =
                            atlas.GetPlatformSettings(DefaultPlatformName);
                        if (platformSettings != null)
                        {
                            platformSettings.maxTextureSize = atlasSize;
                            platformSettings.textureCompression = _compressionLevel;

                            if (_crunchCompression is >= 0 and <= 100)
                            {
                                platformSettings.crunchedCompression = true;
                                platformSettings.compressionQuality = _crunchCompression;
                            }
                            else
                            {
                                if (100 < _crunchCompression)
                                {
                                    this.LogError(
                                        $"Invalid crunch compression: {_crunchCompression}"
                                    );
                                }

                                platformSettings.crunchedCompression = false;
                            }

                            atlas.SetPlatformSettings(platformSettings);
                        }
                        else
                        {
                            this.LogWarn(
                                $"Failed to find TextureImporterPlatformSettings for atlas {atlasName}"
                            );
                        }

                        atlas.Add(chunk.Select(sprite => sprite as Object).ToArray());
                        atlas.SetIncludeInBuild(true);
                        string path = Path.Combine(_outputFolder, atlasName + ".spriteatlas");
                        AssetDatabase.CreateAsset(atlas, path);
                    }
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget, false);

                bool anyChanged = false;
                foreach (
                    SpriteAtlas atlas in atlases
                        .Select(AssetDatabase.GetAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<SpriteAtlas>)
                        .Where(Objects.NotNull)
                )
                {
                    Texture2D preview = atlas.GetPreviewTexture();
                    if (preview == null)
                    {
                        continue;
                    }

                    TextureImporterPlatformSettings platformSettings = atlas.GetPlatformSettings(
                        DefaultPlatformName
                    );
                    if (platformSettings == null)
                    {
                        continue;
                    }
                    platformSettings.maxTextureSize = Mathf.Max(preview.width, preview.height);
                    atlas.SetPlatformSettings(platformSettings);
                    anyChanged = true;
                }

                if (anyChanged)
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            this.Log($"[SpriteAtlasGenerator] Created {processed} atlases in '{_outputFolder}'.");
        }
    }
#endif
}

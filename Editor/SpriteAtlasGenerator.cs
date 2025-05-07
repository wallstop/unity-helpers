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
    using UnityEditor;
    using UnityEditor.U2D;
    using UnityEngine;
    using UnityEngine.U2D;
    using Utils;
    using Object = UnityEngine.Object;

    public sealed class SpriteAtlasGenerator : EditorWindow
    {
        private const string Name = "Sprite Atlas Generator";
        private const string DefaultPlatformName = "DefaultTexturePlatform";
        private const int MaxAtlasDimension = 8192;
        private const int MaxTextureSize = 16384;
        private const long AtlasAreaBudget = (long)MaxAtlasDimension * MaxAtlasDimension;
        private const int MaxAtlasNameLength = 100;

        private sealed class AtlasCandidate
        {
            public string OriginalGroupKey { get; }
            public List<Sprite> Sprites { get; }
            public long TotalArea { get; }
            public string CandidateName { get; }

            public AtlasCandidate(
                string originalGroupKey,
                List<Sprite> sprites,
                string candidateName
            )
            {
                OriginalGroupKey = originalGroupKey;
                Sprites = new List<Sprite>(sprites);
                CandidateName = candidateName;
                TotalArea = 0;
                if (Sprites != null)
                {
                    foreach (Sprite sprite in Sprites)
                    {
                        if (sprite != null && sprite.rect is { width: > 0, height: > 0 })
                        {
                            TotalArea += (long)(sprite.rect.width * sprite.rect.height);
                        }
                    }
                }
            }
        }

        private sealed class MergeableAtlas
        {
            public HashSet<string> OriginalGroupKeys { get; }
            public List<Sprite> Sprites { get; }
            public long TotalArea { get; }
            public string RepresentativeInitialName { get; }

            public MergeableAtlas(
                string originalGroupKey,
                List<Sprite> initialSprites,
                string initialCandidateName,
                long totalArea
            )
            {
                OriginalGroupKeys = new HashSet<string>(StringComparer.Ordinal)
                {
                    originalGroupKey,
                };
                Sprites = initialSprites;
                TotalArea = totalArea;
                RepresentativeInitialName = initialCandidateName;
            }

            private MergeableAtlas(
                HashSet<string> combinedKeys,
                List<Sprite> combinedSprites,
                long combinedArea,
                string representativeName
            )
            {
                OriginalGroupKeys = combinedKeys;
                Sprites = combinedSprites;
                TotalArea = combinedArea;
                RepresentativeInitialName = representativeName;
            }

            public static MergeableAtlas Merge(MergeableAtlas atlas1, MergeableAtlas atlas2)
            {
                HashSet<string> newKeys = new(atlas1.OriginalGroupKeys, StringComparer.Ordinal);
                newKeys.UnionWith(atlas2.OriginalGroupKeys);

                List<Sprite> newSprites = new(atlas1.Sprites.Count + atlas2.Sprites.Count);
                newSprites.AddRange(atlas1.Sprites);
                newSprites.AddRange(atlas2.Sprites);

                long newArea = atlas1.TotalArea + atlas2.TotalArea;

                return new MergeableAtlas(
                    newKeys,
                    newSprites,
                    newArea,
                    atlas1.RepresentativeInitialName
                );
            }

            public string GenerateFinalName()
            {
                if (!OriginalGroupKeys.Any())
                {
                    return "EmptyOrInvalidAtlas";
                }

                List<string> sortedKeys = OriginalGroupKeys
                    .OrderBy(k => k, StringComparer.Ordinal)
                    .ToList();
                string name = string.Join("_", sortedKeys);

                if (name.Length > MaxAtlasNameLength)
                {
                    const string suffix = "_etc";
                    int actualLength = MaxAtlasNameLength - suffix.Length;
                    if (actualLength <= 0)
                    {
                        return name.Substring(0, MaxAtlasNameLength);
                    }

                    name = name.Substring(0, actualLength) + suffix;
                }
                return name;
            }
        }

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

        [SerializeField]
        private bool _optimizeGroupings = true;

        private int _matchCount;
        private int _totalCount;
        private GUIStyle _impactButtonStyle;

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
            _impactButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = Color.yellow },
                fontStyle = FontStyle.Bold,
            };
            GUILayout.Label("Source Folders", EditorStyles.boldLabel);
            SerializedObject so = new(this);
            so.Update();
            EditorGUILayout.PropertyField(so.FindProperty(nameof(_sourceFolders)), true);
            so.ApplyModifiedProperties();
            GUILayout.Space(8);
            using (new GUIHorizontalScope())
            {
                EditorGUILayout.LabelField("Sprite Name Regex");
                GUILayout.FlexibleSpace();
                _nameRegex = EditorGUILayout.TextField(_nameRegex);
            }

            GUILayout.Space(4);
            using (new GUIHorizontalScope())
            {
                if (GUILayout.Button("Calculate Matches"))
                {
                    UpdateMatchCounts();
                }
            }

            using (new GUIHorizontalScope())
            {
                EditorGUILayout.LabelField($"Matches: {_matchCount}");
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Non-matches: {_totalCount - _matchCount}");
            }

            GUILayout.Space(4);
            using (new GUIHorizontalScope())
            {
                EditorGUILayout.LabelField("Crunch Compression");
                GUILayout.FlexibleSpace();
                _crunchCompression = EditorGUILayout.IntField(_crunchCompression);
            }

            GUILayout.Space(4);
            using (new GUIHorizontalScope())
            {
                EditorGUILayout.LabelField("Compression Level", GUILayout.Width(150));
                _compressionLevel = (TextureImporterCompression)
                    EditorGUILayout.EnumPopup(_compressionLevel);
            }

            GUILayout.Space(4);
            using (new GUIHorizontalScope())
            {
                EditorGUILayout.LabelField("Optimize Groupings");
                _optimizeGroupings = EditorGUILayout.Toggle(_optimizeGroupings);
            }

            GUILayout.Space(12);
            using (new GUIHorizontalScope())
            {
                EditorGUILayout.LabelField("Atlas Output Folder");
                EditorGUILayout.LabelField(_outputFolder, EditorStyles.textField);
            }

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
            using (new GUIHorizontalScope())
            {
                if (GUILayout.Button("Generate Atlases", _impactButtonStyle))
                {
                    GenerateAtlases();
                }
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

            try
            {
                float total = _sourceFolders.Length;
                foreach (Object obj in _sourceFolders)
                {
                    if (obj == null)
                    {
                        continue;
                    }

                    string folderPath = AssetDatabase.GetAssetPath(obj);
                    if (
                        string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath)
                    )
                    {
                        this.LogWarn($"Skipping invalid or null source folder entry.");
                        continue;
                    }

                    string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
                    for (int i = 0; i < guids.Length; i++)
                    {
                        EditorUtility.DisplayProgressBar(
                            Name,
                            "Calculating...",
                            i * 1f / guids.Length / total
                        );

                        string guid = guids[i];
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
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Repaint();
        }

        private void GenerateAtlases()
        {
            List<SpriteAtlas> atlases = new();
            int processed = 0;
            AssetDatabase.StartAssetEditing();
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

                EditorUtility.DisplayProgressBar(Name, "Deleting old atlases...", 0.05f);
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

                EditorUtility.DisplayProgressBar(
                    Name,
                    "Scanning sprites & initial grouping...",
                    0.1f
                );
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

                float sourceFolderIncrement =
                    _sourceFolders.Length > 0 ? 0.2f / _sourceFolders.Length : 0f;
                float sourceFolderProgress = 0.1f;

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
                        sourceFolderProgress
                    );
                    sourceFolderProgress += sourceFolderIncrement;
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
                }

                int totalChunks = 0;
                Dictionary<string, List<List<Sprite>>> groupChunks = new();

                EditorUtility.DisplayProgressBar(Name, "Calculating chunks...", 0.3f);

                foreach (KeyValuePair<string, List<Sprite>> kv in groups)
                {
                    List<Sprite> spritesInGroup = kv
                        .Value.Where(s => s != null && s.rect is { width: > 0, height: > 0 })
                        .OrderByDescending(s => s.rect.width * s.rect.height)
                        .ToList();
                    if (!spritesInGroup.Any())
                    {
                        continue;
                    }

                    List<List<Sprite>> chunks = new();
                    List<Sprite> current = new();
                    long currentArea = 0;
                    foreach (Sprite sprite in spritesInGroup)
                    {
                        long area = (long)(sprite.rect.width * sprite.rect.height);
                        if (area > AtlasAreaBudget)
                        {
                            this.LogWarn(
                                $"Sprite '{sprite.name}' ({sprite.rect.width}x{sprite.rect.height}) is larger than max atlas area budget and will be placed in its own atlas chunk."
                            );
                            continue;
                        }
                        if (currentArea + area <= AtlasAreaBudget && current.Count < 2000)
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

                List<(string Name, List<Sprite> Sprites)> finalAtlasesData;
                if (_optimizeGroupings)
                {
                    EditorUtility.DisplayProgressBar(
                        Name,
                        "Preparing for merge optimization...",
                        0.4f
                    );

                    List<AtlasCandidate> allInitialCandidates = new();
                    foreach (
                        (
                            string originalGroupKey,
                            List<List<Sprite>> chunksForThisGroup
                        ) in groupChunks
                    )
                    {
                        for (int i = 0; i < chunksForThisGroup.Count; i++)
                        {
                            if (!chunksForThisGroup[i].Any())
                            {
                                continue;
                            }

                            string candidateName =
                                chunksForThisGroup.Count > 1
                                    ? $"{originalGroupKey}_{i}"
                                    : originalGroupKey;
                            allInitialCandidates.Add(
                                new AtlasCandidate(
                                    originalGroupKey,
                                    chunksForThisGroup[i],
                                    candidateName
                                )
                            );
                        }
                    }

                    allInitialCandidates = allInitialCandidates
                        .OrderByDescending(c => c.TotalArea)
                        .ThenBy(c => c.CandidateName, StringComparer.Ordinal)
                        .ToList();

                    List<MergeableAtlas> workingAtlases = allInitialCandidates
                        .Select(c => new MergeableAtlas(
                            c.OriginalGroupKey,
                            c.Sprites,
                            c.CandidateName,
                            c.TotalArea
                        ))
                        .ToList();
                    int passNumber = 0;
                    float mergeOptimizationProgressStart = 0.30f;
                    float mergeOptimizationProgressRange = 0.50f;

                    while (true)
                    {
                        passNumber++;
                        bool mergedInThisPass = false;
                        float currentPassProgress =
                            mergeOptimizationProgressStart
                            + passNumber * (mergeOptimizationProgressRange / 15.0f);
                        EditorUtility.DisplayProgressBar(
                            Name,
                            $"Optimizing atlas count (Pass {passNumber}, {workingAtlases.Count} atlases)...",
                            Mathf.Min(
                                currentPassProgress,
                                mergeOptimizationProgressStart + mergeOptimizationProgressRange
                            )
                        );

                        workingAtlases = workingAtlases
                            .OrderByDescending(a => a.TotalArea)
                            .ThenBy(a => a.RepresentativeInitialName, StringComparer.Ordinal)
                            .ToList();

                        bool[] isSubsumed = new bool[workingAtlases.Count];

                        for (int i = 0; i < workingAtlases.Count; i++)
                        {
                            if (isSubsumed[i])
                            {
                                continue;
                            }

                            MergeableAtlas baseAtlas = workingAtlases[i];
                            string baseRepresentativeKey = baseAtlas
                                .OriginalGroupKeys.OrderBy(k => k, StringComparer.Ordinal)
                                .First();

                            int bestPartnerIndex = -1;
                            MergeableAtlas bestPartnerObject = null;
                            int currentMinLevenshtein = int.MaxValue;

                            for (int j = i + 1; j < workingAtlases.Count; j++)
                            {
                                if (isSubsumed[j])
                                {
                                    continue;
                                }

                                MergeableAtlas potentialPartner = workingAtlases[j];
                                if (
                                    baseAtlas.TotalArea + potentialPartner.TotalArea
                                    > AtlasAreaBudget
                                )
                                {
                                    continue;
                                }

                                string partnerRepresentativeKey = potentialPartner
                                    .OriginalGroupKeys.OrderBy(k => k, StringComparer.Ordinal)
                                    .First();
                                int distance = baseRepresentativeKey.LevenshteinDistance(
                                    partnerRepresentativeKey
                                );
                                bool updateBest = false;
                                if (bestPartnerObject == null || distance < currentMinLevenshtein)
                                {
                                    updateBest = true;
                                }
                                else if (distance == currentMinLevenshtein)
                                {
                                    if (
                                        potentialPartner.TotalArea > bestPartnerObject.TotalArea
                                        || (
                                            potentialPartner.TotalArea
                                                == bestPartnerObject.TotalArea
                                            && string.Compare(
                                                potentialPartner.RepresentativeInitialName,
                                                bestPartnerObject.RepresentativeInitialName,
                                                StringComparison.Ordinal
                                            ) < 0
                                        )
                                    )
                                    {
                                        updateBest = true;
                                    }
                                }
                                if (updateBest)
                                {
                                    currentMinLevenshtein = distance;
                                    bestPartnerObject = potentialPartner;
                                    bestPartnerIndex = j;
                                }
                            }
                            if (bestPartnerObject != null)
                            {
                                workingAtlases[i] = MergeableAtlas.Merge(
                                    baseAtlas,
                                    bestPartnerObject
                                );
                                isSubsumed[bestPartnerIndex] = true;
                                mergedInThisPass = true;
                            }
                        }
                        if (!mergedInThisPass)
                        {
                            break;
                        }

                        workingAtlases = workingAtlases.Where((_, k) => !isSubsumed[k]).ToList();
                        if (passNumber > 100)
                        {
                            this.LogWarn(
                                $"Merge optimization exceeded 100 passes, aborting merge loop."
                            );
                            break;
                        }
                    }

                    finalAtlasesData = workingAtlases
                        .Select(a => (Name: a.GenerateFinalName(), Sprites: a.Sprites))
                        .OrderBy(a => a.Name)
                        .ToList();
                }
                else
                {
                    finalAtlasesData = groupChunks
                        .SelectMany(chunk =>
                        {
                            string prefix = chunk.Key;
                            List<List<Sprite>> chunks = chunk.Value;
                            List<(string, List<Sprite>)> finalChunks = new();
                            for (int i = 0; i < chunks.Count; i++)
                            {
                                string atlasName = chunks.Count > 1 ? $"{prefix}_{i}" : prefix;
                                finalChunks.Add((atlasName, chunks[i]));
                            }
                            return finalChunks;
                        })
                        .ToList();
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

                            chunkIndex++;
                        }
                    }
                }

                foreach ((string atlasName, List<Sprite> sprites) in finalAtlasesData)
                {
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
                    textureSettings.readable = true;
                    atlas.SetTextureSettings(textureSettings);

                    TextureImporterPlatformSettings platformSettings = atlas.GetPlatformSettings(
                        DefaultPlatformName
                    );

                    if (platformSettings == null)
                    {
                        platformSettings = new TextureImporterPlatformSettings
                        {
                            name = DefaultPlatformName,
                        };
                        this.LogWarn(
                            $"Could not get default platform settings for {atlasName}. Creating new default."
                        );
                    }

                    platformSettings.overridden = true;
                    platformSettings.maxTextureSize = MaxAtlasDimension;
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

                    Object[] validSprites = sprites
                        .Select(sprite => sprite as Object)
                        .Where(Objects.NotNull)
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
                }
            }
            catch (Exception e)
            {
                this.LogError($"An unexpected error occurred during atlas generation.", e);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            if (processed > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget, false);
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

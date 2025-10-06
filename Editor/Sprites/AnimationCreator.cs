namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;
    using CustomEditors;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using Object = UnityEngine.Object;

    [Serializable]
    public sealed class AnimationData
    {
        public const float DefaultFramesPerSecond = 12;

        public List<Sprite> frames = new();
        public float framesPerSecond = DefaultFramesPerSecond;
        public string animationName = string.Empty;
        public bool isCreatedFromAutoParse;
        public bool loop;
    }

    public sealed class AnimationCreatorWindow : EditorWindow
    {
        private static readonly char[] WhiteSpaceSplitters = { ' ', '\t', '\n', '\r' };

        private SerializedObject _serializedObject;
        private SerializedProperty _animationDataProp;
        private SerializedProperty _animationSourcesProp;
        private SerializedProperty _spriteNameRegexProp;
        private SerializedProperty _textProp;

        public List<AnimationData> animationData = new();
        public List<Object> animationSources = new();
        public string spriteNameRegex = ".*";
        public string text;

        [HideInInspector]
        [SerializeField]
        private List<Sprite> _filteredSprites = new();
        private int _matchedSpriteCount;
        private int _unmatchedSpriteCount;
        private Regex _compiledRegex;
        private string _lastUsedRegex;
        private string _searchString = string.Empty;
        private Vector2 _scrollPosition;
        private string _errorMessage = string.Empty;
        private bool _animationDataIsExpanded = true;

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Animation Creator", priority = -3)]
        public static void ShowWindow()
        {
            GetWindow<AnimationCreatorWindow>("Animation Creator");
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _animationDataProp = _serializedObject.FindProperty(nameof(animationData));
            _animationSourcesProp = _serializedObject.FindProperty(nameof(animationSources));
            _spriteNameRegexProp = _serializedObject.FindProperty(nameof(spriteNameRegex));
            _textProp = _serializedObject.FindProperty(nameof(text));

            UpdateRegex();
            FindAndFilterSprites();
            Repaint();
        }

        private void OnGUI()
        {
            _serializedObject.Update();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            PersistentDirectoryGUI.PathSelectorObjectArray(
                _animationSourcesProp,
                nameof(AnimationCreatorWindow)
            );
            EditorGUILayout.PropertyField(_spriteNameRegexProp);
            EditorGUILayout.PropertyField(_textProp);

            if (!string.IsNullOrWhiteSpace(_errorMessage))
            {
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
            }
            else if (
                _animationSourcesProp.arraySize == 0
                || _animationSourcesProp.FindPropertyRelative("Array.size").intValue == 0
                || animationSources.TrueForAll(val => Objects.Null(val))
            )
            {
                EditorGUILayout.HelpBox(
                    "Please specify at least one Animation Source (folder).",
                    MessageType.Error
                );
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Data", EditorStyles.boldLabel);
            _searchString = EditorGUILayout.TextField("Search Animation Name", _searchString);

            DrawFilteredAnimationData();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sprite Filter Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Regex Pattern:", spriteNameRegex);
            EditorGUILayout.LabelField("Matched Sprites:", _matchedSpriteCount.ToString());
            EditorGUILayout.LabelField("Unmatched Sprites:", _unmatchedSpriteCount.ToString());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            DrawActionButtons();

            EditorGUILayout.EndScrollView();

            _ = _serializedObject.ApplyModifiedProperties();
        }

        private void DrawCheckSpritesButton()
        {
            if (GUILayout.Button("Check/Refresh Filtered Sprites"))
            {
                UpdateRegex();
                FindAndFilterSprites();
                Repaint();
            }
        }

        private void DrawFilteredAnimationData()
        {
            int listSize = _animationDataProp.arraySize;
            string[] searchTerms = string.IsNullOrWhiteSpace(_searchString)
                ? Array.Empty<string>()
                : _searchString.Split(WhiteSpaceSplitters, StringSplitOptions.RemoveEmptyEntries);

            List<int> matchingIndices = new();
            for (int i = 0; i < listSize; ++i)
            {
                SerializedProperty elementProp = _animationDataProp.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = elementProp.FindPropertyRelative(
                    nameof(AnimationData.animationName)
                );

                string currentName =
                    nameProp != null ? nameProp.stringValue ?? string.Empty : string.Empty;

                bool matchesSearch = true;
                if (searchTerms.Length > 0)
                {
                    matchesSearch = searchTerms.All(term =>
                        currentName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    );
                }

                if (matchesSearch)
                {
                    matchingIndices.Add(i);
                }
            }
            int matchCount = matchingIndices.Count;
            string foldoutLabel =
                $"{_animationDataProp.displayName} (Showing {matchCount} / {listSize})";
            _animationDataIsExpanded = EditorGUILayout.Foldout(
                _animationDataIsExpanded,
                foldoutLabel,
                true
            );

            if (_animationDataIsExpanded)
            {
                using EditorGUI.IndentLevelScope indent = new();
                if (matchCount > 0)
                {
                    foreach (int index in matchingIndices)
                    {
                        SerializedProperty elementProp = _animationDataProp.GetArrayElementAtIndex(
                            index
                        );

                        SerializedProperty nameProp = elementProp.FindPropertyRelative(
                            nameof(AnimationData.animationName)
                        );
                        string currentName =
                            nameProp != null ? nameProp.stringValue ?? string.Empty : string.Empty;
                        string labelText = string.IsNullOrWhiteSpace(currentName)
                            ? $"Element {index} (No Name)"
                            : currentName;

                        EditorGUILayout.PropertyField(elementProp, new GUIContent(labelText), true);
                    }
                }
                else if (listSize > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"No animation data matched the search term '{_searchString}'.",
                        MessageType.Info
                    );
                }
            }
        }

        private void DrawActionButtons()
        {
            DrawCheckSpritesButton();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(_filteredSprites.Count == 0))
            {
                if (
                    GUILayout.Button(
                        $"Populate First Slot with {_filteredSprites.Count} Matched Sprites"
                    )
                )
                {
                    if (animationData.Count == 0)
                    {
                        this.LogWarn($"Add at least one Animation Data entry first.");
                    }
                    else if (animationData[0].frames.Count > 0)
                    {
                        if (
                            !EditorUtility.DisplayDialog(
                                "Confirm Overwrite",
                                "This will replace the frames currently in the first animation slot. Are you sure?",
                                "Replace",
                                "Cancel"
                            )
                        )
                        {
                            return;
                        }
                    }
                    if (animationData.Count > 0)
                    {
                        animationData[0].frames = new List<Sprite>(_filteredSprites);
                        animationData[0].animationName = "All_Matched_Sprites";
                        animationData[0].isCreatedFromAutoParse = false;
                        _serializedObject.Update();
                        Repaint();
                        this.Log($"Populated first slot with {_filteredSprites.Count} sprites.");
                    }
                }

                if (GUILayout.Button("Auto-Parse Matched Sprites into Animations"))
                {
                    if (
                        EditorUtility.DisplayDialog(
                            "Confirm Auto-Parse",
                            "This will replace the current animation list with animations generated from matched sprites based on their names (e.g., 'Player_Run_0', 'Player_Run_1'). Are you sure?",
                            "Parse",
                            "Cancel"
                        )
                    )
                    {
                        AutoParseSprites();
                        _serializedObject.Update();
                        Repaint();
                    }
                }
            }

            if (GUILayout.Button("Create new Animation Data"))
            {
                animationData.Add(new AnimationData());
                _serializedObject.Update();
                Repaint();
            }

            if (_filteredSprites.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Cannot perform sprite actions: No sprites matched the filter criteria or sources are empty.",
                    MessageType.Info
                );
            }

            bool canBulkName =
                animationData is { Count: > 0 }
                && animationData.Any(data => data.frames?.Count > 0)
                && !string.IsNullOrWhiteSpace(text);

            using (new EditorGUI.DisabledScope(!canBulkName))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Bulk Naming Operations", EditorStyles.boldLabel);

                if (GUILayout.Button($"Append '{text}' To All Animation Names"))
                {
                    bool changed = false;
                    foreach (AnimationData data in animationData)
                    {
                        if (
                            !string.IsNullOrWhiteSpace(data.animationName)
                            && !data.animationName.EndsWith($"_{text}")
                        )
                        {
                            data.animationName += $"_{text}";
                            changed = true;
                        }
                    }
                    if (changed)
                    {
                        this.Log($"Appended '{text}' to animation names.");
                        _serializedObject.Update();
                        Repaint();
                    }
                    else
                    {
                        this.LogWarn(
                            $"No animation names modified. Either none exist or they already end with '_{text}'."
                        );
                    }
                }

                if (GUILayout.Button($"Remove '{text}' From End of Names"))
                {
                    bool changed = false;
                    string suffix = $"_{text}";
                    foreach (AnimationData data in animationData)
                    {
                        if (
                            !string.IsNullOrWhiteSpace(data.animationName)
                            && data.animationName.EndsWith(suffix)
                        )
                        {
                            data.animationName = data.animationName.Remove(
                                data.animationName.Length - suffix.Length
                            );
                            changed = true;
                        }
                        else if (
                            !string.IsNullOrWhiteSpace(data.animationName)
                            && data.animationName.EndsWith(text)
                        )
                        {
                            data.animationName = data.animationName.Remove(
                                data.animationName.Length - text.Length
                            );
                            changed = true;
                        }
                    }
                    if (changed)
                    {
                        this.Log($"Removed '{text}' suffix from animation names.");
                        _serializedObject.Update();
                        Repaint();
                    }
                    else
                    {
                        this.LogWarn(
                            $"No animation names modified. Either none exist or they do not end with '{text}' or '_{text}'."
                        );
                    }
                }
            }

            if (
                !canBulkName
                && animationData is { Count: > 0 }
                && animationData.Any(data => data.frames?.Count > 0)
            )
            {
                EditorGUILayout.HelpBox(
                    "Enter text in the 'Text' field above to enable bulk naming operations.",
                    MessageType.Info
                );
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(animationData is not { Count: > 0 }))
            {
                if (GUILayout.Button("Create Animations"))
                {
                    CreateAnimations();
                }
            }
            if (animationData is not { Count: > 0 })
            {
                EditorGUILayout.HelpBox(
                    "Add Animation Data entries before creating.",
                    MessageType.Warning
                );
            }
        }

        private void CreateAnimations()
        {
            if (animationData is not { Count: > 0 })
            {
                this.LogError($"No animation data to create.");
                return;
            }

            string[] searchTerms = string.IsNullOrWhiteSpace(_searchString)
                ? Array.Empty<string>()
                : _searchString
                    .ToLowerInvariant()
                    .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            List<AnimationData> dataToCreate = new();
            if (searchTerms.Length == 0)
            {
                dataToCreate.AddRange(animationData);
            }
            else
            {
                foreach (AnimationData data in animationData)
                {
                    string lowerName = (data.animationName ?? string.Empty).ToLowerInvariant();
                    if (searchTerms.All(term => lowerName.Contains(term)))
                    {
                        dataToCreate.Add(data);
                    }
                }
                this.Log(
                    $"Creating animations based on current search filter '{_searchString}'. Only {dataToCreate.Count} out of {animationData.Count} items will be processed."
                );
            }

            if (dataToCreate.Count == 0)
            {
                this.LogError(
                    $"No animation data matches the current search filter '{_searchString}'. Nothing to create."
                );
                return;
            }

            int totalAnimations = dataToCreate.Count;
            int currentAnimationIndex = 0;
            bool errorOccurred = false;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (AnimationData data in dataToCreate)
                {
                    currentAnimationIndex++;
                    string animationName = data.animationName;
                    if (string.IsNullOrWhiteSpace(animationName))
                    {
                        this.LogWarn(
                            $"Ignoring animation data entry (original index unknown due to filtering) without an animation name."
                        );
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(
                        "Creating Animations",
                        $"Processing '{animationName}' ({currentAnimationIndex}/{totalAnimations})",
                        (float)currentAnimationIndex / totalAnimations
                    );

                    float framesPerSecond = data.framesPerSecond;
                    if (framesPerSecond <= 0)
                    {
                        this.LogWarn(
                            $"Ignoring animation '{animationName}' with invalid FPS ({framesPerSecond})."
                        );
                        continue;
                    }

                    List<Sprite> frames = data.frames;
                    if (frames is not { Count: > 0 })
                    {
                        this.LogWarn(
                            $"Ignoring animation '{animationName}' because it has no frames."
                        );
                        continue;
                    }

                    List<Sprite> validFrames = frames.Where(f => f != null).ToList();
                    if (validFrames.Count == 0)
                    {
                        this.LogWarn(
                            $"Ignoring animation '{animationName}' because it only contains null frames."
                        );
                        continue;
                    }

                    validFrames.Sort((s1, s2) => EditorUtility.NaturalCompare(s1.name, s2.name));

                    List<ObjectReferenceKeyframe> keyFrames = new(validFrames.Count);
                    float timeStep = 1f / framesPerSecond;
                    float currentTime = 0f;

                    foreach (
                        ObjectReferenceKeyframe keyFrame in validFrames.Select(
                            sprite => new ObjectReferenceKeyframe
                            {
                                time = currentTime,
                                value = sprite,
                            }
                        )
                    )
                    {
                        keyFrames.Add(keyFrame);
                        currentTime += timeStep;
                    }

                    if (keyFrames.Count <= 0)
                    {
                        this.LogWarn(
                            $"No valid keyframes could be generated for animation '{animationName}'."
                        );
                        continue;
                    }

                    AnimationClip animationClip = new() { frameRate = framesPerSecond };

                    if (data.loop)
                    {
                        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(
                            animationClip
                        );
                        settings.loopTime = true;
                        AnimationUtility.SetAnimationClipSettings(animationClip, settings);
                    }

                    AnimationUtility.SetObjectReferenceCurve(
                        animationClip,
                        EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
                        keyFrames.ToArray()
                    );

                    string firstFramePath = AssetDatabase.GetAssetPath(validFrames[0]);
                    string assetPath =
                        Path.GetDirectoryName(firstFramePath).SanitizePath() ?? "Assets";
                    if (!assetPath.EndsWith("/"))
                    {
                        assetPath += "/";
                    }

                    string finalPath = AssetDatabase.GenerateUniqueAssetPath(
                        $"{assetPath}{animationName}.anim"
                    );
                    AssetDatabase.CreateAsset(animationClip, finalPath);
                    this.Log($"Created animation at '{finalPath}'.");
                }
            }
            catch (Exception e)
            {
                errorOccurred = true;
                this.LogError($"An error occurred during animation creation: {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!errorOccurred)
                {
                    this.Log($"Finished creating {totalAnimations} animations.");
                }
                else
                {
                    this.LogError($"Animation creation finished with errors. Check console.");
                }

                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void UpdateRegex()
        {
            if (_compiledRegex == null || _lastUsedRegex != spriteNameRegex)
            {
                try
                {
                    _compiledRegex = new Regex(spriteNameRegex, RegexOptions.Compiled);
                    _lastUsedRegex = spriteNameRegex;
                    _errorMessage = "";
                    this.Log($"Regex updated to: {spriteNameRegex}");
                }
                catch (ArgumentException ex)
                {
                    _compiledRegex = null;
                    _lastUsedRegex = spriteNameRegex;
                    _errorMessage = $"Invalid Regex: {ex.Message}";
                    this.LogError($"Invalid Regex '{spriteNameRegex}': {ex.Message}");
                }
            }
        }

        private void FindAndFilterSprites()
        {
            _filteredSprites.Clear();
            _matchedSpriteCount = 0;
            _unmatchedSpriteCount = 0;

            if (animationSources is not { Count: > 0 } || _compiledRegex == null)
            {
                if (_compiledRegex == null && !string.IsNullOrWhiteSpace(spriteNameRegex))
                {
                    this.LogWarn(
                        $"Cannot find sprites, regex pattern '{spriteNameRegex}' is invalid."
                    );
                }
                else if (animationSources is not { Count: > 0 })
                {
                    this.LogWarn($"Cannot find sprites, no animation sources specified.");
                }
                return;
            }

            List<string> searchPaths = new();
            foreach (Object source in animationSources.Where(Objects.NotNull))
            {
                string path = AssetDatabase.GetAssetPath(source);
                if (!string.IsNullOrWhiteSpace(path) && AssetDatabase.IsValidFolder(path))
                {
                    searchPaths.Add(path);
                }
                else if (source != null)
                {
                    this.LogWarn($"Source '{source.name}' is not a valid folder. Skipping.");
                }
            }

            if (searchPaths.Count == 0)
            {
                this.LogWarn($"No valid folders found in Animation Sources.");
                return;
            }

            string[] assetGuids = AssetDatabase.FindAssets("t:sprite", searchPaths.ToArray());
            float totalAssets = assetGuids.Length;
            this.Log($"Found {totalAssets} total sprite assets in specified paths.");

            try
            {
                EditorUtility.DisplayProgressBar(
                    "Finding and Filtering Sprites",
                    $"Scanning {assetGuids.Length} assets...",
                    0f
                );

                for (int i = 0; i < totalAssets; i++)
                {
                    string guid = assetGuids[i];
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    if (i % 20 == 0 || Mathf.Approximately(i, totalAssets - 1))
                    {
                        float progress = (i + 1) / totalAssets;
                        EditorUtility.DisplayProgressBar(
                            "Finding and Filtering Sprites",
                            $"Checking: {Path.GetFileName(path)} ({i + 1}/{assetGuids.Length})",
                            progress
                        );
                    }

                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

                    if (sprite != null)
                    {
                        if (_compiledRegex.IsMatch(sprite.name))
                        {
                            _filteredSprites.Add(sprite);
                            _matchedSpriteCount++;
                        }
                        else
                        {
                            _unmatchedSpriteCount++;
                        }
                    }
                }
                this.Log(
                    $"Sprite filtering complete. Matched: {_matchedSpriteCount}, Unmatched: {_unmatchedSpriteCount}."
                );
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void AutoParseSprites()
        {
            if (_filteredSprites.Count == 0)
            {
                this.LogWarn($"Cannot Auto-Parse, no matched sprites available.");
                return;
            }

            Dictionary<string, Dictionary<string, List<Sprite>>> spritesByPrefixAndAssetPath = new(
                StringComparer.Ordinal
            );
            int totalSprites = _filteredSprites.Count;
            int processedCount = 0;
            this.Log($"Starting auto-parse for {_filteredSprites.Count} matched sprites.");

            try
            {
                foreach (Sprite sprite in _filteredSprites)
                {
                    processedCount++;
                    if (processedCount % 10 == 0 || processedCount == totalSprites)
                    {
                        EditorUtility.DisplayProgressBar(
                            "Auto-Parsing Sprites",
                            $"Processing: {sprite.name} ({processedCount}/{totalSprites})",
                            (float)processedCount / totalSprites
                        );
                    }

                    string assetPath = AssetDatabase.GetAssetPath(sprite);
                    string directoryPath = Path.GetDirectoryName(assetPath).SanitizePath() ?? "";
                    string frameName = sprite.name;

                    int splitIndex = frameName.LastIndexOf('_');
                    string prefix = frameName;

                    if (splitIndex > 0 && splitIndex < frameName.Length - 1)
                    {
                        bool allDigitsAfter = true;
                        for (int j = splitIndex + 1; j < frameName.Length; j++)
                        {
                            if (!char.IsDigit(frameName[j]))
                            {
                                allDigitsAfter = false;
                                break;
                            }
                        }

                        if (allDigitsAfter)
                        {
                            prefix = frameName.Substring(0, splitIndex);
                        }
                        else
                        {
                            prefix = frameName;
                            this.LogWarn(
                                $"Sprite name '{frameName}' has an underscore but not only digits after the last one. Treating as single frame or check naming."
                            );
                        }
                    }
                    else if (splitIndex == -1)
                    {
                        prefix = frameName;
                        this.LogWarn(
                            $"Sprite name '{frameName}' has no underscore. Treating as single frame or check naming."
                        );
                    }

                    if (!string.IsNullOrWhiteSpace(prefix))
                    {
                        if (
                            !spritesByPrefixAndAssetPath.TryGetValue(
                                directoryPath,
                                out Dictionary<string, List<Sprite>> spritesByPrefix
                            )
                        )
                        {
                            spritesByPrefix = new Dictionary<string, List<Sprite>>(
                                StringComparer.Ordinal
                            );
                            spritesByPrefixAndAssetPath.Add(directoryPath, spritesByPrefix);
                        }

                        spritesByPrefix.GetOrAdd(prefix).Add(sprite);
                    }
                    else
                    {
                        this.LogWarn(
                            $"Could not extract valid prefix for frame '{frameName}' at path '{assetPath}'. Skipping."
                        );
                    }
                }

                if (spritesByPrefixAndAssetPath.Count > 0)
                {
                    int removedCount = animationData.RemoveAll(data => data.isCreatedFromAutoParse);
                    this.Log($"Removed {removedCount} previously auto-parsed animation entries.");

                    int addedCount = 0;
                    foreach (
                        KeyValuePair<
                            string,
                            Dictionary<string, List<Sprite>>
                        > kvpAssetPath in spritesByPrefixAndAssetPath
                    )
                    {
                        string dirName = new DirectoryInfo(kvpAssetPath.Key).Name;

                        foreach ((string key, List<Sprite> spritesInGroup) in kvpAssetPath.Value)
                        {
                            if (spritesInGroup.Count == 0)
                            {
                                continue;
                            }

                            spritesInGroup.Sort(
                                (s1, s2) => EditorUtility.NaturalCompare(s1.name, s2.name)
                            );

                            string finalAnimName;

                            bool keyIsLikelyFullName =
                                spritesInGroup.Count > 0 && key == spritesInGroup[0].name;

                            if (keyIsLikelyFullName)
                            {
                                int lastUnderscore = key.LastIndexOf('_');

                                if (lastUnderscore > 0 && lastUnderscore < key.Length - 1)
                                {
                                    string suffix = key.Substring(lastUnderscore + 1);
                                    finalAnimName = SanitizeName($"Anim_{suffix}");
                                    this.Log(
                                        $"Naming non-standard sprite group '{key}' as '{finalAnimName}' using suffix '{suffix}'."
                                    );
                                }
                                else
                                {
                                    finalAnimName = SanitizeName($"Anim_{key}");
                                    this.LogWarn(
                                        $"Naming non-standard sprite group '{key}' as '{finalAnimName}'. Could not extract suffix."
                                    );
                                }
                            }
                            else
                            {
                                finalAnimName = SanitizeName($"Anim_{key}");
                                this.Log(
                                    $"Naming standard sprite group '{key}' as '{finalAnimName}'."
                                );
                            }

                            animationData.Add(
                                new AnimationData
                                {
                                    frames = spritesInGroup,
                                    framesPerSecond = AnimationData.DefaultFramesPerSecond,
                                    animationName = finalAnimName,
                                    isCreatedFromAutoParse = true,
                                    loop = false,
                                }
                            );
                            addedCount++;
                        }
                    }

                    this.Log($"Auto-parsed into {addedCount} new animation groups.");
                }
                else
                {
                    this.LogWarn(
                        $"Auto-parsing did not result in any animation groups. Check sprite naming conventions (e.g., 'Prefix_0', 'Prefix_1')."
                    );
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _serializedObject.Update();
            }
        }

        private static string SanitizeName(string inputName)
        {
            inputName = inputName.Replace(" ", "_");
            inputName = Regex.Replace(inputName, @"[^a-zA-Z0-9_]", "");

            if (string.IsNullOrWhiteSpace(inputName))
            {
                return "Default_Animation";
            }

            return inputName.Trim('_');
        }
    }

#endif
}

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
    using WallstopStudios.UnityHelpers.Utils;
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
        private SerializedProperty _autoRefreshProp;
        private SerializedProperty _groupingCaseInsensitiveProp;
        private SerializedProperty _includeFolderNameProp;
        private SerializedProperty _includeFullFolderPathProp;
        private SerializedProperty _autoParseNamePrefixProp;
        private SerializedProperty _autoParseNameSuffixProp;
        private SerializedProperty _useCustomGroupRegexProp;
        private SerializedProperty _customGroupRegexProp;
        private SerializedProperty _customGroupRegexIgnoreCaseProp;
        private SerializedProperty _resolveDuplicateNamesProp;
        private SerializedProperty _regexTestInputProp;
        private SerializedProperty _strictNumericOrderingProp;

        public List<AnimationData> animationData = new();
        public List<Object> animationSources = new();
        public string spriteNameRegex = ".*";
        public string text;
        public bool autoRefresh = true;
        public bool groupingCaseInsensitive = true;
        public bool includeFolderNameInAnimName;
        public bool includeFullFolderPathInAnimName;
        public string autoParseNamePrefix = string.Empty;
        public string autoParseNameSuffix = string.Empty;
        public bool useCustomGroupRegex;
        public string customGroupRegex = string.Empty;
        public bool customGroupRegexIgnoreCase = true;
        public bool resolveDuplicateAnimationNames = true;
        public string regexTestInput = string.Empty;
        public bool strictNumericOrdering = false;

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
        private Regex _compiledGroupRegex;
        private string _lastGroupRegex;
        private string _groupRegexErrorMessage = string.Empty;
        private int _lastSourcesHash;
        private bool _animationDataIsExpanded = true;
        private bool _autoParsePreviewExpanded = false;
        private bool _autoParseDryRunExpanded = false;

        private sealed class AutoParsePreviewRecord
        {
            public string folder;
            public string baseName;
            public int count;
            public bool hasIndex;
        }

        private readonly List<AutoParsePreviewRecord> _autoParsePreview = new();

        private sealed class AutoParseDryRunRecord
        {
            public string folderPath;
            public string finalName;
            public string finalAssetPath;
            public int count;
            public bool hasIndex;
            public bool duplicateResolved;
        }

        private readonly List<AutoParseDryRunRecord> _autoParseDryRun = new();

        private static readonly Regex s_ParenIndexRegex = new(
            @"^(?<base>.*?)[\s]*\(\s*(?<index>\d+)\s*\)\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );
        private static readonly Regex s_SeparatorIndexRegex = new(
            @"^(?<base>.*?)[_\-\.\s]+(?<index>\d+)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );
        private static readonly Regex s_TrailingIndexRegex = new(
            @"^(?<base>.*?)(?<index>\d+)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

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
            _autoRefreshProp = _serializedObject.FindProperty(nameof(autoRefresh));
            _groupingCaseInsensitiveProp = _serializedObject.FindProperty(
                nameof(groupingCaseInsensitive)
            );
            _includeFolderNameProp = _serializedObject.FindProperty(
                nameof(includeFolderNameInAnimName)
            );
            _includeFullFolderPathProp = _serializedObject.FindProperty(
                nameof(includeFullFolderPathInAnimName)
            );
            _autoParseNamePrefixProp = _serializedObject.FindProperty(nameof(autoParseNamePrefix));
            _autoParseNameSuffixProp = _serializedObject.FindProperty(nameof(autoParseNameSuffix));
            _useCustomGroupRegexProp = _serializedObject.FindProperty(nameof(useCustomGroupRegex));
            _customGroupRegexProp = _serializedObject.FindProperty(nameof(customGroupRegex));
            _customGroupRegexIgnoreCaseProp = _serializedObject.FindProperty(
                nameof(customGroupRegexIgnoreCase)
            );
            _resolveDuplicateNamesProp = _serializedObject.FindProperty(
                nameof(resolveDuplicateAnimationNames)
            );
            _regexTestInputProp = _serializedObject.FindProperty(nameof(regexTestInput));
            _strictNumericOrderingProp = _serializedObject.FindProperty(
                nameof(strictNumericOrdering)
            );

            UpdateRegex();
            UpdateGroupRegex();
            FindAndFilterSprites();
            _lastSourcesHash = ComputeSourcesHash();
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
            EditorGUILayout.PropertyField(_autoRefreshProp, new GUIContent("Auto Refresh Filter"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grouping & Naming", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _groupingCaseInsensitiveProp,
                new GUIContent("Case-Insensitive Grouping")
            );
            EditorGUILayout.PropertyField(
                _includeFolderNameProp,
                new GUIContent("Prefix Leaf Folder Name")
            );
            EditorGUILayout.PropertyField(
                _includeFullFolderPathProp,
                new GUIContent("Prefix Full Folder Path")
            );
            EditorGUILayout.PropertyField(
                _autoParseNamePrefixProp,
                new GUIContent("Auto-Parse Name Prefix")
            );
            EditorGUILayout.PropertyField(
                _autoParseNameSuffixProp,
                new GUIContent("Auto-Parse Name Suffix")
            );
            EditorGUILayout.PropertyField(
                _resolveDuplicateNamesProp,
                new GUIContent("Resolve Duplicate Animation Names")
            );
            EditorGUILayout.PropertyField(
                _strictNumericOrderingProp,
                new GUIContent("Strict Numeric Ordering")
            );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Custom Group Regex", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _useCustomGroupRegexProp,
                new GUIContent("Enable Custom Group Regex")
            );
            using (new EditorGUI.DisabledScope(!_useCustomGroupRegexProp.boolValue))
            {
                EditorGUILayout.PropertyField(
                    _customGroupRegexProp,
                    new GUIContent("Pattern (?<base>)(?<index>)")
                );
                EditorGUILayout.PropertyField(
                    _customGroupRegexIgnoreCaseProp,
                    new GUIContent("Ignore Case (Regex)")
                );
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Regex Tester", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_regexTestInputProp, new GUIContent("Test Input"));
            if (!string.IsNullOrEmpty(regexTestInput))
            {
                if (_compiledRegex != null)
                {
                    bool match = _compiledRegex.IsMatch(regexTestInput);
                    EditorGUILayout.LabelField("Filter Regex Match:", match ? "Yes" : "No");
                }
                else
                {
                    EditorGUILayout.LabelField("Filter Regex Match:", "Invalid Pattern");
                }

                if (useCustomGroupRegex)
                {
                    if (_compiledGroupRegex != null)
                    {
                        Match m = _compiledGroupRegex.Match(regexTestInput);
                        if (m.Success)
                        {
                            string b = m.Groups["base"].Success ? m.Groups["base"].Value : "";
                            string idx = m.Groups["index"].Success ? m.Groups["index"].Value : "";
                            EditorGUILayout.LabelField("Custom Group Base:", b);
                            EditorGUILayout.LabelField(
                                "Custom Group Index:",
                                string.IsNullOrEmpty(idx) ? "(none)" : idx
                            );
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Custom Group Result:", "No match");
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Custom Group Result:", "Invalid Pattern");
                    }
                }

                if (TryExtractBaseAndIndex(regexTestInput, out string fbBase, out int fbIndex))
                {
                    EditorGUILayout.LabelField("Fallback Base:", fbBase);
                    EditorGUILayout.LabelField(
                        "Fallback Index:",
                        fbIndex >= 0 ? fbIndex.ToString() : "(none)"
                    );
                }
                else
                {
                    EditorGUILayout.LabelField("Fallback Result:", "No index; base = input");
                }
            }

            if (!string.IsNullOrWhiteSpace(_errorMessage))
            {
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
            }
            if (!string.IsNullOrWhiteSpace(_groupRegexErrorMessage))
            {
                EditorGUILayout.HelpBox(_groupRegexErrorMessage, MessageType.Error);
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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Auto-Parse Preview", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(_filteredSprites.Count == 0))
            {
                if (GUILayout.Button("Generate Auto-Parse Preview"))
                {
                    GenerateAutoParsePreview();
                    _autoParsePreviewExpanded = true;
                }
                if (GUILayout.Button("Generate Dry-Run Apply"))
                {
                    GenerateAutoParseDryRun();
                    _autoParseDryRunExpanded = true;
                }
            }
            if (_filteredSprites.Count == 0)
            {
                EditorGUILayout.HelpBox("No matched sprites to preview.", MessageType.Info);
            }
            _autoParsePreviewExpanded = EditorGUILayout.Foldout(
                _autoParsePreviewExpanded,
                $"Preview Groups ({_autoParsePreview.Count})",
                true
            );
            if (_autoParsePreviewExpanded && _autoParsePreview.Count > 0)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    int shown = 0;
                    foreach (var rec in _autoParsePreview)
                    {
                        EditorGUILayout.LabelField(
                            $"{rec.folder} / {rec.baseName}",
                            $"Frames: {rec.count} | Numeric: {(rec.hasIndex ? "Yes" : "No")}"
                        );
                        shown++;
                        if (shown >= 200)
                        {
                            EditorGUILayout.LabelField($"Showing first {shown} groups...");
                            break;
                        }
                    }
                }
            }
            _autoParseDryRunExpanded = EditorGUILayout.Foldout(
                _autoParseDryRunExpanded,
                $"Dry-Run Results ({_autoParseDryRun.Count})",
                true
            );
            if (_autoParseDryRunExpanded && _autoParseDryRun.Count > 0)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    int shown = 0;
                    foreach (var rec in _autoParseDryRun)
                    {
                        string info =
                            $"Name: {rec.finalName} | Frames: {rec.count} | Numeric: {(rec.hasIndex ? "Yes" : "No")}";
                        if (rec.duplicateResolved)
                        {
                            info += " | Renamed to avoid duplicate";
                        }
                        EditorGUILayout.LabelField(rec.folderPath, info);
                        EditorGUILayout.LabelField("→ Asset Path:", rec.finalAssetPath);
                        shown++;
                        if (shown >= 200)
                        {
                            EditorGUILayout.LabelField($"Showing first {shown} results...");
                            break;
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            _ = _serializedObject.ApplyModifiedProperties();

            // Auto refresh behavior at end of GUI to pick up any changed fields
            if (autoRefresh)
            {
                bool regexChanged = _compiledRegex == null || _lastUsedRegex != spriteNameRegex;
                int currentSourcesHash = ComputeSourcesHash();
                bool sourcesChanged = currentSourcesHash != _lastSourcesHash;
                if (regexChanged)
                {
                    UpdateRegex();
                }
                if (regexChanged || sourcesChanged)
                {
                    _lastSourcesHash = currentSourcesHash;
                    FindAndFilterSprites();
                    Repaint();
                }
                if (
                    _compiledGroupRegex == null
                    || _lastGroupRegex != customGroupRegex
                    || customGroupRegexIgnoreCase
                        != ((_compiledGroupRegex?.Options & RegexOptions.IgnoreCase) != 0)
                )
                {
                    UpdateGroupRegex();
                }
            }
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

                    using PooledResource<List<Sprite>> validFramesResource =
                        Buffers<Sprite>.List.Get();
                    List<Sprite> validFrames = validFramesResource.resource;
                    foreach (Sprite f in frames)
                    {
                        if (f != null)
                        {
                            validFrames.Add(f);
                        }
                    }
                    if (validFrames.Count == 0)
                    {
                        this.LogWarn(
                            $"Ignoring animation '{animationName}' because it only contains null frames."
                        );
                        continue;
                    }

                    validFrames.Sort((s1, s2) => EditorUtility.NaturalCompare(s1.name, s2.name));

                    float timeStep = 1f / framesPerSecond;
                    using PooledResource<ObjectReferenceKeyframe[]> keyframesResource =
                        WallstopFastArrayPool<ObjectReferenceKeyframe>.Get(
                            validFrames.Count,
                            out ObjectReferenceKeyframe[] keyframes
                        );
                    float currentTime = 0f;
                    for (int k = 0; k < validFrames.Count; k++)
                    {
                        keyframes[k].time = currentTime;
                        keyframes[k].value = validFrames[k];
                        currentTime += timeStep;
                    }

                    if (keyframes.Length <= 0)
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
                        keyframes
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
                    _compiledRegex = new Regex(
                        spriteNameRegex,
                        RegexOptions.Compiled | RegexOptions.CultureInvariant
                    );
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

        private void UpdateGroupRegex()
        {
            _groupRegexErrorMessage = string.Empty;
            _compiledGroupRegex = null;
            if (!useCustomGroupRegex)
            {
                _lastGroupRegex = customGroupRegex;
                return;
            }

            if (string.IsNullOrWhiteSpace(customGroupRegex))
            {
                _lastGroupRegex = customGroupRegex;
                _groupRegexErrorMessage = "Custom Group Regex enabled but pattern is empty.";
                return;
            }

            try
            {
                RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant;
                if (customGroupRegexIgnoreCase)
                {
                    options |= RegexOptions.IgnoreCase;
                }
                _compiledGroupRegex = new Regex(customGroupRegex, options);
                _lastGroupRegex = customGroupRegex;
            }
            catch (ArgumentException ex)
            {
                _compiledGroupRegex = null;
                _lastGroupRegex = customGroupRegex;
                _groupRegexErrorMessage = $"Invalid Custom Group Regex: {ex.Message}";
                this.LogError($"Invalid Custom Group Regex '{customGroupRegex}': {ex.Message}");
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
            int totalAssets = assetGuids.Length;
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

                    if (i % 20 == 0 || i == totalAssets - 1)
                    {
                        float progress = (i + 1) / (float)totalAssets;
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

            try
            {
                Dictionary<string, Dictionary<string, List<(int index, Sprite sprite)>>> groups =
                    GroupFilteredSprites(withProgress: true);

                if (groups.Count == 0)
                {
                    this.LogWarn(
                        $"Auto-parsing did not result in any animation groups. Check naming."
                    );
                    return;
                }

                int removedCount = animationData.RemoveAll(data => data.isCreatedFromAutoParse);
                this.Log($"Removed {removedCount} previously auto-parsed animation entries.");

                int added = ApplyAutoParseGroups(groups);
                this.Log($"Auto-parsed into {added} new animation groups.");
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

        private static string StripDensitySuffix(string name)
        {
            // Removes common density suffixes like @2x, @0.5x, @3x at the end
            return Regex.Replace(name, @"@\d+(?:\.\d+)?x$", string.Empty);
        }

        private static bool TryExtractBaseAndIndex(string name, out string baseName, out int index)
        {
            baseName = null;
            index = -1;

            // Try patterns in order of specificity
            // 1) name(001), name (1)
            Match m = s_ParenIndexRegex.Match(name);
            if (m.Success)
            {
                baseName = m.Groups["base"].Value;
                _ = int.TryParse(m.Groups["index"].Value, out index);
                baseName = baseName.TrimEnd('_', '-', '.', ' ');
                return true;
            }

            // 2) name_001, name-001, name 001, name.001 (last numeric segment)
            m = s_SeparatorIndexRegex.Match(name);
            if (m.Success)
            {
                baseName = m.Groups["base"].Value;
                _ = int.TryParse(m.Groups["index"].Value, out index);
                baseName = baseName.TrimEnd('_', '-', '.', ' ');
                return true;
            }

            // 3) name001 (trailing digits without separators)
            m = s_TrailingIndexRegex.Match(name);
            if (m.Success && m.Groups["base"].Length > 0)
            {
                baseName = m.Groups["base"].Value;
                _ = int.TryParse(m.Groups["index"].Value, out index);
                baseName = baseName.TrimEnd('_', '-', '.', ' ');
                return true;
            }

            // Not found
            return false;
        }

        private int ComputeSourcesHash()
        {
            unchecked
            {
                int hash = 17;
                if (animationSources != null)
                {
                    for (int i = 0; i < animationSources.Count; i++)
                    {
                        Object src = animationSources[i];
                        int id = src != null ? src.GetInstanceID() : 0;
                        string path = src != null ? AssetDatabase.GetAssetPath(src) : string.Empty;
                        hash = hash * 31 + id;
                        hash = hash * 31 + (path?.GetHashCode() ?? 0);
                    }
                }
                return hash;
            }
        }

        private Dictionary<
            string,
            Dictionary<string, List<(int index, Sprite sprite)>>
        > GroupFilteredSprites(bool withProgress)
        {
            Dictionary<
                string,
                Dictionary<string, List<(int index, Sprite sprite)>>
            > spritesByBaseAndAssetPath = new(StringComparer.Ordinal);

            int total = _filteredSprites.Count;
            int processed = 0;

            foreach (Sprite sprite in _filteredSprites)
            {
                processed++;
                if (withProgress && (processed % 10 == 0 || processed == total))
                {
                    EditorUtility.DisplayProgressBar(
                        "Auto-Parsing Sprites",
                        $"Processing: {sprite.name} ({processed}/{total})",
                        (float)processed / total
                    );
                }

                string assetPath = AssetDatabase.GetAssetPath(sprite);
                string directoryPath =
                    Path.GetDirectoryName(assetPath).SanitizePath() ?? string.Empty;
                string frameName = StripDensitySuffix(sprite.name);

                string baseName;
                int frameIndex;

                if (useCustomGroupRegex && _compiledGroupRegex != null)
                {
                    Match m = _compiledGroupRegex.Match(frameName);
                    if (m.Success)
                    {
                        Group baseGroup = m.Groups["base"];
                        Group indexGroup = m.Groups["index"];
                        baseName = baseGroup.Success ? baseGroup.Value : frameName;
                        frameIndex =
                            indexGroup.Success && int.TryParse(indexGroup.Value, out int idx)
                                ? idx
                                : -1;
                    }
                    else if (!TryExtractBaseAndIndex(frameName, out baseName, out frameIndex))
                    {
                        baseName = frameName;
                        frameIndex = -1;
                    }
                }
                else if (!TryExtractBaseAndIndex(frameName, out baseName, out frameIndex))
                {
                    baseName = frameName;
                    frameIndex = -1;
                }

                if (string.IsNullOrWhiteSpace(baseName))
                {
                    this.LogWarn(
                        $"Could not extract valid base name for '{frameName}' at '{assetPath}'. Skipping."
                    );
                    continue;
                }

                if (!spritesByBaseAndAssetPath.TryGetValue(directoryPath, out var byBase))
                {
                    byBase = new Dictionary<string, List<(int index, Sprite sprite)>>(
                        groupingCaseInsensitive
                            ? StringComparer.OrdinalIgnoreCase
                            : StringComparer.Ordinal
                    );
                    spritesByBaseAndAssetPath.Add(directoryPath, byBase);
                }

                List<(int index, Sprite sprite)> list = byBase.GetOrAdd(baseName);
                list.Add((frameIndex, sprite));
            }

            return spritesByBaseAndAssetPath;
        }

        private int ApplyAutoParseGroups(
            Dictionary<string, Dictionary<string, List<(int index, Sprite sprite)>>> groups
        )
        {
            int addedCount = 0;

            HashSet<string> usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (AnimationData data in animationData)
            {
                if (!data.isCreatedFromAutoParse && !string.IsNullOrWhiteSpace(data.animationName))
                {
                    usedNames.Add(data.animationName);
                }
            }

            foreach (
                KeyValuePair<
                    string,
                    Dictionary<string, List<(int index, Sprite sprite)>>
                > kvpAssetPath in groups
            )
            {
                string folderName = new DirectoryInfo(kvpAssetPath.Key).Name;
                foreach (
                    (string baseKey, List<(int index, Sprite sprite)> entries) in kvpAssetPath.Value
                )
                {
                    if (entries.Count == 0)
                    {
                        continue;
                    }

                    bool hasAnyIndex = entries.Any(e => e.index >= 0);
                    if (strictNumericOrdering)
                    {
                        if (hasAnyIndex)
                        {
                            entries.Sort((a, b) => a.index.CompareTo(b.index));
                        }
                        // else leave discovery order
                    }
                    else
                    {
                        if (hasAnyIndex)
                        {
                            entries.Sort((a, b) => a.index.CompareTo(b.index));
                        }
                        else
                        {
                            entries.Sort(
                                (a, b) => EditorUtility.NaturalCompare(a.sprite.name, b.sprite.name)
                            );
                        }
                    }

                    List<Sprite> framesForAnim = new List<Sprite>(entries.Count);
                    foreach (var e in entries)
                    {
                        framesForAnim.Add(e.sprite);
                    }

                    string finalAnimName = ComposeFinalName(
                        baseKey,
                        kvpAssetPath.Key,
                        usedNames,
                        out bool _
                    );
                    usedNames.Add(finalAnimName);

                    animationData.Add(
                        new AnimationData
                        {
                            frames = framesForAnim,
                            framesPerSecond = AnimationData.DefaultFramesPerSecond,
                            animationName = finalAnimName,
                            isCreatedFromAutoParse = true,
                            loop = false,
                        }
                    );
                    addedCount++;
                }
            }

            return addedCount;
        }

        private static string EnsureUniqueName(string baseName, ISet<string> used)
        {
            if (!used.Contains(baseName))
            {
                return baseName;
            }
            int counter = 2;
            string candidate;
            do
            {
                candidate = $"{baseName}_{counter}";
                counter++;
            } while (used.Contains(candidate));
            return candidate;
        }

        private string ComposeFinalName(
            string baseKey,
            string directoryPath,
            ISet<string> usedNames,
            out bool duplicateResolved
        )
        {
            string finalNameCore = baseKey;
            string folderPrefix = GetFolderPrefix(directoryPath);
            if (!string.IsNullOrEmpty(folderPrefix))
            {
                finalNameCore = folderPrefix + "_" + finalNameCore;
            }
            if (!string.IsNullOrEmpty(autoParseNamePrefix))
            {
                finalNameCore = autoParseNamePrefix + finalNameCore;
            }
            if (!string.IsNullOrEmpty(autoParseNameSuffix))
            {
                finalNameCore = finalNameCore + autoParseNameSuffix;
            }
            string finalAnimName = SanitizeName(finalNameCore);

            duplicateResolved = false;
            if (resolveDuplicateAnimationNames && usedNames != null)
            {
                if (usedNames.Contains(finalAnimName))
                {
                    finalAnimName = EnsureUniqueName(finalAnimName, usedNames);
                    duplicateResolved = true;
                }
            }
            return finalAnimName;
        }

        private string GetFolderPrefix(string directoryPath)
        {
            if (!includeFolderNameInAnimName && !includeFullFolderPathInAnimName)
            {
                return string.Empty;
            }
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return string.Empty;
            }
            string sanitized = directoryPath.Replace('\\', '/');
            if (includeFullFolderPathInAnimName)
            {
                if (sanitized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    sanitized = sanitized.Substring("Assets/".Length);
                }
                sanitized = sanitized.Trim('/');
                sanitized = sanitized.Replace('/', '_');
                return SanitizeName(sanitized);
            }
            return new DirectoryInfo(directoryPath).Name;
        }

        private void GenerateAutoParseDryRun()
        {
            _autoParseDryRun.Clear();
            Dictionary<string, Dictionary<string, List<(int index, Sprite sprite)>>> groups =
                GroupFilteredSprites(withProgress: false);

            HashSet<string> usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (AnimationData data in animationData)
            {
                if (!data.isCreatedFromAutoParse && !string.IsNullOrWhiteSpace(data.animationName))
                {
                    usedNames.Add(data.animationName);
                }
            }

            foreach (
                KeyValuePair<
                    string,
                    Dictionary<string, List<(int index, Sprite sprite)>>
                > kvp in groups
            )
            {
                string dir = kvp.Key;
                foreach ((string baseKey, List<(int index, Sprite sprite)> entries) in kvp.Value)
                {
                    bool hasAnyIndex = entries.Any(e => e.index >= 0);
                    if (strictNumericOrdering)
                    {
                        if (hasAnyIndex)
                        {
                            entries.Sort((a, b) => a.index.CompareTo(b.index));
                        }
                    }
                    else
                    {
                        if (hasAnyIndex)
                        {
                            entries.Sort((a, b) => a.index.CompareTo(b.index));
                        }
                        else
                        {
                            entries.Sort(
                                (a, b) => EditorUtility.NaturalCompare(a.sprite.name, b.sprite.name)
                            );
                        }
                    }

                    string finalName = ComposeFinalName(
                        baseKey,
                        dir,
                        usedNames,
                        out bool wasResolved
                    );
                    usedNames.Add(finalName);

                    string folderPath = dir;
                    if (!folderPath.EndsWith("/"))
                    {
                        folderPath += "/";
                    }
                    string finalAssetPath = folderPath + finalName + ".anim";

                    _autoParseDryRun.Add(
                        new AutoParseDryRunRecord
                        {
                            folderPath = dir,
                            finalName = finalName,
                            finalAssetPath = finalAssetPath,
                            count = entries.Count,
                            hasIndex = hasAnyIndex,
                            duplicateResolved = wasResolved,
                        }
                    );
                }
            }
        }

        private void GenerateAutoParsePreview()
        {
            _autoParsePreview.Clear();
            Dictionary<string, Dictionary<string, List<(int index, Sprite sprite)>>> groups =
                GroupFilteredSprites(withProgress: false);
            foreach (
                KeyValuePair<
                    string,
                    Dictionary<string, List<(int index, Sprite sprite)>>
                > dir in groups
            )
            {
                string folderName = new DirectoryInfo(dir.Key).Name;
                foreach ((string baseKey, List<(int index, Sprite sprite)> entries) in dir.Value)
                {
                    AutoParsePreviewRecord rec = new()
                    {
                        folder = folderName,
                        baseName = baseKey,
                        count = entries.Count,
                        hasIndex = entries.Any(e => e.index >= 0),
                    };
                    _autoParsePreview.Add(rec);
                }
            }
            _autoParsePreview.Sort(
                (a, b) => string.Compare(a.folder, b.folder, StringComparison.Ordinal)
            );
        }
    }

#endif
}

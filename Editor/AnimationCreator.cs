namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;
    using Core.Extension;
    using Object = UnityEngine.Object;

    [Serializable]
    public sealed class AnimationData
    {
        public const int DefaultFramesPerSecond = 12;

        public List<Sprite> frames = new();
        public int framesPerSecond = DefaultFramesPerSecond;
        public string animationName = string.Empty;
        public bool isCreatedFromAutoParse;
        public bool loop;
    }

    public sealed class AnimationCreator : ScriptableWizard
    {
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

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Animation Creator", priority = -3)]
        public static void CreateAnimation()
        {
            DisplayWizard<AnimationCreator>("Animation Creator", "Create", "Update Metadata");
        }

        private void OnWizardUpdate()
        {
            helpString = "";
            errorString = "";

            if (
                animationSources is not { Count: not 0 }
                || animationSources.TrueForAll(s => s == null)
            )
            {
                errorString = "Please specify at least one Animation Source (folder).";
            }

            UpdateRegex();
            FindAndFilterSprites();
        }

        private void OnWizardOtherButton()
        {
            OnWizardUpdate();
        }

        private void OnWizardCreate()
        {
            if (animationData is not { Count: not 0 })
            {
                this.LogError($"No animation data to create.");
                return;
            }

            int totalAnimations = animationData.Count;
            int currentAnimationIndex = 0;
            bool errorOccurred = false;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (AnimationData data in animationData)
                {
                    currentAnimationIndex++;
                    string animationName = data.animationName;
                    if (string.IsNullOrWhiteSpace(animationName))
                    {
                        this.LogWarn(
                            $"Ignoring animation data entry {currentAnimationIndex} without an animation name."
                        );
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(
                        "Creating Animations",
                        $"Processing '{animationName}' ({currentAnimationIndex}/{totalAnimations})",
                        (float)currentAnimationIndex / totalAnimations
                    );

                    int framesPerSecond = data.framesPerSecond;
                    if (framesPerSecond <= 0)
                    {
                        this.LogWarn(
                            $"Ignoring animation '{animationName}' with invalid FPS ({framesPerSecond})."
                        );
                        continue;
                    }

                    List<Sprite> frames = data.frames;
                    if (frames is not { Count: not 0 })
                    {
                        this.LogWarn(
                            $"Ignoring animation '{animationName}' because it has no frames."
                        );
                        continue;
                    }

                    frames.Sort((s1, s2) => EditorUtility.NaturalCompare(s1.name, s2.name));

                    List<ObjectReferenceKeyframe> keyFrames = new(frames.Count);
                    float timeStep = 1f / framesPerSecond;
                    float currentTime = 0f;

                    foreach (Sprite sprite in frames)
                    {
                        if (sprite == null)
                        {
                            continue;
                        }

                        ObjectReferenceKeyframe keyFrame = new()
                        {
                            time = currentTime,
                            value = sprite,
                        };
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

                    AnimationClip animationClip = new();
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

                    string firstFramePath = AssetDatabase.GetAssetPath(frames[0]);
                    string assetPath =
                        Path.GetDirectoryName(firstFramePath)?.Replace("\\", "/") ?? "Assets";
                    if (!assetPath.EndsWith("/"))
                    {
                        assetPath += "/";
                    }

                    string finalPath = AssetDatabase.GenerateUniqueAssetPath(
                        assetPath + animationName + ".anim"
                    );
                    AssetDatabase.CreateAsset(animationClip, finalPath);
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

        protected override bool DrawWizardGUI()
        {
            SerializedObject serializedObject = new(this);
            List<Object> oldAnimationSources = animationSources.ToList();
            serializedObject.Update();
            bool guiChanged = base.DrawWizardGUI();
            if (
                serializedObject.ApplyModifiedProperties()
                || !oldAnimationSources.SequenceEqual(animationSources)
            )
            {
                UpdateRegex();
                FindAndFilterSprites();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sprite Filter Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Regex Pattern:", spriteNameRegex);
            EditorGUILayout.LabelField("Matched Sprites:", _matchedSpriteCount.ToString());
            EditorGUILayout.LabelField("Unmatched Sprites:", _unmatchedSpriteCount.ToString());
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            if (animationData is { Count: > 0 } && _filteredSprites.Count > 0)
            {
                if (
                    GUILayout.Button(
                        $"Populate First Slot with {_filteredSprites.Count} Matched Sprites"
                    )
                )
                {
                    if (animationData[0].frames.Count > 0)
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
                            return guiChanged;
                        }
                    }

                    animationData[0].frames = new List<Sprite>(_filteredSprites);
                    animationData[0].animationName = "All_Matched_Sprites";
                    animationData[0].isCreatedFromAutoParse = false;

                    guiChanged = true;
                    Repaint();
                }
            }
            else if (animationData is not { Count: not 0 })
            {
                EditorGUILayout.HelpBox(
                    "Add an Animation Data entry to populate.",
                    MessageType.Warning
                );
            }
            else if (_filteredSprites.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No sprites matched the filter criteria.",
                    MessageType.Info
                );
            }

            if (_filteredSprites.Count > 0)
            {
                if (GUILayout.Button("Auto-Parse Matched Sprites into Animations"))
                {
                    if (
                        !EditorUtility.DisplayDialog(
                            "Confirm Auto-Parse",
                            "This will replace the current animation list with animations generated from matched sprites based on their names (e.g., 'Player_Run_0', 'Player_Run_1'). Are you sure?",
                            "Parse",
                            "Cancel"
                        )
                    )
                    {
                        return guiChanged;
                    }

                    AutoParseSprites();
                    guiChanged = true;
                    Repaint();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Cannot Auto-Parse: No matched sprites found or Animation Sources are empty.",
                    MessageType.Info
                );
            }

            if (
                animationData is { Count: > 0 }
                && animationData.Any(data => data.frames?.Count > 0)
                && !string.IsNullOrWhiteSpace(text)
            )
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Bulk Naming Operations", EditorStyles.boldLabel);

                if (GUILayout.Button($"Append '{text}' To All Animation Names"))
                {
                    foreach (AnimationData data in animationData)
                    {
                        if (
                            !string.IsNullOrEmpty(data.animationName)
                            && !data.animationName.EndsWith("_" + text)
                        )
                        {
                            data.animationName += "_" + text;
                            guiChanged = true;
                        }
                    }

                    if (guiChanged)
                    {
                        Repaint();
                    }
                }

                if (GUILayout.Button($"Remove '{text}' From End of Names"))
                {
                    string suffix = "_" + text;
                    foreach (AnimationData data in animationData)
                    {
                        if (
                            !string.IsNullOrEmpty(data.animationName)
                            && data.animationName.EndsWith(suffix)
                        )
                        {
                            data.animationName = data.animationName.Remove(
                                data.animationName.Length - suffix.Length
                            );
                            guiChanged = true;
                        }
                        else if (
                            !string.IsNullOrEmpty(data.animationName)
                            && data.animationName.EndsWith(text)
                        )
                        {
                            data.animationName = data.animationName.Remove(
                                data.animationName.Length - text.Length
                            );
                            guiChanged = true;
                        }
                    }

                    if (guiChanged)
                    {
                        Repaint();
                    }
                }
            }
            else if (
                animationData is { Count: > 0 }
                && animationData.Any(data => data.frames?.Count > 0)
            )
            {
                EditorGUILayout.HelpBox(
                    "Enter text in the 'Text' field above to enable bulk naming operations.",
                    MessageType.Info
                );
            }
            return guiChanged;
        }

        private void UpdateRegex()
        {
            if (_compiledRegex == null || _lastUsedRegex != spriteNameRegex)
            {
                try
                {
                    _compiledRegex = new Regex(spriteNameRegex, RegexOptions.Compiled);
                    _lastUsedRegex = spriteNameRegex;
                    errorString = "";
                }
                catch (ArgumentException ex)
                {
                    _compiledRegex = null;
                    errorString = $"Invalid Regex: {ex.Message}";
                    this.LogError($"Invalid Regex '{spriteNameRegex}': {ex.Message}");
                }
            }
        }

        private void FindAndFilterSprites()
        {
            _filteredSprites.Clear();
            _matchedSpriteCount = 0;
            _unmatchedSpriteCount = 0;

            if (animationSources is not { Count: not 0 } || _compiledRegex == null)
            {
                return;
            }

            List<string> searchPaths = new();
            foreach (Object source in animationSources)
            {
                if (source == null)
                {
                    continue;
                }

                string path = AssetDatabase.GetAssetPath(source);
                if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
                {
                    searchPaths.Add(path);
                }
                else
                {
                    this.LogWarn($"Source '{source.name}' is not a valid folder. Skipping.");
                }
            }

            if (searchPaths.Count == 0)
            {
                return;
            }

            string[] assetGuids = AssetDatabase.FindAssets("t:sprite", searchPaths.ToArray());
            float totalAssets = assetGuids.Length;

            try
            {
                for (int i = 0; i < totalAssets; i++)
                {
                    string guid = assetGuids[i];
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    float progress = (i + 1) / totalAssets;
                    EditorUtility.DisplayProgressBar(
                        "Finding and Filtering Sprites",
                        $"Checking: {Path.GetFileName(path)} ({i + 1}/{assetGuids.Length})",
                        progress
                    );

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

            try
            {
                foreach (Sprite sprite in _filteredSprites)
                {
                    processedCount++;
                    EditorUtility.DisplayProgressBar(
                        "Auto-Parsing Sprites",
                        $"Processing: {sprite.name} ({processedCount}/{totalSprites})",
                        (float)processedCount / totalSprites
                    );

                    string assetPath = AssetDatabase.GetAssetPath(sprite);
                    string directoryPath =
                        Path.GetDirectoryName(assetPath)?.Replace("\\", "/") ?? "";
                    string frameName = sprite.name;

                    int splitIndex = frameName.Length;
                    for (int i = frameName.Length - 1; i >= 0; i--)
                    {
                        if (!char.IsDigit(frameName[i]))
                        {
                            splitIndex = frameName[i] == '_' ? i : i + 1;
                            break;
                        }

                        if (i == 0)
                        {
                            splitIndex = 0;
                        }
                    }

                    if (
                        splitIndex > 0
                        && frameName[splitIndex - 1] == '_'
                        && splitIndex < frameName.Length
                        && char.IsDigit(frameName[splitIndex])
                    )
                    {
                        splitIndex--;
                    }

                    if (splitIndex > 0 && splitIndex < frameName.Length)
                    {
                        string prefix = frameName.Substring(0, splitIndex);
                        if (!string.IsNullOrWhiteSpace(prefix))
                        {
                            Dictionary<string, List<Sprite>> spritesByPrefix =
                                spritesByPrefixAndAssetPath.GetOrAdd(directoryPath);
                            spritesByPrefix.GetOrAdd(prefix).Add(sprite);
                        }
                        else
                        {
                            this.LogWarn(
                                $"Could not extract valid prefix for frame '{frameName}' at path '{assetPath}'. Skipping."
                            );
                        }
                    }
                    else
                    {
                        this.LogWarn(
                            $"Failed to determine animation group prefix for frame '{frameName}' at path '{assetPath}'. Skipping."
                        );
                    }
                }

                if (spritesByPrefixAndAssetPath.Count > 0)
                {
                    animationData.RemoveAll(data => data.isCreatedFromAutoParse);

                    foreach (
                        KeyValuePair<
                            string,
                            Dictionary<string, List<Sprite>>
                        > kvpAssetPath in spritesByPrefixAndAssetPath
                    )
                    {
                        string dirName = new DirectoryInfo(kvpAssetPath.Key).Name;

                        foreach (KeyValuePair<string, List<Sprite>> kvpPrefix in kvpAssetPath.Value)
                        {
                            kvpPrefix.Value.Sort(
                                (s1, s2) => EditorUtility.NaturalCompare(s1.name, s2.name)
                            );

                            animationData.Add(
                                new AnimationData
                                {
                                    frames = kvpPrefix.Value,
                                    framesPerSecond = AnimationData.DefaultFramesPerSecond,
                                    animationName = $"Anim_{dirName}_{kvpPrefix.Key}",
                                    isCreatedFromAutoParse = true,
                                }
                            );
                        }
                    }

                    this.Log($"Auto-parsed into {animationData.Count} animation groups.");
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
            }
        }
    }

#endif
}

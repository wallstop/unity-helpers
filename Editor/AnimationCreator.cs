namespace UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Extension;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [Serializable]
    public sealed class AnimationData
    {
        public const int DefaultFramesPerSecond = 12;

        public List<Texture2D> frames = new();
        public int framesPerSecond = DefaultFramesPerSecond;
        public string animationName = string.Empty;
    }

    public sealed class AnimationCreator : ScriptableWizard
    {
        public List<AnimationData> animationData = new() { new AnimationData() };
        public List<Object> animationSources = new();
        public string text;

        [MenuItem("Tools/Unity Helpers/Animation Creator", priority = -3)]
        public static void CreateAnimation()
        {
            _ = DisplayWizard<AnimationCreator>("Animation Creator", "Create");
        }

        protected override bool DrawWizardGUI()
        {
            bool returnValue = base.DrawWizardGUI();

            if (animationData is { Count: 1 })
            {
                AnimationData data = animationData[0];

                bool filled = false;
                if (
                    data.frames is { Count: 0 }
                    && GUILayout.Button("Fill Sprites From Animation Sources")
                )
                {
                    List<string> animationPaths = new();
                    foreach (Object animationSource in animationSources)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(animationSource);
                        animationPaths.Add(assetPath);
                    }

                    foreach (
                        string assetGuid in AssetDatabase.FindAssets(
                            "t:sprite",
                            animationPaths.ToArray()
                        )
                    )
                    {
                        string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        if (sprite != null && sprite.texture != null)
                        {
                            data.frames.Add(sprite.texture);
                        }
                    }

                    filled = true;
                }

                if (
                    data.frames is { Count: > 0 }
                    && (filled || GUILayout.Button("Auto Parse Sprites"))
                )
                {
                    Dictionary<
                        string,
                        Dictionary<string, List<Texture2D>>
                    > texturesByPrefixAndAssetPath = new();
                    foreach (Texture2D frame in data.frames)
                    {
                        string assetPathWithFrameName = AssetDatabase.GetAssetPath(frame);
                        string frameName = frame.name;
                        string assetPath = assetPathWithFrameName.Substring(
                            0,
                            assetPathWithFrameName.LastIndexOf(frameName, StringComparison.Ordinal)
                        );
                        int lastNumericIndex = frameName.Length - 1;
                        for (int i = frameName.Length - 1; 0 <= i; --i)
                        {
                            if (char.IsNumber(frameName[i]))
                            {
                                continue;
                            }

                            lastNumericIndex = i + 1;
                            break;
                        }

                        int lastUnderscoreIndex = frameName.LastIndexOf('_');
                        int lastIndex =
                            lastUnderscoreIndex == lastNumericIndex - 1
                                ? lastUnderscoreIndex
                                : Math.Max(lastUnderscoreIndex, lastNumericIndex);
                        if (0 < lastIndex)
                        {
                            Dictionary<string, List<Texture2D>> texturesByPrefix =
                                texturesByPrefixAndAssetPath.GetOrAdd(assetPath);
                            string key = frameName.Substring(0, lastIndex);
                            texturesByPrefix.GetOrAdd(key).Add(frame);
                        }
                        else
                        {
                            this.LogWarn("Failed to process frame {0}.", frameName);
                        }
                    }

                    if (0 < texturesByPrefixAndAssetPath.Count)
                    {
                        animationData.Clear();
                        animationData.AddRange(
                            texturesByPrefixAndAssetPath.SelectMany(assetPathAndTextures =>
                                assetPathAndTextures.Value.Select(
                                    textureAndPrefix => new AnimationData
                                    {
                                        frames = textureAndPrefix.Value,
                                        framesPerSecond = data.framesPerSecond,
                                        animationName = $"Anim_{textureAndPrefix.Key}",
                                    }
                                )
                            )
                        );
                    }
                }
            }

            if (
                animationData is { Count: > 0 }
                && animationData.Any(data => data.frames is { Count: > 0 })
                && !string.IsNullOrWhiteSpace(text)
            )
            {
                if (GUILayout.Button("Append Text To All Animations"))
                {
                    foreach (AnimationData data in animationData)
                    {
                        data.animationName += "_" + text;
                    }
                }

                if (GUILayout.Button("Remove Text From All Animations"))
                {
                    foreach (AnimationData data in animationData)
                    {
                        if (data.animationName.EndsWith(text))
                        {
                            data.animationName = data.animationName.Remove(
                                data.animationName.Length - text.Length
                            );
                        }
                    }
                }
            }

            return returnValue;
        }

        private void OnWizardCreate()
        {
            if (animationData is not { Count: > 0 })
            {
                return;
            }

            foreach (AnimationData data in animationData)
            {
                string animationName = data.animationName;
                if (string.IsNullOrWhiteSpace(animationName))
                {
                    this.LogWarn("Ignoring animationData without an animation name.");
                    continue;
                }

                int framesPerSecond = data.framesPerSecond;
                if (framesPerSecond <= 0)
                {
                    this.LogWarn(
                        "Ignoring animationData with FPS of {0} with name {1}.",
                        framesPerSecond,
                        animationName
                    );
                    continue;
                }

                List<Texture2D> frames = data.frames;
                if (frames is not { Count: > 0 })
                {
                    this.LogWarn(
                        "Ignoring animationData without frames with name {0}.",
                        animationName
                    );
                    continue;
                }

                List<ObjectReferenceKeyframe> keyFrames = new(frames.Count);

                float currentTime = 0f;
                float timeStep = 1f / framesPerSecond;
                foreach (Texture2D frame in frames)
                {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                        AssetDatabase.GetAssetPath(frame)
                    );
                    if (sprite == null)
                    {
                        continue;
                    }

                    ObjectReferenceKeyframe keyFrame = new() { time = currentTime, value = sprite };
                    keyFrames.Add(keyFrame);

                    currentTime += timeStep;
                }

                if (keyFrames.Count <= 0)
                {
                    this.LogWarn(
                        "Ignoring animationData with empty frames with name {0}.",
                        animationName
                    );
                    continue;
                }

                AnimationClip animationClip = new();
                AnimationUtility.SetObjectReferenceCurve(
                    animationClip,
                    EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
                    keyFrames.ToArray()
                );
                string assetPathWithFileNameAndExtension = AssetDatabase.GetAssetPath(frames[0]);
                string assetPath = assetPathWithFileNameAndExtension.Substring(
                    0,
                    assetPathWithFileNameAndExtension.LastIndexOf("/", StringComparison.Ordinal) + 1
                );

                ProjectWindowUtil.CreateAsset(animationClip, assetPath + animationName + ".anim");
            }
        }
    }
#endif
}

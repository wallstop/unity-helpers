namespace UnityHelpers.Editor
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Reflection;
    using Core.Attributes;
    using Core.Helper;
    using UnityEngine;
    using UnityEditor;
    using Utils;

    // https://gist.githubusercontent.com/yujen/5e1cd78e2a341260b38029de08a449da/raw/ac60c1002e0e14375de5b2b0a167af00df3f74b4/SeniaAnimationEventEditor.cs
    public sealed class AnimationEventEditor : EditorWindow
    {
        private static readonly IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> TypesToMethods;

        static AnimationEventEditor()
        {
            Dictionary<Type, IReadOnlyList<MethodInfo>> typesToMethods = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass)
                .Where(type => typeof(MonoBehaviour).IsAssignableFrom(type))
                .ToDictionary(
                    type => type,
                    type => (IReadOnlyList<MethodInfo>)type.GetPossibleAnimatorEventsForType());
            foreach (KeyValuePair<Type, IReadOnlyList<MethodInfo>> entry in typesToMethods.ToList())
            {
                if (entry.Value.Count <= 0)
                {
                    _ = typesToMethods.Remove(entry.Key);
                }
            }

            TypesToMethods = typesToMethods;
        }


        [MenuItem("Tools/Unity Helpers/AnimationEvent Editor")]
        private static void AnimationEventEditorMenu()
        {
            GetWindow(typeof(AnimationEventEditor));
        }

        public class AnimationEventItem
        {
            public AnimationEventItem(AnimationEvent animationEvent)
            {
                this.animationEvent = animationEvent;
                search = string.Empty;
            }

            public Type selectedType;
            public MethodInfo selectedMethod;
            public string search;
            public AnimationEvent animationEvent;
            public Texture2D texture;
            public bool isTextureReadable;
            public bool isInvalidTextureRect;
            public Sprite sprite;
            public int? originalIndex;
            public bool overrideEnumValues;
        }

        private IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> Lookup =>
            _explicitMode ? AnimationEventHelpers.TypesToMethods : TypesToMethods;

        private int MaxFrameIndex =>
            _currentClip == null ? 0 : (int)Math.Round(_currentClip.frameRate * _currentClip.length);

        private Vector2 _scrollPosition;
        private Animator _sourceAnimator;
        private AnimationClip _currentClip;
        private bool _explicitMode = true;
        private bool _controlFrameTime = false;
        private string _animationSearchString = string.Empty;
        private List<ObjectReferenceKeyframe> _referenceCurve;

        private readonly List<AnimationEvent> _baseClipEvents = new();
        private readonly List<AnimationEventItem> _state = new();
        private readonly Dictionary<AnimationEventItem, string> _lastSeenSearch = new();

        private readonly Dictionary<AnimationEventItem, IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>>> _lookups =
            new();

        private int _selectedFrameIndex = -1;

        private void OnGUI()
        {
            Animator tmpAnimator = EditorGUILayout.ObjectField(
                "Animator Object", _sourceAnimator, typeof(Animator), true) as Animator;
            if (tmpAnimator == null)
            {
                _sourceAnimator = null;
                _state.Clear();
                return;
            }

            if (_sourceAnimator != tmpAnimator)
            {
                _sourceAnimator = tmpAnimator;
                _currentClip = null;
            }

            _explicitMode = EditorGUILayout.Toggle(
                new GUIContent(
                    "Explicit Mode", "If true, restricts results to only those that explicitly with [AnimationEvent]"),
                _explicitMode);
            _controlFrameTime = EditorGUILayout.Toggle(
                new GUIContent(
                    "Control Frame Time",
                    "Select to edit precise time of animation events instead of snapping to nearest frame"),
                _controlFrameTime);

            AnimationClip selectedClip = DrawAndFilterAnimationClips();
            if (selectedClip == null)
            {
                return;
            }

            if (_currentClip != selectedClip)
            {
                _currentClip = selectedClip;
                RefreshAnimationEvents();
            }

            _selectedFrameIndex = EditorGUILayout.IntField("FrameIndex", _selectedFrameIndex);

            float frameRate = _currentClip.frameRate;
            float oldFrameRate = frameRate;
            if (GUILayout.Button("Add Event"))
            {
                if (0 <= _selectedFrameIndex)
                {
                    _state.Add(new AnimationEventItem(new AnimationEvent { time = _selectedFrameIndex / frameRate }));
                }
            }

            frameRate = _currentClip.frameRate = EditorGUILayout.FloatField("FrameRate", frameRate);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Need a copy because we might be mutating it
            foreach (AnimationEventItem item in _state.ToList())
            {
                AnimationEvent animEvent = item.animationEvent;

                int frame = Mathf.RoundToInt(animEvent.time * oldFrameRate);
                EditorGUILayout.PrefixLabel("Frame " + frame);

                DrawSpritePreview(item);

                EditorGUI.indentLevel++;

                RenderAnimationEventItem(item, frame, frameRate);

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();

            DrawControlButtons();
        }

        private AnimationClip DrawAndFilterAnimationClips()
        {
            _animationSearchString = EditorGUILayout.TextField("Animation Search", _animationSearchString);
            List<AnimationClip> animationClips = _sourceAnimator.runtimeAnimatorController.animationClips.ToList();
            int selectedIndex;
            if (string.IsNullOrEmpty(_animationSearchString) || _animationSearchString == "*")
            {
                selectedIndex = EditorGUILayout.Popup(
                    "Animation", animationClips.IndexOf(_currentClip),
                    animationClips.Select(clip => clip.name).ToArray());
            }
            else
            {
                List<string> searchTerms = _animationSearchString
                    .Split(" ")
                    .Select(searchPart => searchPart.ToLowerInvariant().Trim())
                    .Where(trimmed => !string.IsNullOrEmpty(trimmed) && trimmed != "*")
                    .ToList();

                if (0 < searchTerms.Count)
                {
                    foreach (AnimationClip animationClip in animationClips.ToList())
                    {
                        if (_currentClip == animationClip)
                        {
                            continue;
                        }

                        foreach (string searchTerm in searchTerms)
                        {
                            if (animationClip.name.ToLowerInvariant().Contains(searchTerm))
                            {
                                continue;
                            }

                            animationClips.Remove(animationClip);
                        }
                    }
                }

                selectedIndex = EditorGUILayout.Popup(
                    "Animation", animationClips.IndexOf(_currentClip),
                    animationClips.Select(clip => clip.name).ToArray());
            }

            if (selectedIndex < 0)
            {
                _currentClip = null;
                RefreshAnimationEvents();
                return null;
            }

            return animationClips[selectedIndex];
        }

        private int AnimationEventComparison(AnimationEventItem lhs, AnimationEventItem rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return 0;
            }

            if (ReferenceEquals(null, rhs))
            {
                return -1;
            }

            if (ReferenceEquals(null, lhs))
            {
                return 1;
            }

            return AnimationEventEqualityComparer.Instance.Compare(lhs.animationEvent, rhs.animationEvent);
        }

        private void DrawControlButtons()
        {
            if (_baseClipEvents.SequenceEqual(
                    _state.Select(item => item.animationEvent), AnimationEventEqualityComparer.Instance))
            {
                GUILayout.Label("No changes detected...");
                return;
            }

            Color oldColor = GUI.color;
            GUI.color = Color.green;
            if (GUILayout.Button("Save"))
            {
                SaveAnimation();
                AssetDatabase.SaveAssets();
            }

            GUI.color = oldColor;
            if (GUILayout.Button("Reset"))
            {
                RefreshAnimationEvents();
            }

            if (!_state.SequenceEqual(
                    _state.OrderBy(item => item.animationEvent, AnimationEventEqualityComparer.Instance)))
            {
                if (GUILayout.Button("Re-Order"))
                {
                    _state.Sort(AnimationEventComparison);
                }
            }
        }

        private void RenderAnimationEventItem(AnimationEventItem item, int frame, float frameRate)
        {
            int index = _state.IndexOf(item);
            EditorGUILayout.BeginHorizontal();
            try
            {
                if (1 <= index && Math.Abs(_state[index - 1].animationEvent.time - item.animationEvent.time) < 0.001f &&
                    GUILayout.Button("Move Up"))
                {
                    _state.RemoveAt(index);
                    _state.Insert(index - 1, item);
                }

                if (index < _state.Count - 1 &&
                    Math.Abs(_state[index + 1].animationEvent.time - item.animationEvent.time) < 0.001f &&
                    GUILayout.Button("Move Down"))
                {
                    _state.RemoveAt(index);
                    _state.Insert(index + 1, item);
                }

                if (0 <= index && index < _baseClipEvents.Count &&
                    !AnimationEventEqualityComparer.Instance.Equals(item.animationEvent, _baseClipEvents[index]) &&
                    GUILayout.Button("Reset"))
                {
                    AnimationEventEqualityComparer.Instance.CopyInto(item.animationEvent, _baseClipEvents[index]);
                    item.selectedType = null;
                    item.selectedMethod = null;
                }

                if (GUILayout.Button($"Remove Event at frame {frame}"))
                {
                    _state.Remove(item);
                    return;
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }

            IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> lookup = FilterLookup(item);

            TryPopulateTypeAndMethod(item, lookup);

            List<Type> orderedTypes = lookup.Keys.OrderBy(type => type.FullName).Take(20).ToList();
            if (item.selectedType != null && !orderedTypes.Contains(item.selectedType))
            {
                orderedTypes.Add(item.selectedType);
            }

            string[] orderedTypeNames = orderedTypes.Select(type => type.FullName).ToArray();

            SelectFrameTime(item, frame, frameRate);

            SelectFunctionName(item);

            if (!SelectTypes(item, orderedTypes, orderedTypeNames))
            {
                return;
            }

            if (!SelectMethods(item, lookup))
            {
                return;
            }

            RenderEventParameters(item);
        }

        private void SelectFrameTime(AnimationEventItem item, int frame, float frameRate)
        {
            AnimationEvent animEvent = item.animationEvent;
            float previousTime = animEvent.time;
            if (_controlFrameTime)
            {
                float proposedFrameTime = EditorGUILayout.FloatField("FrameTime", animEvent.time);
                animEvent.time = Mathf.Clamp(proposedFrameTime, 0, _currentClip.length);
            }
            else
            {
                int proposedFrame = EditorGUILayout.IntField("FrameIndex", frame);
                animEvent.time = Mathf.Clamp(proposedFrame, 0, MaxFrameIndex) / frameRate;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (previousTime != animEvent.time)
            {
                item.texture = null;
            }
        }

        private void SelectFunctionName(AnimationEventItem item)
        {
            AnimationEvent animEvent = item.animationEvent;
            animEvent.functionName = EditorGUILayout.TextField("FunctionName", animEvent.functionName ?? string.Empty);
            if (!_explicitMode)
            {
                item.search = EditorGUILayout.TextField("Search", item.search);
            }
        }

        private void TryPopulateTypeAndMethod(
            AnimationEventItem item, IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> lookup)
        {
            if (item.selectedType != null)
            {
                return;
            }

            AnimationEvent animEvent = item.animationEvent;
            foreach (KeyValuePair<Type, IReadOnlyList<MethodInfo>> entry in lookup.OrderBy(kvp => kvp.Key.FullName))
            {
                foreach (MethodInfo method in entry.Value)
                {
                    if (string.Equals(method.Name, animEvent.functionName, StringComparison.Ordinal))
                    {
                        item.selectedType = entry.Key;
                        item.selectedMethod = method;
                        return;
                    }
                }
            }
        }

        private bool SelectTypes(AnimationEventItem item, IList<Type> orderedTypes, string[] orderedTypeNames)
        {
            int existingIndex = orderedTypes.IndexOf(item.selectedType);
            int selectedTypeIndex = EditorGUILayout.Popup("TypeName", existingIndex, orderedTypeNames);
            item.selectedType = selectedTypeIndex < 0 ? null : orderedTypes[selectedTypeIndex];
            if (existingIndex != selectedTypeIndex)
            {
                item.selectedMethod = null;
            }

            return item.selectedType != null;
        }

        private bool SelectMethods(AnimationEventItem item, IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> lookup)
        {
            AnimationEvent animEvent = item.animationEvent;
            if (!lookup.TryGetValue(item.selectedType, out IReadOnlyList<MethodInfo> methods))
            {
                methods = new List<MethodInfo>(0);
            }

            if (item.selectedMethod == null || !methods.Contains(item.selectedMethod))
            {
                foreach (MethodInfo method in methods)
                {
                    if (string.Equals(method.Name, animEvent.functionName, StringComparison.Ordinal))
                    {
                        item.selectedMethod = method;
                        break;
                    }
                }

                if (item.selectedMethod != null && !methods.Contains(item.selectedMethod))
                {
                    methods = methods.Concat(Enumerables.Of(item.selectedMethod)).ToList();
                }
            }

            int selectedMethodIndex = EditorGUILayout.Popup(
                "MethodName", methods.ToList().IndexOf(item.selectedMethod),
                methods.Select(method => method.Name).ToArray());
            if (0 <= selectedMethodIndex)
            {
                item.selectedMethod = methods[selectedMethodIndex];
                animEvent.functionName = item.selectedMethod.Name;
                return true;
            }

            return false;
        }

        private void RenderEventParameters(AnimationEventItem item)
        {
            AnimationEvent animEvent = item.animationEvent;
            ParameterInfo[] arrayParameterInfo = item.selectedMethod.GetParameters();
            if (arrayParameterInfo.Length == 1)
            {
                EditorGUI.indentLevel++;

                Type paramType = arrayParameterInfo[0].ParameterType;
                if (paramType == typeof(int))
                {
                    animEvent.intParameter = EditorGUILayout.IntField("IntParameter", animEvent.intParameter);
                }
                else if (paramType.BaseType == typeof(Enum))
                {
                    string[] enumNamesArray = Enum.GetNames(paramType);
                    List<string> enumNames = enumNamesArray.ToList();
                    string enumName = Enum.GetName(paramType, animEvent.intParameter);

                    int index = EditorGUILayout.Popup($"{paramType.Name}", enumNames.IndexOf(enumName), enumNamesArray);
                    if (0 <= index)
                    {
                        animEvent.intParameter = (int)Enum.Parse(paramType, enumNames[index]);
                    }

                    item.overrideEnumValues = EditorGUILayout.Toggle("Override", item.overrideEnumValues);
                    if (item.overrideEnumValues)
                    {
                        animEvent.intParameter = EditorGUILayout.IntField("IntParameter", animEvent.intParameter);
                    }
                }
                else if (paramType == typeof(float))
                {
                    animEvent.floatParameter = EditorGUILayout.FloatField(
                        "FloatParameter", animEvent.floatParameter);
                }
                else if (paramType == typeof(string))
                {
                    animEvent.stringParameter = EditorGUILayout.TextField(
                        "StringParameter", animEvent.stringParameter);
                }
                else if (paramType == typeof(UnityEngine.Object))
                {
                    animEvent.objectReferenceParameter = EditorGUILayout.ObjectField(
                        "ObjectReferenceParameter", animEvent.objectReferenceParameter, typeof(UnityEngine.Object),
                        true);
                }

                EditorGUI.indentLevel--;
            }
        }

        private IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> FilterLookup(AnimationEventItem item)
        {
            IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> lookup;
            if (!_explicitMode)
            {
                if (!_lastSeenSearch.TryGetValue(item, out string lastSearch) || !string.Equals(
                        lastSearch, item.search, StringComparison.InvariantCultureIgnoreCase) ||
                    !_lookups.TryGetValue(item, out lookup))
                {
                    Dictionary<Type, List<MethodInfo>> filtered = Lookup.ToDictionary(
                        kvp => kvp.Key, kvp => kvp.Value.ToList());
                    List<string> searchTerms = item.search
                        .Split(" ")
                        .Select(searchTerm => searchTerm.Trim().ToLowerInvariant())
                        .Where(trimmed => !string.IsNullOrEmpty(trimmed) && trimmed != "*")
                        .ToList();

                    if (0 < searchTerms.Count)
                    {
                        foreach (KeyValuePair<Type, List<MethodInfo>> entry in filtered.ToList())
                        {
                            foreach (string searchTerm in searchTerms)
                            {
                                if (entry.Key.FullName.ToLowerInvariant().Contains(searchTerm))
                                {
                                    continue;
                                }

                                if (entry.Value.Any(
                                        methodInfo => methodInfo.Name.ToLowerInvariant().Contains(searchTerm)))
                                {
                                    continue;
                                }

                                _ = filtered.Remove(entry.Key);
                                break;
                            }

                        }
                    }

                    _lookups[item] = lookup = filtered.ToDictionary(
                        kvp => kvp.Key, kvp => (IReadOnlyList<MethodInfo>)kvp.Value);
                    _lastSeenSearch[item] = item.search;
                }
            }
            else
            {
                lookup = Lookup;
            }

            return lookup;
        }

        private void DrawSpritePreview(AnimationEventItem item)
        {
            SetupPreviewData(item);

            string spriteName = item.sprite == null ? string.Empty : item.sprite.name;
            if (item.texture != null)
            {
                GUILayout.Label(item.texture);
            }
            else if (!item.isTextureReadable && !string.IsNullOrEmpty(spriteName))
            {
                EditorGUILayout.BeginHorizontal();
                try
                {
                    GUILayout.Label($"Sprite '{spriteName}' required \"Read/Write\" enabled");
                    if (item.sprite != null && GUILayout.Button("Fix"))
                    {
                        string assetPath = AssetDatabase.GetAssetPath(item.sprite.texture);
                        if (string.IsNullOrEmpty(assetPath))
                        {
                            return;
                        }

                        TextureImporter tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                        if (tImporter == null)
                        {
                            return;
                        }

                        tImporter.isReadable = true;
                        EditorUtility.SetDirty(tImporter);
                        tImporter.SaveAndReimport();
                        EditorUtility.SetDirty(item.sprite);
                    }
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
            else if (item.isInvalidTextureRect && !string.IsNullOrEmpty(spriteName))
            {
                GUILayout.Label($"Sprite '{spriteName}' is packed too tightly inside its texture");
            }
        }

        private void SetupPreviewData(AnimationEventItem item)
        {
            if (item.texture != null)
            {
                return;
            }

            if (TryFindSpriteForEvent(item, out Sprite currentSprite))
            {
                item.sprite = currentSprite;
                item.isTextureReadable = currentSprite.texture.isReadable;
                item.isInvalidTextureRect = false;
                if (item.isTextureReadable)
                {
                    Rect? maybeTextureRect = null;
                    try
                    {
                        maybeTextureRect = currentSprite.textureRect;
                    }
                    catch
                    {
                        item.isInvalidTextureRect = true;
                    }

                    if (maybeTextureRect != null)
                    {
                        Rect textureRect = maybeTextureRect.Value;
                        item.texture = CopyTexture(textureRect, currentSprite.texture);
                    }
                }
            }
            else
            {
                item.sprite = null;
                item.isTextureReadable = false;
            }
        }

        private bool TryFindSpriteForEvent(AnimationEventItem item, out Sprite sprite)
        {
            sprite = null;
            foreach (ObjectReferenceKeyframe keyFrame in _referenceCurve)
            {
                if (keyFrame.time <= item.animationEvent.time)
                {
                    Sprite frameSprite = keyFrame.value as Sprite;
                    if (frameSprite == null)
                    {
                        continue;
                    }

                    sprite = frameSprite;
                    continue;
                }

                return sprite != null;
            }

            return sprite != null;
        }

        private Texture2D CopyTexture(Rect textureRect, Texture2D sourceTexture)
        {
            int width = (int)Math.Ceiling(textureRect.width);
            int height = (int)Math.Ceiling(textureRect.height);
            Texture2D texture = new(width, height)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
            };
            Vector2 offset = textureRect.position;
            int offsetX = (int)Math.Ceiling(offset.x);
            int offsetY = (int)Math.Ceiling(offset.y);
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    Color sourcePixel = sourceTexture.GetPixel(offsetX + x, offsetY + y);
                    texture.SetPixel(x, y, sourcePixel);
                }
            }

            texture.Apply();
            return texture;
        }

        private void RefreshAnimationEvents()
        {
            _state.Clear();
            _baseClipEvents.Clear();
            _lookups.Clear();
            _lastSeenSearch.Clear();
            if (_currentClip == null)
            {
                return;
            }

            for (int i = 0; i < _currentClip.events.Length; i++)
            {
                AnimationEvent animEvent = _currentClip.events[i];
                _state.Add(
                    new AnimationEventItem(animEvent)
                    {
                        originalIndex = i
                    });
                _baseClipEvents.Add(AnimationEventEqualityComparer.Instance.Copy(animEvent));
            }

            _selectedFrameIndex = MaxFrameIndex;
            _referenceCurve = AnimationUtility.GetObjectReferenceCurve(
                _currentClip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite")).ToList();
            _referenceCurve.Sort(
                (lhs, rhs) =>
                {
                    int comparison = lhs.time.CompareTo(rhs.time);
                    if (comparison != 0)
                    {
                        return comparison;
                    }

                    string lhsName = lhs.value == null ? string.Empty : lhs.value.name ?? string.Empty;
                    string rhsName = rhs.value == null ? string.Empty : rhs.value.name ?? string.Empty;
                    return string.Compare(lhsName, rhsName, StringComparison.OrdinalIgnoreCase);
                });
        }

        private void SaveAnimation()
        {
            if (_currentClip != null)
            {
                AnimationUtility.SetAnimationEvents(_currentClip, _state.Select(item => item.animationEvent).ToArray());
                EditorUtility.SetDirty(_currentClip);
                _baseClipEvents.Clear();
                foreach (AnimationEventItem item in _state)
                {
                    _baseClipEvents.Add(AnimationEventEqualityComparer.Instance.Copy(item.animationEvent));
                }
            }
        }
    }
}

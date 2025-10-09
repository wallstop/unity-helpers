namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;

    // https://gist.githubusercontent.com/yujen/5e1cd78e2a341260b38029de08a449da/raw/ac60c1002e0e14375de5b2b0a167af00df3f74b4/SeniaAnimationEventEditor.cs
    public sealed class AnimationEventEditor : EditorWindow
    {
        private static IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> _cachedTypesToMethods;

        private static IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> TypesToMethods
        {
            get
            {
                if (_cachedTypesToMethods == null)
                {
                    InitializeTypeCache();
                }
                return _cachedTypesToMethods;
            }
        }

        private static void InitializeTypeCache()
        {
            Dictionary<Type, IReadOnlyList<MethodInfo>> typesToMethods = AppDomain
                .CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass)
                .Where(type => typeof(MonoBehaviour).IsAssignableFrom(type))
                .ToDictionary(
                    type => type,
                    type =>
                        (IReadOnlyList<MethodInfo>)
                            AnimationEventHelpers.GetPossibleAnimatorEventsForType(type)
                );
            foreach (KeyValuePair<Type, IReadOnlyList<MethodInfo>> entry in typesToMethods.ToList())
            {
                if (entry.Value.Count <= 0)
                {
                    _ = typesToMethods.Remove(entry.Key);
                }
            }

            _cachedTypesToMethods = typesToMethods;
        }

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/AnimationEvent Editor")]
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
                typeSearch = string.Empty;
            }

            public Type selectedType;
            public MethodInfo selectedMethod;
            public string search;
            public string typeSearch;
            public AnimationEvent animationEvent;
            public Texture2D texture;
            public bool isTextureReadable;
            public bool isInvalidTextureRect;
            public Sprite sprite;
            public int? originalIndex;
            public bool overrideEnumValues;
            public bool isValid = true;
            public string validationMessage = string.Empty;

            // Cache for filtered lookup
            public IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> cachedLookup;
            public string lastSearchForCache;
        }

        private IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> Lookup =>
            _explicitMode ? AnimationEventHelpers.TypesToMethods : TypesToMethods;

        private int MaxFrameIndex =>
            _currentClip == null
                ? 0
                : (int)Math.Round(_currentClip.frameRate * _currentClip.length);

        private Vector2 _scrollPosition;
        private Animator _sourceAnimator;
        private AnimationClip _currentClip;
        private bool _explicitMode = true;
        private bool _controlFrameTime;
        private string _animationSearchString = string.Empty;
        private List<ObjectReferenceKeyframe> _referenceCurve;

        // Cached frame rate to detect changes
        private float _cachedFrameRate;
        private bool _frameRateChanged;

        private readonly List<AnimationEvent> _baseClipEvents = new();
        private readonly List<AnimationEventItem> _state = new();

        // Cache for sprite previews
        private readonly Dictionary<Sprite, Texture2D> _spriteTextureCache = new();

        // Cache for filtered animation clips
        private List<AnimationClip> _filteredClips;
        private string _lastAnimationSearch;

        private int _selectedFrameIndex = -1;

        // Keyboard shortcut state
        private int _focusedEventIndex = -1;

        private void OnGUI()
        {
            HandleKeyboardShortcuts();

            Animator tmpAnimator =
                EditorGUILayout.ObjectField(
                    "Animator Object",
                    _sourceAnimator,
                    typeof(Animator),
                    true
                ) as Animator;
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
                    "Explicit Mode",
                    "If true, restricts results to only those that explicitly with [AnimationEvent]"
                ),
                _explicitMode
            );
            _controlFrameTime = EditorGUILayout.Toggle(
                new GUIContent(
                    "Control Frame Time",
                    "Select to edit precise time of animation events instead of snapping to nearest frame"
                ),
                _controlFrameTime
            );

            AnimationClip selectedClip = DrawAndFilterAnimationClips();
            if (selectedClip == null)
            {
                return;
            }

            if (_currentClip != selectedClip)
            {
                _currentClip = selectedClip;
                _cachedFrameRate = _currentClip.frameRate;
                RefreshAnimationEvents();
            }

            _selectedFrameIndex = EditorGUILayout.IntField("FrameIndex", _selectedFrameIndex);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Event"))
                {
                    if (0 <= _selectedFrameIndex)
                    {
                        AddNewEvent(_selectedFrameIndex / _cachedFrameRate);
                    }
                }

                if (GUILayout.Button("Add Event at Time 0"))
                {
                    AddNewEvent(0f);
                }
            }

            // Frame rate with change detection and undo support
            EditorGUI.BeginChangeCheck();
            float newFrameRate = EditorGUILayout.FloatField("FrameRate", _cachedFrameRate);
            if (EditorGUI.EndChangeCheck())
            {
                _frameRateChanged = true;
                _cachedFrameRate = newFrameRate;
            }

            if (_frameRateChanged)
            {
                EditorGUILayout.HelpBox(
                    "Frame rate will be saved when you click 'Save'. Click 'Reset' to revert.",
                    MessageType.Info
                );
            }

            DrawGuiLine(height: 5, color: new Color(0f, 0.5f, 1f, 1f));
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Use cached list to avoid allocations
            int stateCount = _state.Count;
            for (int i = 0; i < stateCount; ++i)
            {
                AnimationEventItem item = _state[i];
                AnimationEvent animEvent = item.animationEvent;

                int frame = Mathf.RoundToInt(animEvent.time * _cachedFrameRate);

                // Highlight focused event
                Color oldBgColor = GUI.backgroundColor;
                if (i == _focusedEventIndex)
                {
                    GUI.backgroundColor = new Color(0.3f, 0.6f, 1f, 0.3f);
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = oldBgColor;

                EditorGUILayout.PrefixLabel("Frame " + frame);

                DrawSpritePreview(item);

                using (new EditorGUI.IndentLevelScope())
                {
                    RenderAnimationEventItem(item, frame, i);
                    if (i != stateCount - 1)
                    {
                        DrawGuiLine(height: 3, color: new Color(0f, 1f, 0.3f, 1f));
                        EditorGUILayout.Space();
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            DrawControlButtons();

            // Show keyboard shortcuts help
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Shortcuts: Delete (remove event), Ctrl+D (duplicate), Up/Down (navigate)",
                EditorStyles.miniLabel
            );
        }

        private void HandleKeyboardShortcuts()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown)
            {
                return;
            }

            if (_state.Count == 0)
            {
                return;
            }

            // Ensure focused index is valid
            if (_focusedEventIndex < 0 || _focusedEventIndex >= _state.Count)
            {
                _focusedEventIndex = 0;
            }

            switch (e.keyCode)
            {
                case KeyCode.Delete:
                    if (_focusedEventIndex >= 0 && _focusedEventIndex < _state.Count)
                    {
                        RecordUndo("Delete Animation Event");
                        _state.RemoveAt(_focusedEventIndex);
                        _focusedEventIndex = Mathf.Clamp(_focusedEventIndex, 0, _state.Count - 1);
                        e.Use();
                        Repaint();
                    }
                    break;

                case KeyCode.D:
                    if (e.control && _focusedEventIndex >= 0 && _focusedEventIndex < _state.Count)
                    {
                        RecordUndo("Duplicate Animation Event");
                        AnimationEventItem original = _state[_focusedEventIndex];
                        AnimationEvent duplicated = AnimationEventEqualityComparer.Instance.Copy(
                            original.animationEvent
                        );
                        _state.Insert(_focusedEventIndex + 1, new AnimationEventItem(duplicated));
                        _focusedEventIndex++;
                        e.Use();
                        Repaint();
                    }
                    break;

                case KeyCode.UpArrow:
                    _focusedEventIndex = Mathf.Max(0, _focusedEventIndex - 1);
                    e.Use();
                    Repaint();
                    break;

                case KeyCode.DownArrow:
                    _focusedEventIndex = Mathf.Min(_state.Count - 1, _focusedEventIndex + 1);
                    e.Use();
                    Repaint();
                    break;
            }
        }

        private void AddNewEvent(float time)
        {
            RecordUndo("Add Animation Event");
            _state.Add(new AnimationEventItem(new AnimationEvent { time = time }));
        }

        private AnimationClip DrawAndFilterAnimationClips()
        {
            EditorGUI.BeginChangeCheck();
            _animationSearchString = EditorGUILayout.TextField(
                "Animation Search",
                _animationSearchString
            );
            bool searchChanged = EditorGUI.EndChangeCheck();

            // Cache filtered clips
            if (
                _filteredClips == null
                || searchChanged
                || _lastAnimationSearch != _animationSearchString
            )
            {
                _filteredClips = FilterAnimationClips(
                    _sourceAnimator.runtimeAnimatorController.animationClips.ToList()
                );
                _lastAnimationSearch = _animationSearchString;
            }

            int selectedIndex = EditorGUILayout.Popup(
                "Animation",
                _filteredClips.IndexOf(_currentClip),
                _filteredClips.Select(clip => clip.name).ToArray()
            );

            if (selectedIndex < 0)
            {
                _currentClip = null;
                RefreshAnimationEvents();
                return null;
            }

            return _filteredClips[selectedIndex];
        }

        private List<AnimationClip> FilterAnimationClips(List<AnimationClip> clips)
        {
            if (
                string.IsNullOrEmpty(_animationSearchString)
                || string.Equals(_animationSearchString, "*", StringComparison.Ordinal)
            )
            {
                return clips;
            }

            List<string> searchTerms = _animationSearchString
                .Split(" ")
                .Select(searchPart => searchPart.ToLowerInvariant().Trim())
                .Where(trimmed =>
                    !string.IsNullOrEmpty(trimmed)
                    && !string.Equals(trimmed, "*", StringComparison.Ordinal)
                )
                .ToList();

            if (searchTerms.Count == 0)
            {
                return clips;
            }

            List<AnimationClip> filtered = new List<AnimationClip>();
            foreach (AnimationClip clip in clips)
            {
                bool matches = true;
                foreach (string searchTerm in searchTerms)
                {
                    if (!clip.name.ToLowerInvariant().Contains(searchTerm))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches || clip == _currentClip)
                {
                    filtered.Add(clip);
                }
            }

            return filtered;
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

            return AnimationEventEqualityComparer.Instance.Compare(
                lhs.animationEvent,
                rhs.animationEvent
            );
        }

        private void DrawControlButtons()
        {
            if (
                _baseClipEvents.SequenceEqual(
                    _state.Select(item => item.animationEvent),
                    AnimationEventEqualityComparer.Instance
                ) && !_frameRateChanged
            )
            {
                GUILayout.Label("No changes detected...");
                return;
            }

            Color oldColor = GUI.color;
            GUI.color = Color.green;
            if (GUILayout.Button("Save"))
            {
                SaveAnimation();
            }

            GUI.color = oldColor;
            if (GUILayout.Button("Reset"))
            {
                RefreshAnimationEvents();
            }

            if (
                !_state.SequenceEqual(
                    _state.OrderBy(
                        item => item.animationEvent,
                        AnimationEventEqualityComparer.Instance
                    )
                )
            )
            {
                if (GUILayout.Button("Re-Order"))
                {
                    RecordUndo("Re-order Animation Events");
                    _state.Sort(AnimationEventComparison);
                }
            }
        }

        private void RenderAnimationEventItem(AnimationEventItem item, int frame, int itemIndex)
        {
            int index = itemIndex;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (
                    1 <= index
                    && Math.Abs(_state[index - 1].animationEvent.time - item.animationEvent.time)
                        < 0.001f
                    && GUILayout.Button("Move Up")
                )
                {
                    RecordUndo("Move Animation Event Up");
                    _state.RemoveAt(index);
                    _state.Insert(index - 1, item);
                    _focusedEventIndex = index - 1;
                }

                if (
                    index < _state.Count - 1
                    && Math.Abs(_state[index + 1].animationEvent.time - item.animationEvent.time)
                        < 0.001f
                    && GUILayout.Button("Move Down")
                )
                {
                    RecordUndo("Move Animation Event Down");
                    _state.RemoveAt(index);
                    _state.Insert(index + 1, item);
                    _focusedEventIndex = index + 1;
                }

                if (
                    item.originalIndex.HasValue
                    && item.originalIndex.Value >= 0
                    && item.originalIndex.Value < _baseClipEvents.Count
                    && !AnimationEventEqualityComparer.Instance.Equals(
                        item.animationEvent,
                        _baseClipEvents[item.originalIndex.Value]
                    )
                    && GUILayout.Button("Reset")
                )
                {
                    RecordUndo("Reset Animation Event");
                    AnimationEventEqualityComparer.Instance.CopyInto(
                        item.animationEvent,
                        _baseClipEvents[item.originalIndex.Value]
                    );
                    item.selectedType = null;
                    item.selectedMethod = null;
                    item.cachedLookup = null;
                }

                if (GUILayout.Button($"Remove Event at frame {frame}"))
                {
                    RecordUndo("Remove Animation Event");
                    _state.Remove(item);
                    return;
                }

                if (GUILayout.Button("Duplicate", GUILayout.Width(80)))
                {
                    RecordUndo("Duplicate Animation Event");
                    AnimationEvent duplicated = AnimationEventEqualityComparer.Instance.Copy(
                        item.animationEvent
                    );
                    _state.Insert(index + 1, new AnimationEventItem(duplicated));
                }
            }

            IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> lookup = FilterLookup(item);

            TryPopulateTypeAndMethod(item, lookup);
            ValidateEvent(item, lookup);

            SelectFrameTime(item, frame);

            SelectFunctionName(item);

            // Show validation status
            if (!item.isValid)
            {
                EditorGUILayout.HelpBox(item.validationMessage, MessageType.Warning);
            }

            if (!SelectTypes(item, lookup))
            {
                return;
            }

            if (!SelectMethods(item, lookup))
            {
                return;
            }

            RenderEventParameters(item);
        }

        private void SelectFrameTime(AnimationEventItem item, int frame)
        {
            AnimationEvent animEvent = item.animationEvent;
            EditorGUI.BeginChangeCheck();
            float proposedTime;

            if (_controlFrameTime)
            {
                proposedTime = EditorGUILayout.FloatField("FrameTime", animEvent.time);
                proposedTime = Mathf.Clamp(proposedTime, 0, _currentClip.length);
            }
            else
            {
                int proposedFrame = EditorGUILayout.IntField("FrameIndex", frame);
                proposedTime = Mathf.Clamp(proposedFrame, 0, MaxFrameIndex) / _cachedFrameRate;
            }

            if (EditorGUI.EndChangeCheck())
            {
                RecordUndo("Change Animation Event Time");
                animEvent.time = proposedTime;
                // Invalidate texture cache when time changes
                item.texture = null;
            }
        }

        private void SelectFunctionName(AnimationEventItem item)
        {
            AnimationEvent animEvent = item.animationEvent;
            EditorGUI.BeginChangeCheck();
            string newFunctionName = EditorGUILayout.TextField(
                "FunctionName",
                animEvent.functionName ?? string.Empty
            );
            if (EditorGUI.EndChangeCheck())
            {
                RecordUndo("Change Animation Event Function");
                animEvent.functionName = newFunctionName;
                item.selectedType = null;
                item.selectedMethod = null;
            }

            if (!_explicitMode)
            {
                EditorGUI.BeginChangeCheck();
                item.search = EditorGUILayout.TextField("Method Search", item.search);
                if (EditorGUI.EndChangeCheck())
                {
                    item.cachedLookup = null;
                }

                EditorGUI.BeginChangeCheck();
                item.typeSearch = EditorGUILayout.TextField("Type Search", item.typeSearch);
                if (EditorGUI.EndChangeCheck())
                {
                    item.cachedLookup = null;
                }
            }
        }

        private void ValidateEvent(
            AnimationEventItem item,
            IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> lookup
        )
        {
            item.isValid = true;
            item.validationMessage = string.Empty;

            if (string.IsNullOrEmpty(item.animationEvent.functionName))
            {
                item.isValid = false;
                item.validationMessage = "Function name is empty";
                return;
            }

            if (item.selectedType == null || item.selectedMethod == null)
            {
                // Try to find matching method
                bool found = false;
                foreach (var entry in lookup)
                {
                    foreach (var method in entry.Value)
                    {
                        if (
                            string.Equals(
                                method.Name,
                                item.animationEvent.functionName,
                                StringComparison.Ordinal
                            )
                        )
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        break;
                }

                if (!found)
                {
                    item.isValid = false;
                    item.validationMessage =
                        $"No method named '{item.animationEvent.functionName}' found in available types";
                }
            }
        }

        private void TryPopulateTypeAndMethod(
            AnimationEventItem item,
            IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> lookup
        )
        {
            if (item.selectedType != null)
            {
                return;
            }

            if (lookup == null)
            {
                return;
            }

            AnimationEvent animEvent = item.animationEvent;
            foreach (
                KeyValuePair<Type, IReadOnlyList<MethodInfo>> entry in lookup.OrderBy(kvp =>
                    kvp.Key.FullName
                )
            )
            {
                foreach (MethodInfo method in entry.Value)
                {
                    if (
                        string.Equals(method.Name, animEvent.functionName, StringComparison.Ordinal)
                    )
                    {
                        item.selectedType = entry.Key;
                        item.selectedMethod = method;
                        return;
                    }
                }
            }
        }

        private bool SelectTypes(
            AnimationEventItem item,
            IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> lookup
        )
        {
            if (lookup == null || lookup.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No types with animation event methods found",
                    MessageType.Info
                );
                return false;
            }

            // Get all types, but prioritize the selected one and filter by search
            List<Type> allTypes = lookup.Keys.OrderBy(type => type.FullName).ToList();
            List<Type> filteredTypes = allTypes;

            // Apply type search filter
            if (!string.IsNullOrEmpty(item.typeSearch))
            {
                string searchLower = item.typeSearch.ToLowerInvariant();
                filteredTypes = allTypes
                    .Where(t => t.FullName.ToLowerInvariant().Contains(searchLower))
                    .ToList();

                // Always include selected type even if it doesn't match search
                if (item.selectedType != null && !filteredTypes.Contains(item.selectedType))
                {
                    filteredTypes.Insert(0, item.selectedType);
                }
            }

            // Limit to reasonable number, but show more if searching
            int limit = string.IsNullOrEmpty(item.typeSearch) ? 50 : 200;
            List<Type> displayTypes = filteredTypes.Take(limit).ToList();

            if (item.selectedType != null && !displayTypes.Contains(item.selectedType))
            {
                displayTypes.Insert(0, item.selectedType);
            }

            string[] orderedTypeNames = displayTypes.Select(type => type.FullName).ToArray();

            int existingIndex = displayTypes.IndexOf(item.selectedType);

            EditorGUI.BeginChangeCheck();
            int selectedTypeIndex = EditorGUILayout.Popup(
                "TypeName",
                existingIndex,
                orderedTypeNames
            );

            if (EditorGUI.EndChangeCheck())
            {
                RecordUndo("Change Animation Event Type");
                item.selectedType = selectedTypeIndex < 0 ? null : displayTypes[selectedTypeIndex];
                item.selectedMethod = null;
            }

            if (filteredTypes.Count > limit)
            {
                EditorGUILayout.HelpBox(
                    $"Showing {limit} of {filteredTypes.Count} types. Use Type Search to filter.",
                    MessageType.Info
                );
            }

            return item.selectedType != null;
        }

        private bool SelectMethods(
            AnimationEventItem item,
            IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> lookup
        )
        {
            if (lookup == null)
            {
                return false;
            }

            AnimationEvent animEvent = item.animationEvent;
            if (!lookup.TryGetValue(item.selectedType, out IReadOnlyList<MethodInfo> methods))
            {
                methods = new List<MethodInfo>(0);
            }

            List<MethodInfo> methodsList = methods.ToList();

            if (item.selectedMethod == null || !methodsList.Contains(item.selectedMethod))
            {
                foreach (MethodInfo method in methodsList)
                {
                    if (
                        string.Equals(method.Name, animEvent.functionName, StringComparison.Ordinal)
                    )
                    {
                        item.selectedMethod = method;
                        break;
                    }
                }

                if (item.selectedMethod != null && !methodsList.Contains(item.selectedMethod))
                {
                    methodsList.Add(item.selectedMethod);
                }
            }

            EditorGUI.BeginChangeCheck();
            int selectedMethodIndex = EditorGUILayout.Popup(
                "MethodName",
                methodsList.IndexOf(item.selectedMethod),
                methodsList.Select(method => method.Name).ToArray()
            );

            if (EditorGUI.EndChangeCheck() && selectedMethodIndex >= 0)
            {
                RecordUndo("Change Animation Event Method");
                item.selectedMethod = methodsList[selectedMethodIndex];
                animEvent.functionName = item.selectedMethod.Name;
            }

            return item.selectedMethod != null;
        }

        private void RenderEventParameters(AnimationEventItem item)
        {
            AnimationEvent animEvent = item.animationEvent;
            ParameterInfo[] arrayParameterInfo = item.selectedMethod.GetParameters();
            if (arrayParameterInfo.Length == 1)
            {
                using EditorGUI.IndentLevelScope indent = new();

                Type paramType = arrayParameterInfo[0].ParameterType;
                EditorGUI.BeginChangeCheck();

                if (paramType == typeof(int))
                {
                    int newValue = EditorGUILayout.IntField("IntParameter", animEvent.intParameter);
                    if (EditorGUI.EndChangeCheck())
                    {
                        RecordUndo("Change Animation Event Parameter");
                        animEvent.intParameter = newValue;
                    }
                }
                else if (paramType.BaseType == typeof(Enum))
                {
                    string[] enumNamesArray = Enum.GetNames(paramType);
                    List<string> enumNames = enumNamesArray.ToList();
                    string enumName = Enum.GetName(paramType, animEvent.intParameter);

                    int index = EditorGUILayout.Popup(
                        $"{paramType.Name}",
                        enumNames.IndexOf(enumName),
                        enumNamesArray
                    );

                    bool checkEnded = EditorGUI.EndChangeCheck();
                    if (checkEnded && index >= 0)
                    {
                        RecordUndo("Change Animation Event Parameter");
                        animEvent.intParameter = (int)Enum.Parse(paramType, enumNames[index]);
                    }

                    item.overrideEnumValues = EditorGUILayout.Toggle(
                        "Override",
                        item.overrideEnumValues
                    );
                    if (item.overrideEnumValues)
                    {
                        EditorGUI.BeginChangeCheck();
                        int overrideValue = EditorGUILayout.IntField(
                            "IntParameter",
                            animEvent.intParameter
                        );
                        if (EditorGUI.EndChangeCheck())
                        {
                            RecordUndo("Change Animation Event Parameter");
                            animEvent.intParameter = overrideValue;
                        }
                    }
                }
                else if (paramType == typeof(float))
                {
                    float newValue = EditorGUILayout.FloatField(
                        "FloatParameter",
                        animEvent.floatParameter
                    );
                    if (EditorGUI.EndChangeCheck())
                    {
                        RecordUndo("Change Animation Event Parameter");
                        animEvent.floatParameter = newValue;
                    }
                }
                else if (paramType == typeof(string))
                {
                    string newValue = EditorGUILayout.TextField(
                        "StringParameter",
                        animEvent.stringParameter
                    );
                    if (EditorGUI.EndChangeCheck())
                    {
                        RecordUndo("Change Animation Event Parameter");
                        animEvent.stringParameter = newValue;
                    }
                }
                else if (paramType == typeof(UnityEngine.Object))
                {
                    UnityEngine.Object newValue = EditorGUILayout.ObjectField(
                        "ObjectReferenceParameter",
                        animEvent.objectReferenceParameter,
                        typeof(UnityEngine.Object),
                        true
                    );
                    if (EditorGUI.EndChangeCheck())
                    {
                        RecordUndo("Change Animation Event Parameter");
                        animEvent.objectReferenceParameter = newValue;
                    }
                }
                else
                {
                    // End the change check that was started
                    EditorGUI.EndChangeCheck();
                }
            }
        }

        private IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> FilterLookup(
            AnimationEventItem item
        )
        {
            IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> lookup;
            if (!_explicitMode)
            {
                string currentSearch = item.search + "|" + item.typeSearch;

                // Use cached lookup if search hasn't changed
                if (item.cachedLookup != null && item.lastSearchForCache == currentSearch)
                {
                    return item.cachedLookup;
                }

                Dictionary<Type, List<MethodInfo>> filtered =
                    new Dictionary<Type, List<MethodInfo>>();

                List<string> methodSearchTerms = item
                    .search.Split(" ")
                    .Select(searchTerm => searchTerm.Trim().ToLowerInvariant())
                    .Where(trimmed =>
                        !string.IsNullOrEmpty(trimmed)
                        && !string.Equals(trimmed, "*", StringComparison.Ordinal)
                    )
                    .ToList();

                List<string> typeSearchTerms = item
                    .typeSearch.Split(" ")
                    .Select(searchTerm => searchTerm.Trim().ToLowerInvariant())
                    .Where(trimmed =>
                        !string.IsNullOrEmpty(trimmed)
                        && !string.Equals(trimmed, "*", StringComparison.Ordinal)
                    )
                    .ToList();

                foreach (var kvp in Lookup)
                {
                    Type type = kvp.Key;

                    // Filter by type search
                    if (typeSearchTerms.Count > 0)
                    {
                        bool typeMatches = true;
                        string typeLower = type.FullName.ToLowerInvariant();
                        foreach (string searchTerm in typeSearchTerms)
                        {
                            if (!typeLower.Contains(searchTerm))
                            {
                                typeMatches = false;
                                break;
                            }
                        }
                        if (!typeMatches)
                        {
                            continue;
                        }
                    }

                    List<MethodInfo> methodList = kvp.Value.ToList();

                    // Filter by method search
                    if (methodSearchTerms.Count > 0)
                    {
                        List<MethodInfo> filteredMethods = new List<MethodInfo>();
                        foreach (MethodInfo method in methodList)
                        {
                            bool methodMatches = true;
                            string methodLower = method.Name.ToLowerInvariant();
                            foreach (string searchTerm in methodSearchTerms)
                            {
                                if (!methodLower.Contains(searchTerm))
                                {
                                    methodMatches = false;
                                    break;
                                }
                            }
                            if (methodMatches)
                            {
                                filteredMethods.Add(method);
                            }
                        }

                        if (filteredMethods.Count > 0)
                        {
                            filtered[type] = filteredMethods;
                        }
                    }
                    else
                    {
                        filtered[type] = methodList;
                    }
                }

                item.cachedLookup = lookup = filtered.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IReadOnlyList<MethodInfo>)kvp.Value
                );
                item.lastSearchForCache = currentSearch;
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
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label($"Sprite '{spriteName}' required \"Read/Write\" enabled");
                    if (item.sprite != null && GUILayout.Button("Fix"))
                    {
                        string assetPath = AssetDatabase.GetAssetPath(item.sprite.texture);
                        if (string.IsNullOrEmpty(assetPath))
                        {
                            return;
                        }

                        TextureImporter tImporter =
                            AssetImporter.GetAtPath(assetPath) as TextureImporter;
                        if (tImporter == null)
                        {
                            return;
                        }

                        Undo.RecordObject(tImporter, "Enable Texture Read/Write");
                        tImporter.isReadable = true;
                        EditorUtility.SetDirty(tImporter);
                        tImporter.SaveAndReimport();
                        EditorUtility.SetDirty(item.sprite);
                    }
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

                // Try to use cached texture first
                if (_spriteTextureCache.TryGetValue(currentSprite, out Texture2D cachedTexture))
                {
                    item.texture = cachedTexture;
                    item.isTextureReadable = true;
                    item.isInvalidTextureRect = false;
                    return;
                }

                // Try AssetPreview first (doesn't require Read/Write)
                Texture2D preview = AssetPreview.GetAssetPreview(currentSprite);
                if (preview != null)
                {
                    item.texture = preview;
                    _spriteTextureCache[currentSprite] = preview;
                    item.isTextureReadable = true;
                    item.isInvalidTextureRect = false;
                    return;
                }

                // Fall back to manual copy if texture is readable
                item.isTextureReadable = currentSprite.texture.isReadable;
                item.isInvalidTextureRect = false;
                if (item.isTextureReadable)
                {
                    Rect? maybeTextureRect = null;
                    try
                    {
                        maybeTextureRect = currentSprite.textureRect;
                    }
                    catch (Exception)
                    {
                        item.isInvalidTextureRect = true;
                    }

                    if (maybeTextureRect != null)
                    {
                        Rect textureRect = maybeTextureRect.Value;
                        Texture2D copiedTexture = CopyTexture(textureRect, currentSprite.texture);
                        item.texture = copiedTexture;
                        _spriteTextureCache[currentSprite] = copiedTexture;
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
            if (_referenceCurve == null || _referenceCurve.Count == 0)
            {
                return false;
            }

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
            _spriteTextureCache.Clear();
            _focusedEventIndex = -1;
            _frameRateChanged = false;

            if (_currentClip == null)
            {
                return;
            }

            _cachedFrameRate = _currentClip.frameRate;

            for (int i = 0; i < _currentClip.events.Length; i++)
            {
                AnimationEvent animEvent = _currentClip.events[i];
                _state.Add(new AnimationEventItem(animEvent) { originalIndex = i });
                _baseClipEvents.Add(AnimationEventEqualityComparer.Instance.Copy(animEvent));
            }

            _selectedFrameIndex = MaxFrameIndex;
            _referenceCurve = AnimationUtility
                .GetObjectReferenceCurve(
                    _currentClip,
                    EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite")
                )
                .ToList();
            _referenceCurve.Sort(
                (lhs, rhs) =>
                {
                    int comparison = lhs.time.CompareTo(rhs.time);
                    if (comparison != 0)
                    {
                        return comparison;
                    }

                    string lhsName =
                        lhs.value == null ? string.Empty : lhs.value.name ?? string.Empty;
                    string rhsName =
                        rhs.value == null ? string.Empty : rhs.value.name ?? string.Empty;
                    return string.Compare(lhsName, rhsName, StringComparison.OrdinalIgnoreCase);
                }
            );
        }

        private void RecordUndo(string operationName)
        {
            Undo.RecordObject(this, operationName);
        }

        private void SaveAnimation()
        {
            if (_currentClip != null)
            {
                Undo.RecordObject(_currentClip, "Save Animation Events");

                AnimationUtility.SetAnimationEvents(
                    _currentClip,
                    _state.Select(item => item.animationEvent).ToArray()
                );

                // Apply frame rate changes if any
                if (_frameRateChanged)
                {
                    _currentClip.frameRate = _cachedFrameRate;
                    _frameRateChanged = false;
                }

                EditorUtility.SetDirty(_currentClip);
                AssetDatabase.SaveAssetIfDirty(_currentClip);
                _baseClipEvents.Clear();
                foreach (AnimationEventItem item in _state)
                {
                    _baseClipEvents.Add(
                        AnimationEventEqualityComparer.Instance.Copy(item.animationEvent)
                    );
                }
            }
        }

        private void DrawGuiLine(int height = 1, Color? color = null)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, height);
            rect.height = height;
            int minusWidth = EditorGUI.indentLevel * 16;
            rect.xMin += minusWidth;
            EditorGUI.DrawRect(rect, color ?? new Color(0.5f, 0.5f, 0.5f, 1f));
        }
    }
#endif
}

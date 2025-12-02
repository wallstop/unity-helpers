namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// UI Toolkit drawer for <see cref="StringInListAttribute"/> that provides search, pagination, and autocomplete.
    /// </summary>
    [CustomPropertyDrawer(typeof(StringInListAttribute))]
    public sealed class StringInListDrawer : PropertyDrawer
    {
        private sealed class PopupState
        {
            public string search = string.Empty;
            public int page;
        }

        private const float ClearWidth = 50f;
        private const float ButtonWidth = 24f;
        private const float PageLabelWidth = 90f;
        private const float PaginationButtonHeight = 20f;
        private const float DropdownBottomPadding = 6f;
        private const float NoResultsVerticalPadding = 6f;
        private const float NoResultsHorizontalPadding = 6f;
        private const float PopupWidth = 360f;
        private const float PopupMinHeight = 180f;
        private const float PopupMaxHeight = 460f;
        private const float EmptyStateHeight = 150f;
        private const float OptionBottomPadding = 6f;
        private const float OptionRowExtraHeight = 1.5f;
        private static readonly Dictionary<string, PopupState> PopupStates = new();

        private static PopupState GetOrCreateState(string key)
        {
            if (!PopupStates.TryGetValue(key, out PopupState state))
            {
                state = new PopupState();
                PopupStates[key] = state;
            }
            return state;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            StringInListAttribute stringInList = (StringInListAttribute)attribute;
            UnityEngine.Object context = property.serializedObject?.targetObject;
            string[] options = stringInList.GetOptions(context) ?? Array.Empty<string>();
            int pageSize = Mathf.Max(1, UnityHelpersSettings.GetStringInListPageLimit());
            if (options.Length > pageSize && IsSupportedSimpleProperty(property))
            {
                return EditorGUIUtility.singleLineHeight;
            }

            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            StringInListAttribute stringInList = (StringInListAttribute)attribute;
            UnityEngine.Object context = property.serializedObject?.targetObject;
            string[] options = stringInList.GetOptions(context) ?? Array.Empty<string>();
            int pageSize = Mathf.Max(1, UnityHelpersSettings.GetStringInListPageLimit());

            if (options.Length == 0)
            {
                EditorGUI.HelpBox(
                    position,
                    "No options available for StringInList.",
                    MessageType.Info
                );
                return;
            }

            if (IsSupportedArray(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            if (!IsSupportedSimpleProperty(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            if (options.Length > pageSize)
            {
                DrawPopupDropdown(position, property, label, options, pageSize, stringInList);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            string[] displayOptions = GetOptionDisplayArray(stringInList, options);
            if (property.propertyType == SerializedPropertyType.String)
            {
                int currentIndex = Array.IndexOf(options, property.stringValue);
                int newIndex = EditorGUI.Popup(position, label.text, currentIndex, displayOptions);
                if (newIndex >= 0 && newIndex < options.Length)
                {
                    property.stringValue = options[newIndex];
                }
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                string currentString;
                if (property.intValue >= 0 && property.intValue < options.Length)
                {
                    currentString = options[property.intValue];
                }
                else
                {
                    currentString = property.intValue.ToString();
                }

                int currentIndex = Array.IndexOf(options, currentString);
                int newIndex = EditorGUI.Popup(position, label.text, currentIndex, displayOptions);
                if (newIndex >= 0 && newIndex < options.Length)
                {
                    property.intValue = newIndex;
                }
            }
            EditorGUI.EndProperty();
        }

        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            StringInListAttribute stringInList = (StringInListAttribute)attribute;
            UnityEngine.Object context = property.serializedObject?.targetObject;
            string[] options = stringInList.GetOptions(context) ?? Array.Empty<string>();
            int pageSize = Mathf.Max(1, UnityHelpersSettings.GetStringInListPageLimit());

            if (options.Length == 0)
            {
                return new HelpBox(
                    "No options available for StringInList.",
                    HelpBoxMessageType.Info
                );
            }

            if (IsSupportedArray(property))
            {
                return new StringInListArrayElement(property, options, stringInList);
            }

            if (!IsSupportedSimpleProperty(property))
            {
                PropertyField fallback = new(property) { label = property.displayName };
                return fallback;
            }

            if (options.Length > pageSize)
            {
                StringInListPopupSelectorElement popupElement = new(options, stringInList);
                popupElement.BindProperty(property, property.displayName);
                return popupElement;
            }

            StringInListSelector selector = new(options, stringInList);
            selector.BindProperty(property, property.displayName);
            return selector;
        }

        private static bool IsSupportedSimpleProperty(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.String
                || property.propertyType == SerializedPropertyType.Integer;
        }

        private static bool IsSupportedArray(SerializedProperty property)
        {
            if (!property.isArray || property.propertyType != SerializedPropertyType.Generic)
            {
                return false;
            }

            string elementType = property.arrayElementType;
            return string.Equals(elementType, "string", StringComparison.Ordinal)
                || string.Equals(elementType, "int", StringComparison.Ordinal);
        }

        private static int CalculatePageCount(int pageSize, int filteredCount)
        {
            if (filteredCount <= 0)
            {
                return 1;
            }

            return (filteredCount + pageSize - 1) / pageSize;
        }

        private static void DrawPopupDropdown(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            string[] options,
            int pageSize,
            StringInListAttribute attribute
        )
        {
            string stateKey =
                $"{property.serializedObject?.targetObject?.GetInstanceID() ?? 0}:{property.propertyPath}";
            PopupState state = GetOrCreateState(stateKey);

            EditorGUI.BeginProperty(position, label, property);
            Rect fieldRect = EditorGUI.PrefixLabel(position, label);
            bool previousMixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

            string displayValue = ResolveDisplayValue(
                property,
                options,
                attribute,
                out string tooltip
            );
            GUIContent buttonContent = new(displayValue, tooltip);
            if (EditorGUI.DropdownButton(fieldRect, buttonContent, FocusType.Keyboard))
            {
                UnityEditor.PopupWindow.Show(
                    fieldRect,
                    new StringInListPopupContent(property, options, state, pageSize, attribute)
                );
            }

            EditorGUI.showMixedValue = previousMixed;

            EditorGUI.EndProperty();
        }

        private static int ResolveCurrentSelectionIndex(
            SerializedProperty property,
            string[] options
        )
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                string selected = property.stringValue ?? string.Empty;
                return Array.IndexOf(options, selected);
            }

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                int index = property.intValue;
                if (index >= 0 && index < options.Length)
                {
                    return index;
                }
            }

            return -1;
        }

        private static string ResolveDisplayValue(
            SerializedProperty property,
            string[] options,
            StringInListAttribute attribute,
            out string tooltip
        )
        {
            tooltip = string.Empty;
            if (property == null)
            {
                return string.Empty;
            }

            if (property.hasMultipleDifferentValues)
            {
                return "\u2014";
            }

            if (property.propertyType == SerializedPropertyType.String)
            {
                string selected = property.stringValue ?? string.Empty;
                return GetOptionLabel(attribute, selected, out tooltip);
            }

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                int index = property.intValue;
                if (index >= 0 && index < options.Length)
                {
                    return GetOptionLabel(attribute, options[index] ?? string.Empty, out tooltip);
                }

                return GetOptionLabel(attribute, property.intValue.ToString(), out tooltip);
            }

            return string.Empty;
        }

        private static void ApplySelection(
            SerializedProperty property,
            string[] options,
            int optionIndex
        )
        {
            if (optionIndex < 0 || optionIndex >= options.Length)
            {
                return;
            }

            if (property.propertyType == SerializedPropertyType.String)
            {
                property.stringValue = options[optionIndex] ?? string.Empty;
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = optionIndex;
            }
        }

        private sealed class StringInListPopupContent : PopupWindowContent
        {
            private readonly SerializedObject _serializedObject;
            private readonly string _propertyPath;
            private readonly string[] _options;
            private readonly PopupState _state;
            private readonly bool _isStringProperty;
            private readonly bool _isIntegerProperty;
            private readonly StringInListAttribute _attribute;
            private static readonly GUIContent PreviousPageContent = new("<", "Previous page");
            private static readonly GUIContent NextPageContent = new(">", "Next page");
            private int _pageSize;

            public StringInListPopupContent(
                SerializedProperty property,
                string[] options,
                PopupState state,
                int pageSize,
                StringInListAttribute attribute
            )
            {
                _serializedObject = property.serializedObject;
                _propertyPath = property.propertyPath;
                _options = options ?? Array.Empty<string>();
                _state = state ?? new PopupState();
                _isStringProperty = property.propertyType == SerializedPropertyType.String;
                _isIntegerProperty = property.propertyType == SerializedPropertyType.Integer;
                _pageSize = Mathf.Max(1, pageSize);
                _attribute = attribute;
            }

            public override Vector2 GetWindowSize()
            {
                int pageSize = ResolvePageSize();
                float lineHeight = EditorGUIUtility.singleLineHeight + 4f;
                float height = 72f + pageSize * lineHeight;
                return new Vector2(PopupWidth, Mathf.Clamp(height, PopupMinHeight, PopupMaxHeight));
            }

            public override void OnGUI(Rect rect)
            {
                if (_serializedObject == null || string.IsNullOrEmpty(_propertyPath))
                {
                    EditorGUILayout.HelpBox(
                        "Unable to resolve property for StringInList.",
                        MessageType.Warning
                    );
                    return;
                }

                _serializedObject.UpdateIfRequiredOrScript();
                SerializedProperty property = _serializedObject.FindProperty(_propertyPath);
                if (property == null)
                {
                    EditorGUILayout.HelpBox(
                        "Unable to resolve property for StringInList.",
                        MessageType.Warning
                    );
                    return;
                }

                DrawSearchControls();

                using (
                    PooledResource<List<int>> filteredLease = Buffers<int>.List.Get(
                        out List<int> filtered
                    )
                )
                {
                    filtered.Clear();

                    bool hasSearch = !string.IsNullOrWhiteSpace(_state.search);
                    string searchTerm = _state.search ?? string.Empty;
                    if (hasSearch)
                    {
                        for (int i = 0; i < _options.Length; i++)
                        {
                            string option = _options[i] ?? string.Empty;
                            if (option.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                filtered.Add(i);
                            }
                        }
                    }

                    int filteredCount = hasSearch ? filtered.Count : _options.Length;
                    if (filteredCount == 0)
                    {
                        DrawEmptyResultsMessage();
                        _state.page = 0;
                        return;
                    }

                    int pageSize = ResolvePageSize();
                    int pageCount = CalculatePageCount(pageSize, filteredCount);
                    _state.page = Mathf.Clamp(_state.page, 0, pageCount - 1);
                    EnsureWindowFitsPageSize(pageSize, pageCount);

                    if (pageCount > 1)
                    {
                        DrawPaginationControls(pageCount);
                    }
                    else
                    {
                        EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                    }

                    int startIndex = _state.page * pageSize;
                    int endIndex = Math.Min(filteredCount, startIndex + pageSize);
                    int currentSelectionIndex = ResolveCurrentSelectionIndex(property, _options);

                    using (new EditorGUILayout.VerticalScope())
                    {
                        for (int i = startIndex; i < endIndex; i++)
                        {
                            int optionIndex = hasSearch ? filtered[i] : i;
                            GUIContent optionContent = GetOptionContent(optionIndex);
                            bool isSelected = optionIndex == currentSelectionIndex;
                            GUIStyle style = isSelected
                                ? Styles.SelectedOptionButton
                                : Styles.OptionButton;
                            if (
                                GUILayout.Button(
                                    optionContent,
                                    style,
                                    GUILayout.ExpandWidth(true),
                                    GUILayout.Height(GetOptionControlHeight())
                                )
                            )
                            {
                                ApplySelection(optionIndex);
                            }
                        }

                        GUILayout.Space(
                            EditorGUIUtility.standardVerticalSpacing + OptionBottomPadding
                        );
                    }
                }
            }

            private void DrawSearchControls()
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Search", GUILayout.Width(55f));
                    EditorGUI.BeginChangeCheck();
                    string newSearch = EditorGUILayout.TextField(
                        _state.search ?? string.Empty,
                        GUILayout.ExpandWidth(true)
                    );
                    if (EditorGUI.EndChangeCheck())
                    {
                        _state.search = newSearch ?? string.Empty;
                        _state.page = 0;
                    }

                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_state.search)))
                    {
                        if (GUILayout.Button("Clear", GUILayout.Width(60f)))
                        {
                            _state.search = string.Empty;
                            _state.page = 0;
                        }
                    }
                }
            }

            private void DrawEmptyResultsMessage()
            {
                EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                EditorGUILayout.HelpBox("No results match the current search.", MessageType.Info);
                EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                ForceEmptyStateHeight();
            }

            private void ForceEmptyStateHeight()
            {
                if (editorWindow == null)
                {
                    return;
                }

                Vector2 size = editorWindow.position.size;
                float targetHeight = Mathf.Min(size.y, EmptyStateHeight);
                if (targetHeight < size.y - 0.5f)
                {
                    Rect windowPosition = editorWindow.position;
                    windowPosition.height = targetHeight;
                    editorWindow.position = windowPosition;
                }
            }

            private void DrawPaginationControls(int pageCount)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(_state.page <= 0))
                    {
                        if (
                            GUILayout.Button(
                                PreviousPageContent,
                                Styles.PaginationButtonLeft,
                                GUILayout.Width(ButtonWidth + 8f)
                            )
                        )
                        {
                            _state.page = Mathf.Max(0, _state.page - 1);
                        }
                    }

                    GUILayout.Label(
                        $"Page {_state.page + 1}/{Mathf.Max(1, pageCount)}",
                        Styles.PaginationLabel,
                        GUILayout.Width(PageLabelWidth),
                        GUILayout.Height(Styles.PaginationButtonLeft.fixedHeight)
                    );

                    using (new EditorGUI.DisabledScope(_state.page >= pageCount - 1))
                    {
                        if (
                            GUILayout.Button(
                                NextPageContent,
                                Styles.PaginationButtonRight,
                                GUILayout.Width(ButtonWidth + 8f)
                            )
                        )
                        {
                            _state.page = Mathf.Min(pageCount - 1, _state.page + 1);
                        }
                    }
                    GUILayout.FlexibleSpace();
                }
            }

            private static float s_cachedOptionControlHeight = -1f;
            private static float s_cachedOptionRowHeight = -1f;

            private static float GetOptionControlHeight()
            {
                if (s_cachedOptionControlHeight > 0f)
                {
                    return s_cachedOptionControlHeight;
                }

                float width = PopupWidth - 32f;
                float measured = Styles.OptionButton.CalcHeight(GUIContent.none, width);
                if (measured <= 0f || float.IsNaN(measured))
                {
                    measured = EditorGUIUtility.singleLineHeight + OptionRowExtraHeight;
                }

                s_cachedOptionControlHeight = measured;
                return measured;
            }

            private static float GetOptionRowHeight()
            {
                if (s_cachedOptionRowHeight > 0f)
                {
                    return s_cachedOptionRowHeight;
                }

                RectOffset margin = Styles.OptionButton.margin;
                float marginHeight = margin != null ? margin.vertical : 0f;
                s_cachedOptionRowHeight = GetOptionControlHeight() + marginHeight;
                return s_cachedOptionRowHeight;
            }

            private void EnsureWindowFitsPageSize(int pageSize, int pageCount)
            {
                if (editorWindow == null)
                {
                    return;
                }

                float searchHeight =
                    EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                float paginationHeight =
                    pageCount > 1
                        ? EditorGUIUtility.singleLineHeight
                            + EditorGUIUtility.standardVerticalSpacing
                        : EditorGUIUtility.standardVerticalSpacing;
                float optionListHeight = pageSize * GetOptionRowHeight();
                float footerHeight =
                    (EditorGUIUtility.standardVerticalSpacing * 2f)
                    + OptionBottomPadding
                    + DropdownBottomPadding;
                float targetHeight = Mathf.Clamp(
                    searchHeight + paginationHeight + optionListHeight + footerHeight,
                    PopupMinHeight,
                    PopupMaxHeight
                );
                Vector2 size = editorWindow.position.size;
                if (size.y < targetHeight - 0.5f)
                {
                    Rect windowPosition = editorWindow.position;
                    windowPosition.height = targetHeight;
                    editorWindow.position = windowPosition;
                }
            }

            private int ResolvePageSize()
            {
                int resolved = Mathf.Max(1, UnityHelpersSettings.GetStringInListPageLimit());
                if (resolved != _pageSize)
                {
                    _pageSize = resolved;
                    _state.page = 0;
                }
                return _pageSize;
            }

            private void ApplySelection(int optionIndex)
            {
                if (
                    optionIndex < 0
                    || optionIndex >= _options.Length
                    || _serializedObject == null
                    || string.IsNullOrEmpty(_propertyPath)
                )
                {
                    return;
                }

                SerializedObject serializedObject = _serializedObject;
                Undo.RecordObjects(serializedObject.targetObjects, "Change String In List");
                serializedObject.Update();

                SerializedProperty property = serializedObject.FindProperty(_propertyPath);
                if (property == null)
                {
                    serializedObject.ApplyModifiedProperties();
                    return;
                }

                if (_isStringProperty)
                {
                    property.stringValue = _options[optionIndex] ?? string.Empty;
                }
                else if (_isIntegerProperty)
                {
                    property.intValue = optionIndex;
                }

                serializedObject.ApplyModifiedProperties();
                editorWindow?.Close();
                GUIUtility.ExitGUI();
            }

            private GUIContent GetOptionContent(int optionIndex)
            {
                string value =
                    optionIndex >= 0 && optionIndex < _options.Length
                        ? _options[optionIndex] ?? string.Empty
                        : string.Empty;
                string label = GetOptionLabel(_attribute, value, out string tooltip);
                return string.IsNullOrEmpty(tooltip)
                    ? new GUIContent(label)
                    : new GUIContent(label, tooltip);
            }

            private static class Styles
            {
                public static readonly GUIStyle OptionButton;
                public static readonly GUIStyle SelectedOptionButton;
                public static readonly GUIStyle PaginationButtonLeft;
                public static readonly GUIStyle PaginationButtonRight;
                public static readonly GUIStyle PaginationLabel;

                static Styles()
                {
                    OptionButton = new GUIStyle("Button")
                    {
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(6, 6, 1, 1),
                    };
                    SelectedOptionButton = new GUIStyle(OptionButton)
                    {
                        fontStyle = FontStyle.Bold,
                    };
                    float paginationHeight = EditorGUIUtility.singleLineHeight + 4f;
                    PaginationButtonLeft = new GUIStyle(EditorStyles.miniButtonLeft)
                    {
                        fixedHeight = paginationHeight,
                        padding = new RectOffset(6, 6, 0, 0),
                    };
                    PaginationButtonRight = new GUIStyle(EditorStyles.miniButtonRight)
                    {
                        fixedHeight = paginationHeight,
                        padding = new RectOffset(6, 6, 0, 0),
                    };
                    PaginationLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        padding = new RectOffset(0, 0, 0, 0),
                    };
                }
            }
        }

        private sealed class StringInListPopupSelectorElement : BaseField<string>
        {
            private readonly string[] _options;
            private readonly StringInListAttribute _attribute;
            private SerializedObject _serializedObject;
            private string _propertyPath = string.Empty;
            private GUIContent _labelContent = GUIContent.none;
            private int _pageSize;

            private static VisualElement CreateInputElement(out IMGUIContainer container)
            {
                container = new IMGUIContainer();
                return container;
            }

            public StringInListPopupSelectorElement(
                string[] options,
                StringInListAttribute attribute
            )
                : base(string.Empty, CreateInputElement(out IMGUIContainer container))
            {
                AddToClassList("unity-base-field");
                AddToClassList("unity-base-field__aligned");
                labelElement.AddToClassList("unity-base-field__label");
                labelElement.AddToClassList("unity-label");

                _attribute = attribute;
                _options = options ?? Array.Empty<string>();
                _pageSize = Mathf.Max(1, UnityHelpersSettings.GetStringInListPageLimit());

                container.style.flexGrow = 1f;
                container.style.marginLeft = 0f;
                container.style.paddingLeft = 0f;
                container.onGUIHandler = OnGUIHandler;
            }

            public void BindProperty(SerializedProperty property, string label)
            {
                _serializedObject = property?.serializedObject;
                _propertyPath = property?.propertyPath ?? string.Empty;
                string labelText = label ?? property?.displayName ?? property?.name ?? string.Empty;
                this.label = labelText;
                _labelContent = new GUIContent(labelText);
            }

            public void UnbindProperty()
            {
                _serializedObject = null;
                _propertyPath = string.Empty;
            }

            private void OnGUIHandler()
            {
                if (_serializedObject == null || string.IsNullOrEmpty(_propertyPath))
                {
                    return;
                }

                _serializedObject.UpdateIfRequiredOrScript();
                SerializedProperty property = _serializedObject.FindProperty(_propertyPath);
                if (property == null)
                {
                    return;
                }

                string displayValue = ResolveDisplayValue(property, _options, _attribute, out _);
                SetValueWithoutNotify(displayValue);

                Rect controlRect = EditorGUILayout.GetControlRect();
                int resolvedPageSize = Mathf.Max(
                    1,
                    UnityHelpersSettings.GetStringInListPageLimit()
                );
                if (resolvedPageSize != _pageSize)
                {
                    _pageSize = resolvedPageSize;
                }
                DrawPopupDropdown(
                    controlRect,
                    property,
                    _labelContent,
                    _options,
                    _pageSize,
                    _attribute
                );
                _serializedObject.ApplyModifiedProperties();
            }
        }

        private sealed class StringInListSelector : BaseField<string>
        {
            private readonly string[] _options;
            private readonly StringInListAttribute _attribute;
            private readonly VisualElement _searchRow;
            private readonly TextField _searchField;
            private readonly Button _clearButton;
            private readonly VisualElement _paginationContainer;
            private readonly Button _previousButton;
            private readonly Label _pageLabel;
            private readonly Button _nextButton;
            private readonly DropdownField _dropdown;
            private readonly Label _noResultsLabel;
            private readonly Label _suggestionHintLabel;
            private List<int> _filteredIndices;
            private PooledResource<List<int>> _filteredIndicesLease;
            private List<int> _pageOptionIndices;
            private PooledResource<List<int>> _pageOptionIndicesLease;
            private List<string> _pageChoices;
            private PooledResource<List<string>> _pageChoicesLease;
            private List<string> _pageChoiceTooltips;
            private PooledResource<List<string>> _pageChoiceTooltipsLease;

            private SerializedObject _boundObject;
            private string _propertyPath = string.Empty;
            private bool _isStringProperty;
            private bool _isIntegerProperty;
            private string _searchText = string.Empty;
            private string _suggestion = string.Empty;
            private int _pageIndex;
            private int _lastResolvedPageSize;
            private bool _searchVisible;
            private int _suggestionOptionIndex;
            private int _currentFilteredCount;

            private bool _buffersInitialized;

            private static VisualElement CreateInputElement(out VisualElement element)
            {
                element = new VisualElement();
                return element;
            }

            public StringInListSelector(string[] options, StringInListAttribute attribute)
                : base(string.Empty, CreateInputElement(out VisualElement baseInput))
            {
                EnsureBuffers();
                RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
                RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

                _options = options ?? Array.Empty<string>();
                _attribute = attribute;
                _lastResolvedPageSize = Mathf.Max(
                    1,
                    UnityHelpersSettings.GetStringInListPageLimit()
                );
                _suggestionOptionIndex = -1;

                AddToClassList("unity-base-field");
                AddToClassList("unity-base-field__aligned");
                labelElement.AddToClassList("unity-base-field__label");
                labelElement.AddToClassList("unity-label");

                baseInput.AddToClassList("unity-base-field__input");
                baseInput.style.flexGrow = 1f;
                baseInput.style.marginLeft = 0f;
                baseInput.style.paddingLeft = 0f;
                baseInput.style.flexDirection = FlexDirection.Column;

                _searchRow = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center,
                        marginBottom = 4f,
                        marginLeft = 0f,
                        paddingLeft = 0f,
                    },
                };

                VisualElement searchWrapper = new()
                {
                    style = { flexGrow = 1f, position = Position.Relative },
                };

                _searchField = new TextField
                {
                    name = "StringInListSearch",
                    style = { flexGrow = 1f },
                };
                _searchField.RegisterValueChangedCallback(OnSearchChanged);
                _searchField.RegisterCallback<KeyDownEvent>(OnSearchKeyDown);
                searchWrapper.Add(_searchField);

                _clearButton = new Button(OnClearClicked)
                {
                    text = "Clear",
                    style = { marginLeft = 4f },
                };
                _clearButton.SetEnabled(false);

                _paginationContainer = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center,
                        marginLeft = 4f,
                        display = DisplayStyle.None,
                    },
                };

                _previousButton = new Button(OnPreviousPage)
                {
                    text = "<",
                    style =
                    {
                        marginRight = 0f,
                        minWidth = ButtonWidth,
                        height = PaginationButtonHeight,
                        paddingLeft = 6f,
                        paddingRight = 6f,
                    },
                };
                _previousButton.AddToClassList("unity-toolbar-button");

                _pageLabel = new Label
                {
                    style =
                    {
                        minWidth = 80f,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        marginRight = 0f,
                        paddingLeft = 6f,
                        paddingRight = 6f,
                        minHeight = PaginationButtonHeight,
                        alignSelf = Align.Center,
                    },
                };
                _nextButton = new Button(OnNextPage)
                {
                    text = ">",
                    style =
                    {
                        minWidth = ButtonWidth,
                        height = PaginationButtonHeight,
                        paddingLeft = 6f,
                        paddingRight = 6f,
                    },
                };
                _nextButton.AddToClassList("unity-toolbar-button");

                _paginationContainer.Add(_previousButton);
                _paginationContainer.Add(_pageLabel);
                _paginationContainer.Add(_nextButton);

                _searchRow.Add(searchWrapper);
                _searchRow.Add(_clearButton);
                _searchRow.Add(_paginationContainer);
                baseInput.Add(_searchRow);

                _suggestionHintLabel = new Label
                {
                    style =
                    {
                        display = DisplayStyle.None,
                        marginLeft = 4f,
                        marginBottom = 2f,
                        color = new Color(0.7f, 0.85f, 1f, 0.75f),
                        unityFontStyleAndWeight = FontStyle.Italic,
                        fontSize = 11f,
                    },
                    pickingMode = PickingMode.Ignore,
                };
                baseInput.Add(_suggestionHintLabel);

                _dropdown = new DropdownField
                {
                    choices = _pageChoices,
                    style =
                    {
                        flexGrow = 1f,
                        marginLeft = 0f,
                        paddingLeft = 0f,
                        marginBottom = DropdownBottomPadding,
                    },
                    label = string.Empty,
                };
                _dropdown.labelElement.style.display = DisplayStyle.None;
                _dropdown.RegisterValueChangedCallback(OnDropdownValueChanged);
                baseInput.Add(_dropdown);

                _noResultsLabel = new Label("No results match the current search.")
                {
                    style =
                    {
                        display = DisplayStyle.None,
                        marginLeft = 0f,
                        marginTop = 4f,
                        paddingTop = NoResultsVerticalPadding,
                        paddingBottom = NoResultsVerticalPadding,
                        paddingLeft = NoResultsHorizontalPadding,
                        paddingRight = NoResultsHorizontalPadding,
                        unityTextAlign = TextAnchor.MiddleCenter,
                    },
                };
                _noResultsLabel.AddToClassList("unity-help-box");
                baseInput.Add(_noResultsLabel);

                ApplySearchVisibility(ShouldShowSearch(_lastResolvedPageSize));

                RegisterCallback<AttachToPanelEvent>(_ => Undo.undoRedoPerformed += OnUndoRedo);
                RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= OnUndoRedo);
            }

            public void BindProperty(SerializedProperty property, string labelText)
            {
                _boundObject = property.serializedObject;
                _propertyPath = property.propertyPath;
                _isStringProperty = property.propertyType == SerializedPropertyType.String;
                _isIntegerProperty = property.propertyType == SerializedPropertyType.Integer;
                UpdateLabel(labelText, property.tooltip);
                _pageIndex = 0;
                _searchText = string.Empty;
                _suggestion = string.Empty;

                _searchField.SetValueWithoutNotify(string.Empty);
                UpdateClearButton(_searchVisible);
                UpdateSuggestionDisplay(string.Empty, -1, -1);

                UpdateFromProperty();
            }

            public void UnbindProperty()
            {
                _boundObject = null;
                _propertyPath = string.Empty;
                UpdateLabel(string.Empty, string.Empty);
            }

            private void UpdateLabel(string labelText, string labelTooltip)
            {
                bool hasLabel = !string.IsNullOrWhiteSpace(labelText);
                label = hasLabel ? labelText : string.Empty;
                labelElement.style.display = hasLabel ? DisplayStyle.Flex : DisplayStyle.None;
                labelElement.tooltip = labelTooltip;
                _dropdown.tooltip = labelTooltip;
            }

            private void OnUndoRedo()
            {
                UpdateFromProperty();
            }

            private void OnSearchChanged(ChangeEvent<string> evt)
            {
                if (!_searchVisible)
                {
                    return;
                }

                _searchText = evt.newValue ?? string.Empty;
                _pageIndex = 0;
                UpdateClearButton(_searchVisible);
                UpdateFromProperty();
            }

            private void OnSearchKeyDown(KeyDownEvent evt)
            {
                if (!_searchVisible)
                {
                    return;
                }

                if (
                    (
                        evt.keyCode == KeyCode.Tab
                        || evt.keyCode == KeyCode.Return
                        || evt.keyCode == KeyCode.KeypadEnter
                    ) && !string.IsNullOrEmpty(_suggestion)
                )
                {
                    evt.PreventDefault();
                    evt.StopPropagation();
                    evt.StopImmediatePropagation();
                    bool commitSelection = evt.keyCode == KeyCode.Tab && !evt.shiftKey;
                    AcceptSuggestion(commitSelection);
                }
            }

            private void OnClearClicked()
            {
                if (!_searchVisible || string.IsNullOrEmpty(_searchText))
                {
                    return;
                }

                _searchText = string.Empty;
                _searchField.SetValueWithoutNotify(string.Empty);

                _pageIndex = 0;
                UpdateClearButton(_searchVisible);
                UpdateSuggestionDisplay(string.Empty, -1, -1);
                UpdateFromProperty();
            }

            private void OnPreviousPage()
            {
                if (_pageIndex <= 0)
                {
                    return;
                }

                _pageIndex--;
                UpdateFromProperty();
            }

            private void OnNextPage()
            {
                int pageSize = ResolvePageSize();
                int pageCount = CalculatePageCount(pageSize, _currentFilteredCount);
                if (_pageIndex >= pageCount - 1)
                {
                    return;
                }

                _pageIndex++;
                UpdateFromProperty();
            }

            private void OnDropdownValueChanged(ChangeEvent<string> evt)
            {
                string newValue = evt.newValue;
                if (string.IsNullOrEmpty(newValue))
                {
                    return;
                }

                int optionIndex = ResolveOptionIndex(newValue);
                if (optionIndex < 0)
                {
                    return;
                }

                ApplySelection(optionIndex);
            }

            private void UpdateFromProperty()
            {
                EnsureBuffers();

                if (_boundObject == null || string.IsNullOrEmpty(_propertyPath))
                {
                    return;
                }

                _boundObject.Update();

                SerializedProperty property = _boundObject.FindProperty(_propertyPath);
                if (property == null)
                {
                    return;
                }

                if (property.propertyType == SerializedPropertyType.String)
                {
                    _isStringProperty = true;
                    _isIntegerProperty = false;
                }
                else if (property.propertyType == SerializedPropertyType.Integer)
                {
                    _isStringProperty = false;
                    _isIntegerProperty = true;
                }

                int pageSize = ResolvePageSize();
                bool searchActive = ShouldShowSearch(pageSize);
                ApplySearchVisibility(searchActive);

                int selectedOptionIndex = GetCurrentSelectionIndex(property);

                _filteredIndices.Clear();
                string effectiveSearch = searchActive
                    ? (_searchText ?? string.Empty)
                    : string.Empty;
                bool hasSearch = searchActive && !string.IsNullOrWhiteSpace(effectiveSearch);
                if (hasSearch)
                {
                    for (int i = 0; i < _options.Length; i++)
                    {
                        string option = _options[i] ?? string.Empty;
                        bool matchesValue = option.StartsWith(
                            effectiveSearch,
                            StringComparison.OrdinalIgnoreCase
                        );
                        bool matchesLabel = false;
                        if (!matchesValue)
                        {
                            string optionLabel = GetOptionLabel(_attribute, option, out _);
                            matchesLabel =
                                !string.IsNullOrEmpty(optionLabel)
                                && optionLabel.StartsWith(
                                    effectiveSearch,
                                    StringComparison.OrdinalIgnoreCase
                                );
                        }

                        if (matchesValue || matchesLabel)
                        {
                            _filteredIndices.Add(i);
                        }
                    }
                }

                int filteredCount = hasSearch ? _filteredIndices.Count : _options.Length;
                if (filteredCount == 0)
                {
                    ToggleDropdownVisibility(false);
                    _pageChoices.Clear();
                    _pageChoiceTooltips.Clear();
                    _dropdown.choices = _pageChoices;
                    _dropdown.SetValueWithoutNotify(string.Empty);
                    SetValueWithoutNotify(string.Empty);
                    _dropdown.SetEnabled(false);
                    _dropdown.tooltip = string.Empty;
                    _noResultsLabel.style.display = DisplayStyle.Flex;
                    UpdatePagination(searchActive, 0, pageSize, 0);
                    UpdateSuggestionDisplay(string.Empty, -1, -1);
                    _currentFilteredCount = 0;
                    return;
                }

                _noResultsLabel.style.display = DisplayStyle.None;
                ToggleDropdownVisibility(true);

                int pageCount = CalculatePageCount(pageSize, filteredCount);
                if (selectedOptionIndex >= 0)
                {
                    int filteredIndex = hasSearch
                        ? _filteredIndices.IndexOf(selectedOptionIndex)
                        : selectedOptionIndex;
                    if (filteredIndex >= 0)
                    {
                        _pageIndex = filteredIndex / pageSize;
                    }
                    else if (_pageIndex >= pageCount)
                    {
                        _pageIndex = 0;
                    }
                }
                else if (_pageIndex >= pageCount)
                {
                    _pageIndex = 0;
                }

                UpdatePagination(searchActive, pageCount, pageSize, filteredCount);

                bool paginate = searchActive && filteredCount > pageSize;

                _pageOptionIndices.Clear();
                _pageChoices.Clear();
                _pageChoiceTooltips.Clear();

                int startIndex = paginate ? _pageIndex * pageSize : 0;
                int endIndex = paginate
                    ? Math.Min(filteredCount, startIndex + pageSize)
                    : filteredCount;

                for (int i = startIndex; i < endIndex; i++)
                {
                    int optionIndex = hasSearch ? _filteredIndices[i] : i;
                    string rawValue = _options[optionIndex] ?? string.Empty;
                    string optionLabel = GetOptionLabel(_attribute, rawValue, out string tooltip);
                    _pageOptionIndices.Add(optionIndex);
                    _pageChoices.Add(optionLabel);
                    _pageChoiceTooltips.Add(tooltip);
                }

                _dropdown.choices = _pageChoices;

                string dropdownValue = string.Empty;
                string dropdownTooltip = string.Empty;
                if (selectedOptionIndex >= 0)
                {
                    string selectedValue = _options[selectedOptionIndex] ?? string.Empty;
                    dropdownValue = GetOptionLabel(_attribute, selectedValue, out dropdownTooltip);
                }

                if (string.IsNullOrEmpty(dropdownValue) && _pageChoices.Count > 0)
                {
                    dropdownValue = _pageChoices[0];
                    dropdownTooltip =
                        _pageChoiceTooltips.Count > 0 ? _pageChoiceTooltips[0] : string.Empty;
                }

                _dropdown.SetValueWithoutNotify(dropdownValue);
                SetValueWithoutNotify(dropdownValue);
                _dropdown.SetEnabled(_pageChoices.Count > 0);
                _dropdown.tooltip = dropdownTooltip;

                _currentFilteredCount = filteredCount;

                UpdateSuggestion(hasSearch);
            }

            private void EnsureBuffers()
            {
                if (_buffersInitialized)
                {
                    return;
                }

                _filteredIndicesLease = Buffers<int>.List.Get(out _filteredIndices);
                _pageOptionIndicesLease = Buffers<int>.List.Get(out _pageOptionIndices);
                _pageChoicesLease = Buffers<string>.List.Get(out _pageChoices);
                _pageChoiceTooltipsLease = Buffers<string>.List.Get(out _pageChoiceTooltips);
                _buffersInitialized = true;
            }

            private void OnAttachedToPanel(AttachToPanelEvent _)
            {
                if (!_buffersInitialized)
                {
                    EnsureBuffers();
                }
            }

            private void OnDetachedFromPanel(DetachFromPanelEvent _)
            {
                ReleaseBuffers();
            }

            private void ReleaseBuffers()
            {
                if (!_buffersInitialized)
                {
                    return;
                }

                _filteredIndicesLease.Dispose();
                _pageOptionIndicesLease.Dispose();
                _pageChoicesLease.Dispose();
                _pageChoiceTooltipsLease.Dispose();

                _filteredIndices = null;
                _pageOptionIndices = null;
                _pageChoices = null;
                _pageChoiceTooltips = null;
                _buffersInitialized = false;
            }

            private void UpdatePagination(
                bool searchActive,
                int pageCount,
                int pageSize,
                int filteredCount
            )
            {
                if (
                    _paginationContainer == null
                    || _previousButton == null
                    || _nextButton == null
                    || _pageLabel == null
                )
                {
                    return;
                }

                bool showPagination = searchActive && filteredCount > pageSize;
                _paginationContainer.style.display = showPagination
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

                if (!showPagination)
                {
                    _pageLabel.text = string.Empty;
                    _previousButton.SetEnabled(false);
                    _nextButton.SetEnabled(false);
                    return;
                }

                int clampedPageCount = Math.Max(1, pageCount);
                _pageIndex = Mathf.Clamp(_pageIndex, 0, clampedPageCount - 1);
                _pageLabel.text = $"Page {_pageIndex + 1}/{clampedPageCount}";
                _previousButton.SetEnabled(_pageIndex > 0);
                _nextButton.SetEnabled(_pageIndex < clampedPageCount - 1);
            }

            private void UpdateClearButton(bool searchActive)
            {
                if (_clearButton == null)
                {
                    return;
                }

                _clearButton.SetEnabled(searchActive && !string.IsNullOrEmpty(_searchText));
            }

            private void UpdateSuggestion(bool hasSearch)
            {
                if (!hasSearch || _filteredIndices.Count == 0)
                {
                    UpdateSuggestionDisplay(string.Empty, -1, -1);
                    return;
                }

                bool searchVisible = hasSearch && !string.IsNullOrEmpty(_searchText);
                int optionIndex = _filteredIndices[0];
                string optionValue = _options[optionIndex] ?? string.Empty;
                string optionLabel = GetOptionLabel(_attribute, optionValue, out _);
                bool prefixMatch =
                    searchVisible
                    && optionLabel.StartsWith(_searchText, StringComparison.OrdinalIgnoreCase);

                UpdateSuggestionDisplay(optionLabel, optionIndex, prefixMatch ? 0 : -1);
            }

            private void UpdateSuggestionDisplay(
                string suggestionValue,
                int optionIndex,
                int matchPosition
            )
            {
                _suggestion = suggestionValue;
                _suggestionOptionIndex = optionIndex;
                bool suggestionsVisible =
                    _searchVisible
                    && !string.IsNullOrEmpty(suggestionValue)
                    && optionIndex >= 0
                    && matchPosition == 0;

                if (_suggestionHintLabel != null)
                {
                    _suggestionHintLabel.text = suggestionsVisible
                        ? $"↹ Tab selects: {suggestionValue}"
                        : string.Empty;
                    _suggestionHintLabel.style.display = suggestionsVisible
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                }

                if (!suggestionsVisible)
                {
                    _suggestionOptionIndex = -1;
                }
            }

            private void ToggleDropdownVisibility(bool hasResults)
            {
                if (_dropdown == null)
                {
                    return;
                }

                _dropdown.style.display = hasResults ? DisplayStyle.Flex : DisplayStyle.None;
            }

            private void AcceptSuggestion(bool commitSelection)
            {
                if (
                    !_searchVisible
                    || string.IsNullOrEmpty(_suggestion)
                    || _searchField == null
                    || _suggestionOptionIndex < 0
                )
                {
                    return;
                }

                string previous = _searchText ?? string.Empty;
                int originalLength = previous.Length;
                int optionIndexToApply = commitSelection ? _suggestionOptionIndex : -1;

                _searchText = _suggestion;
                _searchField.SetValueWithoutNotify(_suggestion);
                int selectionStart = Mathf.Clamp(originalLength, 0, _suggestion.Length);
                _searchField.schedule.Execute(() =>
                {
                    _searchField.Focus();
                    _searchField.SelectRange(selectionStart, _suggestion.Length);
                });
                UpdateClearButton(_searchVisible);
                UpdateSuggestionDisplay(string.Empty, -1, -1);
                UpdateFromProperty();

                if (commitSelection && optionIndexToApply >= 0)
                {
                    ApplySelection(optionIndexToApply);
                }
            }

            private int ResolvePageSize()
            {
                int resolved = Mathf.Max(1, UnityHelpersSettings.GetStringInListPageLimit());
                if (resolved != _lastResolvedPageSize)
                {
                    _lastResolvedPageSize = resolved;
                    _pageIndex = 0;
                }

                return resolved;
            }

            private bool ShouldShowSearch(int pageSize)
            {
                return _options.Length > pageSize;
            }

            private void ApplySearchVisibility(bool searchVisible)
            {
                if (_searchRow == null)
                {
                    return;
                }

                _searchVisible = searchVisible;
                _searchRow.style.display = searchVisible ? DisplayStyle.Flex : DisplayStyle.None;

                if (_dropdown != null)
                {
                    _dropdown.style.marginTop = searchVisible ? 2f : 0f;
                }

                if (!searchVisible)
                {
                    if (!string.IsNullOrEmpty(_searchText))
                    {
                        _searchText = string.Empty;
                        _searchField.SetValueWithoutNotify(string.Empty);
                    }

                    UpdateSuggestionDisplay(string.Empty, -1, -1);
                    _suggestionOptionIndex = -1;

                    if (_paginationContainer != null)
                    {
                        _paginationContainer.style.display = DisplayStyle.None;
                    }

                    if (_previousButton != null)
                    {
                        _previousButton.SetEnabled(false);
                    }

                    if (_nextButton != null)
                    {
                        _nextButton.SetEnabled(false);
                    }
                }

                UpdateClearButton(searchVisible);
                if (!searchVisible && _suggestionHintLabel != null)
                {
                    _suggestionHintLabel.text = string.Empty;
                    _suggestionHintLabel.style.display = DisplayStyle.None;
                }
            }

            private int ResolveOptionIndex(string optionValue)
            {
                if (string.IsNullOrEmpty(optionValue))
                {
                    return -1;
                }

                for (int i = 0; i < _pageChoices.Count && i < _pageOptionIndices.Count; i++)
                {
                    if (string.Equals(_pageChoices[i], optionValue, StringComparison.Ordinal))
                    {
                        return _pageOptionIndices[i];
                    }
                }

                for (int i = 0; i < _options.Length; i++)
                {
                    string label = GetOptionLabel(_attribute, _options[i], out _);
                    if (string.Equals(label, optionValue, StringComparison.Ordinal))
                    {
                        return i;
                    }
                }

                return Array.IndexOf(_options, optionValue);
            }

            private int GetCurrentSelectionIndex(SerializedProperty property)
            {
                if (_isStringProperty)
                {
                    string selectionValue = property.stringValue ?? string.Empty;
                    return Array.IndexOf(_options, selectionValue);
                }

                if (_isIntegerProperty)
                {
                    int index = property.intValue;
                    if (index < 0 || index >= _options.Length)
                    {
                        return -1;
                    }

                    return index;
                }

                return -1;
            }

            private void ApplySelection(int optionIndex)
            {
                if (_boundObject == null || string.IsNullOrEmpty(_propertyPath))
                {
                    return;
                }

                SerializedObject serializedObject = _boundObject;
                Undo.RecordObjects(serializedObject.targetObjects, "Change String In List");
                serializedObject.Update();

                SerializedProperty property = serializedObject.FindProperty(_propertyPath);
                if (property == null)
                {
                    return;
                }

                string selectedValue = _options[optionIndex] ?? string.Empty;

                if (_isStringProperty)
                {
                    property.stringValue = selectedValue;
                }
                else if (_isIntegerProperty)
                {
                    property.intValue = optionIndex;
                }

                SetValueWithoutNotify(selectedValue);
                serializedObject.ApplyModifiedProperties();
                UpdateFromProperty();
            }
        }

        private sealed class StringInListArrayElement : VisualElement
        {
            private readonly string[] _options;
            private readonly SerializedObject _serializedObject;
            private readonly string _propertyPath;
            private readonly List<int> _indices = new();
            private readonly ListView _listView;
            private readonly ToolbarButton _removeButton;
            private readonly int _pageSize;
            private readonly StringInListAttribute _attribute;

            public StringInListArrayElement(
                SerializedProperty property,
                string[] options,
                StringInListAttribute attribute
            )
            {
                _options = options ?? Array.Empty<string>();
                _serializedObject = property.serializedObject;
                _propertyPath = property.propertyPath;
                _pageSize = Mathf.Max(1, UnityHelpersSettings.GetStringInListPageLimit());
                _attribute = attribute;

                AddToClassList("unity-base-field");
                style.flexDirection = FlexDirection.Column;

                Label header = new(property.displayName);
                header.AddToClassList("unity-base-field__label");
                header.style.unityFontStyleAndWeight = FontStyle.Bold;
                header.style.marginBottom = 2f;
                Add(header);

                Toolbar toolbar = new() { style = { marginLeft = -2f, marginBottom = 4f } };

                ToolbarButton addButton = new(AddItem) { text = "Add" };
                _removeButton = new ToolbarButton(RemoveSelected) { text = "Remove" };
                _removeButton.SetEnabled(false);

                toolbar.Add(addButton);
                toolbar.Add(_removeButton);
                Add(toolbar);

                _listView = new ListView(_indices, -1f, MakeItem, BindItem)
                {
                    unbindItem = UnbindItem,
                    name = "StringInListArray",
                    showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                    selectionType = SelectionType.Single,
                    reorderable = true,
                    virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                    style = { flexGrow = 1f },
                };
                _listView.itemIndexChanged += OnItemIndexChanged;
                _listView.itemsRemoved += OnItemsRemoved;
                _listView.selectionChanged += OnSelectionChanged;
                _listView.RegisterCallback<KeyDownEvent>(OnListKeyDown);
                Add(_listView);

                RegisterCallback<AttachToPanelEvent>(_ => Undo.undoRedoPerformed += OnUndoRedo);
                RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= OnUndoRedo);

                Refresh();
            }

            private VisualElement MakeItem()
            {
                if (_options.Length > _pageSize)
                {
                    StringInListPopupSelectorElement popup = new(_options, _attribute)
                    {
                        style = { marginBottom = 4f },
                    };
                    return popup;
                }

                StringInListSelector selector = new(_options, _attribute)
                {
                    style = { marginBottom = 4f },
                };
                return selector;
            }

            private void BindItem(VisualElement element, int index)
            {
                SerializedProperty arrayProperty = GetArrayProperty();
                if (arrayProperty == null || index < 0 || index >= arrayProperty.arraySize)
                {
                    return;
                }

                SerializedProperty elementProperty = arrayProperty.GetArrayElementAtIndex(index);
                if (element is StringInListSelector selector)
                {
                    selector.BindProperty(elementProperty, elementProperty.displayName);
                }
                else if (element is StringInListPopupSelectorElement popup)
                {
                    popup.BindProperty(elementProperty, elementProperty.displayName);
                }
            }

            private void UnbindItem(VisualElement element, int index)
            {
                if (element is StringInListSelector selector)
                {
                    selector.UnbindProperty();
                }
                else if (element is StringInListPopupSelectorElement popup)
                {
                    popup.UnbindProperty();
                }
            }

            private void AddItem()
            {
                SerializedProperty arrayProperty = GetArrayProperty();
                if (arrayProperty == null)
                {
                    return;
                }

                Undo.RecordObjects(_serializedObject.targetObjects, "Add String Entry");
                _serializedObject.Update();

                int newIndex = arrayProperty.arraySize;
                arrayProperty.arraySize++;
                SerializedProperty newElement = arrayProperty.GetArrayElementAtIndex(newIndex);
                if (newElement.propertyType == SerializedPropertyType.String)
                {
                    newElement.stringValue = string.Empty;
                }
                else if (newElement.propertyType == SerializedPropertyType.Integer)
                {
                    newElement.intValue = 0;
                }

                _serializedObject.ApplyModifiedProperties();
                Refresh();
                _listView.selectedIndex = _indices.Count - 1;
            }

            private void RemoveSelected()
            {
                int selectedIndex = _listView.selectedIndex;
                if (selectedIndex < 0)
                {
                    return;
                }

                SerializedProperty arrayProperty = GetArrayProperty();
                if (arrayProperty == null)
                {
                    return;
                }

                Undo.RecordObjects(_serializedObject.targetObjects, "Remove String Entry");
                _serializedObject.Update();
                arrayProperty.DeleteArrayElementAtIndex(selectedIndex);
                _serializedObject.ApplyModifiedProperties();
                Refresh();

                if (_indices.Count > 0)
                {
                    _listView.selectedIndex = Mathf.Clamp(selectedIndex, 0, _indices.Count - 1);
                }
            }

            private void OnItemIndexChanged(int oldIndex, int newIndex)
            {
                if (oldIndex == newIndex)
                {
                    return;
                }

                SerializedProperty arrayProperty = GetArrayProperty();
                if (arrayProperty == null)
                {
                    return;
                }

                Undo.RecordObjects(_serializedObject.targetObjects, "Reorder String Entries");
                _serializedObject.Update();
                arrayProperty.MoveArrayElement(oldIndex, newIndex);
                _serializedObject.ApplyModifiedProperties();
                Refresh();
                _listView.selectedIndex = newIndex;
            }

            private void OnItemsRemoved(IEnumerable<int> removedIndices)
            {
                Refresh();
            }

            private void OnSelectionChanged(IEnumerable<object> _)
            {
                _removeButton.SetEnabled(_listView.selectedIndex >= 0 && _indices.Count > 0);
            }

            private void OnListKeyDown(KeyDownEvent evt)
            {
                if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
                {
                    RemoveSelected();
                    evt.StopPropagation();
                }
            }

            private void OnUndoRedo()
            {
                Refresh();
            }

            private SerializedProperty GetArrayProperty()
            {
                return _serializedObject.FindProperty(_propertyPath);
            }

            private void Refresh()
            {
                _serializedObject.Update();
                SerializedProperty arrayProperty = GetArrayProperty();

                _indices.Clear();
                if (arrayProperty != null)
                {
                    int size = arrayProperty.arraySize;
                    for (int i = 0; i < size; i++)
                    {
                        _indices.Add(i);
                    }
                }

                _listView.RefreshItems();
                _removeButton.SetEnabled(_listView.selectedIndex >= 0 && _indices.Count > 0);
            }
        }

        private static bool IsSerializableTypeProvider(StringInListAttribute attribute)
        {
            return attribute?.ProviderType == typeof(SerializableTypeCatalog)
                && string.Equals(
                    attribute.ProviderMethodName,
                    nameof(SerializableTypeCatalog.GetAssemblyQualifiedNames),
                    StringComparison.Ordinal
                );
        }

        private static string[] GetOptionDisplayArray(
            StringInListAttribute attribute,
            string[] options
        )
        {
            if (!IsSerializableTypeProvider(attribute))
            {
                return options;
            }

            string[] names = SerializableTypeCatalog.GetDisplayNames();
            if (names == null || names.Length != options.Length)
            {
                return options;
            }

            return names;
        }

        private static string GetOptionLabel(
            StringInListAttribute attribute,
            string optionValue,
            out string tooltip
        )
        {
            if (
                IsSerializableTypeProvider(attribute)
                && SerializableTypeCatalog.TryGetDisplayInfo(
                    optionValue,
                    out string displayName,
                    out string displayTooltip
                )
            )
            {
                tooltip = displayTooltip;
                return displayName;
            }

            tooltip = string.Empty;
            return optionValue ?? string.Empty;
        }
    }
#endif
}

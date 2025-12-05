namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Styles;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// UI Toolkit drawer for <see cref="WValueDropDownAttribute"/> that provides search, pagination, and autocomplete.
    /// </summary>
    [CustomPropertyDrawer(typeof(WValueDropDownAttribute))]
    public sealed class WValueDropDownDrawer : PropertyDrawer
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
        private const float OptionBottomPadding = 6f;
        private const float OptionRowExtraHeight = 1.5f;
        private const float EmptySearchHorizontalPadding = 32f;
        private const float EmptySearchExtraPadding = 12f;
        private const string EmptyResultsMessage = "No results match the current search.";
        private static readonly GUIContent EmptyResultsContent = new(EmptyResultsMessage);
        private static float s_cachedOptionControlHeight = -1f;
        private static float s_cachedOptionRowHeight = -1f;
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
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (attribute is not WValueDropDownAttribute dropdownAttribute)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            UnityEngine.Object context = property.serializedObject?.targetObject;
            object[] options = dropdownAttribute.GetOptions(context) ?? Array.Empty<object>();
            int pageSize = Mathf.Max(1, UnityHelpersSettings.GetStringInListPageLimit());

            if (options.Length == 0)
            {
                EditorGUI.HelpBox(
                    position,
                    "No options available for WValueDropDown.",
                    MessageType.Info
                );
                return;
            }

            if (!IsSupportedProperty(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            if (options.Length > pageSize)
            {
                DrawPopupDropdown(position, property, label, options, pageSize, dropdownAttribute);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            string[] displayOptions = BuildDisplayLabels(options);
            int currentIndex = ResolveSelectedIndex(property, dropdownAttribute.ValueType, options);
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, displayOptions);
            if (newIndex >= 0 && newIndex < options.Length)
            {
                ApplyOption(property, options[newIndex]);
            }
            EditorGUI.EndProperty();
        }

        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (attribute is not WValueDropDownAttribute dropdownAttribute)
            {
                PropertyField fallback = new(property) { label = property.displayName };
                return fallback;
            }

            UnityEngine.Object context = property.serializedObject?.targetObject;
            object[] options = dropdownAttribute.GetOptions(context) ?? Array.Empty<object>();
            int pageSize = Mathf.Max(1, UnityHelpersSettings.GetStringInListPageLimit());

            if (options.Length == 0)
            {
                return new HelpBox(
                    "No options available for WValueDropDown.",
                    HelpBoxMessageType.Info
                );
            }

            if (!IsSupportedProperty(property))
            {
                PropertyField fallback = new(property) { label = property.displayName };
                return fallback;
            }

            if (options.Length > pageSize)
            {
                WValueDropDownPopupSelectorElement popupElement = new(options, dropdownAttribute);
                popupElement.BindProperty(property, property.displayName);
                return popupElement;
            }

            WValueDropDownSelector selector = new(options, dropdownAttribute);
            selector.BindProperty(property, property.displayName);
            return selector;
        }

        private static bool IsSupportedProperty(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.Integer
                || property.propertyType == SerializedPropertyType.Float
                || property.propertyType == SerializedPropertyType.String
                || property.propertyType == SerializedPropertyType.Enum;
        }

        private static int CalculatePageCount(int pageSize, int filteredCount)
        {
            if (filteredCount <= 0)
            {
                return 1;
            }

            return (filteredCount + pageSize - 1) / pageSize;
        }

        private static int CalculateRowsOnPage(int filteredCount, int pageSize, int currentPage)
        {
            if (filteredCount <= 0 || pageSize <= 0)
            {
                return 1;
            }

            int maxPageIndex = CalculatePageCount(pageSize, filteredCount) - 1;
            int clampedPage = Mathf.Clamp(currentPage, 0, Mathf.Max(0, maxPageIndex));
            int startIndex = clampedPage * pageSize;
            int remaining = filteredCount - startIndex;
            if (remaining <= 0)
            {
                return 1;
            }

            return Mathf.Min(pageSize, remaining);
        }

        private static void DrawPopupDropdown(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            object[] options,
            int pageSize,
            WValueDropDownAttribute attribute
        )
        {
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
                string[] displayLabels = BuildDisplayLabels(options);
                int currentIndex = ResolveSelectedIndex(property, attribute.ValueType, options);

                SerializedObject serializedObject = property.serializedObject;
                string propertyPath = property.propertyPath;

                WDropdownPopupData data = new()
                {
                    DisplayLabels = displayLabels,
                    Tooltips = null,
                    SelectedIndex = currentIndex,
                    PageSize = pageSize,
                    OnSelectionChanged = (selectedIndex) =>
                    {
                        if (selectedIndex < 0 || selectedIndex >= options.Length)
                        {
                            return;
                        }

                        serializedObject.Update();
                        SerializedProperty prop = serializedObject.FindProperty(propertyPath);
                        if (prop == null)
                        {
                            return;
                        }

                        Undo.RecordObjects(
                            serializedObject.targetObjects,
                            "Change ValueDropDown Selection"
                        );
                        ApplyOption(prop, options[selectedIndex]);
                        serializedObject.ApplyModifiedProperties();
                    },
                };

                Rect screenRect = GUIUtility.GUIToScreenRect(fieldRect);
                WDropdownPopupWindow.Show(screenRect, data);
            }

            EditorGUI.showMixedValue = previousMixed;

            EditorGUI.EndProperty();
        }

        private static int ResolveSelectedIndex(
            SerializedProperty property,
            Type valueType,
            object[] options
        )
        {
            for (int index = 0; index < options.Length; index += 1)
            {
                if (OptionMatches(property, valueType, options[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        private static string ResolveDisplayValue(
            SerializedProperty property,
            object[] options,
            WValueDropDownAttribute attribute,
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

            int selectedIndex = ResolveSelectedIndex(property, attribute.ValueType, options);
            if (selectedIndex >= 0 && selectedIndex < options.Length)
            {
                return FormatOption(options[selectedIndex]);
            }

            return string.Empty;
        }

        private static bool OptionMatches(
            SerializedProperty property,
            Type valueType,
            object option
        )
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return MatchesInteger(property, valueType, option);
                case SerializedPropertyType.Float:
                    return MatchesFloat(property, valueType, option);
                case SerializedPropertyType.String:
                    return MatchesString(property, option);
                case SerializedPropertyType.Enum:
                    return MatchesEnum(property, option);
                default:
                    return false;
            }
        }

        private static bool MatchesInteger(
            SerializedProperty property,
            Type valueType,
            object option
        )
        {
            if (option == null)
            {
                return false;
            }

            try
            {
                Type targetType = Nullable.GetUnderlyingType(valueType) ?? valueType;
                object converted = Convert.ChangeType(
                    property.longValue,
                    targetType,
                    CultureInfo.InvariantCulture
                );
                return Equals(converted, option);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool MatchesFloat(SerializedProperty property, Type valueType, object option)
        {
            if (option == null)
            {
                return false;
            }

            try
            {
                Type targetType = Nullable.GetUnderlyingType(valueType) ?? valueType;
                double currentValue = IsDoubleProperty(property)
                    ? property.doubleValue
                    : property.floatValue;
                object converted = Convert.ChangeType(
                    currentValue,
                    targetType,
                    CultureInfo.InvariantCulture
                );
                return Equals(converted, option);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool MatchesString(SerializedProperty property, object option)
        {
            if (option == null)
            {
                return string.IsNullOrEmpty(property.stringValue);
            }

            return string.Equals(property.stringValue, option as string, StringComparison.Ordinal);
        }

        private static bool MatchesEnum(SerializedProperty property, object option)
        {
            if (option == null)
            {
                return false;
            }

            if (option is Enum enumValue)
            {
                string optionName = enumValue.ToString();
                if (property.enumNames == null || property.enumNames.Length == 0)
                {
                    return false;
                }

                int enumIndex = property.enumValueIndex;
                if (enumIndex < 0 || enumIndex >= property.enumNames.Length)
                {
                    return false;
                }

                string currentName = property.enumNames[enumIndex];
                return string.Equals(currentName, optionName, StringComparison.Ordinal);
            }

            if (option is string optionString)
            {
                if (property.enumNames == null || property.enumNames.Length == 0)
                {
                    return false;
                }

                int enumIndex = property.enumValueIndex;
                if (enumIndex < 0 || enumIndex >= property.enumNames.Length)
                {
                    return false;
                }

                string currentName = property.enumNames[enumIndex];
                return string.Equals(currentName, optionString, StringComparison.Ordinal);
            }

            return false;
        }

        private static string[] BuildDisplayLabels(object[] options)
        {
            string[] labels = new string[options.Length];
            for (int index = 0; index < options.Length; index += 1)
            {
                labels[index] = FormatOption(options[index]);
            }

            return labels;
        }

        private static string FormatOption(object option)
        {
            if (option == null)
            {
                return "(null)";
            }

            if (option is IFormattable formattable)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            return option.ToString();
        }

        internal static void ApplyOption(SerializedProperty property, object selectedOption)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    ApplyInteger(property, selectedOption);
                    break;
                case SerializedPropertyType.Float:
                    ApplyFloat(property, selectedOption);
                    break;
                case SerializedPropertyType.String:
                    ApplyString(property, selectedOption);
                    break;
                case SerializedPropertyType.Enum:
                    ApplyEnum(property, selectedOption);
                    break;
            }
        }

        private static void ApplyInteger(SerializedProperty property, object selectedOption)
        {
            if (selectedOption == null)
            {
                return;
            }

            try
            {
                long value = Convert.ToInt64(selectedOption, CultureInfo.InvariantCulture);
                property.longValue = value;
            }
            catch (Exception) { }
        }

        private static void ApplyFloat(SerializedProperty property, object selectedOption)
        {
            if (selectedOption == null)
            {
                return;
            }

            try
            {
                double value = Convert.ToDouble(selectedOption, CultureInfo.InvariantCulture);
                if (IsDoubleProperty(property))
                {
                    property.doubleValue = value;
                }
                else
                {
                    property.floatValue = (float)value;
                }
            }
            catch (Exception) { }
        }

        private static bool IsDoubleProperty(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            return string.Equals(property.type, "double", StringComparison.Ordinal);
        }

        private static void ApplyString(SerializedProperty property, object selectedOption)
        {
            property.stringValue =
                selectedOption == null
                    ? string.Empty
                    : Convert.ToString(selectedOption, CultureInfo.InvariantCulture)
                        ?? string.Empty;
        }

        private static void ApplyEnum(SerializedProperty property, object selectedOption)
        {
            if (selectedOption == null)
            {
                return;
            }

            string optionName;
            if (selectedOption is Enum enumValue)
            {
                optionName = enumValue.ToString();
            }
            else if (selectedOption is string stringValue)
            {
                optionName = stringValue;
            }
            else
            {
                optionName = Convert.ToString(selectedOption, CultureInfo.InvariantCulture);
            }

            if (property.enumNames == null || property.enumNames.Length == 0)
            {
                return;
            }

            for (int index = 0; index < property.enumNames.Length; index += 1)
            {
                if (string.Equals(property.enumNames[index], optionName, StringComparison.Ordinal))
                {
                    property.enumValueIndex = index;
                    return;
                }
            }
        }

        private sealed class WValueDropDownPopupContent : PopupWindowContent
        {
            private readonly SerializedObject _serializedObject;
            private readonly string _propertyPath;
            private readonly object[] _options;
            private readonly PopupState _state;
            private readonly WValueDropDownAttribute _attribute;
            private static readonly GUIContent PreviousPageContent = new("<", "Previous page");
            private static readonly GUIContent NextPageContent = new(">", "Next page");
            private int _pageSize;
            private float _emptyStateMeasuredHeight = -1f;

            public WValueDropDownPopupContent(
                SerializedProperty property,
                object[] options,
                PopupState state,
                int pageSize,
                WValueDropDownAttribute attribute
            )
            {
                _serializedObject = property.serializedObject;
                _propertyPath = property.propertyPath;
                _options = options ?? Array.Empty<object>();
                _state = state ?? new PopupState();
                _pageSize = Mathf.Max(1, pageSize);
                _attribute = attribute;
            }

            public override Vector2 GetWindowSize()
            {
                int pageSize = ResolvePageSize();
                int filteredCount = CalculateFilteredCount();
                bool includePagination = filteredCount > pageSize;
                float height;
                if (filteredCount == 0)
                {
                    float measured = _emptyStateMeasuredHeight;
                    height = CalculateEmptySearchHeight(measuredHelpBoxHeight: measured);
                    return new Vector2(PopupWidth, height);
                }

                int pageCount = CalculatePageCount(pageSize, filteredCount);
                _state.page = Mathf.Clamp(_state.page, 0, pageCount - 1);
                int rowsOnPage = CalculateRowsOnPage(filteredCount, pageSize, _state.page);
                includePagination = pageCount > 1;
                height = CalculatePopupTargetHeight(rowsOnPage, includePagination);
                return new Vector2(PopupWidth, height);
            }

            public override void OnGUI(Rect rect)
            {
                if (_serializedObject == null || string.IsNullOrEmpty(_propertyPath))
                {
                    EditorGUILayout.HelpBox(
                        "Unable to resolve property for WValueDropDown.",
                        MessageType.Warning
                    );
                    return;
                }

                _serializedObject.UpdateIfRequiredOrScript();
                SerializedProperty property = _serializedObject.FindProperty(_propertyPath);
                if (property == null)
                {
                    EditorGUILayout.HelpBox(
                        "Unable to resolve property for WValueDropDown.",
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
                            string optionLabel = FormatOption(_options[i]);
                            if (
                                optionLabel.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase)
                                >= 0
                            )
                            {
                                filtered.Add(i);
                            }
                        }
                    }

                    int filteredCount = hasSearch ? filtered.Count : _options.Length;
                    int pageSize = ResolvePageSize();
                    int pageCount = CalculatePageCount(pageSize, filteredCount);
                    _state.page = Mathf.Clamp(_state.page, 0, pageCount - 1);
                    if (filteredCount == 0)
                    {
                        DrawEmptyResultsMessage();
                        _state.page = 0;
                        return;
                    }

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
                    int rowsOnPage = Mathf.Max(1, endIndex - startIndex);
                    int currentSelectionIndex = ResolveSelectedIndex(
                        property,
                        _attribute.ValueType,
                        _options
                    );

                    using (new EditorGUILayout.VerticalScope())
                    {
                        for (int i = startIndex; i < endIndex; i++)
                        {
                            int optionIndex = hasSearch ? filtered[i] : i;
                            GUIContent optionContent = GetOptionContent(optionIndex);
                            bool isSelected = optionIndex == currentSelectionIndex;
                            GUIStyle style = isSelected
                                ? PopupStyles.SelectedOptionButton
                                : PopupStyles.OptionButton;
                            if (
                                GUILayout.Button(
                                    optionContent,
                                    style,
                                    GUILayout.ExpandWidth(true),
                                    GUILayout.Height(GetOptionControlHeight())
                                )
                            )
                            {
                                ApplySelection(property, optionIndex);
                            }
                        }

                        GUILayout.Space(
                            EditorGUIUtility.standardVerticalSpacing + OptionBottomPadding
                        );
                    }

                    bool includePagination = pageCount > 1;
                    EnsureWindowFitsPageSize(rowsOnPage, includePagination);
                    _emptyStateMeasuredHeight = -1f;
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
                EditorGUILayout.HelpBox(EmptyResultsMessage, MessageType.Info);
                float measuredHelpHeight = TryGetLastRectHeight();
                if (measuredHelpHeight > 0f)
                {
                    _emptyStateMeasuredHeight = measuredHelpHeight;
                }
                EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                float targetHeight = CalculateEmptySearchHeight(_emptyStateMeasuredHeight);
                EnsureWindowHeight(targetHeight);
            }

            private void EnsureWindowHeight(float targetHeight)
            {
                if (editorWindow == null)
                {
                    return;
                }

                Rect windowPosition = editorWindow.position;
                float delta = Mathf.Abs(windowPosition.height - targetHeight);
                if (delta <= 0.5f)
                {
                    return;
                }

                windowPosition.height = targetHeight;
                editorWindow.position = windowPosition;
            }

            private static float TryGetLastRectHeight()
            {
                Event evt = Event.current;
                if (evt == null || evt.type != EventType.Repaint)
                {
                    return -1f;
                }

                Rect lastRect = GUILayoutUtility.GetLastRect();
                return lastRect.height > 0f ? lastRect.height : -1f;
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
                                PopupStyles.PaginationButtonLeft,
                                GUILayout.Width(ButtonWidth + 8f)
                            )
                        )
                        {
                            _state.page = Mathf.Max(0, _state.page - 1);
                        }
                    }

                    GUILayout.Label(
                        $"Page {_state.page + 1}/{Mathf.Max(1, pageCount)}",
                        PopupStyles.PaginationLabel,
                        GUILayout.Width(PageLabelWidth),
                        GUILayout.Height(PopupStyles.PaginationButtonLeft.fixedHeight)
                    );

                    using (new EditorGUI.DisabledScope(_state.page >= pageCount - 1))
                    {
                        if (
                            GUILayout.Button(
                                NextPageContent,
                                PopupStyles.PaginationButtonRight,
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

            private void EnsureWindowFitsPageSize(int rowsOnPage, bool includePagination)
            {
                if (editorWindow == null)
                {
                    return;
                }

                float measuredHeight = CalculateMeasuredContentHeight(includePagination);
                float fallbackHeight = CalculatePopupTargetHeight(rowsOnPage, includePagination);
                float targetHeight = measuredHeight > 0f ? measuredHeight : fallbackHeight;
                EnsureWindowHeight(targetHeight);
            }

            private float CalculateMeasuredContentHeight(bool includePagination)
            {
                Event current = Event.current;
                if (current == null || current.type != EventType.Repaint)
                {
                    return -1f;
                }

                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (lastRect.height <= 0f && lastRect.yMax <= 0f)
                {
                    return -1f;
                }

                float measuredHeight = lastRect.yMax;
                float minimumHeight =
                    CalculatePopupChromeHeight(includePagination) + GetOptionRowHeight();
                float result = Mathf.Max(measuredHeight, minimumHeight);
                return result;
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

            private int CalculateFilteredCount()
            {
                if (string.IsNullOrWhiteSpace(_state.search))
                {
                    return _options.Length;
                }

                string searchTerm = _state.search ?? string.Empty;
                int count = 0;
                for (int i = 0; i < _options.Length; i++)
                {
                    string optionLabel = FormatOption(_options[i]);
                    if (optionLabel.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        count++;
                    }
                }
                return count;
            }

            private void ApplySelection(SerializedProperty property, int optionIndex)
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
                Undo.RecordObjects(serializedObject.targetObjects, "Change Value Dropdown");
                serializedObject.Update();

                SerializedProperty prop = serializedObject.FindProperty(_propertyPath);
                if (prop == null)
                {
                    serializedObject.ApplyModifiedProperties();
                    return;
                }

                ApplyOption(prop, _options[optionIndex]);

                serializedObject.ApplyModifiedProperties();
                editorWindow?.Close();
                GUIUtility.ExitGUI();
            }

            private GUIContent GetOptionContent(int optionIndex)
            {
                string label =
                    optionIndex >= 0 && optionIndex < _options.Length
                        ? FormatOption(_options[optionIndex])
                        : string.Empty;
                return new GUIContent(label);
            }
        }

        private sealed class WValueDropDownPopupSelectorElement : BaseField<string>
        {
            private readonly object[] _options;
            private readonly WValueDropDownAttribute _attribute;
            private SerializedObject _serializedObject;
            private string _propertyPath = string.Empty;
            private GUIContent _labelContent = GUIContent.none;
            private int _pageSize;

            private static VisualElement CreateInputElement(out IMGUIContainer container)
            {
                container = new IMGUIContainer();
                return container;
            }

            public WValueDropDownPopupSelectorElement(
                object[] options,
                WValueDropDownAttribute attribute
            )
                : base(string.Empty, CreateInputElement(out IMGUIContainer container))
            {
                AddToClassList("unity-base-field");
                AddToClassList("unity-base-field__aligned");
                labelElement.AddToClassList("unity-base-field__label");
                labelElement.AddToClassList("unity-label");

                _attribute = attribute;
                _options = options ?? Array.Empty<object>();
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

        private sealed class WValueDropDownSelector : BaseField<string>
        {
            private readonly object[] _options;
            private readonly WValueDropDownAttribute _attribute;
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

            private SerializedObject _boundObject;
            private string _propertyPath = string.Empty;
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

            public WValueDropDownSelector(object[] options, WValueDropDownAttribute attribute)
                : base(string.Empty, CreateInputElement(out VisualElement baseInput))
            {
                EnsureBuffers();
                RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
                RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

                _options = options ?? Array.Empty<object>();
                _attribute = attribute;
                _lastResolvedPageSize = Mathf.Max(
                    1,
                    UnityHelpersSettings.GetStringInListPageLimit()
                );
                _suggestionOptionIndex = -1;

                // Apply dropdown styles
                WDropdownStyleLoader.ApplyStyles(this);

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
                _searchRow.AddToClassList(WDropdownStyleLoader.ClassNames.SearchContainer);

                VisualElement searchWrapper = new()
                {
                    style = { flexGrow = 1f, position = Position.Relative },
                };
                searchWrapper.AddToClassList(WDropdownStyleLoader.ClassNames.SearchWrapper);

                _searchField = new TextField
                {
                    name = "WValueDropDownSearch",
                    style = { flexGrow = 1f },
                };
                _searchField.AddToClassList(WDropdownStyleLoader.ClassNames.Search);
                _searchField.RegisterValueChangedCallback(OnSearchChanged);
                _searchField.RegisterCallback<KeyDownEvent>(OnSearchKeyDown);
                searchWrapper.Add(_searchField);

                _clearButton = new Button(OnClearClicked)
                {
                    text = "Clear",
                    style = { marginLeft = 4f },
                };
                _clearButton.AddToClassList(WDropdownStyleLoader.ClassNames.ClearButton);
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
                _paginationContainer.AddToClassList(WDropdownStyleLoader.ClassNames.Pagination);

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
                _previousButton.AddToClassList(WDropdownStyleLoader.ClassNames.PaginationButton);

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
                _pageLabel.AddToClassList(WDropdownStyleLoader.ClassNames.PaginationLabel);

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
                _nextButton.AddToClassList(WDropdownStyleLoader.ClassNames.PaginationButton);

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
                _suggestionHintLabel.AddToClassList(WDropdownStyleLoader.ClassNames.Suggestion);
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
                _noResultsLabel.AddToClassList(WDropdownStyleLoader.ClassNames.NoResults);
                baseInput.Add(_noResultsLabel);

                ApplySearchVisibility(ShouldShowSearch(_lastResolvedPageSize));

                RegisterCallback<AttachToPanelEvent>(_ => Undo.undoRedoPerformed += OnUndoRedo);
                RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= OnUndoRedo);
            }

            public void BindProperty(SerializedProperty property, string labelText)
            {
                _boundObject = property.serializedObject;
                _propertyPath = property.propertyPath;
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

                int pageSize = ResolvePageSize();
                bool searchActive = ShouldShowSearch(pageSize);
                ApplySearchVisibility(searchActive);

                int selectedOptionIndex = ResolveSelectedIndex(
                    property,
                    _attribute.ValueType,
                    _options
                );

                _filteredIndices.Clear();
                string effectiveSearch = searchActive
                    ? (_searchText ?? string.Empty)
                    : string.Empty;
                bool hasSearch = searchActive && !string.IsNullOrWhiteSpace(effectiveSearch);
                if (hasSearch)
                {
                    for (int i = 0; i < _options.Length; i++)
                    {
                        string optionLabel = FormatOption(_options[i]);
                        bool matchesValue = optionLabel.StartsWith(
                            effectiveSearch,
                            StringComparison.OrdinalIgnoreCase
                        );
                        if (matchesValue)
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

                int startIndex = paginate ? _pageIndex * pageSize : 0;
                int endIndex = paginate
                    ? Math.Min(filteredCount, startIndex + pageSize)
                    : filteredCount;

                for (int i = startIndex; i < endIndex; i++)
                {
                    int optionIndex = hasSearch ? _filteredIndices[i] : i;
                    string optionLabel = FormatOption(_options[optionIndex]);
                    _pageOptionIndices.Add(optionIndex);
                    _pageChoices.Add(optionLabel);
                }

                _dropdown.choices = _pageChoices;

                string dropdownValue = string.Empty;
                if (selectedOptionIndex >= 0)
                {
                    dropdownValue = FormatOption(_options[selectedOptionIndex]);
                }

                if (string.IsNullOrEmpty(dropdownValue) && _pageChoices.Count > 0)
                {
                    dropdownValue = _pageChoices[0];
                }

                _dropdown.SetValueWithoutNotify(dropdownValue);
                SetValueWithoutNotify(dropdownValue);
                _dropdown.SetEnabled(_pageChoices.Count > 0);

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

                _filteredIndices = null;
                _pageOptionIndices = null;
                _pageChoices = null;
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
                string optionLabel = FormatOption(_options[optionIndex]);
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

            private int ResolveOptionIndex(string optionLabel)
            {
                if (string.IsNullOrEmpty(optionLabel))
                {
                    return -1;
                }

                for (int i = 0; i < _pageChoices.Count && i < _pageOptionIndices.Count; i++)
                {
                    if (string.Equals(_pageChoices[i], optionLabel, StringComparison.Ordinal))
                    {
                        return _pageOptionIndices[i];
                    }
                }

                for (int i = 0; i < _options.Length; i++)
                {
                    string label = FormatOption(_options[i]);
                    if (string.Equals(label, optionLabel, StringComparison.Ordinal))
                    {
                        return i;
                    }
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
                Undo.RecordObjects(serializedObject.targetObjects, "Change Value Dropdown");
                serializedObject.Update();

                SerializedProperty property = serializedObject.FindProperty(_propertyPath);
                if (property == null)
                {
                    return;
                }

                ApplyOption(property, _options[optionIndex]);

                SetValueWithoutNotify(FormatOption(_options[optionIndex]));
                serializedObject.ApplyModifiedProperties();
                UpdateFromProperty();
            }
        }

        private static float CalculatePopupTargetHeight(int rowsOnPage, bool includePagination)
        {
            int clampedRows = Mathf.Max(1, rowsOnPage);
            float chromeHeight = CalculatePopupChromeHeight(includePagination);
            float optionListHeight = clampedRows * GetOptionRowHeight();
            float unclampedHeight = chromeHeight + optionListHeight;
            return unclampedHeight;
        }

        private static float CalculatePopupChromeHeight(bool includePagination)
        {
            float searchHeight = EditorGUIUtility.singleLineHeight;
            float paginationHeight = includePagination
                ? PopupStyles.PaginationButtonLeft.fixedHeight
                : EditorGUIUtility.standardVerticalSpacing;
            float footerHeight = EditorGUIUtility.standardVerticalSpacing + OptionBottomPadding;
            return searchHeight + paginationHeight + footerHeight;
        }

        private static float CalculateEmptySearchHeight(float measuredHelpBoxHeight = -1f)
        {
            GUIStyle helpStyle = EditorStyles.helpBox;
            int helpMargin = helpStyle.margin?.horizontal ?? 0;
            float availableWidth = PopupWidth - EmptySearchHorizontalPadding - helpMargin;
            availableWidth = Mathf.Max(32f, availableWidth);
            float helpBoxHeight;
            if (measuredHelpBoxHeight > 0f)
            {
                helpBoxHeight = measuredHelpBoxHeight;
            }
            else
            {
                float calculated = helpStyle.CalcHeight(EmptyResultsContent, availableWidth);
                float marginVertical = helpStyle.margin?.vertical ?? 0;
                helpBoxHeight = calculated + marginVertical;
            }

            float searchRow =
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            float topSpacer = EditorGUIUtility.standardVerticalSpacing;
            float bottomSpacer = EditorGUIUtility.standardVerticalSpacing;
            float footer =
                EditorGUIUtility.standardVerticalSpacing
                + OptionBottomPadding
                + EmptySearchExtraPadding;

            float result = searchRow + topSpacer + helpBoxHeight + bottomSpacer + footer;
            return result;
        }

        private static float GetOptionRowHeight()
        {
            if (s_cachedOptionRowHeight > 0f)
            {
                return s_cachedOptionRowHeight;
            }

            float controlHeight = GetOptionControlHeight();
            RectOffset margin = PopupStyles.OptionButton.margin;
            float adjustedMargin = 0f;
            if (margin != null)
            {
                adjustedMargin = Mathf.Max(
                    0f,
                    margin.vertical - EditorGUIUtility.standardVerticalSpacing
                );
            }
            else
            {
                adjustedMargin = EditorGUIUtility.standardVerticalSpacing;
            }

            s_cachedOptionRowHeight = controlHeight + adjustedMargin;
            return s_cachedOptionRowHeight;
        }

        private static float GetOptionControlHeight()
        {
            if (s_cachedOptionControlHeight > 0f)
            {
                return s_cachedOptionControlHeight;
            }

            float width = PopupWidth - 32f;
            float measured = PopupStyles.OptionButton.CalcHeight(GUIContent.none, width);
            if (measured <= 0f || float.IsNaN(measured))
            {
                measured = EditorGUIUtility.singleLineHeight + OptionRowExtraHeight;
            }

            s_cachedOptionControlHeight = measured;
            return measured;
        }

        internal static class TestHooks
        {
            public static float CalculatePopupTargetHeight(int rowsOnPage, bool includePagination)
            {
                return WValueDropDownDrawer.CalculatePopupTargetHeight(
                    rowsOnPage,
                    includePagination
                );
            }

            public static float CalculatePopupChromeHeight(bool includePagination)
            {
                return WValueDropDownDrawer.CalculatePopupChromeHeight(includePagination);
            }

            public static float GetOptionRowHeight()
            {
                return WValueDropDownDrawer.GetOptionRowHeight();
            }

            public static float GetOptionControlHeight()
            {
                return WValueDropDownDrawer.GetOptionControlHeight();
            }

            public static int OptionButtonMarginVertical =>
                PopupStyles.OptionButton.margin?.vertical ?? 0;

            public static float OptionFooterPadding => OptionBottomPadding;

            public static float PaginationButtonHeight =>
                PopupStyles.PaginationButtonLeft.fixedHeight;

            public static float PopupWidthValue => PopupWidth;

            public static float EmptySearchHorizontalPaddingValue => EmptySearchHorizontalPadding;

            public static string EmptyResultsMessageValue => EmptyResultsMessage;

            public static float EmptySearchExtraPaddingValue => EmptySearchExtraPadding;

            public static float CalculateEmptySearchHeight()
            {
                return WValueDropDownDrawer.CalculateEmptySearchHeight();
            }

            public static float CalculateEmptySearchHeightWithMeasurement(float measuredHelpHeight)
            {
                return WValueDropDownDrawer.CalculateEmptySearchHeight(measuredHelpHeight);
            }

            public static int CalculateRowsOnPage(int filteredCount, int pageSize, int currentPage)
            {
                return WValueDropDownDrawer.CalculateRowsOnPage(
                    filteredCount,
                    pageSize,
                    currentPage
                );
            }
        }

        private static class PopupStyles
        {
            public static readonly GUIStyle OptionButton;
            public static readonly GUIStyle SelectedOptionButton;
            public static readonly GUIStyle PaginationButtonLeft;
            public static readonly GUIStyle PaginationButtonRight;
            public static readonly GUIStyle PaginationLabel;

            static PopupStyles()
            {
                OptionButton = new GUIStyle("Button")
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(6, 6, 1, 1),
                };
                SelectedOptionButton = new GUIStyle(OptionButton) { fontStyle = FontStyle.Bold };
                float paginationHeight = PaginationButtonHeight;
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
#endif
}

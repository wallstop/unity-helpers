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
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// UI Toolkit drawer for <see cref="StringInListAttribute"/> that provides search, pagination, and autocomplete.
    /// </summary>
    [CustomPropertyDrawer(typeof(StringInListAttribute))]
    public sealed class StringInListDrawer : PropertyDrawer
    {
        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            StringInListAttribute stringInList = (StringInListAttribute)attribute;
            string[] options = stringInList.List ?? Array.Empty<string>();

            if (options.Length == 0)
            {
                return new HelpBox(
                    "No options available for StringInList.",
                    HelpBoxMessageType.Info
                );
            }

            if (IsSupportedArray(property))
            {
                return new StringInListArrayElement(property, options);
            }

            if (!IsSupportedSimpleProperty(property))
            {
                PropertyField fallback = new(property) { label = property.displayName };
                return fallback;
            }

            StringInListSelector selector = new(options);
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

        private sealed class StringInListSelector : BaseField<string>
        {
            private readonly string[] _options;
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
            private bool _isStringProperty;
            private bool _isIntegerProperty;
            private string _searchText = string.Empty;
            private string _suggestion = string.Empty;
            private int _pageIndex;
            private int _lastResolvedPageSize;
            private bool _searchVisible;
            private int _suggestionOptionIndex;

            private bool _buffersInitialized;

            private static VisualElement CreateInputElement(out VisualElement element)
            {
                element = new VisualElement();
                return element;
            }

            public StringInListSelector(string[] options)
                : base(string.Empty, CreateInputElement(out VisualElement baseInput))
            {
                EnsureBuffers();
                RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
                RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

                _options = options ?? Array.Empty<string>();
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
                    style = { marginRight = 4f },
                };
                _pageLabel = new Label
                {
                    style =
                    {
                        minWidth = 80f,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        marginRight = 4f,
                    },
                };
                _nextButton = new Button(OnNextPage) { text = ">" };

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
                        color = new StyleColor(new Color(1f, 0.4f, 0.4f)),
                        marginLeft = 2f,
                    },
                };
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
                int pageCount = GetPageCount(pageSize, _filteredIndices.Count);
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
                for (int i = 0; i < _options.Length; i++)
                {
                    string option = _options[i] ?? string.Empty;
                    if (
                        !hasSearch
                        || option.StartsWith(effectiveSearch, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        _filteredIndices.Add(i);
                    }
                }

                if (_filteredIndices.Count == 0)
                {
                    _pageChoices.Clear();
                    _dropdown.choices = _pageChoices;
                    _dropdown.SetValueWithoutNotify(string.Empty);
                    SetValueWithoutNotify(string.Empty);
                    _dropdown.SetEnabled(false);
                    _noResultsLabel.style.display = DisplayStyle.Flex;
                    UpdatePagination(searchActive, 0, pageSize, 0);
                    UpdateSuggestionDisplay(string.Empty, -1, -1);
                    return;
                }

                _noResultsLabel.style.display = DisplayStyle.None;

                int pageCount = GetPageCount(pageSize, _filteredIndices.Count);
                if (selectedOptionIndex >= 0)
                {
                    int filteredIndex = _filteredIndices.IndexOf(selectedOptionIndex);
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

                UpdatePagination(searchActive, pageCount, pageSize, _filteredIndices.Count);

                bool paginate = searchActive && _filteredIndices.Count > pageSize;

                _pageOptionIndices.Clear();
                _pageChoices.Clear();

                int startIndex = paginate ? _pageIndex * pageSize : 0;
                int endIndex = paginate
                    ? Math.Min(_filteredIndices.Count, startIndex + pageSize)
                    : _filteredIndices.Count;

                for (int i = startIndex; i < endIndex; i++)
                {
                    int optionIndex = _filteredIndices[i];
                    _pageOptionIndices.Add(optionIndex);
                    _pageChoices.Add(_options[optionIndex] ?? string.Empty);
                }

                _dropdown.choices = _pageChoices;

                string dropdownValue = string.Empty;
                if (selectedOptionIndex >= 0)
                {
                    int localIndex = _pageOptionIndices.IndexOf(selectedOptionIndex);
                    if (localIndex >= 0)
                    {
                        dropdownValue = _options[selectedOptionIndex] ?? string.Empty;
                    }
                }

                if (string.IsNullOrEmpty(dropdownValue) && _pageChoices.Count > 0)
                {
                    dropdownValue = _pageChoices[0];
                }

                _dropdown.SetValueWithoutNotify(dropdownValue);
                SetValueWithoutNotify(dropdownValue);
                _dropdown.SetEnabled(_pageChoices.Count > 0);

                UpdateSuggestion(searchActive);
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

            private void UpdateSuggestion(bool searchActive)
            {
                if (_filteredIndices.Count == 0)
                {
                    UpdateSuggestionDisplay(string.Empty, -1, -1);
                    return;
                }

                bool hasSearch = searchActive && !string.IsNullOrEmpty(_searchText);
                int optionIndex = _filteredIndices[0];
                string optionValue = _options[optionIndex] ?? string.Empty;
                bool prefixMatch =
                    hasSearch
                    && optionValue.StartsWith(_searchText, StringComparison.OrdinalIgnoreCase);

                UpdateSuggestionDisplay(optionValue, optionIndex, prefixMatch ? 0 : -1);
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
                int localIndex = _pageChoices.IndexOf(optionValue);
                if (localIndex >= 0 && localIndex < _pageOptionIndices.Count)
                {
                    return _pageOptionIndices[localIndex];
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

            private static int GetPageCount(int pageSize, int filteredCount)
            {
                if (filteredCount <= 0)
                {
                    return 1;
                }

                return (filteredCount + pageSize - 1) / pageSize;
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

            public StringInListArrayElement(SerializedProperty property, string[] options)
            {
                _options = options ?? Array.Empty<string>();
                _serializedObject = property.serializedObject;
                _propertyPath = property.propertyPath;

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
                StringInListSelector selector = new(_options) { style = { marginBottom = 4f } };
                return selector;
            }

            private void BindItem(VisualElement element, int index)
            {
                if (element is not StringInListSelector selector)
                {
                    return;
                }

                SerializedProperty arrayProperty = GetArrayProperty();
                if (arrayProperty == null || index < 0 || index >= arrayProperty.arraySize)
                {
                    selector.UnbindProperty();
                    return;
                }

                SerializedProperty elementProperty = arrayProperty.GetArrayElementAtIndex(index);
                selector.BindProperty(elementProperty, elementProperty.displayName);
            }

            private void UnbindItem(VisualElement element, int index)
            {
                StringInListSelector selector = element as StringInListSelector;
                selector?.UnbindProperty();
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
    }
#endif
}

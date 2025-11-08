namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    /// <summary>
    /// UI Toolkit drawer for <see cref="StringInList"/> that provides search, pagination, and autocomplete.
    /// </summary>
    [CustomPropertyDrawer(typeof(StringInList))]
    public sealed class StringInListDrawer : PropertyDrawer
    {
        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            StringInList stringInList = (StringInList)attribute;
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
                PropertyField fallback = new PropertyField(property);
                fallback.label = property.displayName;
                return fallback;
            }

            StringInListSelector selector = new StringInListSelector(options);
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

        private sealed class StringInListSelector : VisualElement
        {
            private readonly string[] options;
            private readonly VisualElement searchRow;
            private readonly TextField searchField;
            private readonly Label suggestionLabel;
            private readonly Button clearButton;
            private readonly VisualElement paginationContainer;
            private readonly Button previousButton;
            private readonly Label pageLabel;
            private readonly Button nextButton;
            private readonly DropdownField dropdown;
            private readonly Label noResultsLabel;
            private readonly List<int> filteredIndices = new List<int>();
            private readonly List<int> pageOptionIndices = new List<int>();
            private readonly List<string> pageChoices = new List<string>();

            private SerializedObject boundObject;
            private string propertyPath = string.Empty;
            private bool isStringProperty;
            private bool isIntegerProperty;
            private string searchText = string.Empty;
            private string suggestion = string.Empty;
            private int pageIndex;
            private int lastResolvedPageSize = -1;
            private bool searchVisible;

            public StringInListSelector(string[] options)
            {
                this.options = options ?? Array.Empty<string>();
                lastResolvedPageSize = Mathf.Max(
                    1,
                    UnityHelpersSettings.GetStringInListPageLimit()
                );

                AddToClassList("unity-base-field");
                style.flexDirection = FlexDirection.Column;

                searchRow = new VisualElement();
                searchRow.style.flexDirection = FlexDirection.Row;
                searchRow.style.alignItems = Align.Center;
                searchRow.style.marginBottom = 4f;

                VisualElement searchWrapper = new VisualElement();
                searchWrapper.style.flexGrow = 1f;
                searchWrapper.style.position = Position.Relative;

                searchField = new TextField { name = "StringInListSearch" };
                searchField.style.flexGrow = 1f;
                searchField.RegisterValueChangedCallback(OnSearchChanged);
                searchField.RegisterCallback<KeyDownEvent>(OnSearchKeyDown);
                searchWrapper.Add(searchField);

                suggestionLabel = new Label();
                suggestionLabel.style.position = Position.Absolute;
                suggestionLabel.style.left = 6f;
                suggestionLabel.style.top = 2f;
                suggestionLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                suggestionLabel.style.color = new Color(1f, 1f, 1f, 0.35f);
                suggestionLabel.pickingMode = PickingMode.Ignore;
                suggestionLabel.style.display = DisplayStyle.None;
                searchWrapper.Add(suggestionLabel);

                clearButton = new Button(OnClearClicked) { text = "Clear" };
                clearButton.style.marginLeft = 4f;
                clearButton.SetEnabled(false);

                paginationContainer = new VisualElement();
                paginationContainer.style.flexDirection = FlexDirection.Row;
                paginationContainer.style.alignItems = Align.Center;
                paginationContainer.style.marginLeft = 4f;
                paginationContainer.style.display = DisplayStyle.None;

                previousButton = new Button(OnPreviousPage) { text = "<" };
                previousButton.style.marginRight = 4f;
                pageLabel = new Label();
                pageLabel.style.minWidth = 80f;
                pageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                pageLabel.style.marginRight = 4f;
                nextButton = new Button(OnNextPage) { text = ">" };

                paginationContainer.Add(previousButton);
                paginationContainer.Add(pageLabel);
                paginationContainer.Add(nextButton);

                searchRow.Add(searchWrapper);
                searchRow.Add(clearButton);
                searchRow.Add(paginationContainer);
                Add(searchRow);

                dropdown = new DropdownField { choices = new List<string>() };
                dropdown.style.flexGrow = 1f;
                dropdown.RegisterValueChangedCallback(OnDropdownValueChanged);
                Add(dropdown);

                noResultsLabel = new Label("No results match the current search.");
                noResultsLabel.style.display = DisplayStyle.None;
                noResultsLabel.style.color = new StyleColor(new Color(1f, 0.4f, 0.4f));
                noResultsLabel.style.marginLeft = 2f;
                Add(noResultsLabel);

                ApplySearchVisibility(ShouldShowSearch(lastResolvedPageSize));

                RegisterCallback<AttachToPanelEvent>(_ => Undo.undoRedoPerformed += OnUndoRedo);
                RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= OnUndoRedo);
            }

            public void BindProperty(SerializedProperty property, string labelText)
            {
                boundObject = property.serializedObject;
                propertyPath = property.propertyPath;
                isStringProperty = property.propertyType == SerializedPropertyType.String;
                isIntegerProperty = property.propertyType == SerializedPropertyType.Integer;
                dropdown.label = labelText;
                pageIndex = 0;
                searchText = string.Empty;
                suggestion = string.Empty;

                searchField.SetValueWithoutNotify(string.Empty);
                UpdateClearButton(searchVisible);
                UpdateSuggestionDisplay(string.Empty);

                UpdateFromProperty();
            }

            public void UnbindProperty()
            {
                boundObject = null;
                propertyPath = string.Empty;
            }

            private void OnUndoRedo()
            {
                UpdateFromProperty();
            }

            private void OnSearchChanged(ChangeEvent<string> evt)
            {
                if (!searchVisible)
                {
                    return;
                }

                searchText = evt.newValue ?? string.Empty;
                pageIndex = 0;
                UpdateClearButton(searchVisible);
                UpdateFromProperty();
            }

            private void OnSearchKeyDown(KeyDownEvent evt)
            {
                if (!searchVisible)
                {
                    return;
                }

                if (
                    (
                        evt.keyCode == KeyCode.Tab
                        || evt.keyCode == KeyCode.Return
                        || evt.keyCode == KeyCode.KeypadEnter
                    ) && !string.IsNullOrEmpty(suggestion)
                )
                {
                    evt.StopPropagation();
                    evt.PreventDefault();
                    AcceptSuggestion();
                }
            }

            private void OnClearClicked()
            {
                if (!searchVisible || string.IsNullOrEmpty(searchText))
                {
                    return;
                }

                searchText = string.Empty;
                searchField.SetValueWithoutNotify(string.Empty);

                pageIndex = 0;
                UpdateClearButton(searchVisible);
                UpdateSuggestionDisplay(string.Empty);
                UpdateFromProperty();
            }

            private void OnPreviousPage()
            {
                if (pageIndex <= 0)
                {
                    return;
                }

                pageIndex--;
                UpdateFromProperty();
            }

            private void OnNextPage()
            {
                int pageSize = ResolvePageSize();
                int pageCount = GetPageCount(pageSize, filteredIndices.Count);
                if (pageIndex >= pageCount - 1)
                {
                    return;
                }

                pageIndex++;
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
                if (boundObject == null || string.IsNullOrEmpty(propertyPath))
                {
                    return;
                }

                boundObject.Update();

                SerializedProperty property = boundObject.FindProperty(propertyPath);
                if (property == null)
                {
                    return;
                }

                if (property.propertyType == SerializedPropertyType.String)
                {
                    isStringProperty = true;
                    isIntegerProperty = false;
                }
                else if (property.propertyType == SerializedPropertyType.Integer)
                {
                    isStringProperty = false;
                    isIntegerProperty = true;
                }

                int pageSize = ResolvePageSize();
                bool searchActive = ShouldShowSearch(pageSize);
                ApplySearchVisibility(searchActive);

                int selectedOptionIndex = GetCurrentSelectionIndex(property);

                filteredIndices.Clear();
                string effectiveSearch = searchActive ? (searchText ?? string.Empty) : string.Empty;
                bool hasSearch = searchActive && !string.IsNullOrWhiteSpace(effectiveSearch);
                for (int i = 0; i < options.Length; i++)
                {
                    string option = options[i] ?? string.Empty;
                    if (
                        !hasSearch
                        || option.IndexOf(effectiveSearch, StringComparison.OrdinalIgnoreCase) >= 0
                    )
                    {
                        filteredIndices.Add(i);
                    }
                }

                if (filteredIndices.Count == 0)
                {
                    dropdown.choices = new List<string>();
                    dropdown.SetValueWithoutNotify(string.Empty);
                    dropdown.SetEnabled(false);
                    noResultsLabel.style.display = DisplayStyle.Flex;
                    UpdatePagination(searchActive, 0, pageSize, 0);
                    UpdateSuggestionDisplay(string.Empty);
                    return;
                }

                noResultsLabel.style.display = DisplayStyle.None;

                int pageCount = GetPageCount(pageSize, filteredIndices.Count);
                if (selectedOptionIndex >= 0)
                {
                    int filteredIndex = filteredIndices.IndexOf(selectedOptionIndex);
                    if (filteredIndex >= 0)
                    {
                        pageIndex = filteredIndex / pageSize;
                    }
                    else if (pageIndex >= pageCount)
                    {
                        pageIndex = 0;
                    }
                }
                else if (pageIndex >= pageCount)
                {
                    pageIndex = 0;
                }

                UpdatePagination(searchActive, pageCount, pageSize, filteredIndices.Count);

                bool paginate = searchActive && filteredIndices.Count > pageSize;

                pageOptionIndices.Clear();
                pageChoices.Clear();

                int startIndex = paginate ? pageIndex * pageSize : 0;
                int endIndex = paginate
                    ? Math.Min(filteredIndices.Count, startIndex + pageSize)
                    : filteredIndices.Count;

                for (int i = startIndex; i < endIndex; i++)
                {
                    int optionIndex = filteredIndices[i];
                    pageOptionIndices.Add(optionIndex);
                    pageChoices.Add(options[optionIndex] ?? string.Empty);
                }

                dropdown.choices = new List<string>(pageChoices);

                string dropdownValue = string.Empty;
                if (selectedOptionIndex >= 0)
                {
                    int localIndex = pageOptionIndices.IndexOf(selectedOptionIndex);
                    if (localIndex >= 0)
                    {
                        dropdownValue = options[selectedOptionIndex] ?? string.Empty;
                    }
                }

                if (string.IsNullOrEmpty(dropdownValue) && pageChoices.Count > 0)
                {
                    dropdownValue = pageChoices[0];
                }

                dropdown.SetValueWithoutNotify(dropdownValue);
                dropdown.SetEnabled(pageChoices.Count > 0);

                UpdateSuggestion(searchActive);
            }

            private void UpdatePagination(
                bool searchActive,
                int pageCount,
                int pageSize,
                int filteredCount
            )
            {
                if (
                    paginationContainer == null
                    || previousButton == null
                    || nextButton == null
                    || pageLabel == null
                )
                {
                    return;
                }

                bool showPagination = searchActive && filteredCount > pageSize;
                paginationContainer.style.display = showPagination
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

                if (!showPagination)
                {
                    pageLabel.text = string.Empty;
                    previousButton.SetEnabled(false);
                    nextButton.SetEnabled(false);
                    return;
                }

                int clampedPageCount = Math.Max(1, pageCount);
                pageIndex = Mathf.Clamp(pageIndex, 0, clampedPageCount - 1);
                pageLabel.text = $"Page {pageIndex + 1}/{clampedPageCount}";
                previousButton.SetEnabled(pageIndex > 0);
                nextButton.SetEnabled(pageIndex < clampedPageCount - 1);
            }

            private void UpdateClearButton(bool searchActive)
            {
                if (clearButton == null)
                {
                    return;
                }

                clearButton.SetEnabled(searchActive && !string.IsNullOrEmpty(searchText));
            }

            private void UpdateSuggestion(bool searchActive)
            {
                if (!searchActive || suggestionLabel == null)
                {
                    UpdateSuggestionDisplay(string.Empty);
                    return;
                }

                if (string.IsNullOrEmpty(searchText))
                {
                    UpdateSuggestionDisplay(string.Empty);
                    return;
                }

                string bestMatch = string.Empty;
                for (int i = 0; i < filteredIndices.Count; i++)
                {
                    string option = options[filteredIndices[i]] ?? string.Empty;
                    if (option.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        bestMatch = option;
                        break;
                    }
                }

                if (
                    string.IsNullOrEmpty(bestMatch)
                    || string.Equals(bestMatch, searchText, StringComparison.Ordinal)
                )
                {
                    UpdateSuggestionDisplay(string.Empty);
                    return;
                }

                UpdateSuggestionDisplay(bestMatch);
            }

            private void UpdateSuggestionDisplay(string suggestionValue)
            {
                suggestion = suggestionValue;
                if (suggestionLabel == null)
                {
                    return;
                }

                bool visible =
                    !string.IsNullOrEmpty(suggestionValue)
                    && !string.Equals(suggestionValue, searchText, StringComparison.Ordinal);
                suggestionLabel.text = suggestionValue;
                suggestionLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            private void AcceptSuggestion()
            {
                if (!searchVisible || string.IsNullOrEmpty(suggestion) || searchField == null)
                {
                    return;
                }

                searchText = suggestion;
                searchField.SetValueWithoutNotify(suggestion);
                searchField.SelectRange(suggestion.Length, suggestion.Length);
                UpdateClearButton(searchVisible);
                UpdateSuggestionDisplay(string.Empty);
                UpdateFromProperty();
            }

            private int ResolvePageSize()
            {
                int resolved = Mathf.Max(1, UnityHelpersSettings.GetStringInListPageLimit());
                if (resolved != lastResolvedPageSize)
                {
                    lastResolvedPageSize = resolved;
                    pageIndex = 0;
                }

                return resolved;
            }

            private bool ShouldShowSearch(int pageSize)
            {
                return options.Length > pageSize;
            }

            private void ApplySearchVisibility(bool visible)
            {
                if (searchRow == null)
                {
                    return;
                }

                searchVisible = visible;
                searchRow.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

                if (dropdown != null)
                {
                    dropdown.style.marginTop = visible ? 2f : 0f;
                }

                if (!visible)
                {
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        searchText = string.Empty;
                        searchField.SetValueWithoutNotify(string.Empty);
                    }

                    UpdateSuggestionDisplay(string.Empty);

                    if (paginationContainer != null)
                    {
                        paginationContainer.style.display = DisplayStyle.None;
                    }

                    if (previousButton != null)
                    {
                        previousButton.SetEnabled(false);
                    }

                    if (nextButton != null)
                    {
                        nextButton.SetEnabled(false);
                    }
                }

                UpdateClearButton(visible);
            }

            private int ResolveOptionIndex(string optionValue)
            {
                int localIndex = pageChoices.IndexOf(optionValue);
                if (localIndex >= 0 && localIndex < pageOptionIndices.Count)
                {
                    return pageOptionIndices[localIndex];
                }

                return Array.IndexOf(options, optionValue);
            }

            private int GetCurrentSelectionIndex(SerializedProperty property)
            {
                if (isStringProperty)
                {
                    string value = property.stringValue ?? string.Empty;
                    return Array.IndexOf(options, value);
                }

                if (isIntegerProperty)
                {
                    int index = property.intValue;
                    if (index < 0 || index >= options.Length)
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
                if (boundObject == null || string.IsNullOrEmpty(propertyPath))
                {
                    return;
                }

                SerializedObject serializedObject = boundObject;
                Undo.RecordObjects(serializedObject.targetObjects, "Change String In List");
                serializedObject.Update();

                SerializedProperty property = serializedObject.FindProperty(propertyPath);
                if (property == null)
                {
                    return;
                }

                if (isStringProperty)
                {
                    property.stringValue = options[optionIndex] ?? string.Empty;
                }
                else if (isIntegerProperty)
                {
                    property.intValue = optionIndex;
                }

                serializedObject.ApplyModifiedProperties();
                UpdateFromProperty();
            }
        }

        private sealed class StringInListArrayElement : VisualElement
        {
            private readonly string[] options;
            private readonly SerializedObject serializedObject;
            private readonly string propertyPath;
            private readonly List<int> indices = new List<int>();
            private readonly ListView listView;
            private readonly ToolbarButton addButton;
            private readonly ToolbarButton removeButton;

            public StringInListArrayElement(SerializedProperty property, string[] options)
            {
                this.options = options ?? Array.Empty<string>();
                serializedObject = property.serializedObject;
                propertyPath = property.propertyPath;

                AddToClassList("unity-base-field");
                style.flexDirection = FlexDirection.Column;

                Label header = new Label(property.displayName);
                header.AddToClassList("unity-base-field__label");
                header.style.unityFontStyleAndWeight = FontStyle.Bold;
                header.style.marginBottom = 2f;
                Add(header);

                Toolbar toolbar = new Toolbar();
                toolbar.style.marginLeft = -2f;
                toolbar.style.marginBottom = 4f;

                addButton = new ToolbarButton(AddItem) { text = "Add" };
                removeButton = new ToolbarButton(RemoveSelected) { text = "Remove" };
                removeButton.SetEnabled(false);

                toolbar.Add(addButton);
                toolbar.Add(removeButton);
                Add(toolbar);

                listView = new ListView(indices, -1f, MakeItem, BindItem);
                listView.unbindItem = UnbindItem;
                listView.name = "StringInListArray";
                listView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
                listView.selectionType = SelectionType.Single;
                listView.reorderable = true;
                listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
                listView.style.flexGrow = 1f;
                listView.itemIndexChanged += OnItemIndexChanged;
                listView.itemsRemoved += OnItemsRemoved;
                listView.selectionChanged += OnSelectionChanged;
                listView.RegisterCallback<KeyDownEvent>(OnListKeyDown);
                Add(listView);

                RegisterCallback<AttachToPanelEvent>(_ => Undo.undoRedoPerformed += OnUndoRedo);
                RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= OnUndoRedo);

                Refresh();
            }

            private VisualElement MakeItem()
            {
                StringInListSelector selector = new StringInListSelector(options);
                selector.style.marginBottom = 4f;
                return selector;
            }

            private void BindItem(VisualElement element, int index)
            {
                StringInListSelector selector = element as StringInListSelector;
                if (selector == null)
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

                Undo.RecordObjects(serializedObject.targetObjects, "Add String Entry");
                serializedObject.Update();

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

                serializedObject.ApplyModifiedProperties();
                Refresh();
                listView.selectedIndex = indices.Count - 1;
            }

            private void RemoveSelected()
            {
                int selectedIndex = listView.selectedIndex;
                if (selectedIndex < 0)
                {
                    return;
                }

                SerializedProperty arrayProperty = GetArrayProperty();
                if (arrayProperty == null)
                {
                    return;
                }

                Undo.RecordObjects(serializedObject.targetObjects, "Remove String Entry");
                serializedObject.Update();
                arrayProperty.DeleteArrayElementAtIndex(selectedIndex);
                serializedObject.ApplyModifiedProperties();
                Refresh();

                if (indices.Count > 0)
                {
                    listView.selectedIndex = Mathf.Clamp(selectedIndex, 0, indices.Count - 1);
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

                Undo.RecordObjects(serializedObject.targetObjects, "Reorder String Entries");
                serializedObject.Update();
                arrayProperty.MoveArrayElement(oldIndex, newIndex);
                serializedObject.ApplyModifiedProperties();
                Refresh();
                listView.selectedIndex = newIndex;
            }

            private void OnItemsRemoved(IEnumerable<int> removedIndices)
            {
                Refresh();
            }

            private void OnSelectionChanged(IEnumerable<object> _)
            {
                removeButton.SetEnabled(listView.selectedIndex >= 0 && indices.Count > 0);
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
                return serializedObject.FindProperty(propertyPath);
            }

            private void Refresh()
            {
                serializedObject.Update();
                SerializedProperty arrayProperty = GetArrayProperty();

                indices.Clear();
                if (arrayProperty != null)
                {
                    int size = arrayProperty.arraySize;
                    for (int i = 0; i < size; i++)
                    {
                        indices.Add(i);
                    }
                }

                listView.RefreshItems();
                removeButton.SetEnabled(listView.selectedIndex >= 0 && indices.Count > 0);
            }
        }
    }
#endif
}

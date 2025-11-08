namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    /// <summary>
    /// Property drawer that provides search, paging, and lightweight autocomplete for SerializableType.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializableType))]
    public sealed class SerializableTypeDrawer : PropertyDrawer
    {
        private sealed class DrawerState
        {
            public string Search = string.Empty;
            public string LastValue = string.Empty;
            public int Page;
        }

        private const int PageSize = 25;
        private const float ClearWidth = 50f;
        private const float ButtonWidth = 24f;
        private const float PageLabelWidth = 90f;

        private static readonly Dictionary<string, DrawerState> States =
            new Dictionary<string, DrawerState>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            return (lineHeight * 2f) + spacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty assemblyQualifiedName = property.FindPropertyRelative(
                "_assemblyQualifiedName"
            );
            if (assemblyQualifiedName == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            DrawerState state = GetState(property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            Rect searchRow = new Rect(position.x, position.y, position.width, lineHeight);
            Rect popupRow = new Rect(
                position.x,
                searchRow.yMax + spacing,
                position.width,
                lineHeight
            );

            Rect searchRowIndented = EditorGUI.IndentedRect(searchRow);
            Rect popupRowIndented = EditorGUI.IndentedRect(popupRow);

            float buttonSpacing = spacing;
            float controlsWidth =
                ClearWidth + (ButtonWidth * 2f) + PageLabelWidth + (buttonSpacing * 3f);

            float searchLabelWidth = Mathf.Min(
                EditorGUIUtility.labelWidth,
                Math.Max(0f, searchRowIndented.width - controlsWidth - 10f)
            );

            Rect searchLabelRect = new Rect(
                searchRowIndented.x,
                searchRowIndented.y,
                searchLabelWidth,
                lineHeight
            );

            float searchFieldWidth = Mathf.Max(
                0f,
                searchRowIndented.width - searchLabelWidth - controlsWidth
            );

            Rect searchFieldRect = new Rect(
                searchLabelRect.xMax,
                searchRowIndented.y,
                searchFieldWidth,
                lineHeight
            );

            Rect clearRect = new Rect(
                searchFieldRect.xMax + buttonSpacing,
                searchRowIndented.y,
                ClearWidth,
                lineHeight
            );

            Rect prevRect = new Rect(
                clearRect.xMax + buttonSpacing,
                searchRowIndented.y,
                ButtonWidth,
                lineHeight
            );

            Rect pageInfoRect = new Rect(
                prevRect.xMax + buttonSpacing,
                searchRowIndented.y,
                PageLabelWidth,
                lineHeight
            );

            Rect nextRect = new Rect(
                pageInfoRect.xMax + buttonSpacing,
                searchRowIndented.y,
                ButtonWidth,
                lineHeight
            );

            using (new EditorGUI.PropertyScope(position, GUIContent.none, property))
            {
                EditorGUI.LabelField(searchLabelRect, "Search");

                string controlName = $"SerializableTypeSearch_{property.propertyPath}";
                GUI.SetNextControlName(controlName);
                string incomingSearch = EditorGUI.TextField(
                    searchFieldRect,
                    GUIContent.none,
                    state.Search ?? string.Empty
                );
                if (!string.Equals(incomingSearch, state.Search, StringComparison.Ordinal))
                {
                    state.Search = incomingSearch ?? string.Empty;
                    state.Page = 0;
                }

                IReadOnlyList<SerializableTypeCatalog.SerializableTypeDescriptor> filtered =
                    SerializableTypeCatalog.GetFilteredDescriptors(state.Search ?? string.Empty);

                int pageCount = Math.Max(1, (filtered.Count + PageSize - 1) / PageSize);
                state.Page = Mathf.Clamp(state.Page, 0, pageCount - 1);

                string currentValue = assemblyQualifiedName.stringValue ?? string.Empty;
                int globalIndex = FindDescriptorIndex(filtered, currentValue);

                if (!string.Equals(state.LastValue, currentValue, StringComparison.Ordinal))
                {
                    state.LastValue = currentValue;
                    if (globalIndex >= 0)
                    {
                        state.Page = globalIndex / PageSize;
                    }
                }

                int startIndex = state.Page * PageSize;
                if (startIndex >= filtered.Count && filtered.Count > 0)
                {
                    state.Page = 0;
                    startIndex = 0;
                }

                int endIndex = Math.Min(filtered.Count, startIndex + PageSize);
                int pageLength = Math.Max(0, endIndex - startIndex);
                if (pageLength == 0 && filtered.Count > 0)
                {
                    pageLength = Math.Min(PageSize, filtered.Count);
                    startIndex = 0;
                    endIndex = pageLength;
                }

                bool hasSuggestion = false;
                bool acceptedSuggestion = false;
                string suggestion = string.Empty;
                string focusedControl = GUI.GetNameOfFocusedControl();
                if (focusedControl == controlName && filtered.Count > 0)
                {
                    suggestion = filtered[0].DisplayName;
                    if (
                        !string.IsNullOrEmpty(state.Search)
                        && !string.IsNullOrEmpty(suggestion)
                        && suggestion.StartsWith(state.Search, StringComparison.OrdinalIgnoreCase)
                        && suggestion.Length > state.Search.Length
                        && searchFieldRect.width > 0f
                    )
                    {
                        hasSuggestion = true;
                        Event evt = Event.current;
                        if (
                            evt.type == EventType.KeyDown
                            && (evt.keyCode == KeyCode.Tab || evt.keyCode == KeyCode.Return)
                        )
                        {
                            state.Search = suggestion;
                            GUI.changed = true;
                            acceptedSuggestion = true;
                            evt.Use();
                            filtered = SerializableTypeCatalog.GetFilteredDescriptors(state.Search);
                            pageCount = Math.Max(1, (filtered.Count + PageSize - 1) / PageSize);
                            state.Page = Mathf.Clamp(state.Page, 0, pageCount - 1);
                            startIndex = state.Page * PageSize;
                            endIndex = Math.Min(filtered.Count, startIndex + PageSize);
                            pageLength = Math.Max(0, endIndex - startIndex);
                            globalIndex = FindDescriptorIndex(filtered, currentValue);
                            GUI.FocusControl(controlName);
                        }
                    }
                }

                if (hasSuggestion)
                {
                    if (filtered.Count > 0)
                    {
                        suggestion = filtered[0].DisplayName;
                        if (
                            string.IsNullOrEmpty(state.Search)
                            || !suggestion.StartsWith(
                                state.Search,
                                StringComparison.OrdinalIgnoreCase
                            )
                            || suggestion.Length <= state.Search.Length
                            || searchFieldRect.width <= 0f
                        )
                        {
                            hasSuggestion = false;
                        }
                    }
                    else
                    {
                        hasSuggestion = false;
                    }

                    if (acceptedSuggestion)
                    {
                        hasSuggestion = false;
                    }
                }

                if (hasSuggestion && Event.current.type == EventType.Repaint)
                {
                    Color originalColor = GUI.color;
                    GUI.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.35f);
                    EditorGUI.LabelField(searchFieldRect, suggestion, EditorStyles.label);
                    GUI.color = originalColor;
                }

                GUI.enabled = filtered.Count > 0;
                EditorGUI.LabelField(pageInfoRect, $"Page {state.Page + 1}/{pageCount}");

                GUI.enabled =
                    !string.IsNullOrEmpty(state.Search) || !string.IsNullOrEmpty(currentValue);
                if (GUI.Button(clearRect, "Clear"))
                {
                    assemblyQualifiedName.stringValue = string.Empty;
                    currentValue = string.Empty;
                    globalIndex = -1;
                    state.LastValue = string.Empty;
                    state.Search = string.Empty;
                    state.Page = 0;
                    filtered = SerializableTypeCatalog.GetFilteredDescriptors(string.Empty);
                    pageCount = Math.Max(1, (filtered.Count + PageSize - 1) / PageSize);
                    startIndex = 0;
                    endIndex = Math.Min(filtered.Count, PageSize);
                    pageLength = Math.Max(0, endIndex - startIndex);
                    GUI.FocusControl(controlName);
                }

                GUI.enabled = state.Page > 0;
                if (GUI.Button(prevRect, "<"))
                {
                    state.Page = Math.Max(0, state.Page - 1);
                }

                GUI.enabled = state.Page < (pageCount - 1);
                if (GUI.Button(nextRect, ">"))
                {
                    state.Page = Math.Min(pageCount - 1, state.Page + 1);
                }
                GUI.enabled = true;

                GUIContent[] optionContents;
                string[] optionValues;
                if (pageLength == 0)
                {
                    optionContents = new[]
                    {
                        new GUIContent(SerializableTypeCatalog.NoneDisplayName),
                    };
                    optionValues = new[] { string.Empty };
                }
                else
                {
                    optionContents = new GUIContent[pageLength];
                    optionValues = new string[pageLength];
                    for (int index = 0; index < pageLength; index++)
                    {
                        SerializableTypeCatalog.SerializableTypeDescriptor descriptor = filtered[
                            startIndex + index
                        ];
                        optionContents[index] = new GUIContent(descriptor.DisplayName);
                        optionValues[index] = descriptor.AssemblyQualifiedName;
                    }
                }

                int localIndex = -1;
                if (globalIndex >= startIndex && globalIndex < endIndex)
                {
                    localIndex = globalIndex - startIndex;
                }

                if (localIndex < 0)
                {
                    localIndex = 0;
                }

                Rect popupRect = popupRowIndented;
                EditorGUI.BeginChangeCheck();
                int selectedIndex = EditorGUI.Popup(popupRect, localIndex, optionContents);
                if (EditorGUI.EndChangeCheck())
                {
                    selectedIndex = Mathf.Clamp(selectedIndex, 0, optionValues.Length - 1);
                    string selectedValue = optionValues[selectedIndex];
                    if (!string.Equals(selectedValue, currentValue, StringComparison.Ordinal))
                    {
                        assemblyQualifiedName.stringValue = selectedValue;
                        state.LastValue = selectedValue;
                    }
                }
            }
        }

        private static DrawerState GetState(SerializedProperty property)
        {
            string key = property.propertyPath;
            if (States.TryGetValue(key, out DrawerState existing))
            {
                return existing;
            }

            DrawerState state = new DrawerState();
            States[key] = state;
            return state;
        }

        private static int FindDescriptorIndex(
            IReadOnlyList<SerializableTypeCatalog.SerializableTypeDescriptor> descriptors,
            string assemblyQualifiedName
        )
        {
            if (descriptors == null || descriptors.Count == 0)
            {
                return -1;
            }

            for (int index = 0; index < descriptors.Count; index++)
            {
                SerializableTypeCatalog.SerializableTypeDescriptor descriptor = descriptors[index];
                if (
                    string.Equals(
                        descriptor.AssemblyQualifiedName,
                        assemblyQualifiedName,
                        StringComparison.Ordinal
                    )
                )
                {
                    return index;
                }
            }

            return -1;
        }
    }
#endif
}

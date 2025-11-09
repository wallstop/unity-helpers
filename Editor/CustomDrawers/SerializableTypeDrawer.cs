namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    /// <summary>
    /// Property drawer that provides search, paging, and lightweight autocomplete for SerializableType.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializableType))]
    public sealed class SerializableTypeDrawer : PropertyDrawer
    {
        private sealed class DrawerState
        {
            public string search = string.Empty;
            public string lastValue = string.Empty;
            public int page;
        }

        private const float ClearWidth = 50f;
        private const float ButtonWidth = 24f;
        private const float PageLabelWidth = 90f;

        private static readonly Dictionary<string, DrawerState> States = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            return lineHeight * 2f + spacing;
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

            Rect searchRow = new(position.x, position.y, position.width, lineHeight);
            Rect popupRow = new(position.x, searchRow.yMax + spacing, position.width, lineHeight);

            Rect searchRowIndented = EditorGUI.IndentedRect(searchRow);
            Rect popupRowIndented = EditorGUI.IndentedRect(popupRow);

            float controlsWidth = ClearWidth + ButtonWidth * 2f + PageLabelWidth + spacing * 3f;

            bool showSearchLabel = searchRowIndented.width - controlsWidth > 60f;
            float searchLabelWidth = showSearchLabel
                ? Mathf.Min(60f, searchRowIndented.width - controlsWidth - 10f)
                : 0f;

            Rect searchLabelRect = new(
                searchRowIndented.x,
                searchRowIndented.y,
                searchLabelWidth,
                lineHeight
            );

            float searchFieldWidth = Mathf.Max(
                0f,
                searchRowIndented.width - searchLabelWidth - controlsWidth
            );

            Rect searchFieldRect = new(
                showSearchLabel ? searchLabelRect.xMax : searchRowIndented.x,
                searchRowIndented.y,
                searchFieldWidth,
                lineHeight
            );

            Rect clearRect = new(
                searchFieldRect.xMax + spacing,
                searchRowIndented.y,
                ClearWidth,
                lineHeight
            );

            Rect prevRect = new(
                clearRect.xMax + spacing,
                searchRowIndented.y,
                ButtonWidth,
                lineHeight
            );

            Rect pageInfoRect = new(
                prevRect.xMax + spacing,
                searchRowIndented.y,
                PageLabelWidth,
                lineHeight
            );

            Rect nextRect = new(
                pageInfoRect.xMax + spacing,
                searchRowIndented.y,
                ButtonWidth,
                lineHeight
            );

            int pageSize = Mathf.Max(1, UnityHelpersSettings.GetStringInListPageLimit());

            using EditorGUI.PropertyScope scope = new(position, label, property);
            GUIContent propertyLabel = scope.content;
            if (showSearchLabel)
            {
                EditorGUI.LabelField(searchLabelRect, "Search");
            }

            string controlName = $"SerializableTypeSearch_{property.propertyPath}";
            GUI.SetNextControlName(controlName);
            string incomingSearch = EditorGUI.TextField(
                searchFieldRect,
                GUIContent.none,
                state.search ?? string.Empty
            );
            if (!string.Equals(incomingSearch, state.search, StringComparison.Ordinal))
            {
                state.search = incomingSearch ?? string.Empty;
                state.page = 0;
            }

            IReadOnlyList<SerializableTypeCatalog.SerializableTypeDescriptor> filtered =
                SerializableTypeCatalog.GetFilteredDescriptors(state.search ?? string.Empty);

            int pageCount = Math.Max(1, (filtered.Count + pageSize - 1) / pageSize);
            state.page = Mathf.Clamp(state.page, 0, pageCount - 1);

            string currentValue = assemblyQualifiedName.stringValue ?? string.Empty;
            int globalIndex = FindDescriptorIndex(filtered, currentValue);

            if (!string.Equals(state.lastValue, currentValue, StringComparison.Ordinal))
            {
                state.lastValue = currentValue;
                if (globalIndex >= 0)
                {
                    state.page = globalIndex / pageSize;
                }
            }

            int startIndex = state.page * pageSize;
            if (startIndex >= filtered.Count && filtered.Count > 0)
            {
                state.page = 0;
                startIndex = 0;
            }

            int endIndex = Math.Min(filtered.Count, startIndex + pageSize);
            int pageLength = Math.Max(0, endIndex - startIndex);
            if (pageLength == 0 && filtered.Count > 0)
            {
                pageLength = Math.Min(pageSize, filtered.Count);
                startIndex = 0;
                endIndex = pageLength;
            }

            SerializableTypeCatalog.SerializableTypeDescriptor chosenDescriptor =
                filtered.Count > 0 ? filtered[0] : default;
            int suggestionMatchPos = string.IsNullOrEmpty(state.search) ? 0 : -1;
            string focusedControl = GUI.GetNameOfFocusedControl();
            if (focusedControl == controlName && filtered.Count > 0)
            {
                if (!string.IsNullOrEmpty(state.search))
                {
                    int bestPos = int.MaxValue;
                    int bestLength = int.MaxValue;
                    for (int i = 0; i < filtered.Count; i++)
                    {
                        SerializableTypeCatalog.SerializableTypeDescriptor descriptor = filtered[i];
                        string displayName = descriptor.DisplayName ?? string.Empty;
                        int matchPos = displayName.IndexOf(
                            state.search,
                            StringComparison.OrdinalIgnoreCase
                        );
                        if (matchPos < 0)
                        {
                            continue;
                        }

                        if (
                            matchPos < bestPos
                            || (matchPos == bestPos && displayName.Length < bestLength)
                        )
                        {
                            bestPos = matchPos;
                            bestLength = displayName.Length;
                            chosenDescriptor = descriptor;
                        }
                    }
                    suggestionMatchPos = bestPos == int.MaxValue ? -1 : bestPos;
                }

                string suggestion = chosenDescriptor.DisplayName;
                if (
                    !string.IsNullOrEmpty(state.search)
                    && !string.IsNullOrEmpty(suggestion)
                    && suggestionMatchPos >= 0
                    && !string.Equals(suggestion, state.search, StringComparison.OrdinalIgnoreCase)
                )
                {
                    Event evt = Event.current;
                    if (
                        evt.type == EventType.KeyDown
                        && evt.keyCode is KeyCode.Tab or KeyCode.Return
                    )
                    {
                        bool commitSelection = evt.keyCode == KeyCode.Tab && !evt.shift;
                        state.search = suggestion;
                        GUI.changed = true;
                        GUI.FocusControl(controlName);
                        EditorGUI.FocusTextInControl(controlName);
                        evt.Use();
                        filtered = SerializableTypeCatalog.GetFilteredDescriptors(state.search);
                        pageCount = Math.Max(1, (filtered.Count + pageSize - 1) / pageSize);
                        state.page = Mathf.Clamp(state.page, 0, pageCount - 1);
                        startIndex = state.page * pageSize;
                        endIndex = Math.Min(filtered.Count, startIndex + pageSize);
                        pageLength = Math.Max(0, endIndex - startIndex);
                        globalIndex = FindDescriptorIndex(filtered, currentValue);
                        if (commitSelection)
                        {
                            SerializableTypeCatalog.SerializableTypeDescriptor descriptor =
                                FindDescriptorIndex(
                                    filtered,
                                    chosenDescriptor.AssemblyQualifiedName
                                ) >= 0
                                    ? chosenDescriptor
                                    : filtered[0];
                            assemblyQualifiedName.stringValue = descriptor.AssemblyQualifiedName;
                            currentValue = descriptor.AssemblyQualifiedName;
                            state.lastValue = currentValue;
                            globalIndex = FindDescriptorIndex(filtered, currentValue);
                        }
                    }
                }
            }

            bool showPagination = filtered.Count > pageSize;
            GUI.enabled = filtered.Count > 0 && showPagination;
            if (showPagination)
            {
                EditorGUI.LabelField(pageInfoRect, $"Page {state.page + 1}/{pageCount}");
            }
            else
            {
                EditorGUI.LabelField(pageInfoRect, GUIContent.none);
            }

            GUI.enabled = !string.IsNullOrEmpty(state.search);
            if (GUI.Button(clearRect, "Clear"))
            {
                state.search = string.Empty;
                state.page = 0;
                GUI.changed = true;
                filtered = SerializableTypeCatalog.GetFilteredDescriptors(string.Empty);
                pageCount = Math.Max(1, (filtered.Count + pageSize - 1) / pageSize);
                globalIndex = FindDescriptorIndex(filtered, currentValue);
                if (globalIndex >= 0)
                {
                    state.page = Mathf.Clamp(globalIndex / pageSize, 0, pageCount - 1);
                }
                startIndex = state.page * pageSize;
                endIndex = Math.Min(filtered.Count, startIndex + pageSize);
                pageLength = Math.Max(0, endIndex - startIndex);
                showPagination = filtered.Count > pageSize;
                GUI.FocusControl(controlName);
            }

            GUI.enabled = showPagination && filtered.Count > 0 && state.page > 0;
            if (GUI.Button(prevRect, "<"))
            {
                state.page = Math.Max(0, state.page - 1);
            }

            GUI.enabled = showPagination && filtered.Count > 0 && state.page < pageCount - 1;
            if (GUI.Button(nextRect, ">"))
            {
                state.page = Math.Min(pageCount - 1, state.page + 1);
            }
            GUI.enabled = true;

            GUIContent[] optionContents;
            string[] optionValues;
            if (pageLength == 0)
            {
                optionContents = new[] { new GUIContent(SerializableTypeCatalog.NoneDisplayName) };
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

            EditorGUI.BeginChangeCheck();
            int selectedIndex = EditorGUI.Popup(
                popupRowIndented,
                propertyLabel,
                localIndex,
                optionContents
            );
            if (EditorGUI.EndChangeCheck())
            {
                selectedIndex = Mathf.Clamp(selectedIndex, 0, optionValues.Length - 1);
                string selectedValue = optionValues[selectedIndex];
                if (!string.Equals(selectedValue, currentValue, StringComparison.Ordinal))
                {
                    assemblyQualifiedName.stringValue = selectedValue;
                    state.lastValue = selectedValue;
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

            DrawerState state = new();
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

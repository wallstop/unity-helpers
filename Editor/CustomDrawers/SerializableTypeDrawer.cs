namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    /// <summary>
    /// Property drawer that provides search and pagination for <see cref="SerializableType"/>.
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

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            return (lineHeight * 2f) + spacing;
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty nameProperty = property.FindPropertyRelative(
                "_assemblyQualifiedName"
            );
            if (nameProperty == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            DrawerState state = GetState(property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            Rect typeRect = new Rect(position.x, position.y, position.width, lineHeight);

            float reservedWidth = ClearWidth + (ButtonWidth * 2f) + PageLabelWidth + (spacing * 4f);
            float searchWidth = Math.Max(0f, position.width - reservedWidth);
            Rect searchRect = new Rect(
                position.x,
                typeRect.yMax + spacing,
                searchWidth,
                lineHeight
            );

            Rect clearRect = new Rect(
                searchRect.xMax + spacing,
                searchRect.y,
                ClearWidth,
                lineHeight
            );
            Rect prevRect = new Rect(
                clearRect.xMax + spacing,
                searchRect.y,
                ButtonWidth,
                lineHeight
            );
            Rect pageInfoRect = new Rect(
                prevRect.xMax + spacing,
                searchRect.y,
                PageLabelWidth,
                lineHeight
            );
            Rect nextRect = new Rect(
                pageInfoRect.xMax + spacing,
                searchRect.y,
                ButtonWidth,
                lineHeight
            );

            EditorGUI.BeginProperty(position, label, property);
            int originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            try
            {
                string searchInput = state.Search;
                if (searchRect.width > 0f)
                {
                    searchInput = EditorGUI.TextField(searchRect, "Search", state.Search);
                }
                else
                {
                    EditorGUI.LabelField(searchRect, GUIContent.none);
                }

                if (!string.Equals(searchInput, state.Search, StringComparison.Ordinal))
                {
                    state.Search = searchInput;
                    state.Page = 0;
                }

                IReadOnlyList<SerializableTypeCatalog.SerializableTypeDescriptor> filtered =
                    SerializableTypeCatalog.GetFilteredDescriptors(state.Search);

                int pageCount = Math.Max(1, (filtered.Count + PageSize - 1) / PageSize);

                string currentValue = nameProperty.stringValue ?? string.Empty;
                int globalIndex = FindDescriptorIndex(filtered, currentValue);

                if (!string.Equals(state.LastValue, currentValue, StringComparison.Ordinal))
                {
                    state.LastValue = currentValue;
                    if (globalIndex >= 0)
                    {
                        state.Page = globalIndex / PageSize;
                    }
                }

                state.Page = Mathf.Clamp(state.Page, 0, pageCount - 1);

                int startIndex = state.Page * PageSize;
                int endIndex = Math.Min(filtered.Count, startIndex + PageSize);
                if (endIndex <= startIndex && filtered.Count > 0)
                {
                    startIndex = 0;
                    endIndex = Math.Min(filtered.Count, PageSize);
                }

                int pageLength = Math.Max(0, endIndex - startIndex);
                if (pageLength == 0 && filtered.Count > 0)
                {
                    pageLength = Math.Min(PageSize, filtered.Count);
                    startIndex = 0;
                    endIndex = pageLength;
                }

                string[] optionLabels = new string[pageLength];
                string[] optionValues = new string[pageLength];
                for (int index = 0; index < pageLength; index++)
                {
                    SerializableTypeCatalog.SerializableTypeDescriptor descriptor = filtered[
                        startIndex + index
                    ];
                    optionLabels[index] = descriptor.DisplayName;
                    optionValues[index] = descriptor.AssemblyQualifiedName;
                }

                int localIndex = -1;
                if (globalIndex >= startIndex && globalIndex < endIndex)
                {
                    localIndex = globalIndex - startIndex;
                }

                if (optionLabels.Length == 0)
                {
                    optionLabels = new[] { SerializableTypeCatalog.NoneDisplayName };
                    optionValues = new[] { string.Empty };
                    localIndex = 0;
                }

                int initialIndex = localIndex >= 0 ? localIndex : 0;
                EditorGUI.BeginChangeCheck();
                int selectedIndex = EditorGUI.Popup(
                    typeRect,
                    label.text,
                    initialIndex,
                    optionLabels
                );
                if (EditorGUI.EndChangeCheck())
                {
                    string selectedValue = optionValues[selectedIndex];
                    if (!string.Equals(selectedValue, currentValue, StringComparison.Ordinal))
                    {
                        nameProperty.stringValue = selectedValue;
                        state.LastValue = selectedValue;
                    }
                }

                GUI.enabled = !string.IsNullOrEmpty(currentValue);
                if (GUI.Button(clearRect, "Clear"))
                {
                    nameProperty.stringValue = string.Empty;
                    state.LastValue = string.Empty;
                    state.Page = 0;
                }
                GUI.enabled = true;

                EditorGUI.LabelField(pageInfoRect, $"Page {state.Page + 1}/{pageCount}");

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
            }
            finally
            {
                EditorGUI.indentLevel = originalIndent;
                EditorGUI.EndProperty();
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

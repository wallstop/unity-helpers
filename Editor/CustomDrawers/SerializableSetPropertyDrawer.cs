namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Text;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    [CustomPropertyDrawer(typeof(SerializableHashSet<>), true)]
    [CustomPropertyDrawer(typeof(SerializableSortedSet<>), true)]
    public sealed class SerializableSetPropertyDrawer : PropertyDrawer
    {
        private const float RowSpacing = 2f;
        private const float SectionSpacing = 4f;
        private const float ButtonSpacing = 4f;
        private const float RemoveButtonWidth = 20f;
        private const float PaginationButtonWidth = 28f;
        private const float ToolbarHeight = 18f;
        private const int DefaultPageSize = 15;
        internal const int MaxPageSize = 250;
        private const int MaxAutoAddAttempts = 256;

        private static readonly GUIContent AddEntryContent = new GUIContent("Add");
        private static readonly GUIContent ClearAllContent = new GUIContent("Clear All");
        private static readonly GUIContent SortContent = new GUIContent("Sort");
        private static readonly GUIContent PageSizeContent = new GUIContent("Page Size");
        private static readonly GUIContent FirstPageContent = new GUIContent("<<", "First Page");
        private static readonly GUIContent PreviousPageContent = new GUIContent(
            "<",
            "Previous Page"
        );
        private static readonly GUIContent NextPageContent = new GUIContent(">", "Next Page");
        private static readonly GUIContent LastPageContent = new GUIContent(">>", "Last Page");
        private static readonly Color DuplicateHighlightColor = new Color(1f, 0.78f, 0.65f, 0.4f);
        private static readonly object NullComparable = new object();

        private static readonly GUIStyle AddButtonStyle = CreateSolidButtonStyle(
            new Color(0.22f, 0.62f, 0.29f)
        );
        private static readonly GUIStyle ClearAllActiveButtonStyle = CreateSolidButtonStyle(
            new Color(0.82f, 0.27f, 0.27f)
        );
        private static readonly GUIStyle RemoveButtonStyle = CreateSolidButtonStyle(
            new Color(0.86f, 0.23f, 0.23f)
        );

        private readonly Dictionary<string, PaginationState> _paginationStates = new();
        private readonly Dictionary<string, DuplicateState> _duplicateStates = new();

        internal sealed class PaginationState
        {
            public int page;
            public int pageSize = DefaultPageSize;
        }

        internal sealed class DuplicateState
        {
            public bool HasDuplicates;
            public readonly HashSet<int> DuplicateIndices = new HashSet<int>();
            public string Summary = string.Empty;
        }

        private struct SetElementData
        {
            public SerializedPropertyType PropertyType;
            public object Comparable;
            public object Value;
        }

        private static GUIStyle CreateSolidButtonStyle(Color baseColor)
        {
            Color hoverColor = AdjustColorBrightness(baseColor, 0.12f);
            Color activeColor = AdjustColorBrightness(baseColor, -0.18f);

            GUIStyle style = new GUIStyle(EditorStyles.miniButton);
            Texture2D normalTexture = CreateSolidTexture(baseColor);
            Texture2D hoverTexture = CreateSolidTexture(hoverColor);
            Texture2D activeTexture = CreateSolidTexture(activeColor);

            style.normal.background = normalTexture;
            style.normal.textColor = Color.white;
            style.hover.background = hoverTexture;
            style.hover.textColor = Color.white;
            style.active.background = activeTexture;
            style.active.textColor = Color.white;

            style.onNormal.background = normalTexture;
            style.onNormal.textColor = Color.white;
            style.onHover.background = hoverTexture;
            style.onHover.textColor = Color.white;
            style.onActive.background = activeTexture;
            style.onActive.textColor = Color.white;

            style.focused.background = normalTexture;
            style.focused.textColor = Color.white;
            style.onFocused.background = normalTexture;
            style.onFocused.textColor = Color.white;

            return style;
        }

        private static Texture2D CreateSolidTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point,
            };
            Color opaque = new Color(color.r, color.g, color.b, 1f);
            texture.SetPixel(0, 0, opaque);
            texture.Apply();
            return texture;
        }

        private static Color AdjustColorBrightness(Color color, float amount)
        {
            if (amount > 0f)
            {
                return Color.Lerp(color, Color.white, Mathf.Clamp01(amount));
            }

            if (amount < 0f)
            {
                return Color.Lerp(color, Color.black, Mathf.Clamp01(-amount));
            }

            return color;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty itemsProperty = property.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            bool hasItemsArray = itemsProperty != null && itemsProperty.isArray;
            int totalCount = hasItemsArray ? itemsProperty.arraySize : 0;

            string propertyPath = property.propertyPath;
            object setInstanceForLabel = GetSetInstance(property, propertyPath);
            int uniqueCount =
                setInstanceForLabel != null ? CountSetElements(setInstanceForLabel) : totalCount;
            string foldoutLabel = BuildFoldoutLabel(label, uniqueCount);

            EditorGUI.BeginProperty(position, label, property);

            Rect foldoutRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight
            );

            property.isExpanded = EditorGUI.Foldout(
                foldoutRect,
                property.isExpanded,
                foldoutLabel,
                true
            );
            float y = foldoutRect.yMax + SectionSpacing;

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            PaginationState pagination = GetOrCreatePaginationState(property);
            EnsurePaginationBounds(pagination, totalCount);

            bool isSortedSet = IsSortedSet(property);
            DuplicateState duplicateState = EvaluateDuplicateState(property, itemsProperty);

            EditorGUI.indentLevel++;

            Rect toolbarRect = new Rect(position.x, y, position.width, ToolbarHeight);
            DrawToolbar(
                toolbarRect,
                ref property,
                propertyPath,
                ref itemsProperty,
                pagination,
                isSortedSet
            );
            y = toolbarRect.yMax + SectionSpacing;

            itemsProperty = property.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            hasItemsArray = itemsProperty != null && itemsProperty.isArray;
            totalCount = hasItemsArray ? itemsProperty.arraySize : 0;
            EnsurePaginationBounds(pagination, totalCount);
            duplicateState = EvaluateDuplicateState(property, itemsProperty);
            bool drawHelpBox =
                duplicateState.HasDuplicates && !string.IsNullOrEmpty(duplicateState.Summary);

            if (drawHelpBox)
            {
                float helpHeight = EditorGUIUtility.singleLineHeight * 1.6f;
                Rect helpRect = new Rect(position.x, y, position.width, helpHeight);
                EditorGUI.HelpBox(helpRect, duplicateState.Summary, MessageType.Warning);
                y = helpRect.yMax + SectionSpacing;
            }

            if (!hasItemsArray || totalCount == 0)
            {
                Rect emptyRect = new Rect(
                    position.x,
                    y,
                    position.width,
                    EditorGUIUtility.singleLineHeight
                );
                EditorGUI.LabelField(emptyRect, GUIContent.none, new GUIContent("Set is empty."));
                y = emptyRect.yMax + SectionSpacing;
            }
            else
            {
                int startIndex = pagination.page * pagination.pageSize;
                int endIndex = Mathf.Min(startIndex + pagination.pageSize, totalCount);

                for (int index = startIndex; index < endIndex; index++)
                {
                    SerializedProperty element = itemsProperty.GetArrayElementAtIndex(index);
                    float elementHeight = EditorGUI.GetPropertyHeight(
                        element,
                        GUIContent.none,
                        true
                    );

                    Rect rowRect = new Rect(position.x, y, position.width, elementHeight);
                    Rect indentedRect = EditorGUI.IndentedRect(rowRect);
                    Rect fieldRect = new Rect(
                        indentedRect.x,
                        rowRect.y,
                        indentedRect.width - RemoveButtonWidth - ButtonSpacing,
                        elementHeight
                    );
                    Rect removeRect = new Rect(
                        fieldRect.xMax + ButtonSpacing,
                        rowRect.y,
                        RemoveButtonWidth,
                        EditorGUIUtility.singleLineHeight
                    );

                    if (duplicateState.DuplicateIndices.Contains(index))
                    {
                        Rect highlightRect = new Rect(
                            rowRect.x,
                            rowRect.y,
                            rowRect.width,
                            elementHeight
                        );
                        EditorGUI.DrawRect(highlightRect, DuplicateHighlightColor);
                    }

                    EditorGUI.PropertyField(fieldRect, element, GUIContent.none, true);

                    using (new EditorGUI.DisabledScope(totalCount == 0))
                    {
                        GUIStyle removeButtonStyle =
                            totalCount > 0 ? RemoveButtonStyle : EditorStyles.miniButton;
                        if (GUI.Button(removeRect, "-", removeButtonStyle))
                        {
                            SetElementData elementData = ReadElementData(element);
                            RemoveEntry(itemsProperty, index);
                            RemoveValueFromSet(property, propertyPath, elementData.Value);
                            property.serializedObject.ApplyModifiedProperties();
                            property.serializedObject.Update();
                            property = property.serializedObject.FindProperty(propertyPath);
                            itemsProperty = property.FindPropertyRelative(
                                SerializableHashSetSerializedPropertyNames.Items
                            );
                            hasItemsArray = itemsProperty != null && itemsProperty.isArray;
                            totalCount = hasItemsArray ? itemsProperty.arraySize : 0;
                            EnsurePaginationBounds(pagination, totalCount);
                            EvaluateDuplicateState(property, itemsProperty, force: true);
                            EditorGUI.indentLevel--;
                            EditorGUI.EndProperty();
                            return;
                        }
                    }

                    y = fieldRect.yMax + RowSpacing;
                }
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded)
            {
                return height;
            }

            SerializedProperty itemsProperty = property.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            bool hasItemsArray = itemsProperty != null && itemsProperty.isArray;
            int totalCount = hasItemsArray ? itemsProperty.arraySize : 0;

            height += SectionSpacing + ToolbarHeight + SectionSpacing;

            PaginationState pagination = GetOrCreatePaginationState(property);
            EnsurePaginationBounds(pagination, totalCount);

            DuplicateState duplicateState = EvaluateDuplicateState(property, itemsProperty);
            if (duplicateState.HasDuplicates && !string.IsNullOrEmpty(duplicateState.Summary))
            {
                height += EditorGUIUtility.singleLineHeight * 1.6f + SectionSpacing;
            }

            if (!hasItemsArray || totalCount == 0)
            {
                height += EditorGUIUtility.singleLineHeight + SectionSpacing;
                return height;
            }

            int startIndex = pagination.page * pagination.pageSize;
            int endIndex = Mathf.Min(startIndex + pagination.pageSize, totalCount);

            for (int index = startIndex; index < endIndex; index++)
            {
                SerializedProperty element = itemsProperty.GetArrayElementAtIndex(index);
                float elementHeight = EditorGUI.GetPropertyHeight(element, GUIContent.none, true);
                height += elementHeight + RowSpacing;
            }

            return height;
        }

        internal PaginationState GetOrCreatePaginationState(SerializedProperty property)
        {
            string key = property.propertyPath;
            if (!_paginationStates.TryGetValue(key, out PaginationState state))
            {
                state = new PaginationState();
                _paginationStates[key] = state;
            }

            if (state.pageSize < UnityHelpersSettings.MinPageSize)
            {
                state.pageSize = UnityHelpersSettings.MinPageSize;
            }

            if (state.pageSize > MaxPageSize)
            {
                state.pageSize = MaxPageSize;
            }

            return state;
        }

        private void EnsurePaginationBounds(PaginationState state, int totalCount)
        {
            int pageSize = Mathf.Clamp(
                state.pageSize,
                UnityHelpersSettings.MinPageSize,
                MaxPageSize
            );

            state.pageSize = pageSize;

            int pageCount = pageSize > 0 ? Mathf.Max(1, (totalCount + pageSize - 1) / pageSize) : 1;

            if (state.page >= pageCount)
            {
                state.page = pageCount - 1;
            }

            if (state.page < 0)
            {
                state.page = 0;
            }
        }

        private void DrawToolbar(
            Rect rect,
            ref SerializedProperty property,
            string propertyPath,
            ref SerializedProperty itemsProperty,
            PaginationState pagination,
            bool isSortedSet
        )
        {
            SerializedObject serializedObject = property.serializedObject;
            int totalCount =
                itemsProperty != null && itemsProperty.isArray ? itemsProperty.arraySize : 0;

            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect addRect = new Rect(rect.x, rect.y, 60f, lineHeight);
            Rect clearRect = new Rect(addRect.xMax + ButtonSpacing, rect.y, 80f, lineHeight);
            float nextX = clearRect.xMax + ButtonSpacing;

            if (GUI.Button(addRect, AddEntryContent, AddButtonStyle))
            {
                if (TryAddNewElement(ref property, propertyPath, ref itemsProperty, pagination))
                {
                    serializedObject = property.serializedObject;
                    itemsProperty = property.FindPropertyRelative(
                        SerializableHashSetSerializedPropertyNames.Items
                    );
                    totalCount =
                        itemsProperty != null && itemsProperty.isArray
                            ? itemsProperty.arraySize
                            : 0;
                    EnsurePaginationBounds(pagination, totalCount);
                }
            }

            GUIStyle clearButtonStyle =
                totalCount > 0 ? ClearAllActiveButtonStyle : EditorStyles.miniButton;
            using (new EditorGUI.DisabledScope(totalCount == 0))
            {
                if (GUI.Button(clearRect, ClearAllContent, clearButtonStyle))
                {
                    if (TryClearSet(ref property, propertyPath, ref itemsProperty))
                    {
                        serializedObject = property.serializedObject;
                        itemsProperty = property.FindPropertyRelative(
                            SerializableHashSetSerializedPropertyNames.Items
                        );
                        totalCount =
                            itemsProperty != null && itemsProperty.isArray
                                ? itemsProperty.arraySize
                                : 0;
                        pagination.page = 0;
                        EnsurePaginationBounds(pagination, totalCount);
                    }
                }
            }

            Rect sortRect = Rect.zero;
            if (isSortedSet)
            {
                sortRect = new Rect(nextX, rect.y, 60f, lineHeight);
                using (new EditorGUI.DisabledScope(!CanSortElements(itemsProperty)))
                {
                    if (GUI.Button(sortRect, SortContent, EditorStyles.miniButton))
                    {
                        if (TrySortElements(ref property, propertyPath, itemsProperty))
                        {
                            serializedObject = property.serializedObject;
                            itemsProperty = property.FindPropertyRelative(
                                SerializableHashSetSerializedPropertyNames.Items
                            );
                            totalCount =
                                itemsProperty != null && itemsProperty.isArray
                                    ? itemsProperty.arraySize
                                    : 0;
                            EnsurePaginationBounds(pagination, totalCount);
                        }
                    }
                }
                nextX = sortRect.xMax + ButtonSpacing;
            }
            else
            {
                nextX = clearRect.xMax + ButtonSpacing;
            }

            Rect entriesRect = new Rect(nextX, rect.y, 100f, lineHeight);
            EditorGUI.LabelField(entriesRect, $"Entries: {totalCount}", EditorStyles.miniLabel);
            nextX = entriesRect.xMax + ButtonSpacing;

            float navigationWidth = PaginationButtonWidth * 4f + ButtonSpacing * 3f;
            float pageSizeWidth = Mathf.Max(
                170f,
                EditorStyles.miniLabel.CalcSize(PageSizeContent).x + 60f
            );
            float pageInfoWidth = totalCount > 0 ? 110f : 0f;

            Rect navigationRect = new Rect(
                rect.xMax - navigationWidth,
                rect.y,
                navigationWidth,
                lineHeight
            );

            Rect pageSizeRect = new Rect(
                navigationRect.x - ButtonSpacing - pageSizeWidth,
                rect.y,
                pageSizeWidth,
                lineHeight
            );

            Rect pageInfoRect =
                totalCount > 0
                    ? new Rect(
                        pageSizeRect.x - ButtonSpacing - pageInfoWidth,
                        rect.y,
                        pageInfoWidth,
                        lineHeight
                    )
                    : Rect.zero;

            if (totalCount > 0)
            {
                int pageSize = Mathf.Max(1, pagination.pageSize);
                int pageCount = Mathf.Max(1, (totalCount + pageSize - 1) / pageSize);
                int currentPage = Mathf.Clamp(pagination.page + 1, 1, pageCount);
                EditorGUI.LabelField(
                    pageInfoRect,
                    $"Page {currentPage}/{pageCount}",
                    EditorStyles.miniLabel
                );
                DrawPageSizeField(pageSizeRect, pagination, totalCount);
                DrawPaginationButtons(navigationRect, pagination, totalCount);
            }
            else
            {
                DrawPageSizeField(pageSizeRect, pagination, totalCount);
                using (new EditorGUI.DisabledScope(true))
                {
                    DrawPaginationButtons(navigationRect, pagination, totalCount);
                }
            }
        }

        private void DrawPageSizeField(Rect rect, PaginationState pagination, int totalCount)
        {
            float padding = 4f;
            float requiredLabelWidth = EditorStyles.miniLabel.CalcSize(PageSizeContent).x + padding;
            float labelWidth = Mathf.Min(requiredLabelWidth, rect.width * 0.6f);
            Rect labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
            float fieldWidth = Mathf.Max(0f, rect.width - labelWidth - padding);
            Rect fieldRect = new Rect(labelRect.xMax + padding, rect.y, fieldWidth, rect.height);

            EditorGUI.LabelField(labelRect, PageSizeContent, EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();
            int newSize = EditorGUI.IntField(fieldRect, GUIContent.none, pagination.pageSize);
            if (EditorGUI.EndChangeCheck())
            {
                pagination.pageSize = Mathf.Clamp(
                    newSize,
                    UnityHelpersSettings.MinPageSize,
                    MaxPageSize
                );
                pagination.page = 0;
            }
        }

        private void DrawPaginationButtons(Rect rect, PaginationState pagination, int totalCount)
        {
            int pageSize = pagination.pageSize;
            int pageCount = pageSize > 0 ? Mathf.Max(1, (totalCount + pageSize - 1) / pageSize) : 1;
            int currentPage = pagination.page;

            Rect firstRect = new Rect(rect.x, rect.y, PaginationButtonWidth, rect.height);
            Rect prevRect = new Rect(
                firstRect.xMax + ButtonSpacing,
                rect.y,
                PaginationButtonWidth,
                rect.height
            );
            Rect nextRect = new Rect(
                prevRect.xMax + ButtonSpacing,
                rect.y,
                PaginationButtonWidth,
                rect.height
            );
            Rect lastRect = new Rect(
                nextRect.xMax + ButtonSpacing,
                rect.y,
                PaginationButtonWidth,
                rect.height
            );

            using (new EditorGUI.DisabledScope(currentPage <= 0))
            {
                if (GUI.Button(firstRect, FirstPageContent, EditorStyles.miniButton))
                {
                    pagination.page = 0;
                }

                if (GUI.Button(prevRect, PreviousPageContent, EditorStyles.miniButton))
                {
                    pagination.page = Mathf.Max(0, pagination.page - 1);
                }
            }

            using (new EditorGUI.DisabledScope(currentPage >= pageCount - 1))
            {
                if (GUI.Button(nextRect, NextPageContent, EditorStyles.miniButton))
                {
                    pagination.page = Mathf.Min(pageCount - 1, pagination.page + 1);
                }

                if (GUI.Button(lastRect, LastPageContent, EditorStyles.miniButton))
                {
                    pagination.page = pageCount - 1;
                }
            }
        }

        private static void RemoveEntry(SerializedProperty itemsProperty, int index)
        {
            if (itemsProperty == null || !itemsProperty.isArray)
            {
                return;
            }

            if (index < 0 || index >= itemsProperty.arraySize)
            {
                return;
            }

            itemsProperty.DeleteArrayElementAtIndex(index);

            if (
                index < itemsProperty.arraySize
                && itemsProperty.GetArrayElementAtIndex(index).propertyType
                    == SerializedPropertyType.ObjectReference
                && itemsProperty.GetArrayElementAtIndex(index).objectReferenceValue == null
            )
            {
                itemsProperty.DeleteArrayElementAtIndex(index);
            }
        }

        internal DuplicateState EvaluateDuplicateState(
            SerializedProperty property,
            SerializedProperty itemsProperty,
            bool force = false
        )
        {
            string key = property.propertyPath;
            if (!_duplicateStates.TryGetValue(key, out DuplicateState state))
            {
                state = new DuplicateState();
                _duplicateStates[key] = state;
            }

            state.DuplicateIndices.Clear();
            state.HasDuplicates = false;
            state.Summary = string.Empty;

            if (itemsProperty == null || !itemsProperty.isArray || itemsProperty.arraySize <= 1)
            {
                return state;
            }

            Dictionary<object, List<int>> duplicates = new Dictionary<object, List<int>>();
            int count = itemsProperty.arraySize;

            for (int index = 0; index < count; index++)
            {
                SerializedProperty element = itemsProperty.GetArrayElementAtIndex(index);
                SetElementData data = ReadElementData(element);
                object keyValue = data.Comparable ?? NullComparable;

                if (!duplicates.TryGetValue(keyValue, out List<int> indices))
                {
                    indices = new List<int>();
                    duplicates[keyValue] = indices;
                }

                indices.Add(index);
            }

            StringBuilder summaryBuilder = new StringBuilder();
            int duplicateGroupCount = 0;

            foreach (KeyValuePair<object, List<int>> pair in duplicates)
            {
                if (pair.Value.Count <= 1)
                {
                    continue;
                }

                duplicateGroupCount++;
                state.HasDuplicates = true;
                foreach (int duplicateIndex in pair.Value)
                {
                    state.DuplicateIndices.Add(duplicateIndex);
                }

                if (summaryBuilder.Length > 0)
                {
                    summaryBuilder.AppendLine();
                }

                if (duplicateGroupCount <= 5)
                {
                    summaryBuilder.Append("Value ");
                    summaryBuilder.Append(ConvertDuplicateKeyToString(pair.Key));
                    summaryBuilder.Append(" at indices ");
                    AppendIndexList(summaryBuilder, pair.Value);
                }
            }

            if (duplicateGroupCount > 5)
            {
                summaryBuilder.AppendLine();
                summaryBuilder.Append("Additional duplicate groups omitted for brevity.");
            }

            if (state.HasDuplicates)
            {
                state.Summary =
                    summaryBuilder.Length > 0
                        ? summaryBuilder.ToString()
                        : "Duplicate values detected.";
            }

            return state;
        }

        private static string ConvertDuplicateKeyToString(object key)
        {
            if (key == NullComparable || key == null)
            {
                return "null";
            }

            return key switch
            {
                UnityEngine.Object obj => obj != null ? obj.name : "null object",
                _ => key.ToString() ?? "null",
            };
        }

        private static void AppendIndexList(StringBuilder builder, List<int> indices)
        {
            for (int i = 0; i < indices.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(indices[i]);
            }
        }

        private static bool IsSortedSet(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            if (property.type.IndexOf("SerializableSortedSet", StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            Type fieldType = property.GetManagedType();
            return fieldType != null
                && TypeMatchesGenericDefinition(fieldType, typeof(SerializableSortedSet<>));
        }

        private static bool TypeMatchesGenericDefinition(Type candidate, Type openGeneric)
        {
            if (candidate == null)
            {
                return false;
            }

            if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == openGeneric)
            {
                return true;
            }

            if (candidate.BaseType != null)
            {
                return TypeMatchesGenericDefinition(candidate.BaseType, openGeneric);
            }

            return false;
        }

        private static Type GetElementType(SerializedProperty property)
        {
            Type managedType = property.GetManagedType();
            if (managedType == null)
            {
                return null;
            }

            Type current = managedType;
            while (current != null)
            {
                if (current.IsGenericType)
                {
                    Type definition = current.GetGenericTypeDefinition();
                    if (
                        definition == typeof(SerializableHashSet<>)
                        || definition == typeof(SerializableSortedSet<>)
                    )
                    {
                        Type[] arguments = current.GetGenericArguments();
                        if (arguments.Length == 1)
                        {
                            return arguments[0];
                        }
                    }
                }

                current = current.BaseType;
            }

            return null;
        }

        private static bool CanSortElements(SerializedProperty itemsProperty)
        {
            if (itemsProperty == null || !itemsProperty.isArray || itemsProperty.arraySize <= 1)
            {
                return false;
            }

            SerializedProperty element = itemsProperty.GetArrayElementAtIndex(0);
            return SupportsSorting(element.propertyType);
        }

        private static bool SupportsSorting(SerializedPropertyType propertyType)
        {
            switch (propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.String:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Color:
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Rect:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.Quaternion:
                case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.RectInt:
                case SerializedPropertyType.Bounds:
                case SerializedPropertyType.BoundsInt:
                case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.Hash128:
                    return true;
                default:
                    return false;
            }
        }

        internal bool TryAddNewElement(
            ref SerializedProperty property,
            string propertyPath,
            ref SerializedProperty itemsProperty,
            PaginationState pagination
        )
        {
            object setInstance = GetSetInstance(property, propertyPath);
            if (setInstance == null)
            {
                return false;
            }

            Type setType = setInstance.GetType();
            Type elementType = GetElementType(property);

            if (elementType == null)
            {
                return false;
            }

            MethodInfo addMethod = setType.GetMethod("Add", new[] { elementType });
            MethodInfo containsMethod = setType.GetMethod("Contains", new[] { elementType });

            if (addMethod == null || containsMethod == null)
            {
                return false;
            }

            FieldInfo itemsField = setType.GetField(
                "_items",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            FieldInfo preserveField = setType.BaseType?.GetField(
                "_preserveSerializedEntries",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Array existingItems = itemsField?.GetValue(setInstance) as Array;
            int existingSerializedCount = existingItems?.Length ?? 0;
            int serializedArrayCount =
                itemsProperty != null && itemsProperty.isArray
                    ? itemsProperty.arraySize
                    : existingSerializedCount;
            int currentUniqueCount = CountSetElements(setInstance);
            bool hasSerializedDuplicates = serializedArrayCount > currentUniqueCount;

            foreach (object candidate in GenerateCandidateValues(elementType, setInstance))
            {
                object boxed = candidate;
                bool alreadyExists = (bool)
                    containsMethod.Invoke(setInstance, new object[] { boxed });
                if (alreadyExists)
                {
                    continue;
                }

                bool added = (bool)addMethod.Invoke(setInstance, new object[] { boxed });
                if (!added)
                {
                    continue;
                }

                SerializedObject serializedObject = property.serializedObject;

                if (hasSerializedDuplicates && itemsField != null)
                {
                    int copyCount = Math.Max(serializedArrayCount, existingSerializedCount);
                    Array expanded = Array.CreateInstance(elementType, copyCount + 1);

                    if (itemsProperty != null && itemsProperty.isArray)
                    {
                        int propertyCount = itemsProperty.arraySize;
                        for (int index = 0; index < propertyCount; index++)
                        {
                            SerializedProperty elementProperty =
                                itemsProperty.GetArrayElementAtIndex(index);
                            SetElementData elementData = ReadElementData(elementProperty);
                            expanded.SetValue(
                                ConvertValueForElementType(elementType, elementData.Value),
                                index
                            );
                        }

                        if (existingItems != null && existingSerializedCount > propertyCount)
                        {
                            Array.Copy(
                                existingItems,
                                propertyCount,
                                expanded,
                                propertyCount,
                                existingSerializedCount - propertyCount
                            );
                        }
                    }
                    else if (existingItems != null)
                    {
                        Array.Copy(existingItems, expanded, existingSerializedCount);
                    }

                    expanded.SetValue(ConvertValueForElementType(elementType, boxed), copyCount);
                    itemsField.SetValue(setInstance, expanded);
                    if (preserveField != null)
                    {
                        preserveField.SetValue(setInstance, true);
                    }

                    serializedObject.Update();
                    property = serializedObject.FindProperty(propertyPath);
                    itemsProperty = property?.FindPropertyRelative(
                        SerializableHashSetSerializedPropertyNames.Items
                    );
                    int totalCount =
                        itemsProperty != null && itemsProperty.isArray
                            ? itemsProperty.arraySize
                            : copyCount + 1;
                    EnsurePaginationBounds(pagination, totalCount);
                    EvaluateDuplicateState(property, itemsProperty, force: true);
                    return true;
                }

                InvokeOnBeforeSerialize(setType, setInstance);
                serializedObject.Update();
                property = serializedObject.FindProperty(propertyPath);
                itemsProperty = property?.FindPropertyRelative(
                    SerializableHashSetSerializedPropertyNames.Items
                );
                int refreshedCount =
                    itemsProperty != null && itemsProperty.isArray ? itemsProperty.arraySize : 0;
                EnsurePaginationBounds(pagination, refreshedCount);
                return true;
            }

            Debug.LogWarning("Unable to generate a unique value for this set element type.");
            return false;
        }

        private bool TryClearSet(
            ref SerializedProperty property,
            string propertyPath,
            ref SerializedProperty itemsProperty
        )
        {
            object setInstance = GetSetInstance(property, propertyPath);
            if (setInstance == null)
            {
                return false;
            }

            Type setType = setInstance.GetType();
            MethodInfo clearMethod = setType.GetMethod("Clear", Type.EmptyTypes);
            if (clearMethod == null)
            {
                return false;
            }

            clearMethod.Invoke(setInstance, null);
            InvokeOnBeforeSerialize(setType, setInstance);
            property.serializedObject.Update();
            property = property.serializedObject.FindProperty(propertyPath);
            itemsProperty = property?.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            return true;
        }

        private bool TrySortElements(
            ref SerializedProperty property,
            string propertyPath,
            SerializedProperty itemsProperty
        )
        {
            if (itemsProperty == null || !itemsProperty.isArray || itemsProperty.arraySize <= 1)
            {
                return false;
            }

            int count = itemsProperty.arraySize;
            List<SetElementData> elements = new List<SetElementData>(count);

            for (int index = 0; index < count; index++)
            {
                SerializedProperty elementProperty = itemsProperty.GetArrayElementAtIndex(index);
                elements.Add(ReadElementData(elementProperty));
            }

            elements.Sort(
                (left, right) =>
                {
                    int comparison = CompareComparableValues(left.Comparable, right.Comparable);
                    if (comparison != 0)
                    {
                        return comparison;
                    }

                    string leftFallback = left.Value != null ? left.Value.ToString() : string.Empty;
                    string rightFallback =
                        right.Value != null ? right.Value.ToString() : string.Empty;
                    return string.CompareOrdinal(leftFallback, rightFallback);
                }
            );

            for (int index = 0; index < count; index++)
            {
                SerializedProperty elementProperty = itemsProperty.GetArrayElementAtIndex(index);
                WriteElementValue(elementProperty, elements[index]);
            }

            object setInstance = GetSetInstance(property, propertyPath);
            Type elementType = GetElementType(property);
            if (setInstance != null && elementType != null)
            {
                Type setType = setInstance.GetType();
                MethodInfo clearMethod = setType.GetMethod("Clear", Type.EmptyTypes);
                MethodInfo addMethod = setType.GetMethod("Add", new[] { elementType });
                if (clearMethod != null && addMethod != null)
                {
                    clearMethod.Invoke(setInstance, null);
                    foreach (SetElementData element in elements)
                    {
                        addMethod.Invoke(setInstance, new[] { element.Value });
                    }
                }

                InvokeOnBeforeSerialize(setType, setInstance);
            }

            property.serializedObject.Update();
            property = property.serializedObject.FindProperty(propertyPath);
            return true;
        }

        private void RemoveValueFromSet(
            SerializedProperty property,
            string propertyPath,
            object value
        )
        {
            object setInstance = GetSetInstance(property, propertyPath);
            if (setInstance == null)
            {
                return;
            }

            Type setType = setInstance.GetType();
            Type elementType = GetElementType(property);
            if (elementType == null)
            {
                return;
            }

            MethodInfo removeMethod = setType.GetMethod("Remove", new[] { elementType });
            if (removeMethod == null)
            {
                return;
            }

            removeMethod.Invoke(setInstance, new[] { value });
            InvokeOnBeforeSerialize(setType, setInstance);
        }

        private static object GetSetInstance(SerializedProperty property, string propertyPath)
        {
            return GetTargetObjectOfProperty(property.serializedObject.targetObject, propertyPath);
        }

        private static void InvokeOnBeforeSerialize(Type setType, object setInstance)
        {
            MethodInfo method = setType.GetMethod(
                "OnBeforeSerialize",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            method?.Invoke(setInstance, null);
        }

        private static IEnumerable<object> GenerateCandidateValues(
            Type elementType,
            object setInstance
        )
        {
            int existingCount = CountSetElements(setInstance);

            if (elementType == typeof(string))
            {
                yield return "New Entry";
                for (int i = 1; i < MaxAutoAddAttempts; i++)
                {
                    yield return $"New Entry ({i})";
                }
                yield break;
            }

            if (IsSignedIntegral(elementType))
            {
                for (long i = 0; i < MaxAutoAddAttempts; i++)
                {
                    yield return Convert.ChangeType(i, elementType);
                    if (i > 0)
                    {
                        yield return Convert.ChangeType(-i, elementType);
                    }
                }
                yield break;
            }

            if (IsUnsignedIntegral(elementType))
            {
                for (long i = 0; i < MaxAutoAddAttempts; i++)
                {
                    yield return Convert.ChangeType(i, elementType);
                }
                yield break;
            }

            if (
                elementType == typeof(float)
                || elementType == typeof(double)
                || elementType == typeof(decimal)
            )
            {
                for (int i = 0; i < MaxAutoAddAttempts; i++)
                {
                    yield return Convert.ChangeType(i, elementType);
                }
                yield break;
            }

            if (elementType == typeof(bool))
            {
                yield return false;
                yield return true;
                yield break;
            }

            if (elementType == typeof(char))
            {
                for (int i = 0; i < MaxAutoAddAttempts; i++)
                {
                    yield return (char)('A' + (i % 26));
                }
                yield break;
            }

            if (elementType.IsEnum)
            {
                foreach (object value in Enum.GetValues(elementType))
                {
                    yield return value;
                }
                yield break;
            }

            if (elementType == typeof(Vector2))
            {
                for (int i = 0; i < MaxAutoAddAttempts; i++)
                {
                    yield return new Vector2(i + 1f, 0f);
                }
                yield break;
            }

            if (elementType == typeof(Vector3))
            {
                for (int i = 0; i < MaxAutoAddAttempts; i++)
                {
                    yield return new Vector3(i + 1f, 0f, 0f);
                }
                yield break;
            }

            if (elementType == typeof(Vector4))
            {
                for (int i = 0; i < MaxAutoAddAttempts; i++)
                {
                    yield return new Vector4(i + 1f, 0f, 0f, 0f);
                }
                yield break;
            }

            if (elementType == typeof(Vector2Int))
            {
                for (int i = 0; i < MaxAutoAddAttempts; i++)
                {
                    yield return new Vector2Int(i + 1, 0);
                }
                yield break;
            }

            if (elementType == typeof(Vector3Int))
            {
                for (int i = 0; i < MaxAutoAddAttempts; i++)
                {
                    yield return new Vector3Int(i + 1, 0, 0);
                }
                yield break;
            }

            if (elementType == typeof(Rect))
            {
                for (int i = 0; i < MaxAutoAddAttempts; i++)
                {
                    yield return new Rect(i + 1f, 0f, 1f, 1f);
                }
                yield break;
            }

            if (elementType == typeof(RectInt))
            {
                for (int i = 0; i < MaxAutoAddAttempts; i++)
                {
                    yield return new RectInt(i + 1, 0, 1, 1);
                }
                yield break;
            }

            if (elementType == typeof(Bounds))
            {
                for (int i = 0; i < MaxAutoAddAttempts; i++)
                {
                    Bounds bounds = new Bounds(new Vector3(i + 1f, 0f, 0f), Vector3.one);
                    yield return bounds;
                }
                yield break;
            }

            if (elementType == typeof(BoundsInt))
            {
                for (int i = 0; i < MaxAutoAddAttempts; i++)
                {
                    BoundsInt bounds = new BoundsInt(new Vector3Int(i + 1, 0, 0), Vector3Int.one);
                    yield return bounds;
                }
                yield break;
            }

            if (elementType == typeof(Color))
            {
                for (int i = 0; i < MaxAutoAddAttempts; i++)
                {
                    float hue =
                        (existingCount + i) % MaxAutoAddAttempts / (float)MaxAutoAddAttempts;
                    yield return Color.HSVToRGB(hue, 0.8f, 1f);
                }
                yield break;
            }

            if (elementType == typeof(Quaternion))
            {
                for (int i = 0; i < MaxAutoAddAttempts; i++)
                {
                    yield return Quaternion.Euler((existingCount + i) * 10f, 0f, 0f);
                }
                yield break;
            }

            if (elementType == typeof(Hash128))
            {
                for (uint i = 1; i <= MaxAutoAddAttempts; i++)
                {
                    yield return new Hash128(i, 0u, 0u, 0u);
                }
                yield break;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(elementType))
            {
                yield return null;
                yield break;
            }

            if (!elementType.IsAbstract && elementType.GetConstructor(Type.EmptyTypes) != null)
            {
                for (int i = 0; i < MaxAutoAddAttempts; i++)
                {
                    yield return Activator.CreateInstance(elementType);
                }
                yield break;
            }

            if (elementType.IsValueType)
            {
                yield return Activator.CreateInstance(elementType);
            }
            else
            {
                yield return null;
            }
        }

        private static bool IsSignedIntegral(Type type)
        {
            return type == typeof(int)
                || type == typeof(long)
                || type == typeof(short)
                || type == typeof(sbyte);
        }

        private static bool IsUnsignedIntegral(Type type)
        {
            return type == typeof(uint)
                || type == typeof(ulong)
                || type == typeof(ushort)
                || type == typeof(byte);
        }

        private static int CountSetElements(object setInstance)
        {
            if (setInstance is not IEnumerable enumerable)
            {
                return 0;
            }

            int count = 0;
            foreach (object _ in enumerable)
            {
                count++;
            }

            return count;
        }

        private static object ConvertValueForElementType(Type elementType, object value)
        {
            if (elementType == null)
            {
                return value;
            }

            if (value == null)
            {
                return elementType.IsValueType ? Activator.CreateInstance(elementType) : null;
            }

            if (elementType.IsInstanceOfType(value))
            {
                return value;
            }

            try
            {
                if (elementType.IsEnum)
                {
                    if (value is string stringValue)
                    {
                        return Enum.Parse(elementType, stringValue);
                    }

                    return Enum.ToObject(elementType, value);
                }

                if (value is IConvertible)
                {
                    return Convert.ChangeType(value, elementType, CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                // Fallback handled below.
            }

            return elementType.IsValueType ? Activator.CreateInstance(elementType) : null;
        }

        private static object GetTargetObjectOfProperty(object target, string propertyPath)
        {
            if (target == null || string.IsNullOrEmpty(propertyPath))
            {
                return null;
            }

            object currentTarget = target;
            string[] elements = propertyPath.Replace(".Array.data[", "[").Split('.');
            foreach (string element in elements)
            {
                if (currentTarget == null)
                {
                    return null;
                }

                if (element.Contains("["))
                {
                    int leftBracket = element.IndexOf('[');
                    string elementName = element.Substring(0, leftBracket);
                    string indexPart = element
                        .Substring(leftBracket)
                        .Replace("[", string.Empty)
                        .Replace("]", string.Empty);
                    if (!int.TryParse(indexPart, out int index))
                    {
                        return null;
                    }

                    currentTarget = GetIndexedValue(currentTarget, elementName, index);
                }
                else
                {
                    currentTarget = GetMemberValue(currentTarget, element);
                }
            }

            return currentTarget;
        }

        private static object GetMemberValue(object source, string name)
        {
            if (source == null)
            {
                return null;
            }

            Type type = source.GetType();

            FieldInfo field = type.GetField(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (field != null)
            {
                return field.GetValue(source);
            }

            PropertyInfo property = type.GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            return property?.GetValue(source);
        }

        private static object GetIndexedValue(object source, string name, int index)
        {
            object collection = GetMemberValue(source, name);
            if (collection is not IEnumerable enumerable)
            {
                return null;
            }

            IEnumerator enumerator = enumerable.GetEnumerator();
            for (int i = 0; i <= index; i++)
            {
                if (!enumerator.MoveNext())
                {
                    return null;
                }
            }

            return enumerator.Current;
        }

        internal void SortElements(SerializedProperty property, SerializedProperty itemsProperty)
        {
            TrySortElements(ref property, property.propertyPath, itemsProperty);
        }

        private static int CompareComparableValues(object left, object right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null || left == NullComparable)
            {
                return right == null || right == NullComparable ? 0 : -1;
            }

            if (right == null || right == NullComparable)
            {
                return 1;
            }

            if (left is IComparable comparable)
            {
                return comparable.CompareTo(right);
            }

            if (right is IComparable comparableRight)
            {
                return -comparableRight.CompareTo(left);
            }

            string leftString = left.ToString() ?? string.Empty;
            string rightString = right.ToString() ?? string.Empty;
            return string.CompareOrdinal(leftString, rightString);
        }

        private static SetElementData ReadElementData(SerializedProperty property)
        {
            SetElementData data = new SetElementData
            {
                PropertyType = property.propertyType,
                Comparable = null,
                Value = null,
            };

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.ArraySize:
                    long longValue = property.longValue;
                    data.Value = longValue;
                    data.Comparable = longValue;
                    break;
                case SerializedPropertyType.Boolean:
                    bool boolValue = property.boolValue;
                    data.Value = boolValue;
                    data.Comparable = boolValue ? 1 : 0;
                    break;
                case SerializedPropertyType.Float:
                    double doubleValue = property.doubleValue;
                    data.Value = doubleValue;
                    data.Comparable = doubleValue;
                    break;
                case SerializedPropertyType.String:
                    string stringValue = property.stringValue ?? string.Empty;
                    data.Value = stringValue;
                    data.Comparable = stringValue;
                    break;
                case SerializedPropertyType.Color:
                    Color colorValue = property.colorValue;
                    data.Value = colorValue;
                    data.Comparable = colorValue;
                    break;
                case SerializedPropertyType.Vector2:
                    Vector2 vector2Value = property.vector2Value;
                    data.Value = vector2Value;
                    data.Comparable = vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    Vector3 vector3Value = property.vector3Value;
                    data.Value = vector3Value;
                    data.Comparable = vector3Value;
                    break;
                case SerializedPropertyType.Vector4:
                    Vector4 vector4Value = property.vector4Value;
                    data.Value = vector4Value;
                    data.Comparable = vector4Value;
                    break;
                case SerializedPropertyType.Rect:
                    Rect rectValue = property.rectValue;
                    data.Value = rectValue;
                    data.Comparable = rectValue;
                    break;
                case SerializedPropertyType.Bounds:
                    Bounds boundsValue = property.boundsValue;
                    data.Value = boundsValue;
                    data.Comparable = boundsValue;
                    break;
                case SerializedPropertyType.Vector2Int:
                    Vector2Int vector2IntValue = property.vector2IntValue;
                    data.Value = vector2IntValue;
                    data.Comparable = vector2IntValue;
                    break;
                case SerializedPropertyType.Vector3Int:
                    Vector3Int vector3IntValue = property.vector3IntValue;
                    data.Value = vector3IntValue;
                    data.Comparable = vector3IntValue;
                    break;
                case SerializedPropertyType.RectInt:
                    RectInt rectIntValue = property.rectIntValue;
                    data.Value = rectIntValue;
                    data.Comparable = rectIntValue;
                    break;
                case SerializedPropertyType.BoundsInt:
                    BoundsInt boundsIntValue = property.boundsIntValue;
                    data.Value = boundsIntValue;
                    data.Comparable = boundsIntValue;
                    break;
                case SerializedPropertyType.Hash128:
                    Hash128 hashValue = property.hash128Value;
                    data.Value = hashValue;
                    data.Comparable = hashValue;
                    break;
                case SerializedPropertyType.Quaternion:
                    Quaternion quaternionValue = property.quaternionValue;
                    data.Value = quaternionValue;
                    data.Comparable = quaternionValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    UnityEngine.Object objectReferenceValue = property.objectReferenceValue;
                    data.Value = objectReferenceValue;
                    data.Comparable =
                        objectReferenceValue != null
                            ? objectReferenceValue.GetInstanceID()
                            : NullComparable;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    AnimationCurve curveValue = property.animationCurveValue;
                    data.Value = curveValue;
                    data.Comparable = curveValue != null ? curveValue.length : 0;
                    break;
                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.Generic:
                    try
                    {
                        object boxed = property.boxedValue;
                        data.Value = boxed;
                        data.Comparable = boxed ?? NullComparable;
                    }
                    catch (Exception)
                    {
                        data.Value = property.propertyPath;
                        data.Comparable = property.propertyPath;
                    }
                    break;
                default:
                    data.Value = property.propertyPath;
                    data.Comparable = property.propertyPath;
                    break;
            }

            return data;
        }

        private static void WriteElementValue(SerializedProperty property, SetElementData data)
        {
            switch (data.PropertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.ArraySize:
                    property.longValue = Convert.ToInt64(data.Value);
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = Convert.ToBoolean(data.Value);
                    break;
                case SerializedPropertyType.Float:
                    property.doubleValue = Convert.ToDouble(data.Value);
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = data.Value as string ?? string.Empty;
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = data.Value is Color color ? color : Color.white;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = data.Value is Vector2 vector2 ? vector2 : Vector2.zero;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = data.Value is Vector3 vector3 ? vector3 : Vector3.zero;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = data.Value is Vector4 vector4 ? vector4 : Vector4.zero;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = data.Value is Rect rect ? rect : default;
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = data.Value is Bounds bounds ? bounds : default;
                    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = data.Value is Vector2Int vector2Int
                        ? vector2Int
                        : default;
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = data.Value is Vector3Int vector3Int
                        ? vector3Int
                        : default;
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = data.Value is RectInt rectInt ? rectInt : default;
                    break;
                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = data.Value is BoundsInt boundsInt
                        ? boundsInt
                        : default;
                    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = data.Value is Quaternion quaternion
                        ? quaternion
                        : Quaternion.identity;
                    break;
                case SerializedPropertyType.Hash128:
                    property.hash128Value = data.Value is Hash128 hash ? hash : default;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = data.Value as UnityEngine.Object;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = data.Value as AnimationCurve;
                    break;
                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.Generic:
                    try
                    {
                        property.boxedValue = data.Value;
                    }
                    catch (Exception) { }
                    break;
            }
        }

        private static string BuildFoldoutLabel(GUIContent label, int count)
        {
            string baseLabel = label != null ? label.text : "Serialized HashSet";
            return $"{baseLabel} ({count})";
        }
    }

    internal static class SerializableSetPropertyDrawerExtensions
    {
        public static Type GetManagedType(this SerializedProperty property)
        {
            if (property == null)
            {
                return null;
            }

            Type type = property.serializedObject.targetObject.GetType();
            string[] path = property.propertyPath.Split('.');
            return ResolveType(type, path, 0);
        }

        private static Type ResolveType(Type rootType, string[] path, int index)
        {
            if (rootType == null || path == null || index >= path.Length)
            {
                return rootType;
            }

            string segment = path[index];
            FieldInfo field = rootType.GetField(
                segment,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (field == null)
            {
                return rootType;
            }

            Type fieldType = field.FieldType;

            if (
                segment == "Array"
                && index + 1 < path.Length
                && path[index + 1].StartsWith("data[", StringComparison.Ordinal)
            )
            {
                if (fieldType.IsArray)
                {
                    return ResolveType(fieldType.GetElementType(), path, index + 2);
                }

                if (fieldType.IsGenericType)
                {
                    Type[] arguments = fieldType.GetGenericArguments();
                    if (arguments.Length == 1)
                    {
                        return ResolveType(arguments[0], path, index + 2);
                    }
                }

                return fieldType;
            }

            return ResolveType(fieldType, path, index + 1);
        }
    }
}

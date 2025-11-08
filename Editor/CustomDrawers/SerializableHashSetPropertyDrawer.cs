namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    [CustomPropertyDrawer(typeof(SerializableHashSet<>), true)]
    [CustomPropertyDrawer(typeof(SerializableSortedSet<>), true)]
    public sealed class SerializableHashSetPropertyDrawer : PropertyDrawer
    {
        private const float RowSpacing = 2f;
        private const float SectionSpacing = 4f;
        private const float ButtonSpacing = 4f;
        private const float RemoveButtonWidth = 20f;
        private const float PaginationButtonWidth = 24f;
        private const float ToolbarHeight = 18f;
        private const int DefaultPageSize = 15;
        internal const int MaxPageSize = 250;

        private static readonly GUIContent ClearAllContent = new GUIContent("Clear All");
        private static readonly GUIContent SortContent = new GUIContent("Sort");
        private static readonly GUIContent PageSizeContent = new GUIContent("Size");
        private static readonly GUIContent FirstPageContent = new GUIContent("<<", "First Page");
        private static readonly GUIContent PreviousPageContent = new GUIContent(
            "<",
            "Previous Page"
        );
        private static readonly GUIContent NextPageContent = new GUIContent(">", "Next Page");
        private static readonly GUIContent LastPageContent = new GUIContent(">>", "Last Page");
        private static readonly Color DuplicateHighlightColor = new Color(1f, 0.78f, 0.65f, 0.4f);
        private static readonly object NullComparable = new object();

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

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty itemsProperty = property.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            bool hasItemsArray = itemsProperty != null && itemsProperty.isArray;
            int totalCount = hasItemsArray ? itemsProperty.arraySize : 0;

            string foldoutLabel = BuildFoldoutLabel(label, totalCount);

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
            bool drawHelpBox =
                duplicateState.HasDuplicates && !string.IsNullOrEmpty(duplicateState.Summary);

            EditorGUI.indentLevel++;

            Rect toolbarRect = new Rect(position.x, y, position.width, ToolbarHeight);
            DrawToolbar(toolbarRect, property, itemsProperty, pagination, isSortedSet);
            y = toolbarRect.yMax + SectionSpacing;

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
                        if (GUI.Button(removeRect, "-", EditorStyles.miniButton))
                        {
                            RemoveEntry(itemsProperty, index);
                            property.serializedObject.ApplyModifiedProperties();
                            property.serializedObject.Update();
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
            SerializedProperty property,
            SerializedProperty itemsProperty,
            PaginationState pagination,
            bool isSortedSet
        )
        {
            SerializedObject serializedObject = property.serializedObject;
            int totalCount =
                itemsProperty != null && itemsProperty.isArray ? itemsProperty.arraySize : 0;

            Rect clearRect = new Rect(rect.x, rect.y, 80f, EditorGUIUtility.singleLineHeight);
            Rect sortRect = new Rect(
                clearRect.xMax + ButtonSpacing,
                rect.y,
                60f,
                EditorGUIUtility.singleLineHeight
            );

            using (new EditorGUI.DisabledScope(totalCount == 0))
            {
                if (GUI.Button(clearRect, ClearAllContent, EditorStyles.miniButton))
                {
                    if (itemsProperty != null)
                    {
                        itemsProperty.ClearArray();
                    }

                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();
                    EvaluateDuplicateState(property, itemsProperty, force: true);
                    pagination.page = 0;
                    return;
                }
            }

            if (isSortedSet)
            {
                using (new EditorGUI.DisabledScope(!CanSortElements(itemsProperty)))
                {
                    if (GUI.Button(sortRect, SortContent, EditorStyles.miniButton))
                    {
                        SortElements(property, itemsProperty);
                        serializedObject.ApplyModifiedProperties();
                        serializedObject.Update();
                        EvaluateDuplicateState(property, itemsProperty, force: true);
                    }
                }
            }

            float navigationWidth = PaginationButtonWidth * 4f + ButtonSpacing * 3f;
            float pageSizeWidth = 110f;
            float controlsRight = rect.xMax;

            Rect navigationRect = new Rect(
                controlsRight - navigationWidth,
                rect.y,
                navigationWidth,
                EditorGUIUtility.singleLineHeight
            );

            Rect pageSizeRect = new Rect(
                navigationRect.x - ButtonSpacing - pageSizeWidth,
                rect.y,
                pageSizeWidth,
                EditorGUIUtility.singleLineHeight
            );

            DrawPageSizeField(pageSizeRect, pagination);
            DrawPaginationButtons(navigationRect, pagination, totalCount);
        }

        private void DrawPageSizeField(Rect rect, PaginationState pagination)
        {
            float labelWidth = 32f;
            Rect labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
            Rect fieldRect = new Rect(
                rect.x + labelWidth + 2f,
                rect.y,
                rect.width - labelWidth - 2f,
                rect.height
            );

            EditorGUI.LabelField(labelRect, PageSizeContent, EditorStyles.miniLabel);

            int newSize = EditorGUI.IntField(fieldRect, pagination.pageSize);
            if (newSize != pagination.pageSize)
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

            if (property.type.IndexOf("SerializableSortedHashSet", StringComparison.Ordinal) >= 0)
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

        internal void SortElements(SerializedProperty property, SerializedProperty itemsProperty)
        {
            if (itemsProperty == null || !itemsProperty.isArray || itemsProperty.arraySize <= 1)
            {
                return;
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

            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
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

    internal static class SerializableHashSetPropertyDrawerExtensions
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

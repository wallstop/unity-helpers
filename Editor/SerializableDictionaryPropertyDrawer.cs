namespace WallstopStudios.UnityHelpers.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    [CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
    public sealed class SerializableDictionaryPropertyDrawer : PropertyDrawer
    {
        private readonly Dictionary<string, ReorderableList> _lists = new();
        private readonly Dictionary<string, PendingEntry> _pendingEntries = new();
        private readonly Dictionary<string, PaginationState> _paginationStates = new();

        private const float PendingSectionPadding = 6f;
        private const float PendingClearButtonWidth = 80f;
        private const float PendingAddButtonWidth = 110f;
        private const int DefaultPageSize = 15;
        private const float PaginationButtonWidth = 24f;
        private const float PaginationLabelWidth = 80f;
        private const float PaginationControlSpacing = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedObject serializedObject = property.serializedObject;
            serializedObject.UpdateIfRequiredOrScript();

            SerializedProperty keysProperty = property.FindPropertyRelative("_keys");
            SerializedProperty valuesProperty = property.FindPropertyRelative("_values");
            EnsureParallelArraySizes(keysProperty, valuesProperty);

            Type keyType;
            Type valueType;
            if (!TryResolveKeyValueTypes(fieldInfo, out keyType, out valueType))
            {
                EditorGUI.LabelField(position, label.text, "Unsupported dictionary type");
                EditorGUI.EndProperty();
                return;
            }

            Rect foldoutRect = new(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight
            );
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            float y = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;

            if (property.isExpanded)
            {
                ReorderableList list = GetOrCreateList(property, keysProperty, valuesProperty);
                Rect listRect = new(position.x, y, position.width, list.GetHeight());

                int previousIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                list.DoList(listRect);
                EditorGUI.indentLevel = previousIndent;

                y = listRect.yMax + EditorGUIUtility.standardVerticalSpacing;

                PendingEntry pending = GetOrCreatePendingEntry(property, keyType, valueType);
                DrawPendingEntryUI(
                    ref y,
                    position,
                    pending,
                    list,
                    property,
                    keysProperty,
                    valuesProperty,
                    keyType,
                    valueType
                );
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded)
            {
                return height;
            }

            SerializedProperty keysProperty = property.FindPropertyRelative("_keys");
            SerializedProperty valuesProperty = property.FindPropertyRelative("_values");
            EnsureParallelArraySizes(keysProperty, valuesProperty);
            ReorderableList list = GetOrCreateList(property, keysProperty, valuesProperty);

            float listHeight = list.GetHeight();
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float pendingHeight = GetPendingSectionHeight();

            height += spacing + listHeight + spacing + pendingHeight;
            return height;
        }

        private ReorderableList GetOrCreateList(
            SerializedProperty dictionaryProperty,
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty
        )
        {
            string key = GetListKey(dictionaryProperty);
            PaginationState pagination = GetOrCreatePaginationState(dictionaryProperty);
            ClampPaginationState(pagination, keysProperty.arraySize);

            if (_lists.TryGetValue(key, out ReorderableList cached))
            {
                EnsureListSelectionWithinPage(cached, pagination, keysProperty);
                return cached;
            }

            ReorderableList list = new ReorderableList(
                dictionaryProperty.serializedObject,
                keysProperty,
                draggable: true,
                displayHeader: true,
                displayAddButton: false,
                displayRemoveButton: true
            );

            list.drawHeaderCallback = rect =>
            {
                DrawListHeader(rect, dictionaryProperty, keysProperty, list, pagination);
            };

            list.elementHeightCallback = index =>
            {
                if (!IsIndexInCurrentPage(index, pagination, keysProperty.arraySize))
                {
                    return 0f;
                }

                return EditorGUIUtility.singleLineHeight
                    + (EditorGUIUtility.standardVerticalSpacing * 2f);
            };

            list.drawElementCallback = (rect, index, _, _) =>
            {
                if (!IsIndexInCurrentPage(index, pagination, keysProperty.arraySize))
                {
                    return;
                }

                SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(index);
                SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(index);

                rect.y += EditorGUIUtility.standardVerticalSpacing;
                float gap = 6f;
                float halfWidth = (rect.width - gap) * 0.5f;

                Rect keyRect = new(rect.x, rect.y, halfWidth, EditorGUIUtility.singleLineHeight);
                Rect valueRect = new(
                    rect.x + halfWidth + gap,
                    rect.y,
                    halfWidth,
                    EditorGUIUtility.singleLineHeight
                );

                EditorGUI.PropertyField(keyRect, keyProperty, GUIContent.none, true);
                EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none, true);
            };

            list.onRemoveCallback = reorderableList =>
            {
                int removeIndex = reorderableList.index;
                if (removeIndex < 0 || removeIndex >= keysProperty.arraySize)
                {
                    return;
                }

                keysProperty.DeleteArrayElementAtIndex(removeIndex);
                valuesProperty.DeleteArrayElementAtIndex(removeIndex);
                dictionaryProperty.serializedObject.ApplyModifiedProperties();
                SyncRuntimeDictionary(dictionaryProperty);
                ClampPaginationState(pagination, keysProperty.arraySize);
                EnsureListSelectionWithinPage(reorderableList, pagination, keysProperty);
                GUI.changed = true;
            };

            list.onReorderCallbackWithDetails = (_, oldIndex, newIndex) =>
            {
                valuesProperty.MoveArrayElement(oldIndex, newIndex);
            };

            list.onSelectCallback = reorderableList =>
            {
                if (reorderableList.index >= 0)
                {
                    int targetPage = GetPageForIndex(reorderableList.index, pagination.PageSize);
                    SetPageIndex(pagination, targetPage, reorderableList, keysProperty);
                }
            };

            EnsureListSelectionWithinPage(list, pagination, keysProperty);

            _lists[key] = list;
            return list;
        }

        private PendingEntry GetOrCreatePendingEntry(
            SerializedProperty property,
            Type keyType,
            Type valueType
        )
        {
            string key = GetListKey(property);
            if (_pendingEntries.TryGetValue(key, out PendingEntry entry))
            {
                return entry;
            }

            entry = new PendingEntry
            {
                KeyType = keyType,
                ValueType = valueType,
                Key = GetDefaultValue(keyType),
                Value = GetDefaultValue(valueType),
            };
            _pendingEntries[key] = entry;
            return entry;
        }

        private PaginationState GetOrCreatePaginationState(SerializedProperty property)
        {
            string key = GetListKey(property);
            if (_paginationStates.TryGetValue(key, out PaginationState state))
            {
                if (state.PageSize <= 0)
                {
                    state.PageSize = DefaultPageSize;
                }

                return state;
            }

            PaginationState newState = new PaginationState
            {
                PageIndex = 0,
                PageSize = DefaultPageSize,
            };
            _paginationStates[key] = newState;
            return newState;
        }

        private void DrawListHeader(
            Rect rect,
            SerializedProperty dictionaryProperty,
            SerializedProperty keysProperty,
            ReorderableList list,
            PaginationState pagination
        )
        {
            ClampPaginationState(pagination, keysProperty.arraySize);
            EnsureListSelectionWithinPage(list, pagination, keysProperty);

            float spacing = PaginationControlSpacing;
            float controlsWidth =
                (PaginationButtonWidth * 4f) + (spacing * 3f) + PaginationLabelWidth;
            float labelWidth = Mathf.Max(0f, rect.width - controlsWidth - spacing);

            Rect labelRect = new(rect.x, rect.y, labelWidth, rect.height);
            EditorGUI.LabelField(labelRect, dictionaryProperty.displayName);

            Rect controlsRect = new(rect.xMax - controlsWidth, rect.y, controlsWidth, rect.height);
            Rect pageLabelRect = new(
                controlsRect.x,
                controlsRect.y,
                PaginationLabelWidth,
                controlsRect.height
            );

            int itemCount = keysProperty.arraySize;
            int totalPages = GetTotalPages(itemCount, pagination.PageSize);
            string pageLabel = string.Format("Page {0}/{1}", pagination.PageIndex + 1, totalPages);
            EditorGUI.LabelField(pageLabelRect, pageLabel, EditorStyles.miniLabel);

            float buttonX = pageLabelRect.xMax + spacing;
            Rect firstRect = new(
                buttonX,
                controlsRect.y,
                PaginationButtonWidth,
                controlsRect.height
            );
            buttonX += PaginationButtonWidth + spacing;
            Rect prevRect = new(
                buttonX,
                controlsRect.y,
                PaginationButtonWidth,
                controlsRect.height
            );
            buttonX += PaginationButtonWidth + spacing;
            Rect nextRect = new(
                buttonX,
                controlsRect.y,
                PaginationButtonWidth,
                controlsRect.height
            );
            buttonX += PaginationButtonWidth + spacing;
            Rect lastRect = new(
                buttonX,
                controlsRect.y,
                PaginationButtonWidth,
                controlsRect.height
            );

            using (new EditorGUI.DisabledScope(pagination.PageIndex <= 0))
            {
                if (GUI.Button(firstRect, "|<", EditorStyles.miniButton))
                {
                    SetPageIndex(pagination, 0, list, keysProperty);
                }

                if (GUI.Button(prevRect, "<", EditorStyles.miniButton))
                {
                    SetPageIndex(pagination, pagination.PageIndex - 1, list, keysProperty);
                }
            }

            using (new EditorGUI.DisabledScope(pagination.PageIndex >= totalPages - 1))
            {
                if (GUI.Button(nextRect, ">", EditorStyles.miniButton))
                {
                    SetPageIndex(pagination, pagination.PageIndex + 1, list, keysProperty);
                }

                if (GUI.Button(lastRect, ">|", EditorStyles.miniButton))
                {
                    SetPageIndex(pagination, totalPages - 1, list, keysProperty);
                }
            }
        }

        private void ClampPaginationState(PaginationState pagination, int itemCount)
        {
            if (pagination.PageSize <= 0)
            {
                pagination.PageSize = DefaultPageSize;
            }

            int totalPages = GetTotalPages(itemCount, pagination.PageSize);
            pagination.PageIndex = Mathf.Clamp(pagination.PageIndex, 0, totalPages - 1);
        }

        private static int GetTotalPages(int itemCount, int pageSize)
        {
            if (pageSize <= 0)
            {
                return 1;
            }

            if (itemCount <= 0)
            {
                return 1;
            }

            return Mathf.CeilToInt(itemCount / (float)pageSize);
        }

        private static bool IsIndexInCurrentPage(
            int index,
            PaginationState pagination,
            int itemCount
        )
        {
            if (pagination.PageSize <= 0)
            {
                return true;
            }

            int startIndex = pagination.PageIndex * pagination.PageSize;
            int endIndex = Mathf.Min(startIndex + pagination.PageSize, itemCount);
            return index >= startIndex && index < endIndex;
        }

        private static int GetPageForIndex(int index, int pageSize)
        {
            if (pageSize <= 0)
            {
                return 0;
            }

            if (index <= 0)
            {
                return 0;
            }

            return index / pageSize;
        }

        private void SetPageIndex(
            PaginationState pagination,
            int targetPage,
            ReorderableList list,
            SerializedProperty keysProperty
        )
        {
            if (pagination.PageSize <= 0)
            {
                pagination.PageSize = DefaultPageSize;
            }

            int totalPages = GetTotalPages(keysProperty.arraySize, pagination.PageSize);
            int clampedPage = Mathf.Clamp(targetPage, 0, totalPages - 1);
            if (pagination.PageIndex == clampedPage)
            {
                return;
            }

            pagination.PageIndex = clampedPage;
            EnsureListSelectionWithinPage(list, pagination, keysProperty);
            GUI.changed = true;
        }

        private void EnsureListSelectionWithinPage(
            ReorderableList list,
            PaginationState pagination,
            SerializedProperty keysProperty
        )
        {
            int itemCount = keysProperty.arraySize;
            if (itemCount <= 0)
            {
                list.index = -1;
                return;
            }

            if (pagination.PageSize <= 0)
            {
                pagination.PageSize = DefaultPageSize;
            }

            int startIndex = pagination.PageIndex * pagination.PageSize;
            int endIndex = Mathf.Min(startIndex + pagination.PageSize, itemCount);

            if (startIndex >= itemCount)
            {
                pagination.PageIndex = GetTotalPages(itemCount, pagination.PageSize) - 1;
                startIndex = pagination.PageIndex * pagination.PageSize;
                endIndex = Mathf.Min(startIndex + pagination.PageSize, itemCount);
            }

            if (list.index < startIndex || list.index >= endIndex)
            {
                list.index = startIndex;
            }
        }

        private void DrawPendingEntryUI(
            ref float y,
            Rect fullPosition,
            PendingEntry pending,
            ReorderableList list,
            SerializedProperty dictionaryProperty,
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty,
            Type keyType,
            Type valueType
        )
        {
            float rowHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float sectionHeight = GetPendingSectionHeight();

            Rect containerRect = new(fullPosition.x, y, fullPosition.width, sectionHeight);
            Color backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.18f, 0.18f, 1f)
                : new Color(0.92f, 0.92f, 0.92f, 1f);
            EditorGUI.DrawRect(containerRect, backgroundColor);

            float innerX = containerRect.x + PendingSectionPadding;
            float innerWidth = containerRect.width - (PendingSectionPadding * 2f);
            float innerY = containerRect.y + PendingSectionPadding;

            Rect headerRect = new(innerX, innerY, innerWidth, rowHeight);
            EditorGUI.LabelField(headerRect, "New Entry", EditorStyles.boldLabel);
            innerY += rowHeight + spacing;

            Rect keyRect = new(innerX, innerY, innerWidth, rowHeight);
            pending.Key = DrawFieldForType(keyRect, "Key", pending.Key, keyType);
            innerY += rowHeight + spacing;

            Rect valueRect = new(innerX, innerY, innerWidth, rowHeight);
            pending.Value = DrawFieldForType(valueRect, "Value", pending.Value, valueType);
            innerY += rowHeight + spacing;

            bool keySupported = IsTypeSupported(keyType);
            bool valueSupported = IsTypeSupported(valueType);
            bool keyValid = KeyIsValid(keyType, pending.Key);
            bool canCommit = keySupported && valueSupported && keyValid;

            int existingIndex = FindExistingKeyIndex(keysProperty, keyType, pending.Key);
            string buttonLabel = existingIndex >= 0 ? "Overwrite" : "Add";

            Rect buttonsRect = new(innerX, innerY, innerWidth, rowHeight);
            Rect addRect = new(buttonsRect.x, buttonsRect.y, PendingAddButtonWidth, rowHeight);
            Rect clearRect = new(
                buttonsRect.xMax - PendingClearButtonWidth,
                buttonsRect.y,
                PendingClearButtonWidth,
                rowHeight
            );

            float infoX = addRect.xMax + spacing;
            float availableInfoWidth = clearRect.x - spacing - infoX;
            if (availableInfoWidth > 0f)
            {
                Rect infoRect = new(infoX, buttonsRect.y, availableInfoWidth, rowHeight);
                string infoMessage = GetPendingInfoMessage(
                    keySupported,
                    valueSupported,
                    keyValid,
                    existingIndex,
                    keyType,
                    valueType
                );
                if (!string.IsNullOrEmpty(infoMessage))
                {
                    GUI.Label(infoRect, infoMessage, EditorStyles.miniLabel);
                }
            }

            using (new EditorGUI.DisabledScope(!canCommit))
            {
                if (GUI.Button(addRect, buttonLabel))
                {
                    CommitResult result = CommitEntry(
                        keysProperty,
                        valuesProperty,
                        keyType,
                        valueType,
                        pending,
                        existingIndex,
                        dictionaryProperty
                    );
                    if (result.Added)
                    {
                        pending.Key = GetDefaultValue(keyType);
                        pending.Value = GetDefaultValue(valueType);
                    }

                    if (result.Index >= 0)
                    {
                        PaginationState pagination = GetOrCreatePaginationState(dictionaryProperty);
                        int targetPage = GetPageForIndex(result.Index, pagination.PageSize);
                        SetPageIndex(pagination, targetPage, list, keysProperty);
                        list.index = result.Index;
                    }

                    GUI.FocusControl(null);
                }
            }

            if (GUI.Button(clearRect, "Clear"))
            {
                pending.Key = GetDefaultValue(keyType);
                pending.Value = GetDefaultValue(valueType);
                GUI.FocusControl(null);
            }

            y = containerRect.yMax;
        }

        private static string GetPendingInfoMessage(
            bool keySupported,
            bool valueSupported,
            bool keyValid,
            int existingIndex,
            Type keyType,
            Type valueType
        )
        {
            if (!keySupported)
            {
                return $"Unsupported key type ({keyType.Name})";
            }

            if (!valueSupported)
            {
                return $"Unsupported value type ({valueType.Name})";
            }

            if (!keyValid)
            {
                return "Enter a valid key to continue.";
            }

            if (existingIndex >= 0)
            {
                return "Existing key will be overwritten.";
            }

            return "Ready to add entry.";
        }

        private static float GetPendingSectionHeight()
        {
            float rowHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            return (rowHeight * 4f) + (spacing * 3f) + (PendingSectionPadding * 2f);
        }

        private CommitResult CommitEntry(
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty,
            Type keyType,
            Type valueType,
            PendingEntry pending,
            int existingIndex,
            SerializedProperty dictionaryProperty
        )
        {
            SerializedObject serializedObject = dictionaryProperty.serializedObject;
            UnityEngine.Object[] targets = serializedObject.targetObjects;
            if (targets.Length > 0)
            {
                string undoLabel =
                    existingIndex >= 0 ? "Overwrite Dictionary Entry" : "Add Dictionary Entry";
                Undo.RecordObjects(targets, undoLabel);
            }

            bool addedNewEntry = false;
            int affectedIndex = existingIndex;

            if (existingIndex >= 0)
            {
                SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(
                    existingIndex
                );
                SetPropertyValue(valueProperty, pending.Value, valueType);
                affectedIndex = existingIndex;
            }
            else
            {
                int insertIndex = keysProperty.arraySize;
                keysProperty.InsertArrayElementAtIndex(insertIndex);
                valuesProperty.InsertArrayElementAtIndex(insertIndex);

                SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(insertIndex);
                SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(
                    insertIndex
                );

                ClearArrayElement(keyProperty);
                ClearArrayElement(valueProperty);

                SetPropertyValue(keyProperty, pending.Key, keyType);
                SetPropertyValue(valueProperty, pending.Value, valueType);
                addedNewEntry = true;
                affectedIndex = insertIndex;
            }

            serializedObject.ApplyModifiedProperties();
            SyncRuntimeDictionary(dictionaryProperty);
            GUI.changed = true;
            return new CommitResult { Added = addedNewEntry, Index = affectedIndex };
        }

        private static bool TryResolveKeyValueTypes(
            FieldInfo fieldInfo,
            out Type keyType,
            out Type valueType
        )
        {
            keyType = null;
            valueType = null;
            if (fieldInfo == null)
            {
                return false;
            }

            Type type = fieldInfo.FieldType;
            while (type != null)
            {
                if (type.IsGenericType)
                {
                    Type genericDefinition = type.GetGenericTypeDefinition();
                    if (genericDefinition == typeof(SerializableDictionary<,>))
                    {
                        Type[] args = type.GetGenericArguments();
                        keyType = args[0];
                        valueType = args[1];
                        return true;
                    }

                    if (
                        genericDefinition == typeof(SerializableDictionary<,,>)
                        || genericDefinition == typeof(SerializableDictionaryBase<,,>)
                    )
                    {
                        Type[] args = type.GetGenericArguments();
                        keyType = args[0];
                        valueType = args[1];
                        return true;
                    }
                }

                type = type.BaseType;
            }

            return false;
        }

        private static void EnsureParallelArraySizes(
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty
        )
        {
            if (keysProperty == null || valuesProperty == null)
            {
                return;
            }

            if (keysProperty.arraySize == valuesProperty.arraySize)
            {
                return;
            }

            int size = Mathf.Min(keysProperty.arraySize, valuesProperty.arraySize);
            if (size < 0)
            {
                size = 0;
            }

            keysProperty.arraySize = size;
            valuesProperty.arraySize = size;
        }

        private static string GetListKey(SerializedProperty property)
        {
            int targetId =
                property.serializedObject.targetObject != null
                    ? property.serializedObject.targetObject.GetInstanceID()
                    : 0;
            return $"{targetId}_{property.propertyPath}";
        }

        private static bool KeyIsValid(Type keyType, object keyValue)
        {
            if (keyType == typeof(string))
            {
                return !string.IsNullOrEmpty(keyValue as string);
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(keyType))
            {
                return keyValue is UnityEngine.Object obj && obj != null;
            }

            return keyValue != null || keyType.IsValueType;
        }

        private static int FindExistingKeyIndex(
            SerializedProperty keysProperty,
            Type keyType,
            object keyValue
        )
        {
            if (!KeyIsValid(keyType, keyValue))
            {
                return -1;
            }

            for (int i = 0; i < keysProperty.arraySize; i++)
            {
                SerializedProperty element = keysProperty.GetArrayElementAtIndex(i);
                object existingValue = GetPropertyValue(element, keyType);

                if (ValuesEqual(existingValue, keyValue))
                {
                    return i;
                }
            }

            return -1;
        }

        private static object DrawFieldForType(Rect rect, string label, object current, Type type)
        {
            GUIContent content = new(label);

            if (!IsTypeSupported(type))
            {
                EditorGUI.LabelField(
                    rect,
                    content,
                    new GUIContent($"Unsupported type ({type.Name})")
                );
                return current;
            }

            if (type == typeof(string))
            {
                return EditorGUI.TextField(rect, content, current as string ?? string.Empty);
            }

            if (type == typeof(int))
            {
                return EditorGUI.IntField(rect, content, current is int i ? i : default);
            }

            if (type == typeof(float))
            {
                return EditorGUI.FloatField(rect, content, current is float f ? f : default);
            }

            if (type == typeof(double))
            {
                return EditorGUI.DoubleField(rect, content, current is double d ? d : default);
            }

            if (type == typeof(long))
            {
                return EditorGUI.LongField(rect, content, current is long l ? l : default);
            }

            if (type == typeof(bool))
            {
                return EditorGUI.Toggle(rect, content, current is bool b && b);
            }

            if (type == typeof(Vector2))
            {
                return EditorGUI.Vector2Field(
                    rect,
                    label,
                    current is Vector2 v2 ? v2 : Vector2.zero
                );
            }

            if (type == typeof(Vector3))
            {
                return EditorGUI.Vector3Field(
                    rect,
                    label,
                    current is Vector3 v3 ? v3 : Vector3.zero
                );
            }

            if (type == typeof(Vector4))
            {
                return EditorGUI.Vector4Field(
                    rect,
                    label,
                    current is Vector4 v4 ? v4 : Vector4.zero
                );
            }

            if (type == typeof(Vector2Int))
            {
                Vector2Int value = current is Vector2Int v2int ? v2int : default;
                Vector2Int newValue = EditorGUI.Vector2IntField(rect, label, value);
                return newValue;
            }

            if (type == typeof(Vector3Int))
            {
                Vector3Int value = current is Vector3Int v3int ? v3int : default;
                Vector3Int newValue = EditorGUI.Vector3IntField(rect, label, value);
                return newValue;
            }

            if (type == typeof(Rect))
            {
                Rect value = current is Rect rectValue ? rectValue : default;
                return EditorGUI.RectField(rect, label, value);
            }

            if (type == typeof(RectInt))
            {
                RectInt value = current is RectInt rectInt ? rectInt : default;
                return EditorGUI.RectIntField(rect, label, value);
            }

            if (type == typeof(Bounds))
            {
                Bounds value = current is Bounds bounds ? bounds : default;
                return EditorGUI.BoundsField(rect, label, value);
            }

            if (type == typeof(BoundsInt))
            {
                BoundsInt value = current is BoundsInt boundsInt ? boundsInt : default;
                return EditorGUI.BoundsIntField(rect, label, value);
            }

            if (type == typeof(Color))
            {
                Color value = current is Color color ? color : Color.clear;
                return EditorGUI.ColorField(rect, label, value);
            }

            if (type == typeof(AnimationCurve))
            {
                AnimationCurve value = current as AnimationCurve ?? new AnimationCurve();
                return EditorGUI.CurveField(rect, label, value);
            }

            if (type.IsEnum)
            {
                Enum currentEnum = current as Enum ?? (Enum)Enum.ToObject(type, 0);
                return EditorGUI.EnumPopup(rect, content, currentEnum);
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                UnityEngine.Object obj = current as UnityEngine.Object;
                return EditorGUI.ObjectField(rect, content, obj, type, allowSceneObjects: false);
            }

            EditorGUI.LabelField(rect, content, new GUIContent($"Unsupported type ({type.Name})"));
            return current;
        }

        private static bool IsTypeSupported(Type type)
        {
            return type == typeof(string)
                || type == typeof(int)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(long)
                || type == typeof(bool)
                || type == typeof(Vector2)
                || type == typeof(Vector3)
                || type == typeof(Vector4)
                || type == typeof(Vector2Int)
                || type == typeof(Vector3Int)
                || type == typeof(Rect)
                || type == typeof(RectInt)
                || type == typeof(Bounds)
                || type == typeof(BoundsInt)
                || type == typeof(Color)
                || type == typeof(AnimationCurve)
                || type.IsEnum
                || typeof(UnityEngine.Object).IsAssignableFrom(type);
        }

        private static void SetPropertyValue(SerializedProperty property, object value, Type type)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.longValue = Convert.ToInt64(value);
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = value is bool b && b;
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = Convert.ToSingle(value);
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = value as string ?? string.Empty;
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = value is Color color ? color : Color.clear;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = value as UnityEngine.Object;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = value is Rect rect ? rect : default;
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = value is RectInt rectInt ? rectInt : default;
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = value is Bounds bounds ? bounds : default;
                    break;
                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = value is BoundsInt boundsInt ? boundsInt : default;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = value is Vector2 vector2 ? vector2 : default;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = value is Vector3 vector3 ? vector3 : default;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = value is Vector4 vector4 ? vector4 : default;
                    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = value is Quaternion quaternion
                        ? quaternion
                        : Quaternion.identity;
                    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = value is Vector2Int vector2Int
                        ? vector2Int
                        : default;
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = value is Vector3Int vector3Int
                        ? vector3Int
                        : default;
                    break;
                case SerializedPropertyType.Enum:
                    int enumNumeric = value != null ? Convert.ToInt32(value) : 0;
                    property.intValue = enumNumeric;
                    string enumName = value != null ? Enum.GetName(type, value) : null;
                    if (!string.IsNullOrEmpty(enumName))
                    {
                        int nameIndex = Array.IndexOf(property.enumNames, enumName);
                        property.enumValueIndex = nameIndex >= 0 ? nameIndex : enumNumeric;
                    }
                    else
                    {
                        property.enumValueIndex = enumNumeric;
                    }
                    break;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = value as AnimationCurve ?? new AnimationCurve();
                    break;
                case SerializedPropertyType.Hash128:
                    property.hash128Value = value is Hash128 hash ? hash : default;
                    break;
                case SerializedPropertyType.ManagedReference:
                    property.managedReferenceValue = value;
                    break;
                default:
                    throw new NotSupportedException(
                        $"Unsupported property type: {property.propertyType}"
                    );
            }
        }

        private static object GetPropertyValue(SerializedProperty property, Type type)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    long longValue = property.longValue;
                    if (type.IsEnum)
                    {
                        return Enum.ToObject(type, longValue);
                    }

                    if (type == typeof(int))
                    {
                        return (int)longValue;
                    }

                    return longValue;
                case SerializedPropertyType.Boolean:
                    return property.boolValue;
                case SerializedPropertyType.Float:
                    float floatValue = property.floatValue;
                    return type == typeof(double) ? (double)floatValue : floatValue;
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Color:
                    return property.colorValue;
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue;
                case SerializedPropertyType.Rect:
                    return property.rectValue;
                case SerializedPropertyType.RectInt:
                    return property.rectIntValue;
                case SerializedPropertyType.Bounds:
                    return property.boundsValue;
                case SerializedPropertyType.BoundsInt:
                    return property.boundsIntValue;
                case SerializedPropertyType.Vector2:
                    return property.vector2Value;
                case SerializedPropertyType.Vector3:
                    return property.vector3Value;
                case SerializedPropertyType.Vector4:
                    return property.vector4Value;
                case SerializedPropertyType.Quaternion:
                    return property.quaternionValue;
                case SerializedPropertyType.Vector2Int:
                    return property.vector2IntValue;
                case SerializedPropertyType.Vector3Int:
                    return property.vector3IntValue;
                case SerializedPropertyType.Enum:
                    return Enum.ToObject(type, property.intValue);
                case SerializedPropertyType.AnimationCurve:
                    return property.animationCurveValue;
                case SerializedPropertyType.Hash128:
                    return property.hash128Value;
                case SerializedPropertyType.ManagedReference:
                    return property.managedReferenceValue;
                default:
                    return null;
            }
        }

        private static void ClearArrayElement(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.longValue = default;
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = default;
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = default;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = string.Empty;
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = Color.clear;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = null;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = default;
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = default;
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = default;
                    break;
                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = default;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = default;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = default;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = default;
                    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = Quaternion.identity;
                    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = default;
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = default;
                    break;
                case SerializedPropertyType.Enum:
                    property.enumValueIndex = 0;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = new AnimationCurve();
                    break;
                case SerializedPropertyType.Hash128:
                    property.hash128Value = default;
                    break;
                case SerializedPropertyType.ManagedReference:
                    property.managedReferenceValue = null;
                    break;
            }
        }

        private static bool ValuesEqual(object left, object right)
        {
            if (left == null && right == null)
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            return left.Equals(right);
        }

        private static object GetDefaultValue(Type type)
        {
            if (type == typeof(string))
            {
                return string.Empty;
            }

            if (type == typeof(Color))
            {
                return Color.clear;
            }

            if (type == typeof(AnimationCurve))
            {
                return new AnimationCurve();
            }

            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }

        private struct CommitResult
        {
            public bool Added;
            public int Index;
        }

        private sealed class PendingEntry
        {
            public Type KeyType;
            public Type ValueType;
            public object Key;
            public object Value;
        }

        private sealed class PaginationState
        {
            public int PageIndex;
            public int PageSize;
        }

        private void SyncRuntimeDictionary(SerializedProperty dictionaryProperty)
        {
            SerializedObject serializedObject = dictionaryProperty.serializedObject;
            UnityEngine.Object[] targets = serializedObject.targetObjects;

            foreach (UnityEngine.Object target in targets)
            {
                object dictionaryInstance = GetTargetObjectOfProperty(
                    target,
                    dictionaryProperty.propertyPath
                );
                if (dictionaryInstance is ISerializationCallbackReceiver receiver)
                {
                    receiver.OnAfterDeserialize();
                    EditorUtility.SetDirty(target);
                }
            }

            serializedObject.UpdateIfRequiredOrScript();
        }

        private static object GetTargetObjectOfProperty(object target, string propertyPath)
        {
            if (target == null || string.IsNullOrEmpty(propertyPath))
            {
                return null;
            }

            string path = propertyPath.Replace(".Array.data[", "[");
            string[] elements = path.Split('.');

            object current = target;
            Type currentType = current.GetType();

            foreach (string elementWithPotentialIndex in elements)
            {
                if (current == null)
                {
                    return null;
                }

                string element = elementWithPotentialIndex;
                int index = -1;
                int bracketIndex = element.IndexOf('[', StringComparison.Ordinal);
                if (bracketIndex >= 0)
                {
                    int endBracket = element.IndexOf(']', bracketIndex + 1);
                    if (endBracket < 0)
                    {
                        return null;
                    }

                    string indexString = element.Substring(
                        bracketIndex + 1,
                        endBracket - bracketIndex - 1
                    );
                    if (!int.TryParse(indexString, out index))
                    {
                        return null;
                    }

                    element = element.Substring(0, bracketIndex);
                }

                if (!string.IsNullOrEmpty(element))
                {
                    FieldInfo field = currentType.GetField(
                        element,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );

                    if (field != null)
                    {
                        current = field.GetValue(current);
                        currentType = current?.GetType();
                    }
                    else
                    {
                        PropertyInfo propertyInfo = currentType.GetProperty(
                            element,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );

                        if (propertyInfo == null)
                        {
                            return null;
                        }

                        current = propertyInfo.GetValue(current);
                        currentType = current?.GetType();
                    }
                }

                if (index >= 0)
                {
                    if (current is System.Collections.IList list)
                    {
                        if (index < 0 || index >= list.Count)
                        {
                            return null;
                        }

                        current = list[index];
                        currentType = current?.GetType();
                    }
                    else if (current is Array array)
                    {
                        if (index < 0 || index >= array.Length)
                        {
                            return null;
                        }

                        current = array.GetValue(index);
                        currentType = current?.GetType();
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return current;
        }
    }
}

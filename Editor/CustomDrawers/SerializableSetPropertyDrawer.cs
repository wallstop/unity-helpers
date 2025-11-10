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
        private const float PaginationButtonWidth = 28f;
        private const int DefaultPageSize = 15;
        private const int MaxAutoAddAttempts = 256;

        private static readonly GUIContent AddEntryContent = new("Add");
        private static readonly GUIContent ClearAllContent = new("Clear All");
        private static readonly GUIContent SortContent = new("Sort");
        private static readonly GUIContent FirstPageContent = new("<<", "First Page");
        private static readonly GUIContent PreviousPageContent = new("<", "Previous Page");
        private static readonly GUIContent NextPageContent = new(">", "Next Page");
        private static readonly GUIContent LastPageContent = new(">>", "Last Page");
        private static readonly object NullComparable = new();

        private static readonly GUIStyle AddButtonStyle = CreateSolidButtonStyle(
            new Color(0.22f, 0.62f, 0.29f)
        );
        private static readonly GUIStyle ClearAllActiveButtonStyle = CreateSolidButtonStyle(
            new Color(0.82f, 0.27f, 0.27f)
        );
        private static readonly GUIStyle ClearAllInactiveButtonStyle = CreateSolidButtonStyle(
            new Color(0.55f, 0.55f, 0.55f)
        );
        private static readonly GUIStyle SortActiveButtonStyle = CreateSolidButtonStyle(
            new Color(0.22f, 0.62f, 0.29f)
        );
        private static readonly GUIStyle SortInactiveButtonStyle = CreateSolidButtonStyle(
            new Color(0.55f, 0.55f, 0.55f)
        );
        private static readonly GUIStyle RemoveButtonStyle = CreateSolidButtonStyle(
            new Color(0.86f, 0.23f, 0.23f)
        );
        private static readonly Color DuplicatePrimaryColor = new(0.99f, 0.82f, 0.35f, 0.55f);
        private static readonly Color DuplicateSecondaryColor = new(0.96f, 0.45f, 0.45f, 0.65f);
        private static readonly Color DuplicateOutlineColor = new(0.65f, 0.18f, 0.18f, 0.9f);
        private static readonly Color LightSelectionColor = new(0.33f, 0.62f, 0.95f, 0.65f);
        private static readonly Color DarkSelectionColor = new(0.2f, 0.45f, 0.85f, 0.7f);
        private static readonly Color LightRowColor = new(0.97f, 0.97f, 0.97f, 1f);
        private static readonly Color DarkRowColor = new(0.16f, 0.16f, 0.16f, 0.45f);
        private static readonly Color NullEntryHighlightColor = new(0.84f, 0.2f, 0.2f, 0.6f);
        private const float DuplicateShakeAmplitude = 2f;
        private const float DuplicateShakeFrequency = 7f;
        private const float DuplicateOutlineThickness = 1f;
        private static readonly GUIContent NullEntryTooltipContent = new();

        private readonly Dictionary<string, PaginationState> _paginationStates = new();
        private readonly Dictionary<string, DuplicateState> _duplicateStates = new();
        private readonly Dictionary<string, NullEntryState> _nullEntryStates = new();

        internal sealed class PaginationState
        {
            public int page;
            public int pageSize = DefaultPageSize;
            public int selectedIndex = -1;
        }

        internal sealed class DuplicateState
        {
            public bool hasDuplicates;
            public readonly HashSet<int> duplicateIndices = new();
            public string summary = string.Empty;
            public readonly Dictionary<int, double> animationStartTimes = new();
            public readonly Dictionary<int, bool> primaryFlags = new();
            public readonly Dictionary<object, List<int>> grouping = new();
            public readonly Stack<List<int>> listPool = new();
            public readonly StringBuilder summaryBuilder = new();
            public readonly List<int> animationKeysScratch = new();
            public readonly List<object> groupingKeysScratch = new();
        }

        internal sealed class NullEntryState
        {
            public bool hasNullEntries;
            public readonly HashSet<int> nullIndices = new();
            public readonly Dictionary<int, string> tooltips = new();
            public string summary = string.Empty;
            public readonly List<int> scratch = new();
        }

        private struct SetElementData
        {
            public SerializedPropertyType propertyType;
            public object comparable;
            public object value;
        }

        private sealed class RowRenderInfo
        {
            public SerializedProperty property;
            public float height;
            public int index;
            public bool isDuplicate;
            public bool isPrimaryDuplicate;
            public bool isSelected;
            public float shakeOffset;
            public bool hasNullValue;
            public string nullTooltip;
        }

        private static float GetToolbarHeight()
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            return lineHeight * 2f + RowSpacing;
        }

        static SerializableSetPropertyDrawer()
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            ConfigureButtonStyle(AddButtonStyle, lineHeight);
            ConfigureButtonStyle(ClearAllActiveButtonStyle, lineHeight);
            ConfigureButtonStyle(ClearAllInactiveButtonStyle, lineHeight);
            ConfigureButtonStyle(SortActiveButtonStyle, lineHeight);
            ConfigureButtonStyle(SortInactiveButtonStyle, lineHeight);
            ConfigureButtonStyle(RemoveButtonStyle, lineHeight);
            RemoveButtonStyle.fixedWidth = 0f;
            RemoveButtonStyle.padding = new RectOffset(3, 3, 1, 1);
            RemoveButtonStyle.margin = new RectOffset(0, 0, 1, 1);
        }

        private static GUIStyle CreateSolidButtonStyle(Color baseColor)
        {
            Color hoverColor = AdjustColorBrightness(baseColor, 0.12f);
            Color activeColor = AdjustColorBrightness(baseColor, -0.18f);

            GUIStyle style = new(EditorStyles.miniButton);
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

        private static void ConfigureButtonStyle(GUIStyle style, float lineHeight)
        {
            if (style == null)
            {
                return;
            }

            style.fixedHeight = lineHeight;
            style.margin = new RectOffset(1, 1, 1, 1);
            style.padding = new RectOffset(6, 6, 2, 2);
        }

        private static Texture2D CreateSolidTexture(Color color)
        {
            Texture2D texture = new(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point,
            };
            Color opaque = new(color.r, color.g, color.b, 1f);
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
            SerializedObject serializedObject = property.serializedObject;
            serializedObject.UpdateIfRequiredOrScript();

            SerializedProperty itemsProperty = property.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            bool hasItemsArray = itemsProperty is { isArray: true };
            int totalCount = hasItemsArray ? itemsProperty.arraySize : 0;

            string propertyPath = property.propertyPath;
            string foldoutLabel = BuildFoldoutLabel(label);

            EditorGUI.BeginProperty(position, label, property);

            Rect foldoutRect = new(
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

            int previousIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect toolbarRect = new(position.x, y, position.width, GetToolbarHeight());
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
            hasItemsArray = itemsProperty is { isArray: true };
            totalCount = hasItemsArray ? itemsProperty.arraySize : 0;
            EnsurePaginationBounds(pagination, totalCount);
            DuplicateState duplicateState = EvaluateDuplicateState(property, itemsProperty);
            NullEntryState nullState = EvaluateNullEntryState(property, itemsProperty);

            if (nullState.hasNullEntries && !string.IsNullOrEmpty(nullState.summary))
            {
                float helpHeight = GetWarningBarHeight();
                Rect helpRect = new(position.x, y, position.width, helpHeight);
                EditorGUI.HelpBox(helpRect, nullState.summary, MessageType.Warning);
                y = helpRect.yMax + SectionSpacing;
            }

            if (duplicateState.hasDuplicates && !string.IsNullOrEmpty(duplicateState.summary))
            {
                float helpHeight = GetWarningBarHeight();
                Rect helpRect = new(position.x, y, position.width, helpHeight);
                EditorGUI.HelpBox(helpRect, duplicateState.summary, MessageType.Warning);
                y = helpRect.yMax + SectionSpacing;
            }

            if (!hasItemsArray || totalCount == 0)
            {
                float blockPadding = 6f;
                float messageHeight = EditorGUIUtility.singleLineHeight;
                float blockHeight = blockPadding * 2f + messageHeight;
                Rect blockRect = new(position.x, y, position.width, blockHeight);
                GUI.Box(blockRect, GUIContent.none, EditorStyles.helpBox);

                Rect messageRect = new(
                    blockRect.x + blockPadding,
                    blockRect.y + blockPadding,
                    blockRect.width - blockPadding * 2f,
                    messageHeight
                );
                EditorGUI.LabelField(messageRect, "Set is empty.", EditorStyles.miniLabel);
            }
            else
            {
                int startIndex = pagination.page * pagination.pageSize;
                int endIndex = Mathf.Min(startIndex + pagination.pageSize, totalCount);
                UnityHelpersSettings.DuplicateRowAnimationMode animationMode =
                    UnityHelpersSettings.GetDuplicateRowAnimationMode();
                bool highlightDuplicates =
#pragma warning disable CS0618 // Type or member is obsolete
                    animationMode != UnityHelpersSettings.DuplicateRowAnimationMode.None;
#pragma warning restore CS0618 // Type or member is obsolete
                bool animateDuplicates =
                    animationMode == UnityHelpersSettings.DuplicateRowAnimationMode.Tween;
                int tweenCycleLimit = UnityHelpersSettings.GetDuplicateRowTweenCycleLimit();

                List<RowRenderInfo> rows = new(endIndex - startIndex);
                float rowsHeight = 0f;

                for (int index = startIndex; index < endIndex; index++)
                {
                    SerializedProperty element = itemsProperty.GetArrayElementAtIndex(index);
                    float elementHeight = EditorGUI.GetPropertyHeight(
                        element,
                        GUIContent.none,
                        true
                    );

                    bool isDuplicate = duplicateState.duplicateIndices.Contains(index);
                    if (
                        isDuplicate
                        && highlightDuplicates
                        && animateDuplicates
                        && !duplicateState.animationStartTimes.ContainsKey(index)
                    )
                    {
                        duplicateState.animationStartTimes[index] =
                            EditorApplication.timeSinceStartup;
                    }

                    float shakeOffset =
                        isDuplicate && highlightDuplicates && animateDuplicates
                            ? GetDuplicateShakeOffset(duplicateState, index, tweenCycleLimit)
                            : 0f;

                    bool hasNullValue =
                        nullState.hasNullEntries && nullState.nullIndices.Contains(index);
                    string nullTooltip = string.Empty;
                    if (hasNullValue && !nullState.tooltips.TryGetValue(index, out nullTooltip))
                    {
                        nullTooltip = string.Empty;
                    }

                    RowRenderInfo info = new()
                    {
                        property = element.Copy(),
                        height = elementHeight,
                        index = index,
                        isDuplicate = isDuplicate,
                        isPrimaryDuplicate = duplicateState.primaryFlags.GetValueOrDefault(
                            index,
                            false
                        ),
                        isSelected = pagination.selectedIndex == index,
                        shakeOffset = shakeOffset,
                        hasNullValue = hasNullValue,
                        nullTooltip = nullTooltip,
                    };

                    rows.Add(info);
                    rowsHeight += elementHeight;
                    if (index < endIndex - 1)
                    {
                        rowsHeight += RowSpacing;
                    }
                }

                float blockPadding = 6f;
                float blockHeight = blockPadding * 2f + rowsHeight;
                Rect blockRect = new(position.x, y, position.width, blockHeight);
                GUI.Box(blockRect, GUIContent.none, EditorStyles.helpBox);

                float contentY = blockRect.y + blockPadding;
                float contentX = blockRect.x + blockPadding;
                float contentWidth = blockRect.width - blockPadding * 2f;

                for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    RowRenderInfo row = rows[rowIndex];

                    Rect backgroundRect = new(contentX, contentY, contentWidth, row.height);

                    Color baseRowColor = EditorGUIUtility.isProSkin ? DarkRowColor : LightRowColor;
                    EditorGUI.DrawRect(backgroundRect, baseRowColor);

                    Rect outlineRect = Rect.zero;
                    bool shouldDrawOutline = false;
                    bool outlineForNull = false;

                    if (row.isDuplicate && highlightDuplicates)
                    {
                        Rect duplicateRect = ExpandRowRectVertically(backgroundRect);
                        duplicateRect.x += row.shakeOffset;
                        duplicateRect.xMin += 1f;
                        duplicateRect.xMax -= 1f;
                        duplicateRect.yMin += 1f;
                        duplicateRect.yMax -= 1f;
                        duplicateRect.height = Mathf.Max(0f, duplicateRect.height);

                        Color duplicateColor = row.isPrimaryDuplicate
                            ? DuplicatePrimaryColor
                            : DuplicateSecondaryColor;
                        EditorGUI.DrawRect(duplicateRect, duplicateColor);
                        outlineRect = duplicateRect;
                        shouldDrawOutline = true;
                    }

                    if (row.hasNullValue)
                    {
                        Rect nullRect = ExpandRowRectVertically(backgroundRect);
                        nullRect.x += row.shakeOffset;
                        nullRect.xMin += 1f;
                        nullRect.xMax -= 1f;
                        nullRect.yMin += 1f;
                        nullRect.yMax -= 1f;
                        nullRect.height = Mathf.Max(0f, nullRect.height);

                        EditorGUI.DrawRect(nullRect, NullEntryHighlightColor);
                        outlineRect = nullRect;
                        outlineForNull = true;
                        DrawNullEntryTooltip(nullRect, row.nullTooltip);
                    }

                    if (row.isSelected)
                    {
                        Rect selectionRect = ExpandRowRectVertically(backgroundRect);
                        Color selectionColor = EditorGUIUtility.isProSkin
                            ? DarkSelectionColor
                            : LightSelectionColor;
                        EditorGUI.DrawRect(selectionRect, selectionColor);
                    }

                    Rect contentRect = new(
                        backgroundRect.x + 6f,
                        backgroundRect.y,
                        backgroundRect.width - 12f,
                        row.height
                    );
                    if (Mathf.Abs(row.shakeOffset) > Mathf.Epsilon)
                    {
                        contentRect.x += row.shakeOffset;
                        contentRect.width = Mathf.Max(
                            0f,
                            contentRect.width - Mathf.Abs(row.shakeOffset)
                        );
                    }

                    if (
                        Event.current.type == EventType.MouseDown
                        && backgroundRect.Contains(Event.current.mousePosition)
                    )
                    {
                        pagination.selectedIndex = row.index;
                    }

                    EditorGUI.PropertyField(contentRect, row.property, GUIContent.none, true);

                    if (outlineForNull)
                    {
                        DrawDuplicateOutline(outlineRect);
                    }
                    else if (shouldDrawOutline && highlightDuplicates)
                    {
                        DrawDuplicateOutline(outlineRect);
                    }

                    contentY += row.height;
                    if (rowIndex < rows.Count - 1)
                    {
                        contentY += RowSpacing;
                    }
                }
            }

            EditorGUI.indentLevel = previousIndentLevel;
            EditorGUI.EndProperty();

            bool applied = serializedObject.ApplyModifiedProperties();
            if (applied)
            {
                SyncRuntimeSet(property);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            PaginationState pagination = GetOrCreatePaginationState(property);

            if (!property.isExpanded)
            {
                return height;
            }

            SerializedProperty itemsProperty = property.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            bool hasItemsArray = itemsProperty is { isArray: true };
            int totalCount = hasItemsArray ? itemsProperty.arraySize : 0;

            height += SectionSpacing + GetToolbarHeight() + SectionSpacing;

            EnsurePaginationBounds(pagination, totalCount);

            DuplicateState duplicateState = EvaluateDuplicateState(property, itemsProperty);
            NullEntryState nullState = EvaluateNullEntryState(property, itemsProperty);

            if (nullState.hasNullEntries && !string.IsNullOrEmpty(nullState.summary))
            {
                height += GetWarningBarHeight() + SectionSpacing;
            }

            if (duplicateState.hasDuplicates && !string.IsNullOrEmpty(duplicateState.summary))
            {
                height += GetWarningBarHeight() + SectionSpacing;
            }

            if (!hasItemsArray || totalCount == 0)
            {
                float blockPadding = 6f;
                float messageHeight = EditorGUIUtility.singleLineHeight;
                height += blockPadding * 2f + messageHeight + SectionSpacing;
                return height;
            }
            else
            {
                int startIndex = pagination.page * pagination.pageSize;
                int endIndex = Mathf.Min(startIndex + pagination.pageSize, totalCount);
                float rowsHeight = 0f;
                for (int index = startIndex; index < endIndex; index++)
                {
                    SerializedProperty element = itemsProperty.GetArrayElementAtIndex(index);
                    float elementHeight = EditorGUI.GetPropertyHeight(
                        element,
                        GUIContent.none,
                        true
                    );
                    rowsHeight += elementHeight;
                    if (index < endIndex - 1)
                    {
                        rowsHeight += RowSpacing;
                    }
                }

                float blockPadding = 6f;
                height += blockPadding * 2f + rowsHeight + SectionSpacing;

                return height;
            }
        }

        internal PaginationState GetOrCreatePaginationState(SerializedProperty property)
        {
            string key = property.propertyPath;
            if (!_paginationStates.TryGetValue(key, out PaginationState state))
            {
                state = new PaginationState();
                _paginationStates[key] = state;
            }

            int configuredPageSize = UnityHelpersSettings.GetSerializableSetPageSize();
            if (state.pageSize != configuredPageSize)
            {
                state.pageSize = configuredPageSize;
                state.page = 0;
            }

            return state;
        }

        private void EnsurePaginationBounds(PaginationState state, int totalCount)
        {
            int pageSize = state.pageSize;
            if (totalCount <= 0)
            {
                state.page = 0;
                state.selectedIndex = -1;
                return;
            }

            if (state.selectedIndex >= totalCount)
            {
                state.selectedIndex = totalCount - 1;
            }

            if (state.selectedIndex < 0)
            {
                state.selectedIndex = -1;
            }

            int pageCount = pageSize > 0 ? Mathf.Max(1, (totalCount + pageSize - 1) / pageSize) : 1;

            if (state.page >= pageCount)
            {
                state.page = pageCount - 1;
            }

            if (state.page < 0)
            {
                state.page = 0;
            }

            if (state.selectedIndex >= 0)
            {
                int selectedPage = Mathf.Clamp(state.selectedIndex / pageSize, 0, pageCount - 1);
                state.page = selectedPage;
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
            if (!TryGetSetInspector(property, propertyPath, out _))
            {
                return;
            }

            float lineHeight = EditorGUIUtility.singleLineHeight;

            Rect firstRowRect = new(rect.x, rect.y, rect.width, lineHeight);
            Rect secondRowRect = new(
                rect.x,
                firstRowRect.yMax + RowSpacing,
                rect.width,
                lineHeight
            );

            int totalCount = itemsProperty is { isArray: true } ? itemsProperty.arraySize : 0;

            Rect addRect = new(firstRowRect.x, firstRowRect.y, 60f, lineHeight);
            if (GUI.Button(addRect, AddEntryContent, AddButtonStyle))
            {
                if (TryAddNewElement(ref property, propertyPath, ref itemsProperty, pagination))
                {
                    itemsProperty = property.FindPropertyRelative(
                        SerializableHashSetSerializedPropertyNames.Items
                    );
                    totalCount = itemsProperty is { isArray: true } ? itemsProperty.arraySize : 0;
                    EnsurePaginationBounds(pagination, totalCount);
                }
            }

            float nextX = addRect.xMax + ButtonSpacing;
            Rect clearRect = new(nextX, firstRowRect.y, 80f, lineHeight);
            bool hasEntries = totalCount > 0;
            GUIStyle clearButtonStyle = hasEntries
                ? ClearAllActiveButtonStyle
                : ClearAllInactiveButtonStyle;

            if (GUI.Button(clearRect, ClearAllContent, clearButtonStyle) && hasEntries)
            {
                if (TryClearSet(ref property, propertyPath, ref itemsProperty))
                {
                    itemsProperty = property.FindPropertyRelative(
                        SerializableHashSetSerializedPropertyNames.Items
                    );
                    totalCount = itemsProperty is { isArray: true } ? itemsProperty.arraySize : 0;
                    pagination.page = 0;
                    pagination.selectedIndex = -1;
                    EnsurePaginationBounds(pagination, totalCount);
                }
            }

            nextX = clearRect.xMax + ButtonSpacing;
            if (isSortedSet)
            {
                Rect sortRect = new(nextX, firstRowRect.y, 60f, lineHeight);
                bool canSortElements = CanSortElements(itemsProperty);
                bool needsSorting = canSortElements && NeedsSorting(itemsProperty);
                GUIStyle sortStyle = needsSorting ? SortActiveButtonStyle : SortInactiveButtonStyle;
                if (GUI.Button(sortRect, SortContent, sortStyle) && needsSorting)
                {
                    if (TrySortElements(ref property, propertyPath, itemsProperty))
                    {
                        itemsProperty = property.FindPropertyRelative(
                            SerializableHashSetSerializedPropertyNames.Items
                        );
                        totalCount = itemsProperty is { isArray: true }
                            ? itemsProperty.arraySize
                            : 0;
                        EnsurePaginationBounds(pagination, totalCount);
                    }
                }
            }

            totalCount = itemsProperty is { isArray: true } ? itemsProperty.arraySize : 0;
            DrawToolbarSecondaryRow(
                secondRowRect,
                ref property,
                propertyPath,
                ref itemsProperty,
                pagination,
                totalCount
            );
        }

        private void DrawToolbarSecondaryRow(
            Rect rect,
            ref SerializedProperty property,
            string propertyPath,
            ref SerializedProperty itemsProperty,
            PaginationState pagination,
            int totalCount
        )
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float rightCursor = rect.xMax;
            float availableWidth = rect.width;

            bool hasSelection =
                totalCount > 0
                && pagination.selectedIndex >= 0
                && pagination.selectedIndex < totalCount;

            float removeButtonWidth = Mathf.Max(18f, PaginationButtonWidth - 8f);
            if (hasSelection)
            {
                Rect removeRect = new(
                    rightCursor - removeButtonWidth,
                    rect.y,
                    removeButtonWidth,
                    lineHeight
                );
                if (GUI.Button(removeRect, "-", RemoveButtonStyle))
                {
                    TryRemoveSelectedEntry(
                        ref property,
                        propertyPath,
                        ref itemsProperty,
                        pagination
                    );
                    totalCount = itemsProperty is { isArray: true } ? itemsProperty.arraySize : 0;
                }

                rightCursor = removeRect.x - ButtonSpacing;
                availableWidth = Mathf.Max(0f, rightCursor - rect.x);
            }

            float navigationWidth = PaginationButtonWidth * 4f + ButtonSpacing * 3f;
            bool showNavigation = availableWidth >= navigationWidth;
            if (showNavigation)
            {
                Rect navigationRect = new(
                    rightCursor - navigationWidth,
                    rect.y,
                    navigationWidth,
                    lineHeight
                );
                DrawPaginationButtons(navigationRect, pagination, totalCount);
                rightCursor = navigationRect.x - ButtonSpacing;
                availableWidth = Mathf.Max(0f, rightCursor - rect.x);
            }

            GUIContent pageInfoContent = GUIContent.none;
            if (totalCount > 0)
            {
                int pageSize = Mathf.Max(1, pagination.pageSize);
                int pageCount = Mathf.Max(1, (totalCount + pageSize - 1) / pageSize);
                int currentPage = Mathf.Clamp(pagination.page + 1, 1, pageCount);
                pageInfoContent = new GUIContent($"Page {currentPage}/{pageCount}");
            }

            float pageInfoWidth =
                pageInfoContent != GUIContent.none
                    ? EditorStyles.miniLabel.CalcSize(pageInfoContent).x
                    : 0f;
            bool showPageInfo =
                pageInfoContent != GUIContent.none && availableWidth >= pageInfoWidth;
            if (showPageInfo)
            {
                Rect pageInfoRect = new(
                    rightCursor - pageInfoWidth,
                    rect.y,
                    pageInfoWidth,
                    lineHeight
                );
                EditorGUI.LabelField(pageInfoRect, pageInfoContent, EditorStyles.miniLabel);
                rightCursor = pageInfoRect.x - ButtonSpacing;
                availableWidth = Mathf.Max(0f, rightCursor - rect.x);
            }

            GUIContent entriesContent = new($"Entries: {totalCount}");
            float entriesWidth = EditorStyles.miniLabel.CalcSize(entriesContent).x;
            if (entriesWidth <= availableWidth)
            {
                Rect entriesRect = new(rect.x, rect.y, entriesWidth, lineHeight);
                EditorGUI.LabelField(entriesRect, entriesContent, EditorStyles.miniLabel);
            }
        }

        private void DrawPaginationButtons(Rect rect, PaginationState pagination, int totalCount)
        {
            int pageSize = pagination.pageSize;
            int pageCount = pageSize > 0 ? Mathf.Max(1, (totalCount + pageSize - 1) / pageSize) : 1;
            int currentPage = pagination.page;

            Rect firstRect = new(rect.x, rect.y, PaginationButtonWidth, rect.height);
            Rect prevRect = new(
                firstRect.xMax + ButtonSpacing,
                rect.y,
                PaginationButtonWidth,
                rect.height
            );
            Rect nextRect = new(
                prevRect.xMax + ButtonSpacing,
                rect.y,
                PaginationButtonWidth,
                rect.height
            );
            Rect lastRect = new(
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
                    SnapSelectionToPage(pagination, totalCount);
                }

                if (GUI.Button(prevRect, PreviousPageContent, EditorStyles.miniButton))
                {
                    pagination.page = Mathf.Max(0, pagination.page - 1);
                    SnapSelectionToPage(pagination, totalCount);
                }
            }

            using (new EditorGUI.DisabledScope(currentPage >= pageCount - 1))
            {
                if (GUI.Button(nextRect, NextPageContent, EditorStyles.miniButton))
                {
                    pagination.page = Mathf.Min(pageCount - 1, pagination.page + 1);
                    SnapSelectionToPage(pagination, totalCount);
                }

                if (GUI.Button(lastRect, LastPageContent, EditorStyles.miniButton))
                {
                    pagination.page = pageCount - 1;
                    SnapSelectionToPage(pagination, totalCount);
                }
            }
        }

        private static void SnapSelectionToPage(PaginationState pagination, int totalCount)
        {
            if (totalCount <= 0)
            {
                pagination.selectedIndex = -1;
                return;
            }

            int pageSize = Mathf.Max(1, pagination.pageSize);
            int pageStart = pagination.page * pageSize;
            if (pageStart >= totalCount)
            {
                pageStart = Mathf.Max(0, totalCount - 1);
            }

            pagination.selectedIndex = Mathf.Clamp(pageStart, 0, totalCount - 1);
        }

        private static float GetDuplicateShakeOffset(
            DuplicateState state,
            int index,
            int cycleLimit
        )
        {
            if (
                UnityHelpersSettings.GetDuplicateRowAnimationMode()
                    != UnityHelpersSettings.DuplicateRowAnimationMode.Tween
                || !state.animationStartTimes.TryGetValue(index, out double startTime)
            )
            {
                return 0f;
            }

            double elapsed = EditorApplication.timeSinceStartup - startTime;
            if (elapsed <= 0d)
            {
                return 0f;
            }

            float offset = (float)(
                Math.Sin(elapsed * DuplicateShakeFrequency) * DuplicateShakeAmplitude
            );

            if (cycleLimit >= 0)
            {
                double cycles = elapsed * DuplicateShakeFrequency / (2d * Math.PI);
                if (cycles >= cycleLimit)
                {
                    return 0f;
                }
            }

            return offset;
        }

        internal static Rect ExpandRowRectVertically(Rect rect)
        {
            rect.yMin -= 1f;
            rect.yMax += 1f;
            return rect;
        }

        private static void DrawDuplicateOutline(Rect rect)
        {
            Rect top = new(rect.x, rect.y, rect.width, DuplicateOutlineThickness);
            Rect bottom = new(
                rect.x,
                rect.yMax - DuplicateOutlineThickness,
                rect.width,
                DuplicateOutlineThickness
            );
            Rect left = new(rect.x, rect.y, DuplicateOutlineThickness, rect.height);
            Rect right = new(
                rect.xMax - DuplicateOutlineThickness,
                rect.y,
                DuplicateOutlineThickness,
                rect.height
            );

            EditorGUI.DrawRect(top, DuplicateOutlineColor);
            EditorGUI.DrawRect(bottom, DuplicateOutlineColor);
            EditorGUI.DrawRect(left, DuplicateOutlineColor);
            EditorGUI.DrawRect(right, DuplicateOutlineColor);
        }

        private static float GetWarningBarHeight()
        {
            return EditorGUIUtility.singleLineHeight * 1.6f;
        }

        private static void DrawNullEntryTooltip(Rect rect, string tooltip)
        {
            if (string.IsNullOrEmpty(tooltip) || Event.current.type != EventType.Repaint)
            {
                return;
            }

            NullEntryTooltipContent.text = string.Empty;
            NullEntryTooltipContent.image = null;
            NullEntryTooltipContent.tooltip = tooltip;
            GUI.Label(rect, NullEntryTooltipContent, GUIStyle.none);
        }

        private static bool ElementTypeSupportsNull(Type type)
        {
            return type != null
                && (!type.IsValueType || typeof(UnityEngine.Object).IsAssignableFrom(type));
        }

        private static string BuildNullEntrySummary(List<int> indices)
        {
            if (indices == null || indices.Count == 0)
            {
                return string.Empty;
            }

            indices.Sort();

            if (indices.Count == 1)
            {
                return $"Null entry detected at index {indices[0]}. Value will be ignored at runtime.";
            }

            const int maxDisplay = 5;
            int displayCount = Math.Min(indices.Count, maxDisplay);

            StringBuilder builder = new();
            builder.Append("Null entries detected at indices ");

            for (int i = 0; i < displayCount; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(indices[i]);
            }

            if (indices.Count > maxDisplay)
            {
                builder.Append(", ... (");
                builder.Append(indices.Count - maxDisplay);
                builder.Append(" more)");
            }

            builder.Append(". Values will be ignored at runtime.");
            return builder.ToString();
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

        internal NullEntryState EvaluateNullEntryState(
            SerializedProperty property,
            SerializedProperty itemsProperty
        )
        {
            string key = property.propertyPath;
            if (!_nullEntryStates.TryGetValue(key, out NullEntryState state))
            {
                state = new NullEntryState();
                _nullEntryStates[key] = state;
            }

            bool hasEvent = Event.current != null;
            EventType eventType = hasEvent ? Event.current.type : EventType.Repaint;
            if (eventType != EventType.Repaint)
            {
                return state;
            }

            state.nullIndices.Clear();
            state.tooltips.Clear();
            state.summary = string.Empty;
            state.hasNullEntries = false;
            state.scratch.Clear();

            if (
                itemsProperty == null
                || !itemsProperty.isArray
                || itemsProperty.arraySize == 0
                || !TryGetSetInspector(
                    property,
                    property.propertyPath,
                    out ISerializableSetInspector inspector
                )
            )
            {
                return state;
            }

            Type elementType = inspector.ElementType;
            if (!ElementTypeSupportsNull(elementType))
            {
                return state;
            }

            int count = itemsProperty.arraySize;
            for (int index = 0; index < count; index++)
            {
                SerializedProperty element = itemsProperty.GetArrayElementAtIndex(index);
                SetElementData data = ReadElementData(element);
                if (ReferenceEquals(data.value, null))
                {
                    state.nullIndices.Add(index);
                    state.tooltips[index] =
                        $"Null entry detected at index {index}. Value will be ignored at runtime.";
                    state.scratch.Add(index);
                }
            }

            if (state.nullIndices.Count > 0)
            {
                state.hasNullEntries = true;
                state.summary = BuildNullEntrySummary(state.scratch);
            }
            else if (_nullEntryStates.ContainsKey(key))
            {
                _nullEntryStates.Remove(key);
            }

            return state;
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

            if (state.grouping.Count > 0)
            {
                state.groupingKeysScratch.Clear();
                foreach (KeyValuePair<object, List<int>> bucket in state.grouping)
                {
                    bucket.Value.Clear();
                    state.listPool.Push(bucket.Value);
                    state.groupingKeysScratch.Add(bucket.Key);
                }

                for (int i = 0; i < state.groupingKeysScratch.Count; i++)
                {
                    state.grouping.Remove(state.groupingKeysScratch[i]);
                }

                state.groupingKeysScratch.Clear();
            }

            bool hasEvent = Event.current != null;
            EventType eventType = hasEvent ? Event.current.type : EventType.Repaint;
            if (!force && eventType != EventType.Repaint)
            {
                return state;
            }

            state.duplicateIndices.Clear();
            state.primaryFlags.Clear();
            state.summaryBuilder.Clear();
            state.summary = string.Empty;
            state.hasDuplicates = false;

            if (itemsProperty == null || !itemsProperty.isArray || itemsProperty.arraySize <= 1)
            {
                state.animationStartTimes.Clear();
                return state;
            }

            int count = itemsProperty.arraySize;
            for (int index = 0; index < count; index++)
            {
                SerializedProperty element = itemsProperty.GetArrayElementAtIndex(index);
                SetElementData data = ReadElementData(element);
                object keyValue = data.comparable ?? NullComparable;

                if (!state.grouping.TryGetValue(keyValue, out List<int> list))
                {
                    list = state.listPool.Count > 0 ? state.listPool.Pop() : new List<int>(4);
                    state.grouping[keyValue] = list;
                }

                list.Add(index);
            }

            UnityHelpersSettings.DuplicateRowAnimationMode animationMode =
                UnityHelpersSettings.GetDuplicateRowAnimationMode();
            bool animateDuplicates =
                animationMode == UnityHelpersSettings.DuplicateRowAnimationMode.Tween;
            double now = animateDuplicates ? EditorApplication.timeSinceStartup : 0d;

            int duplicateGroupCount = 0;
            state.groupingKeysScratch.Clear();
            state.groupingKeysScratch.AddRange(state.grouping.Keys);

            for (int keyIndex = 0; keyIndex < state.groupingKeysScratch.Count; keyIndex++)
            {
                object groupingKey = state.groupingKeysScratch[keyIndex];
                if (!state.grouping.TryGetValue(groupingKey, out List<int> indices))
                {
                    continue;
                }

                if (indices.Count <= 1)
                {
                    indices.Clear();
                    state.listPool.Push(indices);
                    state.grouping.Remove(groupingKey);
                    continue;
                }

                indices.Sort();
                duplicateGroupCount++;
                state.hasDuplicates = true;

                bool isPrimary = true;
                foreach (int duplicateIndex in indices)
                {
                    state.duplicateIndices.Add(duplicateIndex);
                    state.primaryFlags[duplicateIndex] = isPrimary;
                    isPrimary = false;

                    if (
                        animateDuplicates
                        && (force || !state.animationStartTimes.ContainsKey(duplicateIndex))
                    )
                    {
                        state.animationStartTimes[duplicateIndex] = now;
                    }
                }

                if (state.summaryBuilder.Length > 0)
                {
                    state.summaryBuilder.AppendLine();
                }

                if (duplicateGroupCount <= 5)
                {
                    state.summaryBuilder.Append("Value ");
                    state.summaryBuilder.Append(ConvertDuplicateKeyToString(groupingKey));
                    state.summaryBuilder.Append(" at indices ");
                    AppendIndexList(state.summaryBuilder, indices);
                }

                indices.Clear();
                state.listPool.Push(indices);
                state.grouping.Remove(groupingKey);
            }

            state.groupingKeysScratch.Clear();

            if (duplicateGroupCount > 5)
            {
                if (state.summaryBuilder.Length > 0)
                {
                    state.summaryBuilder.AppendLine();
                }

                state.summaryBuilder.Append("Additional duplicate groups omitted for brevity.");
            }

            if (state.animationStartTimes.Count > 0)
            {
                state.animationKeysScratch.Clear();
                state.animationKeysScratch.AddRange(state.animationStartTimes.Keys);
                for (int i = 0; i < state.animationKeysScratch.Count; i++)
                {
                    int trackedIndex = state.animationKeysScratch[i];
                    if (!state.duplicateIndices.Contains(trackedIndex) || !animateDuplicates)
                    {
                        state.animationStartTimes.Remove(trackedIndex);
                    }
                }
                state.animationKeysScratch.Clear();
            }

            if (!animateDuplicates)
            {
                state.animationStartTimes.Clear();
            }

            if (state.hasDuplicates)
            {
                state.summary =
                    state.summaryBuilder.Length > 0
                        ? state.summaryBuilder.ToString()
                        : "Duplicate values detected.";
            }
            else
            {
                state.summary = string.Empty;
            }

            state.grouping.Clear();

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
                _ => key.ToString(),
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
            bool matchesFieldType =
                fieldType != null
                && TypeMatchesGenericDefinition(fieldType, typeof(SerializableSortedSet<>));
            return matchesFieldType || IsRuntimeSortedSet(property);
        }

        private static bool IsRuntimeSortedSet(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            object instance = GetSetInstance(property, property.propertyPath);
            if (instance == null)
            {
                return false;
            }

            Type instanceType = instance.GetType();
            return TypeMatchesGenericDefinition(instanceType, typeof(SerializableSortedSet<>));
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

        private static bool NeedsSorting(SerializedProperty itemsProperty)
        {
            if (!CanSortElements(itemsProperty))
            {
                return false;
            }

            int count = itemsProperty.arraySize;
            SetElementData previous = default;
            bool hasPrevious = false;

            for (int index = 0; index < count; index++)
            {
                SerializedProperty elementProperty = itemsProperty.GetArrayElementAtIndex(index);
                SetElementData current = ReadElementData(elementProperty);

                if (hasPrevious)
                {
                    int comparison = CompareComparableValues(
                        previous.comparable,
                        current.comparable
                    );
                    if (comparison > 0)
                    {
                        return true;
                    }

                    if (comparison == 0)
                    {
                        string previousFallback =
                            previous.value != null ? previous.value.ToString() : string.Empty;
                        string currentFallback =
                            current.value != null ? current.value.ToString() : string.Empty;
                        if (string.CompareOrdinal(previousFallback, currentFallback) > 0)
                        {
                            return true;
                        }
                    }
                }

                previous = current;
                hasPrevious = true;
            }

            return false;
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
            if (
                !TryGetSetInspector(property, propertyPath, out ISerializableSetInspector inspector)
            )
            {
                return false;
            }

            Type elementType = inspector.ElementType;
            if (elementType == null)
            {
                return false;
            }

            if (ElementTypeSupportsNull(elementType) && elementType != typeof(string))
            {
                if (
                    AppendNullPlaceholderEntry(
                        ref property,
                        propertyPath,
                        ref itemsProperty,
                        pagination,
                        inspector
                    )
                )
                {
                    return true;
                }
            }

            List<object> existingValues = new();
            if (itemsProperty is { isArray: true })
            {
                for (int index = 0; index < itemsProperty.arraySize; index++)
                {
                    SerializedProperty element = itemsProperty.GetArrayElementAtIndex(index);
                    existingValues.Add(ReadElementData(element).value);
                }
            }
            else
            {
                Array snapshot = inspector.GetSerializedItemsSnapshot();
                if (snapshot is { Length: > 0 })
                {
                    foreach (object value in snapshot)
                    {
                        existingValues.Add(value);
                    }
                }
            }

            foreach (
                object candidate in GenerateCandidateValues(elementType, inspector.UniqueCount)
            )
            {
                if (inspector.ContainsElement(candidate))
                {
                    continue;
                }

                if (!inspector.TryAddElement(candidate, out object normalizedValue))
                {
                    continue;
                }

                SerializedObject serializedObject = property.serializedObject;
                existingValues.Add(normalizedValue);

                Array updated = Array.CreateInstance(elementType, existingValues.Count);
                for (int index = 0; index < existingValues.Count; index++)
                {
                    object coerced = ConvertSnapshotValue(elementType, existingValues[index]);
                    updated.SetValue(coerced, index);
                }

                inspector.SetSerializedItemsSnapshot(updated, preserveSerializedEntries: true);
                inspector.SynchronizeSerializedState();

                serializedObject.Update();
                property = serializedObject.FindProperty(propertyPath);
                itemsProperty = property?.FindPropertyRelative(
                    SerializableHashSetSerializedPropertyNames.Items
                );
                int totalCount = itemsProperty is { isArray: true } ? itemsProperty.arraySize : 0;
                EnsurePaginationBounds(pagination, totalCount);
                EvaluateDuplicateState(property, itemsProperty, force: true);
                EvaluateNullEntryState(property, itemsProperty);
                SyncRuntimeSet(property);
                if (totalCount > 0)
                {
                    pagination.selectedIndex = totalCount - 1;
                }
                return true;
            }

            if (ElementTypeSupportsNull(elementType))
            {
                if (
                    AppendNullPlaceholderEntry(
                        ref property,
                        propertyPath,
                        ref itemsProperty,
                        pagination,
                        inspector
                    )
                )
                {
                    return true;
                }
            }

            Debug.LogWarning("Unable to generate a unique value for this set element type.");
            return false;
        }

        private bool AppendNullPlaceholderEntry(
            ref SerializedProperty property,
            string propertyPath,
            ref SerializedProperty itemsProperty,
            PaginationState pagination,
            ISerializableSetInspector inspector
        )
        {
            Type elementType = inspector.ElementType;
            SerializedObject serializedObject = property.serializedObject;

            Array snapshot = inspector.GetSerializedItemsSnapshot();
            int existingCount = snapshot?.Length ?? inspector.SerializedCount;
            Array expanded = Array.CreateInstance(elementType, existingCount + 1);

            if (snapshot is { Length: > 0 })
            {
                Array.Copy(snapshot, expanded, snapshot.Length);
            }
            else if (itemsProperty is { isArray: true })
            {
                for (
                    int index = 0;
                    index < itemsProperty.arraySize && index < existingCount;
                    index++
                )
                {
                    SerializedProperty element = itemsProperty.GetArrayElementAtIndex(index);
                    object value = ReadElementData(element).value;
                    expanded.SetValue(ConvertSnapshotValue(elementType, value), index);
                }
            }

            expanded.SetValue(null, existingCount);
            inspector.SetSerializedItemsSnapshot(expanded, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();

            serializedObject.Update();
            property = serializedObject.FindProperty(propertyPath);
            itemsProperty = property?.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            int totalCount = itemsProperty is { isArray: true } ? itemsProperty.arraySize : 0;
            EnsurePaginationBounds(pagination, totalCount);
            EvaluateDuplicateState(property, itemsProperty, force: true);
            EvaluateNullEntryState(property, itemsProperty);
            SyncRuntimeSet(property);
            if (totalCount > 0)
            {
                pagination.selectedIndex = totalCount - 1;
            }

            return true;
        }

        internal static void SyncRuntimeSet(SerializedProperty setProperty)
        {
            if (setProperty == null)
            {
                return;
            }

            SerializedObject sharedSerializedObject = setProperty.serializedObject;
            UnityEngine.Object[] targets = sharedSerializedObject.targetObjects;
            string propertyPath = setProperty.propertyPath;

            foreach (UnityEngine.Object target in targets)
            {
                using SerializedObject targetSerializedObject = new(target);
                targetSerializedObject.UpdateIfRequiredOrScript();
                SerializedProperty targetSetProperty = targetSerializedObject.FindProperty(
                    propertyPath
                );
                if (targetSetProperty == null)
                {
                    continue;
                }

                SerializedProperty targetItemsProperty = targetSetProperty.FindPropertyRelative(
                    SerializableHashSetSerializedPropertyNames.Items
                );

                object setInstance = GetTargetObjectOfProperty(target, propertyPath);
                if (setInstance is not ISerializableSetInspector inspector)
                {
                    continue;
                }

                Array snapshot = BuildSnapshotArray(targetItemsProperty, inspector.ElementType);
                inspector.SetSerializedItemsSnapshot(snapshot, preserveSerializedEntries: true);

                if (setInstance is ISerializableSetEditorSync editorSync)
                {
                    editorSync.EditorAfterDeserialize();
                }

                inspector.SynchronizeSerializedState();
                EditorUtility.SetDirty(target);
            }

            sharedSerializedObject.UpdateIfRequiredOrScript();
        }

        private bool TryClearSet(
            ref SerializedProperty property,
            string propertyPath,
            ref SerializedProperty itemsProperty
        )
        {
            if (
                !TryGetSetInspector(property, propertyPath, out ISerializableSetInspector inspector)
            )
            {
                return false;
            }

            inspector.ClearElements();
            inspector.SynchronizeSerializedState();
            property.serializedObject.Update();
            property = property.serializedObject.FindProperty(propertyPath);
            itemsProperty = property?.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            SyncRuntimeSet(property);
            return true;
        }

        private void TryRemoveSelectedEntry(
            ref SerializedProperty property,
            string propertyPath,
            ref SerializedProperty itemsProperty,
            PaginationState pagination
        )
        {
            if (itemsProperty == null || !itemsProperty.isArray)
            {
                return;
            }

            int targetIndex = pagination.selectedIndex;
            if (targetIndex < 0 || targetIndex >= itemsProperty.arraySize)
            {
                return;
            }

            SerializedProperty element = itemsProperty.GetArrayElementAtIndex(targetIndex);
            SetElementData elementData = ReadElementData(element);
            RemoveEntry(itemsProperty, targetIndex);
            RemoveValueFromSet(property, propertyPath, elementData.value);
            property.serializedObject.ApplyModifiedProperties();
            SyncRuntimeSet(property);
            property.serializedObject.Update();
            property = property.serializedObject.FindProperty(propertyPath);
            itemsProperty = property.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            bool hasItemsArray = itemsProperty is { isArray: true };
            int totalCount = hasItemsArray ? itemsProperty.arraySize : 0;
            if (pagination.selectedIndex >= totalCount)
            {
                pagination.selectedIndex = totalCount - 1;
            }
            EnsurePaginationBounds(pagination, totalCount);
            EvaluateDuplicateState(property, itemsProperty, force: true);
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
            List<SetElementData> elements = new(count);

            for (int index = 0; index < count; index++)
            {
                SerializedProperty elementProperty = itemsProperty.GetArrayElementAtIndex(index);
                elements.Add(ReadElementData(elementProperty));
            }

            elements.Sort(
                (left, right) =>
                {
                    int comparison = CompareComparableValues(left.comparable, right.comparable);
                    if (comparison != 0)
                    {
                        return comparison;
                    }

                    string leftFallback = left.value != null ? left.value.ToString() : string.Empty;
                    string rightFallback =
                        right.value != null ? right.value.ToString() : string.Empty;
                    return string.CompareOrdinal(leftFallback, rightFallback);
                }
            );

            for (int index = 0; index < count; index++)
            {
                SerializedProperty elementProperty = itemsProperty.GetArrayElementAtIndex(index);
                WriteElementValue(elementProperty, elements[index]);
            }

            if (TryGetSetInspector(property, propertyPath, out ISerializableSetInspector inspector))
            {
                inspector.ClearElements();
                foreach (SetElementData element in elements)
                {
                    inspector.TryAddElement(element.value, out _);
                }

                inspector.SynchronizeSerializedState();
            }

            property.serializedObject.Update();
            property = property.serializedObject.FindProperty(propertyPath);
            SyncRuntimeSet(property);
            return true;
        }

        private void RemoveValueFromSet(
            SerializedProperty property,
            string propertyPath,
            object value
        )
        {
            if (
                !TryGetSetInspector(property, propertyPath, out ISerializableSetInspector inspector)
            )
            {
                return;
            }

            if (inspector.RemoveElement(value))
            {
                inspector.SynchronizeSerializedState();
            }
        }

        private static object GetSetInstance(SerializedProperty property, string propertyPath)
        {
            return GetTargetObjectOfProperty(property.serializedObject.targetObject, propertyPath);
        }

        private bool TryGetSetInspector(
            SerializedProperty property,
            string propertyPath,
            out ISerializableSetInspector inspector
        )
        {
            object setInstance = GetSetInstance(property, propertyPath);
            inspector = setInstance as ISerializableSetInspector;
            return inspector != null;
        }

        private static IEnumerable<object> GenerateCandidateValues(
            Type elementType,
            int existingCount
        )
        {
            if (elementType == typeof(string))
            {
                yield return string.Empty;
                for (int i = 1; i < MaxAutoAddAttempts; i++)
                {
                    yield return $"New Entry {i}";
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
                    Bounds bounds = new(new Vector3(i + 1f, 0f, 0f), Vector3.one);
                    yield return bounds;
                }
                yield break;
            }

            if (elementType == typeof(BoundsInt))
            {
                for (int i = 0; i < MaxAutoAddAttempts; i++)
                {
                    BoundsInt bounds = new(new Vector3Int(i + 1, 0, 0), Vector3Int.one);
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
            try
            {
                for (int i = 0; i <= index; i++)
                {
                    if (!enumerator.MoveNext())
                    {
                        return null;
                    }
                }

                return enumerator.Current;
            }
            finally
            {
                if (enumerator is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
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

            string leftString = left.ToString();
            string rightString = right.ToString();
            return string.CompareOrdinal(leftString, rightString);
        }

        private static SetElementData ReadElementData(SerializedProperty property)
        {
            SetElementData data = new()
            {
                propertyType = property.propertyType,
                comparable = null,
                value = null,
            };

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.ArraySize:
                    long longValue = property.longValue;
                    data.value = longValue;
                    data.comparable = longValue;
                    break;
                case SerializedPropertyType.Boolean:
                    bool boolValue = property.boolValue;
                    data.value = boolValue;
                    data.comparable = boolValue ? 1 : 0;
                    break;
                case SerializedPropertyType.Float:
                    double doubleValue = property.doubleValue;
                    data.value = doubleValue;
                    data.comparable = doubleValue;
                    break;
                case SerializedPropertyType.String:
                    string stringValue = property.stringValue ?? string.Empty;
                    data.value = stringValue;
                    data.comparable = stringValue;
                    break;
                case SerializedPropertyType.Color:
                    Color colorValue = property.colorValue;
                    data.value = colorValue;
                    data.comparable = colorValue;
                    break;
                case SerializedPropertyType.Vector2:
                    Vector2 vector2Value = property.vector2Value;
                    data.value = vector2Value;
                    data.comparable = vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    Vector3 vector3Value = property.vector3Value;
                    data.value = vector3Value;
                    data.comparable = vector3Value;
                    break;
                case SerializedPropertyType.Vector4:
                    Vector4 vector4Value = property.vector4Value;
                    data.value = vector4Value;
                    data.comparable = vector4Value;
                    break;
                case SerializedPropertyType.Rect:
                    Rect rectValue = property.rectValue;
                    data.value = rectValue;
                    data.comparable = rectValue;
                    break;
                case SerializedPropertyType.Bounds:
                    Bounds boundsValue = property.boundsValue;
                    data.value = boundsValue;
                    data.comparable = boundsValue;
                    break;
                case SerializedPropertyType.Vector2Int:
                    Vector2Int vector2IntValue = property.vector2IntValue;
                    data.value = vector2IntValue;
                    data.comparable = vector2IntValue;
                    break;
                case SerializedPropertyType.Vector3Int:
                    Vector3Int vector3IntValue = property.vector3IntValue;
                    data.value = vector3IntValue;
                    data.comparable = vector3IntValue;
                    break;
                case SerializedPropertyType.RectInt:
                    RectInt rectIntValue = property.rectIntValue;
                    data.value = rectIntValue;
                    data.comparable = rectIntValue;
                    break;
                case SerializedPropertyType.BoundsInt:
                    BoundsInt boundsIntValue = property.boundsIntValue;
                    data.value = boundsIntValue;
                    data.comparable = boundsIntValue;
                    break;
                case SerializedPropertyType.Hash128:
                    Hash128 hashValue = property.hash128Value;
                    data.value = hashValue;
                    data.comparable = hashValue;
                    break;
                case SerializedPropertyType.Quaternion:
                    Quaternion quaternionValue = property.quaternionValue;
                    data.value = quaternionValue;
                    data.comparable = quaternionValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    UnityEngine.Object objectReferenceValue = property.objectReferenceValue;
                    data.value = objectReferenceValue;
                    data.comparable =
                        objectReferenceValue != null
                            ? objectReferenceValue.GetInstanceID()
                            : NullComparable;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    AnimationCurve curveValue = property.animationCurveValue;
                    data.value = curveValue;
                    data.comparable = curveValue?.length ?? 0;
                    break;
                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.Generic:
                    try
                    {
                        object boxed = property.boxedValue;
                        data.value = boxed;
                        data.comparable = boxed ?? NullComparable;
                    }
                    catch (Exception)
                    {
                        data.value = property.propertyPath;
                        data.comparable = property.propertyPath;
                    }
                    break;
                default:
                    data.value = property.propertyPath;
                    data.comparable = property.propertyPath;
                    break;
            }

            return data;
        }

        private static Array BuildSnapshotArray(SerializedProperty itemsProperty, Type elementType)
        {
            elementType ??= typeof(object);

            if (itemsProperty == null || !itemsProperty.isArray)
            {
                return Array.CreateInstance(elementType, 0);
            }

            int count = itemsProperty.arraySize;
            Array snapshot = Array.CreateInstance(elementType, count);

            for (int index = 0; index < count; index++)
            {
                SerializedProperty element = itemsProperty.GetArrayElementAtIndex(index);
                SetElementData elementData = ReadElementData(element);
                object value = ConvertSnapshotValue(elementType, elementData.value);
                snapshot.SetValue(value, index);
            }

            return snapshot;
        }

        private static object ConvertSnapshotValue(Type elementType, object value)
        {
            if (value == null)
            {
                return null;
            }

            if (elementType == null)
            {
                return value;
            }

            Type nullableUnderlying = Nullable.GetUnderlyingType(elementType);
            if (nullableUnderlying != null)
            {
                return ConvertSnapshotValue(nullableUnderlying, value);
            }

            if (elementType.IsInstanceOfType(value))
            {
                return value;
            }

            Type targetType = elementType;

            try
            {
                if (targetType.IsEnum)
                {
                    if (value is string enumName)
                    {
                        return Enum.Parse(targetType, enumName, ignoreCase: true);
                    }

                    return Enum.ToObject(targetType, value);
                }

                if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(targetType))
                {
                    return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                }
            }
            catch (Exception)
            {
                return value;
            }

            return value;
        }

        private static void WriteElementValue(SerializedProperty property, SetElementData data)
        {
            switch (data.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.ArraySize:
                    property.longValue = Convert.ToInt64(data.value);
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = Convert.ToBoolean(data.value);
                    break;
                case SerializedPropertyType.Float:
                    property.doubleValue = Convert.ToDouble(data.value);
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = data.value as string ?? string.Empty;
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = data.value is Color color ? color : Color.white;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = data.value is Vector2 vector2 ? vector2 : Vector2.zero;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = data.value is Vector3 vector3 ? vector3 : Vector3.zero;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = data.value is Vector4 vector4 ? vector4 : Vector4.zero;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = data.value is Rect rect ? rect : default;
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = data.value is Bounds bounds ? bounds : default;
                    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = data.value is Vector2Int vector2Int
                        ? vector2Int
                        : default;
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = data.value is Vector3Int vector3Int
                        ? vector3Int
                        : default;
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = data.value is RectInt rectInt ? rectInt : default;
                    break;
                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = data.value is BoundsInt boundsInt
                        ? boundsInt
                        : default;
                    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = data.value is Quaternion quaternion
                        ? quaternion
                        : Quaternion.identity;
                    break;
                case SerializedPropertyType.Hash128:
                    property.hash128Value = data.value is Hash128 hash ? hash : default;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = data.value as UnityEngine.Object;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = data.value as AnimationCurve;
                    break;
                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.Generic:
                    try
                    {
                        property.boxedValue = data.value;
                    }
                    catch (Exception)
                    {
                        // Swallow
                    }
                    break;
            }
        }

        private static string BuildFoldoutLabel(GUIContent label)
        {
            string baseLabel = label != null ? label.text : "Serialized HashSet";
            return baseLabel;
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
            while (true)
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
                    segment != "Array"
                    || index + 1 >= path.Length
                    || !path[index + 1].StartsWith("data[", StringComparison.Ordinal)
                )
                {
                    rootType = fieldType;
                    index += 1;
                    continue;
                }

                if (fieldType.IsArray)
                {
                    rootType = fieldType.GetElementType();
                    index += 2;
                    continue;
                }

                if (fieldType.IsGenericType)
                {
                    Type[] arguments = fieldType.GetGenericArguments();
                    if (arguments.Length == 1)
                    {
                        rootType = arguments[0];
                        index += 2;
                        continue;
                    }
                }

                return fieldType;
            }
        }
    }
}

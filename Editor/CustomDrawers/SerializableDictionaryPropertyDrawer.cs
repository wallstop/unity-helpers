namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Text;
    using UnityEditor;
    using UnityEditor.AnimatedValues;
    using UnityEditorInternal;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Utils;
    using Object = UnityEngine.Object;

    [CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
    [CustomPropertyDrawer(typeof(SerializableSortedDictionary<,>), true)]
    [CustomPropertyDrawer(typeof(SerializableSortedDictionary<,,>), true)]
    public sealed class SerializableDictionaryPropertyDrawer : PropertyDrawer
    {
        private readonly Dictionary<string, ReorderableList> _lists = new();
        private readonly Dictionary<string, PendingEntry> _pendingEntries = new();
        private readonly Dictionary<string, PaginationState> _paginationStates = new();
        private readonly Dictionary<string, ListPageCache> _pageCaches = new();
        private readonly Dictionary<string, KeyIndexCache> _keyIndexCaches = new();
        private readonly Dictionary<string, DuplicateKeyState> _duplicateStates = new();
        private readonly Dictionary<string, NullKeyState> _nullKeyStates = new();
        private readonly Dictionary<string, Type> _valueTypes = new();
        internal Rect LastResolvedPosition { get; private set; }
        internal Rect LastListRect { get; private set; }
        internal bool HasLastListRect { get; private set; }
        private static readonly BindingFlags ReflectionBindingFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly char[] PropertyPathSeparators = { '.' };

        private const float PendingSectionPadding = 6f;
        internal const float PendingFoldoutToggleOffset = 12f;
        internal const float PendingFoldoutToggleOffsetProjectSettings = 2f;
        internal const float PendingFoldoutLabelPadding = 4f;
        internal const float DictionaryRowFieldPadding = 4f;
        internal const float ExpandableValueFoldoutLabelWidth = 16f;
        internal const float PendingExpandableValueFoldoutGutter = 8f;
        internal const float RowExpandableValueFoldoutGutter = 32f;
        private const float PendingAddButtonWidth = 110f;
        private const int DefaultPageSize =
            UnityHelpersSettings.DefaultSerializableDictionaryPageSize;

        private const int MaxPageSize = UnityHelpersSettings.MaxSerializableDictionaryPageSize;
        private const int DuplicateSummaryDisplayLimit = 5;
        private const float PaginationButtonWidth = 28f;
        private const float PaginationLabelWidth = 80f;
        private const float PaginationControlSpacing = 4f;
        private const float DuplicateShakeAmplitude = 2f;
        private const float DuplicateShakeFrequency = 7f;
        private const float DuplicateBorderThickness = 1f;
        private static readonly Color LightRowColor = new(0.97f, 0.97f, 0.97f, 1f);
        private static readonly Color DarkRowColor = new(0.16f, 0.16f, 0.16f, 0.45f);
        private static readonly Color LightSelectionColor = new(0.33f, 0.62f, 0.95f, 0.65f);
        private static readonly Color DarkSelectionColor = new(0.2f, 0.45f, 0.85f, 0.7f);
        private static readonly ConcurrentDictionary<
            Type,
            Func<object>
        > ParameterlessConstructorCache = new();
        private static readonly ConcurrentDictionary<Type, byte> UnsupportedParameterlessTypes =
            new();
        private static readonly Color DuplicatePrimaryColor = new(0.99f, 0.82f, 0.35f, 0.55f);
        private static readonly Color DuplicateSecondaryColor = new(0.96f, 0.45f, 0.45f, 0.65f);
        private static readonly Color DuplicateOutlineColor = new(0.65f, 0.18f, 0.18f, 0.9f);
        private static readonly Color NullKeyHighlightColor = new(0.84f, 0.2f, 0.2f, 0.6f);
        private static readonly Dictionary<Type, PaletteValueRenderer> PaletteValueRenderers =
            BuildPaletteValueRenderers();
        private static readonly GUIContent DuplicateTooltipContent = new();
        private static readonly GUIContent DuplicateIconContentCache = new();
        private static readonly GUIContent NullKeyTooltipContent = new();
        private static readonly GUIContent NullKeyIconContentCache = new();
        private static readonly GUIContent DuplicateIconTemplate = EditorGUIUtility.IconContent(
            "console.warnicon.sml"
        );
        private static readonly GUIContent PendingFoldoutContent = EditorGUIUtility.TrTextContent(
            "New Entry"
        );
        private static GUIStyle _footerLabelStyle;
        private static GUIStyle _pendingFoldoutLabelStyle;
        private static readonly GUIContent FoldoutSpacerLabel = new(" ");
        private static readonly object NullKeySentinel = new();

        internal static bool HasLastPendingHeaderRect { get; private set; }
        internal static Rect LastPendingHeaderRect { get; private set; }
        internal static Rect LastPendingFoldoutToggleRect { get; private set; }
        internal static bool HasLastRowRects { get; private set; }
        internal static Rect LastRowOriginalRect { get; private set; }
        internal static Rect LastRowKeyRect { get; private set; }
        internal static Rect LastRowValueRect { get; private set; }
        internal static float LastRowValueBaseX { get; private set; }
        internal static bool LastPendingValueUsedFoldoutLabel { get; private set; }
        internal static bool LastRowValueUsedFoldoutLabel { get; private set; }
        internal static float LastPendingValueFoldoutOffset { get; private set; }
        internal static float LastRowValueFoldoutOffset { get; private set; }

        internal static void ResetLayoutTrackingForTests()
        {
            HasLastPendingHeaderRect = false;
            HasLastRowRects = false;
            LastPendingValueUsedFoldoutLabel = false;
            LastRowValueUsedFoldoutLabel = false;
            LastPendingValueFoldoutOffset = 0f;
            LastRowValueFoldoutOffset = 0f;
        }

        /// <summary>
        /// Draws the expandable dictionary inspector UI, including toolbar actions and inline key/value editing.
        /// </summary>
        /// <param name="position">The rectangle reserved by Unity.</param>
        /// <param name="property">The serialized dictionary being edited.</param>
        /// <param name="label">The label displayed for the field.</param>
        /// <example>
        /// <code>
        /// EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(inventory)));
        /// </code>
        /// </example>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect originalPosition = position;
            Rect contentPosition = ResolveContentRect(originalPosition);
            LastResolvedPosition = contentPosition;
            HasLastListRect = false;

            EditorGUI.BeginProperty(originalPosition, label, property);
            int previousIndentLevel = EditorGUI.indentLevel;

            try
            {
                position = contentPosition;

                SerializedObject serializedObject = property.serializedObject;
                serializedObject.UpdateIfRequiredOrScript();

                SerializedProperty keysProperty = property.FindPropertyRelative(
                    SerializableDictionarySerializedPropertyNames.Keys
                );
                SerializedProperty valuesProperty = property.FindPropertyRelative(
                    SerializableDictionarySerializedPropertyNames.Values
                );
                EnsureParallelArraySizes(keysProperty, valuesProperty);

                if (
                    !TryResolveKeyValueTypes(
                        fieldInfo,
                        out Type keyType,
                        out Type valueType,
                        out bool isSortedDictionary
                    )
                )
                {
                    EditorGUI.LabelField(position, label.text, "Unsupported dictionary type");
                    return;
                }

                string cacheKey = GetListKey(property);
                _valueTypes[cacheKey] = valueType;
                DuplicateKeyState duplicateState = RefreshDuplicateState(
                    cacheKey,
                    keysProperty,
                    keyType
                );
                NullKeyState nullKeyState = RefreshNullKeyState(cacheKey, keysProperty, keyType);

                Rect foldoutRect = new(
                    position.x,
                    position.y,
                    position.width,
                    EditorGUIUtility.singleLineHeight
                );
                property.isExpanded = EditorGUI.Foldout(
                    foldoutRect,
                    property.isExpanded,
                    label,
                    true
                );

                float iconSize = EditorGUIUtility.singleLineHeight;
                float iconPadding = 4f;
                float nextIconX = Mathf.Max(position.x, position.xMax - iconSize - iconPadding);

                if (duplicateState is { HasDuplicates: true })
                {
                    Rect iconRect = new(nextIconX, foldoutRect.y, iconSize, iconSize);
                    GUI.Label(iconRect, GetDuplicateIconContent(duplicateState.SummaryTooltip));
                    nextIconX -= iconSize + iconPadding;
                }

                if (nullKeyState is { HasNullKeys: true })
                {
                    float clampedX = Mathf.Max(position.x, nextIconX);
                    Rect iconRect = new(clampedX, foldoutRect.y, iconSize, iconSize);
                    GUI.Label(iconRect, GetNullKeyIconContent(nullKeyState.WarningMessage));
                }

                float y = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;

                if (property.isExpanded)
                {
                    float warningHeight = GetWarningBarHeight();

                    if (nullKeyState is { HasNullKeys: true })
                    {
                        Rect warningRect = new(position.x, y, position.width, warningHeight);
                        EditorGUI.HelpBox(
                            warningRect,
                            nullKeyState.WarningMessage,
                            MessageType.Warning
                        );
                        y = warningRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                    }

                    if (
                        duplicateState is { HasDuplicates: true }
                        && !string.IsNullOrEmpty(duplicateState.SummaryTooltip)
                    )
                    {
                        Rect warningRect = new(position.x, y, position.width, warningHeight);
                        EditorGUI.HelpBox(
                            warningRect,
                            duplicateState.SummaryTooltip,
                            MessageType.Warning
                        );
                        y = warningRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                    }

                    ReorderableList list = GetOrCreateList(property);
                    PaginationState pagination = GetOrCreatePaginationState(property);
                    PendingEntry pending = GetOrCreatePendingEntry(
                        property,
                        keyType,
                        valueType,
                        isSortedDictionary
                    );

                    float pendingY = y;
                    DrawPendingEntryUI(
                        ref pendingY,
                        position,
                        pending,
                        list,
                        pagination,
                        property,
                        keysProperty,
                        valuesProperty,
                        keyType,
                        valueType
                    );
                    y = pendingY + EditorGUIUtility.standardVerticalSpacing;

                    Rect listRect = new(position.x, y, position.width, list.GetHeight());
                    LastListRect = listRect;
                    HasLastListRect = true;

                    int previousIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;
                    list.DoList(listRect);
                    EditorGUI.indentLevel = previousIndent;
                }

                bool applied = ApplyModifiedPropertiesWithUndoFallback(
                    serializedObject,
                    property,
                    "OnGUI::ListApply"
                );
                if (applied)
                {
                    SyncRuntimeDictionary(property);
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
                EditorGUI.EndProperty();
            }
        }

        private static Rect ResolveContentRect(Rect position)
        {
            const float MinimumGroupIndent = 6f;

            Rect padded = GroupGUIWidthUtility.ApplyCurrentPadding(position);
            Rect indented = EditorGUI.IndentedRect(padded);

            if (EditorGUI.indentLevel <= 0)
            {
                indented.xMin += MinimumGroupIndent;
                indented.width = Mathf.Max(0f, indented.width - MinimumGroupIndent);
            }

            return indented;
        }

        /// <summary>
        /// Calculates how much vertical space is required to render the dictionary, including any expanded entries and validation messages.
        /// </summary>
        /// <param name="property">The serialized dictionary wrapper.</param>
        /// <param name="label">The label shown for the field.</param>
        /// <returns>The height Unity should reserve for the drawer.</returns>
        /// <example>
        /// <code>
        /// float height = drawer.GetPropertyHeight(property, GUIContent.none);
        /// </code>
        /// </example>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded)
            {
                return height;
            }

            SerializedProperty keysProperty = property.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = property.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );
            EnsureParallelArraySizes(keysProperty, valuesProperty);
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            if (
                !TryResolveKeyValueTypes(
                    fieldInfo,
                    out Type keyType,
                    out Type valueType,
                    out bool isSortedDictionary
                )
            )
            {
                return height;
            }

            PendingEntry pending = GetOrCreatePendingEntry(
                property,
                keyType,
                valueType,
                isSortedDictionary
            );
            string cacheKey = GetListKey(property);
            DuplicateKeyState duplicateState = RefreshDuplicateState(
                cacheKey,
                keysProperty,
                keyType
            );
            NullKeyState nullKeyState = RefreshNullKeyState(cacheKey, keysProperty, keyType);

            height += spacing;

            float warningHeight = GetWarningBarHeight();
            if (nullKeyState is { HasNullKeys: true })
            {
                height += warningHeight + spacing;
            }

            if (
                duplicateState is { HasDuplicates: true }
                && !string.IsNullOrEmpty(duplicateState.SummaryTooltip)
            )
            {
                height += warningHeight + spacing;
            }

            float pendingHeight = GetPendingSectionHeight(pending, keyType, valueType);
            height += pendingHeight + spacing;

            ReorderableList list = GetOrCreateList(property);
            float listHeight = list.GetHeight();
            height += listHeight;
            return height;
        }

        internal ReorderableList GetOrCreateList(SerializedProperty dictionaryProperty)
        {
            string key = GetListKey(dictionaryProperty);
            _valueTypes.TryGetValue(key, out Type resolvedValueType);
            PaginationState pagination = GetOrCreatePaginationState(dictionaryProperty);

            SerializedProperty keysProperty = ResolveKeysProperty();
            SerializedProperty valuesProperty = ResolveValuesProperty();
            ClampPaginationState(pagination, keysProperty.arraySize);
            float defaultRowHeight =
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2f;
            float emptyHeight = Mathf.Max(
                EditorGUIUtility.standardVerticalSpacing * 2f,
                EditorGUIUtility.standardVerticalSpacing
            );

            Func<ListPageCache> cacheProvider = () =>
                EnsurePageCache(key, ResolveKeysProperty(), pagination);

            ListPageCache cache = cacheProvider();

            if (_lists.TryGetValue(key, out ReorderableList cached))
            {
                cached.list = cache.entries;
                SyncListSelectionWithPagination(cached, pagination, cache);
                cached.drawNoneElementCallback = _ => { };
                cached.elementHeight =
                    ResolveKeysProperty().arraySize == 0 ? emptyHeight : defaultRowHeight;
                cached.elementHeightCallback = ResolveRowHeight;
                return cached;
            }

            ReorderableList list = new(
                cache.entries,
                typeof(PageEntry),
                draggable: true,
                displayHeader: true,
                displayAddButton: false,
                displayRemoveButton: false
            )
            {
                elementHeight =
                    ResolveKeysProperty().arraySize == 0 ? emptyHeight : defaultRowHeight,
            };

            list.drawHeaderCallback = rect =>
            {
                ListPageCache currentCache = cacheProvider();
                SyncListSelectionWithPagination(list, pagination, currentCache);
                DrawListHeader(rect, ResolveKeysProperty(), list, pagination, cacheProvider);
            };

            list.elementHeightCallback = ResolveRowHeight;
            list.drawNoneElementCallback = _ => { };

            list.drawElementBackgroundCallback = (rect, index, _, _) =>
            {
                ListPageCache currentCache = cacheProvider();
                if (!RelativeIndexIsValid(currentCache, index))
                {
                    return;
                }

                if (Event.current.type != EventType.Repaint)
                {
                    return;
                }

                SerializedProperty currentKeys = ResolveKeysProperty();
                SerializedProperty currentValues = ResolveValuesProperty();

                int globalIndex = currentCache.entries[index].arrayIndex;
                if (
                    globalIndex < 0
                    || globalIndex >= currentKeys.arraySize
                    || globalIndex >= currentValues.arraySize
                )
                {
                    return;
                }

                SerializedProperty keyProperty = currentKeys.GetArrayElementAtIndex(globalIndex);
                SerializedProperty valueProperty = currentValues.GetArrayElementAtIndex(
                    globalIndex
                );

                float rowHeight = CalculateDictionaryRowHeight(keyProperty, valueProperty);
                Rect backgroundRect = new(rect.x, rect.y, rect.width, rowHeight);
                Color rowColor = EditorGUIUtility.isProSkin ? DarkRowColor : LightRowColor;
                EditorGUI.DrawRect(backgroundRect, rowColor);

                bool hasDuplicate = TryGetDuplicateInfo(
                    key,
                    globalIndex,
                    out DuplicateKeyInfo duplicateInfo,
                    out DuplicateKeyState duplicateState
                );
                bool hasNullKey = TryGetNullKeyInfo(key, globalIndex, out NullKeyInfo nullKeyInfo);

                UnityHelpersSettings.DuplicateRowAnimationMode animationMode =
                    UnityHelpersSettings.GetDuplicateRowAnimationMode();
                bool highlightDuplicates =
#pragma warning disable CS0618 // Type or member is obsolete
                    animationMode != UnityHelpersSettings.DuplicateRowAnimationMode.None;
#pragma warning restore CS0618 // Type or member is obsolete
                bool animateDuplicates =
                    animationMode == UnityHelpersSettings.DuplicateRowAnimationMode.Tween;
                int tweenCycleLimit = UnityHelpersSettings.GetDuplicateRowTweenCycleLimit();
                double currentTime = animateDuplicates ? EditorApplication.timeSinceStartup : 0d;

                float shakeOffset = 0f;
                if (hasDuplicate && animateDuplicates && duplicateState != null)
                {
                    shakeOffset = duplicateState.GetAnimationOffset(
                        globalIndex,
                        currentTime,
                        tweenCycleLimit
                    );
                }

                Rect highlightRect = ExpandDictionaryRowRect(backgroundRect);
                Rect animatedRect = highlightRect;
                animatedRect.x += shakeOffset;

                Rect insetHighlightRect = animatedRect;
                insetHighlightRect.xMin += 1f;
                insetHighlightRect.xMax -= 1f;
                insetHighlightRect.yMin += 1f;
                insetHighlightRect.yMax -= 1f;
                insetHighlightRect.height = Mathf.Max(0f, insetHighlightRect.height);

                if (hasDuplicate)
                {
                    if (highlightDuplicates)
                    {
                        Color highlightColor = duplicateInfo.isPrimary
                            ? DuplicatePrimaryColor
                            : DuplicateSecondaryColor;
                        EditorGUI.DrawRect(insetHighlightRect, highlightColor);
                        DrawDuplicateOutline(insetHighlightRect);
                    }

                    DrawDuplicateTooltip(insetHighlightRect, duplicateInfo.tooltip);
                }

                if (hasNullKey)
                {
                    EditorGUI.DrawRect(insetHighlightRect, NullKeyHighlightColor);
                    DrawDuplicateOutline(insetHighlightRect);
                    DrawNullTooltip(insetHighlightRect, nullKeyInfo.tooltip);
                }

                if (list.index == index)
                {
                    Rect selectionRect = highlightRect;
                    selectionRect.x += shakeOffset;
                    Color selectionColor = EditorGUIUtility.isProSkin
                        ? DarkSelectionColor
                        : LightSelectionColor;
                    EditorGUI.DrawRect(selectionRect, selectionColor);
                }
            };

            list.drawElementCallback = (rect, index, _, _) =>
            {
                ListPageCache currentCache = cacheProvider();
                if (!RelativeIndexIsValid(currentCache, index))
                {
                    return;
                }

                SerializedProperty currentKeys = ResolveKeysProperty();
                SerializedProperty currentValues = ResolveValuesProperty();

                int globalIndex = currentCache.entries[index].arrayIndex;
                if (
                    globalIndex < 0
                    || globalIndex >= currentKeys.arraySize
                    || globalIndex >= currentValues.arraySize
                )
                {
                    return;
                }

                SerializedProperty keyProperty = currentKeys.GetArrayElementAtIndex(globalIndex);
                SerializedProperty valueProperty = currentValues.GetArrayElementAtIndex(
                    globalIndex
                );

                float spacing = EditorGUIUtility.standardVerticalSpacing;
                bool valueSupportsFoldoutForHeight =
                    resolvedValueType != null
                    && ValueTypeSupportsFoldout(resolvedValueType)
                    && SerializedPropertySupportsFoldout(valueProperty);
                if (valueSupportsFoldoutForHeight)
                {
                    valueProperty.isExpanded = true;
                }
                float keyHeight = EditorGUI.GetPropertyHeight(keyProperty, GUIContent.none, true);
                float valueHeight = EditorGUI.GetPropertyHeight(
                    valueProperty,
                    GUIContent.none,
                    true
                );
                float contentY = rect.y + spacing;
                bool hasDuplicate = TryGetDuplicateInfo(
                    key,
                    globalIndex,
                    out DuplicateKeyInfo duplicateInfo,
                    out DuplicateKeyState duplicateState
                );
                bool hasNullKey = TryGetNullKeyInfo(key, globalIndex, out NullKeyInfo nullKeyInfo);

                UnityHelpersSettings.DuplicateRowAnimationMode animationMode =
                    UnityHelpersSettings.GetDuplicateRowAnimationMode();
                bool animateDuplicates =
                    animationMode == UnityHelpersSettings.DuplicateRowAnimationMode.Tween;
                int tweenCycleLimit = UnityHelpersSettings.GetDuplicateRowTweenCycleLimit();
                double currentTime = animateDuplicates ? EditorApplication.timeSinceStartup : 0d;

                if (hasDuplicate && animateDuplicates && duplicateState != null)
                {
                    rect.x += duplicateState.GetAnimationOffset(
                        globalIndex,
                        currentTime,
                        tweenCycleLimit
                    );
                }

                float gap = 6f;
                bool complexValue =
                    resolvedValueType != null && TypeSupportsComplexEditing(resolvedValueType);
                float keyColumnWidth = complexValue
                    ? Mathf.Min(Mathf.Max(rect.width * 0.35f, 170f), rect.width - gap - 80f)
                    : (rect.width - gap) * 0.5f;
                keyColumnWidth = Mathf.Max(100f, keyColumnWidth);
                float valueColumnWidth = Mathf.Max(0f, rect.width - keyColumnWidth - gap);
                if (valueSupportsFoldoutForHeight)
                {
                    float targetValueWidth = Mathf.Max(rect.width * 0.55f, 220f);
                    targetValueWidth = Mathf.Min(targetValueWidth, rect.width - gap - 110f);
                    valueColumnWidth = Mathf.Max(valueColumnWidth, targetValueWidth);
                    keyColumnWidth = Mathf.Max(100f, rect.width - gap - valueColumnWidth);
                    valueColumnWidth = Mathf.Max(0f, rect.width - keyColumnWidth - gap);
                    gap += 6f;
                }

                Rect keyRect = new(rect.x, contentY, keyColumnWidth, keyHeight);
                Rect valueRect = new(
                    rect.x + keyColumnWidth + gap,
                    contentY,
                    valueColumnWidth,
                    valueHeight
                );

                LastRowOriginalRect = rect;
                LastRowValueBaseX = valueRect.x;

                float rowFieldPadding = DictionaryRowFieldPadding;
                keyRect.x += rowFieldPadding;
                keyRect.width = Mathf.Max(0f, keyRect.width - rowFieldPadding);
                valueRect.x += rowFieldPadding;
                valueRect.width = Mathf.Max(0f, valueRect.width - rowFieldPadding);

                bool valueSupportsFoldout = valueSupportsFoldoutForHeight;

                float iconSize = EditorGUIUtility.singleLineHeight;
                float iconSpacing = 3f;
                float consumedWidth = 0f;

                if (hasDuplicate)
                {
                    Rect iconRect = new(keyRect.x + consumedWidth, keyRect.y, iconSize, iconSize);
                    GUIContent iconContent = GetDuplicateIconContent(duplicateInfo.tooltip);
                    GUI.Label(iconRect, iconContent);
                    consumedWidth += iconSize + iconSpacing;
                }

                if (hasNullKey)
                {
                    Rect iconRect = new(keyRect.x + consumedWidth, keyRect.y, iconSize, iconSize);
                    GUIContent iconContent = GetNullKeyIconContent(nullKeyInfo.tooltip);
                    GUI.Label(iconRect, iconContent);
                    consumedWidth += iconSize + iconSpacing;
                }

                if (consumedWidth > 0f)
                {
                    consumedWidth -= iconSpacing;
                    keyRect.x += consumedWidth;
                    keyRect.width -= consumedWidth;
                    if (keyRect.width < 0f)
                    {
                        keyRect.width = 0f;
                    }
                }

                HasLastRowRects = true;
                LastRowKeyRect = keyRect;
                LastRowValueRect = valueRect;

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(keyRect, keyProperty, GUIContent.none, true);
                if (EditorGUI.EndChangeCheck())
                {
                    InvalidateKeyCache(key);
                    ApplyAndSyncPaletteRowChange(dictionaryProperty, "KeyFieldChanged");
                }

                GUIContent valueLabel = GUIContent.none;
                float previousLabelWidth = EditorGUIUtility.labelWidth;
                bool restoredLabelWidth = false;
                if (valueSupportsFoldout)
                {
                    valueLabel = FoldoutSpacerLabel;
                    EditorGUIUtility.labelWidth = Mathf.Max(
                        previousLabelWidth,
                        ExpandableValueFoldoutLabelWidth
                    );
                    restoredLabelWidth = true;
                    float foldoutOffset = RowExpandableValueFoldoutGutter;
                    valueRect.x += foldoutOffset;
                    valueRect.width = Mathf.Max(0f, valueRect.width - foldoutOffset);
                    LastRowValueFoldoutOffset = foldoutOffset;
                    if (valueProperty != null && !valueProperty.isExpanded)
                    {
                        valueProperty.isExpanded = true;
                    }
                }
                else
                {
                    LastRowValueFoldoutOffset = 0f;
                }

                try
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(valueRect, valueProperty, valueLabel, true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        MarkListCacheDirty(key);
                        ApplyAndSyncPaletteRowChange(dictionaryProperty, "ValueFieldChanged");
                    }
                }
                finally
                {
                    if (restoredLabelWidth)
                    {
                        EditorGUIUtility.labelWidth = previousLabelWidth;
                    }
                }
                LastRowValueUsedFoldoutLabel = valueSupportsFoldout;
            };

            list.onRemoveCallback = reorderableList =>
            {
                ListPageCache currentCache = cacheProvider();
                if (!RelativeIndexIsValid(currentCache, reorderableList.index))
                {
                    return;
                }

                int globalIndex = currentCache.entries[reorderableList.index].arrayIndex;
                RemoveEntryAtIndex(
                    globalIndex,
                    reorderableList,
                    dictionaryProperty,
                    keysProperty,
                    valuesProperty,
                    pagination
                );
            };

            list.onReorderCallbackWithDetails = (_, oldIndex, _) =>
            {
                ListPageCache currentCache = cacheProvider();
                if (
                    !RelativeIndexIsValid(currentCache, oldIndex)
                    || currentCache.entries.Count == 0
                )
                {
                    return;
                }

                List<int> orderedIndices = new(currentCache.entries.Count);
                foreach (PageEntry entry in currentCache.entries)
                {
                    orderedIndices.Add(entry.arrayIndex);
                }

                int pageSize = Mathf.Max(1, pagination.pageSize);
                int maxStart = Mathf.Max(0, keysProperty.arraySize - orderedIndices.Count);
                int pageStart = Mathf.Clamp(pagination.pageIndex * pageSize, 0, maxStart);

                ApplyDictionarySliceOrder(keysProperty, valuesProperty, orderedIndices, pageStart);
                int relativeSelection = Mathf.Clamp(list.index, 0, orderedIndices.Count - 1);
                pagination.selectedIndex = pageStart + relativeSelection;
                InvalidateKeyCache(key);
                MarkListCacheDirty(key);

                ListPageCache refreshedCache = EnsurePageCache(key, keysProperty, pagination);
                list.list = refreshedCache.entries;
                SyncListSelectionWithPagination(list, pagination, refreshedCache);
            };

            list.onSelectCallback = reorderableList =>
            {
                if (reorderableList.index >= 0)
                {
                    ListPageCache currentCache = cacheProvider();
                    if (RelativeIndexIsValid(currentCache, reorderableList.index))
                    {
                        pagination.selectedIndex = currentCache
                            .entries[reorderableList.index]
                            .arrayIndex;
                    }
                }
            };

            list.footerHeight =
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 1.5f;
            list.drawFooterCallback = rect =>
            {
                DrawListFooter(
                    rect,
                    list,
                    dictionaryProperty,
                    keysProperty,
                    valuesProperty,
                    pagination,
                    cacheProvider
                );
            };

            SyncListSelectionWithPagination(list, pagination, cache);
            list.elementHeight = keysProperty.arraySize == 0 ? emptyHeight : defaultRowHeight;

            _lists[key] = list;
            return list;

            SerializedProperty ResolveKeysProperty()
            {
                return dictionaryProperty.FindPropertyRelative(
                    SerializableDictionarySerializedPropertyNames.Keys
                );
            }

            SerializedProperty ResolveValuesProperty()
            {
                return dictionaryProperty.FindPropertyRelative(
                    SerializableDictionarySerializedPropertyNames.Values
                );
            }

            float ResolveRowHeight(int elementIndex)
            {
                ListPageCache currentCache = cacheProvider();
                if (!RelativeIndexIsValid(currentCache, elementIndex))
                {
                    return defaultRowHeight;
                }

                SerializedProperty currentKeys = ResolveKeysProperty();
                SerializedProperty currentValues = ResolveValuesProperty();

                int globalIndex = currentCache.entries[elementIndex].arrayIndex;
                if (
                    globalIndex < 0
                    || globalIndex >= currentKeys.arraySize
                    || globalIndex >= currentValues.arraySize
                )
                {
                    return defaultRowHeight;
                }

                SerializedProperty rowKeyProperty = currentKeys.GetArrayElementAtIndex(globalIndex);
                SerializedProperty rowValueProperty = currentValues.GetArrayElementAtIndex(
                    globalIndex
                );

                return CalculateDictionaryRowHeight(rowKeyProperty, rowValueProperty);
            }
        }

        private DuplicateKeyState RefreshDuplicateState(
            string cacheKey,
            SerializedProperty keysProperty,
            Type keyType
        )
        {
            if (string.IsNullOrEmpty(cacheKey))
            {
                return null;
            }

            DuplicateKeyState state = _duplicateStates.GetOrAdd(cacheKey);

            bool hasEvent = Event.current != null;
            EventType eventType = hasEvent ? Event.current.type : EventType.Repaint;
            bool shouldRefresh = eventType == EventType.Repaint;
            bool changed = false;

            if (shouldRefresh)
            {
                changed = state.Refresh(keysProperty, keyType);
            }

            if (!state.HasDuplicates && state.IsEmpty)
            {
                _duplicateStates.Remove(cacheKey);
            }

            if (state.HasDuplicates)
            {
                EditorWindow focusedWindow = EditorWindow.focusedWindow;
                if (focusedWindow != null)
                {
                    focusedWindow.Repaint();
                }
            }
            else if (changed)
            {
                MarkListCacheDirty(cacheKey);
            }

            return state;
        }

        private NullKeyState RefreshNullKeyState(
            string cacheKey,
            SerializedProperty keysProperty,
            Type keyType
        )
        {
            if (string.IsNullOrEmpty(cacheKey))
            {
                return null;
            }

            bool hasEvent = Event.current != null;
            EventType eventType = hasEvent ? Event.current.type : EventType.Repaint;
            if (eventType != EventType.Repaint)
            {
                return _nullKeyStates.GetValueOrDefault(cacheKey);
            }

            if (!TypeSupportsNullReferences(keyType))
            {
                if (_nullKeyStates.Remove(cacheKey))
                {
                    MarkListCacheDirty(cacheKey);
                    RequestRepaint();
                }

                return null;
            }

            NullKeyState state = _nullKeyStates.GetOrAdd(cacheKey);
            bool changed = state.Refresh(keysProperty, keyType);

            if (!state.HasNullKeys)
            {
                _nullKeyStates.Remove(cacheKey);
                if (changed)
                {
                    MarkListCacheDirty(cacheKey);
                    RequestRepaint();
                }

                return null;
            }

            if (changed)
            {
                MarkListCacheDirty(cacheKey);
                RequestRepaint();
            }

            return state;
        }

        private bool TryGetDuplicateInfo(
            string cacheKey,
            int arrayIndex,
            out DuplicateKeyInfo info,
            out DuplicateKeyState state
        )
        {
            info = null;
            state = null;
            if (string.IsNullOrEmpty(cacheKey))
            {
                return false;
            }

            return _duplicateStates.TryGetValue(cacheKey, out state)
                && state.TryGetInfo(arrayIndex, out info);
        }

        private bool TryGetNullKeyInfo(string cacheKey, int arrayIndex, out NullKeyInfo info)
        {
            info = null;
            if (string.IsNullOrEmpty(cacheKey))
            {
                return false;
            }

            return _nullKeyStates.TryGetValue(cacheKey, out NullKeyState state)
                && state.TryGetInfo(arrayIndex, out info);
        }

        private static void DrawDuplicateTooltip(Rect rect, string tooltip)
        {
            if (string.IsNullOrEmpty(tooltip) || Event.current.type != EventType.Repaint)
            {
                return;
            }

            DuplicateTooltipContent.text = string.Empty;
            DuplicateTooltipContent.image = null;
            DuplicateTooltipContent.tooltip = tooltip;
            GUI.Label(rect, DuplicateTooltipContent, GUIStyle.none);
        }

        private static GUIContent GetDuplicateIconContent(string tooltip)
        {
            if (DuplicateIconTemplate != null)
            {
                DuplicateIconContentCache.image = DuplicateIconTemplate.image;
                DuplicateIconContentCache.text = string.Empty;
            }
            else
            {
                DuplicateIconContentCache.image = null;
                DuplicateIconContentCache.text = "!";
            }

            DuplicateIconContentCache.tooltip = tooltip;
            return DuplicateIconContentCache;
        }

        private static void DrawNullTooltip(Rect rect, string tooltip)
        {
            if (string.IsNullOrEmpty(tooltip) || Event.current.type != EventType.Repaint)
            {
                return;
            }

            NullKeyTooltipContent.text = string.Empty;
            NullKeyTooltipContent.image = null;
            NullKeyTooltipContent.tooltip = tooltip;
            GUI.Label(rect, NullKeyTooltipContent, GUIStyle.none);
        }

        internal static float CalculateDictionaryRowHeight(
            SerializedProperty keyProperty,
            SerializedProperty valueProperty
        )
        {
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float keyHeight =
                keyProperty != null
                    ? EditorGUI.GetPropertyHeight(keyProperty, GUIContent.none, true)
                    : EditorGUIUtility.singleLineHeight;
            float valueHeight =
                valueProperty != null
                    ? EditorGUI.GetPropertyHeight(valueProperty, GUIContent.none, true)
                    : EditorGUIUtility.singleLineHeight;
            float contentHeight = Mathf.Max(
                EditorGUIUtility.singleLineHeight,
                Mathf.Max(keyHeight, valueHeight)
            );
            return contentHeight + spacing * 2f;
        }

        internal static Rect ExpandDictionaryRowRect(Rect rect)
        {
            rect.yMin -= 1f;
            rect.yMax += 1f;
            return rect;
        }

        private static GUIContent GetNullKeyIconContent(string tooltip)
        {
            if (DuplicateIconTemplate != null)
            {
                NullKeyIconContentCache.image = DuplicateIconTemplate.image;
                NullKeyIconContentCache.text = string.Empty;
            }
            else
            {
                NullKeyIconContentCache.image = null;
                NullKeyIconContentCache.text = "!";
            }

            NullKeyIconContentCache.tooltip = tooltip;
            return NullKeyIconContentCache;
        }

        internal static float EvaluateDuplicateTweenOffset(
            int arrayIndex,
            double startTime,
            double currentTime,
            int cycleLimit
        )
        {
            if (cycleLimit == 0)
            {
                return 0f;
            }

            if (currentTime < startTime)
            {
                startTime = currentTime;
            }

            if (cycleLimit > 0)
            {
                double cycleDuration = (2d * Math.PI) / DuplicateShakeFrequency;
                double elapsed = currentTime - startTime;
                double maxDuration = cycleDuration * cycleLimit;
                if (elapsed >= maxDuration)
                {
                    return 0f;
                }
            }

            float phase = (float)(currentTime * DuplicateShakeFrequency);
            float seed = arrayIndex * 0.35f;
            return Mathf.Sin(phase + seed) * DuplicateShakeAmplitude;
        }

        private static void DrawDuplicateOutline(Rect rect)
        {
            Rect top = new(rect.x, rect.y, rect.width, DuplicateBorderThickness);
            Rect bottom = new(
                rect.x,
                rect.yMax - DuplicateBorderThickness,
                rect.width,
                DuplicateBorderThickness
            );
            Rect left = new(rect.x, rect.y, DuplicateBorderThickness, rect.height);
            Rect right = new(
                rect.xMax - DuplicateBorderThickness,
                rect.y,
                DuplicateBorderThickness,
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

        private static bool TypeSupportsNullReferences(Type type)
        {
            return type != null && (!type.IsValueType || typeof(Object).IsAssignableFrom(type));
        }

        private static string BuildNullKeySummary(ICollection<int> indices)
        {
            if (indices == null || indices.Count == 0)
            {
                return string.Empty;
            }

            using PooledResource<List<int>> sortedLease = Buffers<int>.GetList(
                indices.Count,
                out List<int> sorted
            );
            sorted.AddRange(indices);
            sorted.Sort();

            if (sorted.Count == 1)
            {
                return $"Null key detected at index {sorted[0]}. Entry will be ignored at runtime.";
            }

            const int maxDisplay = 5;
            int displayCount = Math.Min(sorted.Count, maxDisplay);

            using PooledResource<StringBuilder> builderLease = Buffers.GetStringBuilder(
                Math.Max(sorted.Count * 6 + 64, 64),
                out StringBuilder summaryBuilder
            );
            summaryBuilder.Clear();
            summaryBuilder.Append("Null keys detected at indices ");

            for (int i = 0; i < displayCount; i++)
            {
                if (i > 0)
                {
                    summaryBuilder.Append(", ");
                }

                summaryBuilder.Append(sorted[i]);
            }

            if (sorted.Count > maxDisplay)
            {
                summaryBuilder.Append(", ... (");
                summaryBuilder.Append(sorted.Count - maxDisplay);
                summaryBuilder.Append(" more)");
            }

            summaryBuilder.Append(". Entries will be ignored at runtime.");
            return summaryBuilder.ToString();
        }

        private static string FormatDuplicateKeyDisplay(object keyValue)
        {
            object actualKey = ReferenceEquals(keyValue, NullKeySentinel) ? null : keyValue;

            if (actualKey == null)
            {
                return "<null>";
            }

            if (actualKey is string stringKey)
            {
                if (string.IsNullOrWhiteSpace(stringKey))
                {
                    return "<empty>";
                }

                return stringKey;
            }

            if (actualKey is Object unityObject)
            {
                if (unityObject == null)
                {
                    return "<missing object>";
                }

                return string.IsNullOrEmpty(unityObject.name)
                    ? unityObject.GetType().Name
                    : unityObject.name;
            }

            return actualKey.ToString();
        }

        private static string BuildDuplicateTooltip(string formattedKey, List<int> indices)
        {
            if (indices == null || indices.Count == 0)
            {
                return $"Duplicate key \"{formattedKey}\" detected.";
            }

            int count = indices.Count;
            int[] positions = new int[count];
            for (int index = 0; index < count; index++)
            {
                positions[index] = indices[index] + 1;
            }

            Array.Sort(positions);

            string[] parts = new string[count];
            for (int index = 0; index < count; index++)
            {
                parts[index] = positions[index].ToString(CultureInfo.InvariantCulture);
            }

            string joinedPositions = string.Join(", ", parts);
            return $"Duplicate key \"{formattedKey}\" is assigned to entries {joinedPositions}. The last entry will be used at runtime.";
        }

        internal PendingEntry GetOrCreatePendingEntry(
            SerializedProperty property,
            Type keyType,
            Type valueType,
            bool isSortedDictionary
        )
        {
            string key = GetListKey(property);
            if (_pendingEntries.TryGetValue(key, out PendingEntry entry))
            {
                entry.isSorted = isSortedDictionary;
                EnsurePendingFoldoutAnim(entry);
                return entry;
            }

            entry = new PendingEntry
            {
                key = GetDefaultValue(keyType),
                value = GetDefaultValue(valueType),
                isExpanded = false,
                isSorted = isSortedDictionary,
            };
            if (ShouldTweenPendingFoldout(isSortedDictionary))
            {
                entry.foldoutAnim = CreatePendingFoldoutAnim(entry.isExpanded, isSortedDictionary);
            }
            _pendingEntries[key] = entry;
            return entry;
        }

        internal PaginationState GetOrCreatePaginationState(SerializedProperty property)
        {
            string key = GetListKey(property);
            int configuredPageSize = UnityHelpersSettings.GetSerializableDictionaryPageSize();
            if (_paginationStates.TryGetValue(key, out PaginationState state))
            {
                if (state.pageSize <= 0)
                {
                    state.pageSize = configuredPageSize > 0 ? configuredPageSize : DefaultPageSize;
                }

                if (state.pageSize != configuredPageSize && configuredPageSize > 0)
                {
                    state.pageSize = configuredPageSize;
                    state.pageIndex = 0;
                }

                return state;
            }

            PaginationState newState = new()
            {
                pageIndex = 0,
                pageSize = configuredPageSize > 0 ? configuredPageSize : DefaultPageSize,
                selectedIndex = -1,
            };
            _paginationStates[key] = newState;
            return newState;
        }

        internal ListPageCache EnsurePageCache(
            string cacheKey,
            SerializedProperty keysProperty,
            PaginationState pagination
        )
        {
            ListPageCache cache = GetOrCreatePageCache(cacheKey);
            int itemCount = keysProperty.arraySize;
            if (
                cache.dirty
                || cache.pageIndex != pagination.pageIndex
                || cache.pageSize != pagination.pageSize
                || cache.itemCount != itemCount
            )
            {
                RefreshPageCache(cache, keysProperty, pagination);
            }

            return cache;
        }

        private ListPageCache GetOrCreatePageCache(string cacheKey)
        {
            if (_pageCaches.TryGetValue(cacheKey, out ListPageCache cache))
            {
                return cache;
            }

            ListPageCache newCache = new();
            _pageCaches[cacheKey] = newCache;
            return newCache;
        }

        private static void RefreshPageCache(
            ListPageCache cache,
            SerializedProperty keysProperty,
            PaginationState pagination
        )
        {
            cache.entries.Clear();
            cache.itemCount = keysProperty.arraySize;
            cache.pageIndex = pagination.pageIndex;
            int effectivePageSize = Mathf.Clamp(pagination.pageSize, 1, MaxPageSize);
            cache.pageSize = effectivePageSize;
            pagination.pageSize = effectivePageSize;
            cache.dirty = false;

            if (cache.itemCount <= 0)
            {
                return;
            }

            int startIndex = 0;
            int endIndex = cache.itemCount;
            if (effectivePageSize > 0)
            {
                startIndex = Mathf.Clamp(
                    pagination.pageIndex * effectivePageSize,
                    0,
                    cache.itemCount
                );
                endIndex = Mathf.Min(startIndex + effectivePageSize, cache.itemCount);
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                PageEntry entry = new() { arrayIndex = i };
                cache.entries.Add(entry);
            }
        }

        internal void MarkListCacheDirty(string cacheKey)
        {
            _lists.Remove(cacheKey);

            if (!_pageCaches.TryGetValue(cacheKey, out ListPageCache cache))
            {
                return;
            }

            cache.entries.Clear();
            cache.dirty = true;
            cache.pageIndex = -1;
            cache.pageSize = -1;
            cache.itemCount = -1;
        }

        private static int GetRelativeIndex(ListPageCache cache, int globalIndex)
        {
            if (globalIndex < 0)
            {
                return -1;
            }

            for (int i = 0; i < cache.entries.Count; i++)
            {
                if (cache.entries[i].arrayIndex == globalIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void ApplyDictionarySliceOrder(
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty,
            List<int> orderedIndices,
            int pageStart
        )
        {
            if (keysProperty == null || valuesProperty == null || orderedIndices == null)
            {
                return;
            }

            for (int i = 0; i < orderedIndices.Count; i++)
            {
                int desiredIndex = pageStart + i;
                int currentIndex = orderedIndices[i];
                if (currentIndex == desiredIndex)
                {
                    continue;
                }

                keysProperty.MoveArrayElement(currentIndex, desiredIndex);
                valuesProperty.MoveArrayElement(currentIndex, desiredIndex);

                if (currentIndex < desiredIndex)
                {
                    for (int j = i + 1; j < orderedIndices.Count; j++)
                    {
                        if (orderedIndices[j] > currentIndex && orderedIndices[j] <= desiredIndex)
                        {
                            orderedIndices[j]--;
                        }
                    }
                }
                else
                {
                    for (int j = i + 1; j < orderedIndices.Count; j++)
                    {
                        if (orderedIndices[j] >= desiredIndex && orderedIndices[j] < currentIndex)
                        {
                            orderedIndices[j]++;
                        }
                    }
                }

                orderedIndices[i] = desiredIndex;
            }
        }

        private static bool RelativeIndexIsValid(ListPageCache cache, int relativeIndex)
        {
            return relativeIndex >= 0 && relativeIndex < cache.entries.Count;
        }

        internal static void SyncListSelectionWithPagination(
            ReorderableList list,
            PaginationState pagination,
            ListPageCache cache
        )
        {
            if (cache.entries.Count == 0)
            {
                list.index = -1;
                pagination.selectedIndex = -1;
                return;
            }

            int relativeIndex = GetRelativeIndex(cache, pagination.selectedIndex);
            if (relativeIndex < 0)
            {
                relativeIndex = 0;
                pagination.selectedIndex = cache.entries[0].arrayIndex;
            }

            list.index = relativeIndex;
        }

        private void DrawListHeader(
            Rect rect,
            SerializedProperty keysProperty,
            ReorderableList list,
            PaginationState pagination,
            Func<ListPageCache> cacheProvider
        )
        {
            ClampPaginationState(pagination, keysProperty.arraySize);
            ListPageCache cache = cacheProvider();
            SyncListSelectionWithPagination(list, pagination, cache);

            float navWidthFull = (PaginationButtonWidth * 4f) + (PaginationControlSpacing * 3f);
            float navWidthPrevNext = (PaginationButtonWidth * 2f) + PaginationControlSpacing;

            PaginationControlLayout layout = PaginationControlLayout.Full;
            bool showPageLabel = true;
            float navWidth = navWidthFull;
            float controlsWidth = ComputeControlsWidth(navWidth, showPageLabel);

            if (controlsWidth > rect.width)
            {
                showPageLabel = false;
                controlsWidth = ComputeControlsWidth(navWidth, showPageLabel);
            }

            if (controlsWidth > rect.width)
            {
                layout = PaginationControlLayout.PrevNext;
                navWidth = navWidthPrevNext;
                controlsWidth = ComputeControlsWidth(navWidth, showPageLabel);
            }

            if (controlsWidth > rect.width)
            {
                layout = PaginationControlLayout.None;
                navWidth = 0f;
                showPageLabel = false;
                controlsWidth = 0f;
            }

            if (controlsWidth <= 0f)
            {
                return;
            }

            Rect controlsRect = new(rect.xMax - controlsWidth, rect.y, controlsWidth, rect.height);

            int itemCount = keysProperty.arraySize;
            int totalPages = GetTotalPages(itemCount, pagination.pageSize);
            float navStartX = controlsRect.x;

            if (showPageLabel)
            {
                Rect pageLabelRect = new(
                    controlsRect.x,
                    controlsRect.y,
                    PaginationLabelWidth,
                    controlsRect.height
                );
                string pageLabel = $"Page {pagination.pageIndex + 1}/{totalPages}";
                EditorGUI.LabelField(pageLabelRect, pageLabel, EditorStyles.miniLabel);
                navStartX = pageLabelRect.xMax + (navWidth > 0f ? PaginationControlSpacing : 0f);
            }

            if (layout == PaginationControlLayout.None)
            {
                return;
            }

            GUIContent prevContent = EditorGUIUtility.TrTextContent("<", "Previous page");
            GUIContent nextContent = EditorGUIUtility.TrTextContent(">", "Next page");

            switch (layout)
            {
                case PaginationControlLayout.Full:
                    GUIContent firstContent = EditorGUIUtility.TrTextContent(
                        "<<",
                        "Jump to first page"
                    );
                    GUIContent lastContent = EditorGUIUtility.TrTextContent(
                        ">>",
                        "Jump to last page"
                    );

                    Rect firstRect = new(
                        navStartX,
                        controlsRect.y,
                        PaginationButtonWidth,
                        controlsRect.height
                    );
                    Rect prevRect = new(
                        firstRect.xMax + PaginationControlSpacing,
                        controlsRect.y,
                        PaginationButtonWidth,
                        controlsRect.height
                    );
                    Rect nextRect = new(
                        prevRect.xMax + PaginationControlSpacing,
                        controlsRect.y,
                        PaginationButtonWidth,
                        controlsRect.height
                    );
                    Rect lastRect = new(
                        nextRect.xMax + PaginationControlSpacing,
                        controlsRect.y,
                        PaginationButtonWidth,
                        controlsRect.height
                    );

                    using (new EditorGUI.DisabledScope(pagination.pageIndex <= 0))
                    {
                        if (GUI.Button(firstRect, firstContent, EditorStyles.miniButton))
                        {
                            SetPageIndex(pagination, 0, keysProperty, forceImmediateRefresh: true);
                            cache = cacheProvider();
                            SyncListSelectionWithPagination(list, pagination, cache);
                        }

                        if (GUI.Button(prevRect, prevContent, EditorStyles.miniButton))
                        {
                            SetPageIndex(
                                pagination,
                                pagination.pageIndex - 1,
                                keysProperty,
                                forceImmediateRefresh: true
                            );
                            cache = cacheProvider();
                            SyncListSelectionWithPagination(list, pagination, cache);
                        }
                    }

                    using (new EditorGUI.DisabledScope(pagination.pageIndex >= totalPages - 1))
                    {
                        if (GUI.Button(nextRect, nextContent, EditorStyles.miniButton))
                        {
                            SetPageIndex(
                                pagination,
                                pagination.pageIndex + 1,
                                keysProperty,
                                forceImmediateRefresh: true
                            );
                            cache = cacheProvider();
                            SyncListSelectionWithPagination(list, pagination, cache);
                        }

                        if (GUI.Button(lastRect, lastContent, EditorStyles.miniButton))
                        {
                            SetPageIndex(
                                pagination,
                                totalPages - 1,
                                keysProperty,
                                forceImmediateRefresh: true
                            );
                            cache = cacheProvider();
                            SyncListSelectionWithPagination(list, pagination, cache);
                        }
                    }

                    break;

                case PaginationControlLayout.PrevNext:
                    Rect prevOnlyRect = new(
                        navStartX,
                        controlsRect.y,
                        PaginationButtonWidth,
                        controlsRect.height
                    );
                    Rect nextOnlyRect = new(
                        prevOnlyRect.xMax + PaginationControlSpacing,
                        controlsRect.y,
                        PaginationButtonWidth,
                        controlsRect.height
                    );

                    using (new EditorGUI.DisabledScope(pagination.pageIndex <= 0))
                    {
                        if (GUI.Button(prevOnlyRect, prevContent, EditorStyles.miniButton))
                        {
                            SetPageIndex(
                                pagination,
                                pagination.pageIndex - 1,
                                keysProperty,
                                forceImmediateRefresh: true
                            );
                            cache = cacheProvider();
                            SyncListSelectionWithPagination(list, pagination, cache);
                        }
                    }

                    using (new EditorGUI.DisabledScope(pagination.pageIndex >= totalPages - 1))
                    {
                        if (GUI.Button(nextOnlyRect, nextContent, EditorStyles.miniButton))
                        {
                            SetPageIndex(
                                pagination,
                                pagination.pageIndex + 1,
                                keysProperty,
                                forceImmediateRefresh: true
                            );
                            cache = cacheProvider();
                            SyncListSelectionWithPagination(list, pagination, cache);
                        }
                    }

                    break;
            }

            return;

            float ComputeControlsWidth(float navigationWidth, bool includePageLabel)
            {
                float width = navigationWidth;
                if (includePageLabel)
                {
                    width += PaginationLabelWidth;
                    if (navigationWidth > 0f)
                    {
                        width += PaginationControlSpacing;
                    }
                }

                return width;
            }
        }

        private void DrawListFooter(
            Rect rect,
            ReorderableList list,
            SerializedProperty dictionaryProperty,
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty,
            PaginationState pagination,
            Func<ListPageCache> cacheProvider
        )
        {
            if (Event.current.type == EventType.Repaint)
            {
                GUIStyle footerStyle =
                    ReorderableList.defaultBehaviours.footerBackground ?? "RL Footer";
                footerStyle.Draw(rect, GUIContent.none, false, false, false, false);
            }

            int itemCount = keysProperty.arraySize;
            int pageStart = Mathf.Clamp(pagination.pageIndex * pagination.pageSize, 0, itemCount);
            int pageEnd = Mathf.Min(pageStart + pagination.pageSize, itemCount);

            float padding = 4f;
            float buttonWidth = 26f;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float verticalCenter = rect.y + Mathf.Max(0f, (rect.height - lineHeight) * 0.5f);
            float labelHeight = lineHeight + 4f;
            float labelY = rect.y + Mathf.Max(0f, (rect.height - labelHeight) * 0.5f);
            float clearWidth = 80f;
            float sortWidth = 60f;

            GUIStyle footerLabelStyle = GetFooterLabelStyle();
            string rangeText =
                itemCount == 0 ? "Empty" : $"{pageStart + 1}-{pageEnd} of {itemCount}";
            Vector2 rangeSize = footerLabelStyle.CalcSize(new GUIContent(rangeText));

            ListPageCache cache = cacheProvider();
            int selectedGlobalIndex = pagination.selectedIndex;
            int relativeSelected = GetRelativeIndex(cache, selectedGlobalIndex);

            bool canRemove =
                relativeSelected >= 0
                && selectedGlobalIndex >= 0
                && selectedGlobalIndex < keysProperty.arraySize;
            bool canClear = itemCount > 0;

            bool resolvedTypes = TryResolveKeyValueTypes(
                fieldInfo,
                out Type keyType,
                out Type valueType,
                out bool isSortedDictionary
            );

            object dictionaryInstance;
            if (!resolvedTypes || keyType == null || valueType == null)
            {
                dictionaryInstance = GetDictionaryInstance(dictionaryProperty);
                if (
                    TryResolveKeyValueTypesFromInstance(
                        dictionaryInstance,
                        out Type runtimeKeyType,
                        out Type runtimeValueType,
                        out bool runtimeSorted
                    )
                )
                {
                    keyType ??= runtimeKeyType;
                    valueType ??= runtimeValueType;
                    isSortedDictionary = runtimeSorted;
                }
            }
            else
            {
                dictionaryInstance = GetDictionaryInstance(dictionaryProperty);
            }

            Func<object, object, int> comparison = null;
            if (keyType != null && valueType != null)
            {
                if (isSortedDictionary)
                {
                    object comparerInstance = ResolveComparerInstance(
                        dictionaryProperty,
                        dictionaryInstance,
                        keyType
                    );
                    comparison = CreateComparisonDelegate(comparerInstance, keyType);
                }

                if (comparison == null && KeyTypeSupportsManualSorting(keyType))
                {
                    comparison = CreateManualKeyComparisonDelegate(keyType);
                }
            }
            bool showSort =
                comparison != null
                && keyType != null
                && keysProperty != null
                && itemCount > 1
                && !KeysAreSorted(keysProperty, keyType, comparison);
            bool sortEnabled = showSort;

            GUIContent clearAllContent = EditorGUIUtility.TrTextContent(
                "Clear All",
                "Remove every entry from the dictionary"
            );
            GUIContent removeContent = EditorGUIUtility.TrTextContent("-", "Remove selected entry");
            GUIContent sortContent = EditorGUIUtility.TrTextContent(
                "Sort",
                "Sort entries by key using natural ordering"
            );

            bool showRange = itemCount > 0 || rangeText == "Empty";
            bool showClear = true;
            bool showRemove = true;

            float requiredWidth = CalculateRequiredWidth(
                showRange,
                showClear,
                showSort,
                showRemove
            );
            if (requiredWidth > rect.width && showRange)
            {
                showRange = false;
            }

            requiredWidth = CalculateRequiredWidth(showRange, showClear, showSort, showRemove);
            if (requiredWidth > rect.width && showClear)
            {
                showClear = false;
            }

            requiredWidth = CalculateRequiredWidth(showRange, showClear, showSort, showRemove);
            if (requiredWidth > rect.width && showSort)
            {
                showSort = false;
            }

            requiredWidth = CalculateRequiredWidth(showRange, showClear, showSort, showRemove);
            if (requiredWidth > rect.width && showRemove)
            {
                showRemove = false;
            }

            if (!showRange && !showClear && !showSort && !showRemove)
            {
                return;
            }

            Rect removeRect = default;
            Rect clearRect = default;
            Rect sortRect = default;

            float currentX = rect.xMax - padding;

            if (showRemove)
            {
                currentX -= buttonWidth;
                removeRect = new Rect(currentX, verticalCenter, buttonWidth, lineHeight);
                if (showClear || showSort || showRange)
                {
                    currentX -= PaginationControlSpacing;
                }
            }

            if (showClear)
            {
                currentX -= clearWidth;
                clearRect = new Rect(currentX, verticalCenter, clearWidth, lineHeight);
                if (showSort || showRange)
                {
                    currentX -= PaginationControlSpacing;
                }
            }

            if (showSort)
            {
                currentX -= sortWidth;
                sortRect = new Rect(currentX, verticalCenter, sortWidth, lineHeight);
                if (showRange)
                {
                    currentX -= PaginationControlSpacing;
                }
            }

            if (showRange)
            {
                float labelLeft = rect.x + padding;
                float labelWidth = Mathf.Max(0f, currentX - labelLeft);
                Rect labelRect = new(labelLeft, labelY, labelWidth, labelHeight);
                EditorGUI.LabelField(labelRect, rangeText, footerLabelStyle);
            }

            if (showSort)
            {
                DrawFooterButton(
                    sortRect,
                    sortContent,
                    "Sort",
                    sortEnabled,
                    () =>
                    {
                        if (!sortEnabled)
                        {
                            return;
                        }

                        SortDictionaryEntries(
                            dictionaryProperty,
                            keysProperty,
                            valuesProperty,
                            keyType,
                            valueType,
                            comparison,
                            pagination,
                            list
                        );
                    }
                );
            }

            if (showClear)
            {
                DrawFooterButton(
                    clearRect,
                    clearAllContent,
                    "ClearAll",
                    canClear,
                    () =>
                    {
                        bool confirmed = EditorUtility.DisplayDialog(
                            "Clear Dictionary",
                            "Remove all entries from this dictionary?",
                            "Clear",
                            "Cancel"
                        );
                        if (confirmed)
                        {
                            ClearDictionary(
                                dictionaryProperty,
                                keysProperty,
                                valuesProperty,
                                pagination,
                                list
                            );
                        }
                    }
                );
            }

            if (showRemove)
            {
                DrawFooterButton(
                    removeRect,
                    removeContent,
                    "Remove",
                    canRemove,
                    () =>
                    {
                        RemoveEntryAtIndex(
                            selectedGlobalIndex,
                            list,
                            dictionaryProperty,
                            keysProperty,
                            valuesProperty,
                            pagination
                        );
                    }
                );
            }

            return;

            float CalculateRequiredWidth(
                bool includeRange,
                bool includeClear,
                bool includeSort,
                bool includeRemove
            )
            {
                float width = padding + padding;

                if (includeRange)
                {
                    width += rangeSize.x;
                }

                bool hasRightControls = includeClear || includeSort || includeRemove;
                if (includeRange && hasRightControls)
                {
                    width += PaginationControlSpacing;
                }

                float rightWidth = 0f;
                if (includeRemove)
                {
                    rightWidth += buttonWidth;
                }
                if (includeClear)
                {
                    if (rightWidth > 0f)
                    {
                        rightWidth += PaginationControlSpacing;
                    }
                    rightWidth += clearWidth;
                }
                if (includeSort)
                {
                    if (rightWidth > 0f)
                    {
                        rightWidth += PaginationControlSpacing;
                    }
                    rightWidth += sortWidth;
                }

                width += rightWidth;
                return width;
            }
        }

        internal void RemoveEntryAtIndex(
            int removeIndex,
            ReorderableList list,
            SerializedProperty dictionaryProperty,
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty,
            PaginationState pagination
        )
        {
            if (removeIndex < 0 || removeIndex >= keysProperty.arraySize)
            {
                return;
            }

            SerializedObject serializedObject = dictionaryProperty.serializedObject;
            Object[] targets = serializedObject.targetObjects;
            if (targets.Length > 0)
            {
                Undo.RecordObjects(targets, "Remove Dictionary Entry");
            }

            keysProperty.DeleteArrayElementAtIndex(removeIndex);
            valuesProperty.DeleteArrayElementAtIndex(removeIndex);
            ApplyModifiedPropertiesWithUndoFallback(
                serializedObject,
                dictionaryProperty,
                "RemoveEntry"
            );
            serializedObject.Update();
            SyncRuntimeDictionary(dictionaryProperty);
            foreach (Object target in serializedObject.targetObjects)
            {
                if (target != null)
                {
                    EditorUtility.SetDirty(target);
                }
            }
            ClampPaginationState(pagination, keysProperty.arraySize);

            if (keysProperty.arraySize > 0)
            {
                int clampedIndex = Mathf.Clamp(removeIndex, 0, keysProperty.arraySize - 1);
                pagination.selectedIndex = clampedIndex;
                int totalPages = GetTotalPages(keysProperty.arraySize, pagination.pageSize);
                int desiredPage = GetPageForIndex(clampedIndex, pagination.pageSize);
                pagination.pageIndex = Mathf.Clamp(desiredPage, 0, totalPages - 1);
            }
            else
            {
                pagination.selectedIndex = -1;
                pagination.pageIndex = 0;
            }

            GUI.changed = true;
            InvalidateKeyCache(GetListKey(dictionaryProperty));

            string cacheKey = GetListKey(dictionaryProperty);
            ListPageCache cache = EnsurePageCache(cacheKey, keysProperty, pagination);
            SyncListSelectionWithPagination(list, pagination, cache);

            EditorWindow focusedWindow = EditorWindow.focusedWindow;
            if (focusedWindow != null)
            {
                focusedWindow.Repaint();
            }
        }

        private void ClearDictionary(
            SerializedProperty dictionaryProperty,
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty,
            PaginationState pagination,
            ReorderableList list
        )
        {
            if (keysProperty.arraySize <= 0)
            {
                return;
            }

            SerializedObject serializedObject = dictionaryProperty.serializedObject;
            Object[] targets = serializedObject.targetObjects;
            if (targets.Length > 0)
            {
                Undo.RecordObjects(targets, "Clear Dictionary Entries");
            }

            keysProperty.ClearArray();
            valuesProperty.ClearArray();
            ApplyModifiedPropertiesWithUndoFallback(
                serializedObject,
                dictionaryProperty,
                "ClearEntries"
            );
            SyncRuntimeDictionary(dictionaryProperty);

            ClampPaginationState(pagination, keysProperty.arraySize);
            pagination.selectedIndex = -1;
            pagination.pageIndex = 0;
            GUI.changed = true;
            InvalidateKeyCache(GetListKey(dictionaryProperty));

            string cacheKey = GetListKey(dictionaryProperty);
            ListPageCache cache = EnsurePageCache(cacheKey, keysProperty, pagination);
            SyncListSelectionWithPagination(list, pagination, cache);

            EditorWindow focusedWindow = EditorWindow.focusedWindow;
            if (focusedWindow != null)
            {
                focusedWindow.Repaint();
            }
        }

        private void DrawFooterButton(
            Rect rect,
            GUIContent content,
            string actionKey,
            bool enabled,
            Action onClick
        )
        {
            GUIStyle style = SolidButtonStyles.GetSolidButtonStyle(actionKey, enabled);
            if (!enabled)
            {
                EditorGUI.DrawRect(rect, SolidButtonStyles.DisabledColor);
                GUI.Label(rect, content, style);
                return;
            }

            if (GUI.Button(rect, content, style))
            {
                onClick?.Invoke();
            }
        }

        private void ClampPaginationState(PaginationState pagination, int itemCount)
        {
            if (pagination.pageSize <= 0)
            {
                pagination.pageSize = DefaultPageSize;
            }

            pagination.pageSize = Mathf.Clamp(pagination.pageSize, 1, MaxPageSize);

            int totalPages = GetTotalPages(itemCount, pagination.pageSize);
            pagination.pageIndex = Mathf.Clamp(pagination.pageIndex, 0, totalPages - 1);
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

        private static void SetPageIndex(
            PaginationState pagination,
            int targetPage,
            SerializedProperty keysProperty,
            bool forceImmediateRefresh = false
        )
        {
            if (pagination.pageSize <= 0)
            {
                pagination.pageSize = DefaultPageSize;
            }

            pagination.pageSize = Mathf.Clamp(pagination.pageSize, 1, MaxPageSize);

            int totalPages = GetTotalPages(keysProperty.arraySize, pagination.pageSize);
            int clampedPage = Mathf.Clamp(targetPage, 0, totalPages - 1);
            if (pagination.pageIndex == clampedPage)
            {
                return;
            }

            pagination.pageIndex = clampedPage;
            pagination.selectedIndex = -1;
            GUI.changed = true;
            if (forceImmediateRefresh)
            {
                EditorWindow focusedWindow = EditorWindow.focusedWindow;
                if (focusedWindow != null)
                {
                    focusedWindow.Repaint();
                }
            }
        }

        private void DrawPendingEntryUI(
            ref float y,
            Rect fullPosition,
            PendingEntry pending,
            ReorderableList list,
            PaginationState pagination,
            SerializedProperty dictionaryProperty,
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty,
            Type keyType,
            Type valueType
        )
        {
            int previousIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            try
            {
                AnimBool foldoutAnim = EnsurePendingFoldoutAnim(pending);
                float foldoutProgress;
                if (foldoutAnim != null)
                {
                    foldoutAnim.target = pending.isExpanded;
                    foldoutProgress = foldoutAnim.faded;
                }
                else
                {
                    foldoutProgress = pending.isExpanded ? 1f : 0f;
                }

                PendingSectionMetrics pendingMetrics = CalculatePendingSectionMetrics(
                    pending,
                    keyType,
                    valueType
                );
                float sectionHeight = pendingMetrics.EvaluateHeight(foldoutProgress);
                float rowHeight = EditorGUIUtility.singleLineHeight;
                float spacing = EditorGUIUtility.standardVerticalSpacing;

                Rect containerRect = new(fullPosition.x, y, fullPosition.width, sectionHeight);
                Color backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.18f, 0.18f, 0.18f, 1f)
                    : new Color(0.92f, 0.92f, 0.92f, 1f);
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(containerRect, backgroundColor);
                }

                GUI.BeginGroup(containerRect);

                float innerWidth = Mathf.Max(0f, containerRect.width - PendingSectionPadding * 2f);
                float innerY = PendingSectionPadding;

                Rect headerRect = new(PendingSectionPadding, innerY, innerWidth, rowHeight);
                float resolvedToggleOffset = ResolvePendingFoldoutToggleOffset(dictionaryProperty);
                float toggleWidthBudget = Mathf.Max(0f, headerRect.width - resolvedToggleOffset);
                float foldoutToggleWidth = Mathf.Min(
                    EditorGUIUtility.singleLineHeight,
                    toggleWidthBudget
                );
                Rect foldoutToggleRect = new(
                    headerRect.x + resolvedToggleOffset,
                    headerRect.y,
                    foldoutToggleWidth,
                    headerRect.height
                );
                float labelWidth = Mathf.Max(
                    0f,
                    headerRect.width - (foldoutToggleRect.xMax - headerRect.x)
                );
                Rect labelHitRect = new(
                    foldoutToggleRect.xMax,
                    headerRect.y,
                    labelWidth,
                    headerRect.height
                );
                Rect labelRect = new(
                    labelHitRect.x + PendingFoldoutLabelPadding,
                    headerRect.y,
                    Mathf.Max(0f, labelHitRect.width - PendingFoldoutLabelPadding),
                    headerRect.height
                );

                HasLastPendingHeaderRect = true;
                LastPendingHeaderRect = headerRect;
                LastPendingFoldoutToggleRect = foldoutToggleRect;

                Event currentEvent = Event.current;
                bool expanded = pending.isExpanded;
                if (
                    currentEvent.type == EventType.MouseDown
                    && currentEvent.button == 0
                    && labelHitRect.Contains(currentEvent.mousePosition)
                )
                {
                    expanded = !expanded;
                    GUI.changed = true;
                    currentEvent.Use();
                }

                EditorGUI.BeginChangeCheck();
                bool arrowExpanded = EditorGUI.Foldout(
                    foldoutToggleRect,
                    expanded,
                    GUIContent.none,
                    toggleOnLabelClick: false
                );
                if (EditorGUI.EndChangeCheck())
                {
                    expanded = arrowExpanded;
                }

                GUIStyle pendingLabelStyle = GetPendingFoldoutLabelStyle();
                EditorGUIUtility.AddCursorRect(labelHitRect, MouseCursor.Link);
                EditorGUI.LabelField(labelRect, PendingFoldoutContent, pendingLabelStyle);

                if (expanded != pending.isExpanded)
                {
                    pending.isExpanded = expanded;
                    foldoutAnim = EnsurePendingFoldoutAnim(pending);
                    if (foldoutAnim != null)
                    {
                        foldoutAnim.target = expanded;
                        foldoutProgress = foldoutAnim.faded;
                    }
                    else
                    {
                        foldoutProgress = expanded ? 1f : 0f;
                    }

                    sectionHeight = pendingMetrics.EvaluateHeight(foldoutProgress);
                    RequestRepaint();
                }

                containerRect.height = sectionHeight;

                float contentFade = Mathf.Clamp01(foldoutProgress);
                if (contentFade <= 0f && !pending.isExpanded)
                {
                    GUI.EndGroup();
                    y = containerRect.yMax;
                    LastPendingValueUsedFoldoutLabel = false;
                    LastPendingValueFoldoutOffset = 0f;
                    return;
                }

                innerY += rowHeight + spacing;

                Color previousColor = GUI.color;
                bool adjustColor = !Mathf.Approximately(contentFade, 1f);
                if (adjustColor)
                {
                    GUI.color = new Color(
                        previousColor.r,
                        previousColor.g,
                        previousColor.b,
                        previousColor.a * contentFade
                    );
                }

                float keyHeight = pendingMetrics.KeyHeight;
                Rect keyRect = new(PendingSectionPadding, innerY, innerWidth, keyHeight);
                pending.key = DrawFieldForType(
                    keyRect,
                    "Key",
                    pending.key,
                    keyType,
                    pending,
                    false
                );
                innerY += keyHeight + spacing;

                float valueHeight = pendingMetrics.ValueHeight;
                Rect valueRect = new(PendingSectionPadding, innerY, innerWidth, valueHeight);
                bool pendingValueSupportsFoldout = PendingValueSupportsFoldout(pending, valueType);
                float pendingValueFoldoutOffset = pendingValueSupportsFoldout
                    ? PendingExpandableValueFoldoutGutter
                    : 0f;
                if (pendingValueFoldoutOffset > 0f)
                {
                    valueRect.x += pendingValueFoldoutOffset;
                    valueRect.width = Mathf.Max(0f, valueRect.width - pendingValueFoldoutOffset);
                }
                pending.value = DrawFieldForType(
                    valueRect,
                    "Value",
                    pending.value,
                    valueType,
                    pending,
                    true
                );
                LastPendingValueUsedFoldoutLabel = pendingValueSupportsFoldout;
                LastPendingValueFoldoutOffset = pendingValueFoldoutOffset;
                if (
                    pendingValueSupportsFoldout
                    && pending.valueWrapperProperty != null
                    && !pending.valueWrapperProperty.isExpanded
                )
                {
                    pending.valueWrapperProperty.isExpanded = true;
                }
                innerY += valueHeight + spacing;

                bool keySupported = IsTypeSupported(keyType);
                bool valueSupported = IsTypeSupported(valueType);
                string pendingKeyString = pending.key as string;
                bool keyValid = KeyIsValid(keyType, pending.key);
                bool isBlankStringKey =
                    keyType == typeof(string) && string.IsNullOrWhiteSpace(pendingKeyString);
                bool canCommit = keySupported && valueSupported && (keyValid || isBlankStringKey);

                int existingIndex = FindExistingKeyIndex(
                    dictionaryProperty,
                    keysProperty,
                    keyType,
                    pending.key
                );
                string buttonLabel = existingIndex >= 0 ? "Overwrite" : "Add";
                bool entryAlreadyExists =
                    existingIndex >= 0
                    && EntryMatchesExisting(
                        keysProperty,
                        valuesProperty,
                        existingIndex,
                        keyType,
                        valueType,
                        pending
                    );
                if (entryAlreadyExists)
                {
                    canCommit = false;
                }

                Rect buttonsRect = new(PendingSectionPadding, innerY, innerWidth, rowHeight);
                float resetWidth = 70f;
                Rect addRect = new(buttonsRect.x, buttonsRect.y, PendingAddButtonWidth, rowHeight);
                Rect resetRect = new(addRect.xMax + spacing, buttonsRect.y, resetWidth, rowHeight);

                float infoX = resetRect.xMax + spacing;
                float availableInfoWidth = Mathf.Max(0f, buttonsRect.xMax - infoX);
                string infoMessage;
                if (entryAlreadyExists)
                {
                    infoMessage = "Entry already exists with the same value.";
                }
                else
                {
                    infoMessage = GetPendingInfoMessage(
                        keySupported,
                        valueSupported,
                        keyValid || isBlankStringKey,
                        existingIndex,
                        keyType,
                        valueType
                    );
                    if (string.IsNullOrEmpty(infoMessage) && isBlankStringKey)
                    {
                        string descriptor = string.IsNullOrEmpty(pendingKeyString)
                            ? "empty"
                            : "whitespace-only";
                        infoMessage = $"Adding entry with {descriptor} string key.";
                    }
                }
                if (availableInfoWidth > 0f && !string.IsNullOrEmpty(infoMessage))
                {
                    Rect infoRect = new(infoX, buttonsRect.y, availableInfoWidth, rowHeight);
                    GUI.Label(infoRect, infoMessage, EditorStyles.miniLabel);
                }

                using (new EditorGUI.DisabledScope(!canCommit))
                {
                    string tooltip;
                    if (entryAlreadyExists)
                    {
                        tooltip = "Current item already exists in the dictionary.";
                    }
                    else if (existingIndex >= 0)
                    {
                        tooltip = "Overwrite the existing entry with this key.";
                    }
                    else if (isBlankStringKey)
                    {
                        string descriptor = string.IsNullOrEmpty(pendingKeyString)
                            ? "empty string"
                            : "whitespace-only string";
                        tooltip = $"Add a new entry using a {descriptor} key.";
                    }
                    else
                    {
                        tooltip = "Add a new entry to the dictionary.";
                    }

                    GUIContent addContent = EditorGUIUtility.TrTextContent(buttonLabel, tooltip);

                    string styleKey =
                        existingIndex >= 0 ? "Overwrite"
                        : isBlankStringKey ? "AddEmpty"
                        : "Add";
                    GUIStyle addStyle = SolidButtonStyles.GetSolidButtonStyle(
                        styleKey,
                        GUI.enabled
                    );

                    if (GUI.Button(addRect, addContent, addStyle))
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
                        if (result.added)
                        {
                            pending.key = GetDefaultValue(keyType);
                            pending.value = GetDefaultValue(valueType);
                            ReleasePendingWrapper(pending, false);
                            ReleasePendingWrapper(pending, true);
                        }

                        if (result.index >= 0)
                        {
                            int targetPage = GetPageForIndex(result.index, pagination.pageSize);
                            int totalPages = GetTotalPages(
                                keysProperty.arraySize,
                                pagination.pageSize
                            );
                            pagination.pageIndex = Mathf.Clamp(targetPage, 0, totalPages - 1);
                            pagination.selectedIndex = result.index;

                            string listKey = GetListKey(dictionaryProperty);
                            ListPageCache cache = EnsurePageCache(
                                listKey,
                                keysProperty,
                                pagination
                            );
                            SyncListSelectionWithPagination(list, pagination, cache);
                        }

                        GUI.FocusControl(null);
                    }
                }

                bool isPendingDefault = PendingEntryIsAtDefault(pending, keyType, valueType);
                GUIContent resetContent = EditorGUIUtility.TrTextContent(
                    "Reset",
                    "Restore pending key/value to their defaults"
                );
                using (new EditorGUI.DisabledScope(isPendingDefault))
                {
                    GUIStyle resetStyle = SolidButtonStyles.GetSolidButtonStyle(
                        "Reset",
                        GUI.enabled
                    );
                    if (GUI.Button(resetRect, resetContent, resetStyle))
                    {
                        ResetPendingEntryToDefault(pending, keyType, valueType);
                        GUI.FocusControl(null);
                    }
                }

                if (adjustColor)
                {
                    GUI.color = previousColor;
                }

                GUI.EndGroup();

                y = containerRect.yMax;
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        private static float ResolvePendingFoldoutToggleOffset(
            SerializedProperty dictionaryProperty
        )
        {
            SerializedObject serializedObject = dictionaryProperty?.serializedObject;
            if (!TargetsUnityHelpersSettings(serializedObject))
            {
                return PendingFoldoutToggleOffset;
            }

            return PendingFoldoutToggleOffsetProjectSettings;
        }

        private static bool TargetsUnityHelpersSettings(SerializedObject serializedObject)
        {
            if (serializedObject == null)
            {
                return false;
            }

            if (serializedObject.targetObject is UnityHelpersSettings)
            {
                return true;
            }

            Object[] targets = serializedObject.targetObjects;
            if (targets == null || targets.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < targets.Length; index++)
            {
                if (targets[index] is UnityHelpersSettings)
                {
                    return true;
                }
            }

            return false;
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

        private static bool PendingEntryIsAtDefault(
            PendingEntry pending,
            Type keyType,
            Type valueType
        )
        {
            object defaultKey = GetDefaultValue(keyType);
            object defaultValue = GetDefaultValue(valueType);
            return ValuesEqual(pending.key, defaultKey) && ValuesEqual(pending.value, defaultValue);
        }

        private static void ResetPendingEntryToDefault(
            PendingEntry pending,
            Type keyType,
            Type valueType
        )
        {
            pending.key = GetDefaultValue(keyType);
            pending.value = GetDefaultValue(valueType);
            ReleasePendingWrapper(pending, false);
            ReleasePendingWrapper(pending, true);
        }

        private static PendingSectionMetrics CalculatePendingSectionMetrics(
            PendingEntry pending,
            Type keyType,
            Type valueType
        )
        {
            float rowHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float keyHeight = GetPendingFieldHeight(pending, keyType, isValueField: false);
            float valueHeight = GetPendingFieldHeight(pending, valueType, isValueField: true);
            float collapsedHeight = rowHeight + PendingSectionPadding * 2f;
            float expandedExtraHeight =
                spacing + keyHeight + spacing + valueHeight + spacing + rowHeight;
            return new PendingSectionMetrics(
                collapsedHeight,
                expandedExtraHeight,
                keyHeight,
                valueHeight
            );
        }

        private static float GetPendingSectionHeight(
            PendingEntry pending,
            Type keyType,
            Type valueType
        )
        {
            PendingSectionMetrics metrics = CalculatePendingSectionMetrics(
                pending,
                keyType,
                valueType
            );
            return metrics.EvaluateHeight(GetPendingFoldoutProgress(pending));
        }

        private readonly struct PendingSectionMetrics
        {
            public float CollapsedHeight { get; }
            public float ExpandedExtraHeight { get; }
            public float KeyHeight { get; }
            public float ValueHeight { get; }

            public PendingSectionMetrics(
                float collapsedHeight,
                float expandedExtraHeight,
                float keyHeight,
                float valueHeight
            )
            {
                CollapsedHeight = collapsedHeight;
                ExpandedExtraHeight = expandedExtraHeight;
                KeyHeight = keyHeight;
                ValueHeight = valueHeight;
            }

            public float EvaluateHeight(float foldoutProgress)
            {
                return CollapsedHeight + ExpandedExtraHeight * Mathf.Clamp01(foldoutProgress);
            }
        }

        internal CommitResult CommitEntry(
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty,
            Type keyType,
            Type valueType,
            object key,
            object value,
            SerializedProperty dictionaryProperty,
            int existingIndex = -1,
            bool isSortedDictionary = false
        )
        {
            PendingEntry pending = new()
            {
                key = key,
                value = value,
                isSorted = isSortedDictionary,
            };
            return CommitEntry(
                keysProperty,
                valuesProperty,
                keyType,
                valueType,
                pending,
                existingIndex,
                dictionaryProperty
            );
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
            Object[] targets = serializedObject.targetObjects;
            if (targets.Length > 0)
            {
                string undoLabel =
                    existingIndex >= 0 ? "Overwrite Dictionary Entry" : "Add Dictionary Entry";
                Undo.RecordObjects(targets, undoLabel);
            }

            bool addedNewEntry = false;
            int affectedIndex;

            if (existingIndex >= 0)
            {
                SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(
                    existingIndex
                );
                SetPropertyValue(valueProperty, pending.value, valueType);
                RegisterPaletteManualEditForKey(dictionaryProperty, pending.key, keyType);
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

                SetPropertyValue(keyProperty, pending.key, keyType);
                SetPropertyValue(valueProperty, pending.value, valueType);
                RegisterPaletteManualEditForKey(dictionaryProperty, pending.key, keyType);
                addedNewEntry = true;
                affectedIndex = insertIndex;
            }

            ApplyModifiedPropertiesWithUndoFallback(
                serializedObject,
                dictionaryProperty,
                "CommitEntry"
            );
            serializedObject.Update();
            SyncRuntimeDictionary(dictionaryProperty);

            if (targets.Length > 0)
            {
                foreach (Object target in targets)
                {
                    if (target != null)
                    {
                        EditorUtility.SetDirty(target);
                    }
                }
            }

            string listKey = GetListKey(dictionaryProperty);
            MarkListCacheDirty(listKey);
            InvalidateKeyCache(listKey);
            GUI.changed = true;
            return new CommitResult { added = addedNewEntry, index = affectedIndex };
        }

        private static bool TryResolveKeyValueTypes(
            FieldInfo fieldInfo,
            out Type keyType,
            out Type valueType,
            out bool isSortedDictionary
        )
        {
            keyType = null;
            valueType = null;
            isSortedDictionary = false;
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
                    if (
                        genericDefinition == typeof(SerializableSortedDictionary<,>)
                        || genericDefinition == typeof(SerializableSortedDictionary<,,>)
                        || genericDefinition == typeof(SerializableSortedDictionaryBase<,,>)
                    )
                    {
                        Type[] args = type.GetGenericArguments();
                        keyType = args[0];
                        valueType = args[1];
                        isSortedDictionary = true;
                        return true;
                    }

                    if (genericDefinition == typeof(SerializableDictionary<,>))
                    {
                        Type[] args = type.GetGenericArguments();
                        keyType = args[0];
                        valueType = args[1];
                        isSortedDictionary = false;
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
                        isSortedDictionary = false;
                        return true;
                    }
                }

                type = type.BaseType;
            }

            return false;
        }

        private static object GetDictionaryInstance(SerializedProperty dictionaryProperty)
        {
            if (dictionaryProperty == null)
            {
                return null;
            }

            SerializedObject serializedObject = dictionaryProperty.serializedObject;
            if (serializedObject == null || serializedObject.targetObject == null)
            {
                return null;
            }

            return GetTargetObjectOfProperty(
                serializedObject.targetObject,
                dictionaryProperty.propertyPath
            );
        }

        private static bool TryResolveKeyValueTypesFromInstance(
            object dictionaryInstance,
            out Type keyType,
            out Type valueType,
            out bool isSortedDictionary
        )
        {
            keyType = null;
            valueType = null;
            isSortedDictionary = false;

            if (dictionaryInstance == null)
            {
                return false;
            }

            Type type = dictionaryInstance.GetType();
            while (type != null)
            {
                if (type.IsGenericType)
                {
                    Type genericDefinition = type.GetGenericTypeDefinition();
                    if (
                        genericDefinition == typeof(SerializableSortedDictionary<,>)
                        || genericDefinition == typeof(SerializableSortedDictionary<,,>)
                        || genericDefinition == typeof(SerializableSortedDictionaryBase<,,>)
                    )
                    {
                        Type[] args = type.GetGenericArguments();
                        keyType = args[0];
                        valueType = args[1];
                        isSortedDictionary = true;
                        return true;
                    }

                    if (
                        genericDefinition == typeof(SerializableDictionary<,>)
                        || genericDefinition == typeof(SerializableDictionary<,,>)
                        || genericDefinition == typeof(SerializableDictionaryBase<,,>)
                    )
                    {
                        Type[] args = type.GetGenericArguments();
                        keyType = args[0];
                        valueType = args[1];
                        isSortedDictionary = false;
                        return true;
                    }
                }

                type = type.BaseType;
            }

            return false;
        }

        private static object ResolveComparerInstance(
            SerializedProperty dictionaryProperty,
            object dictionaryInstance,
            Type keyType
        )
        {
            object instance = dictionaryInstance ?? GetDictionaryInstance(dictionaryProperty);
            if (instance == null)
            {
                return CreateDefaultComparer(keyType);
            }

            PropertyInfo comparerProperty = instance
                .GetType()
                .GetProperty("Comparer", BindingFlags.Public | BindingFlags.Instance);
            if (comparerProperty != null)
            {
                object comparerValue = comparerProperty.GetValue(instance);
                if (comparerValue != null)
                {
                    return comparerValue;
                }
            }

            return CreateDefaultComparer(keyType);
        }

        private static object CreateDefaultComparer(Type keyType)
        {
            if (keyType == null)
            {
                return null;
            }

            Type comparerType = typeof(Comparer<>).MakeGenericType(keyType);
            PropertyInfo defaultProperty = comparerType.GetProperty(
                "Default",
                BindingFlags.Public | BindingFlags.Static
            );
            return defaultProperty != null ? defaultProperty.GetValue(null) : null;
        }

        private static Func<object, object, int> CreateComparisonDelegate(
            object comparer,
            Type keyType
        )
        {
            object comparerInstance = comparer ?? CreateDefaultComparer(keyType);
            if (comparerInstance == null || keyType == null)
            {
                return null;
            }

            MethodInfo compareMethod = comparerInstance
                .GetType()
                .GetMethod("Compare", new[] { keyType, keyType });
            if (compareMethod == null)
            {
                return null;
            }

            return delegate(object left, object right)
            {
                object leftValue = ConvertKeyForComparison(left, keyType);
                object rightValue = ConvertKeyForComparison(right, keyType);
                object result = compareMethod.Invoke(
                    comparerInstance,
                    new[] { leftValue, rightValue }
                );
                return result != null ? Convert.ToInt32(result, CultureInfo.InvariantCulture) : 0;
            };
        }

        private static Func<object, object, int> CreateManualKeyComparisonDelegate(Type keyType)
        {
            if (!KeyTypeSupportsManualSorting(keyType))
            {
                return null;
            }

            if (IsUnityObjectKey(keyType))
            {
                return CreateUnityObjectComparisonDelegate(keyType);
            }

            return CreateComparisonDelegate(null, keyType);
        }

        private static Func<object, object, int> CreateUnityObjectComparisonDelegate(Type keyType)
        {
            if (!IsUnityObjectKey(keyType))
            {
                return null;
            }

            return delegate(object left, object right)
            {
                Object leftObject = ConvertKeyToUnityObject(left);
                Object rightObject = ConvertKeyToUnityObject(right);
                return UnityObjectNameComparer<Object>.Instance.Compare(leftObject, rightObject);
            };
        }

        private static Object ConvertKeyToUnityObject(object key)
        {
            return key as Object;
        }

        internal static bool ShouldShowDictionarySortButton(
            SerializedProperty keysProperty,
            Type keyType,
            int itemCount,
            Func<object, object, int> comparison
        )
        {
            if (comparison == null || keyType == null || keysProperty == null || itemCount <= 1)
            {
                return false;
            }

            return !KeysAreSorted(keysProperty, keyType, comparison);
        }

        private static bool KeyTypeSupportsManualSorting(Type keyType)
        {
            if (keyType == null)
            {
                return false;
            }

            if (IsUnityObjectKey(keyType))
            {
                return true;
            }

            Type candidate = Nullable.GetUnderlyingType(keyType) ?? keyType;
            if (typeof(IComparable).IsAssignableFrom(candidate))
            {
                return true;
            }

            Type genericComparable = typeof(IComparable<>).MakeGenericType(candidate);
            return genericComparable.IsAssignableFrom(candidate);
        }

        private static bool IsUnityObjectKey(Type keyType)
        {
            return keyType != null && typeof(Object).IsAssignableFrom(keyType);
        }

        private static object ConvertKeyForComparison(object key, Type keyType)
        {
            if (key == null)
            {
                return null;
            }

            if (keyType.IsInstanceOfType(key))
            {
                return key;
            }

            if (keyType.IsEnum)
            {
                return Enum.ToObject(keyType, key);
            }

            try
            {
                return Convert.ChangeType(key, keyType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return key;
            }
        }

        internal static bool KeysAreSorted(
            SerializedProperty keysProperty,
            Type keyType,
            Func<object, object, int> comparison
        )
        {
            if (keysProperty == null || comparison == null)
            {
                return false;
            }

            int count = keysProperty.arraySize;
            if (count <= 1)
            {
                return true;
            }

            SerializedProperty firstProperty = keysProperty.GetArrayElementAtIndex(0);
            object previous = GetPropertyValue(firstProperty, keyType);

            for (int index = 1; index < count; index++)
            {
                SerializedProperty currentProperty = keysProperty.GetArrayElementAtIndex(index);
                object currentKey = GetPropertyValue(currentProperty, keyType);
                int compareResult = comparison(previous, currentKey);
                if (compareResult > 0)
                {
                    return false;
                }

                previous = currentKey;
            }

            return true;
        }

        internal void SortDictionaryEntries(
            SerializedProperty dictionaryProperty,
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty,
            Type keyType,
            Type valueType,
            Func<object, object, int> comparison,
            PaginationState pagination,
            ReorderableList list
        )
        {
            if (comparison == null)
            {
                return;
            }

            int count = keysProperty.arraySize;
            if (count <= 1)
            {
                return;
            }

            SerializedObject serializedObject = dictionaryProperty.serializedObject;
            Object[] targets = serializedObject.targetObjects;
            if (targets.Length > 0)
            {
                Undo.RecordObjects(targets, "Sort Dictionary Keys");
            }

            object selectedKey = null;
            int selectedIndex = pagination.selectedIndex;
            if (selectedIndex >= 0 && selectedIndex < count)
            {
                SerializedProperty selectedProperty = keysProperty.GetArrayElementAtIndex(
                    selectedIndex
                );
                selectedKey = GetPropertyValue(selectedProperty, keyType);
            }

            List<KeyValueSnapshot> entries = new(count);
            for (int index = 0; index < count; index++)
            {
                SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(index);
                SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(index);
                KeyValueSnapshot snapshot = new()
                {
                    key = GetPropertyValue(keyProperty, keyType),
                    value = GetPropertyValue(valueProperty, valueType),
                    originalIndex = index,
                };
                entries.Add(snapshot);
            }

            KeyValueSnapshotComparer comparer = new(comparison);
            entries.Sort(comparer);

            for (int index = 0; index < entries.Count; index++)
            {
                KeyValueSnapshot snapshot = entries[index];
                SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(index);
                SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(index);
                SetPropertyValue(keyProperty, snapshot.key, keyType);
                SetPropertyValue(valueProperty, snapshot.value, valueType);
            }

            ApplyModifiedPropertiesWithUndoFallback(
                serializedObject,
                dictionaryProperty,
                "SortEntries"
            );
            SyncRuntimeDictionary(dictionaryProperty);

            string listKey = GetListKey(dictionaryProperty);
            InvalidateKeyCache(listKey);
            MarkListCacheDirty(listKey);

            int newSelectedIndex = -1;
            if (selectedKey != null)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    if (ValuesEqual(entries[index].key, selectedKey))
                    {
                        newSelectedIndex = index;
                        break;
                    }
                }
            }

            if (newSelectedIndex >= 0)
            {
                pagination.selectedIndex = newSelectedIndex;
                int totalPages = GetTotalPages(keysProperty.arraySize, pagination.pageSize);
                int targetPage = GetPageForIndex(newSelectedIndex, pagination.pageSize);
                pagination.pageIndex = Mathf.Clamp(targetPage, 0, totalPages - 1);
            }
            else
            {
                pagination.selectedIndex = Mathf.Min(
                    keysProperty.arraySize - 1,
                    pagination.selectedIndex
                );
                pagination.selectedIndex = Mathf.Max(pagination.selectedIndex, -1);
                int totalPages = GetTotalPages(keysProperty.arraySize, pagination.pageSize);
                int targetPage = GetPageForIndex(
                    Mathf.Max(pagination.selectedIndex, 0),
                    pagination.pageSize
                );
                pagination.pageIndex = Mathf.Clamp(targetPage, 0, totalPages - 1);
            }

            ListPageCache updatedCache = EnsurePageCache(listKey, keysProperty, pagination);
            SyncListSelectionWithPagination(list, pagination, updatedCache);

            GUI.changed = true;

            EditorWindow focusedWindow = EditorWindow.focusedWindow;
            if (focusedWindow != null)
            {
                focusedWindow.Repaint();
            }
        }

        private sealed class KeyValueSnapshot
        {
            public object key;
            public object value;
            public int originalIndex;
        }

        private sealed class KeyValueSnapshotComparer : IComparer<KeyValueSnapshot>
        {
            private readonly Func<object, object, int> _comparison;

            public KeyValueSnapshotComparer(Func<object, object, int> comparison)
            {
                _comparison = comparison;
            }

            public int Compare(KeyValueSnapshot x, KeyValueSnapshot y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                if (_comparison == null)
                {
                    return 0;
                }

                int comparisonResult = _comparison(x.key, y.key);
                if (comparisonResult != 0)
                {
                    return comparisonResult;
                }

                return x.originalIndex.CompareTo(y.originalIndex);
            }
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

        internal static string GetListKey(SerializedProperty property)
        {
            return BuildPropertyCacheKey(property);
        }

        private static string BuildPropertyCacheKey(SerializedProperty property)
        {
            if (property == null)
            {
                return string.Empty;
            }

            SerializedObject serializedObject = property.serializedObject;
            string propertyPath = property.propertyPath ?? string.Empty;

            if (serializedObject == null)
            {
                return propertyPath;
            }

            Object[] targets = serializedObject.targetObjects;
            if (targets == null || targets.Length == 0)
            {
                int fallbackId = RuntimeHelpers.GetHashCode(serializedObject);
                return $"{fallbackId}_{propertyPath}";
            }

            if (targets.Length == 1 && targets[0] != null)
            {
                return $"{targets[0].GetInstanceID()}_{propertyPath}";
            }

            using PooledResource<StringBuilder> keyBuilderLease = Buffers.GetStringBuilder(
                propertyPath.Length + Math.Max(32, targets.Length * 12),
                out StringBuilder keyBuilder
            );
            keyBuilder.Append(propertyPath);
            keyBuilder.Append('|');

            for (int index = 0; index < targets.Length; index++)
            {
                int id = targets[index] != null ? targets[index].GetInstanceID() : 0;
                keyBuilder.Append(id);
                if (index < targets.Length - 1)
                {
                    keyBuilder.Append(',');
                }
            }

            return keyBuilder.ToString();
        }

        internal static bool ApplyModifiedPropertiesWithUndoFallback(
            SerializedObject serializedObject
        )
        {
            return ApplyModifiedPropertiesWithUndoFallback(
                serializedObject,
                propertyPath: null,
                operation: null,
                static so => so.ApplyModifiedProperties(),
                static so => so.ApplyModifiedPropertiesWithoutUndo()
            );
        }

        internal static bool ApplyModifiedPropertiesWithUndoFallback(
            SerializedObject serializedObject,
            SerializedProperty contextProperty,
            string operation
        )
        {
            string propertyPath = contextProperty != null ? contextProperty.propertyPath : null;
            return ApplyModifiedPropertiesWithUndoFallback(
                serializedObject,
                propertyPath,
                operation,
                static so => so.ApplyModifiedProperties(),
                static so => so.ApplyModifiedPropertiesWithoutUndo()
            );
        }

        internal static bool ApplyModifiedPropertiesWithUndoFallback(
            SerializedObject serializedObject,
            Func<SerializedObject, bool> applyWithUndo,
            Func<SerializedObject, bool> applyWithoutUndo
        )
        {
            return ApplyModifiedPropertiesWithUndoFallback(
                serializedObject,
                propertyPath: null,
                operation: null,
                applyWithUndo,
                applyWithoutUndo
            );
        }

        private static bool ApplyModifiedPropertiesWithUndoFallback(
            SerializedObject serializedObject,
            string propertyPath,
            string operation,
            Func<SerializedObject, bool> applyWithUndo,
            Func<SerializedObject, bool> applyWithoutUndo
        )
        {
            if (serializedObject == null)
            {
                return false;
            }

            Func<SerializedObject, bool> applyFunc =
                applyWithUndo ?? (static so => so.ApplyModifiedProperties());
            Func<SerializedObject, bool> fallbackFunc =
                applyWithoutUndo ?? (static so => so.ApplyModifiedPropertiesWithoutUndo());

            bool hadChangesBefore = serializedObject.hasModifiedProperties;
            if (applyFunc(serializedObject))
            {
                PaletteSerializationDiagnostics.ReportDrawerApplyResult(
                    serializedObject,
                    propertyPath,
                    operation,
                    PaletteSerializationDiagnostics.DrawerApplyResult.UndoPathSucceeded,
                    hadChangesBefore,
                    serializedObject.hasModifiedProperties
                );
                return true;
            }

            bool fallbackApplied = fallbackFunc(serializedObject);
            PaletteSerializationDiagnostics.ReportDrawerApplyResult(
                serializedObject,
                propertyPath,
                operation,
                fallbackApplied
                    ? PaletteSerializationDiagnostics.DrawerApplyResult.FallbackPathSucceeded
                    : PaletteSerializationDiagnostics.DrawerApplyResult.Failed,
                hadChangesBefore,
                serializedObject.hasModifiedProperties
            );
            return fallbackApplied;
        }

        private static void ApplyAndSyncPaletteRowChange(
            SerializedProperty dictionaryProperty,
            string operation
        )
        {
            if (
                dictionaryProperty == null
                || !PaletteSerializationDiagnostics.IsPaletteProperty(
                    dictionaryProperty.propertyPath
                )
            )
            {
                return;
            }

            SerializedObject serializedObject = dictionaryProperty.serializedObject;
            bool applied = ApplyModifiedPropertiesWithUndoFallback(
                serializedObject,
                dictionaryProperty,
                operation
            );
            if (!applied)
            {
                return;
            }

            serializedObject.Update();
            SyncRuntimeDictionary(dictionaryProperty);
        }

        private static bool KeyIsValid(Type keyType, object keyValue)
        {
            if (keyType == typeof(string))
            {
                return !string.IsNullOrEmpty(keyValue as string);
            }

            if (typeof(Object).IsAssignableFrom(keyType))
            {
                return keyValue is Object obj && obj != null;
            }

            return keyValue != null || keyType.IsValueType;
        }

        private int FindExistingKeyIndex(
            SerializedProperty dictionaryProperty,
            SerializedProperty keysProperty,
            Type keyType,
            object keyValue
        )
        {
            string cacheKey = GetListKey(dictionaryProperty);
            KeyIndexCache cache = GetOrBuildKeyIndexCache(cacheKey, keysProperty, keyType);
            object lookupKey = keyValue ?? NullKeySentinel;
            return cache.indices.GetValueOrDefault(lookupKey, -1);
        }

        private static bool EntryMatchesExisting(
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty,
            int existingIndex,
            Type keyType,
            Type valueType,
            PendingEntry pending
        )
        {
            if (existingIndex < 0 || existingIndex >= keysProperty.arraySize)
            {
                return false;
            }

            SerializedProperty existingKeyProperty = keysProperty.GetArrayElementAtIndex(
                existingIndex
            );
            SerializedProperty existingValueProperty = valuesProperty.GetArrayElementAtIndex(
                existingIndex
            );
            object existingKey = GetPropertyValue(existingKeyProperty, keyType);
            object existingValue = GetPropertyValue(existingValueProperty, valueType);

            return ValuesEqual(existingKey, pending.key)
                && ValuesEqual(existingValue, pending.value);
        }

        private KeyIndexCache GetOrBuildKeyIndexCache(
            string cacheKey,
            SerializedProperty keysProperty,
            Type keyType
        )
        {
            if (_keyIndexCaches.TryGetValue(cacheKey, out KeyIndexCache cache))
            {
                if (cache.arraySize != keysProperty.arraySize)
                {
                    PopulateKeyIndexCache(cache, keysProperty, keyType);
                }
                return cache;
            }

            cache = new KeyIndexCache();
            PopulateKeyIndexCache(cache, keysProperty, keyType);
            _keyIndexCaches[cacheKey] = cache;
            return cache;
        }

        private static void PopulateKeyIndexCache(
            KeyIndexCache cache,
            SerializedProperty keysProperty,
            Type keyType
        )
        {
            cache.indices.Clear();
            cache.arraySize = keysProperty.arraySize;
            int count = keysProperty.arraySize;
            for (int i = 0; i < count; i++)
            {
                SerializedProperty element = keysProperty.GetArrayElementAtIndex(i);
                object keyValue = GetPropertyValue(element, keyType) ?? NullKeySentinel;
                cache.indices.TryAdd(keyValue, i);
            }
        }

        private void InvalidateKeyCache(string cacheKey)
        {
            _keyIndexCaches.Remove(cacheKey);
            MarkListCacheDirty(cacheKey);
        }

        private static bool ShouldTweenPendingFoldout(bool isSortedDictionary)
        {
            return isSortedDictionary
                ? UnityHelpersSettings.ShouldTweenSerializableSortedDictionaryFoldouts()
                : UnityHelpersSettings.ShouldTweenSerializableDictionaryFoldouts();
        }

        private static float GetPendingFoldoutAnimationSpeed(bool isSortedDictionary)
        {
            return isSortedDictionary
                ? UnityHelpersSettings.GetSerializableSortedDictionaryFoldoutSpeed()
                : UnityHelpersSettings.GetSerializableDictionaryFoldoutSpeed();
        }

        private static AnimBool CreatePendingFoldoutAnim(bool initialValue, bool isSortedDictionary)
        {
            AnimBool anim = new(initialValue)
            {
                speed = GetPendingFoldoutAnimationSpeed(isSortedDictionary),
            };
            anim.valueChanged.AddListener(RequestRepaint);
            return anim;
        }

        private static AnimBool EnsurePendingFoldoutAnim(PendingEntry pending)
        {
            if (pending == null)
            {
                return null;
            }

            bool shouldTween = ShouldTweenPendingFoldout(pending.isSorted);
            if (!shouldTween)
            {
                if (pending.foldoutAnim != null)
                {
                    pending.foldoutAnim.valueChanged.RemoveListener(RequestRepaint);
                    pending.foldoutAnim = null;
                }

                return null;
            }

            if (pending.foldoutAnim == null)
            {
                pending.foldoutAnim = CreatePendingFoldoutAnim(
                    pending.isExpanded,
                    pending.isSorted
                );
            }
            else
            {
                pending.foldoutAnim.speed = GetPendingFoldoutAnimationSpeed(pending.isSorted);
            }

            return pending.foldoutAnim;
        }

        private static void RequestRepaint()
        {
            InternalEditorUtility.RepaintAllViews();
        }

        private static float GetPendingFoldoutProgress(PendingEntry pending)
        {
            if (pending == null)
            {
                return 0f;
            }

            AnimBool anim = EnsurePendingFoldoutAnim(pending);
            if (anim == null)
            {
                return pending.isExpanded ? 1f : 0f;
            }

            anim.target = pending.isExpanded;
            return anim.faded;
        }

        private static GUIStyle GetFooterLabelStyle()
        {
            if (_footerLabelStyle != null)
            {
                return _footerLabelStyle;
            }

            _footerLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 10),
            };

            return _footerLabelStyle;
        }

        private static GUIStyle GetPendingFoldoutLabelStyle()
        {
            if (_pendingFoldoutLabelStyle != null)
            {
                return _pendingFoldoutLabelStyle;
            }

            _pendingFoldoutLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };

            return _pendingFoldoutLabelStyle;
        }

        private static float GetPendingFieldHeight(
            PendingEntry pending,
            Type fieldType,
            bool isValueField
        )
        {
            PaletteValueRenderer paletteRenderer = ResolvePaletteValueRenderer(fieldType);
            if (paletteRenderer != null)
            {
                return paletteRenderer.Height;
            }

            float defaultHeight = EditorGUIUtility.singleLineHeight;
            if (
                pending == null
                || fieldType == null
                || fieldType == typeof(string)
                || fieldType.IsPrimitive
                || fieldType.IsEnum
                || typeof(Object).IsAssignableFrom(fieldType)
                || !TypeSupportsComplexEditing(fieldType)
            )
            {
                return defaultHeight;
            }

            PendingWrapperContext context = EnsurePendingWrapper(pending, fieldType, isValueField);
            if (context.Property == null)
            {
                return defaultHeight;
            }

            if (TypeSupportsComplexEditing(fieldType))
            {
                context.Property.isExpanded = true;
            }

            return EditorGUI.GetPropertyHeight(context.Property, true);
        }

        internal static object DrawFieldForType(
            Rect rect,
            string label,
            object current,
            Type type,
            PendingEntry pending,
            bool isValueField
        )
        {
            GUIContent content = new(label);

            if (TryDrawPaletteValueField(rect, content, ref current, type))
            {
                return current;
            }

            if (TryDrawComplexTypeField(rect, content, ref current, type, pending, isValueField))
            {
                return current;
            }

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
                return EditorGUI.Toggle(rect, content, current is true);
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
                // ReSharper disable once InconsistentNaming
                Vector2Int value = current is Vector2Int v2int ? v2int : default;
                Vector2Int newValue = EditorGUI.Vector2IntField(rect, label, value);
                return newValue;
            }

            if (type == typeof(Vector3Int))
            {
                // ReSharper disable once InconsistentNaming
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

            if (typeof(Object).IsAssignableFrom(type))
            {
                Object obj = current as Object;
                return EditorGUI.ObjectField(rect, content, obj, type, allowSceneObjects: false);
            }

            EditorGUI.LabelField(rect, content, new GUIContent($"Unsupported type ({type.Name})"));
            return current;
        }

        private static bool TryDrawComplexTypeField(
            Rect rect,
            GUIContent content,
            ref object current,
            Type type,
            PendingEntry pending,
            bool isValueField
        )
        {
            if (
                pending == null
                || !TypeSupportsComplexEditing(type)
                || typeof(Object).IsAssignableFrom(type)
                || type == typeof(string)
            )
            {
                return false;
            }

            PendingWrapperContext context = EnsurePendingWrapper(pending, type, isValueField);
            if (context.Property == null)
            {
                return false;
            }

            object targetValue = current ?? GetDefaultValue(type);
            object wrapperValue = context.Wrapper.GetValue();
            if (!ValuesEqual(wrapperValue, targetValue))
            {
                object clone = CloneComplexValue(targetValue, type);
                context.Wrapper.SetValue(clone);
                SyncPendingWrapperManagedReference(context, clone);
            }

            if (type.IsClass && context.Wrapper.GetValue() == null)
            {
                object defaultValue = GetDefaultValue(type);
                context.Wrapper.SetValue(defaultValue);
                SyncPendingWrapperManagedReference(context, defaultValue);
            }

            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = Mathf.Max(0, previousIndent - 1);
            try
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rect, context.Property, content, includeChildren: true);
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyModifiedPropertiesWithUndoFallback(context.Serialized);
                    context.Serialized.Update();
                    object updated = context.Wrapper.GetValue();
                    current = CloneComplexValue(updated, type);
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndent;
            }

            return true;
        }

        private static bool TryDrawPaletteValueField(
            Rect rect,
            GUIContent label,
            ref object current,
            Type type
        )
        {
            PaletteValueRenderer renderer = ResolvePaletteValueRenderer(type);
            if (renderer == null)
            {
                return false;
            }

            object working = EnsurePaletteValueInstance(current, type);
            renderer.Draw(rect, label, ref working);
            current = working;
            return true;
        }

        private static PaletteValueRenderer ResolvePaletteValueRenderer(Type type)
        {
            if (type == null || PaletteValueRenderers == null)
            {
                return null;
            }

            return PaletteValueRenderers.GetValueOrDefault(type);
        }

        private static void RegisterPaletteManualEditForKey(
            SerializedProperty dictionaryProperty,
            object key,
            Type keyType
        )
        {
            if (
                dictionaryProperty == null
                || keyType != typeof(string)
                || key is not string colorKey
                || string.IsNullOrWhiteSpace(colorKey)
                || !PaletteSerializationDiagnostics.IsPaletteProperty(
                    dictionaryProperty.propertyPath
                )
            )
            {
                return;
            }

            SerializedObject serializedObject = dictionaryProperty.serializedObject;
            Object[] targets = serializedObject.targetObjects;
            if (targets == null || targets.Length == 0)
            {
                return;
            }

            for (int index = 0; index < targets.Length; index++)
            {
                if (targets[index] is UnityHelpersSettings)
                {
                    UnityHelpersSettings.RegisterPaletteManualEdit(
                        dictionaryProperty.propertyPath,
                        colorKey
                    );
                }
            }
        }

        private static bool IsTypeSupported(Type type)
        {
            if (type == null)
            {
                return false;
            }

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
                || typeof(Object).IsAssignableFrom(type)
                || TypeSupportsComplexEditing(type);
        }

        private static bool TypeSupportsComplexEditing(Type type)
        {
            if (type == null)
            {
                return false;
            }

            if (typeof(Object).IsAssignableFrom(type))
            {
                return true;
            }

            if (IsSimplePendingFieldType(type))
            {
                return false;
            }

            if (type.IsArray)
            {
                return TypeSupportsComplexEditing(type.GetElementType());
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return TypeSupportsComplexEditing(type.GetGenericArguments()[0]);
            }

            return type.IsSerializable;
        }

        private static bool IsSimplePendingFieldType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            if (type.IsPrimitive || type.IsEnum)
            {
                return true;
            }

            return type == typeof(string)
                || type == typeof(decimal)
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
                || type == typeof(AnimationCurve);
        }

        private static bool ValueTypeSupportsFoldout(Type type)
        {
            if (type == null)
            {
                return false;
            }

            if (typeof(Object).IsAssignableFrom(type))
            {
                return false;
            }

            if (PaletteValueRenderers != null && PaletteValueRenderers.ContainsKey(type))
            {
                return false;
            }

            if (IsSimplePendingFieldType(type))
            {
                return false;
            }

            return TypeSupportsComplexEditing(type);
        }

        private static bool PendingValueSupportsFoldout(PendingEntry pending, Type valueType)
        {
            if (pending == null || !ValueTypeSupportsFoldout(valueType))
            {
                return false;
            }

            PendingWrapperContext context = EnsurePendingWrapper(
                pending,
                valueType,
                isValueField: true
            );
            if (context.Property == null)
            {
                return false;
            }

            context.Property.isExpanded = true;
            return SerializedPropertySupportsFoldout(context.Property);
        }

        private static bool SerializedPropertySupportsFoldout(SerializedProperty property)
        {
            return property != null && property.hasVisibleChildren;
        }

        private static PendingWrapperContext EnsurePendingWrapper(
            PendingEntry pending,
            Type type,
            bool isValueField
        )
        {
            if (pending == null || type == null)
            {
                return PendingWrapperContext.Empty;
            }

            PendingValueWrapper wrapper = isValueField ? pending.valueWrapper : pending.keyWrapper;
            SerializedObject serialized = isValueField
                ? pending.valueWrapperSerialized
                : pending.keyWrapperSerialized;
            SerializedProperty property = isValueField
                ? pending.valueWrapperProperty
                : pending.keyWrapperProperty;

            if (wrapper == null)
            {
                wrapper = ScriptableObject.CreateInstance<PendingValueWrapper>();
                wrapper.hideFlags = HideFlags.HideAndDontSave;
                serialized = null;
                property = null;
            }

            if (serialized == null)
            {
                serialized = new SerializedObject(wrapper);
                property = wrapper.FindValueProperty(serialized);
            }

            if (property == null)
            {
                ReleasePendingWrapper(pending, isValueField);
                return PendingWrapperContext.Empty;
            }

            if (isValueField)
            {
                pending.valueWrapper = wrapper;
                pending.valueWrapperSerialized = serialized;
                pending.valueWrapperProperty = property;
            }
            else
            {
                pending.keyWrapper = wrapper;
                pending.keyWrapperSerialized = serialized;
                pending.keyWrapperProperty = property;
            }

            serialized.Update();

            return new PendingWrapperContext(wrapper, serialized, property);
        }

        private static void SyncPendingWrapperManagedReference(
            PendingWrapperContext context,
            object newValue
        )
        {
            if (context.Property == null || context.Serialized == null)
            {
                return;
            }

            context.Serialized.Update();
            context.Property.managedReferenceValue = newValue;
            context.Serialized.ApplyModifiedPropertiesWithoutUndo();
            context.Serialized.Update();
        }

        private static void ReleasePendingWrapper(PendingEntry pending, bool isValueField)
        {
            if (pending == null)
            {
                return;
            }

            if (isValueField)
            {
                if (pending.valueWrapper != null)
                {
                    Object.DestroyImmediate(pending.valueWrapper);
                }

                pending.valueWrapper = null;
                pending.valueWrapperSerialized = null;
                pending.valueWrapperProperty = null;
            }
            else
            {
                if (pending.keyWrapper != null)
                {
                    Object.DestroyImmediate(pending.keyWrapper);
                }

                pending.keyWrapper = null;
                pending.keyWrapperSerialized = null;
                pending.keyWrapperProperty = null;
            }
        }

        private static bool TryInvokeParameterlessConstructor(Type type, out object instance)
        {
            instance = null;
            if (type == null)
            {
                return false;
            }

            if (ParameterlessConstructorCache.TryGetValue(type, out Func<object> cached))
            {
                instance = cached();
                return instance != null;
            }

            if (UnsupportedParameterlessTypes.ContainsKey(type))
            {
                return false;
            }

            Func<object> factory = TryResolveConstructorFactory(type);
            if (factory != null)
            {
                ParameterlessConstructorCache[type] = factory;
                instance = factory();
                if (instance != null)
                {
                    return true;
                }
            }

            if (TryInstantiateWithoutConstructor(type, out instance))
            {
                return true;
            }

            UnsupportedParameterlessTypes[type] = 0;
            return false;
        }

        private static Func<object> TryResolveConstructorFactory(Type type)
        {
            try
            {
                return ReflectionHelpers.GetParameterlessConstructor(type);
            }
            catch (ArgumentException)
            {
                ConstructorInfo ctor = type.GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    Type.EmptyTypes,
                    modifiers: null
                );
                if (ctor == null)
                {
                    return null;
                }

                return () =>
                {
                    try
                    {
                        return ctor.Invoke(null);
                    }
                    catch
                    {
                        return null;
                    }
                };
            }
            catch
            {
                return null;
            }
        }

        private static bool TryInstantiateWithoutConstructor(Type type, out object instance)
        {
            try
            {
                instance = Activator.CreateInstance(type, nonPublic: true);
                if (instance != null)
                {
                    ParameterlessConstructorCache[type] = () =>
                    {
                        try
                        {
                            return Activator.CreateInstance(type, nonPublic: true);
                        }
                        catch
                        {
                            return null;
                        }
                    };
                    return true;
                }
            }
            catch
            {
                // Continue to FormatterServices fallback
            }

            try
            {
                instance = FormatterServices.GetUninitializedObject(type);
                ParameterlessConstructorCache[type] = () =>
                {
                    try
                    {
                        return FormatterServices.GetUninitializedObject(type);
                    }
                    catch
                    {
                        return null;
                    }
                };
                return instance != null;
            }
            catch
            {
                instance = null;
                return false;
            }
        }

        private static bool TryCreateDefaultInstance(Type type, out object instance)
        {
            instance = null;
            if (type == null)
            {
                return false;
            }

            if (type.IsAbstract || type.IsInterface)
            {
                return false;
            }

            if (typeof(Object).IsAssignableFrom(type))
            {
                return false;
            }

            return TryInvokeParameterlessConstructor(type, out instance);
        }

        private static object CloneComplexValue(object source, Type type)
        {
            if (source == null)
            {
                if (type == null)
                {
                    return null;
                }

                if (type.IsValueType)
                {
                    return TryInvokeParameterlessConstructor(type, out object value) ? value : null;
                }

                return null;
            }

            if (type == null || type.IsValueType || typeof(Object).IsAssignableFrom(type))
            {
                return source;
            }

            if (!TryCreateDefaultInstance(type, out object clone))
            {
                return source;
            }

            try
            {
                string json = JsonUtility.ToJson(source);
                JsonUtility.FromJsonOverwrite(json, clone);
                return clone;
            }
            catch
            {
                return source;
            }
        }

        private static bool TryAssignComplexValue(
            SerializedProperty property,
            object value,
            Type valueType
        )
        {
            if (property == null)
            {
                return false;
            }

            Object[] targets = property.serializedObject.targetObjects;
            if (targets == null || targets.Length == 0)
            {
                return false;
            }

            string finalSegment = GetFinalPathSegment(property.propertyPath);
            bool applied = false;

            foreach (Object target in targets)
            {
                object parent = GetParentObject(target, property.propertyPath);
                if (parent == null)
                {
                    continue;
                }

                object clone = CloneComplexValue(value, valueType);
                if (SetPathComponentValue(parent, finalSegment, clone))
                {
                    applied = true;
                }
            }

            if (applied)
            {
                property.serializedObject.Update();
            }

            return applied;
        }

        private static object GetParentObject(Object target, string propertyPath)
        {
            if (target == null || string.IsNullOrEmpty(propertyPath))
            {
                return null;
            }

            string[] elements = propertyPath
                .Replace(".Array.data[", "[")
                .Split(PropertyPathSeparators, StringSplitOptions.RemoveEmptyEntries);

            object current = target;
            for (int index = 0; index < elements.Length - 1; index++)
            {
                current = GetPathComponentValue(current, elements[index]);
                if (current == null)
                {
                    return null;
                }
            }

            return current;
        }

        private static string GetFinalPathSegment(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
            {
                return string.Empty;
            }

            string[] elements = propertyPath
                .Replace(".Array.data[", "[")
                .Split(PropertyPathSeparators, StringSplitOptions.RemoveEmptyEntries);
            return elements.Length > 0 ? elements[^1] : string.Empty;
        }

        private static object GetPathComponentValue(object source, string path)
        {
            if (source == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            int bracketIndex = path.IndexOf('[');
            if (bracketIndex < 0)
            {
                return GetMemberValue(source, path);
            }

            string elementName = path.Substring(0, bracketIndex);
            int index = ParseElementIndex(path, bracketIndex);
            if (index < 0)
            {
                return null;
            }

            object collection = GetMemberValue(source, elementName);
            if (collection is IList list)
            {
                return index < list.Count ? list[index] : null;
            }

            return null;
        }

        private static bool SetPathComponentValue(object source, string path, object value)
        {
            if (source == null || string.IsNullOrEmpty(path))
            {
                return false;
            }

            int bracketIndex = path.IndexOf('[');
            if (bracketIndex < 0)
            {
                return SetMemberValue(source, path, value);
            }

            string elementName = path.Substring(0, bracketIndex);
            int index = ParseElementIndex(path, bracketIndex);
            if (index < 0)
            {
                return false;
            }

            object collection = GetMemberValue(source, elementName);
            if (collection is IList list && index < list.Count)
            {
                list[index] = value;
                return true;
            }

            return false;
        }

        private static int ParseElementIndex(string path, int bracketIndex)
        {
            int endIndex = path.IndexOf(']', bracketIndex);
            if (endIndex < 0)
            {
                return -1;
            }

            string indexString = path.Substring(bracketIndex + 1, endIndex - bracketIndex - 1);
            return int.TryParse(indexString, out int result) ? result : -1;
        }

        private static object GetMemberValue(object source, string memberName)
        {
            if (source == null || string.IsNullOrEmpty(memberName))
            {
                return null;
            }

            Type type = source.GetType();

            FieldInfo field = type.GetField(memberName, ReflectionBindingFlags);
            if (field != null)
            {
                return field.GetValue(source);
            }

            PropertyInfo propertyInfo = type.GetProperty(memberName, ReflectionBindingFlags);
            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(source);
            }

            return null;
        }

        private static bool SetMemberValue(object source, string memberName, object value)
        {
            if (source == null || string.IsNullOrEmpty(memberName))
            {
                return false;
            }

            Type type = source.GetType();

            FieldInfo field = type.GetField(memberName, ReflectionBindingFlags);
            if (field != null)
            {
                field.SetValue(source, value);
                return true;
            }

            PropertyInfo propertyInfo = type.GetProperty(memberName, ReflectionBindingFlags);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(source, value);
                return true;
            }

            return false;
        }

        private static void CopyComplexValueToPropertyFields(
            SerializedProperty destination,
            object value,
            Type valueType
        )
        {
            if (destination == null || valueType == null)
            {
                return;
            }

            foreach (FieldInfo field in GetSerializableFields(valueType))
            {
                SerializedProperty child = destination.FindPropertyRelative(field.Name);
                if (child == null)
                {
                    continue;
                }

                object fieldValue = field.GetValue(value);
                SetPropertyValue(child, fieldValue, field.FieldType);
            }
        }

        internal sealed class PendingValueWrapper : ScriptableObject
        {
            private const string PropertyName = nameof(boxedValue);

            [SerializeReference]
            private object boxedValue;

            public object GetValue()
            {
                return boxedValue;
            }

            public void SetValue(object incoming)
            {
                boxedValue = incoming;
            }

            public SerializedProperty FindValueProperty(SerializedObject serializedObject)
            {
                return serializedObject.FindProperty(PropertyName);
            }
        }

        private readonly struct PendingWrapperContext
        {
            public static readonly PendingWrapperContext Empty = new(null, null, null);

            public PendingWrapperContext(
                PendingValueWrapper wrapper,
                SerializedObject serialized,
                SerializedProperty property
            )
            {
                Wrapper = wrapper;
                Serialized = serialized;
                Property = property;
            }

            public PendingValueWrapper Wrapper { get; }

            public SerializedObject Serialized { get; }

            public SerializedProperty Property { get; }
        }

        private static Dictionary<Type, PaletteValueRenderer> BuildPaletteValueRenderers()
        {
            Dictionary<Type, PaletteValueRenderer> renderers = new();
            Type settingsType = typeof(UnityHelpersSettings);
            TryRegisterDualColorPaletteRenderer(
                renderers,
                settingsType,
                "WButtonCustomColor",
                "Button",
                "buttonColor",
                "Text",
                "textColor"
            );
            TryRegisterDualColorPaletteRenderer(
                renderers,
                settingsType,
                "WGroupCustomColor",
                "Background",
                "backgroundColor",
                "Text",
                "textColor"
            );
            TryRegisterWEnumPaletteRenderer(renderers, settingsType);
            return renderers;
        }

        private static void TryRegisterDualColorPaletteRenderer(
            IDictionary<Type, PaletteValueRenderer> renderers,
            Type containerType,
            string nestedTypeName,
            string firstLabel,
            string firstFieldName,
            string secondLabel,
            string secondFieldName
        )
        {
            Type nestedType = containerType.GetNestedType(
                nestedTypeName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (nestedType == null)
            {
                return;
            }

            FieldInfo firstField = nestedType.GetField(
                firstFieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            FieldInfo secondField = nestedType.GetField(
                secondFieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (firstField == null || secondField == null)
            {
                return;
            }

            PaletteValueRenderer renderer = PaletteValueRenderer.CreateDualColorRenderer(
                nestedType,
                firstField,
                EditorGUIUtility.TrTextContent(firstLabel),
                secondField,
                EditorGUIUtility.TrTextContent(secondLabel)
            );

            if (renderer != null)
            {
                renderers[nestedType] = renderer;
            }
        }

        private static void TryRegisterWEnumPaletteRenderer(
            IDictionary<Type, PaletteValueRenderer> renderers,
            Type containerType
        )
        {
            Type nestedType = containerType.GetNestedType(
                "WEnumToggleButtonsCustomColor",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (nestedType == null)
            {
                return;
            }

            FieldInfo selectedBackground = nestedType.GetField(
                "selectedBackgroundColor",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            FieldInfo selectedText = nestedType.GetField(
                "selectedTextColor",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            FieldInfo inactiveBackground = nestedType.GetField(
                "inactiveBackgroundColor",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            FieldInfo inactiveText = nestedType.GetField(
                "inactiveTextColor",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (
                selectedBackground == null
                || selectedText == null
                || inactiveBackground == null
                || inactiveText == null
            )
            {
                return;
            }

            PaletteValueRenderer renderer = PaletteValueRenderer.CreateWEnumRenderer(
                nestedType,
                selectedBackground,
                EditorGUIUtility.TrTextContent("Selected BG"),
                selectedText,
                EditorGUIUtility.TrTextContent("Selected Text"),
                inactiveBackground,
                EditorGUIUtility.TrTextContent("Inactive BG"),
                inactiveText,
                EditorGUIUtility.TrTextContent("Inactive Text")
            );
            if (renderer != null)
            {
                renderers[nestedType] = renderer;
            }
        }

        private static object EnsurePaletteValueInstance(object current, Type type)
        {
            object instance = current;
            if (instance == null || !type.IsInstanceOfType(instance))
            {
                instance = GetDefaultValue(type);
            }

            return CloneComplexValue(instance, type) ?? instance;
        }

        private delegate void PaletteValueDrawHandler(
            Rect rect,
            GUIContent label,
            ref object value
        );

        private sealed class PaletteValueRenderer
        {
            private readonly Type _targetType;
            private readonly Func<float> _heightProvider;
            private readonly PaletteValueDrawHandler _drawHandler;

            private PaletteValueRenderer(
                Type targetType,
                Func<float> heightProvider,
                PaletteValueDrawHandler drawHandler
            )
            {
                _targetType = targetType;
                _heightProvider = heightProvider;
                _drawHandler = drawHandler;
            }

            public float Height => _heightProvider();

            public void Draw(Rect rect, GUIContent label, ref object value)
            {
                _drawHandler(rect, label, ref value);
            }

            public static PaletteValueRenderer CreateDualColorRenderer(
                Type targetType,
                FieldInfo firstField,
                GUIContent firstLabel,
                FieldInfo secondField,
                GUIContent secondLabel
            )
            {
                if (
                    targetType == null
                    || firstField == null
                    || secondField == null
                    || firstLabel == null
                    || secondLabel == null
                )
                {
                    return null;
                }

                return new PaletteValueRenderer(
                    targetType,
                    static () => EditorGUIUtility.singleLineHeight,
                    delegate(Rect rect, GUIContent label, ref object instance)
                    {
                        instance = EnsurePaletteValueInstance(instance, targetType);
                        Rect contentRect = EditorGUI.PrefixLabel(rect, label);
                        float spacing = EditorGUIUtility.standardVerticalSpacing;
                        float halfWidth = Mathf.Max(0f, (contentRect.width - spacing) * 0.5f);
                        Rect firstRect = new(
                            contentRect.x,
                            contentRect.y,
                            halfWidth,
                            EditorGUIUtility.singleLineHeight
                        );
                        Rect secondRect = new(
                            firstRect.xMax + spacing,
                            contentRect.y,
                            halfWidth,
                            EditorGUIUtility.singleLineHeight
                        );

                        Color first = (Color)(firstField.GetValue(instance) ?? Color.clear);
                        Color updatedFirst = EditorGUI.ColorField(firstRect, firstLabel, first);
                        if (updatedFirst != first)
                        {
                            firstField.SetValue(instance, updatedFirst);
                        }

                        Color second = (Color)(secondField.GetValue(instance) ?? Color.clear);
                        Color updatedSecond = EditorGUI.ColorField(secondRect, secondLabel, second);
                        if (updatedSecond != second)
                        {
                            secondField.SetValue(instance, updatedSecond);
                        }
                    }
                );
            }

            public static PaletteValueRenderer CreateWEnumRenderer(
                Type targetType,
                FieldInfo selectedBgField,
                GUIContent selectedBgLabel,
                FieldInfo selectedTextField,
                GUIContent selectedTextLabel,
                FieldInfo inactiveBgField,
                GUIContent inactiveBgLabel,
                FieldInfo inactiveTextField,
                GUIContent inactiveTextLabel
            )
            {
                if (
                    targetType == null
                    || selectedBgField == null
                    || selectedTextField == null
                    || inactiveBgField == null
                    || inactiveTextField == null
                )
                {
                    return null;
                }

                return new PaletteValueRenderer(
                    targetType,
                    static () =>
                        (EditorGUIUtility.singleLineHeight * 2f)
                        + EditorGUIUtility.standardVerticalSpacing,
                    delegate(Rect rect, GUIContent label, ref object instance)
                    {
                        instance = EnsurePaletteValueInstance(instance, targetType);
                        Rect contentRect = EditorGUI.PrefixLabel(rect, label);
                        float rowHeight = EditorGUIUtility.singleLineHeight;
                        float spacing = EditorGUIUtility.standardVerticalSpacing;
                        float halfWidth = Mathf.Max(0f, (contentRect.width - spacing) * 0.5f);

                        Rect selectedBgRect = new(
                            contentRect.x,
                            contentRect.y,
                            halfWidth,
                            rowHeight
                        );
                        Rect selectedTextRect = new(
                            selectedBgRect.xMax + spacing,
                            contentRect.y,
                            halfWidth,
                            rowHeight
                        );
                        Rect inactiveBgRect = new(
                            contentRect.x,
                            contentRect.y + rowHeight + spacing,
                            halfWidth,
                            rowHeight
                        );
                        Rect inactiveTextRect = new(
                            inactiveBgRect.xMax + spacing,
                            inactiveBgRect.y,
                            halfWidth,
                            rowHeight
                        );

                        Color selectedBg = (Color)(
                            selectedBgField.GetValue(instance) ?? Color.clear
                        );
                        Color updatedSelectedBg = EditorGUI.ColorField(
                            selectedBgRect,
                            selectedBgLabel,
                            selectedBg
                        );
                        if (updatedSelectedBg != selectedBg)
                        {
                            selectedBgField.SetValue(instance, updatedSelectedBg);
                        }

                        Color selectedText = (Color)(
                            selectedTextField.GetValue(instance) ?? Color.clear
                        );
                        Color updatedSelectedText = EditorGUI.ColorField(
                            selectedTextRect,
                            selectedTextLabel,
                            selectedText
                        );
                        if (updatedSelectedText != selectedText)
                        {
                            selectedTextField.SetValue(instance, updatedSelectedText);
                        }

                        Color inactiveBg = (Color)(
                            inactiveBgField.GetValue(instance) ?? Color.clear
                        );
                        Color updatedInactiveBg = EditorGUI.ColorField(
                            inactiveBgRect,
                            inactiveBgLabel,
                            inactiveBg
                        );
                        if (updatedInactiveBg != inactiveBg)
                        {
                            inactiveBgField.SetValue(instance, updatedInactiveBg);
                        }

                        Color inactiveText = (Color)(
                            inactiveTextField.GetValue(instance) ?? Color.clear
                        );
                        Color updatedInactiveText = EditorGUI.ColorField(
                            inactiveTextRect,
                            inactiveTextLabel,
                            inactiveText
                        );
                        if (updatedInactiveText != inactiveText)
                        {
                            inactiveTextField.SetValue(instance, updatedInactiveText);
                        }
                    }
                );
            }
        }

        internal static void SetPropertyValue(SerializedProperty property, object value, Type type)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.longValue = Convert.ToInt64(value);
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = value is true;
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
                    property.objectReferenceValue = value as Object;
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
                case SerializedPropertyType.Generic:
                    if (!TryAssignComplexValue(property, value, type))
                    {
                        CopyComplexValueToPropertyFields(property, value, type);
                    }
                    break;
                default:
                    throw new NotSupportedException(
                        $"Unsupported property type: {property.propertyType}"
                    );
            }
        }

        internal static object GetPropertyValue(SerializedProperty property, Type type)
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
                case SerializedPropertyType.Generic:
                    return CopyComplexValueFromProperty(property, type);
                default:
                    return null;
            }
        }

        private static object CopyComplexValueFromProperty(
            SerializedProperty source,
            Type valueType
        )
        {
            if (valueType == null)
            {
                return null;
            }

            if (source.propertyType == SerializedPropertyType.ManagedReference)
            {
                return CloneComplexValue(source.managedReferenceValue, valueType);
            }

            if (source.propertyType != SerializedPropertyType.Generic)
            {
                return null;
            }

            if (!TryCreateDefaultInstance(valueType, out object instance))
            {
                return null;
            }

            foreach (FieldInfo field in GetSerializableFields(valueType))
            {
                SerializedProperty child = source.FindPropertyRelative(field.Name);
                if (child == null)
                {
                    continue;
                }

                object fieldValue = GetPropertyValue(child, field.FieldType);
                field.SetValue(instance, fieldValue);
            }

            return instance;
        }

        private static IEnumerable<FieldInfo> GetSerializableFields(Type type)
        {
            if (type == null)
            {
                yield break;
            }

            const BindingFlags Flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Type current = type;
            HashSet<string> yielded = new();

            while (current != null && current != typeof(object))
            {
                FieldInfo[] fields = current.GetFields(Flags);
                foreach (FieldInfo field in fields)
                {
                    if (field.IsStatic || field.IsInitOnly || field.IsLiteral)
                    {
                        continue;
                    }

                    if (field.IsNotSerialized)
                    {
                        continue;
                    }

                    if (
                        !field.IsPublic
                        && !ReflectionHelpers.HasAttributeSafe<SerializeField>(field, inherit: true)
                    )
                    {
                        continue;
                    }

                    if (!yielded.Add($"{field.DeclaringType?.FullName}.{field.Name}"))
                    {
                        continue;
                    }

                    yield return field;
                }

                current = current.BaseType;
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
            if (left is Object leftObj && right is Object rightObj)
            {
                if (leftObj == null && rightObj == null)
                {
                    return true;
                }

                if (leftObj == null || rightObj == null)
                {
                    return false;
                }
            }

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

        internal static object GetDefaultValue(Type type)
        {
            if (type == null)
            {
                return null;
            }

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
                return TryInvokeParameterlessConstructor(type, out object value) ? value : null;
            }

            if (TryCreateDefaultInstance(type, out object instance))
            {
                return instance;
            }

            return null;
        }

        internal struct CommitResult
        {
            public bool added;
            public int index;
        }

        private enum PaginationControlLayout
        {
            None,
            PrevNext,
            Full,
        }

        internal sealed class PendingEntry
        {
            public object key;
            public object value;
            public bool isExpanded;
            public AnimBool foldoutAnim;
            public bool isSorted;
            public PendingValueWrapper keyWrapper;
            public SerializedObject keyWrapperSerialized;
            public SerializedProperty keyWrapperProperty;
            public PendingValueWrapper valueWrapper;
            public SerializedObject valueWrapperSerialized;
            public SerializedProperty valueWrapperProperty;
        }

        public sealed class NullKeyInfo
        {
            public string tooltip = string.Empty;
        }

        internal sealed class NullKeyState
        {
            private readonly HashSet<int> _nullIndices = new();
            private readonly Dictionary<int, NullKeyInfo> _nullLookup = new();
            private readonly List<int> _scratch = new();

            public bool HasNullKeys { get; private set; }
            public string WarningMessage { get; private set; } = string.Empty;

            public bool Refresh(SerializedProperty keysProperty, Type keyType)
            {
                _scratch.Clear();

                if (keysProperty == null || keyType == null || !TypeSupportsNullReferences(keyType))
                {
                    bool changed = HasNullKeys || _nullLookup.Count > 0;
                    if (changed)
                    {
                        _nullIndices.Clear();
                        _nullLookup.Clear();
                    }

                    HasNullKeys = false;
                    WarningMessage = string.Empty;
                    return changed;
                }

                int count = keysProperty.arraySize;
                for (int index = 0; index < count; index++)
                {
                    SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(index);
                    object keyValue =
                        keyProperty != null ? GetPropertyValue(keyProperty, keyType) : null;
                    if (ReferenceEquals(keyValue, null))
                    {
                        _scratch.Add(index);
                    }
                }

                bool changedIndices = UpdateIndices();
                HasNullKeys = _nullIndices.Count > 0;
                WarningMessage = HasNullKeys ? BuildNullKeySummary(_nullIndices) : string.Empty;
                return changedIndices;
            }

            private bool UpdateIndices()
            {
                bool changed = _scratch.Count != _nullIndices.Count;

                if (!changed)
                {
                    foreach (int index in _scratch)
                    {
                        if (!_nullIndices.Contains(index))
                        {
                            changed = true;
                            break;
                        }
                    }
                }

                if (!changed)
                {
                    return false;
                }

                _nullIndices.Clear();
                _nullLookup.Clear();

                foreach (int index in _scratch)
                {
                    _nullIndices.Add(index);
                    _nullLookup[index] = new NullKeyInfo
                    {
                        tooltip =
                            $"Null key detected at index {index}. Entry will be ignored at runtime.",
                    };
                }

                return true;
            }

            public bool TryGetInfo(int arrayIndex, out NullKeyInfo info)
            {
                return _nullLookup.TryGetValue(arrayIndex, out info);
            }
        }

        private sealed class DuplicateKeyInfo
        {
            public string tooltip = string.Empty;
            public bool isPrimary;
        }

        private sealed class DuplicateKeyState
        {
            private readonly Dictionary<int, DuplicateKeyInfo> _duplicateLookup = new();
            private readonly Dictionary<int, double> _duplicateAnimationStartTimes = new();
            private readonly List<int> _animationKeysScratch = new();
            private readonly List<int> _summaryIndicesScratch = new();
            private StringBuilder _summaryBuilder;
            private bool _lastHadDuplicates;

            public bool HasDuplicates { get; private set; }
            public string SummaryTooltip { get; private set; } = string.Empty;

            public bool IsEmpty => _duplicateLookup.Count == 0;

            public bool Refresh(SerializedProperty keysProperty, Type keyType)
            {
                _duplicateLookup.Clear();
                HasDuplicates = false;
                SummaryTooltip = string.Empty;

                if (keysProperty == null || keyType == null)
                {
                    bool previouslyDuplicated = _lastHadDuplicates;
                    _lastHadDuplicates = false;
                    if (_duplicateAnimationStartTimes.Count > 0)
                    {
                        _duplicateAnimationStartTimes.Clear();
                    }
                    return previouslyDuplicated;
                }

                int count = keysProperty.arraySize;
                Dictionary<object, List<int>> grouping = new(new KeyEqualityComparer());

                using PooledResource<StringBuilder> summaryBuilderLease = Buffers.GetStringBuilder(
                    Math.Max(count * 8, 64),
                    out StringBuilder summaryBuilder
                );
                _summaryBuilder = summaryBuilder;
                _summaryBuilder.Clear();

                for (int index = 0; index < count; index++)
                {
                    SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(index);
                    object keyValue =
                        keyProperty != null ? GetPropertyValue(keyProperty, keyType) : null;
                    object lookupKey = keyValue ?? NullKeySentinel;

                    List<int> indices = grouping.GetOrAdd(lookupKey);
                    indices.Add(index);
                }

                int duplicateGroupCount = 0;
                int displayedSummaryGroups = 0;

                foreach (KeyValuePair<object, List<int>> entry in grouping)
                {
                    List<int> indices = entry.Value;
                    if (indices.Count <= 1)
                    {
                        continue;
                    }

                    duplicateGroupCount++;
                    HasDuplicates = true;
                    string formattedKey = FormatDuplicateKeyDisplay(entry.Key);
                    string tooltip = BuildDuplicateTooltip(formattedKey, indices);

                    for (int occurrence = 0; occurrence < indices.Count; occurrence++)
                    {
                        int arrayIndex = indices[occurrence];
                        DuplicateKeyInfo info = new()
                        {
                            tooltip = tooltip,
                            isPrimary = occurrence == 0,
                        };
                        _duplicateLookup[arrayIndex] = info;
                    }

                    if (displayedSummaryGroups < DuplicateSummaryDisplayLimit)
                    {
                        if (_summaryBuilder.Length > 0)
                        {
                            _summaryBuilder.AppendLine();
                        }
                        AppendDuplicateSummaryLine(formattedKey, indices);
                        displayedSummaryGroups++;
                    }
                }

                SummaryTooltip = BuildDuplicateSummaryText(
                    duplicateGroupCount,
                    displayedSummaryGroups
                );

                _summaryBuilder = null;

                UpdateAnimationTracking();

                bool changed = HasDuplicates != _lastHadDuplicates;
                _lastHadDuplicates = HasDuplicates;
                return changed;
            }

            public float GetAnimationOffset(int arrayIndex, double currentTime, int cycleLimit)
            {
                if (!_duplicateAnimationStartTimes.TryGetValue(arrayIndex, out double startTime))
                {
                    startTime = currentTime;
                    _duplicateAnimationStartTimes[arrayIndex] = startTime;
                }

                return EvaluateDuplicateTweenOffset(arrayIndex, startTime, currentTime, cycleLimit);
            }

            public bool TryGetInfo(int arrayIndex, out DuplicateKeyInfo info)
            {
                return _duplicateLookup.TryGetValue(arrayIndex, out info);
            }

            private void AppendDuplicateSummaryLine(string formattedKey, List<int> indices)
            {
                if (string.IsNullOrEmpty(formattedKey) || indices == null || indices.Count <= 1)
                {
                    return;
                }

                if (_summaryBuilder == null)
                {
                    return;
                }

                _summaryIndicesScratch.Clear();
                _summaryIndicesScratch.AddRange(indices);
                _summaryIndicesScratch.Sort();

                _summaryBuilder.Append("Duplicate key ");
                _summaryBuilder.Append(formattedKey);
                _summaryBuilder.Append(" at entries ");

                for (int index = 0; index < _summaryIndicesScratch.Count; index++)
                {
                    if (index > 0)
                    {
                        _summaryBuilder.Append(", ");
                    }

                    _summaryBuilder.Append(_summaryIndicesScratch[index] + 1);
                }
            }

            private string BuildDuplicateSummaryText(int duplicateGroupCount, int displayedGroups)
            {
                if (_summaryBuilder == null)
                {
                    return string.Empty;
                }

                if (duplicateGroupCount <= 0)
                {
                    return string.Empty;
                }

                if (displayedGroups == 0)
                {
                    return duplicateGroupCount == 1
                        ? "Duplicate key detected. Resolve conflicts to prevent silent overwrites. The last entry wins at runtime."
                        : $"{duplicateGroupCount} duplicate keys detected. Resolve conflicts to prevent silent overwrites. The last entry wins at runtime.";
                }

                if (duplicateGroupCount > displayedGroups)
                {
                    if (_summaryBuilder.Length > 0)
                    {
                        _summaryBuilder.AppendLine();
                    }

                    int remainingGroups = duplicateGroupCount - displayedGroups;
                    _summaryBuilder.Append(
                        remainingGroups == 1
                            ? "1 additional duplicate group omitted for brevity."
                            : $"{remainingGroups} additional duplicate groups omitted for brevity."
                    );
                }

                if (_summaryBuilder.Length > 0)
                {
                    _summaryBuilder.AppendLine();
                }

                _summaryBuilder.Append(
                    "Resolve conflicts to prevent silent overwrites. The last entry wins at runtime."
                );

                return _summaryBuilder.ToString();
            }

            private void UpdateAnimationTracking()
            {
                if (!HasDuplicates || _duplicateLookup.Count == 0)
                {
                    if (_duplicateAnimationStartTimes.Count > 0)
                    {
                        _duplicateAnimationStartTimes.Clear();
                    }

                    return;
                }

                double now = EditorApplication.timeSinceStartup;

                foreach (int index in _duplicateLookup.Keys)
                {
                    _duplicateAnimationStartTimes.TryAdd(index, now);
                }

                if (_duplicateAnimationStartTimes.Count == 0)
                {
                    return;
                }

                _animationKeysScratch.Clear();

                foreach (KeyValuePair<int, double> entry in _duplicateAnimationStartTimes)
                {
                    if (!_duplicateLookup.ContainsKey(entry.Key))
                    {
                        _animationKeysScratch.Add(entry.Key);
                    }
                }

                foreach (int animationStartTime in _animationKeysScratch)
                {
                    _duplicateAnimationStartTimes.Remove(animationStartTime);
                }
            }
        }

        internal sealed class PaginationState
        {
            public int pageIndex;
            public int pageSize;
            public int selectedIndex = -1;
        }

        internal sealed class ListPageCache
        {
            public readonly List<PageEntry> entries = new();
            public int pageIndex = -1;
            public int pageSize = -1;
            public int itemCount = -1;
            public bool dirty = true;
        }

        internal sealed class PageEntry
        {
            public int arrayIndex;
        }

        private sealed class KeyIndexCache
        {
            public readonly Dictionary<object, int> indices = new(new KeyEqualityComparer());
            public int arraySize;
        }

        private sealed class KeyEqualityComparer : IEqualityComparer<object>
        {
            bool IEqualityComparer<object>.Equals(object x, object y)
            {
                return EqualsCore(x, y);
            }

            int IEqualityComparer<object>.GetHashCode(object obj)
            {
                return GetHashCodeCore(obj);
            }

            private static bool EqualsCore(object x, object y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (ReferenceEquals(x, NullKeySentinel))
                {
                    x = null;
                }

                if (ReferenceEquals(y, NullKeySentinel))
                {
                    y = null;
                }

                if (x is Object xObj)
                {
                    if (y is Object yObj)
                    {
                        return xObj == yObj;
                    }

                    return false;
                }

                if (y is Object)
                {
                    return false;
                }

                if (x == null || y == null)
                {
                    return x == y;
                }

                return x.Equals(y);
            }

            private static int GetHashCodeCore(object obj)
            {
                if (ReferenceEquals(obj, NullKeySentinel) || obj == null)
                {
                    return 0;
                }

                if (obj is Object objObj && objObj == null)
                {
                    return 0;
                }

                return obj.GetHashCode();
            }
        }

        private static void SyncRuntimeDictionary(SerializedProperty dictionaryProperty)
        {
            SerializedObject serializedObject = dictionaryProperty.serializedObject;
            Object[] targets = serializedObject.targetObjects;

            foreach (Object target in targets)
            {
                object dictionaryInstance = GetTargetObjectOfProperty(
                    target,
                    dictionaryProperty.propertyPath
                );
                if (dictionaryInstance is SerializableDictionaryBase baseDictionary)
                {
                    baseDictionary.EditorAfterDeserialize();
                    EditorUtility.SetDirty(target);
                    if (target is UnityHelpersSettings unitySettings)
                    {
                        unitySettings.SaveSettings();
                    }
                }
                else if (dictionaryInstance is ISerializationCallbackReceiver receiver)
                {
                    receiver.OnAfterDeserialize();
                    EditorUtility.SetDirty(target);
                    if (target is UnityHelpersSettings unitySettings)
                    {
                        unitySettings.SaveSettings();
                    }
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
                    if (current is IList list)
                    {
                        if (index >= list.Count)
                        {
                            return null;
                        }

                        current = list[index];
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

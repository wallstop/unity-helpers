namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.AnimatedValues;
    using UnityEditorInternal;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Editor.Settings;

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

        private const float PendingSectionPadding = 6f;
        private const float PendingAddButtonWidth = 110f;
        private const float PendingFoldoutAnimationSpeed = 2f;
        private const int DefaultPageSize = 15;
        internal const int MaxPageSize = 250;
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
        private static readonly Color ThemeRemoveColor = new(0.92f, 0.29f, 0.33f, 1f);
        private static readonly Color ThemeAddColor = new(0.25f, 0.68f, 0.38f, 1f);
        private static readonly Color ThemeOverwriteColor = new(0.98f, 0.82f, 0.27f, 1f);
        private static readonly Color ThemeResetColor = new(0.7f, 0.7f, 0.7f, 1f);
        private static readonly Color ThemeDisabledColor = new(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color DuplicatePrimaryColor = new(0.99f, 0.82f, 0.35f, 0.55f);
        private static readonly Color DuplicateSecondaryColor = new(0.96f, 0.45f, 0.45f, 0.65f);
        private static readonly Color DuplicateOutlineColor = new(0.65f, 0.18f, 0.18f, 0.9f);
        private static readonly Dictionary<string, GUIStyle> ButtonStyleCache = new();
        private static readonly Dictionary<Color, Texture2D> ColorTextureCache = new();
        private static readonly GUIContent DuplicateTooltipContent = new();
        private static readonly GUIContent DuplicateIconContentCache = new();
        private static readonly GUIContent DuplicateIconTemplate = EditorGUIUtility.IconContent(
            "console.warnicon.sml"
        );
        private static readonly GUIContent PendingFoldoutContent = EditorGUIUtility.TrTextContent(
            "New Entry"
        );
        private static GUIStyle _footerLabelStyle;
        private static readonly object NullKeySentinel = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

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
                    out bool _
                )
            )
            {
                EditorGUI.LabelField(position, label.text, "Unsupported dictionary type");
                EditorGUI.EndProperty();
                return;
            }

            string cacheKey = GetListKey(property);
            DuplicateKeyState duplicateState = RefreshDuplicateState(
                cacheKey,
                keysProperty,
                keyType
            );

            Rect foldoutRect = new(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight
            );
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (duplicateState is { HasDuplicates: true })
            {
                DrawDuplicateFoldoutBadge(position, foldoutRect, duplicateState.SummaryTooltip);
            }

            float y = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;

            if (property.isExpanded)
            {
                ReorderableList list = GetOrCreateList(property, keysProperty, valuesProperty);
                PaginationState pagination = GetOrCreatePaginationState(property);
                PendingEntry pending = GetOrCreatePendingEntry(property, keyType, valueType);

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

                int previousIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                list.DoList(listRect);
                EditorGUI.indentLevel = previousIndent;
            }

            bool applied = serializedObject.ApplyModifiedProperties();
            if (applied)
            {
                SyncRuntimeDictionary(property);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded)
            {
                return height;
            }

            PendingEntry pending = null;
            if (
                TryResolveKeyValueTypes(fieldInfo, out Type keyType, out Type valueType, out bool _)
            )
            {
                pending = GetOrCreatePendingEntry(property, keyType, valueType);
            }

            SerializedProperty keysProperty = property.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = property.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );
            EnsureParallelArraySizes(keysProperty, valuesProperty);
            ReorderableList list = GetOrCreateList(property, keysProperty, valuesProperty);

            float listHeight = list.GetHeight();
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float pendingHeight = GetPendingSectionHeight(pending);

            height += spacing + pendingHeight + spacing + listHeight;
            return height;
        }

        internal ReorderableList GetOrCreateList(
            SerializedProperty dictionaryProperty,
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty
        )
        {
            string key = GetListKey(dictionaryProperty);
            PaginationState pagination = GetOrCreatePaginationState(dictionaryProperty);
            ClampPaginationState(pagination, keysProperty.arraySize);
            float rowHeight =
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2f;
            float emptyHeight = Mathf.Max(
                EditorGUIUtility.standardVerticalSpacing * 2f,
                EditorGUIUtility.standardVerticalSpacing
            );

            Func<ListPageCache> cacheProvider = () =>
                EnsurePageCache(key, keysProperty, pagination);

            ListPageCache cache = cacheProvider();

            if (_lists.TryGetValue(key, out ReorderableList cached))
            {
                SyncListSelectionWithPagination(cached, pagination, cache);
                cached.drawNoneElementCallback = _ => { };
                cached.elementHeight = keysProperty.arraySize == 0 ? emptyHeight : rowHeight;
                return cached;
            }

            ReorderableList list = new(
                cache.entries,
                typeof(PageEntry),
                draggable: true,
                displayHeader: true,
                displayAddButton: false,
                displayRemoveButton: false
            );

            list.drawHeaderCallback = rect =>
            {
                ListPageCache currentCache = cacheProvider();
                SyncListSelectionWithPagination(list, pagination, currentCache);
                DrawListHeader(rect, keysProperty, list, pagination, cacheProvider);
            };

            list.elementHeightCallback = _ => rowHeight;
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

                int globalIndex = currentCache.entries[index].arrayIndex;
                if (
                    globalIndex < 0
                    || globalIndex >= keysProperty.arraySize
                    || globalIndex >= valuesProperty.arraySize
                )
                {
                    return;
                }

                float spacing = EditorGUIUtility.standardVerticalSpacing;
                Rect backgroundRect = new(
                    rect.x,
                    rect.y,
                    rect.width,
                    EditorGUIUtility.singleLineHeight + spacing * 2f
                );
                Color rowColor = EditorGUIUtility.isProSkin ? DarkRowColor : LightRowColor;
                EditorGUI.DrawRect(backgroundRect, rowColor);

                bool hasDuplicate = TryGetDuplicateInfo(
                    key,
                    globalIndex,
                    out DuplicateKeyInfo duplicateInfo,
                    out DuplicateKeyState duplicateState
                );

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

                Rect highlightRect = backgroundRect;
                highlightRect.x += shakeOffset;

                if (hasDuplicate)
                {
                    if (highlightDuplicates)
                    {
                        Color highlightColor = duplicateInfo.isPrimary
                            ? DuplicatePrimaryColor
                            : DuplicateSecondaryColor;
                        EditorGUI.DrawRect(highlightRect, highlightColor);
                        DrawDuplicateOutline(highlightRect);
                    }

                    DrawDuplicateTooltip(highlightRect, duplicateInfo.tooltip);
                }

                if (list.index == index)
                {
                    Rect selectionRect = backgroundRect;
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

                int globalIndex = currentCache.entries[index].arrayIndex;
                if (
                    globalIndex < 0
                    || globalIndex >= keysProperty.arraySize
                    || globalIndex >= valuesProperty.arraySize
                )
                {
                    return;
                }

                SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(globalIndex);
                SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(
                    globalIndex
                );

                rect.y += EditorGUIUtility.standardVerticalSpacing;
                bool hasDuplicate = TryGetDuplicateInfo(
                    key,
                    globalIndex,
                    out DuplicateKeyInfo duplicateInfo,
                    out DuplicateKeyState duplicateState
                );

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
                float halfWidth = (rect.width - gap) * 0.5f;

                Rect keyRect = new(rect.x, rect.y, halfWidth, EditorGUIUtility.singleLineHeight);
                Rect valueRect = new(
                    rect.x + halfWidth + gap,
                    rect.y,
                    halfWidth,
                    EditorGUIUtility.singleLineHeight
                );

                if (hasDuplicate)
                {
                    float iconSize = EditorGUIUtility.singleLineHeight;
                    float iconSpacing = 3f;
                    Rect iconRect = new(keyRect.x, keyRect.y, iconSize, iconSize);
                    GUIContent iconContent = GetDuplicateIconContent(duplicateInfo.tooltip);
                    GUI.Label(iconRect, iconContent);

                    keyRect.x += iconSize + iconSpacing;
                    keyRect.width -= iconSize + iconSpacing;
                    if (keyRect.width < 0f)
                    {
                        keyRect.width = 0f;
                    }
                }

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(keyRect, keyProperty, GUIContent.none, true);
                if (EditorGUI.EndChangeCheck())
                {
                    InvalidateKeyCache(key);
                }

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none, true);
                if (EditorGUI.EndChangeCheck())
                {
                    MarkListCacheDirty(key);
                }
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

            list.onReorderCallbackWithDetails = (_, oldIndex, newIndex) =>
            {
                ListPageCache currentCache = cacheProvider();
                if (
                    !RelativeIndexIsValid(currentCache, oldIndex)
                    || !RelativeIndexIsValid(currentCache, newIndex)
                )
                {
                    return;
                }

                int oldGlobalIndex = currentCache.entries[oldIndex].arrayIndex;
                int newGlobalIndex = currentCache.entries[newIndex].arrayIndex;
                keysProperty.MoveArrayElement(oldGlobalIndex, newGlobalIndex);
                valuesProperty.MoveArrayElement(oldGlobalIndex, newGlobalIndex);
                pagination.selectedIndex = newGlobalIndex;
                InvalidateKeyCache(key);

                ListPageCache refreshedCache = EnsurePageCache(key, keysProperty, pagination);
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
            list.elementHeight = keysProperty.arraySize == 0 ? emptyHeight : rowHeight;

            _lists[key] = list;
            return list;
        }

        private void DrawDuplicateFoldoutBadge(Rect positionRect, Rect foldoutRect, string tooltip)
        {
            float iconSize = EditorGUIUtility.singleLineHeight;
            float padding = 4f;
            Rect iconRect = new(
                Mathf.Max(positionRect.x, positionRect.xMax - iconSize - padding),
                foldoutRect.y,
                iconSize,
                iconSize
            );

            GUIContent iconContent = GetDuplicateIconContent(
                string.IsNullOrEmpty(tooltip)
                    ? "Duplicate keys detected. Resolve conflicts to prevent silent overwrites. The last entry wins at runtime."
                    : tooltip
            );
            GUI.Label(iconRect, iconContent);
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

            bool changed = state.Refresh(keysProperty, keyType);
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

            if (!_duplicateStates.TryGetValue(cacheKey, out state))
            {
                return false;
            }

            return state.TryGetInfo(arrayIndex, out info);
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

            if (actualKey is UnityEngine.Object unityObject)
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

        private PendingEntry GetOrCreatePendingEntry(
            SerializedProperty property,
            Type keyType,
            Type valueType
        )
        {
            string key = GetListKey(property);
            if (_pendingEntries.TryGetValue(key, out PendingEntry entry))
            {
                EnsurePendingFoldoutAnim(entry);
                return entry;
            }

            entry = new PendingEntry
            {
                key = GetDefaultValue(keyType),
                value = GetDefaultValue(valueType),
                isExpanded = false,
            };
            entry.foldoutAnim = CreatePendingFoldoutAnim(entry.isExpanded);
            _pendingEntries[key] = entry;
            return entry;
        }

        internal PaginationState GetOrCreatePaginationState(SerializedProperty property)
        {
            string key = GetListKey(property);
            if (_paginationStates.TryGetValue(key, out PaginationState state))
            {
                if (state.pageSize <= 0)
                {
                    state.pageSize = DefaultPageSize;
                }

                return state;
            }

            PaginationState newState = new()
            {
                pageIndex = 0,
                pageSize = DefaultPageSize,
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
            if (_pageCaches.TryGetValue(cacheKey, out ListPageCache cache))
            {
                cache.entries.Clear();
                cache.dirty = true;
                cache.pageIndex = -1;
                cache.pageSize = -1;
                cache.itemCount = -1;
            }
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

            float spacing = PaginationControlSpacing;
            float buttonWidth = PaginationButtonWidth;
            float navWidthFull = (buttonWidth * 4f) + (spacing * 3f);
            float navWidthPrevNext = (buttonWidth * 2f) + spacing;

            float ComputeControlsWidth(float navWidth, bool includePageLabel)
            {
                float width = navWidth;
                if (includePageLabel)
                {
                    width += PaginationLabelWidth;
                    if (navWidth > 0f)
                    {
                        width += spacing;
                    }
                }

                return width;
            }

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
                navStartX = pageLabelRect.xMax + (navWidth > 0f ? spacing : 0f);
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
                        buttonWidth,
                        controlsRect.height
                    );
                    Rect prevRect = new(
                        firstRect.xMax + spacing,
                        controlsRect.y,
                        buttonWidth,
                        controlsRect.height
                    );
                    Rect nextRect = new(
                        prevRect.xMax + spacing,
                        controlsRect.y,
                        buttonWidth,
                        controlsRect.height
                    );
                    Rect lastRect = new(
                        nextRect.xMax + spacing,
                        controlsRect.y,
                        buttonWidth,
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
                        buttonWidth,
                        controlsRect.height
                    );
                    Rect nextOnlyRect = new(
                        prevOnlyRect.xMax + spacing,
                        controlsRect.y,
                        buttonWidth,
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
            float buttonSpacing = PaginationControlSpacing;
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

            Type keyType = null;
            Type valueType = null;
            bool isSortedDictionary = false;
            bool resolvedTypes = TryResolveKeyValueTypes(
                fieldInfo,
                out keyType,
                out valueType,
                out isSortedDictionary
            );

            object dictionaryInstance = null;
            if (!resolvedTypes || keyType == null || valueType == null)
            {
                dictionaryInstance = GetDictionaryInstance(dictionaryProperty);
                Type runtimeKeyType;
                Type runtimeValueType;
                bool runtimeSorted;
                if (
                    TryResolveKeyValueTypesFromInstance(
                        dictionaryInstance,
                        out runtimeKeyType,
                        out runtimeValueType,
                        out runtimeSorted
                    )
                )
                {
                    if (keyType == null)
                    {
                        keyType = runtimeKeyType;
                    }

                    if (valueType == null)
                    {
                        valueType = runtimeValueType;
                    }

                    isSortedDictionary = runtimeSorted;
                }
            }
            else
            {
                dictionaryInstance = GetDictionaryInstance(dictionaryProperty);
            }

            Func<object, object, int> comparison = null;
            bool canSort = false;
            bool sortEnabled = false;
            if (isSortedDictionary && keyType != null && valueType != null)
            {
                object comparerInstance = ResolveComparerInstance(
                    dictionaryProperty,
                    dictionaryInstance,
                    keyType
                );
                comparison = CreateComparisonDelegate(comparerInstance, keyType);
                if (comparison != null)
                {
                    canSort = itemCount > 1;
                    bool alreadySorted = KeysAreSorted(keysProperty, keyType, comparison);
                    sortEnabled = canSort && !alreadySorted;
                }
            }

            GUIContent clearAllContent = EditorGUIUtility.TrTextContent(
                "Clear All",
                "Remove every entry from the dictionary"
            );
            GUIContent removeContent = EditorGUIUtility.TrTextContent("-", "Remove selected entry");
            GUIContent sortContent = EditorGUIUtility.TrTextContent(
                "Sort",
                "Sort entries by key using the dictionary comparer"
            );

            bool showRange = itemCount > 0 || rangeText == "Empty";
            bool showClear = true;
            bool showSort = isSortedDictionary && comparison != null;
            bool showRemove = true;

            float requiredWidth = float.PositiveInfinity;

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
                    width += buttonSpacing;
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
                        rightWidth += buttonSpacing;
                    }
                    rightWidth += clearWidth;
                }
                if (includeSort)
                {
                    if (rightWidth > 0f)
                    {
                        rightWidth += buttonSpacing;
                    }
                    rightWidth += sortWidth;
                }

                width += rightWidth;
                return width;
            }

            requiredWidth = CalculateRequiredWidth(showRange, showClear, showSort, showRemove);
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
                    currentX -= buttonSpacing;
                }
            }

            if (showClear)
            {
                currentX -= clearWidth;
                clearRect = new Rect(currentX, verticalCenter, clearWidth, lineHeight);
                if (showSort || showRange)
                {
                    currentX -= buttonSpacing;
                }
            }

            if (showSort)
            {
                currentX -= sortWidth;
                sortRect = new Rect(currentX, verticalCenter, sortWidth, lineHeight);
                if (showRange)
                {
                    currentX -= buttonSpacing;
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
                            list,
                            cacheProvider
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
            UnityEngine.Object[] targets = serializedObject.targetObjects;
            if (targets.Length > 0)
            {
                Undo.RecordObjects(targets, "Remove Dictionary Entry");
            }

            keysProperty.DeleteArrayElementAtIndex(removeIndex);
            valuesProperty.DeleteArrayElementAtIndex(removeIndex);
            serializedObject.ApplyModifiedProperties();
            SyncRuntimeDictionary(dictionaryProperty);
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
            UnityEngine.Object[] targets = serializedObject.targetObjects;
            if (targets.Length > 0)
            {
                Undo.RecordObjects(targets, "Clear Dictionary Entries");
            }

            keysProperty.ClearArray();
            valuesProperty.ClearArray();
            serializedObject.ApplyModifiedProperties();
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
            GUIStyle style = GetSolidButtonStyle(actionKey, enabled);
            if (!enabled)
            {
                EditorGUI.DrawRect(rect, ThemeDisabledColor);
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

            float sectionHeight = GetPendingSectionHeight(foldoutProgress);
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

            float innerX = PendingSectionPadding;
            float innerWidth = Mathf.Max(0f, containerRect.width - PendingSectionPadding * 2f);
            float innerY = PendingSectionPadding;

            Rect headerRect = new(innerX, innerY, innerWidth, rowHeight);
            EditorGUI.BeginChangeCheck();
            FontStyle originalFoldoutFontStyle = EditorStyles.foldout.fontStyle;
            EditorStyles.foldout.fontStyle = FontStyle.Bold;
            bool expanded = EditorGUI.Foldout(
                headerRect,
                pending.isExpanded,
                PendingFoldoutContent,
                true
            );
            EditorStyles.foldout.fontStyle = originalFoldoutFontStyle;
            if (EditorGUI.EndChangeCheck())
            {
                pending.isExpanded = expanded;
                GUI.changed = true;
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

                sectionHeight = GetPendingSectionHeight(foldoutProgress);
                RequestRepaint();
            }

            containerRect.height = sectionHeight;

            float contentFade = Mathf.Clamp01(foldoutProgress);
            if (contentFade <= 0f && !pending.isExpanded)
            {
                GUI.EndGroup();
                y = containerRect.yMax;
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

            Rect keyRect = new(innerX, innerY, innerWidth, rowHeight);
            pending.key = DrawFieldForType(keyRect, "Key", pending.key, keyType);
            innerY += rowHeight + spacing;

            Rect valueRect = new(innerX, innerY, innerWidth, rowHeight);
            pending.value = DrawFieldForType(valueRect, "Value", pending.value, valueType);
            innerY += rowHeight + spacing;

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

            Rect buttonsRect = new(innerX, innerY, innerWidth, rowHeight);
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
                GUIStyle addStyle = GetSolidButtonStyle(styleKey, GUI.enabled);

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
                    }

                    if (result.index >= 0)
                    {
                        int targetPage = GetPageForIndex(result.index, pagination.pageSize);
                        int totalPages = GetTotalPages(keysProperty.arraySize, pagination.pageSize);
                        pagination.pageIndex = Mathf.Clamp(targetPage, 0, totalPages - 1);
                        pagination.selectedIndex = result.index;

                        string listKey = GetListKey(dictionaryProperty);
                        ListPageCache cache = EnsurePageCache(listKey, keysProperty, pagination);
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
                GUIStyle resetStyle = GetSolidButtonStyle("Reset", GUI.enabled);
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
        }

        private static GUIStyle GetSolidButtonStyle(string action, bool enabled)
        {
            if (string.IsNullOrEmpty(action))
            {
                action = "Default";
            }

            string cacheKey = $"{action}_{(enabled ? "Enabled" : "Disabled")}";
            if (ButtonStyleCache.TryGetValue(cacheKey, out GUIStyle cached))
            {
                return cached;
            }

            GUIStyle baseStyle = new(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(8, 8, 3, 3),
            };

            Color baseColor = enabled ? GetActionColor(action) : ThemeDisabledColor;
            Color hoverColor = enabled ? AdjustValue(baseColor, 0.08f) : baseColor;
            Color pressedColor = enabled ? AdjustValue(baseColor, -0.08f) : baseColor;
            Color textColor = enabled
                ? GetLegibleTextColor(baseColor)
                : AdjustAlpha(GetLegibleTextColor(baseColor), 0.6f);

            Texture2D normalTexture = GetSolidTexture(baseColor);
            Texture2D hoverTexture = GetSolidTexture(hoverColor);
            Texture2D pressedTexture = GetSolidTexture(pressedColor);

            baseStyle.normal.background = normalTexture;
            baseStyle.hover.background = hoverTexture;
            baseStyle.active.background = pressedTexture;
            baseStyle.focused.background = normalTexture;
            baseStyle.onNormal.background = normalTexture;
            baseStyle.onHover.background = hoverTexture;
            baseStyle.onActive.background = pressedTexture;
            baseStyle.onFocused.background = normalTexture;

            baseStyle.normal.textColor = textColor;
            baseStyle.hover.textColor = textColor;
            baseStyle.active.textColor = textColor;
            baseStyle.focused.textColor = textColor;
            baseStyle.onNormal.textColor = textColor;
            baseStyle.onHover.textColor = textColor;
            baseStyle.onActive.textColor = textColor;
            baseStyle.onFocused.textColor = textColor;

            ButtonStyleCache[cacheKey] = baseStyle;
            return baseStyle;
        }

        private static Color GetActionColor(string action)
        {
            switch (action)
            {
                case "Add":
                    return ThemeAddColor;
                case "Overwrite":
                    return ThemeOverwriteColor;
                case "AddEmpty":
                    return ThemeOverwriteColor;
                case "Reset":
                    return ThemeResetColor;
                case "Sort":
                    return ThemeAddColor;
                case "Remove":
                case "ClearAll":
                    return ThemeRemoveColor;
                default:
                    return ThemeResetColor;
            }
        }

        private static Color AdjustValue(Color color, float delta)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            v = Mathf.Clamp01(v + delta);
            Color result = Color.HSVToRGB(h, s, v);
            result.a = color.a;
            return result;
        }

        private static Color AdjustAlpha(Color color, float multiplier)
        {
            color.a *= multiplier;
            return color;
        }

        private static Color GetLegibleTextColor(Color background)
        {
            float luminance = 0.299f * background.r + 0.587f * background.g + 0.114f * background.b;
            return luminance > 0.55f ? Color.black : Color.white;
        }

        private static Texture2D GetSolidTexture(Color color)
        {
            if (ColorTextureCache.TryGetValue(color, out Texture2D cached))
            {
                return cached;
            }

            Texture2D texture = new(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            ColorTextureCache[color] = texture;
            return texture;
        }

        private static float GetPendingSectionHeight(float foldoutProgress)
        {
            float rowHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float baseHeight = rowHeight * 1f + PendingSectionPadding * 2f;
            float expandedExtraHeight = rowHeight * 3f + spacing * 3f;
            return baseHeight + expandedExtraHeight * Mathf.Clamp01(foldoutProgress);
        }

        private static float GetPendingSectionHeight(PendingEntry pending)
        {
            return GetPendingSectionHeight(GetPendingFoldoutProgress(pending));
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
            int affectedIndex;

            if (existingIndex >= 0)
            {
                SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(
                    existingIndex
                );
                SetPropertyValue(valueProperty, pending.value, valueType);
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
                addedNewEntry = true;
                affectedIndex = insertIndex;
            }

            serializedObject.ApplyModifiedProperties();
            SyncRuntimeDictionary(dictionaryProperty);
            GUI.changed = true;
            InvalidateKeyCache(GetListKey(dictionaryProperty));
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
            ReorderableList list,
            Func<ListPageCache> cacheProvider
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
            UnityEngine.Object[] targets = serializedObject.targetObjects;
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

            serializedObject.ApplyModifiedProperties();
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
                    return y == null ? 0 : -1;
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

        private static AnimBool CreatePendingFoldoutAnim(bool initialValue)
        {
            AnimBool anim = new(initialValue) { speed = PendingFoldoutAnimationSpeed };
            anim.valueChanged.AddListener(RequestRepaint);
            return anim;
        }

        private static AnimBool EnsurePendingFoldoutAnim(PendingEntry pending)
        {
            if (pending == null)
            {
                return null;
            }

            if (pending.foldoutAnim == null)
            {
                pending.foldoutAnim = CreatePendingFoldoutAnim(pending.isExpanded);
            }
            else
            {
                pending.foldoutAnim.speed = PendingFoldoutAnimationSpeed;
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
            if (left is UnityEngine.Object leftObj && right is UnityEngine.Object rightObj)
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
            public bool added;
            public int index;
        }

        private enum PaginationControlLayout
        {
            None,
            PrevNext,
            Full,
        }

        private sealed class PendingEntry
        {
            public object key;
            public object value;
            public bool isExpanded;
            public AnimBool foldoutAnim;
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
                }

                if (duplicateGroupCount > 0)
                {
                    SummaryTooltip =
                        duplicateGroupCount == 1
                            ? "Duplicate key detected. Resolve conflicts to prevent silent overwrites. The last entry wins at runtime."
                            : $"{duplicateGroupCount} duplicate keys detected. Resolve conflicts to prevent silent overwrites. The last entry wins at runtime.";
                }

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
                    if (!_duplicateAnimationStartTimes.ContainsKey(index))
                    {
                        _duplicateAnimationStartTimes[index] = now;
                    }
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

                for (int i = 0; i < _animationKeysScratch.Count; i++)
                {
                    _duplicateAnimationStartTimes.Remove(_animationKeysScratch[i]);
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

                if (x is UnityEngine.Object xObj)
                {
                    if (y is UnityEngine.Object yObj)
                    {
                        return xObj == yObj;
                    }

                    return false;
                }

                if (y is UnityEngine.Object)
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

                if (obj is UnityEngine.Object objObj && objObj == null)
                {
                    return 0;
                }

                return obj.GetHashCode();
            }
        }

        private static void SyncRuntimeDictionary(SerializedProperty dictionaryProperty)
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

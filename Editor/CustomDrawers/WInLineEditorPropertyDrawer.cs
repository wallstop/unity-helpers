namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Utils;

    [CustomPropertyDrawer(typeof(WInLineEditorAttribute))]
    public sealed class WInLineEditorPropertyDrawer : PropertyDrawer
    {
        internal const float InlineBorderThickness = 1f;
        internal const float InlinePadding = 6f;
        internal const float InlineHeaderSpacing = 2f;
        private const float InlinePreviewSpacing = 4f;
        private const float InlinePingButtonWidth = 58f;
        internal const float InlineInspectorRightPadding = 6f;
        internal const float InlineGroupNestingPadding = 4f;
        internal const float InlineGroupEdgePadding = 8f;
        internal const float InlineHorizontalScrollbarHeight = 12f;
        internal const float InlineVerticalScrollbarWidth = 12f;
        private const float InlineHorizontalScrollHysteresis = 12f;
        private const float InlinePreferredWidthReleaseTolerance = 8f;
        internal const int InlineHorizontalScrollbarReleaseRepaintThreshold = 3;
        private const float DefaultViewWidthFallback = 640f;

        private static readonly Color LightInlineBackground = new(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color DarkInlineBackground = new(0.17f, 0.17f, 0.17f, 1f);
        private static readonly Color InlineBorderColor = new(0.25f, 0.25f, 0.25f, 1f);

        private static readonly Dictionary<string, InlineInspectorImGuiState> ImGuiStateCache =
            new();
        private static readonly HashSet<string> HorizontalScrollbarReservationKeys = new();
        private static float s_LastMeasuredViewWidth = DefaultViewWidthFallback;
        private static bool s_UsingCustomViewWidthResolver;
        private static Func<float> s_ViewWidthResolver = () => EditorGUIUtility.currentViewWidth;
        private static readonly Func<Rect> DefaultVisibleRectResolver = CreateVisibleRectResolver();
        private static Func<Rect> s_VisibleRectResolver = DefaultVisibleRectResolver;

        static WInLineEditorPropertyDrawer()
        {
            AssemblyReloadEvents.beforeAssemblyReload += ClearImGuiStateCache;
            EditorApplication.quitting += ClearImGuiStateCache;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!s_UsingCustomViewWidthResolver)
            {
                try
                {
                    float currentViewWidth = EditorGUIUtility.currentViewWidth;
                    if (currentViewWidth > 0f)
                    {
                        s_LastMeasuredViewWidth = currentViewWidth;
                    }
                }
                catch
                {
                    s_LastMeasuredViewWidth = Mathf.Max(
                        DefaultViewWidthFallback,
                        position.x + position.width
                    );
                }
            }
            if (
                !TryResolveSettings(
                    property,
                    out WInLineEditorAttribute inlineAttribute,
                    out FieldInfo resolvedField
                )
            )
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;
            float cursorY = position.y;
            float width = position.width;
            bool hasFoldout = inlineAttribute.mode != WInLineEditorMode.AlwaysExpanded;
            UnityEngine.Object initialValue = property.objectReferenceValue;
            bool hasReference = initialValue != null;
            string sessionKey = BuildSessionKey(property);
            bool defaultExpanded = inlineAttribute.mode == WInLineEditorMode.FoldoutExpanded;
            bool persistedFoldout = hasFoldout
                ? (
                    string.IsNullOrEmpty(sessionKey)
                        ? defaultExpanded
                        : SessionState.GetBool(sessionKey, defaultExpanded)
                )
                : true;
            bool displayFoldout = hasFoldout ? (hasReference ? persistedFoldout : false) : true;

            if (hasFoldout)
            {
                if (hasReference)
                {
                    Rect foldoutRect = ExpandToViewWidth(
                        new Rect(position.x, cursorY, width, lineHeight)
                    );
                    bool userFoldout = EditorGUI.Foldout(foldoutRect, displayFoldout, label, true);

                    if (userFoldout != persistedFoldout)
                    {
                        persistedFoldout = userFoldout;
                        if (!string.IsNullOrEmpty(sessionKey))
                        {
                            SessionState.SetBool(sessionKey, persistedFoldout);
                        }
                    }

                    displayFoldout = persistedFoldout;
                    cursorY += lineHeight;

                    if (inlineAttribute.drawObjectField)
                    {
                        cursorY += verticalSpacing;
                        float objectFieldHeight = EditorGUI.GetPropertyHeight(
                            property,
                            GUIContent.none,
                            true
                        );
                        Rect objectRect = ExpandToViewWidth(
                            new Rect(position.x, cursorY, width, objectFieldHeight)
                        );
                        DrawObjectReferenceField(objectRect, property, GUIContent.none);
                        cursorY += objectFieldHeight;
                    }
                }
                else
                {
                    if (inlineAttribute.drawObjectField)
                    {
                        float objectFieldHeight = EditorGUI.GetPropertyHeight(
                            property,
                            label,
                            true
                        );
                        Rect objectRect = ExpandToViewWidth(
                            new Rect(position.x, cursorY, width, objectFieldHeight)
                        );
                        DrawObjectReferenceField(objectRect, property, label);
                        cursorY += objectFieldHeight;
                    }
                    else
                    {
                        Rect labelRect = ExpandToViewWidth(
                            new Rect(position.x, cursorY, width, lineHeight)
                        );
                        EditorGUI.LabelField(labelRect, label);
                        cursorY += lineHeight;
                    }
                }
            }
            else
            {
                if (inlineAttribute.drawObjectField)
                {
                    float objectFieldHeight = EditorGUI.GetPropertyHeight(property, label, true);
                    Rect objectRect = ExpandToViewWidth(
                        new Rect(position.x, cursorY, width, objectFieldHeight)
                    );
                    DrawObjectReferenceField(objectRect, property, label);
                    cursorY += objectFieldHeight;
                }
                else
                {
                    Rect labelRect = ExpandToViewWidth(
                        new Rect(position.x, cursorY, width, lineHeight)
                    );
                    EditorGUI.LabelField(labelRect, label);
                    cursorY += lineHeight;
                }

                displayFoldout = true;
            }

            UnityEngine.Object currentValue = property.objectReferenceValue;
            if (hasFoldout && currentValue != null)
            {
                displayFoldout = persistedFoldout;
            }
            InlineInspectorImGuiState state = GetOrCreateImGuiState(sessionKey);
            UpdateImGuiStateTarget(state, currentValue);
            EventType currentEventType = Event.current?.type ?? EventType.Layout;
            bool isLayoutPass = currentEventType == EventType.Layout;
            bool isRepaintPass = currentEventType == EventType.Repaint;

            bool shouldShowInline =
                currentValue != null
                && (inlineAttribute.mode == WInLineEditorMode.AlwaysExpanded || displayFoldout);

            if (shouldShowInline)
            {
                bool hasReservedScrollbarSpace =
                    !string.IsNullOrEmpty(sessionKey)
                    && HorizontalScrollbarReservationKeys.Contains(sessionKey);
                float inlineHeight = CalculateInlineContainerHeight(inlineAttribute);
                if (hasReservedScrollbarSpace)
                {
                    inlineHeight += InlineHorizontalScrollbarHeight;
                }
                cursorY += verticalSpacing;
                Rect inlineRect = ExpandToViewWidth(
                    new Rect(position.x, cursorY, width, inlineHeight),
                    out float preferredInlineWidth,
                    out bool widthWasClipped
                );
                bool hasCommittedInlineWidth = state.PreferredInlineRect.width > 0.5f;
                if (isLayoutPass && hasCommittedInlineWidth)
                {
                    inlineRect.x = state.PreferredInlineRect.x;
                    inlineRect.width = state.PreferredInlineRect.width;
                    if (state.PreferredInlineWidthValue > 0.5f)
                    {
                        preferredInlineWidth = state.PreferredInlineWidthValue;
                    }
                    widthWasClipped = state.InlineWidthWasClipped;
                }
                else if (isRepaintPass)
                {
                    state.PreferredInlineRect = inlineRect;
                    state.PreferredInlineWidthValue =
                        preferredInlineWidth > 0f ? preferredInlineWidth : inlineRect.width;
                    state.InlineWidthWasClipped = widthWasClipped;
                }
                DrawInlineInspector(
                    sessionKey,
                    inlineRect,
                    state,
                    inlineAttribute,
                    preferredInlineWidth,
                    widthWasClipped,
                    hasReservedScrollbarSpace
                );
                TrackHorizontalScrollbarReservation(sessionKey, state.UsedHorizontalScroll);
            }
            else
            {
                TrackHorizontalScrollbarReservation(sessionKey, false);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!TryResolveSettings(property, out WInLineEditorAttribute inlineAttribute, out _))
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            float height = 0f;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;
            bool hasFoldout = inlineAttribute.mode != WInLineEditorMode.AlwaysExpanded;
            bool hasReference = property != null && property.objectReferenceValue != null;

            if (hasFoldout && hasReference)
            {
                height += lineHeight;
                if (inlineAttribute.drawObjectField)
                {
                    height += verticalSpacing;
                    height += EditorGUI.GetPropertyHeight(property, GUIContent.none, true);
                }
            }
            else
            {
                if (inlineAttribute.drawObjectField)
                {
                    height += EditorGUI.GetPropertyHeight(property, label, true);
                }
                else
                {
                    height += lineHeight;
                }
            }

            string sessionKey = BuildSessionKey(property);
            bool showInline = ShouldShowInlineInspector(property, inlineAttribute);
            if (showInline)
            {
                height += verticalSpacing;
                height += CalculateInlineContainerHeight(inlineAttribute);

                bool reserveScrollbarSpace = ShouldReserveHorizontalScrollbarSpace(
                    sessionKey,
                    inlineAttribute
                );
                if (reserveScrollbarSpace)
                {
                    height += InlineHorizontalScrollbarHeight;
                }

                TrackHorizontalScrollbarReservation(sessionKey, reserveScrollbarSpace);
            }
            else
            {
                TrackHorizontalScrollbarReservation(sessionKey, false);
            }

            return height;
        }

        private bool TryResolveSettings(
            SerializedProperty property,
            out WInLineEditorAttribute inlineAttribute,
            out FieldInfo resolvedField
        )
        {
            inlineAttribute = null;
            resolvedField = null;

            if (property == null)
            {
                return false;
            }

            FieldInfo field = fieldInfo ?? ResolveFieldInfo(property);
            WInLineEditorAttribute drawerAttribute =
                attribute as WInLineEditorAttribute
                ?? ReflectionHelpers.GetAttributeSafe<WInLineEditorAttribute>(field, true);

            if (
                drawerAttribute == null
                || property.propertyType != SerializedPropertyType.ObjectReference
            )
            {
                return false;
            }

            inlineAttribute = drawerAttribute;
            resolvedField = field;
            return true;
        }

        private static FieldInfo ResolveFieldInfo(SerializedProperty property)
        {
            if (property == null)
            {
                return null;
            }

            SerializedObject owner = property.serializedObject;
            if (owner == null)
            {
                return null;
            }

            UnityEngine.Object target = owner.targetObject;
            if (target == null)
            {
                return null;
            }

            Type currentType = target.GetType();

            string propertyPath = property.propertyPath;
            if (string.IsNullOrEmpty(propertyPath))
            {
                return null;
            }

            string normalizedPath = propertyPath.Replace(".Array.data[", ".[");
            string[] elements = normalizedPath.Split('.');

            FieldInfo resolvedField = null;

            foreach (string rawElement in elements)
            {
                if (string.IsNullOrEmpty(rawElement))
                {
                    continue;
                }

                if (rawElement[0] == '[')
                {
                    currentType = GetElementType(currentType);
                    if (currentType == null)
                    {
                        return null;
                    }

                    continue;
                }

                string memberName = rawElement;
                int bracketIndex = memberName.IndexOf('[');
                if (bracketIndex >= 0)
                {
                    memberName = memberName.Substring(0, bracketIndex);
                }

                FieldInfo field = GetFieldFromHierarchy(currentType, memberName);
                if (field == null)
                {
                    return null;
                }

                resolvedField = field;
                currentType = field.FieldType;
            }

            return resolvedField;
        }

        private static FieldInfo GetFieldFromHierarchy(Type type, string fieldName)
        {
            const BindingFlags Flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Type searchType = type;
            while (searchType != null)
            {
                FieldInfo field = searchType.GetField(fieldName, Flags);
                if (field != null)
                {
                    return field;
                }

                searchType = searchType.BaseType;
            }

            return null;
        }

        private static Type GetElementType(Type collectionType)
        {
            if (collectionType == null)
            {
                return null;
            }

            if (collectionType.IsArray)
            {
                return collectionType.GetElementType();
            }

            if (
                typeof(IList).IsAssignableFrom(collectionType)
                && collectionType.IsGenericType
                && collectionType.GetGenericArguments().Length == 1
            )
            {
                return collectionType.GetGenericArguments()[0];
            }

            if (
                typeof(IEnumerable).IsAssignableFrom(collectionType)
                && collectionType.IsGenericType
                && collectionType.GetGenericArguments().Length == 1
            )
            {
                return collectionType.GetGenericArguments()[0];
            }

            return null;
        }

        private static void DrawObjectReferenceField(
            Rect rect,
            SerializedProperty property,
            GUIContent label
        )
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, property, label, true);
            if (EditorGUI.EndChangeCheck())
            {
                if (property.serializedObject.ApplyModifiedProperties())
                {
                    property.serializedObject.Update();
                }
            }
        }

        private static InlineInspectorImGuiState GetOrCreateImGuiState(string sessionKey)
        {
            if (string.IsNullOrEmpty(sessionKey))
            {
                sessionKey = Guid.NewGuid().ToString();
            }

            if (!ImGuiStateCache.TryGetValue(sessionKey, out InlineInspectorImGuiState state))
            {
                state = new InlineInspectorImGuiState();
                ImGuiStateCache[sessionKey] = state;
            }

            return state;
        }

        private static bool TryGetImGuiState(string sessionKey, out InlineInspectorImGuiState state)
        {
            if (string.IsNullOrEmpty(sessionKey))
            {
                state = null;
                return false;
            }

            return ImGuiStateCache.TryGetValue(sessionKey, out state);
        }

        private static void UpdateImGuiStateTarget(
            InlineInspectorImGuiState state,
            UnityEngine.Object newTarget
        )
        {
            if (state == null)
            {
                return;
            }

            if (state.CurrentTarget == newTarget)
            {
                return;
            }

            state.DisposeEditor();
            state.CurrentTarget = newTarget;
            state.ErrorMessage = null;
            state.SerializedObject = null;

            if (newTarget == null)
            {
                return;
            }

            try
            {
                state.CachedEditor = Editor.CreateEditor(newTarget);
                state.SerializedObject =
                    state.CachedEditor != null ? state.CachedEditor.serializedObject : null;
            }
            catch (Exception ex)
            {
                state.ErrorMessage = ex.Message;
                state.CachedEditor = null;
                state.SerializedObject = null;
            }

            if (state.CachedEditor == null && state.ErrorMessage == null)
            {
                state.ErrorMessage = "Inspector unavailable.";
            }
        }

        private static void DrawInlineInspector(
            string sessionKey,
            Rect position,
            InlineInspectorImGuiState state,
            WInLineEditorAttribute settings,
            float preferredInlineWidth,
            bool widthWasClipped,
            bool hasReservedScrollbarSpace
        )
        {
            EventType currentEventType = Event.current?.type ?? EventType.Layout;
            bool isLayoutPass = currentEventType == EventType.Layout;
            bool canMutateInteractiveState = !isLayoutPass;
            bool canCommitMeasurements = currentEventType == EventType.Repaint;
            Rect previousInspectorRect =
                state.PreferredInspectorRect.width > 0.5f
                    ? state.PreferredInspectorRect
                    : state.LastInspectorRect;
            bool hadHorizontalScroll =
                state.HorizontalScrollOffset > 0.5f
                || state.UsedHorizontalScroll
                || (state.InspectorContentWidth - previousInspectorRect.width) > 0.5f
                || state.PendingHorizontalReset;
            bool hadVerticalScroll =
                state.ScrollPosition.y > 0.5f
                || state.UsedVerticalScroll
                || state.PendingVerticalReset;

            float resolvedViewWidth = ResolveViewWidth();
            Rect rawVisibleRect = ResolveVisibleRect();
            bool visibleRectValid = rawVisibleRect.width > 1f;
            float visibleRectWidth = visibleRectValid
                ? rawVisibleRect.width
                : (
                    state.LastVisibleRectWidth > 1f ? state.LastVisibleRectWidth : resolvedViewWidth
                );

            Rect inlineRect = position;
            if (canCommitMeasurements)
            {
                state.LastInlineRect = inlineRect;
                if (visibleRectValid)
                {
                    state.LastVisibleRectWidth = rawVisibleRect.width;
                }
            }
            DrawInlineBackground(inlineRect);

            GUI.BeginGroup(inlineRect);
            try
            {
                Rect contentRect = GetInlineContentRect(inlineRect);
                if (canCommitMeasurements)
                {
                    state.LastContentRect = contentRect;
                }
                float cursorY = contentRect.y;

                if (settings.drawHeader)
                {
                    Rect headerRect = new Rect(
                        contentRect.x,
                        cursorY,
                        contentRect.width,
                        EditorGUIUtility.singleLineHeight
                    );
                    DrawHeaderRow(headerRect, state);
                    cursorY += EditorGUIUtility.singleLineHeight + InlineHeaderSpacing;
                }

                Rect inspectorRect;
                if (isLayoutPass && previousInspectorRect.width > 0.5f)
                {
                    inspectorRect = new Rect(
                        previousInspectorRect.x,
                        cursorY,
                        previousInspectorRect.width,
                        settings.inspectorHeight
                    );
                }
                else
                {
                    inspectorRect = new Rect(
                        contentRect.x,
                        cursorY,
                        contentRect.width,
                        settings.inspectorHeight
                    );
                }
                if (canCommitMeasurements)
                {
                    state.LastInspectorRect = inspectorRect;
                    state.PreferredInspectorRect = inspectorRect;
                }
                cursorY += inspectorRect.height;

                bool hasInspector = state.CachedEditor != null;
                bool enableVerticalScroll = settings.enableScrolling && hasInspector;
                Rect verticalScrollbarRect = default;
                bool displayVerticalScroll = false;
                bool requiresVerticalScroll = false;
                bool hasVerticalOffset = state.ScrollPosition.y > 0.5f;
                float measuredContentHeight = inspectorRect.height;
                if (enableVerticalScroll)
                {
                    measuredContentHeight =
                        state.InspectorContentHeight > 0.5f
                            ? state.InspectorContentHeight
                            : inspectorRect.height;
                    float heightDeficit = measuredContentHeight - inspectorRect.height;
                    requiresVerticalScroll = heightDeficit > 0.5f;
                    displayVerticalScroll =
                        requiresVerticalScroll || hasVerticalOffset || hadVerticalScroll;
                    if (displayVerticalScroll)
                    {
                        inspectorRect.width = Mathf.Max(
                            0f,
                            inspectorRect.width - InlineVerticalScrollbarWidth
                        );
                        verticalScrollbarRect = new Rect(
                            inspectorRect.x + inspectorRect.width,
                            inspectorRect.y,
                            InlineVerticalScrollbarWidth,
                            inspectorRect.height
                        );
                    }
                }
                else
                {
                    displayVerticalScroll = false;
                    requiresVerticalScroll = false;
                }

                bool shouldResetVertical =
                    state.PendingVerticalReset && !displayVerticalScroll && !requiresVerticalScroll;
                if (shouldResetVertical && canCommitMeasurements)
                {
                    Vector2 resetScroll = state.ScrollPosition;
                    resetScroll.y = 0f;
                    state.ScrollPosition = resetScroll;
                    state.PendingVerticalReset = false;
                    hadVerticalScroll = false;
                }

                if (canCommitMeasurements)
                {
                    if (!enableVerticalScroll)
                    {
                        state.UsedVerticalScroll = false;
                        state.PendingVerticalReset = false;
                        Vector2 scrollPosition = state.ScrollPosition;
                        scrollPosition.y = 0f;
                        state.ScrollPosition = scrollPosition;
                    }
                    else
                    {
                        state.UsedVerticalScroll = requiresVerticalScroll || hadVerticalScroll;
                        if (!displayVerticalScroll)
                        {
                            if (hadVerticalScroll || state.PendingVerticalReset)
                            {
                                state.PendingVerticalReset = true;
                            }
                            else
                            {
                                Vector2 scrollPosition = state.ScrollPosition;
                                scrollPosition.y = 0f;
                                state.ScrollPosition = scrollPosition;
                            }
                        }
                        else
                        {
                            state.PendingVerticalReset = false;
                        }
                    }
                }

                float adjustedPreferredInlineWidth = preferredInlineWidth;
                if (displayVerticalScroll)
                {
                    adjustedPreferredInlineWidth = Mathf.Max(
                        0f,
                        adjustedPreferredInlineWidth - InlineVerticalScrollbarWidth
                    );
                }

                bool reserveHorizontalScrollbarSpace = hasReservedScrollbarSpace;
                float contentWidthPadding = 2f * (InlineBorderThickness + InlinePadding);
                float preferredContentWidth = inspectorRect.width;
                float minimumContentWidth = Mathf.Max(0f, settings.minInspectorWidth);
                if (minimumContentWidth > preferredContentWidth)
                {
                    preferredContentWidth = minimumContentWidth;
                }

                float expandedContentWidth = Mathf.Max(
                    0f,
                    adjustedPreferredInlineWidth - contentWidthPadding
                );
                if (expandedContentWidth > preferredContentWidth)
                {
                    preferredContentWidth = expandedContentWidth;
                }

                if (!widthWasClipped && preferredContentWidth <= inspectorRect.width + 0.5f)
                {
                    if (isLayoutPass && state.PreferredContentWidth > inspectorRect.width + 0.5f)
                    {
                        preferredContentWidth = state.PreferredContentWidth;
                    }
                    else
                    {
                        preferredContentWidth = inspectorRect.width;
                    }
                }

                if (hadHorizontalScroll && state.InspectorContentWidth > preferredContentWidth)
                {
                    preferredContentWidth = state.InspectorContentWidth;
                }

                float effectiveViewportWidth = Mathf.Max(0f, inspectorRect.width);
                float widthDeficit = preferredContentWidth - effectiveViewportWidth;
                bool requiresHorizontalScroll = hasInspector && widthDeficit > 0.5f;

                float releaseWidthThreshold = Mathf.Max(
                    0f,
                    settings.minInspectorWidth - InlinePreferredWidthReleaseTolerance
                );
                bool viewportExceedsMinWidth = inspectorRect.width >= releaseWidthThreshold;
                bool noActiveScrollState =
                    state.HorizontalScrollOffset <= 0.5f
                    && !state.UsedHorizontalScroll
                    && !state.PendingHorizontalReset;
                if (viewportExceedsMinWidth && noActiveScrollState)
                {
                    preferredContentWidth = inspectorRect.width;
                    state.PreferredContentWidth = inspectorRect.width;
                    state.InspectorContentWidth = inspectorRect.width;
                    widthDeficit = preferredContentWidth - effectiveViewportWidth;
                    requiresHorizontalScroll = hasInspector && widthDeficit > 0.5f;
                    if (!requiresHorizontalScroll)
                    {
                        hadHorizontalScroll = false;
                    }
                }

                bool canSnapPreferredWidth =
                    state.HorizontalScrollOffset <= 0.5f
                    && !state.UsedHorizontalScroll
                    && !state.PendingHorizontalReset
                    && preferredContentWidth > inspectorRect.width
                    && inspectorRect.width
                        >= (preferredContentWidth - InlinePreferredWidthReleaseTolerance);
                if (canSnapPreferredWidth)
                {
                    preferredContentWidth = inspectorRect.width;
                    state.PreferredContentWidth = inspectorRect.width;
                    state.InspectorContentWidth = inspectorRect.width;
                    widthDeficit = preferredContentWidth - effectiveViewportWidth;
                    requiresHorizontalScroll = hasInspector && widthDeficit > 0.5f;
                }

                bool canSnapWidth =
                    !hadHorizontalScroll
                    && state.HorizontalScrollOffset <= 0.5f
                    && !state.UsedHorizontalScroll
                    && !state.PendingHorizontalReset;
                if (canSnapWidth && preferredContentWidth > inspectorRect.width + 0.5f)
                {
                    preferredContentWidth = inspectorRect.width;
                    state.PreferredContentWidth = inspectorRect.width;
                    state.InspectorContentWidth = inspectorRect.width;
                    widthDeficit = preferredContentWidth - effectiveViewportWidth;
                    requiresHorizontalScroll = hasInspector && widthDeficit > 0.5f;
                }

                if (!requiresHorizontalScroll && hadHorizontalScroll)
                {
                    bool residualScrollState =
                        state.HorizontalScrollOffset > 0.5f
                        || state.UsedHorizontalScroll
                        || state.PendingHorizontalReset;
                    if (!residualScrollState)
                    {
                        float targetWidth = Mathf.Max(0f, inspectorRect.width);
                        state.PreferredContentWidth = targetWidth;
                        state.InspectorContentWidth = targetWidth;
                        preferredContentWidth = targetWidth;
                        state.HorizontalScrollOffset = 0f;
                        state.PendingHorizontalReset = false;
                        state.UsedHorizontalScroll = false;
                        state.ConsecutiveFitRepaints =
                            InlineHorizontalScrollbarReleaseRepaintThreshold;
                        hadHorizontalScroll = false;
                    }
                }

                if (requiresHorizontalScroll && reserveHorizontalScrollbarSpace)
                {
                    float stableWidth =
                        settings.minInspectorWidth + InlineHorizontalScrollHysteresis;
                    if (stableWidth > preferredContentWidth)
                    {
                        preferredContentWidth = stableWidth;
                    }
                }

                bool displayHorizontalScroll = requiresHorizontalScroll || hadHorizontalScroll;

                if (
                    !displayHorizontalScroll
                    && !requiresHorizontalScroll
                    && state.HorizontalScrollOffset <= 0.5f
                    && !state.PendingHorizontalReset
                    && state.PreferredContentWidth > inspectorRect.width + 1f
                    && canCommitMeasurements
                )
                {
                    float reducedWidth = Mathf.Lerp(
                        state.PreferredContentWidth,
                        inspectorRect.width,
                        0.35f
                    );
                    state.PreferredContentWidth = Mathf.Max(inspectorRect.width, reducedWidth);
                    state.InspectorContentWidth = state.PreferredContentWidth;
                }

                bool shouldResetHorizontal =
                    state.PendingHorizontalReset
                    && !displayHorizontalScroll
                    && !requiresHorizontalScroll;
                if (shouldResetHorizontal && canCommitMeasurements)
                {
                    state.HorizontalScrollOffset = 0f;
                    state.PendingHorizontalReset = false;
                    state.PreferredContentWidth = inspectorRect.width;
                    state.InspectorContentWidth = inspectorRect.width;
                    hadHorizontalScroll = false;
                }

                Rect scrollbarRect = default;
                if (reserveHorizontalScrollbarSpace)
                {
                    scrollbarRect = new Rect(
                        contentRect.x,
                        cursorY,
                        contentRect.width,
                        InlineHorizontalScrollbarHeight
                    );
                    cursorY += scrollbarRect.height;
                }

                float inspectorContentWidth;
                if (displayHorizontalScroll)
                {
                    inspectorContentWidth = Mathf.Max(
                        preferredContentWidth,
                        state.PreferredContentWidth > 0.5f
                            ? state.PreferredContentWidth
                            : state.InspectorContentWidth
                    );
                }
                else
                {
                    inspectorContentWidth = Mathf.Max(
                        inspectorRect.width,
                        state.PreferredContentWidth > 0.5f
                            ? state.PreferredContentWidth
                            : state.InspectorContentWidth
                    );
                }

                if (displayHorizontalScroll)
                {
                    if (canCommitMeasurements)
                    {
                        state.InspectorContentWidth = inspectorContentWidth;
                        state.PreferredContentWidth = inspectorContentWidth;
                        state.PendingHorizontalReset = false;
                    }
                }
                else
                {
                    if (hadHorizontalScroll)
                    {
                        inspectorContentWidth = Mathf.Max(
                            inspectorContentWidth,
                            state.PreferredContentWidth
                        );
                        if (canCommitMeasurements)
                        {
                            state.InspectorContentWidth = inspectorContentWidth;
                            state.PendingHorizontalReset = true;
                            state.ConsecutiveFitRepaints = 0;
                        }
                    }
                    else if (canCommitMeasurements)
                    {
                        state.InspectorContentWidth = inspectorContentWidth;
                        state.HorizontalScrollOffset = 0f;
                        state.PreferredContentWidth = inspectorRect.width;
                        state.PendingHorizontalReset = false;
                    }
                }

                if (canCommitMeasurements)
                {
                    state.UsedHorizontalScroll =
                        displayHorizontalScroll
                        && (requiresHorizontalScroll || hadHorizontalScroll);
                    bool inlineFitsWithoutScroll =
                        !displayHorizontalScroll
                        && !requiresHorizontalScroll
                        && state.HorizontalScrollOffset <= 0.5f
                        && !state.PendingHorizontalReset;
                    if (inlineFitsWithoutScroll)
                    {
                        state.ConsecutiveFitRepaints = Mathf.Min(
                            state.ConsecutiveFitRepaints + 1,
                            InlineHorizontalScrollbarReleaseRepaintThreshold
                        );
                    }
                    else
                    {
                        state.ConsecutiveFitRepaints = 0;
                    }
                }

                if (!hasInspector)
                {
                    DrawMessage(
                        inspectorRect,
                        string.IsNullOrEmpty(state.ErrorMessage)
                            ? "Inspector unavailable."
                            : state.ErrorMessage
                    );
                }
                else
                {
                    DrawInspectorContents(
                        inspectorRect,
                        state,
                        settings,
                        inspectorContentWidth,
                        displayHorizontalScroll,
                        scrollbarRect,
                        effectiveViewportWidth,
                        reserveHorizontalScrollbarSpace,
                        displayVerticalScroll,
                        verticalScrollbarRect,
                        canMutateInteractiveState
                    );
                }

                WInLineEditorDiagnostics.RecordLayoutSample(
                    sessionKey,
                    new WInLineEditorDiagnostics.InlineInspectorLayoutSample(
                        currentEventType,
                        inlineRect.width,
                        inspectorRect.width,
                        preferredContentWidth,
                        state.InspectorContentWidth,
                        effectiveViewportWidth,
                        resolvedViewWidth,
                        visibleRectWidth,
                        reserveHorizontalScrollbarSpace,
                        requiresHorizontalScroll,
                        displayHorizontalScroll,
                        state.HorizontalScrollOffset,
                        requiresVerticalScroll,
                        displayVerticalScroll,
                        state.ScrollPosition.y,
                        EditorGUI.indentLevel,
                        GroupGUIWidthUtility.CurrentLeftPadding,
                        GroupGUIWidthUtility.CurrentRightPadding,
                        preferredInlineWidth,
                        widthWasClipped,
                        state.ConsecutiveFitRepaints
                    )
                );

                if (
                    settings.drawPreview
                    && settings.previewHeight > 0f
                    && state.CachedEditor != null
                )
                {
                    cursorY += InlinePreviewSpacing;
                    Rect previewRect = new Rect(
                        contentRect.x,
                        cursorY,
                        contentRect.width,
                        settings.previewHeight
                    );
                    DrawPreview(previewRect, state);
                }
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private static void DrawHeaderRow(Rect area, InlineInspectorImGuiState state)
        {
            GUIContent content =
                state.CurrentTarget == null
                    ? GUIContent.none
                    : EditorGUIUtility.ObjectContent(
                        state.CurrentTarget,
                        state.CurrentTarget.GetType()
                    );
            Rect labelRect = new Rect(
                area.x,
                area.y,
                area.width - InlinePingButtonWidth - 4f,
                area.height
            );
            GUI.Label(labelRect, content, EditorStyles.boldLabel);

            Rect buttonRect = new Rect(
                area.x + area.width - InlinePingButtonWidth,
                area.y,
                InlinePingButtonWidth,
                area.height
            );
            using (new EditorGUI.DisabledScope(state.CurrentTarget == null))
            {
                if (GUI.Button(buttonRect, "Ping") && state.CurrentTarget != null)
                {
                    EditorGUIUtility.PingObject(state.CurrentTarget);
                    Selection.activeObject = state.CurrentTarget;
                }
            }
        }

        private static void DrawMessage(Rect rect, string message)
        {
            GUIStyle style = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
            };
            GUI.Label(rect, message, style);
        }

        private static void DrawInspectorContents(
            Rect rect,
            InlineInspectorImGuiState state,
            WInLineEditorAttribute settings,
            float contentWidth,
            bool displayHorizontalScroll,
            Rect horizontalScrollbarRect,
            float viewportWidth,
            bool hasReservedHorizontalScrollbarSpace,
            bool displayVerticalScroll,
            Rect verticalScrollbarRect,
            bool canMutateState
        )
        {
            float viewportHeight = rect.height;
            float effectiveViewportWidth =
                viewportWidth > 0f ? Mathf.Max(1f, viewportWidth) : Mathf.Max(1f, rect.width);
            state.EffectiveViewportWidth = effectiveViewportWidth;
            float maxHorizontalScroll = Mathf.Max(0f, contentWidth - effectiveViewportWidth);
            float clampedHorizontalOffset = Mathf.Clamp(
                state.HorizontalScrollOffset,
                0f,
                maxHorizontalScroll
            );
            if (canMutateState)
            {
                state.HorizontalScrollOffset = clampedHorizontalOffset;
            }

            float contentHeight =
                state.InspectorContentHeight > 0.5f ? state.InspectorContentHeight : viewportHeight;
            float maxVerticalScroll = Mathf.Max(0f, contentHeight - viewportHeight);
            float clampedVerticalOffset = Mathf.Clamp(
                state.ScrollPosition.y,
                0f,
                maxVerticalScroll
            );
            if (!displayVerticalScroll)
            {
                clampedVerticalOffset = 0f;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(contentWidth, effectiveViewportWidth),
                Mathf.Max(contentHeight, viewportHeight)
            );

            float horizontalScrollInput = displayHorizontalScroll ? clampedHorizontalOffset : 0f;
            float verticalScrollInput = displayVerticalScroll ? clampedVerticalOffset : 0f;
            Vector2 scrollInput = new Vector2(horizontalScrollInput, verticalScrollInput);
            Vector2 scrollOutput = GUI.BeginScrollView(
                rect,
                scrollInput,
                viewRect,
                GUIStyle.none,
                GUIStyle.none
            );
            try
            {
                GUILayout.BeginArea(viewRect);
                try
                {
                    DrawInspectorBody(viewRect, contentWidth, state, settings);
                }
                finally
                {
                    GUILayout.EndArea();
                }
            }
            finally
            {
                GUI.EndScrollView();
            }

            if (canMutateState)
            {
                float scrollViewHorizontalOffset = displayHorizontalScroll
                    ? Mathf.Clamp(scrollOutput.x, 0f, maxHorizontalScroll)
                    : 0f;
                state.HorizontalScrollOffset = scrollViewHorizontalOffset;

                float scrollViewVerticalOffset = displayVerticalScroll
                    ? Mathf.Clamp(scrollOutput.y, 0f, maxVerticalScroll)
                    : 0f;
                Vector2 updatedScroll = state.ScrollPosition;
                updatedScroll.x = 0f;
                updatedScroll.y = scrollViewVerticalOffset;
                state.ScrollPosition = updatedScroll;
            }

            if (displayHorizontalScroll && hasReservedHorizontalScrollbarSpace)
            {
                Rect resolvedHorizontalRect =
                    horizontalScrollbarRect.width > 0f && horizontalScrollbarRect.height > 0f
                        ? horizontalScrollbarRect
                        : new Rect(
                            rect.x,
                            rect.y + rect.height,
                            rect.width,
                            InlineHorizontalScrollbarHeight
                        );

                float scrollbarRangeMax = Mathf.Max(effectiveViewportWidth, contentWidth);
                float previousOffset = clampedHorizontalOffset;
                float newOffset = GUI.HorizontalScrollbar(
                    resolvedHorizontalRect,
                    previousOffset,
                    effectiveViewportWidth,
                    0f,
                    scrollbarRangeMax
                );

                if (canMutateState)
                {
                    float requiredContentWidth = newOffset + effectiveViewportWidth;
                    if (requiredContentWidth > contentWidth + 0.5f)
                    {
                        contentWidth = requiredContentWidth;
                        scrollbarRangeMax = Mathf.Max(scrollbarRangeMax, requiredContentWidth);
                        maxHorizontalScroll = Mathf.Max(0f, contentWidth - effectiveViewportWidth);
                        state.InspectorContentWidth = Mathf.Max(
                            state.InspectorContentWidth,
                            requiredContentWidth
                        );
                    }

                    state.HorizontalScrollOffset = Mathf.Clamp(newOffset, 0f, maxHorizontalScroll);
                }
            }

            if (displayVerticalScroll)
            {
                float newVerticalOffset = GUI.VerticalScrollbar(
                    verticalScrollbarRect,
                    clampedVerticalOffset,
                    viewportHeight,
                    0f,
                    Mathf.Max(viewportHeight, contentHeight)
                );
                if (canMutateState)
                {
                    Vector2 scrollVector = state.ScrollPosition;
                    scrollVector.y = Mathf.Clamp(newVerticalOffset, 0f, maxVerticalScroll);
                    state.ScrollPosition = scrollVector;
                }
            }
        }

        private static void DrawInspectorBody(
            Rect layoutRect,
            float contentWidth,
            InlineInspectorImGuiState state,
            WInLineEditorAttribute settings
        )
        {
            try
            {
                float targetWidth = Mathf.Max(1f, contentWidth);
                GUILayout.BeginHorizontal(GUILayout.Width(targetWidth));
                GUILayout.BeginVertical(GUILayout.Width(targetWidth));
                state.CachedEditor.OnInspectorGUI();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                Rect sentinel = GUILayoutUtility.GetRect(
                    0f,
                    0f,
                    GUILayout.ExpandWidth(true),
                    GUILayout.ExpandHeight(false)
                );
                if (Event.current.type == EventType.Repaint)
                {
                    state.InspectorContentHeight = Mathf.Max(0f, sentinel.y);
                }
            }
            catch (Exception ex)
            {
                DrawMessage(new Rect(0f, 0f, layoutRect.width, layoutRect.height), ex.Message);
            }
        }

        private static void DrawPreview(Rect rect, InlineInspectorImGuiState state)
        {
            if (!state.CachedEditor.HasPreviewGUI())
            {
                DrawMessage(rect, "Preview unavailable.");
                return;
            }

            state.CachedEditor.OnPreviewGUI(rect, GUIStyle.none);
        }

        private static void DrawInlineBackground(Rect rect)
        {
            Color backgroundColor = EditorGUIUtility.isProSkin
                ? DarkInlineBackground
                : LightInlineBackground;
            EditorGUI.DrawRect(rect, backgroundColor);
            DrawRectBorder(rect, InlineBorderColor, InlineBorderThickness);
        }

        private static Rect GetInlineContentRect(Rect containerRect)
        {
            float offset = InlineBorderThickness + InlinePadding;
            float width = Mathf.Max(0f, containerRect.width - (offset * 2f));
            float height = Mathf.Max(0f, containerRect.height - (offset * 2f));
            return new Rect(offset, offset, width, height);
        }

        private static Rect ExpandToViewWidth(Rect rect)
        {
            return ExpandToViewWidth(rect, out _, out _);
        }

        private static Rect ExpandToViewWidth(
            Rect rect,
            out float preferredWidth,
            out bool widthWasClipped
        )
        {
            Rect visibleRect = ApplyGroupPaddingToVisibleRect(ResolveVisibleRect());
            float viewWidth = ApplyGroupPaddingToViewWidth(ResolveViewWidth());
            float expandedWidth = CalculateExpandedInlineWidth(
                rect,
                viewWidth,
                visibleRect,
                out preferredWidth,
                out widthWasClipped
            );
            rect.width = expandedWidth;
            return rect;
        }

        private static float CalculateExpandedInlineWidth(
            Rect rect,
            float viewWidth,
            Rect visibleRect,
            out float preferredWidth,
            out bool widthWasClipped
        )
        {
            float visibleLeftOffset =
                visibleRect.width > 0f ? Mathf.Max(0f, rect.x - visibleRect.xMin) : 0f;
            bool hasActiveGroupPadding = GroupGUIWidthUtility.CurrentHorizontalPadding > 0f;

            float baseRight = Mathf.Max(
                rect.x + InlineGroupEdgePadding,
                viewWidth - InlineInspectorRightPadding - InlineGroupEdgePadding
            );
            float unclampedRight = baseRight;
            float adjustedRight = baseRight;
            float clippedRight = baseRight;
            bool clippedByVisibleBounds = false;

            if (visibleRect.width > 0f)
            {
                float clipRight = Mathf.Max(
                    rect.x + InlineGroupEdgePadding,
                    visibleRect.xMax - InlineGroupEdgePadding
                );
                clippedRight = Mathf.Min(adjustedRight, clipRight);
                clippedByVisibleBounds = unclampedRight - clippedRight > 0.5f;
                adjustedRight = clippedRight;

                if (!hasActiveGroupPadding && visibleLeftOffset > 0f)
                {
                    float nestingShrink = Mathf.Max(
                        InlineGroupNestingPadding,
                        visibleLeftOffset * 0.5f
                    );
                    adjustedRight = Mathf.Max(
                        rect.x + InlineGroupEdgePadding,
                        adjustedRight - nestingShrink
                    );
                }
            }

            float desiredWidth = Mathf.Max(0f, adjustedRight - rect.x);
            float expansionThreshold = Mathf.Max(24f, visibleLeftOffset * 0.5f);
            float resultWidth = rect.width;
            float gain = desiredWidth - resultWidth;
            if (gain > expansionThreshold)
            {
                resultWidth = desiredWidth;
            }

            preferredWidth = clippedByVisibleBounds
                ? Mathf.Max(0f, unclampedRight - rect.x)
                : resultWidth;
            widthWasClipped = clippedByVisibleBounds;

            return resultWidth;
        }

        internal static float CalculateExpandedInlineWidthForTesting(
            Rect rect,
            float viewWidth,
            Rect visibleRect,
            out float preferredWidth,
            out bool widthWasClipped
        )
        {
            return CalculateExpandedInlineWidth(
                rect,
                viewWidth,
                visibleRect,
                out preferredWidth,
                out widthWasClipped
            );
        }

        internal static Rect ExpandRectForTesting(
            Rect rect,
            out float preferredWidth,
            out bool widthWasClipped
        )
        {
            return ExpandToViewWidth(rect, out preferredWidth, out widthWasClipped);
        }

        internal static bool HasHorizontalScrollbarReservationForTesting(string sessionKey)
        {
            return !string.IsNullOrEmpty(sessionKey)
                && HorizontalScrollbarReservationKeys.Contains(sessionKey);
        }

        private static float ResolveViewWidth()
        {
            try
            {
                float resolved = s_ViewWidthResolver?.Invoke() ?? 0f;
                if (resolved > 0f)
                {
                    s_LastMeasuredViewWidth = resolved;
                }
                return Mathf.Max(0f, resolved);
            }
            catch
            {
                return Mathf.Max(0f, s_LastMeasuredViewWidth);
            }
        }

        private static Rect ResolveVisibleRect()
        {
            try
            {
                return s_VisibleRectResolver();
            }
            catch
            {
                return new Rect(0f, 0f, ResolveViewWidth(), 0f);
            }
        }

        private static Func<Rect> CreateVisibleRectResolver()
        {
            Type guiClipType = typeof(GUI).Assembly.GetType("UnityEngine.GUIClip");
            PropertyInfo visibleRectProperty =
                guiClipType?.GetProperty("visibleRect", BindingFlags.Public | BindingFlags.Static)
                ?? guiClipType?.GetProperty(
                    "visibleRect",
                    BindingFlags.NonPublic | BindingFlags.Static
                );

            if (visibleRectProperty == null)
            {
                return () => new Rect(0f, 0f, ResolveViewWidth(), 0f);
            }

            return () =>
            {
                try
                {
                    object value = visibleRectProperty.GetValue(null, null);
                    return value is Rect rect ? rect : new Rect(0f, 0f, ResolveViewWidth(), 0f);
                }
                catch
                {
                    return new Rect(0f, 0f, ResolveViewWidth(), 0f);
                }
            };
        }

        private static Rect ApplyGroupPaddingToVisibleRect(Rect visibleRect)
        {
            float leftPadding = GroupGUIWidthUtility.CurrentLeftPadding;
            float rightPadding = GroupGUIWidthUtility.CurrentRightPadding;
            if (leftPadding <= 0f && rightPadding <= 0f)
            {
                return visibleRect;
            }

            float xMin = visibleRect.xMin + leftPadding;
            float xMax = visibleRect.xMax - rightPadding;
            if (xMax < xMin)
            {
                float midpoint = (visibleRect.xMin + visibleRect.xMax) * 0.5f;
                xMin = midpoint;
                xMax = midpoint;
            }

            float width = Mathf.Max(0f, xMax - xMin);
            return new Rect(xMin, visibleRect.y, width, visibleRect.height);
        }

        private static float ApplyGroupPaddingToViewWidth(float viewWidth)
        {
            float leftPadding = GroupGUIWidthUtility.CurrentLeftPadding;
            float rightPadding = GroupGUIWidthUtility.CurrentRightPadding;
            float totalPadding = leftPadding + rightPadding;
            if (totalPadding <= 0f)
            {
                return viewWidth;
            }

            return Mathf.Max(0f, viewWidth - totalPadding);
        }

        private static void DrawRectBorder(Rect rect, Color color, float thickness)
        {
            if (thickness <= 0f)
            {
                return;
            }

            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color); // Top
            EditorGUI.DrawRect(
                new Rect(rect.x, rect.yMax - thickness, rect.width, thickness),
                color
            ); // Bottom
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color); // Left
            EditorGUI.DrawRect(
                new Rect(rect.xMax - thickness, rect.y, thickness, rect.height),
                color
            ); // Right
        }

        private static float CalculateInlineContainerHeight(WInLineEditorAttribute settings)
        {
            if (settings == null)
            {
                return 0f;
            }

            float height =
                (InlineBorderThickness * 2f) + (InlinePadding * 2f) + settings.inspectorHeight;

            if (settings.drawHeader)
            {
                height += EditorGUIUtility.singleLineHeight + InlineHeaderSpacing;
            }

            if (settings.drawPreview && settings.previewHeight > 0f)
            {
                height += InlinePreviewSpacing + settings.previewHeight;
            }

            return height;
        }

        private static bool ShouldReserveHorizontalScrollbarSpace(
            string sessionKey,
            WInLineEditorAttribute settings
        )
        {
            if (
                settings == null
                || settings.minInspectorWidth <= 0f
                || string.IsNullOrEmpty(sessionKey)
            )
            {
                return false;
            }

            bool hasReservation = HorizontalScrollbarReservationKeys.Contains(sessionKey);
            bool previousReservation = hasReservation;
            InlineInspectorImGuiState state = null;
            TryGetImGuiState(sessionKey, out state);
            bool hasActiveScroll = HasActiveHorizontalScrollState(sessionKey, state);
            if (hasActiveScroll)
            {
                if (!hasReservation)
                {
                    HorizontalScrollbarReservationKeys.Add(sessionKey);
                    hasReservation = true;
                }

                return true;
            }

            float availableWidth = EstimateInlineAvailableWidth(state);
            if (availableWidth <= 0f)
            {
                return hasReservation;
            }

            float padding = 2f * (InlineBorderThickness + InlinePadding);
            float contentWidth = Mathf.Max(0f, availableWidth - padding);
            bool needsReservation = contentWidth < settings.minInspectorWidth - 0.5f;
            bool awaitingStableFrames =
                state != null
                && state.ConsecutiveFitRepaints < InlineHorizontalScrollbarReleaseRepaintThreshold;
            bool canReleaseReservation =
                !awaitingStableFrames
                && contentWidth > (settings.minInspectorWidth + InlineHorizontalScrollHysteresis);

            if (hasReservation && canReleaseReservation)
            {
                HorizontalScrollbarReservationKeys.Remove(sessionKey);
                hasReservation = false;
            }
            else if (!hasReservation && needsReservation)
            {
                HorizontalScrollbarReservationKeys.Add(sessionKey);
                hasReservation = true;
            }

            WInLineEditorDiagnostics.RecordReservationSample(
                sessionKey,
                new WInLineEditorDiagnostics.InlineInspectorReservationSample(
                    availableWidth,
                    contentWidth,
                    settings.minInspectorWidth,
                    needsReservation,
                    awaitingStableFrames,
                    previousReservation,
                    hasReservation,
                    hasActiveScroll,
                    state?.ConsecutiveFitRepaints ?? 0
                )
            );

            return hasReservation;
        }

        private static float EstimateInlineAvailableWidth(InlineInspectorImGuiState state)
        {
            float viewWidth = ApplyGroupPaddingToViewWidth(ResolveViewWidth());
            if (viewWidth <= 0f)
            {
                return 0f;
            }

            Rect visibleRect = ApplyGroupPaddingToVisibleRect(ResolveVisibleRect());
            float effectiveVisibleWidth =
                visibleRect.width > 1f
                    ? visibleRect.width
                    : (state?.LastVisibleRectWidth > 1f ? state.LastVisibleRectWidth : viewWidth);

            float leftPadding = GroupGUIWidthUtility.CurrentLeftPadding;
            float targetLeft = InlineGroupEdgePadding + leftPadding;
            float targetRight = Mathf.Max(
                targetLeft,
                Mathf.Min(
                    viewWidth - InlineInspectorRightPadding - InlineGroupEdgePadding,
                    effectiveVisibleWidth - InlineGroupEdgePadding
                )
            );
            if (targetRight <= targetLeft)
            {
                return 0f;
            }

            float span = Mathf.Max(0f, targetRight - targetLeft);
            Rect baseRect = new Rect(targetLeft, 0f, span, 0f);
            Rect indentedRect = EditorGUI.IndentedRect(baseRect);
            return Mathf.Max(0f, indentedRect.width - InlineGroupEdgePadding);
        }

        private static void TrackHorizontalScrollbarReservation(string sessionKey, bool reserved)
        {
            if (string.IsNullOrEmpty(sessionKey))
            {
                return;
            }

            if (reserved)
            {
                HorizontalScrollbarReservationKeys.Add(sessionKey);
            }
            else
            {
                HorizontalScrollbarReservationKeys.Remove(sessionKey);
                if (
                    TryGetImGuiState(sessionKey, out InlineInspectorImGuiState releasedState)
                    && releasedState != null
                )
                {
                    releasedState.ConsecutiveFitRepaints =
                        InlineHorizontalScrollbarReleaseRepaintThreshold;
                }
            }
        }

        private static bool HasActiveHorizontalScrollState(
            string sessionKey,
            InlineInspectorImGuiState existingState = null
        )
        {
            if (string.IsNullOrEmpty(sessionKey))
            {
                return false;
            }

            InlineInspectorImGuiState state = existingState;
            if (state == null && !ImGuiStateCache.TryGetValue(sessionKey, out state))
            {
                return false;
            }

            float inspectorWidth =
                state.LastInspectorRect.width > 0.5f
                    ? state.LastInspectorRect.width
                    : state.InspectorContentWidth;
            bool awaitingStableFrames =
                state.ConsecutiveFitRepaints < InlineHorizontalScrollbarReleaseRepaintThreshold;

            return state.HorizontalScrollOffset > 0.5f
                || state.UsedHorizontalScroll
                || state.PendingHorizontalReset
                || (state.InspectorContentWidth - inspectorWidth) > 0.5f
                || (state.PreferredContentWidth - inspectorWidth) > 0.5f
                || awaitingStableFrames;
        }

        internal static float GetInlineContainerHeightForTesting(WInLineEditorAttribute settings)
        {
            return CalculateInlineContainerHeight(settings);
        }

        private static bool ShouldShowInlineInspector(
            SerializedProperty property,
            WInLineEditorAttribute settings
        )
        {
            if (property == null || property.objectReferenceValue == null || settings == null)
            {
                return false;
            }

            if (settings.mode == WInLineEditorMode.AlwaysExpanded)
            {
                return true;
            }

            string sessionKey = BuildSessionKey(property);
            bool defaultExpanded = settings.mode == WInLineEditorMode.FoldoutExpanded;

            if (string.IsNullOrEmpty(sessionKey))
            {
                return defaultExpanded;
            }

            return SessionState.GetBool(sessionKey, defaultExpanded);
        }

        private static string BuildSessionKey(SerializedProperty property)
        {
            if (property == null)
            {
                return string.Empty;
            }

            SerializedObject serializedObject = property.serializedObject;
            if (serializedObject == null || serializedObject.targetObject == null)
            {
                return string.Empty;
            }

            return $"WInLineEditor:{serializedObject.targetObject.GetInstanceID()}:{property.propertyPath}";
        }

        private static void ClearImGuiStateCache()
        {
            foreach (KeyValuePair<string, InlineInspectorImGuiState> entry in ImGuiStateCache)
            {
                InlineInspectorImGuiState state = entry.Value;
                state?.DisposeEditor();
            }

            ImGuiStateCache.Clear();
            HorizontalScrollbarReservationKeys.Clear();
            WInLineEditorDiagnostics.ClearAllDiagnostics();
        }

        internal static void ResetImGuiStateCacheForTesting()
        {
            ClearImGuiStateCache();
        }

        internal static void SetViewWidthResolver(Func<float> resolver)
        {
            s_ViewWidthResolver = resolver ?? (() => EditorGUIUtility.currentViewWidth);
            s_UsingCustomViewWidthResolver = resolver != null;
        }

        internal static void ResetViewWidthResolver()
        {
            s_ViewWidthResolver = () => EditorGUIUtility.currentViewWidth;
            s_LastMeasuredViewWidth = DefaultViewWidthFallback;
            s_UsingCustomViewWidthResolver = false;
        }

        internal static void SetVisibleRectResolver(Func<Rect> resolver)
        {
            s_VisibleRectResolver = resolver ?? DefaultVisibleRectResolver;
        }

        internal static void ResetVisibleRectResolver()
        {
            s_VisibleRectResolver = DefaultVisibleRectResolver;
        }

        internal static bool TryGetImGuiStateInfo(
            string sessionKey,
            out InlineInspectorImGuiStateInfo info
        )
        {
            if (ImGuiStateCache.TryGetValue(sessionKey, out InlineInspectorImGuiState state))
            {
                info = new InlineInspectorImGuiStateInfo(
                    state.CurrentTarget,
                    state.SerializedObject != null,
                    state.ErrorMessage,
                    state.LastInlineRect,
                    state.LastContentRect,
                    state.LastInspectorRect,
                    state.InspectorContentWidth,
                    state.EffectiveViewportWidth,
                    state.InspectorContentHeight,
                    state.UsedHorizontalScroll,
                    state.UsedVerticalScroll,
                    state.HorizontalScrollOffset,
                    state.ScrollPosition,
                    state.PreferredContentWidth,
                    state.PreferredInspectorRect
                );
                return true;
            }

            info = default;
            return false;
        }

        internal static bool SetScrollPositionForTesting(string sessionKey, Vector2 scrollPosition)
        {
            if (ImGuiStateCache.TryGetValue(sessionKey, out InlineInspectorImGuiState state))
            {
                state.ScrollPosition = scrollPosition;
                return true;
            }

            return false;
        }

        internal static bool SetHorizontalScrollOffsetForTesting(string sessionKey, float offset)
        {
            if (ImGuiStateCache.TryGetValue(sessionKey, out InlineInspectorImGuiState state))
            {
                state.HorizontalScrollOffset = offset;
                return true;
            }

            return false;
        }

        internal readonly struct InlineInspectorImGuiStateInfo
        {
            public InlineInspectorImGuiStateInfo(
                UnityEngine.Object target,
                bool hasSerializedObject,
                string errorMessage,
                Rect inlineRect,
                Rect contentRect,
                Rect inspectorRect,
                float inspectorContentWidth,
                float effectiveViewportWidth,
                float inspectorContentHeight,
                bool usesHorizontalScroll,
                bool usesVerticalScroll,
                float horizontalScrollOffset,
                Vector2 scrollPosition,
                float preferredContentWidth,
                Rect preferredInspectorRect
            )
            {
                Target = target;
                HasSerializedObject = hasSerializedObject;
                ErrorMessage = errorMessage;
                InlineRect = inlineRect;
                ContentRect = contentRect;
                InspectorRect = inspectorRect;
                InspectorContentWidth = inspectorContentWidth;
                EffectiveViewportWidth = effectiveViewportWidth;
                InspectorContentHeight = inspectorContentHeight;
                UsesHorizontalScroll = usesHorizontalScroll;
                UsesVerticalScroll = usesVerticalScroll;
                HorizontalScrollOffset = horizontalScrollOffset;
                ScrollPosition = scrollPosition;
                PreferredContentWidth = preferredContentWidth;
                PreferredInspectorRect = preferredInspectorRect;
            }

            public UnityEngine.Object Target { get; }

            public bool HasSerializedObject { get; }

            public string ErrorMessage { get; }

            public Rect InlineRect { get; }

            public Rect ContentRect { get; }

            public Rect InspectorRect { get; }

            public float InspectorContentWidth { get; }

            public float EffectiveViewportWidth { get; }

            public float InspectorContentHeight { get; }

            public bool UsesHorizontalScroll { get; }

            public bool UsesVerticalScroll { get; }

            public float HorizontalScrollOffset { get; }

            public Vector2 ScrollPosition { get; }

            public float PreferredContentWidth { get; }

            public Rect PreferredInspectorRect { get; }
        }

        private sealed class InlineInspectorImGuiState
        {
            public UnityEngine.Object CurrentTarget;
            public Editor CachedEditor;
            public SerializedObject SerializedObject;
            public Vector2 ScrollPosition;
            public float HorizontalScrollOffset;
            public bool UsedHorizontalScroll;
            public float InspectorContentWidth;
            public float EffectiveViewportWidth;
            public bool UsedVerticalScroll;
            public float InspectorContentHeight;
            public string ErrorMessage;
            public Rect LastInlineRect;
            public Rect LastContentRect;
            public Rect LastInspectorRect;
            public Rect PreferredInlineRect;
            public Rect PreferredInspectorRect;
            public float LastVisibleRectWidth;
            public float PreferredInlineWidthValue;
            public float PreferredContentWidth;
            public bool PendingHorizontalReset;
            public bool PendingVerticalReset;
            public bool InlineWidthWasClipped;
            public int ConsecutiveFitRepaints;

            public void DisposeEditor()
            {
                if (CachedEditor != null)
                {
                    UnityEngine.Object.DestroyImmediate(CachedEditor);
                    CachedEditor = null;
                }

                CurrentTarget = null;
                SerializedObject = null;
                ScrollPosition = Vector2.zero;
                HorizontalScrollOffset = 0f;
                UsedHorizontalScroll = false;
                InspectorContentWidth = 0f;
                EffectiveViewportWidth = 0f;
                UsedVerticalScroll = false;
                InspectorContentHeight = 0f;
                ErrorMessage = null;
                LastInlineRect = default;
                LastContentRect = default;
                LastInspectorRect = default;
                PreferredInlineRect = default;
                PreferredInspectorRect = default;
                PreferredInlineWidthValue = 0f;
                PreferredContentWidth = 0f;
                PendingHorizontalReset = false;
                PendingVerticalReset = false;
                InlineWidthWasClipped = false;
                ConsecutiveFitRepaints = 0;
            }
        }
    }
#endif
}

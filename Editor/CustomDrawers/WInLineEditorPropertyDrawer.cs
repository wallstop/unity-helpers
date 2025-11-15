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

    [CustomPropertyDrawer(typeof(WInLineEditorAttribute))]
    public sealed class WInLineEditorPropertyDrawer : PropertyDrawer
    {
        internal const float InlineBorderThickness = 1f;
        internal const float InlinePadding = 6f;
        private const float InlineHeaderSpacing = 2f;
        private const float InlinePreviewSpacing = 4f;
        private const float InlinePingButtonWidth = 58f;
        internal const float InlineInspectorRightPadding = 6f;
        private const float InlineGroupNestingPadding = 4f;
        private const float InlineGroupEdgePadding = 8f;
        internal const float InlineHorizontalScrollbarHeight = 12f;
        private const float InlineHorizontalScrollHysteresis = 12f;

        private static readonly Color LightInlineBackground = new(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color DarkInlineBackground = new(0.17f, 0.17f, 0.17f, 1f);
        private static readonly Color InlineBorderColor = new(0.25f, 0.25f, 0.25f, 1f);

        private static readonly Dictionary<string, InlineInspectorImGuiState> ImGuiStateCache =
            new();
        private static readonly HashSet<string> HorizontalScrollbarReservationKeys = new();
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
                DrawInlineInspector(
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
            Rect position,
            InlineInspectorImGuiState state,
            WInLineEditorAttribute settings,
            float preferredInlineWidth,
            bool widthWasClipped,
            bool hasReservedScrollbarSpace
        )
        {
            Rect inlineRect = position;
            state.LastInlineRect = inlineRect;
            DrawInlineBackground(inlineRect);

            GUI.BeginGroup(inlineRect);
            try
            {
                Rect contentRect = GetInlineContentRect(inlineRect);
                state.LastContentRect = contentRect;
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

                Rect inspectorRect = new Rect(
                    contentRect.x,
                    cursorY,
                    contentRect.width,
                    settings.inspectorHeight
                );
                state.LastInspectorRect = inspectorRect;
                cursorY += inspectorRect.height;

                Rect scrollbarRect = default;
                if (hasReservedScrollbarSpace)
                {
                    scrollbarRect = new Rect(
                        contentRect.x,
                        cursorY,
                        contentRect.width,
                        InlineHorizontalScrollbarHeight
                    );
                    cursorY += scrollbarRect.height;
                }

                bool hasInspector = state.CachedEditor != null;
                float contentWidthPadding = 2f * (InlineBorderThickness + InlinePadding);
                float preferredContentWidth = inspectorRect.width;
                float minimumContentWidth = Mathf.Max(0f, settings.minInspectorWidth);
                if (minimumContentWidth > preferredContentWidth)
                {
                    preferredContentWidth = minimumContentWidth;
                }

                float expandedContentWidth = preferredInlineWidth - contentWidthPadding;
                if (expandedContentWidth > preferredContentWidth)
                {
                    preferredContentWidth = expandedContentWidth;
                }

                if (hasReservedScrollbarSpace)
                {
                    float stableWidth =
                        settings.minInspectorWidth + InlineHorizontalScrollHysteresis;
                    if (stableWidth > preferredContentWidth)
                    {
                        preferredContentWidth = stableWidth;
                    }
                }

                float effectiveViewportWidth = Mathf.Max(0f, inspectorRect.width);
                float widthDeficit = preferredContentWidth - effectiveViewportWidth;
                bool needsHorizontalScroll =
                    (hasInspector && widthDeficit > 0.5f) || hasReservedScrollbarSpace;

                float inspectorContentWidth = needsHorizontalScroll
                    ? preferredContentWidth
                    : inspectorRect.width;
                state.InspectorContentWidth = inspectorContentWidth;
                state.UsedHorizontalScroll = needsHorizontalScroll;
                if (!needsHorizontalScroll)
                {
                    state.HorizontalScrollOffset = 0f;
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
                        needsHorizontalScroll,
                        scrollbarRect,
                        effectiveViewportWidth
                    );
                }

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
            bool needsHorizontalScroll,
            Rect scrollbarRect,
            float viewportWidth
        )
        {
            if (!needsHorizontalScroll)
            {
                GUILayout.BeginArea(rect);
                try
                {
                    DrawInspectorBody(rect, state, settings);
                }
                finally
                {
                    GUILayout.EndArea();
                }

                return;
            }

            float viewportHeight = rect.height;
            float effectiveViewportWidth =
                viewportWidth > 0f ? Mathf.Max(1f, viewportWidth) : Mathf.Max(1f, rect.width);
            float maxScroll = Mathf.Max(0f, contentWidth - effectiveViewportWidth);
            state.HorizontalScrollOffset = Mathf.Clamp(state.HorizontalScrollOffset, 0f, maxScroll);

            GUI.BeginGroup(rect);
            try
            {
                Rect contentRect = new Rect(
                    -state.HorizontalScrollOffset,
                    0f,
                    Mathf.Max(contentWidth, effectiveViewportWidth),
                    viewportHeight
                );
                GUI.BeginGroup(contentRect);
                try
                {
                    Rect areaRect = new Rect(0f, 0f, contentWidth, viewportHeight);
                    GUILayout.BeginArea(areaRect);
                    try
                    {
                        DrawInspectorBody(areaRect, state, settings);
                    }
                    finally
                    {
                        GUILayout.EndArea();
                    }
                }
                finally
                {
                    GUI.EndGroup();
                }
            }
            finally
            {
                GUI.EndGroup();
            }

            Rect resolvedScrollbarRect =
                scrollbarRect.width > 0f && scrollbarRect.height > 0f
                    ? scrollbarRect
                    : new Rect(
                        rect.x,
                        rect.y + rect.height - InlineHorizontalScrollbarHeight,
                        rect.width,
                        InlineHorizontalScrollbarHeight
                    );
            float newOffset = GUI.HorizontalScrollbar(
                resolvedScrollbarRect,
                state.HorizontalScrollOffset,
                effectiveViewportWidth,
                0f,
                Mathf.Max(effectiveViewportWidth, contentWidth)
            );
            state.HorizontalScrollOffset = Mathf.Clamp(newOffset, 0f, maxScroll);
        }

        private static void DrawInspectorBody(
            Rect layoutRect,
            InlineInspectorImGuiState state,
            WInLineEditorAttribute settings
        )
        {
            try
            {
                if (settings.enableScrolling)
                {
                    state.ScrollPosition = EditorGUILayout.BeginScrollView(
                        state.ScrollPosition,
                        GUILayout.Width(layoutRect.width),
                        GUILayout.ExpandHeight(true)
                    );
                    try
                    {
                        state.CachedEditor.OnInspectorGUI();
                    }
                    finally
                    {
                        EditorGUILayout.EndScrollView();
                    }
                }
                else
                {
                    state.CachedEditor.OnInspectorGUI();
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
            Rect visibleRect = ResolveVisibleRect();
            float visibleLeftOffset =
                visibleRect.width > 0f ? Mathf.Max(0f, rect.x - visibleRect.xMin) : 0f;

            float baseRight =
                ResolveViewWidth() - InlineInspectorRightPadding - InlineGroupEdgePadding;
            baseRight = Mathf.Max(rect.x + InlineGroupEdgePadding, baseRight);
            float unclampedRight = baseRight;
            float adjustedRight = baseRight;

            if (visibleRect.width > 0f)
            {
                float clipRight = Mathf.Max(
                    rect.x + InlineGroupEdgePadding,
                    visibleRect.xMax - InlineGroupEdgePadding
                );
                adjustedRight = Mathf.Min(adjustedRight, clipRight);

                if (visibleLeftOffset > 0f)
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
            float gain = desiredWidth - rect.width;
            if (gain > expansionThreshold)
            {
                rect.width = desiredWidth;
            }

            preferredWidth = Mathf.Max(0f, unclampedRight - rect.x);
            widthWasClipped = unclampedRight - adjustedRight > 0.5f;

            return rect;
        }

        private static float ResolveViewWidth()
        {
            try
            {
                return Mathf.Max(0f, s_ViewWidthResolver?.Invoke() ?? 0f);
            }
            catch
            {
                return Mathf.Max(0f, EditorGUIUtility.currentViewWidth);
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

            float availableWidth = EstimateInlineAvailableWidth();
            if (availableWidth <= 0f)
            {
                return hasReservation;
            }

            float padding = 2f * (InlineBorderThickness + InlinePadding);
            float contentWidth = Mathf.Max(0f, availableWidth - padding);
            bool needsReservation = contentWidth < settings.minInspectorWidth - 0.5f;
            bool canReleaseReservation =
                contentWidth > (settings.minInspectorWidth + InlineHorizontalScrollHysteresis);

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

            return hasReservation;
        }

        private static float EstimateInlineAvailableWidth()
        {
            float viewWidth = ResolveViewWidth();
            if (viewWidth <= 0f)
            {
                return 0f;
            }

            float reducedWidth =
                viewWidth - InlineInspectorRightPadding - (InlineGroupEdgePadding * 2f);
            Rect baseRect = new Rect(InlineGroupEdgePadding, 0f, Mathf.Max(0f, reducedWidth), 0f);
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
            }
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
        }

        internal static void ResetImGuiStateCacheForTesting()
        {
            ClearImGuiStateCache();
        }

        internal static void SetViewWidthResolver(Func<float> resolver)
        {
            s_ViewWidthResolver = resolver ?? (() => EditorGUIUtility.currentViewWidth);
        }

        internal static void ResetViewWidthResolver()
        {
            s_ViewWidthResolver = () => EditorGUIUtility.currentViewWidth;
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
                    state.UsedHorizontalScroll,
                    state.HorizontalScrollOffset,
                    state.ScrollPosition
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
                bool usesHorizontalScroll,
                float horizontalScrollOffset,
                Vector2 scrollPosition
            )
            {
                Target = target;
                HasSerializedObject = hasSerializedObject;
                ErrorMessage = errorMessage;
                InlineRect = inlineRect;
                ContentRect = contentRect;
                InspectorRect = inspectorRect;
                InspectorContentWidth = inspectorContentWidth;
                UsesHorizontalScroll = usesHorizontalScroll;
                HorizontalScrollOffset = horizontalScrollOffset;
                ScrollPosition = scrollPosition;
            }

            public UnityEngine.Object Target { get; }

            public bool HasSerializedObject { get; }

            public string ErrorMessage { get; }

            public Rect InlineRect { get; }

            public Rect ContentRect { get; }

            public Rect InspectorRect { get; }

            public float InspectorContentWidth { get; }

            public bool UsesHorizontalScroll { get; }

            public float HorizontalScrollOffset { get; }

            public Vector2 ScrollPosition { get; }
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
            public string ErrorMessage;
            public Rect LastInlineRect;
            public Rect LastContentRect;
            public Rect LastInspectorRect;

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
                ErrorMessage = null;
                LastInlineRect = default;
                LastContentRect = default;
                LastInspectorRect = default;
            }
        }
    }
#endif
}

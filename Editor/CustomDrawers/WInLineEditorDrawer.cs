// Portions of this file are adapted from Unity Editor Toolbox (InlineEditorAttributeDrawer)
// Copyright (c) 2017-2023 arimger
// Licensed under the MIT License: https://github.com/arimger/Unity-Editor-Toolbox/blob/main/LICENSE.md

namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    [CustomPropertyDrawer(typeof(WInLineEditorAttribute))]
    /// <summary>
    /// Portions of this implementation draw inspiration from Unity Editor Toolbox's Inline Editor drawer.
    /// Unity Editor Toolbox is Copyright (c) 2017-2023 arimger and distributed under the MIT License.
    /// Source: https://github.com/arimger/Unity-Editor-Toolbox (MIT)
    /// </summary>
    public sealed class WInLineEditorDrawer : PropertyDrawer
    {
        // Inspired by the Unity Editor Toolbox inline editor drawer (MIT):
        // https://github.com/arimger/Unity-Editor-Toolbox
        private const float HeaderHeight = 20f;
        private const float Spacing = 2f;
        private const float MinimumFoldoutLabelWidth = 40f;
        private const float PingButtonPadding = 6f;
        private const float ContentPadding = 2f;
        private const float FoldoutOffset = 5f;

        private static readonly Dictionary<string, bool> FoldoutStates = new Dictionary<
            string,
            bool
        >(System.StringComparer.Ordinal);
        private static readonly Dictionary<string, Vector2> ScrollPositions = new Dictionary<
            string,
            Vector2
        >(System.StringComparer.Ordinal);
        private static readonly Dictionary<int, Editor> EditorCache = new Dictionary<int, Editor>();
        private static readonly Dictionary<string, float> PropertyWidths = new Dictionary<
            string,
            float
        >(System.StringComparer.Ordinal);
        private static readonly GUIContent PingButtonContent = new GUIContent(
            "Ping",
            "Ping object in the Project window"
        );
        private const string ScriptPropertyPath = "m_Script";

        private static float GetPingButtonWidth()
        {
            GUIStyle style = EditorStyles.miniButton;
            if (style == null)
            {
                return 0f;
            }

            Vector2 contentSize = style.CalcSize(PingButtonContent);
            return Mathf.Ceil(contentSize.x + PingButtonPadding);
        }

        private static float GetHorizontalScrollbarHeight()
        {
            GUIStyle scrollbarStyle = GUI.skin != null ? GUI.skin.horizontalScrollbar : null;
            float height =
                scrollbarStyle != null && scrollbarStyle.fixedHeight > 0f
                    ? scrollbarStyle.fixedHeight
                    : EditorGUIUtility.singleLineHeight;
            return Mathf.Max(12f, height);
        }

        private static void SetPropertyWidth(SerializedProperty property, float width)
        {
            if (property == null)
            {
                return;
            }

            string key = BuildFoldoutKey(property);
            PropertyWidths[key] = Mathf.Max(0f, width);
        }

        private static float GetEstimatedPropertyWidth(SerializedProperty property)
        {
            if (property == null)
            {
                return EditorGUIUtility.currentViewWidth;
            }

            string key = BuildFoldoutKey(property);
            if (PropertyWidths.TryGetValue(key, out float width) && width > 0f)
            {
                return width;
            }

            return EditorGUIUtility.currentViewWidth;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            WInLineEditorAttribute inlineAttribute = (WInLineEditorAttribute)attribute;
            float height = inlineAttribute.DrawObjectField
                ? EditorGUI.GetPropertyHeight(property, label, false)
                : EditorGUIUtility.singleLineHeight;

            Object value = property.hasMultipleDifferentValues
                ? null
                : property.objectReferenceValue;
            if (value == null || property.propertyType != SerializedPropertyType.ObjectReference)
            {
                return height;
            }

            float availableWidth = GetEstimatedPropertyWidth(property);
            float inlineHeight = CalculateInlineHeight(
                property,
                inlineAttribute,
                value,
                availableWidth
            );
            return height
                + (
                    inlineHeight <= 0f
                        ? 0f
                        : EditorGUIUtility.standardVerticalSpacing + inlineHeight
                );
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            WInLineEditorAttribute inlineAttribute = (WInLineEditorAttribute)attribute;
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.HelpBox(
                    position,
                    "WInLineEditor only supports object references.",
                    MessageType.Warning
                );
                EditorGUI.EndProperty();
                return;
            }

            if (property.hasMultipleDifferentValues)
            {
                if (inlineAttribute.DrawObjectField)
                {
                    EditorGUI.PropertyField(position, property, label, false);
                }
                else
                {
                    EditorGUI.LabelField(position, label);
                }

                EditorGUI.EndProperty();
                return;
            }

            SetPropertyWidth(property, position.width);

            float fieldHeight = inlineAttribute.DrawObjectField
                ? EditorGUI.GetPropertyHeight(property, label, false)
                : EditorGUIUtility.singleLineHeight;
            Rect currentRect = new Rect(position.x, position.y, position.width, fieldHeight);

            WInLineEditorMode mode = ResolveMode(inlineAttribute);
            string foldoutKey = BuildFoldoutKey(property);
            bool foldoutState = GetFoldoutState(property, inlineAttribute, mode);

            if (inlineAttribute.DrawObjectField)
            {
                foldoutState = DrawInlineObjectReferenceField(
                    currentRect,
                    property,
                    label,
                    foldoutState,
                    foldoutKey,
                    mode
                );
            }
            else
            {
                EditorGUI.LabelField(currentRect, label);
            }

            currentRect.y += currentRect.height + EditorGUIUtility.standardVerticalSpacing;

            Object value = property.objectReferenceValue;
            if (value != null)
            {
                float inlineHeight = CalculateInlineHeight(
                    property,
                    inlineAttribute,
                    value,
                    currentRect.width
                );
                if (inlineHeight > 0f)
                {
                    InspectorHeightInfo inspectorHeightInfo = ResolveInspectorHeightInfo(
                        value,
                        inlineAttribute,
                        currentRect.width
                    );
                    Rect inlineRect = new Rect(
                        currentRect.x,
                        currentRect.y,
                        currentRect.width,
                        inlineHeight
                    );
                    DrawInlineInspector(
                        inlineRect,
                        property,
                        inlineAttribute,
                        label,
                        value,
                        foldoutState,
                        foldoutKey,
                        mode,
                        inspectorHeightInfo
                    );
                }
            }

            EditorGUI.EndProperty();
        }

        private static float CalculateInlineHeight(
            SerializedProperty property,
            WInLineEditorAttribute inlineAttribute,
            Object value,
            float availableWidth
        )
        {
            WInLineEditorMode mode = ResolveMode(inlineAttribute);
            bool useStandaloneHeader = ShouldDrawStandaloneHeader(inlineAttribute);
            bool showHeader =
                useStandaloneHeader
                && (inlineAttribute.DrawHeader || mode != WInLineEditorMode.AlwaysExpanded);
            bool foldoutState = GetFoldoutState(property, inlineAttribute, mode);
            bool showBody = mode == WInLineEditorMode.AlwaysExpanded || foldoutState;

            float height = 0f;
            if (showHeader)
            {
                height += HeaderHeight + Spacing;
            }

            if (!showBody)
            {
                return height;
            }

            InspectorHeightInfo inspectorHeight = ResolveInspectorHeightInfo(
                value,
                inlineAttribute,
                availableWidth
            );
            height += inspectorHeight.DisplayHeight;
            if (inlineAttribute.DrawPreview)
            {
                height += Spacing + inlineAttribute.PreviewHeight;
            }

            return height;
        }

        private static bool DrawInlineObjectReferenceField(
            Rect rect,
            SerializedProperty property,
            GUIContent label,
            bool foldoutState,
            string foldoutKey,
            WInLineEditorMode mode
        )
        {
            Rect indentedRect = EditorGUI.IndentedRect(rect);
            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float labelWidth = Mathf.Min(EditorGUIUtility.labelWidth, indentedRect.width);
            Rect labelRect = new Rect(
                indentedRect.x,
                indentedRect.y,
                labelWidth,
                indentedRect.height
            );
            Rect fieldRect = new Rect(
                labelRect.xMax,
                indentedRect.y,
                Mathf.Max(0f, indentedRect.width - labelWidth),
                indentedRect.height
            );

            EditorGUI.ObjectField(fieldRect, property, GUIContent.none);
            Object currentValue = property.objectReferenceValue;

            bool showFoldoutToggle =
                currentValue != null && mode != WInLineEditorMode.AlwaysExpanded;
            bool showPingButton = currentValue != null;
            float pingWidth = showPingButton ? GetPingButtonWidth() : 0f;
            float pingSpacing = showPingButton ? Spacing : 0f;
            bool hasSpaceForPing =
                showPingButton
                && labelRect.width - pingWidth - pingSpacing >= MinimumFoldoutLabelWidth;
            if (!hasSpaceForPing)
            {
                showPingButton = false;
                pingWidth = 0f;
                pingSpacing = 0f;
            }

            float foldoutWidth = Mathf.Max(
                0f,
                showPingButton ? labelRect.width - pingWidth - pingSpacing : labelRect.width
            );
            Rect foldoutRect = new Rect(labelRect.x, labelRect.y, foldoutWidth, labelRect.height);

            GUIContent foldoutLabel = label ?? GUIContent.none;
            if (showFoldoutToggle)
            {
                Rect adjustedFoldoutRect = new Rect(
                    foldoutRect.x + FoldoutOffset,
                    foldoutRect.y,
                    Mathf.Max(0f, foldoutRect.width - FoldoutOffset),
                    foldoutRect.height
                );
                bool newState = EditorGUI.Foldout(
                    adjustedFoldoutRect,
                    foldoutState,
                    foldoutLabel,
                    true
                );
                if (newState != foldoutState)
                {
                    foldoutState = newState;
                    SetFoldoutState(foldoutKey, foldoutState);
                }
            }
            else
            {
                EditorGUI.LabelField(foldoutRect, foldoutLabel);
            }

            if (showPingButton)
            {
                Rect pingRect = new Rect(
                    foldoutRect.x + foldoutRect.width + pingSpacing,
                    labelRect.y,
                    pingWidth,
                    labelRect.height
                );
                using (new EditorGUI.DisabledScope(currentValue == null))
                {
                    if (GUI.Button(pingRect, PingButtonContent, EditorStyles.miniButton))
                    {
                        EditorGUIUtility.PingObject(currentValue);
                    }
                }
            }

            EditorGUI.indentLevel = previousIndent;
            return foldoutState;
        }

        private static void DrawInlineInspector(
            Rect rect,
            SerializedProperty property,
            WInLineEditorAttribute inlineAttribute,
            GUIContent label,
            Object value,
            bool foldoutState,
            string foldoutKey,
            WInLineEditorMode mode,
            InspectorHeightInfo inspectorHeight
        )
        {
            bool useStandaloneHeader = ShouldDrawStandaloneHeader(inlineAttribute);
            bool showHeader =
                useStandaloneHeader
                && (inlineAttribute.DrawHeader || mode != WInLineEditorMode.AlwaysExpanded);

            if (showHeader)
            {
                Rect headerRect = new Rect(rect.x, rect.y, rect.width, HeaderHeight);
                bool showFoldoutToggle = mode != WInLineEditorMode.AlwaysExpanded;
                foldoutState = DrawHeader(
                    headerRect,
                    property,
                    value,
                    label,
                    showFoldoutToggle,
                    foldoutState
                );
                SetFoldoutState(foldoutKey, foldoutState);
                rect.y += HeaderHeight + Spacing;
                rect.height -= HeaderHeight + Spacing;
            }

            if (!(mode == WInLineEditorMode.AlwaysExpanded || foldoutState))
            {
                return;
            }

            Editor editor = GetOrCreateEditor(value);
            if (editor == null)
            {
                return;
            }

            Rect inspectorRect = new Rect(
                rect.x,
                rect.y,
                rect.width,
                inspectorHeight.DisplayHeight
            );
            DrawInspectorBody(property, inspectorRect, editor, inlineAttribute, inspectorHeight);
            rect.y += inspectorHeight.DisplayHeight;

            if (inlineAttribute.DrawPreview && editor.HasPreviewGUI())
            {
                rect.y += Spacing;
                Rect previewRect = new Rect(
                    rect.x,
                    rect.y,
                    rect.width,
                    inlineAttribute.PreviewHeight
                );
                GUI.Box(previewRect, GUIContent.none, EditorStyles.helpBox);
                Rect previewContentRect = new Rect(
                    previewRect.x + 2f,
                    previewRect.y + 2f,
                    previewRect.width - 4f,
                    previewRect.height - 4f
                );
                editor.OnPreviewGUI(previewContentRect, GUIStyle.none);
            }
        }

        private static void DrawInspectorBody(
            SerializedProperty property,
            Rect rect,
            Editor editor,
            WInLineEditorAttribute inlineAttribute,
            InspectorHeightInfo inspectorHeight
        )
        {
            Rect backgroundRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            GUI.Box(backgroundRect, GUIContent.none, EditorStyles.helpBox);
            Rect contentRect = new Rect(
                backgroundRect.x + ContentPadding,
                backgroundRect.y,
                backgroundRect.width - (ContentPadding * 2f),
                backgroundRect.height
            );

            string scrollKey = BuildScrollKey(property);
            bool useSerializedInspector = inspectorHeight.UsesSerializedInspector;
            bool needsHorizontalScroll =
                inlineAttribute.EnableScrolling
                && inlineAttribute.MinInspectorWidth > 0f
                && inlineAttribute.MinInspectorWidth - contentRect.width > 0.5f;
            bool needsVerticalScroll =
                inlineAttribute.EnableScrolling
                && inspectorHeight.ContentHeight > contentRect.height + 0.5f;
            bool useScrollView =
                inlineAttribute.EnableScrolling && (needsHorizontalScroll || needsVerticalScroll);

            if (useScrollView)
            {
                Vector2 scrollPosition = GetScrollPosition(scrollKey);
                float viewWidth = needsHorizontalScroll
                    ? Mathf.Max(inlineAttribute.MinInspectorWidth, contentRect.width)
                    : contentRect.width;
                float viewHeight = inspectorHeight.ContentHeight;

                GUI.BeginGroup(contentRect);
                Rect scrollViewRect = new Rect(0f, 0f, contentRect.width, contentRect.height);
                Rect viewRect = new Rect(0f, 0f, viewWidth, viewHeight);
                scrollPosition = GUI.BeginScrollView(
                    scrollViewRect,
                    scrollPosition,
                    viewRect,
                    needsHorizontalScroll,
                    needsVerticalScroll
                );
                DrawInspectorContents(editor, useSerializedInspector, viewRect);
                GUI.EndScrollView();
                GUI.EndGroup();

                ScrollPositions[scrollKey] = scrollPosition;
                return;
            }

            GUI.BeginGroup(contentRect);
            Rect drawRect = new Rect(0f, 0f, contentRect.width, inspectorHeight.ContentHeight);
            DrawInspectorContents(editor, useSerializedInspector, drawRect);
            GUI.EndGroup();
        }

        private static bool ShouldDrawStandaloneHeader(WInLineEditorAttribute inlineAttribute)
        {
            return !inlineAttribute.DrawObjectField;
        }

        private static void DrawInspectorContents(
            Editor editor,
            bool useSerializedInspector,
            Rect rect
        )
        {
            if (editor == null)
            {
                return;
            }

            if (useSerializedInspector)
            {
                DrawSerializedInspector(rect, editor);
                return;
            }

            GUILayout.BeginArea(rect);
            editor.OnInspectorGUI();
            GUILayout.EndArea();
        }

        private static void DrawSerializedInspector(Rect rect, Editor editor)
        {
            SerializedObject serializedObject = editor.serializedObject;
            DrawSerializedObject(rect, serializedObject);
        }

        private static void DrawSerializedObject(Rect rect, SerializedObject serializedObject)
        {
            if (serializedObject == null)
            {
                return;
            }

            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            Rect currentRect = new Rect(rect.x, rect.y, rect.width, 0f);
            bool firstPropertyDrawn = false;
            while (iterator.NextVisible(enterChildren))
            {
                if (iterator.propertyPath == ScriptPropertyPath)
                {
                    enterChildren = false;
                    continue;
                }

                if (firstPropertyDrawn)
                {
                    currentRect.y += EditorGUIUtility.standardVerticalSpacing;
                }

                float propertyHeight = EditorGUI.GetPropertyHeight(iterator, true);
                currentRect.height = propertyHeight;
                EditorGUI.PropertyField(currentRect, iterator, true);
                currentRect.y += propertyHeight;
                enterChildren = false;
                firstPropertyDrawn = true;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static InspectorHeightInfo ResolveInspectorHeightInfo(
            Object value,
            WInLineEditorAttribute inlineAttribute,
            float availableWidth
        )
        {
            if (value == null)
            {
                return InspectorHeightInfo.Empty;
            }

            Editor editor = GetOrCreateEditor(value);
            if (TryCalculateDefaultInspectorContentHeight(value, editor, out float contentHeight))
            {
                InspectorHeightInfo info = BuildInspectorHeightInfo(
                    inlineAttribute,
                    availableWidth,
                    contentHeight,
                    true
                );
                return info;
            }

            float fallbackHeight = inlineAttribute.InspectorHeight;
            return BuildInspectorHeightInfo(inlineAttribute, availableWidth, fallbackHeight, false);
        }

        private static InspectorHeightInfo BuildInspectorHeightInfo(
            WInLineEditorAttribute inlineAttribute,
            float availableWidth,
            float contentHeight,
            bool usesSerializedInspector
        )
        {
            float displayHeight = inlineAttribute.EnableScrolling
                ? Mathf.Min(contentHeight, inlineAttribute.InspectorHeight)
                : contentHeight;

            float effectiveWidth = Mathf.Max(0f, availableWidth - (ContentPadding * 2f));
            bool requiresHorizontalScroll =
                inlineAttribute.EnableScrolling
                && inlineAttribute.MinInspectorWidth > 0f
                && inlineAttribute.MinInspectorWidth - effectiveWidth > 0.5f;
            float horizontalScrollbarHeight = requiresHorizontalScroll
                ? GetHorizontalScrollbarHeight()
                : 0f;

            float finalDisplayHeight = displayHeight + horizontalScrollbarHeight;
            return new InspectorHeightInfo(
                contentHeight,
                finalDisplayHeight,
                usesSerializedInspector,
                horizontalScrollbarHeight
            );
        }

        private static bool TryCalculateDefaultInspectorContentHeight(
            Object value,
            Editor editor,
            out float contentHeight
        )
        {
            contentHeight = 0f;
            if (!CanUseSerializedInspector(value, editor))
            {
                return false;
            }

            SerializedObject serializedObject =
                editor != null ? editor.serializedObject : new SerializedObject(value);
            contentHeight = CalculateSerializedInspectorHeight(serializedObject);
            return true;
        }

        private static bool CanUseSerializedInspector(Object value, Editor editor)
        {
            if (value == null)
            {
                return false;
            }

            bool supportedTarget = value is ScriptableObject || value is MonoBehaviour;
            if (!supportedTarget)
            {
                return false;
            }

            if (editor == null)
            {
                return true;
            }

            return editor.GetType() == typeof(Editor);
        }

        private static float CalculateSerializedInspectorHeight(SerializedObject serializedObject)
        {
            if (serializedObject == null)
            {
                return 0f;
            }

            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            float height = 0f;
            bool firstPropertyMeasured = false;
            while (iterator.NextVisible(enterChildren))
            {
                if (iterator.propertyPath == ScriptPropertyPath)
                {
                    enterChildren = false;
                    continue;
                }

                if (firstPropertyMeasured)
                {
                    height += EditorGUIUtility.standardVerticalSpacing;
                }

                height += EditorGUI.GetPropertyHeight(iterator, true);
                enterChildren = false;
                firstPropertyMeasured = true;
            }

            return Mathf.Max(0f, height);
        }

        private readonly struct InspectorHeightInfo
        {
            public InspectorHeightInfo(
                float contentHeight,
                float displayHeight,
                bool usesSerializedInspector,
                float horizontalScrollbarHeight
            )
            {
                ContentHeight = Mathf.Max(0f, contentHeight);
                DisplayHeight = Mathf.Max(0f, displayHeight);
                UsesSerializedInspector = usesSerializedInspector;
                HorizontalScrollbarHeight = Mathf.Max(0f, horizontalScrollbarHeight);
            }

            public float ContentHeight { get; }
            public float DisplayHeight { get; }
            public bool UsesSerializedInspector { get; }
            public float HorizontalScrollbarHeight { get; }

            public static InspectorHeightInfo Empty => new InspectorHeightInfo(0f, 0f, false, 0f);
        }

        private static bool DrawHeader(
            Rect rect,
            SerializedProperty property,
            Object value,
            GUIContent label,
            bool showFoldoutToggle,
            bool foldoutState
        )
        {
            float pingWidth = GetPingButtonWidth();
            const float HeaderPingSpacing = 4f;
            bool showPingButton =
                value != null
                && rect.width - pingWidth - HeaderPingSpacing >= MinimumFoldoutLabelWidth;
            float labelWidth = showPingButton
                ? Mathf.Max(0f, rect.width - pingWidth - HeaderPingSpacing)
                : rect.width;
            Rect labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
            Rect pingRect = new Rect(
                rect.x + labelWidth + (showPingButton ? HeaderPingSpacing : 0f),
                rect.y,
                pingWidth,
                rect.height
            );

            GUIContent headerContent = EditorGUIUtility.ObjectContent(value, value.GetType());
            if (headerContent == null || string.IsNullOrEmpty(headerContent.text))
            {
                headerContent = new GUIContent(value.name);
            }

            if (!string.IsNullOrEmpty(label?.text))
            {
                headerContent.text = $"{label.text} ({headerContent.text})";
            }

            if (showFoldoutToggle)
            {
                bool newState = EditorGUI.Foldout(labelRect, foldoutState, headerContent, true);
                if (newState != foldoutState)
                {
                    foldoutState = newState;
                }
            }
            else
            {
                EditorGUI.LabelField(labelRect, headerContent, EditorStyles.boldLabel);
            }

            if (showPingButton)
            {
                using (new EditorGUI.DisabledScope(value == null))
                {
                    if (GUI.Button(pingRect, PingButtonContent, EditorStyles.miniButton))
                    {
                        EditorGUIUtility.PingObject(value);
                    }
                }
            }

            return foldoutState;
        }

        private static string BuildFoldoutKey(SerializedProperty property)
        {
            Object target =
                property.serializedObject != null ? property.serializedObject.targetObject : null;
            int id = target != null ? target.GetInstanceID() : 0;
            return $"{id}::{property.propertyPath}";
        }

        private static string BuildScrollKey(SerializedProperty property)
        {
            Object target =
                property.serializedObject != null ? property.serializedObject.targetObject : null;
            int id = target != null ? target.GetInstanceID() : 0;
            return $"scroll::{id}::{property.propertyPath}";
        }

        private static bool GetFoldoutState(
            SerializedProperty property,
            WInLineEditorAttribute inlineAttribute,
            WInLineEditorMode resolvedMode
        )
        {
            string key = BuildFoldoutKey(property);
            if (FoldoutStates.TryGetValue(key, out bool value))
            {
                return value;
            }

            bool initialState = resolvedMode switch
            {
                WInLineEditorMode.AlwaysExpanded => true,
                WInLineEditorMode.FoldoutExpanded => true,
                WInLineEditorMode.FoldoutCollapsed => false,
                _ => true,
            };
            FoldoutStates[key] = initialState;
            return initialState;
        }

        private static void SetFoldoutState(string key, bool value)
        {
            FoldoutStates[key] = value;
        }

        private static Vector2 GetScrollPosition(string key)
        {
            return ScrollPositions.TryGetValue(key, out Vector2 position) ? position : Vector2.zero;
        }

        private static Editor GetOrCreateEditor(Object value)
        {
            if (value == null)
            {
                return null;
            }

            int key = value.GetInstanceID();
            if (!EditorCache.TryGetValue(key, out Editor cachedEditor) || cachedEditor == null)
            {
                Editor.CreateCachedEditor(value, null, ref cachedEditor);
                EditorCache[key] = cachedEditor;
            }

            return cachedEditor;
        }

        private static WInLineEditorMode ResolveMode(WInLineEditorAttribute inlineAttribute)
        {
            if (inlineAttribute.Mode != WInLineEditorMode.UseSettings)
            {
                return inlineAttribute.Mode;
            }

            UnityHelpersSettings.InlineEditorFoldoutBehavior behavior =
                UnityHelpersSettings.GetInlineEditorFoldoutBehavior();
            return behavior switch
            {
                UnityHelpersSettings.InlineEditorFoldoutBehavior.AlwaysOpen =>
                    WInLineEditorMode.AlwaysExpanded,
                UnityHelpersSettings.InlineEditorFoldoutBehavior.StartCollapsed =>
                    WInLineEditorMode.FoldoutCollapsed,
                _ => WInLineEditorMode.FoldoutExpanded,
            };
        }

        internal static void ClearCachedStateForTesting()
        {
            FoldoutStates.Clear();
            ScrollPositions.Clear();
            PropertyWidths.Clear();
            foreach (Editor cachedEditor in EditorCache.Values)
            {
                if (cachedEditor != null)
                {
                    Object.DestroyImmediate(cachedEditor);
                }
            }

            EditorCache.Clear();
        }

        internal static void SetInlineFoldoutStateForTesting(
            SerializedProperty property,
            bool expanded
        )
        {
            if (property == null)
            {
                return;
            }

            string key = BuildFoldoutKey(property);
            FoldoutStates[key] = expanded;
        }
    }
}

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
        private const float PingButtonWidth = 20f;
        private const float Spacing = 2f;
        private const float ScrollbarThickness = 15f;

        private static readonly Dictionary<string, bool> FoldoutStates = new Dictionary<
            string,
            bool
        >(System.StringComparer.Ordinal);
        private static readonly Dictionary<string, Vector2> ScrollPositions = new Dictionary<
            string,
            Vector2
        >(System.StringComparer.Ordinal);
        private static readonly Dictionary<int, Editor> EditorCache = new Dictionary<int, Editor>();

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

            float inlineHeight = CalculateInlineHeight(property, inlineAttribute);
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

            float fieldHeight = inlineAttribute.DrawObjectField
                ? EditorGUI.GetPropertyHeight(property, label, false)
                : EditorGUIUtility.singleLineHeight;
            Rect currentRect = new Rect(position.x, position.y, position.width, fieldHeight);
            if (inlineAttribute.DrawObjectField)
            {
                EditorGUI.PropertyField(currentRect, property, label, false);
                currentRect.y += currentRect.height + EditorGUIUtility.standardVerticalSpacing;
            }
            else
            {
                EditorGUI.LabelField(currentRect, label);
                currentRect.y += currentRect.height + EditorGUIUtility.standardVerticalSpacing;
            }

            Object value = property.objectReferenceValue;
            if (value != null)
            {
                float inlineHeight = CalculateInlineHeight(property, inlineAttribute);
                if (inlineHeight > 0f)
                {
                    Rect inlineRect = new Rect(
                        currentRect.x,
                        currentRect.y,
                        currentRect.width,
                        inlineHeight
                    );
                    DrawInlineInspector(inlineRect, property, inlineAttribute, label, value);
                }
            }

            EditorGUI.EndProperty();
        }

        private static float CalculateInlineHeight(
            SerializedProperty property,
            WInLineEditorAttribute inlineAttribute
        )
        {
            WInLineEditorMode mode = ResolveMode(inlineAttribute);
            bool showHeader =
                inlineAttribute.DrawHeader || mode != WInLineEditorMode.AlwaysExpanded;
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

            height += inlineAttribute.InspectorHeight;
            if (inlineAttribute.DrawPreview)
            {
                height += Spacing + inlineAttribute.PreviewHeight;
            }

            return height;
        }

        private static void DrawInlineInspector(
            Rect rect,
            SerializedProperty property,
            WInLineEditorAttribute inlineAttribute,
            GUIContent label,
            Object value
        )
        {
            WInLineEditorMode mode = ResolveMode(inlineAttribute);
            string foldoutKey = BuildFoldoutKey(property);
            bool showHeader =
                inlineAttribute.DrawHeader || mode != WInLineEditorMode.AlwaysExpanded;
            bool foldoutState = GetFoldoutState(property, inlineAttribute, mode);

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
                inlineAttribute.InspectorHeight
            );
            DrawInspectorBody(property, inspectorRect, editor, inlineAttribute);
            rect.y += inlineAttribute.InspectorHeight;

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
            WInLineEditorAttribute inlineAttribute
        )
        {
            Rect backgroundRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            GUI.Box(backgroundRect, GUIContent.none, EditorStyles.helpBox);
            Rect contentRect = new Rect(
                backgroundRect.x + 2f,
                backgroundRect.y + 2f,
                backgroundRect.width - 4f,
                backgroundRect.height - 4f
            );

            string scrollKey = BuildScrollKey(property);
            if (inlineAttribute.EnableScrolling)
            {
                Vector2 scrollPosition = GetScrollPosition(scrollKey);
                float minWidth =
                    inlineAttribute.MinInspectorWidth <= 0f
                        ? contentRect.width
                        : Mathf.Max(
                            inlineAttribute.MinInspectorWidth,
                            contentRect.width - ScrollbarThickness
                        );
                Rect scrollViewRect = new Rect(0f, 0f, contentRect.width, contentRect.height);
                Rect viewRect = new Rect(0f, 0f, minWidth, inlineAttribute.InspectorHeight);

                GUI.BeginGroup(contentRect);
                scrollPosition = GUI.BeginScrollView(
                    new Rect(0f, 0f, scrollViewRect.width, scrollViewRect.height),
                    scrollPosition,
                    viewRect
                );
                GUILayout.BeginArea(new Rect(0f, 0f, viewRect.width, viewRect.height));
                editor.OnInspectorGUI();
                GUILayout.EndArea();
                GUI.EndScrollView();
                GUI.EndGroup();

                ScrollPositions[scrollKey] = scrollPosition;
            }
            else
            {
                GUI.BeginGroup(contentRect);
                GUILayout.BeginArea(
                    new Rect(0f, 0f, contentRect.width, inlineAttribute.InspectorHeight)
                );
                editor.OnInspectorGUI();
                GUILayout.EndArea();
                GUI.EndGroup();
            }
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
            Rect pingRect = new Rect(
                rect.xMax - PingButtonWidth,
                rect.y,
                PingButtonWidth,
                rect.height
            );
            Rect labelRect = new Rect(
                rect.x,
                rect.y,
                rect.width - PingButtonWidth - 4f,
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

            GUIContent pingContent = new GUIContent("⧉", "Ping object in the Project window");
            using (new EditorGUI.DisabledScope(value == null))
            {
                if (GUI.Button(pingRect, pingContent, EditorStyles.miniButton))
                {
                    EditorGUIUtility.PingObject(value);
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

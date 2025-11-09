namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [CustomPropertyDrawer(typeof(WInLineEditorAttribute))]
    public sealed class WInLineEditorPropertyDrawer : PropertyDrawer
    {
        private const float ContainerPadding = 6f;
        private const float HeaderButtonWidth = 48f;
        private const float MinimumInspectorHeight = 32f;
        private const float ScrollbarWidth = 16f;

        private static readonly Dictionary<string, InlineEditorCache> CacheByProperty = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            WInLineEditorAttribute inlineAttribute = attribute as WInLineEditorAttribute;
            if (inlineAttribute == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            InlineEditorCache cache = GetCache(property);
            EnsureFoldoutInitialization(property, inlineAttribute, cache);

            float height = inlineAttribute.drawObjectField
                ? EditorGUI.GetPropertyHeight(property, label, includeChildren: false)
                : EditorGUIUtility.singleLineHeight;

            float spacing = EditorGUIUtility.standardVerticalSpacing;

            UnityEngine.Object reference = property.objectReferenceValue;
            EnsureEditor(cache, reference, inlineAttribute);

            bool drawInline = ShouldDrawInline(property, inlineAttribute, cache);
            if (drawInline)
            {
                if (height > 0f)
                {
                    height += spacing;
                }

                float inlineHeight = CalculateInlineHeight(inlineAttribute, cache);
                height += inlineHeight;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.HelpBox(
                    position,
                    "WInLineEditor can only be used on object reference fields.",
                    MessageType.Warning
                );
                return;
            }

            WInLineEditorAttribute inlineAttribute = attribute as WInLineEditorAttribute;
            if (inlineAttribute == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            InlineEditorCache cache = GetCache(property);
            EnsureFoldoutInitialization(property, inlineAttribute, cache);

            float fieldHeight = inlineAttribute.drawObjectField
                ? EditorGUI.GetPropertyHeight(property, label, includeChildren: false)
                : EditorGUIUtility.singleLineHeight;

            Rect fieldRect = new Rect(position.x, position.y, position.width, fieldHeight);

            EditorGUI.BeginProperty(position, label, property);

            DrawObjectField(fieldRect, property, label, inlineAttribute);

            UnityEngine.Object reference = property.objectReferenceValue;
            EnsureEditor(cache, reference, inlineAttribute);

            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float currentY = fieldRect.yMax;

            bool drawInline = ShouldDrawInline(property, inlineAttribute, cache);
            if (drawInline)
            {
                currentY += spacing;
                float inlineHeight = CalculateInlineHeight(inlineAttribute, cache);
                Rect inlineRect = new Rect(position.x, currentY, position.width, inlineHeight);
                DrawInlineArea(inlineRect, property, inlineAttribute, cache, reference);
            }

            EditorGUI.EndProperty();
        }

        private static InlineEditorCache GetCache(SerializedProperty property)
        {
            string key = GetPropertyKey(property);
            InlineEditorCache cache;
            if (!CacheByProperty.TryGetValue(key, out cache))
            {
                cache = new InlineEditorCache();
                CacheByProperty[key] = cache;
            }
            return cache;
        }

        private static string GetPropertyKey(SerializedProperty property)
        {
            UnityEngine.Object target =
                property.serializedObject != null ? property.serializedObject.targetObject : null;
            int targetId = target != null ? target.GetInstanceID() : 0;
            return targetId.ToString() + ":" + property.propertyPath;
        }

        private static void EnsureFoldoutInitialization(
            SerializedProperty property,
            WInLineEditorAttribute inlineAttribute,
            InlineEditorCache cache
        )
        {
            if (inlineAttribute.mode == WInLineEditorMode.AlwaysExpanded)
            {
                property.isExpanded = true;
                cache.foldoutInitialized = true;
                return;
            }

            if (cache.foldoutInitialized)
            {
                return;
            }

            property.isExpanded = inlineAttribute.mode == WInLineEditorMode.FoldoutExpanded;
            cache.foldoutInitialized = true;
        }

        private static bool ShouldDrawInline(
            SerializedProperty property,
            WInLineEditorAttribute inlineAttribute,
            InlineEditorCache cache
        )
        {
            if (property.objectReferenceValue == null)
            {
                cache.Dispose();
                return false;
            }

            if (inlineAttribute.mode == WInLineEditorMode.AlwaysExpanded)
            {
                return true;
            }

            return property.isExpanded;
        }

        private static float CalculateInlineHeight(
            WInLineEditorAttribute inlineAttribute,
            InlineEditorCache cache
        )
        {
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            bool hasHeader = inlineAttribute.drawHeader;
            bool hasPreview = inlineAttribute.drawPreview && cache.hasPreview;
            bool hasInspector = inlineAttribute.inspectorHeight > 0f;

            float height = ContainerPadding * 2f;

            if (hasHeader)
            {
                height += EditorGUIUtility.singleLineHeight;
            }

            if (hasInspector)
            {
                height += inlineAttribute.inspectorHeight;
            }

            if (hasPreview)
            {
                height += inlineAttribute.previewHeight;
            }

            int breakCount = 0;
            if (hasHeader && (hasInspector || hasPreview))
            {
                breakCount++;
            }
            if (hasInspector && hasPreview)
            {
                breakCount++;
            }

            height += breakCount * spacing;
            return height;
        }

        private static void DrawObjectField(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            WInLineEditorAttribute inlineAttribute
        )
        {
            if (!inlineAttribute.drawObjectField)
            {
                DrawHeaderFoldout(position, property, label, inlineAttribute);
                return;
            }

            if (inlineAttribute.mode == WInLineEditorMode.AlwaysExpanded)
            {
                EditorGUI.PropertyField(position, property, label, includeChildren: false);
                return;
            }

            int originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float labelWidth = EditorGUIUtility.labelWidth;
            Rect foldoutRect = new Rect(
                position.x,
                position.y,
                labelWidth,
                EditorGUIUtility.singleLineHeight
            );
            bool expanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
            if (expanded != property.isExpanded)
            {
                property.isExpanded = expanded;
            }

            Rect objectFieldRect = new Rect(
                position.x + labelWidth,
                position.y,
                position.width - labelWidth,
                position.height
            );
            EditorGUI.PropertyField(
                objectFieldRect,
                property,
                GUIContent.none,
                includeChildren: false
            );

            EditorGUI.indentLevel = originalIndent;
        }

        private static void DrawHeaderFoldout(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            WInLineEditorAttribute inlineAttribute
        )
        {
            if (inlineAttribute.mode == WInLineEditorMode.AlwaysExpanded)
            {
                string displayName =
                    property.objectReferenceValue != null
                        ? property.objectReferenceValue.name
                        : "None";
                EditorGUI.LabelField(position, label, new GUIContent(displayName));
                return;
            }

            bool expanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
            if (expanded != property.isExpanded)
            {
                property.isExpanded = expanded;
            }
        }

        private static void DrawInlineArea(
            Rect position,
            SerializedProperty property,
            WInLineEditorAttribute inlineAttribute,
            InlineEditorCache cache,
            UnityEngine.Object reference
        )
        {
            GUI.Box(position, GUIContent.none, EditorStyles.helpBox);

            Rect contentRect = new Rect(
                position.x + ContainerPadding,
                position.y + ContainerPadding,
                position.width - (ContainerPadding * 2f),
                position.height - (ContainerPadding * 2f)
            );

            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float currentY = contentRect.y;

            bool hasHeader = inlineAttribute.drawHeader;
            bool hasPreview = inlineAttribute.drawPreview && cache.hasPreview;
            bool hasInspector = inlineAttribute.inspectorHeight > 0f;

            if (hasHeader)
            {
                Rect headerRect = new Rect(
                    contentRect.x,
                    currentY,
                    contentRect.width,
                    EditorGUIUtility.singleLineHeight
                );
                DrawInlineHeader(headerRect, property, reference);
                currentY += EditorGUIUtility.singleLineHeight;
            }

            if (hasHeader && (hasInspector || hasPreview))
            {
                currentY += spacing;
            }

            if (hasInspector)
            {
                Rect inspectorRect = new Rect(
                    contentRect.x,
                    currentY,
                    contentRect.width,
                    inlineAttribute.inspectorHeight
                );
                DrawInspector(inspectorRect, cache, inlineAttribute);
                currentY += inlineAttribute.inspectorHeight;
            }

            if (hasInspector && hasPreview)
            {
                currentY += spacing;
            }

            if (hasPreview)
            {
                Rect previewRect = new Rect(
                    contentRect.x,
                    currentY,
                    contentRect.width,
                    inlineAttribute.previewHeight
                );
                DrawPreview(previewRect, cache);
            }
        }

        private static void DrawInlineHeader(
            Rect position,
            SerializedProperty property,
            UnityEngine.Object reference
        )
        {
            GUIContent displayContent =
                reference != null ? new GUIContent(reference.name) : new GUIContent("None");

            if (reference == null)
            {
                EditorGUI.LabelField(position, displayContent, EditorStyles.boldLabel);
                return;
            }

            float labelWidth = position.width - HeaderButtonWidth - 2f;
            if (labelWidth < 0f)
            {
                labelWidth = 0f;
            }

            Rect labelRect = new Rect(position.x, position.y, labelWidth, position.height);
            EditorGUI.LabelField(labelRect, displayContent, EditorStyles.boldLabel);

            Rect buttonRect = new Rect(
                position.x + labelWidth + 2f,
                position.y,
                HeaderButtonWidth,
                position.height
            );

            if (GUI.Button(buttonRect, "Ping"))
            {
                EditorGUIUtility.PingObject(reference);
                Selection.activeObject = reference;
            }
        }

        private static void DrawInspector(
            Rect position,
            InlineEditorCache cache,
            WInLineEditorAttribute inlineAttribute
        )
        {
            if (cache.cachedEditor == null)
            {
                EditorGUI.LabelField(position, "Inspector unavailable.");
                return;
            }

            float effectiveHeight =
                cache.inspectorContentHeight > 0f
                    ? cache.inspectorContentHeight
                    : inlineAttribute.inspectorHeight;

            if (!inlineAttribute.enableScrolling)
            {
                GUILayout.BeginArea(position);
                EditorGUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins);
                cache.cachedEditor.OnInspectorGUI();
                EditorGUILayout.EndVertical();
                GUILayout.EndArea();
                return;
            }

            float viewWidth = position.width - ScrollbarWidth;
            if (viewWidth < 0f)
            {
                viewWidth = position.width;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                viewWidth,
                Mathf.Max(effectiveHeight, MinimumInspectorHeight)
            );

            cache.scrollPosition = GUI.BeginScrollView(position, cache.scrollPosition, viewRect);

            Rect areaRect = new Rect(0f, 0f, viewRect.width, viewRect.height);

            GUILayout.BeginArea(areaRect);
            EditorGUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins);
            cache.cachedEditor.OnInspectorGUI();
            EditorGUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                float calculatedHeight = lastRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                if (calculatedHeight > 0f)
                {
                    cache.inspectorContentHeight = calculatedHeight;
                }
            }
            GUILayout.EndArea();

            GUI.EndScrollView();
        }

        private static void DrawPreview(Rect position, InlineEditorCache cache)
        {
            if (cache.cachedEditor == null || !cache.hasPreview)
            {
                EditorGUI.LabelField(position, "Preview unavailable.");
                return;
            }

            cache.cachedEditor.OnPreviewGUI(position, GUIStyle.none);
        }

        private static void EnsureEditor(
            InlineEditorCache cache,
            UnityEngine.Object reference,
            WInLineEditorAttribute inlineAttribute
        )
        {
            if (reference == null)
            {
                cache.Dispose();
                return;
            }

            int instanceId = reference.GetInstanceID();
            if (cache.cachedEditor == null || cache.cachedTargetId != instanceId)
            {
                cache.Dispose();
                cache.cachedEditor = Editor.CreateEditor(reference);
                cache.cachedTargetId = instanceId;
                cache.inspectorContentHeight = Mathf.Max(
                    cache.inspectorContentHeight,
                    Mathf.Max(inlineAttribute.inspectorHeight, MinimumInspectorHeight)
                );
            }

            cache.hasPreview = cache.cachedEditor != null && cache.cachedEditor.HasPreviewGUI();
        }

        private sealed class InlineEditorCache
        {
            public Editor cachedEditor;
            public int cachedTargetId;
            public Vector2 scrollPosition;
            public float inspectorContentHeight;
            public bool hasPreview;
            public bool foldoutInitialized;

            public void Dispose()
            {
                if (cachedEditor != null)
                {
                    Object.DestroyImmediate(cachedEditor);
                    cachedEditor = null;
                }

                cachedTargetId = 0;
                scrollPosition = Vector2.zero;
                inspectorContentHeight = 0f;
                hasPreview = false;
                foldoutInitialized = false;
            }
        }
    }
#endif
}

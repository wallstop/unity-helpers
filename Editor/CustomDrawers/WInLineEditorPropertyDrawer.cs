namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
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

        private static readonly Color LightInlineBackground = new(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color DarkInlineBackground = new(0.17f, 0.17f, 0.17f, 1f);
        private static readonly Color InlineBorderColor = new(0.25f, 0.25f, 0.25f, 1f);

        private static readonly Dictionary<string, InlineInspectorImGuiState> ImGuiStateCache =
            new();

        static WInLineEditorPropertyDrawer()
        {
            AssemblyReloadEvents.beforeAssemblyReload += ClearImGuiStateCache;
            EditorApplication.quitting += ClearImGuiStateCache;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property == null)
            {
                return new PropertyField(null);
            }

            if (
                !TryResolveSettings(
                    property,
                    out WInLineEditorAttribute inlineAttribute,
                    out FieldInfo resolvedFieldInfo
                )
            )
            {
                return new PropertyField(property);
            }

            return new InlineInspectorElement(property, inlineAttribute, resolvedFieldInfo);
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
                    Rect foldoutRect = new(position.x, cursorY, width, lineHeight);
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
                        Rect objectRect = new(position.x, cursorY, width, objectFieldHeight);
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
                        Rect objectRect = new(position.x, cursorY, width, objectFieldHeight);
                        DrawObjectReferenceField(objectRect, property, label);
                        cursorY += objectFieldHeight;
                    }
                    else
                    {
                        Rect labelRect = new(position.x, cursorY, width, lineHeight);
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
                    Rect objectRect = new(position.x, cursorY, width, objectFieldHeight);
                    DrawObjectReferenceField(objectRect, property, label);
                    cursorY += objectFieldHeight;
                }
                else
                {
                    Rect labelRect = new(position.x, cursorY, width, lineHeight);
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
                float inlineHeight = CalculateInlineContainerHeight(inlineAttribute);
                cursorY += verticalSpacing;
                Rect inlineRect = new(position.x, cursorY, width, inlineHeight);
                DrawInlineInspector(inlineRect, state, inlineAttribute);
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

            if (ShouldShowInlineInspector(property, inlineAttribute))
            {
                height += verticalSpacing;
                height += CalculateInlineContainerHeight(inlineAttribute);
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

            if (newTarget == null)
            {
                return;
            }

            try
            {
                state.CachedEditor = Editor.CreateEditor(newTarget);
            }
            catch (Exception ex)
            {
                state.ErrorMessage = ex.Message;
                state.CachedEditor = null;
            }

            if (state.CachedEditor == null && state.ErrorMessage == null)
            {
                state.ErrorMessage = "Inspector unavailable.";
            }
        }

        private static void DrawInlineInspector(
            Rect position,
            InlineInspectorImGuiState state,
            WInLineEditorAttribute settings
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
                cursorY += settings.inspectorHeight;

                if (state.CachedEditor == null)
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
                    DrawInspectorContents(inspectorRect, state, settings);
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
            WInLineEditorAttribute settings
        )
        {
            GUILayout.BeginArea(rect);
            try
            {
                if (settings.enableScrolling)
                {
                    state.ScrollPosition = EditorGUILayout.BeginScrollView(
                        state.ScrollPosition,
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
                DrawMessage(new Rect(0f, 0f, rect.width, rect.height), ex.Message);
            }
            finally
            {
                GUILayout.EndArea();
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
        }

        internal static void ResetImGuiStateCacheForTesting()
        {
            ClearImGuiStateCache();
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
                    state.CachedEditor != null,
                    state.ErrorMessage,
                    state.LastInlineRect,
                    state.LastContentRect,
                    state.LastInspectorRect
                );
                return true;
            }

            info = default;
            return false;
        }

        internal readonly struct InlineInspectorImGuiStateInfo
        {
            public InlineInspectorImGuiStateInfo(
                UnityEngine.Object target,
                bool hasEditor,
                string errorMessage,
                Rect inlineRect,
                Rect contentRect,
                Rect inspectorRect
            )
            {
                Target = target;
                HasEditor = hasEditor;
                ErrorMessage = errorMessage;
                InlineRect = inlineRect;
                ContentRect = contentRect;
                InspectorRect = inspectorRect;
            }

            public UnityEngine.Object Target { get; }

            public bool HasEditor { get; }

            public string ErrorMessage { get; }

            public Rect InlineRect { get; }

            public Rect ContentRect { get; }

            public Rect InspectorRect { get; }
        }

        private sealed class InlineInspectorImGuiState
        {
            public UnityEngine.Object CurrentTarget;
            public Editor CachedEditor;
            public Vector2 ScrollPosition;
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
                ScrollPosition = Vector2.zero;
                ErrorMessage = null;
                LastInlineRect = default;
                LastContentRect = default;
                LastInspectorRect = default;
            }
        }

        private sealed class InlineInspectorElement : VisualElement
        {
            private const float FoldoutIndent = 2.5f;
            private const float HeaderIndent = 2.5f;

            private readonly SerializedObject _serializedObject;
            private readonly string _propertyPath;
            private readonly WInLineEditorAttribute _settings;
            private readonly Type _referenceType;
            private readonly bool _allowSceneObjects;

            private readonly Foldout _foldout;
            private readonly VisualElement _headerRow;
            private readonly Label _headerLabel;
            private readonly Button _pingButton;
            private readonly VisualElement _inspectorRoot;
            private readonly ScrollView _scrollView;

            private Editor _cachedEditor;
            private UnityEngine.Object _currentTarget;
            private bool _lastFoldoutExpanded;

            public InlineInspectorElement(
                SerializedProperty property,
                WInLineEditorAttribute inlineAttribute,
                FieldInfo drawerField
            )
            {
                _serializedObject = property.serializedObject;
                _propertyPath = property.propertyPath;
                _settings = inlineAttribute;
                _referenceType = ResolveObjectReferenceType(drawerField);
                _allowSceneObjects = ShouldAllowSceneObjects(_referenceType);
                string sessionKey = BuildSessionKey(property);

                style.flexDirection = FlexDirection.Column;

                ObjectField objectField = _settings.drawObjectField ? CreateObjectField() : null;

                VisualElement inlineParent = this;
                if (_settings.mode == WInLineEditorMode.AlwaysExpanded)
                {
                    _foldout = null;
                    if (objectField != null)
                    {
                        objectField.label = property.displayName;
                        objectField.style.marginBottom = 2f;
                        Add(objectField);
                    }
                    else
                    {
                        Add(
                            new Label(property.displayName)
                            {
                                style =
                                {
                                    unityFontStyleAndWeight = FontStyle.Bold,
                                    marginBottom = 2f,
                                },
                            }
                        );
                    }
                }
                else
                {
                    bool defaultExpanded = _settings.mode == WInLineEditorMode.FoldoutExpanded;
                    bool savedState = string.IsNullOrEmpty(sessionKey)
                        ? defaultExpanded
                        : SessionState.GetBool(sessionKey, defaultExpanded);
                    _lastFoldoutExpanded = savedState;

                    _foldout = new Foldout
                    {
                        text = property.displayName,
                        value = savedState,
                        style = { marginTop = 2f, marginLeft = FoldoutIndent },
                    };

                    _foldout.RegisterValueChangedCallback(evt =>
                    {
                        if (_currentTarget == null)
                        {
                            _foldout.SetValueWithoutNotify(false);
                            UpdateInlineVisibility();
                            return;
                        }

                        if (!string.IsNullOrEmpty(sessionKey))
                        {
                            SessionState.SetBool(sessionKey, evt.newValue);
                        }
                        _lastFoldoutExpanded = evt.newValue;
                        UpdateInlineVisibility();
                    });

                    Add(_foldout);
                    inlineParent = _foldout.contentContainer;

                    Toggle toggle = _foldout.Q<Toggle>();
                    toggle.style.flexDirection = FlexDirection.Row;
                    toggle.style.alignItems = Align.Center;
                    toggle.style.justifyContent = Justify.FlexStart;
                    toggle.style.paddingLeft = 0f;
                    toggle.style.paddingRight = 0f;
                    toggle.style.marginLeft = 0f;
                    toggle.style.marginRight = 0f;
                    toggle.style.flexGrow = 0f;
                    toggle.style.flexShrink = 1f;
                    toggle.style.minWidth = 0f;

                    VisualElement toggleInput = toggle.Q<VisualElement>(
                        className: "unity-toggle__input"
                    );
                    if (toggleInput != null)
                    {
                        toggleInput.style.flexGrow = 0f;
                        toggleInput.style.marginRight = 3f;
                        toggleInput.style.paddingRight = 0f;
                    }

                    VisualElement foldoutInput = toggle.Q<VisualElement>(
                        className: "unity-foldout__input"
                    );
                    if (foldoutInput != null)
                    {
                        foldoutInput.style.flexGrow = 0f;
                        foldoutInput.style.marginRight = 6f;
                        foldoutInput.style.paddingRight = 0f;
                    }

                    Label toggleLabel = toggle.Q<Label>();
                    if (toggleLabel != null)
                    {
                        toggleLabel.style.marginLeft = -4f;
                        toggleLabel.style.marginRight = 0f;
                        toggleLabel.style.paddingLeft = 0f;
                        toggleLabel.style.paddingRight = 0f;
                    }

                    if (objectField != null)
                    {
                        objectField.label = string.Empty;
                        objectField.style.flexGrow = 1f;
                        objectField.style.flexShrink = 1f;
                        objectField.style.flexBasis = 100f;
                        objectField.style.paddingRight = 0f;
                        objectField.style.paddingLeft = 0f;
                        objectField.style.flexBasis = 0f;
                        objectField.style.minWidth = 0f;
                        objectField.style.marginLeft = 0f;
                        objectField.style.marginRight = 2.5f;
                        objectField.style.width = StyleKeyword.Auto;
                        objectField.style.maxWidth = StyleKeyword.None;

                        if (objectField.labelElement != null)
                        {
                            objectField.labelElement.style.display = DisplayStyle.None;
                            objectField.labelElement.style.marginLeft = 0f;
                            objectField.labelElement.style.width = 0f;
                            objectField.labelElement.style.minWidth = 0f;
                        }

                        VisualElement display = objectField.Q<VisualElement>(
                            className: "unity-object-field-display"
                        );
                        if (display != null)
                        {
                            display.style.flexGrow = 1f;
                            display.style.minWidth = 0f;
                            display.style.flexBasis = 0f;
                            display.style.marginLeft = 0f;
                            display.style.alignItems = Align.Center;
                        }

                        Label valueLabel = objectField.Q<Label>(
                            className: "unity-object-field-display__label"
                        );
                        if (valueLabel != null)
                        {
                            valueLabel.style.marginLeft = 4f;
                            valueLabel.style.flexShrink = 1f;
                        }

                        toggle.Add(objectField);
                    }
                }

                if (_settings.drawHeader)
                {
                    _headerRow = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            alignItems = Align.Center,
                            display = DisplayStyle.None,
                            marginBottom = 2f,
                        },
                    };
                    if (_foldout != null)
                    {
                        _headerRow.style.marginLeft = FoldoutIndent + HeaderIndent;
                    }

                    _headerLabel = new Label
                    {
                        style =
                        {
                            unityFontStyleAndWeight = FontStyle.Bold,
                            flexGrow = 1f,
                            marginRight = 6f,
                        },
                    };

                    _pingButton = new Button(OnPingClicked) { text = "Ping" };
                    _pingButton.SetEnabled(false);

                    _headerRow.Add(_headerLabel);
                    _headerRow.Add(_pingButton);
                    inlineParent.Add(_headerRow);
                }
                else
                {
                    _headerRow = null;
                    _headerLabel = null;
                    _pingButton = null;
                }

                _inspectorRoot = new VisualElement
                {
                    style =
                    {
                        display = DisplayStyle.None,
                        borderLeftWidth = 1f,
                        borderRightWidth = 1f,
                        borderTopWidth = 1f,
                        borderBottomWidth = 1f,
                        borderLeftColor = new Color(0.25f, 0.25f, 0.25f),
                        borderRightColor = new Color(0.25f, 0.25f, 0.25f),
                        borderTopColor = new Color(0.25f, 0.25f, 0.25f),
                        borderBottomColor = new Color(0.25f, 0.25f, 0.25f),
                        paddingLeft = 6f,
                        paddingRight = 6f,
                        paddingTop = 6f,
                        paddingBottom = 6f,
                        marginTop = 2f,
                    },
                };
                if (_foldout != null && _headerRow == null)
                {
                    _inspectorRoot.style.marginLeft = FoldoutIndent + HeaderIndent;
                }

                if (_settings.enableScrolling)
                {
                    _scrollView = new ScrollView(ScrollViewMode.Vertical)
                    {
                        style = { flexGrow = 1f },
                    };
                    if (_settings.inspectorHeight > 0f)
                    {
                        _scrollView.style.maxHeight = _settings.inspectorHeight;
                    }
                    _inspectorRoot.Add(_scrollView);
                }
                else
                {
                    _scrollView = null;
                    if (_settings.inspectorHeight > 0f)
                    {
                        _inspectorRoot.style.maxHeight = _settings.inspectorHeight;
                    }
                }

                inlineParent.Add(_inspectorRoot);

                this.TrackPropertyValue(
                    property,
                    p => OnPropertyValueChanged(p.objectReferenceValue)
                );

                RegisterCallback<DetachFromPanelEvent>(_ => DisposeEditor());

                OnPropertyValueChanged(property.objectReferenceValue);
                UpdateInlineVisibility();
            }

            private ObjectField CreateObjectField()
            {
                ObjectField field = new()
                {
                    bindingPath = _propertyPath,
                    objectType = _referenceType,
                    allowSceneObjects = _allowSceneObjects,
                    style =
                    {
                        flexGrow = 1f,
                        flexShrink = 1f,
                        minWidth = 0f,
                    },
                };
                field.RegisterValueChangedCallback(evt => OnPropertyValueChanged(evt.newValue));
                field.Bind(_serializedObject);
                return field;
            }

            private void OnPropertyValueChanged(UnityEngine.Object newValue)
            {
                if (_currentTarget == newValue)
                {
                    if (_foldout != null && newValue == null)
                    {
                        _foldout.SetValueWithoutNotify(false);
                    }

                    UpdateInlineVisibility();
                    return;
                }

                if (_foldout != null && _currentTarget != null)
                {
                    _lastFoldoutExpanded = _foldout.value;
                }

                _currentTarget = newValue;

                if (_foldout != null)
                {
                    if (_currentTarget == null)
                    {
                        _foldout.SetValueWithoutNotify(false);
                    }
                    else
                    {
                        _foldout.SetValueWithoutNotify(_lastFoldoutExpanded);
                    }
                }

                RefreshHeader();
                BuildInspector();
                UpdateInlineVisibility();
            }

            private void RefreshHeader()
            {
                if (_headerRow == null)
                {
                    return;
                }

                if (_currentTarget == null)
                {
                    _headerRow.style.display = DisplayStyle.None;
                    _pingButton?.SetEnabled(false);
                    return;
                }

                GUIContent content = EditorGUIUtility.ObjectContent(
                    _currentTarget,
                    _currentTarget.GetType()
                );
                _headerLabel.text = content?.text ?? _currentTarget.name;
                _headerRow.style.display = DisplayStyle.Flex;
                _pingButton?.SetEnabled(true);
            }

            private void BuildInspector()
            {
                ClearInspector();
                DisposeEditor();

                if (_currentTarget == null)
                {
                    return;
                }

                try
                {
                    _cachedEditor = Editor.CreateEditor(_currentTarget);
                }
                catch (Exception ex)
                {
                    AddInspectorMessage($"Inspector unavailable: {ex.Message}");
                    return;
                }

                if (_cachedEditor == null)
                {
                    AddInspectorMessage("Inspector unavailable.");
                    return;
                }

                InspectorElement inspectorElement = new(_cachedEditor)
                {
                    style = { flexGrow = 1f },
                };

                if (!_settings.drawPreview)
                {
                    VisualElement preview = inspectorElement.Q(
                        className: "unity-inspector-preview"
                    );
                    if (preview != null)
                    {
                        preview.style.display = DisplayStyle.None;
                    }
                }
                else if (_settings.previewHeight > 0f)
                {
                    VisualElement preview = inspectorElement.Q(
                        className: "unity-inspector-preview"
                    );
                    if (preview != null)
                    {
                        preview.style.maxHeight = _settings.previewHeight;
                    }
                }

                if (_scrollView != null)
                {
                    _scrollView.Clear();
                    _scrollView.Add(inspectorElement);
                }
                else
                {
                    _inspectorRoot.Clear();
                    _inspectorRoot.Add(inspectorElement);
                }
            }

            private void ClearInspector()
            {
                if (_scrollView != null)
                {
                    _scrollView.Clear();
                }
                else
                {
                    _inspectorRoot.Clear();
                }
            }

            private void AddInspectorMessage(string message)
            {
                Label label = new(message)
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleCenter,
                        whiteSpace = WhiteSpace.Normal,
                    },
                };

                if (_scrollView != null)
                {
                    _scrollView.Clear();
                    _scrollView.Add(label);
                }
                else
                {
                    _inspectorRoot.Clear();
                    _inspectorRoot.Add(label);
                }
            }

            private void UpdateInlineVisibility()
            {
                bool shouldShow =
                    _currentTarget != null
                    && (
                        _settings.mode == WInLineEditorMode.AlwaysExpanded
                        || (_foldout?.value ?? false)
                    );

                _inspectorRoot.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
                if (_headerRow != null)
                {
                    _headerRow.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            private void OnPingClicked()
            {
                if (_currentTarget == null)
                {
                    return;
                }

                EditorGUIUtility.PingObject(_currentTarget);
                Selection.activeObject = _currentTarget;
            }

            private void DisposeEditor()
            {
                if (_cachedEditor != null)
                {
                    UnityEngine.Object.DestroyImmediate(_cachedEditor);
                    _cachedEditor = null;
                }
            }

            private static Type ResolveObjectReferenceType(FieldInfo drawerField)
            {
                if (drawerField == null)
                {
                    return typeof(UnityEngine.Object);
                }

                Type fieldType = drawerField.FieldType;
                if (fieldType.IsArray)
                {
                    return fieldType.GetElementType();
                }

                if (fieldType.IsGenericType)
                {
                    Type[] arguments = fieldType.GetGenericArguments();
                    if (
                        arguments.Length == 1
                        && typeof(UnityEngine.Object).IsAssignableFrom(arguments[0])
                    )
                    {
                        return arguments[0];
                    }
                }

                return typeof(UnityEngine.Object).IsAssignableFrom(fieldType)
                    ? fieldType
                    : typeof(UnityEngine.Object);
            }

            private static bool ShouldAllowSceneObjects(Type referenceType)
            {
                if (referenceType == null)
                {
                    return true;
                }

                if (typeof(ScriptableObject).IsAssignableFrom(referenceType))
                {
                    return false;
                }

                if (
                    typeof(Component).IsAssignableFrom(referenceType)
                    || typeof(GameObject).IsAssignableFrom(referenceType)
                )
                {
                    return true;
                }

                return referenceType == typeof(UnityEngine.Object);
            }
        }
    }
#endif
}

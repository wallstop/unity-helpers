namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [CustomPropertyDrawer(typeof(WInLineEditorAttribute))]
    public sealed class WInLineEditorPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (attribute is not WInLineEditorAttribute inlineAttribute)
            {
                return new PropertyField(property);
            }

            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                return new PropertyField(property);
            }

            return new InlineInspectorElement(property, inlineAttribute, fieldInfo);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
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

        private sealed class InlineInspectorElement : VisualElement
        {
            private readonly SerializedObject serializedObject;
            private readonly string propertyPath;
            private readonly WInLineEditorAttribute settings;
            private readonly Type referenceType;
            private readonly bool allowSceneObjects;
            private readonly string sessionKey;

            private readonly ObjectField objectField;
            private readonly Foldout foldout;
            private readonly VisualElement headerRow;
            private readonly Label headerLabel;
            private readonly Button pingButton;
            private readonly VisualElement inspectorRoot;
            private readonly ScrollView scrollView;

            private Editor cachedEditor;
            private UnityEngine.Object currentTarget;

            public InlineInspectorElement(
                SerializedProperty property,
                WInLineEditorAttribute inlineAttribute,
                FieldInfo drawerField
            )
            {
                serializedObject = property.serializedObject;
                propertyPath = property.propertyPath;
                settings = inlineAttribute;
                referenceType = ResolveObjectReferenceType(drawerField);
                allowSceneObjects = ShouldAllowSceneObjects(referenceType);
                sessionKey =
                    $"WInLineEditor:{serializedObject.targetObject.GetInstanceID()}:{propertyPath}";

                style.flexDirection = FlexDirection.Column;

                // Object field ------------------------------------------------
                if (settings.drawObjectField)
                {
                    objectField = new ObjectField(property.displayName)
                    {
                        bindingPath = propertyPath,
                        objectType = referenceType,
                        allowSceneObjects = allowSceneObjects,
                    };
                    objectField.RegisterValueChangedCallback(evt =>
                    {
                        OnPropertyValueChanged(evt.newValue as UnityEngine.Object);
                    });
                    objectField.Bind(serializedObject);
                    Add(objectField);
                }
                else
                {
                    objectField = null;
                    Label label = new(property.displayName)
                    {
                        style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2f },
                    };
                    Add(label);
                }

                // Foldout -----------------------------------------------------
                VisualElement inlineParent = this;
                if (settings.mode == WInLineEditorMode.AlwaysExpanded)
                {
                    foldout = null;
                }
                else
                {
                    bool defaultExpanded = settings.mode == WInLineEditorMode.FoldoutExpanded;
                    bool savedState = SessionState.GetBool(sessionKey, defaultExpanded);

                    foldout = new Foldout
                    {
                        text = settings.drawObjectField ? "Details" : property.displayName,
                        value = savedState,
                    };
                    foldout.style.marginTop = 2f;

                    foldout.RegisterValueChangedCallback(evt =>
                    {
                        SessionState.SetBool(sessionKey, evt.newValue);
                        UpdateInlineVisibility();
                    });

                    Add(foldout);
                    inlineParent = foldout.contentContainer;
                }

                // Header row --------------------------------------------------
                if (settings.drawHeader)
                {
                    headerRow = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            alignItems = Align.Center,
                            marginBottom = 2f,
                            display = DisplayStyle.None,
                        },
                    };

                    headerLabel = new Label
                    {
                        style = { flexGrow = 1f, unityFontStyleAndWeight = FontStyle.Bold },
                    };
                    headerLabel.style.marginRight = 6f;

                    pingButton = new Button(OnPingClicked) { text = "Ping" };
                    pingButton.SetEnabled(false);

                    headerRow.Add(headerLabel);
                    headerRow.Add(pingButton);
                    inlineParent.Add(headerRow);
                }
                else
                {
                    headerRow = null;
                    headerLabel = null;
                    pingButton = null;
                }

                // Inspector host ----------------------------------------------
                inspectorRoot = new VisualElement
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

                if (settings.enableScrolling)
                {
                    scrollView = new ScrollView(ScrollViewMode.Vertical)
                    {
                        style = { flexGrow = 1f },
                    };

                    if (settings.inspectorHeight > 0f)
                    {
                        scrollView.style.maxHeight = settings.inspectorHeight;
                    }

                    inspectorRoot.Add(scrollView);
                }
                else
                {
                    scrollView = null;
                    if (settings.inspectorHeight > 0f)
                    {
                        inspectorRoot.style.maxHeight = settings.inspectorHeight;
                    }
                }

                inlineParent.Add(inspectorRoot);

                // Monitor property changes -----------------------------------
                this.TrackPropertyValue(
                    property,
                    changedProperty => OnPropertyValueChanged(changedProperty.objectReferenceValue)
                );

                RegisterCallback<DetachFromPanelEvent>(_ => DisposeEditor());

                // Initial state -----------------------------------------------
                OnPropertyValueChanged(property.objectReferenceValue);
                UpdateInlineVisibility();
            }

            private void OnPropertyValueChanged(UnityEngine.Object newValue)
            {
                if (currentTarget == newValue)
                {
                    UpdateInlineVisibility();
                    return;
                }

                currentTarget = newValue;
                RefreshHeader();
                BuildInspector();
                UpdateInlineVisibility();
            }

            private void RefreshHeader()
            {
                if (headerRow == null)
                {
                    return;
                }

                if (currentTarget == null)
                {
                    headerRow.style.display = DisplayStyle.None;
                    pingButton?.SetEnabled(false);
                    return;
                }

                GUIContent content = EditorGUIUtility.ObjectContent(
                    currentTarget,
                    currentTarget.GetType()
                );
                headerLabel.text = content?.text ?? currentTarget.name;
                headerRow.style.display = DisplayStyle.Flex;
                pingButton?.SetEnabled(true);
            }

            private void BuildInspector()
            {
                ClearInspector();
                DisposeEditor();

                if (currentTarget == null)
                {
                    if (foldout != null)
                    {
                        foldout.SetEnabled(false);
                    }
                    return;
                }

                if (foldout != null)
                {
                    foldout.SetEnabled(true);
                }

                try
                {
                    cachedEditor = Editor.CreateEditor(currentTarget);
                }
                catch (Exception ex)
                {
                    AddInspectorMessage($"Inspector unavailable: {ex.Message}");
                    return;
                }

                if (cachedEditor == null)
                {
                    AddInspectorMessage("Inspector unavailable.");
                    return;
                }

                InspectorElement inspectorElement = new(cachedEditor) { style = { flexGrow = 1f } };

                if (!settings.drawPreview)
                {
                    VisualElement preview = inspectorElement.Q(
                        className: "unity-inspector-preview"
                    );
                    if (preview != null)
                    {
                        preview.style.display = DisplayStyle.None;
                    }
                }
                else if (settings.previewHeight > 0f)
                {
                    VisualElement preview = inspectorElement.Q(
                        className: "unity-inspector-preview"
                    );
                    if (preview != null)
                    {
                        preview.style.maxHeight = settings.previewHeight;
                    }
                }

                if (scrollView != null)
                {
                    scrollView.Clear();
                    scrollView.Add(inspectorElement);
                }
                else
                {
                    inspectorRoot.Clear();
                    inspectorRoot.Add(inspectorElement);
                }
            }

            private void ClearInspector()
            {
                if (scrollView != null)
                {
                    scrollView.Clear();
                }
                else
                {
                    inspectorRoot.Clear();
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

                if (scrollView != null)
                {
                    scrollView.Clear();
                    scrollView.Add(label);
                }
                else
                {
                    inspectorRoot.Clear();
                    inspectorRoot.Add(label);
                }
            }

            private void UpdateInlineVisibility()
            {
                if (foldout != null)
                {
                    foldout.SetEnabled(currentTarget != null);
                }

                bool shouldShow =
                    currentTarget != null
                    && (
                        settings.mode == WInLineEditorMode.AlwaysExpanded
                        || (foldout?.value ?? false)
                    );

                inspectorRoot.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
                if (headerRow != null)
                {
                    headerRow.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            private void OnPingClicked()
            {
                if (currentTarget == null)
                {
                    return;
                }

                EditorGUIUtility.PingObject(currentTarget);
                Selection.activeObject = currentTarget;
            }

            private void DisposeEditor()
            {
                if (cachedEditor != null)
                {
                    UnityEngine.Object.DestroyImmediate(cachedEditor);
                    cachedEditor = null;
                }
            }
        }
    }
#endif
}

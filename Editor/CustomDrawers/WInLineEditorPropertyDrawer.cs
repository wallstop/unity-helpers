namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
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
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property == null)
            {
                return new PropertyField(null);
            }

            FieldInfo resolvedFieldInfo = fieldInfo ?? ResolveFieldInfo(property);
            WInLineEditorAttribute inlineAttribute =
                attribute as WInLineEditorAttribute
                ?? ReflectionHelpers.GetAttributeSafe<WInLineEditorAttribute>(
                    resolvedFieldInfo,
                    inherit: true
                );

            if (
                inlineAttribute == null
                || property.propertyType != SerializedPropertyType.ObjectReference
            )
            {
                return new PropertyField(property);
            }

            return new InlineInspectorElement(property, inlineAttribute, resolvedFieldInfo);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
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
                string sessionKey =
                    $"WInLineEditor:{_serializedObject.targetObject.GetInstanceID()}:{_propertyPath}";

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
                    bool savedState = SessionState.GetBool(sessionKey, defaultExpanded);
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

                        SessionState.SetBool(sessionKey, evt.newValue);
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

namespace UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Attributes;
    using Core.DataVisualizer;
    using Core.Extension;
    using Core.Helper;
    using Core.Serialization;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Object = UnityEngine.Object;
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector.Editor;
#else
    using UnityEditor.UIElements;
#endif

    public sealed class DataVisualizer : EditorWindow
    {
        private const string CustomTypeOrderKey =
            "WallstopStudios.UnityHelpers.DataVisualizer.CustomTypeOrder";
        private const string CustomNamespaceOrderKey =
            "WallstopStudios.UnityHelpers.DataVisualizer.CustomNamespaceOrder";

        private readonly List<(string key, List<Type> types)> _scriptableObjectTypes = new();
        private readonly Dictionary<BaseDataObject, VisualElement> _objectVisualElementMap = new();
        private readonly List<BaseDataObject> _selectedObjects = new();
        private BaseDataObject _selectedObject;

        private VisualElement _namespaceListContainer;
        private VisualElement _objectListContainer;
        private VisualElement _inspectorContainer;
        private ScrollView _objectScrollView;
        private ScrollView _inspectorScrollView;

#if ODIN_INSPECTOR
        private PropertyTree _odinPropertyTree;
        private IMGUIContainer _odinInspectorContainer;
        private IVisualElementScheduledItem _odinRepaintSchedule;
#endif

        [MenuItem("Tools/Unity Helpers/Data Visualizer")]
        public static void ShowWindow()
        {
            DataVisualizer window = GetWindow<DataVisualizer>("Data Visualizer");
            window.titleContent = new GUIContent("Data Visualizer");
        }

        private void OnEnable()
        {
            _objectVisualElementMap.Clear();
            _selectedObject = null;
            _selectedObjects.Clear();
#if ODIN_INSPECTOR
            _odinPropertyTree = null;
#endif
            LoadScriptableObjectTypes();
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Cleanup()
        {
#if ODIN_INSPECTOR
            _odinRepaintSchedule?.Pause();
            _odinRepaintSchedule = null;
            if (_odinPropertyTree != null)
            {
                _odinPropertyTree.OnPropertyValueChanged -= HandleOdinPropertyValueChanged;
                _odinPropertyTree.Dispose();
                _odinPropertyTree = null;
            }
            _odinInspectorContainer?.RemoveFromHierarchy();
            _odinInspectorContainer?.Dispose();
            _odinInspectorContainer = null;
#endif
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();

            VisualElement mainContainer = new()
            {
                name = "main-container",
                style = { flexGrow = 1, flexDirection = FlexDirection.Row },
            };
            root.Add(mainContainer);

            VisualElement namespaceColumn = new()
            {
                name = "namespace-column",
                style =
                {
                    width = 200,
                    borderRightWidth = 1,
                    borderRightColor = Color.gray,
                }, // Fixed width
            };
            ScrollView namespaceScrollView = new(ScrollViewMode.Vertical)
            {
                name = "namespace-scrollview",
                style = { flexGrow = 1 },
            };
            _namespaceListContainer = new VisualElement { name = "namespace-list" };
            namespaceScrollView.Add(_namespaceListContainer);
            namespaceColumn.Add(
                new Label("Namespaces")
                {
                    style = { unityFontStyleAndWeight = FontStyle.Bold, paddingBottom = 5 },
                }
            );
            namespaceColumn.Add(namespaceScrollView);
            mainContainer.Add(namespaceColumn);

            VisualElement objectColumn = new()
            {
                name = "object-column",
                style =
                {
                    width = 200,
                    borderRightWidth = 1,
                    borderRightColor = Color.gray,
                },
            };
            objectColumn.Add(
                new Label("Objects")
                {
                    style = { unityFontStyleAndWeight = FontStyle.Bold, paddingBottom = 5 },
                }
            );
            _objectScrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "object-scrollview",
                style = { flexGrow = 1 },
            };
            _objectListContainer = new VisualElement { name = "object-list" };
            _objectScrollView.Add(_objectListContainer);
            objectColumn.Add(_objectScrollView);
            mainContainer.Add(objectColumn);

            VisualElement inspectorColumn = new()
            {
                name = "inspector-column",
                style = { flexGrow = 1 },
            };
            _inspectorScrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "inspector-scrollview",
                style = { flexGrow = 1 },
            };
            _inspectorContainer = new VisualElement { name = "inspector-content" };
            _inspectorScrollView.Add(_inspectorContainer);
            inspectorColumn.Add(_inspectorScrollView);
            mainContainer.Add(inspectorColumn);

            BuildNamespaceView();
            BuildObjectsView();
            BuildInspectorView();
        }

#if ODIN_INSPECTOR
        private void HandleOdinPropertyValueChanged(InspectorProperty property, int selectionIndex)
        {
            if (_selectedObject == null || _odinPropertyTree == null || property == null)
            {
                return;
            }

            if (
                _odinPropertyTree.WeakTargets == null
                || _odinPropertyTree.WeakTargets.Count <= selectionIndex
                || !ReferenceEquals(_odinPropertyTree.WeakTargets[selectionIndex], _selectedObject)
            )
            {
                return;
            }

            const string titleFieldName = nameof(BaseDataObject._title);
            bool titlePotentiallyChanged = property.Name == titleFieldName;

            if (titlePotentiallyChanged)
            {
                RefreshSelectedElement();
            }
        }
#endif

        private void RefreshSelectedElement()
        {
            if (
                _selectedObject == null
                || !_objectVisualElementMap.TryGetValue(
                    _selectedObject,
                    out VisualElement visualElement
                )
            )
            {
                return;
            }

            BaseDataObject dataObject = _selectedObject;
            visualElement
                .schedule.Execute(() => UpdateObjectTitleRepresentation(dataObject))
                .ExecuteLater(10);
        }

        private void BuildNamespaceView()
        {
            if (_namespaceListContainer == null)
            {
                return;
            }

            _namespaceListContainer.Clear();
            foreach ((string key, List<Type> types) in _scriptableObjectTypes)
            {
                Label namespaceLabel = new(key)
                {
                    style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 5 },
                };
                _namespaceListContainer.Add(namespaceLabel);

                foreach (Type type in types)
                {
                    var typeButton = new Button(() =>
                    {
                        LoadObjectTypes(type);
                        BuildObjectsView();
                        _selectedObject = null;
#if ODIN_INSPECTOR

                        bool recreateTree =
                            _odinPropertyTree?.WeakTargets == null
                            || _odinPropertyTree.WeakTargets.Count == 0
                            || !ReferenceEquals(_odinPropertyTree.WeakTargets[0], _selectedObject);

                        if (recreateTree)
                        {
                            if (_odinPropertyTree != null)
                            {
                                _odinPropertyTree.OnPropertyValueChanged -=
                                    HandleOdinPropertyValueChanged;
                            }

                            _odinPropertyTree = null;
                            if (_selectedObject != null)
                            {
                                try
                                {
                                    _odinPropertyTree = PropertyTree.Create(_selectedObject);
                                    _odinPropertyTree.OnPropertyValueChanged +=
                                        HandleOdinPropertyValueChanged;
                                }
                                catch (Exception e)
                                {
                                    this.LogError(
                                        $"Failed to create Odin PropertyTree for {_selectedObject.name}.",
                                        e
                                    );
                                }
                            }

                            _odinInspectorContainer?.MarkDirtyRepaint();
                        }
#endif
                        BuildInspectorView();
                    })
                    {
                        text = type.Name,
                        style = { marginLeft = 10 },
                    };
                    _namespaceListContainer.Add(typeButton);
                }
            }
        }

        private void BuildObjectsView()
        {
            if (_objectListContainer == null)
            {
                return;
            }

            _objectListContainer.Clear();
            _objectScrollView.scrollOffset = Vector2.zero;

            foreach (BaseDataObject dataObject in _selectedObjects)
            {
                BaseDataObject currentObject = dataObject;
                Button objectButton = new(() =>
                {
                    Selection.activeObject = currentObject;
                    _selectedObject = currentObject;
                    BuildInspectorView();
                })
                {
                    text = currentObject.Title,
                    style = { minHeight = 20 },
                };

                _objectVisualElementMap[dataObject] = objectButton;
                _objectListContainer.Add(objectButton);
            }
        }

        private void BuildInspectorView()
        {
            if (_inspectorContainer == null)
            {
                return;
            }

            _inspectorContainer.Clear();
            _inspectorScrollView.scrollOffset = Vector2.zero;

            if (_selectedObject == null)
            {
                _inspectorContainer.Add(
                    new Label("Select an object to inspect.")
                    {
                        style = { unityTextAlign = TextAnchor.MiddleCenter, paddingTop = 20 },
                    }
                );
                return;
            }

            using SerializedObject serializedObject = new(_selectedObject);
#if ODIN_INSPECTOR
            try
            {
                if (
                    _odinPropertyTree?.WeakTargets == null
                    || _odinPropertyTree.WeakTargets.Count == 0
                    || !ReferenceEquals(_odinPropertyTree.WeakTargets[0], _selectedObject)
                )
                {
                    if (_odinPropertyTree != null)
                    {
                        _odinPropertyTree.OnPropertyValueChanged -= HandleOdinPropertyValueChanged;
                        _odinPropertyTree.Dispose();
                    }
                    _odinPropertyTree = PropertyTree.Create(_selectedObject);
                    _odinPropertyTree.OnPropertyValueChanged += HandleOdinPropertyValueChanged;
                }

                if (_odinInspectorContainer == null)
                {
                    _odinInspectorContainer = new IMGUIContainer(() =>
                    {
                        if (
                            _odinPropertyTree is { WeakTargets: { Count: > 0 } }
                            && ReferenceEquals(_odinPropertyTree.WeakTargets[0], _selectedObject)
                        )
                        {
                            try
                            {
                                _odinPropertyTree.Draw();
                            }
                            catch (Exception e)
                            {
                                this.LogError($"Odin Draw Error.", e);
                                GUILayout.Label("Odin Draw Error");
                            }
                        }
                    })
                    {
                        name = "odin-inspector",
                        style = { flexGrow = 1 },
                    };

                    _odinInspectorContainer.RegisterCallback<FocusInEvent, DataVisualizer>(
                        (evt, context) =>
                        {
                            context._odinRepaintSchedule?.Pause();
                            context._odinRepaintSchedule = context
                                ._odinInspectorContainer.schedule.Execute(() =>
                                {
                                    if (
                                        context
                                            ._odinInspectorContainer
                                            .focusController
                                            ?.focusedElement == _odinInspectorContainer
                                    )
                                    {
                                        context._odinInspectorContainer.MarkDirtyRepaint();
                                    }
                                    else
                                    {
                                        context._odinRepaintSchedule?.Pause();
                                    }
                                })
                                .Every(100);
                        },
                        this
                    );
                    _odinInspectorContainer.RegisterCallback<FocusOutEvent, DataVisualizer>(
                        (evt, context) =>
                        {
                            context._odinRepaintSchedule?.Pause();
                        },
                        this
                    );
                }
                else
                {
                    _odinInspectorContainer.onGUIHandler = () =>
                    {
                        if (
                            _odinPropertyTree?.WeakTargets is not { Count: > 0 }
                            || !ReferenceEquals(_odinPropertyTree.WeakTargets[0], _selectedObject)
                        )
                        {
                            return;
                        }

                        try
                        {
                            _odinPropertyTree.Draw();
                        }
                        catch (Exception e)
                        {
                            this.LogError($"Odin Draw Error.", e);
                            GUILayout.Label("Odin Draw Error");
                        }
                    };
                }
                _inspectorContainer.Add(_odinInspectorContainer);
                _odinInspectorContainer.MarkDirtyRepaint();
            }
            catch (Exception e)
            {
                this.LogError($"Error setting up Odin Inspector.", e);
                _inspectorContainer.Add(new Label($"Odin Inspector Error: {e.Message}"));
            }

#else
            try
            {
                SerializedProperty serializedProperty = serializedObject.GetIterator();
                serializedProperty.NextVisible(true);
                const string titleFieldName = nameof(BaseDataObject._title);
                while (serializedProperty.NextVisible(false))
                {
                    SerializedProperty currentPropCopy = serializedProperty.Copy();
                    PropertyField propertyField = new(currentPropCopy);
                    propertyField.Bind(serializedObject);

                    if (
                        string.Equals(
                            currentPropCopy.name,
                            titleFieldName,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        propertyField.RegisterValueChangeCallback(evt =>
                        {
                            RefreshSelectedElement();
                        });
                    }
                    _inspectorContainer.Add(propertyField);
                }
            }
            catch (Exception e)
            {
                this.LogError($"Error creating standard inspector.", e);
                _inspectorContainer.Add(new Label($"Inspector Error: {e.Message}"));
            }
#endif

            VisualElement customElement = _selectedObject.BuildGUI(serializedObject);
            if (customElement != null)
            {
                _inspectorContainer.Add(customElement);
            }
        }

        private void UpdateObjectTitleRepresentation(BaseDataObject dataObject)
        {
            if (
                dataObject == null
                || _selectedObject != dataObject
                || !_objectVisualElementMap.TryGetValue(dataObject, out VisualElement element)
            )
            {
                return;
            }

            string currentTitle = dataObject.Title;
            if (element is Button buttonElement)
            {
                if (buttonElement.text != currentTitle)
                {
                    buttonElement.text = currentTitle;
                }
            }
            else if (element is Label labelElement)
            {
                if (labelElement.text != currentTitle)
                {
                    labelElement.text = currentTitle;
                }
            }
        }

        private void LoadObjectTypes(Type type)
        {
            _selectedObjects.Clear();
            foreach (string assetGuid in AssetDatabase.FindAssets($"t:{type.Name}"))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (asset is BaseDataObject dataObject && dataObject != null)
                {
                    _selectedObjects.Add(dataObject);
                }
            }
            _selectedObjects.Sort(
                (lhs, rhs) =>
                {
                    if (0 < lhs._customOrder || 0 < rhs._customOrder)
                    {
                        return lhs._customOrder.CompareTo(rhs._customOrder);
                    }
                    return string.Compare(lhs.Title, rhs.Title, StringComparison.OrdinalIgnoreCase);
                }
            );
        }

        private void LoadScriptableObjectTypes()
        {
            _scriptableObjectTypes.Clear();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<BaseDataObject>())
            {
                if (type.IsAbstract)
                {
                    continue;
                }

                string key = GetNamespaceKey(type);

                List<Type> types;
                int index = _scriptableObjectTypes.FindIndex(kvp =>
                    string.Equals(key, kvp.key, StringComparison.OrdinalIgnoreCase)
                );
                if (index < 0)
                {
                    types = new List<Type>();
                    _scriptableObjectTypes.Add((key, types));
                }
                else
                {
                    types = _scriptableObjectTypes[index].types;
                }
                types.Add(type);
            }

            List<string> customNamespaceOrder = LoadCustomOrder(CustomNamespaceOrderKey);
            _scriptableObjectTypes.Sort(
                (lhs, rhs) => CompareUsingCustomOrder(lhs.key, rhs.key, customNamespaceOrder)
            );
            foreach ((string key, List<Type> types) in _scriptableObjectTypes)
            {
                List<string> customTypeNameOrder = LoadCustomOrder($"{CustomTypeOrderKey}.{key}");
                types.Sort(
                    (lhs, rhs) => CompareUsingCustomOrder(lhs.Name, rhs.Name, customTypeNameOrder)
                );
            }
        }

        private static int CompareUsingCustomOrder(
            string keyA,
            string keyB,
            List<string> customOrder
        )
        {
            int indexA = customOrder.IndexOf(keyA);
            int indexB = customOrder.IndexOf(keyB);

            switch (indexA)
            {
                case >= 0 when indexB >= 0:
                    return indexA.CompareTo(indexB);
                case >= 0:
                    return -1;
            }

            return indexB >= 0 ? 1 : string.Compare(keyA, keyB, StringComparison.OrdinalIgnoreCase);
        }

        private List<string> LoadCustomOrder(string customOrderKey)
        {
            try
            {
                string json = EditorPrefs.GetString(customOrderKey, "[]");
                return Serializer.JsonDeserialize<List<string>>(json) ?? new List<string>();
            }
            catch (Exception e)
            {
                this.Log(
                    $"Failed to load custom order for key '{customOrderKey}'. Using default order.",
                    e
                );
                return new List<string>();
            }
        }

        private static string GetNamespaceKey(Type type)
        {
            if (
                type.IsAttributeDefined(out DataVisualizerCustomPropertiesAttribute attribute)
                && !string.IsNullOrWhiteSpace(attribute.Namespace)
            )
            {
                return attribute.Namespace;
            }
            return type.Namespace?.Split('.').Last() ?? "No Namespace";
        }
    }
#endif
}

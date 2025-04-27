namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Sirenix.OdinInspector.Editor;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.DataVisualizer;
    using Core.Extension;
    using Core.Helper;
    using Core.Serialization;
    using Object = UnityEngine.Object;
#if ODIN_INSPECTOR

#else
    using UnityEditor.UIElements;
#endif

    public sealed class DataVisualizer : EditorWindow
    {
        private const float DragUpdateThrottleTime = 0.05f;
        private const string CustomTypeOrderKey =
            "WallstopStudios.UnityHelpers.DataVisualizer.CustomTypeOrder";

        private const string CustomNamespaceOrderKey =
            "WallstopStudios.UnityHelpers.DataVisualizer.CustomNamespaceOrder";

        // {0} = Namespace Key
        private const string CustomTypeOrderKeyFormat =
            "WallstopStudios.UnityHelpers.DataVisualizer.CustomTypeOrder.{0}";

        private enum DragType
        {
            None = 0,
            Object = 1,
            Namespace = 2,
            Type = 3,
        }

        private readonly List<(string key, List<Type> types)> _scriptableObjectTypes = new();
        private readonly Dictionary<BaseDataObject, VisualElement> _objectVisualElementMap = new();
        private readonly List<BaseDataObject> _selectedObjects = new();
        private BaseDataObject _selectedObject;
        private VisualElement _selectedElement;

        private VisualElement _namespaceListContainer;
        private VisualElement _objectListContainer;
        private VisualElement _inspectorContainer;
        private ScrollView _objectScrollView;
        private ScrollView _inspectorScrollView;

        private DragType _activeDragType = DragType.None;
        private object _draggedData;

        private VisualElement _inPlaceGhost;
        private int _lastGhostInsertIndex = -1;
        private VisualElement _lastGhostParent;
        private VisualElement _draggedElement;
        private VisualElement _dragGhost;
        private Vector2 _dragStartPosition;
        private bool _isDragging;
        private float _lastDragUpdateTime;

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
            _selectedElement = null;
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
            CancelDrag();
            _dragGhost?.RemoveFromHierarchy();
            _dragGhost = null;
            _draggedElement = null;
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

            StyleSheet styleSheet = null;
            string packageRoot = DirectoryHelper.FindPackageRootPath(
                DirectoryHelper.GetCallerScriptDirectory()
            );
            if (!string.IsNullOrWhiteSpace(packageRoot))
            {
                char pathSeparator = Path.DirectorySeparatorChar;
                string styleSheetPath =
                    $"{packageRoot}{pathSeparator}Editor{pathSeparator}DataVisualizer{pathSeparator}Styles{pathSeparator}DataVisualizerStyles.uss";
                string unityRelativeStyleSheetPath = DirectoryHelper.AbsoluteToUnityRelativePath(
                    styleSheetPath
                );
                if (!string.IsNullOrWhiteSpace(unityRelativeStyleSheetPath))
                {
                    styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                        unityRelativeStyleSheetPath
                    );
                }
            }
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }
            else
            {
                this.LogError($"Failed to find Data Visualizer style sheet.");
            }

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
                rootVisualElement
                    .schedule.Execute(() => RefreshSelectedElementVisuals(_selectedObject))
                    .ExecuteLater(1);
            }
        }
#endif

        private void BuildNamespaceView()
        {
            if (_namespaceListContainer == null)
            {
                return;
            }

            _namespaceListContainer.Clear();

            const string namespaceItemClass = "object-item";
            const string namespaceLabelClass = "object-item__label";
            const string typeItemClass = "type-item";
            const string typeLabelClass = "type-item__label";

            foreach ((string key, List<Type> types) in _scriptableObjectTypes)
            {
                VisualElement namespaceGroupItem = new()
                {
                    name = $"namespace-group-{key}",
                    userData = key,
                };

                namespaceGroupItem.AddToClassList(namespaceItemClass);
                Label namespaceLabel = new(key)
                {
                    style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 5 },
                };
                namespaceLabel.AddToClassList(namespaceLabelClass);
                namespaceGroupItem.Add(namespaceLabel);
                namespaceGroupItem.RegisterCallback<PointerDownEvent>(OnNamespacePointerDown);
                _namespaceListContainer.Add(namespaceGroupItem);

                VisualElement typesContainer = new()
                {
                    name = $"types-container-{key}",
                    userData = key,
                };
                namespaceGroupItem.Add(typesContainer);

                foreach (Type type in types)
                {
                    VisualElement typeItem = new()
                    {
                        name = $"type-item-{type.Name}",
                        userData = type,
                    };
                    typeItem.AddToClassList(typeItemClass);

                    Label typeLabel = new(type.Name) { name = "type-item-label" };
                    typeLabel.AddToClassList(typeLabelClass);
                    typeItem.Add(typeLabel);

                    typeItem.RegisterCallback<PointerDownEvent>(OnTypePointerDown);
                    typeItem.RegisterCallback<PointerUpEvent>(evt =>
                    {
                        if (
                            !_isDragging
                            && evt.button == 0
                            && typeItem.userData is Type clickedType
                        )
                        {
                            LoadObjectTypes(clickedType);
                            BuildObjectsView();
                            SelectObject(null);
                            evt.StopPropagation();
                        }
                    });

                    typesContainer.Add(typeItem);
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
            _objectVisualElementMap.Clear();
            _objectScrollView.scrollOffset = Vector2.zero;

            foreach (BaseDataObject dataObject in _selectedObjects)
            {
                VisualElement objectItem = new()
                {
                    name = $"object-item-{dataObject.GetInstanceID()}",
                };
                objectItem.AddToClassList("object-item");
                objectItem.userData = dataObject;

                Label titleLabel = new(dataObject.Title) { name = "object-item-label" };
                titleLabel.AddToClassList("object-item__label");
                objectItem.Add(titleLabel);

                objectItem.RegisterCallback<PointerDownEvent>(OnObjectPointerDown);

                // Store mapping and add to list
                _objectVisualElementMap[dataObject] = objectItem;
                _objectListContainer.Add(objectItem);

                if (_selectedObject != null && _selectedObject == dataObject)
                {
                    objectItem.AddToClassList("selected");
                    _selectedElement = objectItem;
                }
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
#if ODIN_INSPECTOR
                if (_odinPropertyTree != null)
                {
                    _odinPropertyTree.OnPropertyValueChanged -= HandleOdinPropertyValueChanged;
                    _odinPropertyTree.Dispose();
                    _odinPropertyTree = null;
                }
                _odinInspectorContainer?.MarkDirtyRepaint();
#endif
                return;
            }

            using SerializedObject serializedObject = new(_selectedObject);
#if ODIN_INSPECTOR
            try
            {
                bool recreateTree =
                    _odinPropertyTree?.WeakTargets == null
                    || _odinPropertyTree.WeakTargets.Count == 0
                    || !ReferenceEquals(_odinPropertyTree.WeakTargets[0], _selectedObject);

                if (recreateTree)
                {
                    if (_odinPropertyTree != null)
                    {
                        _odinPropertyTree.OnPropertyValueChanged -= HandleOdinPropertyValueChanged;
                        _odinPropertyTree.Dispose();
                    }
                    _odinPropertyTree = PropertyTree.Create(_selectedObject);
                    _odinPropertyTree.OnPropertyValueChanged += HandleOdinPropertyValueChanged;
                    _odinInspectorContainer?.MarkDirtyRepaint();
                }

                if (_odinInspectorContainer == null)
                {
                    _odinInspectorContainer = new IMGUIContainer(() => _odinPropertyTree?.Draw())
                    {
                        name = "odin-inspector",
                        style = { flexGrow = 1 },
                    };
                }
                else
                {
                    _odinInspectorContainer.onGUIHandler = () => _odinPropertyTree?.Draw();
                }

                if (_odinInspectorContainer.parent != _inspectorContainer)
                {
                    _inspectorContainer.Add(_odinInspectorContainer);
                }
                _odinInspectorContainer.MarkDirtyRepaint();
            }
            catch (Exception e)
            {
                this.LogError($"Error setting up Odin Inspector.", e);
                _inspectorContainer.Add(new Label($"Odin Inspector Error: {e.Message}"));
                _odinPropertyTree = null;
            }
#else
            try
            {
                SerializedProperty serializedProperty = serializedObject.GetIterator();
                bool enterChildren = true;
                const string titleFieldName = nameof(BaseDataObject._title);

                // Draw the default script field
                if (serializedProperty.NextVisible(enterChildren))
                {
                    using (
                        new EditorGUI.DisabledScope("m_Script" == serializedProperty.propertyPath)
                    )
                    {
                        PropertyField scriptField = new PropertyField(serializedProperty);
                        scriptField.Bind(serializedObject); // Bind is important
                        _inspectorContainer.Add(scriptField);
                    }
                    enterChildren = false; // Don't re-enter children for the script field itself
                }

                while (serializedProperty.NextVisible(enterChildren))
                {
                    SerializedProperty currentPropCopy = serializedProperty.Copy(); // Use copy for safety
                    PropertyField propertyField = new(currentPropCopy);
                    propertyField.Bind(serializedObject); // Bind the field

                    // Check if this is the title field and register callback
                    if (
                        string.Equals(
                            currentPropCopy.propertyPath,
                            titleFieldName,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        propertyField.RegisterValueChangeCallback(evt =>
                        {
                            // Use schedule to avoid modifying data during UI build/event
                            rootVisualElement
                                .schedule.Execute(
                                    () => RefreshSelectedElementVisuals(_selectedObject)
                                )
                                .ExecuteLater(1);
                        });
                    }
                    _inspectorContainer.Add(propertyField);
                    enterChildren = false; // Only step through top-level properties after the first one
                }
                // Apply changes if any occurred
                serializedObject.ApplyModifiedProperties();
            }
            catch (Exception e)
            {
                this.LogError($"Error creating standard inspector.", e);
                _inspectorContainer.Add(new Label($"Inspector Error: {e.Message}"));
            }
#endif
            VisualElement customElement = _selectedObject.BuildGUI(
                new DataVisualizerGUIContext(serializedObject)
            );
            if (customElement != null)
            {
                _inspectorContainer.Add(customElement);
            }
        }

        private void RefreshSelectedElementVisuals(BaseDataObject dataObject)
        {
            if (
                dataObject == null
                || !_objectVisualElementMap.TryGetValue(dataObject, out VisualElement visualElement)
            )
            {
                return;
            }
            UpdateObjectTitleRepresentation(dataObject, visualElement);
        }

        private void UpdateObjectTitleRepresentation(
            BaseDataObject dataObject,
            VisualElement element
        )
        {
            if (dataObject == null || element == null)
            {
                return;
            }

            Label titleLabel = element.Q<Label>(className: "object-item__label");
            if (titleLabel == null)
            {
                this.LogError($"Could not find title label within object item element.");
                return;
            }

            string currentTitle = dataObject.Title;
            if (titleLabel.text != currentTitle)
            {
                titleLabel.text = currentTitle;
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
                    if (0 <= lhs._customOrder && 0 <= rhs._customOrder)
                    {
                        int comparison = lhs._customOrder.CompareTo(rhs._customOrder);
                        if (comparison != 0)
                        {
                            return comparison;
                        }
                    }
                    return string.Compare(lhs.Title, rhs.Title, StringComparison.OrdinalIgnoreCase);
                }
            );
            SelectObject(null);
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

        private void SelectObject(BaseDataObject dataObject)
        {
            _selectedElement?.RemoveFromClassList("selected");
            _selectedObject = dataObject;
            _selectedElement = null;

            if (
                _selectedObject != null
                && _objectVisualElementMap.TryGetValue(
                    _selectedObject,
                    out VisualElement newSelectedElement
                )
            )
            {
                _selectedElement = newSelectedElement;
                _selectedElement.AddToClassList("selected");
                Selection.activeObject = _selectedObject;
                _objectScrollView.ScrollTo(_selectedElement);
            }
            else
            {
                Selection.activeObject = null;
            }

            BuildInspectorView();
        }

        private void OnObjectPointerDown(PointerDownEvent evt)
        {
            VisualElement targetElement = evt.currentTarget as VisualElement;
            if (targetElement?.userData is not BaseDataObject clickedObject)
            {
                return;
            }

            if (_selectedObject != clickedObject)
            {
                SelectObject(clickedObject);
            }

            if (evt.button == 0)
            {
                _draggedElement = targetElement;
                _draggedData = clickedObject;
                _activeDragType = DragType.Object;
                _dragStartPosition = evt.position;
                targetElement.CapturePointer(evt.pointerId);
                targetElement.RegisterCallback<PointerMoveEvent>(OnCapturedPointerMove);
                targetElement.RegisterCallback<PointerUpEvent>(OnCapturedPointerUp);
                targetElement.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
                evt.StopPropagation();
            }
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (_activeDragType != DragType.None && _draggedElement != null)
            {
                _draggedElement.UnregisterCallback<PointerMoveEvent>(OnCapturedPointerMove);
                _draggedElement.UnregisterCallback<PointerUpEvent>(OnCapturedPointerUp);
                _draggedElement.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
                CancelDrag();
            }
        }

        private void OnCapturedPointerMove(PointerMoveEvent evt)
        {
            if (
                _draggedElement == null
                || !_draggedElement.HasPointerCapture(evt.pointerId)
                || _activeDragType == DragType.None
            )
            {
                return;
            }

            if (_dragGhost != null)
            {
                _dragGhost.style.left = evt.position.x - _dragGhost.resolvedStyle.width / 2;
                _dragGhost.style.top = evt.position.y - _dragGhost.resolvedStyle.height;
            }

            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - _lastDragUpdateTime < DragUpdateThrottleTime)
            {
                return;
            }
            _lastDragUpdateTime = currentTime;

            if (!_isDragging)
            {
                if (Vector2.Distance(evt.position, _dragStartPosition) > 5.0f)
                {
                    _isDragging = true;
                    string dragText = _draggedData switch
                    {
                        BaseDataObject dataObj => dataObj.Title,
                        string nsKey => nsKey,
                        Type type => type.Name,
                        _ => "Dragging Item",
                    };
                    StartDragVisuals(evt.position, dragText);
                }
                else
                {
                    return;
                }
            }

            if (_isDragging)
            {
                UpdateInPlaceGhostPosition(evt.position);
            }
        }

        private void OnCapturedPointerUp(PointerUpEvent evt)
        {
            if (
                _draggedElement == null
                || !_draggedElement.HasPointerCapture(evt.pointerId)
                || _activeDragType == DragType.None
            )
            {
                return;
            }

            int pointerId = evt.pointerId;
            bool performDrop = _isDragging;
            DragType dropType = _activeDragType;

            VisualElement draggedElement = _draggedElement;
            try
            {
                _draggedElement.ReleasePointer(pointerId);

                if (performDrop)
                {
                    switch (dropType)
                    {
                        case DragType.Object:
                            PerformObjectDrop();
                            break;
                        case DragType.Namespace:
                            PerformNamespaceDrop();
                            break;
                        case DragType.Type:
                            PerformTypeDrop();
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                this.LogError($"Error during drop execution for {dropType}.", e);
            }
            finally
            {
                draggedElement.UnregisterCallback<PointerMoveEvent>(OnCapturedPointerMove);
                draggedElement.UnregisterCallback<PointerUpEvent>(OnCapturedPointerUp);
                draggedElement.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

                CancelDrag();
            }

            evt.StopPropagation();
        }

        private void PerformNamespaceDrop()
        {
            int targetIndex = _inPlaceGhost?.userData is int index ? index : -1;

            _inPlaceGhost?.RemoveFromHierarchy();

            if (
                _draggedElement == null
                || _draggedData is not string draggedKey
                || _namespaceListContainer == null
            )
            {
                return;
            }

            if (targetIndex < 0)
            {
                return;
            }

            int currentIndex = _namespaceListContainer.IndexOf(_draggedElement);
            if (currentIndex < 0)
            {
                return;
            }

            if (currentIndex < targetIndex)
            {
                targetIndex--;
            }

            _draggedElement.style.display = DisplayStyle.Flex;
            _draggedElement.style.opacity = 1.0f;
            _namespaceListContainer.Insert(targetIndex, _draggedElement);

            int oldDataIndex = _scriptableObjectTypes.FindIndex(kvp => kvp.key == draggedKey);
            if (0 <= oldDataIndex)
            {
                (string key, List<Type> types) draggedItem = _scriptableObjectTypes[oldDataIndex];
                _scriptableObjectTypes.RemoveAt(oldDataIndex);
                int dataInsertIndex = targetIndex;
                dataInsertIndex = Mathf.Clamp(dataInsertIndex, 0, _scriptableObjectTypes.Count);
                _scriptableObjectTypes.Insert(dataInsertIndex, draggedItem);
                UpdateAndSaveNamespaceOrder();
            }
        }

        private void OnNamespacePointerDown(PointerDownEvent evt)
        {
            if (
                evt.currentTarget
                is not VisualElement { userData: string namespaceKey } targetElement
            )
            {
                return;
            }

            if (evt.button == 0)
            {
                _draggedElement = targetElement;
                _draggedData = namespaceKey;
                _activeDragType = DragType.Namespace;
                _dragStartPosition = evt.position;
                targetElement.CapturePointer(evt.pointerId);
                targetElement.RegisterCallback<PointerMoveEvent>(OnCapturedPointerMove);
                targetElement.RegisterCallback<PointerUpEvent>(OnCapturedPointerUp);
                targetElement.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
                evt.StopPropagation();
            }
        }

        private void UpdateAndSaveNamespaceOrder()
        {
            List<string> newNamespaceOrder = _scriptableObjectTypes.Select(kvp => kvp.key).ToList();
            try
            {
                string json = Serializer.JsonStringify(newNamespaceOrder);
                EditorPrefs.SetString(CustomNamespaceOrderKey, json);
            }
            catch (Exception e)
            {
                this.LogError($"Failed to serialize or save custom namespace order.", e);
            }
        }

        private void OnTypePointerDown(PointerDownEvent evt)
        {
            if (evt.currentTarget is not VisualElement { userData: Type type } targetElement)
            {
                return;
            }

            if (evt.button == 0)
            {
                _draggedElement = targetElement;
                _draggedData = type;
                _activeDragType = DragType.Type;
                _dragStartPosition = evt.position;
                _isDragging = false;
                targetElement.CapturePointer(evt.pointerId);
                targetElement.RegisterCallback<PointerMoveEvent>(OnCapturedPointerMove);
                targetElement.RegisterCallback<PointerUpEvent>(OnCapturedPointerUp);
                targetElement.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
                evt.StopPropagation();
            }
        }

        private void PerformTypeDrop()
        {
            int targetIndex = _inPlaceGhost?.userData is int index ? index : -1;
            _inPlaceGhost?.RemoveFromHierarchy();

            VisualElement typesContainer = _draggedElement?.parent;
            string namespaceKey = typesContainer?.userData as string;

            if (
                _draggedElement == null
                || _draggedData is not Type draggedType
                || typesContainer == null
                || string.IsNullOrEmpty(namespaceKey)
            )
            {
                return;
            }

            if (targetIndex < 01)
            {
                return;
            }

            int currentIndex = typesContainer.IndexOf(_draggedElement);
            if (currentIndex < targetIndex)
            {
                targetIndex--;
            }

            _draggedElement.style.display = DisplayStyle.Flex;
            _draggedElement.style.opacity = 1.0f;
            typesContainer.Insert(targetIndex, _draggedElement);

            int namespaceIndex = _scriptableObjectTypes.FindIndex(kvp => kvp.key == namespaceKey);
            if (0 <= namespaceIndex)
            {
                List<Type> typesList = _scriptableObjectTypes[namespaceIndex].types;
                int oldDataIndex = typesList.IndexOf(draggedType);
                if (0 <= oldDataIndex)
                {
                    typesList.RemoveAt(oldDataIndex);
                    int dataInsertIndex = targetIndex;
                    dataInsertIndex = Mathf.Clamp(dataInsertIndex, 0, typesList.Count);
                    typesList.Insert(dataInsertIndex, draggedType);

                    UpdateAndSaveTypeOrder(namespaceKey, typesList);
                }
            }
        }

        private void UpdateAndSaveTypeOrder(string namespaceKey, List<Type> orderedTypes)
        {
            List<string> newTypeNameOrder = orderedTypes.Select(t => t.Name).ToList();
            string prefsKey = string.Format(CustomTypeOrderKeyFormat, namespaceKey);
            try
            {
                string json = Serializer.JsonStringify(newTypeNameOrder);
                EditorPrefs.SetString(prefsKey, json);
            }
            catch (Exception e)
            {
                this.LogError(
                    $"Failed to serialize or save custom type order for namespace '{namespaceKey}'.",
                    e
                );
            }
        }

        private void PerformObjectDrop()
        {
            int targetIndex = _inPlaceGhost?.userData is int index ? index : -1;

            if (_inPlaceGhost != null)
            {
                _inPlaceGhost.RemoveFromHierarchy();
            }

            if (
                _draggedElement == null
                || _draggedData is not BaseDataObject draggedObject
                || _objectListContainer == null
            )
            {
                return;
            }

            if (targetIndex < 0)
            {
                return;
            }

            int currentIndex = _objectListContainer.IndexOf(_draggedElement);
            if (currentIndex < 0)
            {
                return;
            }

            if (currentIndex < targetIndex)
            {
                targetIndex--;
            }

            _draggedElement.style.display = DisplayStyle.Flex;
            _draggedElement.style.opacity = 1.0f;
            _objectListContainer.Insert(targetIndex, _draggedElement);

            int oldDataIndex = _selectedObjects.IndexOf(draggedObject);
            if (0 <= oldDataIndex)
            {
                _selectedObjects.RemoveAt(oldDataIndex);
                int dataInsertIndex = targetIndex;
                dataInsertIndex = Mathf.Clamp(dataInsertIndex, 0, _selectedObjects.Count);
                _selectedObjects.Insert(dataInsertIndex, draggedObject);
                UpdateAndSaveObjectCustomOrder();
            }
        }

        private void StartDragVisuals(Vector2 currentPosition, string dragText)
        {
            if (_draggedElement == null || _draggedData == null)
            {
                return;
            }

            if (_dragGhost == null)
            {
                _dragGhost = new VisualElement
                {
                    name = "drag-ghost-cursor",
                    style = { visibility = Visibility.Visible },
                };
                _dragGhost.style.left = currentPosition.x - _dragGhost.resolvedStyle.width / 2;
                _dragGhost.style.top = currentPosition.y - _dragGhost.resolvedStyle.height;
                _dragGhost.AddToClassList("drag-ghost");
                _dragGhost.BringToFront();
                Label ghostLabel = new(dragText)
                {
                    style = { unityTextAlign = TextAnchor.MiddleLeft },
                };
                _dragGhost.Add(ghostLabel);
                rootVisualElement.Add(_dragGhost);
            }
            else
            {
                Label ghostLabel = _dragGhost.Q<Label>();
                if (ghostLabel != null)
                {
                    ghostLabel.text = dragText;
                }
            }

            _dragGhost.style.visibility = Visibility.Visible;
            _dragGhost.style.left = currentPosition.x - _draggedElement.resolvedStyle.width / 2;
            _dragGhost.style.top = currentPosition.y - _draggedElement.resolvedStyle.height;
            _dragGhost.BringToFront();

            if (_inPlaceGhost == null)
            {
                try
                {
                    _inPlaceGhost = new VisualElement
                    {
                        name = "drag-ghost-overlay",
                        style =
                        {
                            height = _draggedElement.resolvedStyle.height,
                            marginTop = _draggedElement.resolvedStyle.marginTop,
                            marginBottom = _draggedElement.resolvedStyle.marginBottom,
                            marginLeft = _draggedElement.resolvedStyle.marginLeft,
                            marginRight = _draggedElement.resolvedStyle.marginRight,
                        },
                    };

                    foreach (string className in _draggedElement.GetClasses())
                    {
                        _inPlaceGhost.AddToClassList(className);
                    }
                    _inPlaceGhost.AddToClassList("in-place-ghost");

                    Label originalLabel =
                        _draggedElement.Q<Label>(className: "object-item__label")
                        ?? _draggedElement.Q<Label>(className: "type-item__label")
                        ?? _draggedElement.Q<Label>();

                    if (originalLabel != null)
                    {
                        Label ghostLabel = new(originalLabel.text);
                        foreach (string className in originalLabel.GetClasses())
                        {
                            ghostLabel.AddToClassList(className);
                        }
                        ghostLabel.pickingMode = PickingMode.Ignore;
                        _inPlaceGhost.Add(ghostLabel);
                    }
                    else
                    {
                        Label fallbackLabel = new(dragText) { pickingMode = PickingMode.Ignore };
                        _inPlaceGhost.Add(fallbackLabel);
                    }

                    _inPlaceGhost.pickingMode = PickingMode.Ignore;
                    _inPlaceGhost.style.visibility = Visibility.Hidden;
                }
                catch (Exception ex)
                {
                    this.LogError($"Error creating in-place ghost: {ex}");
                    _inPlaceGhost = null;
                }
            }

            _lastGhostInsertIndex = -1;
            _lastGhostParent = null;
            _lastDragUpdateTime = Time.realtimeSinceStartup;
            _draggedElement.style.display = DisplayStyle.None;
            _draggedElement.style.opacity = 0.5f;
        }

        private void UpdateInPlaceGhostPosition(Vector2 pointerPosition)
        {
            VisualElement container = null;
            VisualElement positioningParent;

            switch (_activeDragType)
            {
                case DragType.Object:
                    container = _objectListContainer.contentContainer;
                    positioningParent = _objectListContainer.contentContainer;
                    break;
                case DragType.Namespace:
                    container = _namespaceListContainer;
                    positioningParent = _namespaceListContainer;
                    break;
                case DragType.Type:
                    if (_draggedElement != null)
                    {
                        container = _draggedElement.parent;
                    }
                    positioningParent = container;
                    break;
                default:
                    if (_inPlaceGhost?.parent != null)
                    {
                        _inPlaceGhost.RemoveFromHierarchy();
                    }
                    if (_inPlaceGhost != null)
                    {
                        _inPlaceGhost.style.visibility = Visibility.Hidden;
                    }
                    _lastGhostInsertIndex = -1;
                    _lastGhostParent = null;
                    return;
            }

            if (
                container == null
                || positioningParent == null
                || _draggedElement == null
                || _inPlaceGhost == null
            )
            {
                if (_inPlaceGhost?.parent != null)
                {
                    _inPlaceGhost.RemoveFromHierarchy();
                }

                if (_inPlaceGhost != null)
                {
                    _inPlaceGhost.style.visibility = Visibility.Hidden;
                }

                _lastGhostInsertIndex = -1;
                _lastGhostParent = null;
                return;
            }

            int childCount = container.childCount;
            int targetIndex = -1;
            Vector2 localPointerPos = container.WorldToLocal(pointerPosition);

            int index = 0;
            for (int i = 0; i < childCount; ++i)
            {
                VisualElement child = container.ElementAt(i);

                float childMidY = child.layout.yMin + child.resolvedStyle.height / 2f;
                if (localPointerPos.y < childMidY)
                {
                    targetIndex = index;
                    break;
                }

                if (child != _draggedElement)
                {
                    index++;
                }
            }

            if (targetIndex < 0)
            {
                targetIndex = childCount;
                targetIndex = Math.Max(0, targetIndex);
            }

            bool targetIndexValid = true;
            int maxIndex = positioningParent.childCount;

            if (_inPlaceGhost.parent == positioningParent)
            {
                maxIndex--;
            }

            maxIndex = Math.Max(0, maxIndex);
            targetIndex = Mathf.Clamp(targetIndex, 0, maxIndex + 1);

            if (targetIndex != _lastGhostInsertIndex || positioningParent != _lastGhostParent)
            {
                if (_inPlaceGhost.parent != null && _inPlaceGhost.parent != positioningParent)
                {
                    _inPlaceGhost.RemoveFromHierarchy();
                    positioningParent.Add(_inPlaceGhost);
                }
                else if (0 <= targetIndex && targetIndex <= positioningParent.childCount)
                {
                    _inPlaceGhost.RemoveFromHierarchy();
                    if (positioningParent.childCount < targetIndex)
                    {
                        positioningParent.Add(_inPlaceGhost);
                    }
                    else
                    {
                        positioningParent.Insert(targetIndex, _inPlaceGhost);
                    }
                }
                else
                {
                    targetIndexValid = false;
                }

                if (targetIndexValid)
                {
                    _inPlaceGhost.style.visibility = Visibility.Visible;
                }

                _lastGhostInsertIndex = targetIndex;
                _lastGhostParent = positioningParent;
            }
            else
            {
                _inPlaceGhost.style.visibility = Visibility.Visible;
            }

            if (targetIndexValid)
            {
                _inPlaceGhost.userData = targetIndex;
            }
            else
            {
                if (_inPlaceGhost.parent != null)
                {
                    _inPlaceGhost.RemoveFromHierarchy();
                }

                _inPlaceGhost.style.visibility = Visibility.Hidden;
                _inPlaceGhost.userData = -1;
                _lastGhostInsertIndex = -1;
                _lastGhostParent = null;
            }
        }

        private void UpdateAndSaveObjectCustomOrder()
        {
            List<Object> dirtyObjects = new();
            for (int i = 0; i < _selectedObjects.Count; i++)
            {
                BaseDataObject obj = _selectedObjects[i];
                if (obj == null)
                {
                    continue;
                }

                int newOrder = i + 1;
                if (obj._customOrder != newOrder)
                {
                    obj._customOrder = newOrder;
                    EditorUtility.SetDirty(obj);
                    dirtyObjects.Add(obj);
                }
            }

            if (dirtyObjects.Count > 0)
            {
                AssetDatabase.SaveAssets();
            }
        }

        private void CancelDrag()
        {
            if (_inPlaceGhost != null)
            {
                _inPlaceGhost.RemoveFromHierarchy();
                _inPlaceGhost = null;
            }
            _lastGhostInsertIndex = -1;
            _lastGhostParent = null;

            if (_draggedElement != null)
            {
                _draggedElement.style.display = DisplayStyle.Flex;
                _draggedElement.style.opacity = 1.0f;
            }

            if (_dragGhost != null)
            {
                _dragGhost.style.visibility = Visibility.Hidden;
            }

            _isDragging = false;
            _draggedElement = null;
            _draggedData = null;
            _activeDragType = DragType.None;
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
            return type.Namespace?.Split('.').LastOrDefault() ?? "No Namespace";
        }
    }
#endif
}

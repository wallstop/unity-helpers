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
        private const string CustomTypeOrderKey =
            "WallstopStudios.UnityHelpers.DataVisualizer.CustomTypeOrder";

        private const string CustomNamespaceOrderKey =
            "WallstopStudios.UnityHelpers.DataVisualizer.CustomNamespaceOrder";

        // {0} = Namespace Key
        private const string CustomTypeOrderKeyFormat =
            "WallstopStudios.UnityHelpers.DataVisualizer.CustomTypeOrder.{0}"; // {0} = Namespace Key

        // {0} = Type Name
        private const string CustomObjectOrderKeyFormat =
            "WallstopStudios.UnityHelpers.DataVisualizer.CustomObjectOrder.{0}";

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

        private enum DragType
        {
            None = 0,
            Object = 1,
            Namespace = 2,
            Type = 3,
        } // Added Type

        private DragType _activeDragType = DragType.None;
        private object _draggedData; // Holds BaseDataObject, namespace key (string), or Type

        private VisualElement _draggedElement;
        private BaseDataObject _draggedObject;
        private VisualElement _dragGhost;
        private VisualElement _dropIndicator;
        private Vector2 _dragStartPosition;
        private bool _isDragging;

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
            _dropIndicator?.RemoveFromHierarchy();
            _dragGhost = null;
            _dropIndicator = null;
            _draggedElement = null;
            _draggedObject = null;
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

            _dropIndicator = new VisualElement { name = "drop-indicator" };
            _dropIndicator.AddToClassList("dtop-indicator");
            _dropIndicator.style.visibility = Visibility.Hidden;
            _objectScrollView.Add(_dropIndicator);

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
                        if (!_isDragging && evt.button == 0) // Check if not dragging and left click up
                        {
                            if (typeItem.userData is Type clickedType)
                            {
                                LoadObjectTypes(clickedType);
                                BuildObjectsView();
                                SelectObject(null);
                                evt.StopPropagation(); // Consume the event
                            }
                        }
                    });

                    typesContainer.Add(typeItem);

                    //                     var typeButton = new Button(() =>
                    //                     {
                    //                         LoadObjectTypes(type);
                    //                         BuildObjectsView();
                    //                         _selectedObject = null;
                    // #if ODIN_INSPECTOR
                    //
                    //                         bool recreateTree =
                    //                             _odinPropertyTree?.WeakTargets == null
                    //                             || _odinPropertyTree.WeakTargets.Count == 0
                    //                             || !ReferenceEquals(_odinPropertyTree.WeakTargets[0], _selectedObject);
                    //
                    //                         if (recreateTree)
                    //                         {
                    //                             if (_odinPropertyTree != null)
                    //                             {
                    //                                 _odinPropertyTree.OnPropertyValueChanged -=
                    //                                     HandleOdinPropertyValueChanged;
                    //                             }
                    //
                    //                             _odinPropertyTree = null;
                    //                             if (_selectedObject != null)
                    //                             {
                    //                                 try
                    //                                 {
                    //                                     _odinPropertyTree = PropertyTree.Create(_selectedObject);
                    //                                     _odinPropertyTree.OnPropertyValueChanged +=
                    //                                         HandleOdinPropertyValueChanged;
                    //                                 }
                    //                                 catch (Exception e)
                    //                                 {
                    //                                     this.LogError(
                    //                                         $"Failed to create Odin PropertyTree for {_selectedObject.name}.",
                    //                                         e
                    //                                     );
                    //                                 }
                    //                             }
                    //
                    //                             _odinInspectorContainer?.MarkDirtyRepaint();
                    //                         }
                    // #endif
                    //                         BuildInspectorView();
                    //                     })
                    //                     {
                    //                         text = type.Name,
                    //                         style = { marginLeft = 10 },
                    //                     };
                    //                     _namespaceListContainer.Add(typeButton);
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
                // Create the main container element for the object
                VisualElement objectItem = new VisualElement
                {
                    name = $"object-item-{dataObject.GetInstanceID()}",
                };
                objectItem.AddToClassList("object-item"); // Apply USS style
                objectItem.userData = dataObject; // Store data object reference

                // Create a label for the title
                Label titleLabel = new Label(dataObject.Title) { name = "object-item-label" };
                titleLabel.AddToClassList("object-item__label"); // Apply USS style
                objectItem.Add(titleLabel);

                // --- Register Pointer Down Event for Selection and Drag Start ---
                objectItem.RegisterCallback<PointerDownEvent>(OnObjectPointerDown);

                // Store mapping and add to list
                _objectVisualElementMap[dataObject] = objectItem;
                _objectListContainer.Add(objectItem);

                // --- Re-apply selection style if this object was selected ---
                if (_selectedObject != null && _selectedObject == dataObject)
                {
                    objectItem.AddToClassList("selected");
                    _selectedElement = objectItem; // Ensure _selectedElement is up-to-date
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
                // Clear Odin tree when nothing is selected
                if (_odinPropertyTree != null)
                {
                    _odinPropertyTree.OnPropertyValueChanged -= HandleOdinPropertyValueChanged;
                    _odinPropertyTree.Dispose();
                    _odinPropertyTree = null;
                }
                _odinInspectorContainer?.MarkDirtyRepaint(); // Update IMGUI
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
                    // Odin needs explicit repaint sometimes after tree change
                    _odinInspectorContainer?.MarkDirtyRepaint();
                }

                if (_odinInspectorContainer == null)
                {
                    // Simplified Odin IMGUIContainer setup
                    _odinInspectorContainer = new IMGUIContainer(() => _odinPropertyTree?.Draw())
                    {
                        name = "odin-inspector",
                        style = { flexGrow = 1 },
                    };
                    // Focus handling can be simplified or removed if not strictly needed
                }
                else
                {
                    // Ensure the handler is correct if the container persists
                    _odinInspectorContainer.onGUIHandler = () => _odinPropertyTree?.Draw();
                }

                // Ensure container is added if it was removed or never added
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
                _odinPropertyTree = null; // Prevent further errors
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
            UpdateObjectTitleRepresentation(dataObject, visualElement); // Pass the element
        }

        private void UpdateObjectTitleRepresentation(
            BaseDataObject dataObject,
            VisualElement element
        )
        {
            if (dataObject == null || element == null)
                return;

            // Find the Label within the element
            Label titleLabel = element.Q<Label>(className: "object-item__label"); // More robust query
            if (titleLabel == null)
            {
                Debug.LogError("Could not find title label within object item element.");
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
            // Deselect previous
            if (_selectedElement != null)
            {
                _selectedElement.RemoveFromClassList("selected");
            }

            _selectedObject = dataObject;
            _selectedElement = null; // Reset selected element

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
                Selection.activeObject = _selectedObject; // Update Unity selection
                // Optionally scroll to the selected item
                _objectScrollView.ScrollTo(_selectedElement);
            }
            else
            {
                // If null is passed or object not found, clear Unity selection
                Selection.activeObject = null;
            }

            // Rebuild the inspector view for the newly selected object (or lack thereof)
            BuildInspectorView();
        }

        private void OnObjectPointerDown(PointerDownEvent evt)
        {
            VisualElement targetElement = evt.currentTarget as VisualElement;
            if (targetElement?.userData is not BaseDataObject clickedObject)
                return;

            // --- Handle Selection --- (Remains the same)
            if (_selectedObject != clickedObject)
            {
                SelectObject(clickedObject);
            }

            // --- Initiate Drag ---
            // Check for left mouse button to start drag
            if (evt.button == 0) // Left mouse button
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
            // This handler is attached to _draggedElement
            // Debug.LogWarning($"Pointer Capture Out detected on {_draggedElement?.name}. Cleaning up drag state.");

            // Check if we were actually dragging with this element
            if (_activeDragType != DragType.None && _draggedElement != null)
            {
                // We lost capture unexpectedly, likely means the drag is cancelled externally.
                // Clean up everything as if the drag ended.

                // Ensure local handlers are unregistered (might be redundant if called after Up, but safe)
                // Debug.Log($"Unregistering handlers due to Capture Out on {_draggedElement.name}");
                _draggedElement.UnregisterCallback<PointerMoveEvent>(OnCapturedPointerMove);
                _draggedElement.UnregisterCallback<PointerUpEvent>(OnCapturedPointerUp);
                _draggedElement.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

                // Reset state and visuals
                CancelDrag();
            }
        }

        private void OnCapturedPointerMove(PointerMoveEvent evt)
        {
            // This handler is attached to _draggedElement

            // Ensure we are actually in a drag state initiated by this element
            if (
                _draggedElement == null
                || !_draggedElement.HasPointerCapture(evt.pointerId)
                || _activeDragType == DragType.None
            )
            {
                // Should not happen if registration/capture is correct, but good safety check
                return;
            }

            // Check if we've moved enough to start the visual drag
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
                    return; // Not dragging yet
                }
            }

            // --- Update Drag Visuals (if dragging started) ---
            if (_isDragging) // Check again as it might have just been set true
            {
                // Update Ghost Position (using evt.position relative to window/panel)
                if (_dragGhost != null)
                {
                    // Calculate center offset if desired
                    float ghostOffsetX = _dragGhost.resolvedStyle.width / 2f;
                    float ghostOffsetY = _dragGhost.resolvedStyle.height / 2f;
                    _dragGhost.style.left = evt.position.x - ghostOffsetX;
                    _dragGhost.style.top = evt.position.y - ghostOffsetY;
                }

                // Update Drop Indicator Position
                UpdateDropIndicator(evt.position);
            }

            // Stop propagation? Usually not needed here as the capturing element gets priority.
            // evt.StopPropagation();
        }

        private void OnCapturedPointerUp(PointerUpEvent evt)
        {
            // This handler is attached to _draggedElement

            // Ensure this is the pointer we captured
            if (
                _draggedElement == null
                || !_draggedElement.HasPointerCapture(evt.pointerId)
                || _activeDragType == DragType.None
            )
            {
                // Debug.LogWarning($"OnCapturedPointerUp received event for pointer {evt.pointerId}, but not actively dragging or element mismatch.");
                return;
            }

            int pointerId = evt.pointerId;
            bool performDrop = _isDragging; // Check if we moved enough to count as a drop
            DragType dropType = _activeDragType; // Store before potentially resetting in CancelDrag

            var draggedElement = _draggedElement;
            // Use try...finally to guarantee pointer release and handler unregistration
            try
            {
                // 1. Release Pointer - CRITICAL
                // Debug.Log($"Releasing pointer {pointerId} in OnCapturedPointerUp for {_draggedElement.name}");
                _draggedElement.ReleasePointer(pointerId);

                // 2. Perform Drop Logic (only if we actually dragged)
                if (performDrop)
                {
                    // Debug.Log($"Performing drop for {dropType}...");
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
                    // Debug.Log($"Drop performed for {dropType}.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during drop execution for {dropType}: {ex}");
            }
            finally
            {
                // 3. Unregister Local Handlers - CRITICAL
                // Debug.Log($"Unregistering drag handlers from {_draggedElement.name}");
                draggedElement.UnregisterCallback<PointerMoveEvent>(OnCapturedPointerMove);
                draggedElement.UnregisterCallback<PointerUpEvent>(OnCapturedPointerUp);
                // Also unregister PointerCaptureOutEvent if you handle it (see step 6)
                draggedElement.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

                // 4. Reset State via CancelDrag (which no longer needs to release pointer)
                // Debug.Log("Calling CancelDrag from OnCapturedPointerUp finally block.");
                CancelDrag(); // Reset state variables and visuals
            }

            // Stop the event after handling it here
            evt.StopPropagation();
        }

        private void PerformNamespaceDrop()
        {
            if (
                _draggedElement == null
                || !(_draggedData is string draggedKey)
                || _namespaceListContainer == null
                || _dropIndicator == null
            )
            {
                CancelDrag(); // Includes resetting _activeDragType
                return;
            }

            // Get target index from indicator (calculated by the modified UpdateDropIndicator)
            int targetIndex = _dropIndicator.userData is int index ? index : -1;

            if (targetIndex == -1)
            {
                // Debug.LogWarning("Namespace drop target index invalid.");
                CancelDrag();
                return;
            }

            // --- Reorder Visual Element ---
            int currentIndex = _namespaceListContainer.IndexOf(_draggedElement);
            if (currentIndex == targetIndex || currentIndex + 1 == targetIndex)
            {
                CancelDrag();
                return;
            }

            targetIndex = Mathf.Clamp(targetIndex, 0, _namespaceListContainer.childCount - 1);
            _namespaceListContainer.Insert(targetIndex, _draggedElement);

            // --- Reorder Data (_scriptableObjectTypes list and save to EditorPrefs) ---
            int oldDataIndex = _scriptableObjectTypes.FindIndex(kvp => kvp.key == draggedKey);
            if (oldDataIndex >= 0)
            {
                var draggedItem = _scriptableObjectTypes[oldDataIndex];
                _scriptableObjectTypes.RemoveAt(oldDataIndex);

                int dataInsertIndex = targetIndex; // Visual index corresponds to data index before insertion
                dataInsertIndex = Mathf.Clamp(dataInsertIndex, 0, _scriptableObjectTypes.Count);
                _scriptableObjectTypes.Insert(dataInsertIndex, draggedItem);

                // --- Update and Save Namespace Order ---
                UpdateAndSaveNamespaceOrder();
            }
            else
            {
                Debug.LogError($"Dragged namespace key '{draggedKey}' not found in data list!");
            }

            // Cleanup happens in CancelDrag called by OnGlobalPointerUp
        }

        private void OnNamespacePointerDown(PointerDownEvent evt)
        {
            VisualElement targetElement = evt.currentTarget as VisualElement;
            // Ensure it's a namespace item and has the key string in userData
            if (targetElement == null || !(targetElement.userData is string namespaceKey))
            {
                // Debug.LogWarning("PointerDown target is not a valid namespace item.");
                return;
            }

            // No selection logic needed for namespaces

            // --- Initiate Drag ---
            if (evt.button == 0) // Left mouse button
            {
                _draggedElement = targetElement;
                _draggedData = namespaceKey; // Store the namespace key string
                _activeDragType = DragType.Namespace; // << SET DRAG TYPE
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
            // Extract the ordered keys from the potentially reordered _scriptableObjectTypes list
            List<string> newNamespaceOrder = _scriptableObjectTypes.Select(kvp => kvp.key).ToList();

            try
            {
                // Serialize and save to EditorPrefs
                string json = Serializer.JsonStringify(newNamespaceOrder);
                EditorPrefs.SetString(CustomNamespaceOrderKey, json);
                // Debug.Log($"Saved custom namespace order: {json}");
            }
            catch (Exception e)
            {
                this.LogError($"Failed to serialize or save custom namespace order.", e);
            }
        }

        private void OnTypePointerDown(PointerDownEvent evt)
        {
            VisualElement targetElement = evt.currentTarget as VisualElement;
            // Ensure it's a type item and has the Type in userData
            if (targetElement is not { userData: Type type })
            {
                return;
            }

            // No selection logic needed for types

            if (evt.button == 0) // Left mouse button
            {
                _draggedElement = targetElement;
                _draggedData = type; // Store the System.Type
                _activeDragType = DragType.Type; // << SET DRAG TYPE
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
            if (
                _draggedElement == null
                || !(_draggedData is Type draggedType)
                || _dropIndicator == null
            )
            {
                CancelDrag();
                return;
            }

            // The drop indicator's target container should be the typesContainer
            VisualElement typesContainer = _draggedElement.parent; // The parent should be the typesContainer
            if (typesContainer == null || !(typesContainer.userData is string namespaceKey))
            {
                Debug.LogError("Could not determine parent types container or its namespace key.");
                CancelDrag();
                return;
            }

            // Get target index from indicator (calculated relative to typesContainer)
            int targetIndex = _dropIndicator.userData is int index ? index : -1;
            if (targetIndex == -1)
            {
                CancelDrag();
                return;
            }

            // --- Reorder Visual Element ---
            int currentIndex = typesContainer.IndexOf(_draggedElement);
            if (currentIndex == targetIndex || currentIndex + 1 == targetIndex)
            {
                CancelDrag();
                return;
            }

            targetIndex = Mathf.Clamp(targetIndex, 0, typesContainer.childCount - 1);
            typesContainer.Insert(targetIndex, _draggedElement);

            // --- Reorder Data (Types list within _scriptableObjectTypes and save to EditorPrefs) ---
            int namespaceIndex = _scriptableObjectTypes.FindIndex(kvp => kvp.key == namespaceKey);
            if (namespaceIndex >= 0)
            {
                List<Type> typesList = _scriptableObjectTypes[namespaceIndex].types;
                int oldDataIndex = typesList.IndexOf(draggedType);
                if (oldDataIndex >= 0)
                {
                    typesList.RemoveAt(oldDataIndex);
                    int dataInsertIndex = targetIndex; // Visual index corresponds to data index
                    dataInsertIndex = Mathf.Clamp(dataInsertIndex, 0, typesList.Count);
                    typesList.Insert(dataInsertIndex, draggedType);

                    // --- Update and Save Type Order for this Namespace ---
                    UpdateAndSaveTypeOrder(namespaceKey, typesList);
                }
                else
                {
                    Debug.LogError(
                        $"Dragged type '{draggedType.Name}' not found in data list for namespace '{namespaceKey}'!"
                    );
                }
            }
            else
            {
                Debug.LogError(
                    $"Namespace key '{namespaceKey}' not found in _scriptableObjectTypes!"
                );
            }

            // Cleanup happens in CancelDrag
        }

        private void UpdateAndSaveTypeOrder(string namespaceKey, List<Type> orderedTypes)
        {
            // Extract the ordered type names
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
            if (
                _draggedElement == null
                || !(_draggedData is BaseDataObject draggedObject)
                || _objectListContainer == null
                || _dropIndicator == null
            )
            {
                CancelDrag();
                return;
            }

            int targetIndex = _dropIndicator.userData is int index ? index : -1;
            if (targetIndex == -1)
            {
                CancelDrag();
                return;
            }

            int currentIndex = _objectListContainer.IndexOf(_draggedElement);
            if (currentIndex == targetIndex || currentIndex + 1 == targetIndex)
            {
                CancelDrag();
                return;
            }

            if (currentIndex < targetIndex)
                targetIndex--;
            targetIndex = Mathf.Clamp(targetIndex, 0, _objectListContainer.childCount - 1);
            _objectListContainer.Insert(targetIndex, _draggedElement);

            // --- Reorder Data (_selectedObjects list) ---
            int oldDataIndex = _selectedObjects.IndexOf(draggedObject);
            if (oldDataIndex >= 0)
            {
                _selectedObjects.RemoveAt(oldDataIndex);
                int dataInsertIndex = targetIndex;
                dataInsertIndex = Mathf.Clamp(dataInsertIndex, 0, _selectedObjects.Count);
                _selectedObjects.Insert(dataInsertIndex, draggedObject);

                // --- Update and Save Object Order (GUIDs) using EditorPrefs ---
                if (_selectedObjects.Count > 0) // Need at least one object to know the Type
                {
                    Type objectType = _selectedObjects[0].GetType(); // Assuming all objects in the list are of the same type
                    UpdateAndSaveObjectOrder(objectType, _selectedObjects);
                }
            }
            else
            {
                Debug.LogError("Dragged object not found in data list!");
            }

            // Cleanup in CancelDrag
        }

        private void UpdateAndSaveObjectOrder(Type objectType, List<BaseDataObject> orderedObjects)
        {
            List<string> orderedGuids = new List<string>();
            foreach (BaseDataObject obj in orderedObjects)
            {
                if (obj == null)
                    continue; // Skip null entries if any
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path))
                {
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    if (!string.IsNullOrEmpty(guid))
                    {
                        orderedGuids.Add(guid);
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"Could not get GUID for object '{obj.name}' at path '{path}'. It might not be saved yet or is not an asset."
                        );
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"Could not get asset path for object '{obj.name}'. It might not be saved yet."
                    );
                }
            }

            if (orderedGuids.Count > 0) // Only save if we have GUIDs
            {
                string prefsKey = string.Format(CustomObjectOrderKeyFormat, objectType.Name);
                try
                {
                    string json = Serializer.JsonStringify(orderedGuids);
                    EditorPrefs.SetString(prefsKey, json);
                    // Debug.Log($"Saved custom object order for type '{objectType.Name}': {json}");
                }
                catch (Exception e)
                {
                    this.LogError(
                        $"Failed to serialize or save custom object order for type '{objectType.Name}'.",
                        e
                    );
                }
            }
            else if (orderedObjects.Count > 0)
            {
                // Clear the pref if all objects failed to get a GUID? Or leave stale?
                // Let's leave stale for now. New objects without GUIDs won't be saved.
                Debug.LogWarning(
                    $"Could not save object order for type '{objectType.Name}' as no valid asset GUIDs were found."
                );
            }
        }

        private void StartDragVisuals(Vector2 currentPosition, string dragText) // << Added dragText parameter
        {
            if (_draggedElement == null || _draggedData == null)
                return;

            // Create Ghost Element
            if (_dragGhost == null)
            {
                _dragGhost = new VisualElement();
                _dragGhost.AddToClassList("drag-ghost");
                // Use a label inside the ghost
                Label ghostLabel = new Label(dragText); // << Use parameter
                ghostLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                _dragGhost.Add(ghostLabel);
                rootVisualElement.Add(_dragGhost);
            }
            else
            {
                // Update text if ghost already exists (e.g., if StartDragVisuals was called multiple times)
                var ghostLabel = _dragGhost.Q<Label>();
                if (ghostLabel != null)
                    ghostLabel.text = dragText; // << Update text
            }

            _dragGhost.style.visibility = Visibility.Visible;
            _dragGhost.style.left = currentPosition.x - (_draggedElement.resolvedStyle.width / 2);
            _dragGhost.style.top = currentPosition.y - (_draggedElement.resolvedStyle.height / 2);
            _dragGhost.BringToFront();

            _draggedElement.style.opacity = 0.5f;

            // Show drop indicator (position calculated in UpdateDropIndicator)
            if (_dropIndicator != null)
            {
                _dropIndicator.style.visibility = Visibility.Visible;
                _dropIndicator.BringToFront();
            }
        }

        private void UpdateDropIndicator(Vector2 pointerPosition)
        {
            VisualElement container = null;
            string itemClassName = null;

            // --- Determine container and item class based on drag type ---
            switch (_activeDragType)
            {
                case DragType.Object:
                    container = _objectListContainer;
                    itemClassName = "object-item"; // Class assigned to object items
                    break;
                case DragType.Namespace:
                    container = _namespaceListContainer;
                    // Ensure this class matches what's assigned in BuildNamespaceView to namespace group containers
                    itemClassName = "object-item"; // Or "namespace-group-item" if you used a specific one
                    break;
                case DragType.Type:
                    // Container is the parent of the dragged type element (the 'typesContainer')
                    if (_draggedElement != null)
                        container = _draggedElement.parent;
                    itemClassName = "type-item"; // Class assigned to type items
                    break;
                default:
                    // If drag type is None or unhandled, hide indicator and exit
                    if (_dropIndicator != null)
                        _dropIndicator.style.visibility = Visibility.Hidden;
                    return;
            }

            // --- Detailed Logging (for debugging container/child count issues) ---
            string containerName = container?.name ?? "null";
            // Get expected counts directly from member fields for comparison
            int expectedNamespaceChildCount = _namespaceListContainer?.childCount ?? -1;
            int expectedObjectChildCount = _objectListContainer?.childCount ?? -1;
            int actualContainerChildCount = container?.childCount ?? -1; // Get count from the variable 'container'

            Debug.Log(
                $"UpdateDropIndicator - DragType: {_activeDragType}, "
                    + $"Container Variable Name: {containerName}, "
                    +
                    // Optional deeper checks:
                    // $"Is Container _namespaceListContainer?: {ReferenceEquals(container, _namespaceListContainer)}, " +
                    // $"Is Container _objectListContainer?: {ReferenceEquals(container, _objectListContainer)}, " +
                    $"_namespaceListContainer?.childCount (Expected): {expectedNamespaceChildCount}, "
                    + $"_objectListContainer?.childCount (Expected): {expectedObjectChildCount}, "
                    + $"container.childCount (Actual): {actualContainerChildCount}"
            );
            // --- End Logging ---


            // --- Validate container and essential elements before proceeding ---
            if (
                container == null
                || _draggedElement == null
                || _dropIndicator == null
                || itemClassName == null
            )
            {
                if (_dropIndicator != null)
                    _dropIndicator.style.visibility = Visibility.Hidden;
                // Log specific failure if container is unexpectedly null for a known drag type
                if (
                    (
                        _activeDragType == DragType.Namespace
                        || _activeDragType == DragType.Object
                        || _activeDragType == DragType.Type
                    )
                    && container == null
                )
                {
                    Debug.LogError(
                        $"UpdateDropIndicator: Container variable is unexpectedly null for DragType: {_activeDragType}!"
                    );
                }
                return;
            }

            // --- Logic for finding drop position relative to items in the container ---
            int childCount = container.childCount; // Use the count from the identified 'container'
            int targetIndex = -1; // The index where the item should be inserted BEFORE
            VisualElement elementBefore = null; // The element the indicator should appear AFTER
            Vector2 localPointerPos = container.WorldToLocal(pointerPosition); // Convert window/panel pointer to container's local space

            // Log if the identified container is empty when member field suggests it shouldn't be
            if (
                (
                    _activeDragType == DragType.Namespace
                    && childCount == 0
                    && expectedNamespaceChildCount > 0
                )
                || (
                    _activeDragType == DragType.Object
                    && childCount == 0
                    && expectedObjectChildCount > 0
                )
            )
            {
                Debug.LogWarning(
                    $"UpdateDropIndicator: container.childCount is 0 for {_activeDragType} drag, but the corresponding member list container has children. Container reference or timing issue suspected."
                );
            }

            // Iterate through the container's children to find the insertion point
            for (int i = 0; i < childCount; ++i)
            {
                VisualElement child = container.ElementAt(i);
                // Skip children that aren't relevant items or the item being dragged
                if (
                    child == _draggedElement
                    || !child.ClassListContains(itemClassName)
                    || child.style.display == DisplayStyle.None
                )
                    continue;

                // Determine if the pointer is above the vertical midpoint of the child
                float childMidY = child.layout.yMin + child.resolvedStyle.height / 2f;
                if (localPointerPos.y < childMidY)
                {
                    targetIndex = i; // Found insert position (BEFORE this child)
                    break; // Exit loop once position is found
                }
                elementBefore = child; // This child is confirmed to be BEFORE the potential drop position
            }

            // --- Handle cases: Pointer below all items, or list empty/only contains dragged item ---
            if (targetIndex == -1) // Loop completed without finding an item to insert before
            {
                if (childCount == 0)
                {
                    targetIndex = 0; // Dropping into an empty list, insert at index 0
                    elementBefore = null;
                }
                else
                {
                    targetIndex = childCount; // Append at the end (index equals current count)
                    // Find the last valid item that isn't the dragged element to place indicator AFTER it
                    elementBefore = null; // Reset first
                    for (int i = childCount - 1; i >= 0; i--)
                    {
                        var child = container.ElementAt(i);
                        // Check if it's a valid item and not the one being dragged
                        if (
                            child != _draggedElement
                            && child.ClassListContains(itemClassName)
                            && child.style.display != DisplayStyle.None
                        )
                        {
                            elementBefore = child;
                            break;
                        }
                    }
                    // If elementBefore is still null here, it implies only the dragged element exists (or no valid items found).
                    // If only the dragged element exists, we should treat it as dropping at the beginning (index 0).
                    if (elementBefore == null)
                    {
                        // Check if the container actually contains the dragged element as its only valid item
                        int validItemCount = 0;
                        bool draggedItemPresent = false;
                        for (int i = 0; i < childCount; ++i)
                        {
                            var child = container.ElementAt(i);
                            if (
                                child.ClassListContains(itemClassName)
                                && child.style.display != DisplayStyle.None
                            )
                            {
                                validItemCount++;
                                if (child == _draggedElement)
                                    draggedItemPresent = true;
                            }
                        }
                        if (validItemCount == 1 && draggedItemPresent)
                        {
                            targetIndex = 0; // Only dragged item exists, drop target is index 0
                        }
                        // If no valid items found at all, targetIndex remains childCount (which might be 0)
                    }
                }
            }

            // --- Calculate indicator's vertical position based on targetIndex and elementBefore ---
            float indicatorY = 0;
            bool placeVisible = false; // Flag to determine if the indicator should be shown

            if (targetIndex == 0) // Target is the very beginning
            {
                // Try to find the first actual item element to place indicator above it
                VisualElement firstVisibleItem = null;
                for (int i = 0; i < childCount; ++i)
                {
                    var child = container.ElementAt(i);
                    if (
                        child.ClassListContains(itemClassName)
                        && child.style.display != DisplayStyle.None
                    )
                    {
                        firstVisibleItem = child;
                        break;
                    }
                }

                if (firstVisibleItem != null)
                {
                    // Place slightly above the top edge of the first item
                    indicatorY =
                        firstVisibleItem.layout.yMin
                        - (_dropIndicator.resolvedStyle.height / 2f)
                        - 1;
                    placeVisible = true;
                }
                else
                {
                    // No visible items found (container might be empty or only contain non-item elements), place at top of container
                    indicatorY = 0;
                    placeVisible = true;
                }
            }
            else if (elementBefore != null) // Target is after 'elementBefore'
            {
                // Place indicator slightly below the bottom edge of 'elementBefore'
                indicatorY =
                    elementBefore.layout.yMax - (_dropIndicator.resolvedStyle.height / 2f) + 1;
                placeVisible = true;
            }
            else
            {
                // Fallback scenario: targetIndex > 0 but elementBefore is null (should be rare).
                // Hide the indicator if position is uncertain.
                // Debug.LogWarning($"UpdateDropIndicator: Uncertain indicator position calculation. targetIndex={targetIndex}, elementBefore=null, childCount={childCount}");
                placeVisible = false;
            }

            // --- Set Indicator Parent, Position, Visibility, and Store Target Index ---
            if (placeVisible)
            {
                // Determine the correct parent element for the indicator visual
                VisualElement indicatorParent = null;
                switch (_activeDragType)
                {
                    case DragType.Object:
                        // Place inside the ScrollView's content container for objects
                        indicatorParent = _objectScrollView?.contentContainer;
                        break;
                    case DragType.Namespace:
                        // Place directly inside the namespace list container
                        indicatorParent = _namespaceListContainer;
                        break;
                    case DragType.Type:
                        // Place directly inside the types container (which is 'container' in this case)
                        indicatorParent = container;
                        break;
                }

                // Ensure indicator is added to the hierarchy under the correct parent
                if (indicatorParent != null && _dropIndicator.parent != indicatorParent)
                {
                    // Add will handle reparenting if it's already elsewhere
                    indicatorParent.Add(_dropIndicator);
                }
                else if (indicatorParent == null)
                {
                    // Could not determine parent, hide indicator
                    placeVisible = false;
                    Debug.LogWarning(
                        $"UpdateDropIndicator: Could not determine indicator parent for DragType {_activeDragType}. Hiding indicator."
                    );
                }

                // Final check before showing and positioning
                if (placeVisible && indicatorParent != null)
                {
                    // Clamp Y position within the bounds of the determined parent container
                    float parentHeight = float.IsNaN(indicatorParent.layout.height)
                        ? 0
                        : indicatorParent.layout.height; // Handle potential NaN layout height
                    _dropIndicator.style.top = Mathf.Clamp(indicatorY, 0, parentHeight);
                    _dropIndicator.style.visibility = Visibility.Visible;
                    _dropIndicator.BringToFront(); // Ensure it's visually on top within its current parent
                    _dropIndicator.userData = targetIndex; // Store calculated index for use in PerformDrop methods
                }
                else
                {
                    // Hide if parenting failed or placeVisible became false during parenting logic
                    _dropIndicator.style.visibility = Visibility.Hidden;
                    _dropIndicator.userData = -1; // Invalidate stored index
                }
            }
            else // placeVisible was false from the position calculation phase
            {
                _dropIndicator.style.visibility = Visibility.Hidden;
                _dropIndicator.userData = -1; // Invalidate stored index
            }
        }

        private void UpdateAndSaveObjectCustomOrder()
        {
            List<Object> dirtyObjects = new List<Object>();
            for (int i = 0; i < _selectedObjects.Count; i++)
            {
                BaseDataObject obj = _selectedObjects[i];
                if (obj == null)
                    continue;

                // Assign 1-based order index. All items in the list get a positive order.
                int newOrder = i + 1;
                if (obj._customOrder != newOrder)
                {
                    obj._customOrder = newOrder;
                    EditorUtility.SetDirty(obj);
                    dirtyObjects.Add(obj);
                    // Debug.Log($"Set {obj.name} custom order to {newOrder}");
                }
            }

            if (dirtyObjects.Count > 0)
            {
                // Save all modified assets
                AssetDatabase.SaveAssets();
                // Debug.Log($"Saved custom order for {dirtyObjects.Count} objects.");
            }
        }

        private void UpdateAndSaveCustomOrder()
        {
            List<Object> dirtyObjects = new List<Object>();
            for (int i = 0; i < _selectedObjects.Count; i++)
            {
                BaseDataObject obj = _selectedObjects[i];
                int newOrder = i + 1; // Use 1-based index for custom order
                if (obj._customOrder != newOrder)
                {
                    obj._customOrder = newOrder;
                    EditorUtility.SetDirty(obj);
                    dirtyObjects.Add(obj);
                    // Debug.Log($"Set {obj.name} custom order to {newOrder}");
                }
            }

            if (dirtyObjects.Count > 0)
            {
                // Save all modified assets
                AssetDatabase.SaveAssets();
                // Optional: Refresh Asset Database view if needed, though usually SaveAssets is enough
                // AssetDatabase.Refresh();
                // Debug.Log($"Saved custom order for {dirtyObjects.Count} objects.");
            }
        }

        private void CancelDrag() // No longer needs pointerId parameter
        {
            // Debug.Log("CancelDrag called.");

            // Restore visual appearance
            if (_draggedElement != null)
            {
                _draggedElement.style.opacity = 1.0f;
                // No need to release pointer here - OnCapturedPointerUp handles it.
            }

            // Hide drag visuals
            if (_dragGhost != null)
                _dragGhost.style.visibility = Visibility.Hidden;
            if (_dropIndicator != null)
                _dropIndicator.style.visibility = Visibility.Hidden;

            // Reset state variables - CRITICAL
            _isDragging = false;
            _draggedElement = null;
            _draggedData = null;
            _activeDragType = DragType.None;

            // Debug.Log("CancelDrag finished. State reset.");
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

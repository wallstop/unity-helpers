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

        // Add near other drag state variables
        private VisualElement _inPlaceGhost = null;
        private int _lastGhostInsertIndex = -1; // Track where the in-place ghost was last put
        private VisualElement _lastGhostParent = null; // Track which container it was in
        private VisualElement _draggedElement;
        private BaseDataObject _draggedObject;
        private VisualElement _dragGhost;
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
            _dragGhost = null;
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
                UpdateDragTargeting(evt.position);
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
            // Retrieve target index from where the in-place ghost was visually placed
            int targetIndex = (_inPlaceGhost?.userData is int index) ? index : -1;
            // Debug.Log($"PerformNamespaceDrop - Target Index from InPlaceGhost: {targetIndex}");

            // Validate essential elements and data
            if (
                _draggedElement == null
                || !(_draggedData is string draggedKey)
                || _namespaceListContainer == null
            )
            {
                // Cleanup is handled by OnCapturedPointerUp's finally block calling CancelDrag
                return;
            }

            if (targetIndex == -1)
            {
                Debug.LogWarning(
                    "PerformNamespaceDrop: Invalid target index (-1) retrieved from ghost. Aborting drop."
                );
                // Cleanup is handled by OnCapturedPointerUp's finally block calling CancelDrag
                return;
            }

            // --- Reorder Visual Element (Place ORIGINAL element) ---
            int maxIndex = _namespaceListContainer.childCount; // Max index for insertion
            targetIndex = Mathf.Clamp(targetIndex, 0, maxIndex);

            // Make original element visible again before inserting
            _draggedElement.style.display = DisplayStyle.Flex;

            // Insert original element at the target visual index
            _namespaceListContainer.Insert(targetIndex, _draggedElement);

            // --- Reorder Data (_scriptableObjectTypes list and save to EditorPrefs) ---
            int oldDataIndex = _scriptableObjectTypes.FindIndex(kvp => kvp.key == draggedKey);
            if (oldDataIndex >= 0)
            {
                var draggedItem = _scriptableObjectTypes[oldDataIndex];
                _scriptableObjectTypes.RemoveAt(oldDataIndex);

                // The visual targetIndex directly corresponds to the desired data index
                int dataInsertIndex = targetIndex;
                dataInsertIndex = Mathf.Clamp(dataInsertIndex, 0, _scriptableObjectTypes.Count); // Clamp against current data list size
                _scriptableObjectTypes.Insert(dataInsertIndex, draggedItem);

                // Update and Save Namespace Order to EditorPrefs
                UpdateAndSaveNamespaceOrder();
            }
            else
            {
                Debug.LogError(
                    $"PerformNamespaceDrop: Dragged namespace key '{draggedKey}' not found in data list!"
                );
            }

            // Note: Cleanup (_inPlaceGhost removal, state reset) happens in OnCapturedPointerUp/CancelDrag
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
            // Retrieve target index from where the in-place ghost was visually placed
            int targetIndex = (_inPlaceGhost?.userData is int index) ? index : -1;
            // Debug.Log($"PerformTypeDrop - Target Index from InPlaceGhost: {targetIndex}");

            // Validate essential elements and data
            VisualElement typesContainer = _draggedElement?.parent; // Parent should be the types container
            string namespaceKey = typesContainer?.userData as string; // Get namespace key from container

            if (
                _draggedElement == null
                || !(_draggedData is Type draggedType)
                || typesContainer == null
                || string.IsNullOrEmpty(namespaceKey)
            )
            {
                Debug.LogError(
                    "PerformTypeDrop: Missing dragged element, type data, parent container, or namespace key."
                );
                // Cleanup is handled by OnCapturedPointerUp's finally block calling CancelDrag
                return;
            }

            if (targetIndex == -1)
            {
                Debug.LogWarning(
                    "PerformTypeDrop: Invalid target index (-1) retrieved from ghost. Aborting drop."
                );
                // Cleanup is handled by OnCapturedPointerUp's finally block calling CancelDrag
                return;
            }

            // --- Reorder Visual Element (Place ORIGINAL element) ---
            int maxIndex = typesContainer.childCount; // Max index for insertion
            targetIndex = Mathf.Clamp(targetIndex, 0, maxIndex);

            // Make original element visible again before inserting
            _draggedElement.style.display = DisplayStyle.Flex;

            // Insert original element at the target visual index within its parent container
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

                    // Visual targetIndex corresponds to data index
                    int dataInsertIndex = targetIndex;
                    dataInsertIndex = Mathf.Clamp(dataInsertIndex, 0, typesList.Count); // Clamp against current data list size
                    typesList.Insert(dataInsertIndex, draggedType);

                    // Update and Save Type Order for this Namespace to EditorPrefs
                    UpdateAndSaveTypeOrder(namespaceKey, typesList);
                }
                else
                {
                    Debug.LogError(
                        $"PerformTypeDrop: Dragged type '{draggedType.Name}' not found in data list for namespace '{namespaceKey}'!"
                    );
                }
            }
            else
            {
                Debug.LogError(
                    $"PerformTypeDrop: Namespace key '{namespaceKey}' not found in _scriptableObjectTypes!"
                );
            }

            // Note: Cleanup (_inPlaceGhost removal, state reset) happens in OnCapturedPointerUp/CancelDrag
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
            int targetIndex = (_inPlaceGhost?.userData is int index) ? index : -1;

            if (
                _draggedElement == null
                || !(_draggedData is BaseDataObject draggedObject)
                || _objectListContainer == null
            )
            {
                CancelDrag();
                return;
            }

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

            int maxIndex = _objectListContainer.childCount; // Current count is max insert index
            targetIndex = Mathf.Clamp(targetIndex, 0, maxIndex);

            // Make original element visible again before inserting
            _draggedElement.style.display = DisplayStyle.Flex; // Or Visible if using visibility property

            // Insert original element at the target index
            _objectListContainer.Insert(targetIndex, _draggedElement);

            // --- Reorder Data (_selectedObjects list) ---
            int oldDataIndex = _selectedObjects.IndexOf(draggedObject);
            if (oldDataIndex >= 0)
            {
                _selectedObjects.RemoveAt(oldDataIndex);
                // dataInsertIndex should match the visual targetIndex where it was placed
                int dataInsertIndex = targetIndex;
                // If we inserted visually *after* the original data index, the data index needs adjustment
                // because the list is one shorter *before* insertion compared to visual list *after* insertion.
                // Example: [A, B(dragged), C] -> Drag B after C -> Visual Insert Index = 2. Data list [A, C]. Insert B at index 2 -> [A, C, B] Correct.
                // Example: [A, B, C(dragged)] -> Drag C before B -> Visual Insert Index = 1. Data list [A, B]. Insert C at index 1 -> [A, C, B] Correct.
                // Example: [A(dragged), B, C] -> Drag A after C -> Visual Insert Index = 2. Data list [B, C]. Insert A at index 2 -> [B, C, A] Correct.
                dataInsertIndex = Mathf.Clamp(dataInsertIndex, 0, _selectedObjects.Count); // Clamp against current data list size
                _selectedObjects.Insert(dataInsertIndex, draggedObject);

                // Update persistence (_customOrder)
                UpdateAndSaveObjectCustomOrder();
            }
            else
            {
                Debug.LogError("PerformObjectDrop: Dragged object not found in data list!");
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
                _dragGhost = new VisualElement() { name = "drag-ghost-cursor" };
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

            if (_inPlaceGhost == null)
            {
                try
                {
                    // 1. Create the basic element
                    _inPlaceGhost = new VisualElement();
                    _inPlaceGhost.name = "drag-ghost-inplace";

                    // 2. Copy relevant style classes from the original element
                    //    (Assumes classes define the general look)
                    foreach (var className in _draggedElement.GetClasses())
                    {
                        _inPlaceGhost.AddToClassList(className);
                    }
                    // 3. Add the specific styling class for the ghost effect (opacity etc.)
                    _inPlaceGhost.AddToClassList("in-place-ghost");

                    // 4. Copy size and margins to ensure correct layout spacing
                    //    Using resolvedStyle ensures we get the computed values.
                    _inPlaceGhost.style.height = _draggedElement.resolvedStyle.height;
                    // Width might often be determined by flexbox/parent, but copy if fixed.
                    // _inPlaceGhost.style.width = _draggedElement.resolvedStyle.width;
                    _inPlaceGhost.style.marginTop = _draggedElement.resolvedStyle.marginTop;
                    _inPlaceGhost.style.marginBottom = _draggedElement.resolvedStyle.marginBottom;
                    _inPlaceGhost.style.marginLeft = _draggedElement.resolvedStyle.marginLeft;
                    _inPlaceGhost.style.marginRight = _draggedElement.resolvedStyle.marginRight;

                    // 5. Recreate essential children (e.g., the primary Label)
                    //    Query the original element for its label
                    var originalLabel =
                        _draggedElement.Q<Label>(className: "object-item__label") // Be specific if possible
                        ?? _draggedElement.Q<Label>(className: "type-item__label")
                        ?? _draggedElement.Q<Label>(); // Fallback to first label

                    if (originalLabel != null)
                    {
                        var ghostLabel = new Label(originalLabel.text);
                        // Copy label's classes for consistent text styling
                        foreach (var className in originalLabel.GetClasses())
                        {
                            ghostLabel.AddToClassList(className);
                        }
                        ghostLabel.pickingMode = PickingMode.Ignore; // Ensure label is not interactive
                        _inPlaceGhost.Add(ghostLabel);
                    }
                    else
                    {
                        // Fallback: If no specific label found, maybe add the generic dragText
                        var fallbackLabel = new Label(dragText);
                        fallbackLabel.pickingMode = PickingMode.Ignore;
                        _inPlaceGhost.Add(fallbackLabel);
                    }
                    // Note: This does NOT copy complex child hierarchies. Adapt if your items are more complex.

                    // 6. Set essential properties for the ghost role
                    _inPlaceGhost.pickingMode = PickingMode.Ignore; // Must not interfere with drop target detection
                    _inPlaceGhost.style.visibility = Visibility.Hidden; // Hide initially
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error creating in-place ghost: {ex}");
                    _inPlaceGhost = null; // Ensure it's null if creation failed
                }
            }
            _lastGhostInsertIndex = -1; // Reset insert tracking
            _lastGhostParent = null;

            _draggedElement.style.display = DisplayStyle.None; // Remove original from layout
        }

        private void UpdateDragTargeting(Vector2 pointerPosition)
        {
            VisualElement container = null;
            string itemClassName = null;
            VisualElement indicatorParent = null; // Parent where the in-place ghost should live

            // --- Determine container, item class, and indicator parent ---
            switch (_activeDragType)
            {
                case DragType.Object:
                    container = _objectListContainer;
                    itemClassName = "object-item";
                    indicatorParent = _objectListContainer; // Ghost goes directly in list
                    break;
                case DragType.Namespace:
                    container = _namespaceListContainer;
                    itemClassName = "object-item"; // Or "namespace-group-item"
                    indicatorParent = _namespaceListContainer; // Ghost goes directly in list
                    break;
                case DragType.Type:
                    if (_draggedElement != null)
                        container = _draggedElement.parent;
                    itemClassName = "type-item";
                    indicatorParent = container; // Ghost goes directly in the types container
                    break;
                default:
                    // Hide and remove in-place ghost if drag type is invalid
                    if (_inPlaceGhost != null)
                    {
                        _inPlaceGhost.style.visibility = Visibility.Hidden;
                        _inPlaceGhost.RemoveFromHierarchy(); // Ensure removal
                        _lastGhostInsertIndex = -1;
                        _lastGhostParent = null;
                    }
                    return;
            }

            // Basic validation
            if (
                container == null
                || _draggedElement == null
                || itemClassName == null
                || indicatorParent == null
                || _inPlaceGhost == null
            )
            {
                // Hide and remove in-place ghost if something is wrong
                if (_inPlaceGhost != null)
                {
                    _inPlaceGhost.style.visibility = Visibility.Hidden;
                    _inPlaceGhost.RemoveFromHierarchy();
                    _lastGhostInsertIndex = -1;
                    _lastGhostParent = null;
                }
                return;
            }

            // --- Calculate Target Index (logic reused from UpdateDropIndicator) ---
            int childCount = container.childCount;
            int targetIndex = -1;
            VisualElement elementBefore = null;
            Vector2 localPointerPos = container.WorldToLocal(pointerPosition);

            // Iterate through children to find insert position (ignoring original element which is hidden)
            for (int i = 0; i < childCount; ++i)
            {
                VisualElement child = container.ElementAt(i);
                // Skip non-items, the in-place ghost itself, or hidden elements (like original)
                if (
                    !child.ClassListContains(itemClassName)
                    || child == _inPlaceGhost
                    || child.style.display == DisplayStyle.None
                )
                    continue;

                float childMidY = child.layout.yMin + child.resolvedStyle.height / 2f;
                if (localPointerPos.y < childMidY)
                {
                    targetIndex = i;
                    break;
                }
                elementBefore = child;
            }

            if (targetIndex == -1) // Below all items or list empty/only contained ghost
            {
                if (childCount == 0 || (childCount == 1 && container.Contains(_inPlaceGhost)))
                {
                    targetIndex = 0; // Insert at beginning if empty or only ghost present
                }
                else
                {
                    targetIndex = container.childCount; // Append at the end (adjusting for potential ghost presence)
                    if (container.Contains(_inPlaceGhost))
                        targetIndex--; // If ghost is present, target index is before it if appending
                    targetIndex = Math.Max(0, targetIndex); // Ensure not negative
                }
            }
            // --- End Target Index Calculation ---


            // --- Manage In-Place Ghost ---
            bool targetIndexValid = targetIndex >= 0;

            if (targetIndexValid)
            {
                // Clamp index just in case calculation went out of bounds
                int maxIndex = indicatorParent.childCount; // Max index is current count
                targetIndex = Mathf.Clamp(targetIndex, 0, maxIndex);

                // Check if position needs update (different index or different parent)
                if (targetIndex != _lastGhostInsertIndex || indicatorParent != _lastGhostParent)
                {
                    // Debug.Log($"Updating in-place ghost. New Index: {targetIndex}, Old Index: {_lastGhostInsertIndex}, Parent: {indicatorParent.name}");

                    // Remove from old position first (if any)
                    // Note: Add/Insert automatically handles reparenting if needed, but explicit remove can be clearer
                    _inPlaceGhost.RemoveFromHierarchy();

                    // Insert into the correct parent at the new index
                    indicatorParent.Insert(targetIndex, _inPlaceGhost);

                    // Make sure it's visible
                    _inPlaceGhost.style.visibility = Visibility.Visible;

                    // Update tracking
                    _lastGhostInsertIndex = targetIndex;
                    _lastGhostParent = indicatorParent;
                }
                else
                {
                    // Index and parent are the same, ensure it's visible
                    _inPlaceGhost.style.visibility = Visibility.Visible;
                }
            }
            else // Target index calculation failed, hide the ghost
            {
                if (_inPlaceGhost.parent != null) // Only remove if it's in the hierarchy
                {
                    // Debug.Log($"Hiding in-place ghost. Invalid Target Index.");
                    _inPlaceGhost.style.visibility = Visibility.Hidden;
                    _inPlaceGhost.RemoveFromHierarchy();
                }
                _lastGhostInsertIndex = -1;
                _lastGhostParent = null;
            }

            // Store the calculated valid target index on the ghost itself (or dragged element)
            // so PerformDrop methods can easily retrieve it without recalculating.
            // Using userData on _inPlaceGhost is convenient.
            _inPlaceGhost.userData = targetIndex; // Store the index where the ghost IS currently placed
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
                _draggedElement.style.display = DisplayStyle.Flex; // Restore display
                _draggedElement.style.opacity = 1.0f; // Restore opacity just in case
                // No pointer release needed here
            }

            if (_dragGhost != null)
                _dragGhost.style.visibility = Visibility.Hidden;

            // Remove in-place ghost from hierarchy
            if (_inPlaceGhost != null)
            {
                _inPlaceGhost.RemoveFromHierarchy();
                // Optional: Could pool ghosts instead of destroying/nulling if performance is critical
                _inPlaceGhost = null;
            }
            _lastGhostInsertIndex = -1; // Reset tracking
            _lastGhostParent = null;

            // Reset state variables
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

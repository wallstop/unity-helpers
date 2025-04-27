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
            rootVisualElement.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);
            rootVisualElement.RegisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
        }

        private void OnDisable()
        {
            Cleanup();
            rootVisualElement?.UnregisterCallback<PointerUpEvent>(OnGlobalPointerUp);
            rootVisualElement?.UnregisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
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
            if (
                targetElement == null
                || targetElement.userData == null
                || !(targetElement.userData is BaseDataObject)
            )
            {
                Debug.LogWarning("PointerDown target is not a valid object item.");
                return;
            }

            // --- Handle Selection ---
            BaseDataObject clickedObject = targetElement.userData as BaseDataObject;
            if (_selectedObject != clickedObject)
            {
                SelectObject(clickedObject);
            }

            // --- Initiate Drag ---
            // Check for left mouse button to start drag
            if (evt.button == 0) // 0 = Left mouse button
            {
                _draggedElement = targetElement;
                _draggedObject = clickedObject;
                _dragStartPosition = evt.position; // Use event position relative to window/panel
                //_draggedElement.CapturePointer(evt.pointerId); // Capture pointer for drag events
                _isDragging = false; // Set to true only after a minimum move distance if desired
                // Don't set isDragging = true yet, wait for PointerMove to confirm drag intent
            }

            evt.StopPropagation(); // Prevent event from bubbling up further if needed
        }

        private void OnGlobalPointerMove(PointerMoveEvent evt)
        {
            if (_draggedElement == null || !_draggedElement.HasPointerCapture(evt.pointerId))
            {
                return; // Not dragging or lost capture
            }

            // Check if we've moved enough to consider it a drag
            if (!_isDragging)
            {
                // Simple distance check (adjust threshold as needed)
                if (Vector2.Distance(evt.position, _dragStartPosition) > 5.0f)
                {
                    _isDragging = true;
                    StartDragVisuals(evt.position); // Create ghost etc.
                }
                else
                {
                    return; // Not dragging yet
                }
            }

            // --- Update Drag Visuals ---
            if (_dragGhost != null)
            {
                // Position ghost relative to the rootVisualElement
                _dragGhost.style.left = evt.position.x - (_dragGhost.resolvedStyle.width / 2);
                _dragGhost.style.top = evt.position.y - (_dragGhost.resolvedStyle.height / 2);
            }

            // --- Determine Drop Target and Update Indicator ---
            UpdateDropIndicator(evt.position);

            evt.StopPropagation();
        }

        private void OnGlobalPointerUp(PointerUpEvent evt)
        {
            // Check if we were actually dragging with the pointer that was released
            if (
                _draggedElement == null
                || !_draggedElement.HasPointerCapture(evt.pointerId)
                || _activeDragType == DragType.None
            )
            {
                // This pointer wasn't the one we were tracking for a drag.
                // It might be a right-click release or something else. Ignore it for drag purposes.
                return;
            }

            // Store necessary info before potential modification in CancelDrag
            bool wasDragging = _isDragging;
            DragType finishedDragType = _activeDragType;
            VisualElement releasedElement = _draggedElement; // Keep ref for logging if needed
            int pointerId = evt.pointerId;

            // --- CRITICAL: Release the pointer and cleanup state in a finally block ---
            try
            {
                // 1. Release Pointer Immediately
                // Debug.Log($"Releasing Pointer {pointerId} from {_draggedElement.name}");
                _draggedElement.ReleasePointer(pointerId);

                // 2. Perform Drop Logic if we were actually dragging
                if (wasDragging)
                {
                    // Debug.Log($"Performing drop for {finishedDragType}...");
                    // --- Finalize Drop based on type ---
                    switch (finishedDragType)
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
                    // Debug.Log($"Drop performed for {finishedDragType}.");
                }
                // else: It was just a click, selection logic might have happened on PointerDown/Up on the item.
            }
            catch (Exception ex)
            {
                // Log any error during drop logic
                Debug.LogError($"Error during drop execution for {finishedDragType}: {ex}");
            }
            finally
            {
                // 3. ALWAYS Reset State via CancelDrag
                // Pass the specific pointer ID for context, though CancelDrag might not strictly need it now.
                // We reset state *after* potentially using it in the drop logic.
                // Debug.Log($"Calling CancelDrag from finally block.");
                CancelDrag(pointerId);
            }

            // We handled the drag event, stop it from propagating further.
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

            // Adjust target index if moving item downwards
            if (currentIndex < targetIndex)
            {
                targetIndex--;
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
                //_draggedElement.CapturePointer(evt.pointerId);
                _isDragging = false; // Set to true only after movement threshold
            }

            evt.StopPropagation();
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
                //_draggedElement.CapturePointer(evt.pointerId);
                _isDragging = false;
            }
            evt.StopPropagation();
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
            if (currentIndex < targetIndex)
                targetIndex--;
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

        private void StartDragVisuals(Vector2 currentPosition)
        {
            if (_draggedElement == null || _draggedObject == null)
                return;

            // Create Ghost Element
            if (_dragGhost == null)
            {
                _dragGhost = new VisualElement();
                _dragGhost.AddToClassList("drag-ghost");
                // Add a label to the ghost for better visual feedback
                Label ghostLabel = new Label(_draggedObject.Title);
                ghostLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                _dragGhost.Add(ghostLabel);
                // Add to root so it can be positioned anywhere
                rootVisualElement.Add(_dragGhost);
            }

            _dragGhost.style.visibility = Visibility.Visible;
            _dragGhost.style.left = currentPosition.x - (_draggedElement.resolvedStyle.width / 2);
            _dragGhost.style.top = currentPosition.y - (_draggedElement.resolvedStyle.height / 2);
            _dragGhost.BringToFront(); // Ensure ghost is on top

            // Hide original element slightly
            _draggedElement.style.opacity = 0.5f;

            // Show drop indicator (it will be positioned in PointerMove)
            _dropIndicator.style.visibility = Visibility.Visible;
            _dropIndicator.BringToFront(); // Ensure indicator is visible over items
        }

        private void UpdateDropIndicator(Vector2 pointerPosition)
        {
            if (_objectListContainer == null || _draggedElement == null || _dropIndicator == null)
                return;

            int childCount = _objectListContainer.childCount;
            if (childCount == 0)
            {
                _dropIndicator.style.visibility = Visibility.Hidden;
                return;
            }

            // Convert pointer position to the object list container's local space
            Vector2 localPointerPos = _objectListContainer.WorldToLocal(pointerPosition);

            int targetIndex = -1;
            VisualElement elementBefore = null; // The element the indicator should appear after

            // Iterate through children to find where the pointer is relative to them
            for (int i = 0; i < childCount; ++i)
            {
                VisualElement child = _objectListContainer.ElementAt(i);
                if (child == _draggedElement || !child.ClassListContains("object-item"))
                    continue; // Skip dragged item and non-items

                float childMidY = child.layout.yMin + child.resolvedStyle.height / 2f;

                if (localPointerPos.y < childMidY)
                {
                    targetIndex = i; // Found insert position before this child
                    break;
                }
                elementBefore = child; // This child is before the potential drop position
            }

            // If pointer is below all elements, insert at the end
            if (targetIndex == -1)
            {
                targetIndex = childCount;
                elementBefore = _objectListContainer.ElementAt(childCount - 1); // Place after the last valid item
                // Ensure we don't target the dragged element itself if it's last
                if (elementBefore == _draggedElement && childCount > 1)
                {
                    elementBefore = _objectListContainer.ElementAt(childCount - 2);
                }
                else if (elementBefore == _draggedElement && childCount <= 1)
                {
                    elementBefore = null; // Drop at the beginning if only dragging the single element
                    targetIndex = 0;
                }
            }

            // Calculate indicator position
            float indicatorY;
            if (targetIndex == 0)
            {
                // Place indicator at the top of the container or first element
                VisualElement firstChild = _objectListContainer.ElementAt(0);
                indicatorY =
                    (firstChild == _draggedElement && childCount > 1)
                        ? _objectListContainer.ElementAt(1).layout.yMin
                            - (_dropIndicator.resolvedStyle.height / 2f)
                            - 1 // Above second element
                        : firstChild.layout.yMin - (_dropIndicator.resolvedStyle.height / 2f) - 1; // Above first element
            }
            else if (elementBefore != null)
            {
                // Place indicator below the elementBefore
                indicatorY =
                    elementBefore.layout.yMax - (_dropIndicator.resolvedStyle.height / 2f) + 1;
            }
            else
            {
                // Fallback or case where list might be empty during calculation
                indicatorY = 0;
            }

            _dropIndicator.style.top = Mathf.Clamp(
                indicatorY,
                0,
                _objectListContainer.contentContainer.layout.height
            ); // Clamp within bounds
            _dropIndicator.style.visibility = Visibility.Visible;

            // Store target index in userData for PerformDrop to use
            _dropIndicator.userData = targetIndex;
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

        private void CancelDrag(int pointerIdContext = -1)
        {
            // Debug.Log($"CancelDrag called (Context Pointer ID: {pointerIdContext})");

            // Restore visual appearance of the element that was dragged (if any)
            if (_draggedElement != null)
            {
                _draggedElement.style.opacity = 1.0f;

                // Attempting release here is mostly a fallback/safety net,
                // as the primary release should happen in OnGlobalPointerUp's try block.
                // Check if it *still* has capture for any reason (e.g., error before release in Up handler)
                if (_draggedElement.HasPointerCapture(-1)) // Check for *any* pointer capture
                {
                    Debug.LogWarning(
                        $"CancelDrag found lingering pointer capture on {_draggedElement.name}. Releasing (-1)."
                    );
                    _draggedElement.ReleasePointer(-1); // Release any pointer it might still hold
                }
            }

            // Hide drag visuals
            if (_dragGhost != null)
                _dragGhost.style.visibility = Visibility.Hidden;
            if (_dropIndicator != null)
                _dropIndicator.style.visibility = Visibility.Hidden;

            // Reset state variables - CRITICAL
            _isDragging = false;
            _draggedElement = null; // Allows garbage collection
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

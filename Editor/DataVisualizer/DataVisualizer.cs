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
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.DataVisualizer;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Core.Serialization;
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

            const string namespaceItemClass = "object-item";
            const string namespaceLabelClass = "object-item__label";
            const string typeItemClass = "type-item";
            const string typeLabelClass = "type-item__label";

            foreach ((string key, List<Type> types) in _scriptableObjectTypes)
            {
                VisualElement namespaceGroupItem = new VisualElement()
                {
                    name = $"namespace-group-{key}",
                };

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
                _draggedElement.CapturePointer(evt.pointerId); // Capture pointer for drag events
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
            if (_draggedElement == null || !_draggedElement.HasPointerCapture(evt.pointerId))
            {
                return; // Not the pointer we captured
            }

            _draggedElement.ReleasePointer(evt.pointerId); // Release capture

            if (_isDragging)
            {
                // --- Finalize Drop ---
                //PerformDrop();
            }
            else
            {
                // If not dragging, it was just a click (selection handled in PointerDown)
            }

            // --- Cleanup Drag State ---
            CancelDrag();
            evt.StopPropagation();
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

            // --- Reorder Visual Element (remains the same) ---
            _objectListContainer.Insert(targetIndex, _draggedElement);

            // --- Reorder Data (_selectedObjects list - remains the same) ---
            int oldDataIndex = _selectedObjects.IndexOf(draggedObject);
            if (oldDataIndex >= 0)
            {
                _selectedObjects.RemoveAt(oldDataIndex);
                int dataInsertIndex = targetIndex;
                dataInsertIndex = Mathf.Clamp(dataInsertIndex, 0, _selectedObjects.Count);
                _selectedObjects.Insert(dataInsertIndex, draggedObject);

                // --- Update _customOrder and Save Assets ---
                UpdateAndSaveObjectCustomOrder(); // Call the method to handle _customOrder persistence
            }
            else
            {
                Debug.LogError("Dragged object not found in data list!");
            }

            // Cleanup happens in CancelDrag
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

        private void CancelDrag()
        {
            if (_draggedElement != null)
            {
                _draggedElement.style.opacity = 1.0f; // Restore original opacity
                if (_draggedElement.HasPointerCapture(-1)) // Check general capture just in case specific ID isn't known
                    _draggedElement.ReleasePointer(-1);
            }
            if (_dragGhost != null)
                _dragGhost.style.visibility = Visibility.Hidden;
            if (_dropIndicator != null)
                _dropIndicator.style.visibility = Visibility.Hidden;

            _isDragging = false;
            _draggedElement = null;
            _draggedObject = null;
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

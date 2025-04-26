namespace UnityHelpers.Editor
{
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
#endif

#if UNITY_EDITOR
    public sealed class DataVisualizer : EditorWindow
    {
        private const string CustomTypeOrderKey =
            "WallstopStudios.UnityHelpers.DataVisualizer.CustomTypeOrder";
        private const string CustomNamespaceOrderKey =
            "WallstopStudios.UnityHelpers.DataVisualizer.CustomNamespaceOrder";

        private Vector2 _objectScrollPosition;
        private Vector2 _dataObjectScrollPosition;

        private readonly List<(string key, List<Type> types)> _scriptableObjectTypes = new();
        private readonly Dictionary<BaseDataObject, VisualElement> _objectVisualElementMap = new();
        private readonly List<BaseDataObject> _selectedObjects = new();
        private BaseDataObject _selectedObject;

        private VisualElement _namespaceListContainer;
        private VisualElement _objectListContainer;
        private VisualElement _inspectorContainer;
        private ScrollView _objectScrollView;
        private ScrollView _inspectorScrollView;
        private IVisualElementScheduledItem _odinRepaintSchedule;

#if ODIN_INSPECTOR
        private PropertyTree _odinPropertyTree;
        private IMGUIContainer _odinInspectorContainer;
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
            _objectScrollPosition = Vector2.zero;
            _dataObjectScrollPosition = Vector2.zero;
#if ODIN_INSPECTOR
            _odinPropertyTree = null;
#endif
            LoadScriptableObjectTypes();
            Undo.undoRedoPerformed += UpdateVisuals;
        }

        private void OnDisable()
        {
#if ODIN_INSPECTOR
            // Unhook from the current tree when the window is disabled/closed
            if (_odinPropertyTree != null)
            {
                _odinPropertyTree.OnPropertyValueChanged -= HandleOdinPropertyValueChanged;
            }
#endif
            Undo.undoRedoPerformed -= UpdateVisuals;
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();

            var mainContainer = new VisualElement()
            {
                name = "main-container",
                style = { flexGrow = 1, flexDirection = FlexDirection.Row },
            };
            root.Add(mainContainer);

            // --- Column 1: Namespaces ---
            var namespaceColumn = new VisualElement()
            {
                name = "namespace-column",
                style =
                {
                    width = 200,
                    borderRightWidth = 1,
                    borderRightColor = Color.gray,
                }, // Fixed width
            };
            var namespaceScrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "namespace-scrollview",
            };
            namespaceScrollView.style.flexGrow = 1;
            _namespaceListContainer = new VisualElement() { name = "namespace-list" };
            namespaceScrollView.Add(_namespaceListContainer);
            namespaceColumn.Add(
                new Label("Namespaces")
                {
                    style = { unityFontStyleAndWeight = FontStyle.Bold, paddingBottom = 5 },
                }
            );
            namespaceColumn.Add(namespaceScrollView);
            mainContainer.Add(namespaceColumn);

            // --- Column 2: Objects ---
            var objectColumn = new VisualElement()
            {
                name = "object-column",
                style =
                {
                    width = 200,
                    borderRightWidth = 1,
                    borderRightColor = Color.gray,
                }, // Fixed width
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
            };
            _objectScrollView.style.flexGrow = 1; // Allow scroll view to fill column
            _objectListContainer = new VisualElement() { name = "object-list" }; // Container for buttons
            _objectScrollView.Add(_objectListContainer);
            objectColumn.Add(_objectScrollView);
            mainContainer.Add(objectColumn);

            // --- Column 3: Inspector ---
            var inspectorColumn = new VisualElement()
            {
                name = "inspector-column",
                style = { flexGrow = 1 }, // Takes remaining width
            };
            _inspectorScrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "inspector-scrollview",
            };
            _inspectorScrollView.style.flexGrow = 1;
            _inspectorContainer = new VisualElement() { name = "inspector-content" }; // Container for PropertyFields or Odin
            _inspectorScrollView.Add(_inspectorContainer);
            inspectorColumn.Add(_inspectorScrollView);
            mainContainer.Add(inspectorColumn);

            // --- Initial Population ---
            BuildNamespaceView(); // Populate the first column
            BuildObjectsView(); // Populate the second column (initially empty)
            BuildInspectorView(); // Populate the third column (initially empty)
        }

        private void UpdateVisuals()
        {
            if (_objectListContainer == null || _objectVisualElementMap is not { Count: > 0 })
            {
                return;
            }

            // Iterate through the mapped objects and update button text if it differs
            foreach (var kvp in _objectVisualElementMap)
            {
                BaseDataObject dataObject = kvp.Key;
                VisualElement visualElement = kvp.Value;

                if (dataObject != null && visualElement != null) // Safety checks
                {
                    string currentTitle = dataObject.Title;
                    if (visualElement is Button button)
                    {
                        if (button.text != currentTitle)
                        {
                            button.text = currentTitle;
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Cannot re-render {currentTitle}");
                    }
                }
            }
            // Optional: Maybe update inspector too
            // BuildInspectorView();
        }

#if ODIN_INSPECTOR
        private void HandleOdinPropertyValueChanged(InspectorProperty property, int selectionIndex)
        {
            // --- Basic checks ---
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
#endif // ODIN_INSPECTOR

        // private void OnGUI()
        // {
        //     EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
        //     try
        //     {
        //         DrawNamespaceTab();
        //         DrawObjectsTab();
        //         DrawScriptableObjectTab();
        //     }
        //     finally
        //     {
        //         EditorGUILayout.EndHorizontal();
        //     }
        // }

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
            if (visualElement is Button button)
            {
                if (string.Equals(button.text, dataObject.Title, StringComparison.Ordinal))
                {
                    return;
                }

                visualElement
                    .schedule.Execute(() =>
                    {
                        if (dataObject != null)
                        {
                            if (
                                !string.Equals(
                                    button.text,
                                    dataObject.Title,
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                button.text = dataObject.Title;
                            }
                        }
                    })
                    .ExecuteLater(10);
            }
        }

        private void BuildNamespaceView()
        {
            if (_namespaceListContainer == null)
                return; // Guard
            _namespaceListContainer.Clear();

            foreach ((string key, List<Type> types) in _scriptableObjectTypes)
            {
                // Namespace Label
                var namespaceLabel = new Label(key)
                {
                    style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 5 },
                };
                _namespaceListContainer.Add(namespaceLabel);

                // Type Buttons
                foreach (Type type in types)
                {
                    var typeButton = new Button(() =>
                    {
                        // Action on click:
                        LoadObjectTypes(type); // Load data
                        BuildObjectsView(); // Update object list UI
                        _selectedObject = null; // Clear selection
#if ODIN_INSPECTOR
                        _odinPropertyTree = null; // Clear Odin tree
#endif
                        BuildInspectorView(); // Update inspector UI (clear it)
                    })
                    {
                        text = type.Name,
                        style = { marginLeft = 10 }, // Indent type names
                    };
                    _namespaceListContainer.Add(typeButton);
                }
            }
        }

        // Builds the Object list UI
        private void BuildObjectsView()
        {
            if (_objectListContainer == null)
                return; // Guard
            _objectListContainer.Clear();
            _objectScrollView.scrollOffset = Vector2.zero; // Reset scroll

            foreach (BaseDataObject dataObject in _selectedObjects)
            {
                // Need local copy for the lambda closure
                BaseDataObject currentObject = dataObject;
                var objectButton = new Button(() =>
                {
                    // Action on click:
                    Selection.activeObject = currentObject; // Update Unity's selection first
                    _selectedObject = currentObject; // Update selection
#if ODIN_INSPECTOR
                    // Determine if the Odin Tree needs to be recreated.
                    bool shouldRecreate =
                        _odinPropertyTree == null
                        || _odinPropertyTree.WeakTargets == null
                        || _odinPropertyTree.WeakTargets.Count == 0
                        || !object.ReferenceEquals(
                            _odinPropertyTree.WeakTargets[0],
                            _selectedObject
                        );

                    if (shouldRecreate)
                    {
                        // Optional: Explicitly dispose the old tree if Odin requires it.
                        // Check Odin's documentation if you encounter issues.
                        // _odinPropertyTree?.Dispose();
                        if (_odinPropertyTree != null)
                        {
                            _odinPropertyTree.OnPropertyValueChanged -=
                                HandleOdinPropertyValueChanged;
                            // _odinPropertyTree.Dispose(); // If Odin requires explicit disposal
                        }
                        _odinPropertyTree = null; // Clear the reference first
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
                    }
#endif // ODIN_INSPECTOR

                    BuildInspectorView(); // Update inspector UI with the potentially new tree/selection
                })
                {
                    text = currentObject.Title, // Use the Title property
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
                return; // Guard
            }

            _inspectorContainer.Clear();
            _inspectorScrollView.scrollOffset = Vector2.zero; // Reset scroll

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

#if ODIN_INSPECTOR
            // Use Odin Inspector via IMGUIContainer

            // Ensure tree exists and matches the selected object before drawing
            if (
                _odinPropertyTree != null
                && _odinPropertyTree.WeakTargets != null
                && // Check list exists
                _odinPropertyTree.WeakTargets.Count > 0
                && // Check list has items
                ReferenceEquals(_odinPropertyTree.WeakTargets[0], _selectedObject)
            ) // Check the item matches
            {
                // Reuse the container or create if null
                if (_odinInspectorContainer == null)
                {
                    _odinInspectorContainer = new IMGUIContainer(() =>
                    {
                        // Double-check inside the handler for safety, though outer check should suffice
                        if (
                            _odinPropertyTree != null
                            && _odinPropertyTree.WeakTargets != null
                            && _odinPropertyTree.WeakTargets.Count > 0
                            && ReferenceEquals(_odinPropertyTree.WeakTargets[0], _selectedObject)
                        )
                        {
                            try
                            {
                                _odinPropertyTree.Draw();
                            }
                            catch (Exception e)
                            {
                                // Handle potential drawing errors
                                this.LogError($"Error drawing Odin Inspector.", e);
                                // Optionally display an error in the IMGUI container itself
                                GUILayout.Label($"Error drawing Odin Inspector: {e.Message}");
                            }
                        }
                    })
                    {
                        name = "odin-inspector",
                        style = { flexGrow = 1 }, // Allow IMGUI container to expand
                    };

                    _odinInspectorContainer.RegisterCallback<FocusInEvent, DataVisualizer>(
                        (evt, context) =>
                        {
                            // When the container gains focus, start periodic repainting for the cursor blink
                            context._odinRepaintSchedule?.Pause(); // Ensure any previous schedule is stopped
                            context._odinRepaintSchedule = context
                                ._odinInspectorContainer.schedule.Execute(() =>
                                {
                                    if (
                                        context
                                            ._odinInspectorContainer
                                            .focusController
                                            ?.focusedElement == context._odinInspectorContainer
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
                            // When the container loses focus, stop the periodic repainting
                            context._odinRepaintSchedule?.Pause();
                        },
                        this
                    );
                }
                else
                {
                    // If container exists, ensure its handler is up-to-date
                    // (The lambda captures should handle this, but explicit re-assignment is safe)
                    _odinInspectorContainer.onGUIHandler = () =>
                    {
                        if (
                            _odinPropertyTree != null
                            && _odinPropertyTree.WeakTargets != null
                            && _odinPropertyTree.WeakTargets.Count > 0
                            && object.ReferenceEquals(
                                _odinPropertyTree.WeakTargets[0],
                                _selectedObject
                            )
                        )
                        {
                            try
                            {
                                _odinPropertyTree.Draw();
                            }
                            catch (Exception e)
                            {
                                this.LogError($"Error drawing Odin Inspector.", e);
                                GUILayout.Label($"Error drawing Odin Inspector: {e.Message}");
                            }
                        }
                    };
                }

                // Add the container to the hierarchy if it's not already there
                // (Clear() removes it, so we always add it back if we have a valid tree)
                _inspectorContainer.Add(_odinInspectorContainer);

                // Ensure the container repaints if the underlying data might have changed
                _odinInspectorContainer.MarkDirtyRepaint();
            }
            else if (_selectedObject != null)
            {
                // Handle case where tree creation failed or doesn't match
                _inspectorContainer.Add(
                    new Label(
                        $"Odin Inspector could not be shown for {_selectedObject.name}. Tree invalid or target mismatch."
                    )
                    {
                        name = "odin-error-label",
                    }
                );
            }

#else

            SerializedObject serializedObject = new(_selectedObject);
            SerializedProperty serializedProperty = serializedObject.GetIterator();
            serializedProperty.NextVisible(true);

            const string titleFieldName = nameof(BaseDataObject._title);
            while (serializedProperty.NextVisible(false))
            {
                SerializedProperty currentProperty = serializedProperty.Copy();
                PropertyField propertyField = new(serializedProperty.Copy());
                propertyField.Bind(serializedObject);
                if (string.Equals(currentProperty.name, titleFieldName, StringComparison.Ordinal))
                {
                    propertyField.RegisterValueChangeCallback(evt =>
                    {
                        RefreshSelectedElement();
                    });
                }
                _inspectorContainer.Add(propertyField);
            }
#endif
        }

        private void DrawNamespaceTab()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            try
            {
                GUILayout.Label("Namespaces", EditorStyles.boldLabel);

                foreach ((string key, List<Type> types) in _scriptableObjectTypes)
                {
                    GUILayout.Label(key, EditorStyles.boldLabel);
                    foreach (Type type in types)
                    {
                        if (GUILayout.Button(type.Name))
                        {
                            LoadObjectTypes(type);
                        }
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawObjectsTab()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            try
            {
                GUILayout.Label("Objects", EditorStyles.boldLabel);
                _objectScrollPosition = EditorGUILayout.BeginScrollView(_objectScrollPosition);
                try
                {
                    foreach (BaseDataObject dataObject in _selectedObjects)
                    {
                        if (GUILayout.Button(dataObject.Title))
                        {
                            Selection.activeObject = dataObject;
                            _selectedObject = dataObject;
                        }
                    }
                }
                finally
                {
                    EditorGUILayout.EndScrollView();
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawScriptableObjectTab()
        {
            if (_selectedObject == null)
            {
                return;
            }
            _dataObjectScrollPosition = EditorGUILayout.BeginScrollView(_dataObjectScrollPosition);
            try
            {
#if ODIN_INSPECTOR
                // TODO: How do I render this?
                OdinEditor.CreateEditor(_selectedObject);
#else
                SerializedObject serializedObject = new SerializedObject(_selectedObject);
                SerializedProperty serializedProperty = serializedObject.GetIterator();
                serializedProperty.NextVisible(true);
                while (serializedProperty.NextVisible(false))
                {
                    EditorGUILayout.PropertyField(serializedProperty, true);
                }

                serializedObject.ApplyModifiedProperties();
#endif
            }
            finally { }
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

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

#if ODIN_INSPECTOR
        private PropertyTree _odinPropertyTree;
        private IMGUIContainer _odinInspectorContainer; // Re-added
        private IVisualElementScheduledItem _odinRepaintSchedule; // Re-added for caret blink
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
            Undo.undoRedoPerformed -= UpdateVisuals;
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
                        // --- Re-added Odin Tree Recreation & Event Hooking ---
                        bool recreateTree =
                            _odinPropertyTree == null
                            || _odinPropertyTree.WeakTargets == null
                            || _odinPropertyTree.WeakTargets.Count == 0
                            || !ReferenceEquals(_odinPropertyTree.WeakTargets[0], _selectedObject);

                        if (recreateTree)
                        {
                            // Unhook previous tree event
                            if (_odinPropertyTree != null)
                            {
                                _odinPropertyTree.OnPropertyValueChanged -=
                                    HandleOdinPropertyValueChanged;
                                // _odinPropertyTree.Dispose(); // Optional dispose
                            }

                            _odinPropertyTree = null; // Clear ref
                            if (_selectedObject != null)
                            {
                                try
                                {
                                    _odinPropertyTree = PropertyTree.Create(_selectedObject);
                                    // Hook new tree event
                                    _odinPropertyTree.OnPropertyValueChanged +=
                                        HandleOdinPropertyValueChanged;
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError(
                                        $"Failed to create Odin PropertyTree for {_selectedObject.name}: {e}"
                                    );
                                }
                            }
                            // Invalidate the IMGUIContainer so it redraws with the new tree
                            _odinInspectorContainer?.MarkDirtyRepaint();
                        }
                        // --- End Odin Tree Recreation ---
#endif // ODIN_INSPECTOR
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

            using SerializedObject serializedObject = new(_selectedObject);
#if ODIN_INSPECTOR
            // --- Odin Inspector Path ---
            try
            {
                // Ensure tree exists for the selected object (might have been created in button click)
                if (
                    _odinPropertyTree == null
                    || _odinPropertyTree.WeakTargets == null
                    || _odinPropertyTree.WeakTargets.Count == 0
                    || !object.ReferenceEquals(_odinPropertyTree.WeakTargets[0], _selectedObject)
                )
                {
                    // Attempt to create it if missing/mismatched
                    if (_odinPropertyTree != null)
                    {
                        _odinPropertyTree.OnPropertyValueChanged -= HandleOdinPropertyValueChanged; // Unhook old just in case
                    }
                    _odinPropertyTree = PropertyTree.Create(_selectedObject);
                    _odinPropertyTree.OnPropertyValueChanged += HandleOdinPropertyValueChanged; // Hook new
                }

                // Reuse or create IMGUIContainer
                if (_odinInspectorContainer == null)
                {
                    _odinInspectorContainer = new IMGUIContainer(() =>
                    {
                        // Draw Odin Tree if valid and target matches
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
                                Debug.LogError($"Odin Draw Error: {e}");
                                GUILayout.Label("Odin Draw Error");
                            }
                        }
                    })
                    {
                        name = "odin-inspector",
                        style = { flexGrow = 1 },
                    };

                    // --- Add Caret Blink Fix ---
                    _odinInspectorContainer.RegisterCallback<FocusInEvent>(evt =>
                    {
                        _odinRepaintSchedule?.Pause();
                        _odinRepaintSchedule = _odinInspectorContainer
                            .schedule.Execute(() =>
                            {
                                if (
                                    _odinInspectorContainer.focusController?.focusedElement
                                    == _odinInspectorContainer
                                )
                                {
                                    _odinInspectorContainer.MarkDirtyRepaint();
                                }
                                else
                                {
                                    _odinRepaintSchedule?.Pause();
                                }
                            })
                            .Every(100); // ~100ms interval for blinking
                    });
                    _odinInspectorContainer.RegisterCallback<FocusOutEvent>(evt =>
                    {
                        _odinRepaintSchedule?.Pause();
                    });
                    // --- End Caret Blink Fix ---
                }
                else
                {
                    // Ensure handler is correct if container is reused
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
                                Debug.LogError($"Odin Draw Error: {e}");
                                GUILayout.Label("Odin Draw Error");
                            }
                        }
                    };
                }
                _inspectorContainer.Add(_odinInspectorContainer);
                _odinInspectorContainer.MarkDirtyRepaint(); // Ensure it draws initially
            }
            catch (Exception e)
            {
                Debug.LogError($"Error setting up Odin Inspector: {e}");
                _inspectorContainer.Add(new Label($"Odin Inspector Error: {e.Message}"));
            }

#else // --- Standard Inspector Path ---
            try
            {
                SerializedProperty serializedProperty = serializedObject.GetIterator();
                serializedProperty.NextVisible(true); // Skip script property

                // <<< IMPORTANT: Define the actual field name for the Title property >>>
                string titleFieldName = "title"; // Example: Change "title" to your field name (e.g., "objectName", "_title")

                while (serializedProperty.NextVisible(false))
                {
                    SerializedProperty currentPropCopy = serializedProperty.Copy();
                    var propertyField = new PropertyField(currentPropCopy);
                    propertyField.Bind(serializedObject);

                    // --- Real-time update for Title field ---
                    if (currentPropCopy.name == titleFieldName)
                    {
                        propertyField.RegisterValueChangeCallback(evt =>
                        {
                            // Use the generic update method
                            // Schedule slightly later to ensure data settles before reading Title property
                            rootVisualElement
                                .schedule.Execute(
                                    () => UpdateObjectTitleRepresentation(_selectedObject)
                                )
                                .ExecuteLater(10);
                        });
                    }
                    // --- End Real-time update ---

                    _inspectorContainer.Add(propertyField);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating standard inspector: {e}");
                _inspectorContainer.Add(new Label($"Inspector Error: {e.Message}"));
            }
#endif // ODIN_INSPECTOR / Standard

            VisualElement customElement = _selectedObject.BuildGUI(serializedObject);
            if (customElement != null)
            {
                _inspectorContainer.Add(customElement);
            }
        }

        private void UpdateObjectTitleRepresentation(BaseDataObject dataObject)
        {
            if (
                dataObject != null
                && _selectedObject == dataObject
                && _objectVisualElementMap.TryGetValue(dataObject, out VisualElement element)
            )
            {
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

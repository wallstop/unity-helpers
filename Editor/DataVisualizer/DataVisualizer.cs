namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector.Editor;
#endif
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.DataVisualizer;
    using Core.Extension;
    using Core.Helper;
    using Core.Serialization;
    using UnityEditor.UIElements;
    using Object = UnityEngine.Object;

    public sealed class DataVisualizer : EditorWindow
    {
        private const string PrefsPrefix = "WallstopStudios.UnityHelpers.DataVisualizer.";
        private const string NamespaceCollapsedStateFormat = PrefsPrefix + "NamespaceCollapsed.{0}";
        private const string LastSelectedNamespaceKey = PrefsPrefix + "LastSelectedNamespace";
        private const string LastSelectedTypeFormat = PrefsPrefix + "LastSelectedType.{0}";
        private const string LastSelectedObjectFormat = PrefsPrefix + "LastSelectedObject.{0}";
        private const string CustomTypeOrderKey = PrefsPrefix + "CustomTypeOrder";
        private const string CustomNamespaceOrderKey = PrefsPrefix + "CustomNamespaceOrder";
        private const string CustomTypeOrderKeyFormat = PrefsPrefix + "CustomTypeOrder.{0}";
        private const string PrefsSplitterOuterKey = PrefsPrefix + "SplitterOuterFixedPaneWidth";
        private const string PrefsSplitterInnerKey = PrefsPrefix + "SplitterInnerFixedPaneWidth";

        private const string SettingsDefaultPath = "Assets/DataVisualizerSettings.asset";

        private const string NamespaceItemClass = "object-item";
        private const string NamespaceHeaderClass = "namespace-header";
        private const string NamespaceIndicatorClass = "namespace-indicator";
        private const string NamespaceLabelClass = "object-item__label";
        private const string TypeItemClass = "type-item";
        private const string TypeLabelClass = "type-item__label";
        private const string objectItemClass = "object-item"; // Keep using this base class maybe
        private const string objectItemContentClass = "object-item-content"; // Container for label/main part
        private const string objectItemActionsClass = "object-item-actions"; // Container for buttons
        private const string actionButtonClass = "action-button"; // Style for small buttons

        private const string ArrowCollapsed = "►";
        private const string ArrowExpanded = "▼";

        private const float DragUpdateThrottleTime = 0.05f;
        private const float DefaultOuterSplitWidth = 200f;
        private const float DefaultInnerSplitWidth = 250f;

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

        private TwoPaneSplitView _outerSplitView;
        private TwoPaneSplitView _innerSplitView;
        private VisualElement _namespaceColumnVE; // Need reference to the actual column VE
        private VisualElement _objectColumnVE; // Need reference to the actual column VE

        private float _lastSavedOuterWidth = -1f; // Track last saved value to avoid redundant writes
        private float _lastSavedInnerWidth = -1f;
        private IVisualElementScheduledItem _saveWidthsTask; // Reference to the scheduled task

        private DragType _activeDragType = DragType.None;
        private object _draggedData;
        private Type _selectedType;
        private VisualElement _selectedTypeElement;
        private VisualElement _inPlaceGhost;
        private int _lastGhostInsertIndex = -1;
        private VisualElement _lastGhostParent;
        private VisualElement _draggedElement;
        private VisualElement _dragGhost;
        private Vector2 _dragStartPosition;
        private bool _isDragging;
        private float _lastDragUpdateTime;
        private SerializedObject _currentInspectorScriptableObject;

        private DataVisualizerSettings _settings;
        private VisualElement _settingsPopup;
        private Label _dataFolderPathDisplay;
#if ODIN_INSPECTOR
        private PropertyTree _odinPropertyTree;
        private IMGUIContainer _odinInspectorContainer;
        private IVisualElementScheduledItem _odinRepaintSchedule;
#endif

        [MenuItem("Tools/Data Visualizer")]
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
            _settings = LoadOrCreateSettings();

            LoadScriptableObjectTypes();
            rootVisualElement
                .schedule.Execute(() =>
                {
                    RestorePreviousSelection();
                    StartPeriodicWidthSave();
                })
                .ExecuteLater(10);
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
            _saveWidthsTask?.Pause();
            _saveWidthsTask = null;
            _currentInspectorScriptableObject?.Dispose();
            _currentInspectorScriptableObject = null;
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

        public static void SignalRefresh()
        {
            // Find the open instance of the window (if any)
            DataVisualizer window = GetWindow<DataVisualizer>(false, null, false); // Don't create if not open
            if (window != null)
            {
                Debug.Log("DataVisualizer window found, scheduling refresh.");
                // Schedule the actual refresh on the window's update loop
                window.ScheduleRefresh();
            }
            else
            {
                Debug.Log("DataVisualizer window not open, refresh signal ignored.");
            }
        }

        private void ScheduleRefresh()
        {
            // Ensure execution on the main thread via the window's scheduler
            // A small delay can prevent issues if called during awkward editor states
            rootVisualElement.schedule.Execute(RefreshAllViews).ExecuteLater(50); // 50ms delay
        }

        private void RefreshAllViews()
        {
            Debug.Log("DataVisualizer RefreshAllViews started.");
            if (_settings == null)
            {
                _settings = LoadOrCreateSettings(); // Ensure settings are loaded
            }

            // --- Store current selection state ---
            string previousNamespaceKey =
                _selectedType != null ? GetNamespaceKey(_selectedType) : null;
            string previousTypeName = _selectedType?.Name;
            string previousObjectGuid = null;
            if (_selectedObject != null)
            {
                string path = AssetDatabase.GetAssetPath(_selectedObject);
                if (!string.IsNullOrEmpty(path))
                {
                    previousObjectGuid = AssetDatabase.AssetPathToGUID(path);
                }
            }
            // ---

            // --- Reload Core Data ---
            // Note: _settings itself might need reloading if the settings *file* changed,
            // but LoadOrCreateSettings only loads on first enable or if null.
            // If settings file changes often, consider reloading it here too.
            // _settings = LoadOrCreateSettings(); // Uncomment if settings file changes need explicit reload here

            LoadScriptableObjectTypes(); // Reloads namespace/type structure and sorts them
            // ---


            // --- Attempt to Restore Selection ---
            _selectedType = null; // Reset selection first
            _selectedObject = null;
            _selectedElement = null;
            _selectedTypeElement = null;

            // Find previous Namespace
            int namespaceIndex = -1;
            if (!string.IsNullOrEmpty(previousNamespaceKey))
            {
                namespaceIndex = _scriptableObjectTypes.FindIndex(kvp =>
                    kvp.key == previousNamespaceKey
                );
            }
            if (namespaceIndex == -1 && _scriptableObjectTypes.Count > 0)
            { // Fallback namespace
                namespaceIndex = 0;
                Debug.Log("Refresh: Previous namespace not found or null, using first.");
            }

            // Find previous Type
            if (namespaceIndex != -1)
            {
                List<Type> typesInNamespace = _scriptableObjectTypes[namespaceIndex].types;
                if (typesInNamespace.Count > 0)
                {
                    if (!string.IsNullOrEmpty(previousTypeName))
                    {
                        _selectedType = typesInNamespace.FirstOrDefault(t =>
                            t.Name == previousTypeName
                        );
                    }
                    if (_selectedType == null)
                    { // Fallback type
                        _selectedType = typesInNamespace[0];
                        Debug.Log(
                            "Refresh: Previous type not found or null, using first in namespace."
                        );
                    }
                }
                else
                {
                    Debug.Log("Refresh: No types found in selected/fallback namespace.");
                }
            }
            else
            {
                Debug.Log("Refresh: No namespaces found after reload.");
            }

            // Load objects for the determined type
            if (_selectedType != null)
            {
                LoadObjectTypes(_selectedType); // Reloads _selectedObjects for the type
            }
            else
            {
                _selectedObjects.Clear(); // No type selected, clear object list
            }

            // Find previous Object
            if (
                _selectedType != null
                && !string.IsNullOrEmpty(previousObjectGuid)
                && _selectedObjects.Count > 0
            )
            {
                _selectedObject = _selectedObjects.FirstOrDefault(obj =>
                {
                    if (obj == null)
                        return false;
                    string path = AssetDatabase.GetAssetPath(obj);
                    return !string.IsNullOrEmpty(path)
                        && AssetDatabase.AssetPathToGUID(path) == previousObjectGuid;
                });
                if (_selectedObject == null)
                {
                    Debug.Log("Refresh: Previous object GUID not found in current list.");
                    // Fallback to first object? Or select none? Let's select none if specific one is gone.
                    // _selectedObject = _selectedObjects.Count > 0 ? _selectedObjects[0] : null;
                }
            }
            // --- End Restore Selection Attempt ---


            // --- Rebuild UI ---
            // Need to rebuild all views to reflect potential changes in namespaces, types, objects, and their order/existence.
            BuildNamespaceView(); // Applies collapse state, builds type items (checking for objects)
            BuildObjectsView(); // Builds object list based on potentially changed _selectedObjects

            // Apply visual selection for Type (FindTypeElement needs BuildNamespaceView first)
            VisualElement typeElementToSelect = FindTypeElement(_selectedType);
            if (typeElementToSelect != null)
            {
                _selectedTypeElement = typeElementToSelect;
                _selectedTypeElement.AddToClassList("selected");
                // Ensure namespace is expanded if needed
                var ancestorGroup = FindAncestorNamespaceGroup(_selectedTypeElement); // Use new helper
                if (ancestorGroup != null)
                    ExpandNamespaceGroupIfNeeded(ancestorGroup, false); // Don't re-save state
            }

            // Apply visual selection for Object and update Inspector
            // SelectObject handles null correctly, finds the VE in the rebuilt view, and builds inspector.
            // It will also re-save the (potentially restored) state.
            SelectObject(_selectedObject);

            Debug.Log("DataVisualizer RefreshAllViews finished.");
        }

        private VisualElement FindAncestorNamespaceGroup(VisualElement startingElement)
        {
            if (startingElement == null)
                return null;
            VisualElement currentElement = startingElement;
            while (currentElement != null && currentElement != _namespaceListContainer)
            {
                if (currentElement.ClassListContains("object-item")) // Assuming namespace groups use this class
                {
                    return currentElement;
                }
                currentElement = currentElement.parent;
            }
            return null;
        }

        // Expands a namespace group if it's currently collapsed
        private void ExpandNamespaceGroupIfNeeded(VisualElement namespaceGroupItem, bool saveState)
        {
            if (namespaceGroupItem == null)
                return;
            var indicator = namespaceGroupItem.Q<Label>(className: "namespace-indicator"); // Use class
            string nsKey = namespaceGroupItem.userData as string;
            var typesContainer = namespaceGroupItem.Q<VisualElement>($"types-container-{nsKey}"); // Use name

            if (
                indicator != null
                && typesContainer != null
                && typesContainer.style.display == DisplayStyle.None
            )
            {
                // It's collapsed, expand it
                ApplyNamespaceCollapsedState(indicator, typesContainer, false, saveState); // false = collapsed state = expand
            }
        }

        private DataVisualizerSettings LoadOrCreateSettings()
        {
            DataVisualizerSettings settings = null;

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(DataVisualizerSettings)}");

            if (guids.Length > 0)
            {
                if (guids.Length > 1)
                {
                    this.LogWarn(
                        $"Multiple DataVisualizerSettings assets found ({guids.Length}). Using the first one."
                    );
                }

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }
                    settings = AssetDatabase.LoadAssetAtPath<DataVisualizerSettings>(path);
                    if (settings != null)
                    {
                        break;
                    }
                }
            }

            if (settings == null)
            {
                this.Log(
                    $"No DataVisualizerSettings found, creating default at '{SettingsDefaultPath}'"
                );
                settings = CreateInstance<DataVisualizerSettings>();
                settings._dataFolderPath = DataVisualizerSettings.DefaultDataFolderPath;

                string dir = Path.GetDirectoryName(SettingsDefaultPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                try
                {
                    AssetDatabase.CreateAsset(settings, SettingsDefaultPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    settings = AssetDatabase.LoadAssetAtPath<DataVisualizerSettings>(
                        SettingsDefaultPath
                    );
                }
                catch (Exception e)
                {
                    this.LogError($"Failed to create DataVisualizerSettings asset.", e);
                    settings = CreateInstance<DataVisualizerSettings>();
                    settings._dataFolderPath = DataVisualizerSettings.DefaultDataFolderPath;
                }
            }

            if (settings != null)
            {
                return settings;
            }

            this.LogError(
                $"Failed to load or create DataVisualizerSettings. Using temporary instance."
            );
            settings = CreateInstance<DataVisualizerSettings>();
            settings._dataFolderPath = DataVisualizerSettings.DefaultDataFolderPath;
            return settings;
        }

        private void StartPeriodicWidthSave()
        {
            // Ensure it runs only once / stop previous if needed
            _saveWidthsTask?.Pause();
            // Schedule CheckAndSaveSplitterWidths to run every 1000ms (1 second)
            _saveWidthsTask = rootVisualElement
                .schedule.Execute(CheckAndSaveSplitterWidths)
                .Every(1000);
            Debug.Log("Started periodic splitter width saving.");
        }

        private void CheckAndSaveSplitterWidths()
        {
            // Check if split views and target columns have been created and have valid layout
            if (
                _outerSplitView == null
                || _innerSplitView == null
                || _namespaceColumnVE == null
                || _objectColumnVE == null
                || float.IsNaN(_namespaceColumnVE.resolvedStyle.width)
                || // Wait for layout pass
                float.IsNaN(_objectColumnVE.resolvedStyle.width)
            )
            {
                // Debug.Log("Skipping width save check - elements not ready or layout invalid.");
                return;
            }

            // Get CURRENT actual widths of the columns within the fixed panes
            float currentOuterWidth = _namespaceColumnVE.resolvedStyle.width;
            float currentInnerWidth = _objectColumnVE.resolvedStyle.width;

            bool changed = false;

            // Compare with last saved values (using approximation for floats)
            if (!Mathf.Approximately(currentOuterWidth, _lastSavedOuterWidth))
            {
                Debug.Log(
                    $"Outer width changed: {_lastSavedOuterWidth} -> {currentOuterWidth}. Saving."
                );
                EditorPrefs.SetFloat(PrefsSplitterOuterKey, currentOuterWidth);
                _lastSavedOuterWidth = currentOuterWidth; // Update tracking value
                changed = true;
            }

            if (!Mathf.Approximately(currentInnerWidth, _lastSavedInnerWidth))
            {
                Debug.Log(
                    $"Inner width changed: {_lastSavedInnerWidth} -> {currentInnerWidth}. Saving."
                );
                EditorPrefs.SetFloat(PrefsSplitterInnerKey, currentInnerWidth);
                _lastSavedInnerWidth = currentInnerWidth; // Update tracking value
                changed = true;
            }

            // Optional: If EditorPrefs saving is slow, maybe call EditorPrefs.Save() periodically? Usually not needed.
            // if (changed) { EditorPrefs.Save(); } // Likely unnecessary
        }

        private void RestorePreviousSelection()
        {
            if (_scriptableObjectTypes.Count == 0)
            {
                return;
            }

            string savedNamespaceKey = EditorPrefs.GetString(LastSelectedNamespaceKey, null);
            string selectedNamespaceKey;
            List<Type> typesInNamespace;
            int namespaceIndex = -1;

            if (!string.IsNullOrWhiteSpace(savedNamespaceKey))
            {
                namespaceIndex = _scriptableObjectTypes.FindIndex(kvp =>
                    kvp.key == savedNamespaceKey
                );
            }

            if (0 <= namespaceIndex)
            {
                selectedNamespaceKey = savedNamespaceKey;
                typesInNamespace = _scriptableObjectTypes[namespaceIndex].types;
            }
            else if (_scriptableObjectTypes.Any())
            {
                selectedNamespaceKey = _scriptableObjectTypes[0].key;
                typesInNamespace = _scriptableObjectTypes[0].types;
            }
            else
            {
                selectedNamespaceKey = null;
                typesInNamespace = null;
            }

            if (typesInNamespace is not { Count: > 0 })
            {
                return;
            }

            string typePrefsKey = string.Format(LastSelectedTypeFormat, selectedNamespaceKey);
            string savedTypeName = EditorPrefs.GetString(typePrefsKey, null);
            Type selectedType = null;

            if (!string.IsNullOrWhiteSpace(savedTypeName))
            {
                selectedType = typesInNamespace.Find(t => t.Name == savedTypeName);
            }

            selectedType ??= typesInNamespace[0];
            _selectedType = selectedType;

            LoadObjectTypes(_selectedType);
            BuildNamespaceView();
            BuildObjectsView();

            VisualElement typeElementToSelect = FindTypeElement(_selectedType);
            if (typeElementToSelect != null)
            {
                _selectedTypeElement?.RemoveFromClassList("selected");
                _selectedTypeElement = typeElementToSelect;
                _selectedTypeElement.AddToClassList("selected");

                VisualElement ancestorGroup = null;
                VisualElement currentElement = _selectedTypeElement;
                while (currentElement != null && currentElement != _namespaceListContainer)
                {
                    if (currentElement.ClassListContains(NamespaceItemClass))
                    {
                        ancestorGroup = currentElement;
                        break;
                    }
                    currentElement = currentElement.parent;
                }

                if (ancestorGroup != null)
                {
                    Label indicator = ancestorGroup.Q<Label>(className: NamespaceIndicatorClass);
                    VisualElement typesContainer = ancestorGroup.Q<VisualElement>(
                        $"types-container-{ancestorGroup.userData as string}"
                    );

                    if (
                        indicator != null
                        && typesContainer != null
                        && typesContainer.style.display == DisplayStyle.None
                    )
                    {
                        ApplyNamespaceCollapsedState(indicator, typesContainer, false, false);
                    }
                }
            }

            string objPrefsKey = string.Format(LastSelectedObjectFormat, _selectedType.Name);
            string savedObjectGuid = EditorPrefs.GetString(objPrefsKey, string.Empty);
            BaseDataObject objectToSelect = null;

            if (!string.IsNullOrWhiteSpace(savedObjectGuid) && 0 < _selectedObjects.Count)
            {
                objectToSelect = _selectedObjects.Find(obj =>
                {
                    if (obj == null)
                    {
                        return false;
                    }

                    string path = AssetDatabase.GetAssetPath(obj);
                    return !string.IsNullOrWhiteSpace(path)
                        && string.Equals(
                            AssetDatabase.AssetPathToGUID(path),
                            savedObjectGuid,
                            StringComparison.OrdinalIgnoreCase
                        );
                });
            }

            if (objectToSelect == null && _selectedObjects.Count > 0)
            {
                objectToSelect = _selectedObjects[0];
            }

            SelectObject(objectToSelect);
        }

        private VisualElement FindTypeElement(Type targetType)
        {
            if (targetType == null || _namespaceListContainer == null)
            {
                return null;
            }

            List<VisualElement> typeItems = _namespaceListContainer
                .Query<VisualElement>(className: "type-item")
                .ToList();

            foreach (VisualElement item in typeItems)
            {
                if (item.userData is Type itemType && itemType == targetType)
                {
                    return item;
                }
            }
            return null;
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

            VisualElement headerRow = new()
            {
                name = "header-row",
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexStart,
                    paddingTop = 5,
                    paddingBottom = 5,
                    paddingLeft = 5,
                    paddingRight = 5,
                    borderBottomWidth = 1,
                    borderBottomColor = Color.gray,
                },
            };
            root.Add(headerRow);

            Button settingsButton = new(ToggleSettingsPopup)
            {
                text = "…", //"⚙",
                name = "settings-button",
                tooltip = "Open Settings",
            };
            headerRow.Add(settingsButton);

            float initialOuterWidth = EditorPrefs.GetFloat(
                PrefsSplitterOuterKey,
                DefaultOuterSplitWidth
            );
            float initialInnerWidth = EditorPrefs.GetFloat(
                PrefsSplitterInnerKey,
                DefaultInnerSplitWidth
            );

            _lastSavedOuterWidth = initialOuterWidth;
            _lastSavedInnerWidth = initialInnerWidth;
            Debug.Log(
                $"Loaded initial widths: Outer={initialOuterWidth}, Inner={initialInnerWidth}"
            );

            // VisualElement mainContainer = new()
            // {
            //     name = "main-container",
            //     style = { flexGrow = 1, flexDirection = FlexDirection.Row },
            // };
            // root.Add(mainContainer);

            _namespaceColumnVE = CreateNamespaceColumn(); // Extract creation logic
            _objectColumnVE = CreateObjectColumn(); // Extract creation logic
            VisualElement inspectorColumn = CreateInspectorColumn(); // Extract creation logic

            _innerSplitView = new TwoPaneSplitView(
                0,
                (int)initialInnerWidth,
                TwoPaneSplitViewOrientation.Horizontal
            )
            {
                name = "inner-split-view",
                style = { flexGrow = 1 }, // Grow within outer split view
            };

            _innerSplitView.Add(_objectColumnVE); // Add object column VE directly

            _innerSplitView.Add(inspectorColumn);

            _outerSplitView = new TwoPaneSplitView(
                0,
                (int)initialOuterWidth,
                TwoPaneSplitViewOrientation.Horizontal
            )
            {
                name = "outer-split-view",
                style = { flexGrow = 1 },
            };
            _outerSplitView.Add(_namespaceColumnVE);

            // Pane 1 (Right/Flexible): Inner Split View
            _outerSplitView.Add(_innerSplitView);

            // Add the outer split view to the root
            root.Add(_outerSplitView);

            // VisualElement namespaceColumn = new()
            // {
            //     name = "namespace-column",
            //     style =
            //     {
            //         width = 200,
            //         borderRightWidth = 1,
            //         borderRightColor = Color.gray,
            //     },
            // };
            // ScrollView namespaceScrollView = new(ScrollViewMode.Vertical)
            // {
            //     name = "namespace-scrollview",
            //     style = { flexGrow = 1 },
            // };
            // _namespaceListContainer = new VisualElement { name = "namespace-list" };
            // namespaceScrollView.Add(_namespaceListContainer);
            // namespaceColumn.Add(
            //     new Label("Namespaces")
            //     {
            //         style = { unityFontStyleAndWeight = FontStyle.Bold, paddingBottom = 5 },
            //     }
            // );
            // namespaceColumn.Add(namespaceScrollView);
            // mainContainer.Add(namespaceColumn);

            // VisualElement objectColumn = new()
            // {
            //     name = "object-column",
            //     style =
            //     {
            //         width = 250,
            //         borderRightWidth = 1,
            //         borderRightColor = Color.gray,
            //         flexDirection = FlexDirection.Column,
            //     },
            // };

            // var objectHeader = new VisualElement
            // {
            //     name = "object-header",
            //     style =
            //     {
            //         flexDirection = FlexDirection.Row, // Align label and button horizontally
            //         justifyContent = Justify.SpaceBetween, // Push label left, button right
            //         alignItems = Align.Center,
            //         paddingBottom = 3,
            //         paddingTop = 3,
            //         paddingLeft = 3,
            //         paddingRight = 3,
            //         height = 24, // Fixed height for header
            //         flexShrink = 0, // Prevent shrinking
            //         borderBottomWidth = 1, // Separator line
            //         borderBottomColor = Color.gray,
            //     },
            // };
            // objectColumn.Add(objectHeader); // Add header first
            //
            // objectColumn.Add(
            //     new Label("Objects")
            //     {
            //         style = { unityFontStyleAndWeight = FontStyle.Bold, paddingBottom = 5 },
            //     }
            // );
            //
            // var createButton = new Button(CreateNewObject)
            // {
            //     text = "+",
            //     tooltip = "Create New Object",
            //     name = "create-object-button",
            //     style =
            //     {
            //         width = 20,
            //         height = 20,
            //         paddingLeft = 0,
            //         paddingRight = 0,
            //     }, // Style as small button
            // };
            // createButton.AddToClassList("icon-button"); // Use existing or new style
            // objectHeader.Add(createButton);

            // _objectScrollView = new ScrollView(ScrollViewMode.Vertical)
            // {
            //     name = "object-scrollview",
            //     style = { flexGrow = 1 },
            // };
            // _objectListContainer = new VisualElement { name = "object-list" };
            // _objectScrollView.Add(_objectListContainer);
            // objectColumn.Add(_objectScrollView);
            // mainContainer.Add(objectColumn);

            // VisualElement inspectorColumn = new()
            // {
            //     name = "inspector-column",
            //     style = { flexGrow = 1 },
            // };
            // _inspectorScrollView = new ScrollView(ScrollViewMode.Vertical)
            // {
            //     name = "inspector-scrollview",
            //     style = { flexGrow = 1 },
            // };
            // _inspectorContainer = new VisualElement { name = "inspector-content" };
            // _inspectorScrollView.Add(_inspectorContainer);
            // inspectorColumn.Add(_inspectorScrollView);
            // mainContainer.Add(inspectorColumn);

            _settingsPopup = new VisualElement
            {
                name = "settings-popup",
                style =
                {
                    position = Position.Absolute,
                    top = 30,
                    left = 10,
                    width = 350,
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.95f),
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderBottomColor = Color.gray,
                    borderLeftColor = Color.gray,
                    borderRightColor = Color.gray,
                    borderTopColor = Color.gray,
                    borderBottomLeftRadius = 5,
                    borderBottomRightRadius = 5,
                    borderTopLeftRadius = 5,
                    borderTopRightRadius = 5,
                    paddingBottom = 10,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 10,
                    display = DisplayStyle.None,
                },
            };
            root.Add(_settingsPopup);
            BuildSettingsPopup();
            BuildNamespaceView();
            BuildObjectsView();
            BuildInspectorView();
        }

        private VisualElement CreateNamespaceColumn()
        {
            VisualElement namespaceColumn = new()
            {
                name = "namespace-column",
                style =
                {
                    borderRightWidth = 1,
                    borderRightColor = Color.gray,
                    height = Length.Percent(100),
                }, // Height 100% for SplitView pane
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
                    style =
                    {
                        unityFontStyleAndWeight = FontStyle.Bold,
                        paddingBottom = 5,
                        paddingLeft = 3,
                    },
                }
            );
            namespaceColumn.Add(namespaceScrollView);
            return namespaceColumn;
        }

        private VisualElement CreateObjectColumn()
        {
            VisualElement objectColumn = new()
            {
                name = "object-column",
                style =
                {
                    borderRightWidth = 1,
                    borderRightColor = Color.gray,
                    flexDirection = FlexDirection.Column,
                    height = Length.Percent(100),
                }, // Height 100%
            };
            // Header
            var objectHeader = new VisualElement
            {
                name = "object-header",
                style =
                {
                    flexDirection = FlexDirection.Row, // Align label and button horizontally
                    justifyContent = Justify.SpaceBetween, // Push label left, button right
                    alignItems = Align.Center,
                    paddingBottom = 3,
                    paddingTop = 3,
                    paddingLeft = 3,
                    paddingRight = 3,
                    height = 24, // Fixed height for header
                    flexShrink = 0, // Prevent shrinking
                    borderBottomWidth = 1, // Separator line
                    borderBottomColor = Color.gray,
                },
            };

            objectColumn.Add(
                new Label("Objects")
                {
                    style = { unityFontStyleAndWeight = FontStyle.Bold, paddingBottom = 5 },
                }
            );
            var createButton = new Button(CreateNewObject)
            {
                text = "+",
                tooltip = "Create New Object",
                name = "create-object-button",
                style =
                {
                    width = 20,
                    height = 20,
                    paddingLeft = 0,
                    paddingRight = 0,
                }, // Style as small button
            };
            createButton.AddToClassList("icon-button");
            objectHeader.Add(createButton);
            objectColumn.Add(objectHeader);
            // ScrollView
            _objectScrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "object-scrollview",
                style = { flexGrow = 1 },
            };
            _objectListContainer = new VisualElement { name = "object-list" };
            _objectScrollView.Add(_objectListContainer);
            objectColumn.Add(_objectScrollView);
            return objectColumn;
        }

        private VisualElement CreateInspectorColumn()
        {
            VisualElement inspectorColumn = new()
            {
                name = "inspector-column",
                style = { flexGrow = 1, height = Length.Percent(100) }, // Height 100%
            };
            _inspectorScrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "inspector-scrollview",
                style = { flexGrow = 1 },
            };
            _inspectorContainer = new VisualElement { name = "inspector-content" };
            _inspectorScrollView.Add(_inspectorContainer);
            inspectorColumn.Add(_inspectorScrollView);
            return inspectorColumn;
        }

        private void CreateNewObject()
        {
            // 1. Check if a Type is selected
            if (_selectedType == null)
            {
                EditorUtility.DisplayDialog(
                    "Cannot Create Object",
                    "Please select a Type in the first column before creating an object.",
                    "OK"
                );
                return;
            }

            // 2. Check if Settings and DataFolderPath are valid
            if (_settings == null || string.IsNullOrWhiteSpace(_settings.DataFolderPath))
            {
                EditorUtility.DisplayDialog(
                    "Cannot Create Object",
                    "Data Folder Path is not set correctly in Settings.",
                    "OK"
                );
                return;
            }

            // 3. Ensure target directory exists
            string targetDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                _settings.DataFolderPath
            ); // Get full path
            targetDirectory = Path.GetFullPath(targetDirectory).Replace('\\', '/'); // Normalize

            // Double-check it's within Assets (should be from settings validation, but be safe)
            string projectAssetsPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
            if (!targetDirectory.StartsWith(projectAssetsPath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog(
                    "Invalid Data Folder",
                    $"The configured Data Folder ('{_settings.DataFolderPath}') is not inside the project's Assets folder.",
                    "OK"
                );
                return;
            }

            try
            {
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                    Debug.Log($"Created data directory: {_settings.DataFolderPath}");
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Could not create data directory '{_settings.DataFolderPath}': {e.Message}",
                    "OK"
                );
                return;
            }

            // 4. Create Instance
            ScriptableObject instance = ScriptableObject.CreateInstance(_selectedType);
            if (instance == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Failed to create instance of type '{_selectedType.Name}'.",
                    "OK"
                );
                return;
            }

            // 5. Generate Unique Asset Path
            // Use relative path for asset database functions
            string baseAssetName = $"New {_selectedType.Name}.asset";
            string proposedPath = Path.Combine(_settings.DataFolderPath, baseAssetName)
                .Replace('\\', '/');
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(proposedPath);

            // 6. Create Asset
            try
            {
                AssetDatabase.CreateAsset(instance, uniquePath);
                AssetDatabase.SaveAssets(); // Save immediately
                AssetDatabase.Refresh(); // Make sure Unity recognizes it

                Debug.Log($"Created new object asset at: {uniquePath}");

                // Load the newly created asset to get the correct instance
                BaseDataObject newObject = AssetDatabase.LoadAssetAtPath<BaseDataObject>(
                    uniquePath
                );

                if (newObject != null)
                {
                    // --- Update UI ---
                    // Add to the beginning of the current list
                    _selectedObjects.Insert(0, newObject);
                    UpdateAndSaveObjectCustomOrder();
                    // Rebuild the view
                    BuildObjectsView();
                    // Select the new object
                    SelectObject(newObject);
                }
                else
                {
                    Debug.LogError($"Failed to load the newly created asset at {uniquePath}");
                    // Might still want to rebuild view even if selection fails
                    BuildObjectsView();
                    SelectObject(null);
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    "Error Creating Asset",
                    $"Failed to create asset at '{uniquePath}': {e.Message}",
                    "OK"
                );
                // Clean up the instance if asset creation failed?
                UnityEngine.Object.DestroyImmediate(instance); // Destroy the SO instance if saving failed
            }
        }

        private void ToggleSettingsPopup()
        {
            if (_settingsPopup == null)
            {
                return;
            }

            if (_settingsPopup.style.display == DisplayStyle.None && _settings != null)
            {
                _dataFolderPathDisplay.text = _settings.DataFolderPath;
            }
            _settingsPopup.style.display =
                _settingsPopup.style.display == DisplayStyle.None
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
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

        private void SelectDataFolder()
        {
            if (_settings == null)
            {
                return;
            }

            string currentFullPath = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), _settings.DataFolderPath)
            );
            string startDir = Directory.Exists(currentFullPath)
                ? currentFullPath
                : Application.dataPath;

            string selectedAbsolutePath = EditorUtility.OpenFolderPanel(
                "Select Data Folder",
                startDir,
                string.Empty
            );

            if (string.IsNullOrWhiteSpace(selectedAbsolutePath))
            {
                return;
            }

            string projectAssetsPath = Path.GetFullPath(Application.dataPath);
            selectedAbsolutePath = Path.GetFullPath(selectedAbsolutePath).Replace('\\', '/');
            projectAssetsPath = projectAssetsPath.Replace('\\', '/');
            if (
                !selectedAbsolutePath.StartsWith(
                    projectAssetsPath,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                this.LogError($"Selected folder must be inside the project's Assets folder.");
                EditorUtility.DisplayDialog(
                    "Invalid Folder",
                    "The selected folder must be inside the project's 'Assets' directory.",
                    "OK"
                );
                return;
            }

            string relativePath;
            if (selectedAbsolutePath.Equals(projectAssetsPath, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = "Assets";
            }
            else
            {
                relativePath = "Assets" + selectedAbsolutePath.Substring(projectAssetsPath.Length);
            }

            if (
                string.Equals(
                    _settings.DataFolderPath,
                    relativePath,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return;
            }

            _settings._dataFolderPath = relativePath;
            EditorUtility.SetDirty(_settings);
            AssetDatabase.SaveAssets();
            _dataFolderPathDisplay.text = _settings.DataFolderPath;
            this.Log($"Data folder updated to: {_settings.DataFolderPath}");
        }

        private void BuildSettingsPopup()
        {
            _settingsPopup.Clear();

            _settingsPopup.Add(
                new Label("Settings")
                {
                    style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 },
                }
            );

            Button closeButton = new(() => _settingsPopup.style.display = DisplayStyle.None)
            {
                text = "X",
                style =
                {
                    position = Position.Absolute,
                    top = 2,
                    right = 2,
                    width = 20,
                    height = 20,
                },
            };
            _settingsPopup.Add(closeButton);

            VisualElement dataFolderContainer = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 5,
                },
            };
            _settingsPopup.Add(dataFolderContainer);

            dataFolderContainer.Add(
                new Label("Data Folder:") { style = { width = 80, flexShrink = 0 } }
            );

            _dataFolderPathDisplay = new Label(_settings?.DataFolderPath ?? "N/A")
            {
                name = "data-folder-display",
                style =
                {
                    flexGrow = 1,
                    backgroundColor = new Color(0.1f, 0.1f, 0.1f),
                    paddingBottom = 2,
                    paddingLeft = 2,
                    paddingRight = 2,
                    paddingTop = 2,
                    marginLeft = 5,
                    marginRight = 5,
                },
                pickingMode = PickingMode.Ignore,
            };
            dataFolderContainer.Add(_dataFolderPathDisplay);

            Button selectFolderButton = new(SelectDataFolder)
            {
                text = "Select...",
                style = { flexShrink = 0 },
            };
            dataFolderContainer.Add(selectFolderButton);
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
                VisualElement namespaceGroupItem = new()
                {
                    name = $"namespace-group-{key}",
                    userData = key,
                };

                namespaceGroupItem.AddToClassList(NamespaceItemClass);
                namespaceGroupItem.userData = key;
                _namespaceListContainer.Add(namespaceGroupItem);
                namespaceGroupItem.RegisterCallback<PointerDownEvent>(OnNamespacePointerDown);

                VisualElement header = new() { name = $"namespace-header-{key}" };
                header.AddToClassList(NamespaceHeaderClass);
                namespaceGroupItem.Add(header);

                Label indicator = new(ArrowExpanded) { name = $"namespace-indicator-{key}" };
                indicator.AddToClassList(NamespaceIndicatorClass);
                header.Add(indicator);

                Label namespaceLabel = new(key)
                {
                    name = $"namespace-name-{key}",
                    style = { unityFontStyleAndWeight = FontStyle.Bold },
                };
                namespaceLabel.AddToClassList(NamespaceLabelClass);
                header.Add(namespaceLabel);

                VisualElement typesContainer = new()
                {
                    name = $"types-container-{key}",
                    style = { marginLeft = 10 },
                    userData = key,
                };
                namespaceGroupItem.Add(typesContainer);

                string collapsePrefsKey = string.Format(NamespaceCollapsedStateFormat, key);
                bool isCollapsed = EditorPrefs.GetBool(collapsePrefsKey, false);
                ApplyNamespaceCollapsedState(indicator, typesContainer, isCollapsed, false);

                header.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != 0 || evt.propagationPhase == PropagationPhase.TrickleDown)
                    {
                        return;
                    }

                    VisualElement parentGroup = header.parent;
                    Label associatedIndicator = parentGroup?.Q<Label>(
                        className: NamespaceIndicatorClass
                    );
                    VisualElement associatedTypesContainer = parentGroup?.Q<VisualElement>(
                        $"types-container-{key}"
                    );
                    string nsKey = parentGroup?.userData as string;

                    if (
                        associatedIndicator != null
                        && associatedTypesContainer != null
                        && !string.IsNullOrEmpty(nsKey)
                    )
                    {
                        bool currentlyCollapsed =
                            associatedTypesContainer.style.display == DisplayStyle.None;
                        bool newCollapsedState = !currentlyCollapsed;

                        ApplyNamespaceCollapsedState(
                            associatedIndicator,
                            associatedTypesContainer,
                            newCollapsedState,
                            true
                        );
                    }

                    evt.StopPropagation();
                });

                foreach (Type type in types)
                {
                    bool hasObjects = AssetDatabase.FindAssets($"t:{type.Name}").Length > 0;

                    VisualElement typeItem = new()
                    {
                        name = $"type-item-{type.Name}",
                        userData = type,
                    };

                    typeItem.AddToClassList(TypeItemClass);
                    Label typeLabel = new(type.Name) { name = "type-item-label" };
                    typeLabel.AddToClassList(TypeLabelClass);
                    typeItem.Add(typeLabel);

                    if (hasObjects)
                    {
                        typeItem.RegisterCallback<PointerDownEvent>(OnTypePointerDown);
                        typeItem.RegisterCallback<PointerUpEvent>(evt =>
                        {
                            if (_isDragging || evt.button != 0)
                            {
                                return;
                            }

                            if (typeItem.userData is not Type clickedType)
                            {
                                return;
                            }

                            _selectedTypeElement?.RemoveFromClassList("selected");
                            _selectedType = clickedType;
                            _selectedTypeElement = typeItem;
                            _selectedTypeElement.AddToClassList("selected");
                            SaveNamespaceAndTypeSelectionState(
                                GetNamespaceKey(_selectedType),
                                _selectedType.Name
                            );

                            LoadObjectTypes(clickedType);
                            BaseDataObject objectToSelect = DetermineObjectToAutoSelect();
                            BuildObjectsView();
                            SelectObject(objectToSelect);
                            evt.StopPropagation();
                        });
                    }
                    else
                    {
                        typeItem.AddToClassList("type-item--disabled");
                        typeItem.pickingMode = PickingMode.Ignore;
                        typeItem.focusable = false;
                    }

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
                if (dataObject == null)
                {
                    continue;
                }

                VisualElement objectItemRow = new()
                {
                    name = $"object-item-row-{dataObject.GetInstanceID()}",
                };
                objectItemRow.AddToClassList(objectItemClass); // Apply base style if needed
                // Store data object reference for event handlers (like selection)
                objectItemRow.style.flexDirection = FlexDirection.Row; // Explicitly set row
                objectItemRow.style.alignItems = Align.Center; // Align items vertically
                objectItemRow.userData = dataObject;
                // Register Pointer Down Event for Selection and Drag Start (on the ROW)
                objectItemRow.RegisterCallback<PointerDownEvent>(OnObjectPointerDown);

                VisualElement contentArea = new VisualElement { name = "content" };
                contentArea.AddToClassList(objectItemContentClass);
                objectItemRow.Add(contentArea);

                Label titleLabel = new Label(dataObject.Title) { name = "object-item-label" };
                titleLabel.AddToClassList("object-item__label"); // Apply style
                contentArea.Add(titleLabel);

                VisualElement actionsArea = new VisualElement
                {
                    name = "actions",
                    style =
                    {
                        flexDirection = FlexDirection.Row, // <<< SET HORIZONTAL LAYOUT
                        alignItems = Align.Center, // Align buttons vertically if their heights differ slightly
                        flexShrink = 0, // Ensure this container doesn't shrink horizontally
                    },
                };
                actionsArea.AddToClassList(objectItemActionsClass);
                objectItemRow.Add(actionsArea);

                var cloneButton = new Button(() => CloneObject(dataObject))
                {
                    text = "++", // Document emoji or "⎘" U+2398
                    tooltip = "Clone Object",
                    style = { color = new Color(0.4f, 0.7f, 0.4f) },
                };
                cloneButton.AddToClassList(actionButtonClass);
                actionsArea.Add(cloneButton);

                // Rename Button
                var renameButton = new Button(() => OpenRenamePopup(dataObject))
                {
                    text = "✎", // Pencil emoji or "Rename"
                    tooltip = "Rename Asset",
                    style = { color = new Color(0.2f, 0.6f, 0.9f) },
                };
                renameButton.AddToClassList(actionButtonClass);
                actionsArea.Add(renameButton);

                // Delete Button
                var deleteButton = new Button(() => DeleteObject(dataObject))
                {
                    text = "X", // Trash can emoji U+1F5D1
                    tooltip = "Delete Object",
                    style = { color = new Color(0.9f, 0.4f, 0.4f) }, // Make delete reddish? Requires USS usually
                };
                deleteButton.AddToClassList(actionButtonClass);
                deleteButton.AddToClassList("delete-button");
                // deleteButton.AddToClassList("delete-button"); // For specific styling via USS
                actionsArea.Add(deleteButton);

                // Store mapping (use the main row element for selection/drag)
                _objectVisualElementMap[dataObject] = objectItemRow;
                _objectListContainer.Add(objectItemRow);

                // Re-apply selection style if needed
                if (_selectedObject != null && _selectedObject == dataObject)
                {
                    objectItemRow.AddToClassList("selected");
                    _selectedElement = objectItemRow;
                }

                // VisualElement objectItem = new()
                // {
                //     name = $"object-item-{dataObject.GetInstanceID()}",
                // };
                // objectItem.AddToClassList("object-item");
                // objectItem.userData = dataObject;
                //
                // Label titleLabel = new(dataObject.Title) { name = "object-item-label" };
                // titleLabel.AddToClassList("object-item__label");
                // objectItem.Add(titleLabel);
                //
                // objectItem.RegisterCallback<PointerDownEvent>(OnObjectPointerDown);
                //
                // _objectVisualElementMap[dataObject] = objectItem;
                // _objectListContainer.Add(objectItem);
                //
                // if (_selectedObject != null && _selectedObject == dataObject)
                // {
                //     objectItem.AddToClassList("selected");
                //     _selectedElement = objectItem;
                // }
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

            if (_selectedObject == null || _currentInspectorScriptableObject == null)
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

            bool useOdinInspector = false;
#if ODIN_INSPECTOR
            useOdinInspector =
                !_selectedObject
                    .GetType()
                    .IsAttributeDefined(out DataVisualizerCustomPropertiesAttribute attribute)
                || attribute.UseOdinInspector;

            if (useOdinInspector)
            {
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
                            _odinPropertyTree.OnPropertyValueChanged -=
                                HandleOdinPropertyValueChanged;
                            _odinPropertyTree.Dispose();
                        }

                        _odinPropertyTree = PropertyTree.Create(_selectedObject);
                        _odinPropertyTree.OnPropertyValueChanged += HandleOdinPropertyValueChanged;
                        _odinInspectorContainer?.MarkDirtyRepaint();
                    }

                    if (_odinInspectorContainer == null)
                    {
                        _odinInspectorContainer = new IMGUIContainer(
                            () => _odinPropertyTree?.Draw()
                        )
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
            }
#endif
            if (!useOdinInspector)
            {
#if ODIN_INSPECTOR
                if (
                    _odinInspectorContainer != null
                    && _odinInspectorContainer.parent == _inspectorContainer
                )
                {
                    _odinInspectorContainer.RemoveFromHierarchy();
                }
                _odinPropertyTree?.Dispose();
                _odinPropertyTree = null;
#endif
                try
                {
                    _currentInspectorScriptableObject.UpdateIfRequiredOrScript();
                    SerializedProperty serializedProperty =
                        _currentInspectorScriptableObject.GetIterator();
                    bool enterChildren = true;
                    const string titleFieldName = nameof(BaseDataObject._title);

                    if (serializedProperty.NextVisible(enterChildren))
                    {
                        using (
                            new EditorGUI.DisabledScope(
                                "m_Script" == serializedProperty.propertyPath
                            )
                        )
                        {
                            PropertyField scriptField = new(serializedProperty);
                            scriptField.Bind(_currentInspectorScriptableObject);
                            _inspectorContainer.Add(scriptField);
                        }

                        enterChildren = false;
                    }

                    while (serializedProperty.NextVisible(enterChildren))
                    {
                        SerializedProperty currentPropCopy = serializedProperty.Copy();
                        PropertyField propertyField = new(currentPropCopy);
                        propertyField.Bind(_currentInspectorScriptableObject);

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
                                _currentInspectorScriptableObject.ApplyModifiedProperties();
                                rootVisualElement
                                    .schedule.Execute(
                                        () => RefreshSelectedElementVisuals(_selectedObject)
                                    )
                                    .ExecuteLater(1);
                            });
                        }

                        _inspectorContainer.Add(propertyField);
                        enterChildren = false;
                    }

                    VisualElement customElement = _selectedObject.BuildGUI(
                        new DataVisualizerGUIContext(_currentInspectorScriptableObject)
                    );
                    if (customElement != null)
                    {
                        _inspectorContainer.Add(customElement);
                    }
                }
                catch (Exception e)
                {
                    this.LogError($"Error creating standard inspector.", e);
                    _inspectorContainer.Add(new Label($"Inspector Error: {e.Message}"));
                }
            }
        }

        private void DeleteObject(BaseDataObject objectToDelete)
        {
            if (objectToDelete == null)
                return;

            // Confirmation Dialog
            if (
                EditorUtility.DisplayDialog(
                    "Confirm Delete",
                    $"Are you sure you want to delete the asset '{objectToDelete.name}'?\nThis action cannot be undone.",
                    "Delete",
                    "Cancel"
                )
            )
            {
                string path = AssetDatabase.GetAssetPath(objectToDelete);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogError(
                        $"Could not find asset path for '{objectToDelete.name}'. Cannot delete."
                    );
                    return;
                }

                Debug.Log($"Attempting to delete asset: {path}");

                // Remove from internal list and map FIRST
                bool removed = _selectedObjects.Remove(objectToDelete);
                _objectVisualElementMap.Remove(objectToDelete, out var visualElement);

                // Delete the asset file
                bool deleted = AssetDatabase.DeleteAsset(path);

                if (deleted)
                {
                    Debug.Log($"Asset '{path}' deleted successfully.");
                    // Optionally save/refresh database
                    // AssetDatabase.SaveAssets(); // Usually not needed after DeleteAsset
                    AssetDatabase.Refresh();

                    // Remove visual element from the list container
                    visualElement?.RemoveFromHierarchy();

                    // Clear selection if the deleted object was selected
                    if (_selectedObject == objectToDelete)
                    {
                        SelectObject(null); // This will clear selection and update inspector
                    }
                    // No need to call BuildObjectsView if we manually remove the element.
                    // If list order matters beyond _customOrder, might need rebuild.
                }
                else
                {
                    Debug.LogError($"Failed to delete asset at '{path}'.");
                    // If delete failed, add object back to list? Or refresh view?
                    // For safety, let's rebuild the view to reflect actual state.
                    LoadObjectTypes(_selectedType); // Reload objects for current type
                    BuildObjectsView(); // Rebuild view fully
                    // Re-select previously selected if it wasn't the one we tried to delete
                    SelectObject(_selectedObject);
                }
            }
        }

        private void OpenRenamePopup(BaseDataObject objectToRename)
        {
            if (objectToRename == null)
                return;
            string currentPath = AssetDatabase.GetAssetPath(objectToRename);
            if (string.IsNullOrEmpty(currentPath))
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Cannot rename object: Asset path not found.",
                    "OK"
                );
                return;
            }

            // Get reference to this window to pass to popup for callback
            DataVisualizer mainVisualizerWindow = this;

            // Create and show the modal window
            RenameAssetPopup.ShowWindow(
                currentPath,
                (renameSuccessful) =>
                {
                    // This callback executes after the popup closes
                    Debug.Log($"Rename popup closed. Success: {renameSuccessful}");
                    if (renameSuccessful)
                    {
                        // Refreshing the object view might be needed if title depends on name,
                        // or just refresh the specific element. AssetDatabase refresh might trigger some updates too.
                        // Find the element associated with the (now potentially renamed) object.
                        // Note: objectToRename instance might still hold old name until reloaded?
                        // Safest bet is to fully reload and rebuild.
                        if (_selectedType != null)
                        { // Ensure type context is still valid
                            LoadObjectTypes(_selectedType);
                            BuildObjectsView();
                            // Try to re-select the object (it's the same instance)
                            SelectObject(objectToRename);
                        }
                        else
                        {
                            BuildObjectsView(); // Rebuild without type context if needed
                        }
                    }
                    // Ensure focus returns to main window (ShowModalUtility usually handles this)
                    mainVisualizerWindow?.Focus();
                }
            );
        }

        private void CloneObject(BaseDataObject originalObject)
        {
            if (originalObject == null)
                return;

            string originalPath = AssetDatabase.GetAssetPath(originalObject);
            if (string.IsNullOrEmpty(originalPath))
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Cannot clone object: Original asset path not found.",
                    "OK"
                );
                return;
            }

            // 1. Instantiate a copy
            BaseDataObject cloneInstance = Instantiate(originalObject); // Copies serialized data
            if (cloneInstance == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Failed to instantiate a clone of the object.",
                    "OK"
                );
                return;
            }
            cloneInstance._assetGuid = Guid.NewGuid().ToString(); // Generate a new GUID
            // Note: Instantiate often triggers OnEnable/Awake. Ensure any Guid generation in BaseDataObject handles cloning (e.g., assigns a NEW Guid).


            // 2. Generate Unique Path in the same directory
            string directory = Path.GetDirectoryName(originalPath).Replace('\\', '/');
            string originalName = Path.GetFileNameWithoutExtension(originalPath);
            string extension = Path.GetExtension(originalPath); // Should be ".asset"
            string proposedName = $"{originalName} (Clone){extension}"; // Suggest a name
            string proposedPath = Path.Combine(directory, proposedName).Replace('\\', '/');
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(proposedPath);
            string uniqueName = Path.GetFileNameWithoutExtension(uniquePath); // Get the final unique name

            // 3. Update Clone's Title (optional, based on new unique asset name)
            cloneInstance._title = uniqueName; // Set title to match unique asset name

            // 4. Create Asset
            try
            {
                AssetDatabase.CreateAsset(cloneInstance, uniquePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Cloned object to: {uniquePath}");

                // Load the clone to work with the asset instance
                BaseDataObject cloneAsset = AssetDatabase.LoadAssetAtPath<BaseDataObject>(
                    uniquePath
                );
                if (cloneAsset != null)
                {
                    // --- Update UI ---
                    // Add clone to list (e.g., after original or at end)
                    int originalIndex = _selectedObjects.IndexOf(originalObject);
                    if (originalIndex >= 0)
                    {
                        _selectedObjects.Insert(originalIndex + 1, cloneAsset);
                    }
                    else
                    {
                        _selectedObjects.Add(cloneAsset); // Add to end if original somehow wasn't found
                    }

                    // Optional: Update _customOrder if using it, might need to shift subsequent items
                    // For now, let's assume LoadObjectTypes will sort correctly on next load.
                    UpdateAndSaveObjectCustomOrder();
                    // Rebuild view and select clone
                    BuildObjectsView();
                    SelectObject(cloneAsset);
                }
                else
                {
                    Debug.LogError($"Failed to load the cloned asset at {uniquePath}");
                    BuildObjectsView(); // Rebuild anyway
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    "Error Cloning Asset",
                    $"Failed to create cloned asset at '{uniquePath}': {e.Message}",
                    "OK"
                );
                DestroyImmediate(cloneInstance); // Clean up instance if saving failed
            }
        }

        private BaseDataObject DetermineObjectToAutoSelect()
        {
            if (!_selectedObjects.Any() || _selectedType == null)
            {
                return null;
            }

            BaseDataObject objectToSelect = null;
            string objPrefsKey = string.Format(LastSelectedObjectFormat, _selectedType.Name);
            string savedObjectGuid = EditorPrefs.GetString(objPrefsKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(savedObjectGuid))
            {
                objectToSelect = _selectedObjects.Find(obj =>
                {
                    if (obj == null)
                    {
                        return false;
                    }

                    string path = AssetDatabase.GetAssetPath(obj);
                    return !string.IsNullOrEmpty(path)
                        && string.Equals(
                            AssetDatabase.AssetPathToGUID(path),
                            savedObjectGuid,
                            StringComparison.OrdinalIgnoreCase
                        );
                });
            }
            if (objectToSelect == null)
            {
                objectToSelect = _selectedObjects[0];
            }
            return objectToSelect;
        }

        private static void ApplyNamespaceCollapsedState(
            Label indicator,
            VisualElement typesContainer,
            bool collapsed,
            bool saveState
        )
        {
            if (indicator == null || typesContainer == null)
            {
                return;
            }

            indicator.text = collapsed ? ArrowCollapsed : ArrowExpanded;
            typesContainer.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;

            if (saveState)
            {
                string namespaceKey = typesContainer.parent?.userData as string;
                if (string.IsNullOrWhiteSpace(namespaceKey))
                {
                    return;
                }

                string collapsePrefsKey = string.Format(
                    NamespaceCollapsedStateFormat,
                    namespaceKey
                );
                EditorPrefs.SetBool(collapsePrefsKey, collapsed);
            }
        }

        private void SaveNamespaceAndTypeSelectionState(string namespaceKey, string typeName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(namespaceKey))
                {
                    return;
                }

                EditorPrefs.SetString(LastSelectedNamespaceKey, namespaceKey);
                if (string.IsNullOrWhiteSpace(typeName))
                {
                    return;
                }

                string typePrefsKey = string.Format(LastSelectedTypeFormat, namespaceKey);
                EditorPrefs.SetString(typePrefsKey, typeName);
            }
            catch (Exception e)
            {
                this.LogError($"Error saving type/namespace selection state.", e);
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
                try
                {
                    if (_selectedType != null)
                    {
                        string namespaceKey = GetNamespaceKey(_selectedType);
                        string typeName = _selectedType.Name;
                        string assetPath = AssetDatabase.GetAssetPath(_selectedObject);
                        string objectGuid = null;
                        if (!string.IsNullOrWhiteSpace(assetPath))
                        {
                            objectGuid = AssetDatabase.AssetPathToGUID(assetPath);
                        }

                        if (!string.IsNullOrWhiteSpace(namespaceKey))
                        {
                            EditorPrefs.SetString(LastSelectedNamespaceKey, namespaceKey);
                        }

                        if (!string.IsNullOrWhiteSpace(typeName))
                        {
                            string typePrefsKey = string.Format(
                                LastSelectedTypeFormat,
                                namespaceKey
                            );
                            EditorPrefs.SetString(typePrefsKey, typeName);
                        }

                        if (!string.IsNullOrWhiteSpace(objectGuid))
                        {
                            string objPrefsKey = string.Format(LastSelectedObjectFormat, typeName);
                            EditorPrefs.SetString(objPrefsKey, objectGuid);
                        }
                        else
                        {
                            string objPrefsKey = string.Format(LastSelectedObjectFormat, typeName);
                            EditorPrefs.DeleteKey(objPrefsKey);
                        }
                    }
                }
                catch (Exception e)
                {
                    this.LogError($"Error saving selection state.", e);
                }
            }
            else
            {
                Selection.activeObject = null;
            }

            _currentInspectorScriptableObject?.Dispose();
            _currentInspectorScriptableObject =
                (dataObject != null) ? new SerializedObject(dataObject) : null;
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
                        {
                            PerformObjectDrop();
                            break;
                        }
                        case DragType.Namespace:
                        {
                            PerformNamespaceDrop();
                            break;
                        }
                        case DragType.Type:
                        {
                            PerformTypeDrop();
                            break;
                        }
                        default:
                        {
                            throw new InvalidEnumArgumentException(
                                nameof(dropType),
                                (int)dropType,
                                typeof(DragType)
                            );
                        }
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
                catch (Exception e)
                {
                    this.LogError($"Error creating in-place ghost.", e);
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
                {
                    container = _objectListContainer.contentContainer;
                    positioningParent = _objectListContainer.contentContainer;
                    break;
                }
                case DragType.Namespace:
                {
                    container = _namespaceListContainer;
                    positioningParent = _namespaceListContainer;
                    break;
                }
                case DragType.Type:
                {
                    if (_draggedElement != null)
                    {
                        container = _draggedElement.parent;
                    }

                    positioningParent = container;
                    break;
                }
                default:
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

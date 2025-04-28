namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using Components;
    using Components.UnityHelpers.Editor;
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
    using Data;
    using UnityEditor.UIElements;
    using Object = UnityEngine.Object;

    public sealed class DataVisualizer : EditorWindow
    {
        private const string PrefsPrefix = "WallstopStudios.UnityHelpers.DataVisualizer.";

        private const string PrefsSplitterOuterKey = PrefsPrefix + "SplitterOuterFixedPaneWidth";
        private const string PrefsSplitterInnerKey = PrefsPrefix + "SplitterInnerFixedPaneWidth";

        private const string SettingsDefaultPath = "Assets/DataVisualizerSettings.asset";
        private const string UserStateFileName = "DataVisualizerUserState.json";

        private const string NamespaceItemClass = "object-item";
        private const string NamespaceHeaderClass = "namespace-header";
        private const string NamespaceIndicatorClass = "namespace-indicator";
        private const string NamespaceLabelClass = "object-item__label";
        private const string TypeItemClass = "type-item";
        private const string TypeLabelClass = "type-item__label";
        private const string ObjectItemClass = "object-item";
        private const string ObjectItemContentClass = "object-item-content";
        private const string ObjectItemActionsClass = "object-item-actions";
        private const string ActionButtonClass = "action-button";

        private const string ArrowCollapsed = "►";
        private const string ArrowExpanded = "▼";

        private const float DragDistanceThreshold = 5f;
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
        private VisualElement _namespaceColumnElement;
        private VisualElement _objectColumnElement;

        private float _lastSavedOuterWidth = -1f;
        private float _lastSavedInnerWidth = -1f;
        private IVisualElementScheduledItem _saveWidthsTask;

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

        private string _userStateFilePath; // Full path, determined in OnEnable
        private DataVisualizerUserState _userState; // Holds state when using file persistence
        private bool _userStateDirty = false; // Track if file needs saving

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
            _userStateFilePath = Path.Combine(Application.persistentDataPath, UserStateFileName);
            if (!_settings.PersistStateInSettingsAsset)
            {
                LoadUserStateFromFile();
            }
            else
            {
                _userState = new DataVisualizerUserState(); // Have an empty object if using settings asset
            }

            LoadScriptableObjectTypes();
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
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
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
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
            if (!_settings.PersistStateInSettingsAsset && _userStateDirty)
            {
                SaveUserStateToFile();
            }

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
            DataVisualizer window = GetWindow<DataVisualizer>(false, null, false);
            if (window != null)
            {
                window.ScheduleRefresh();
            }
        }

        private void ScheduleRefresh()
        {
            rootVisualElement.schedule.Execute(RefreshAllViews).ExecuteLater(50);
        }

        private void RefreshAllViews()
        {
            if (_settings == null) { }

            string previousNamespaceKey =
                _selectedType != null ? GetNamespaceKey(_selectedType) : null;
            string previousTypeName = _selectedType?.Name;
            string previousObjectGuid = null;
            if (_selectedObject != null)
            {
                string path = AssetDatabase.GetAssetPath(_selectedObject);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    previousObjectGuid = AssetDatabase.AssetPathToGUID(path);
                }
            }

            LoadScriptableObjectTypes();

            _selectedType = null;
            _selectedObject = null;
            _selectedElement = null;
            _selectedTypeElement = null;

            int namespaceIndex = -1;
            if (!string.IsNullOrWhiteSpace(previousNamespaceKey))
            {
                namespaceIndex = _scriptableObjectTypes.FindIndex(kvp =>
                    string.Equals(kvp.key, previousNamespaceKey, StringComparison.Ordinal)
                );
            }
            if (namespaceIndex < 0 && 0 < _scriptableObjectTypes.Count)
            {
                namespaceIndex = 0;
            }

            if (0 <= namespaceIndex)
            {
                List<Type> typesInNamespace = _scriptableObjectTypes[namespaceIndex].types;
                if (0 < typesInNamespace.Count)
                {
                    if (!string.IsNullOrWhiteSpace(previousTypeName))
                    {
                        _selectedType = typesInNamespace.FirstOrDefault(t =>
                            string.Equals(t.Name, previousTypeName, StringComparison.Ordinal)
                        );
                    }

                    _selectedType ??= typesInNamespace[0];
                }
            }

            if (_selectedType != null)
            {
                LoadObjectTypes(_selectedType);
            }
            else
            {
                _selectedObjects.Clear();
            }

            if (
                _selectedType != null
                && !string.IsNullOrWhiteSpace(previousObjectGuid)
                && 0 < _selectedObjects.Count
            )
            {
                _selectedObject = _selectedObjects.Find(obj =>
                {
                    if (obj == null)
                    {
                        return false;
                    }

                    string path = AssetDatabase.GetAssetPath(obj);
                    return !string.IsNullOrWhiteSpace(path)
                        && string.Equals(
                            AssetDatabase.AssetPathToGUID(path),
                            previousObjectGuid,
                            StringComparison.OrdinalIgnoreCase
                        );
                });
            }

            BuildNamespaceView();
            BuildObjectsView();

            VisualElement typeElementToSelect = FindTypeElement(_selectedType);
            if (typeElementToSelect != null)
            {
                _selectedTypeElement = typeElementToSelect;
                _selectedTypeElement.AddToClassList("selected");
                VisualElement ancestorGroup = FindAncestorNamespaceGroup(_selectedTypeElement);
                if (ancestorGroup != null)
                {
                    ExpandNamespaceGroupIfNeeded(ancestorGroup, false);
                }
            }

            SelectObject(_selectedObject);
        }

        private VisualElement FindAncestorNamespaceGroup(VisualElement startingElement)
        {
            if (startingElement == null)
            {
                return null;
            }

            VisualElement currentElement = startingElement;
            while (currentElement != null && currentElement != _namespaceListContainer)
            {
                if (currentElement.ClassListContains("object-item"))
                {
                    return currentElement;
                }
                currentElement = currentElement.parent;
            }
            return null;
        }

        private void ExpandNamespaceGroupIfNeeded(VisualElement namespaceGroupItem, bool saveState)
        {
            if (namespaceGroupItem == null)
            {
                return;
            }

            Label indicator = namespaceGroupItem.Q<Label>(className: "namespace-indicator");
            string nsKey = namespaceGroupItem.userData as string;
            VisualElement typesContainer = namespaceGroupItem.Q<VisualElement>(
                $"types-container-{nsKey}"
            );

            if (
                indicator != null
                && typesContainer != null
                && typesContainer.style.display == DisplayStyle.None
            )
            {
                ApplyNamespaceCollapsedState(indicator, typesContainer, false, saveState);
            }
        }

        private DataVisualizerSettings LoadOrCreateSettings()
        {
            DataVisualizerSettings settings = null;

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(DataVisualizerSettings)}");

            if (0 < guids.Length)
            {
                if (1 < guids.Length)
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
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
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
            _saveWidthsTask?.Pause();
            _saveWidthsTask = rootVisualElement
                .schedule.Execute(CheckAndSaveSplitterWidths)
                .Every(1000);
        }

        private void CheckAndSaveSplitterWidths()
        {
            if (
                _outerSplitView == null
                || _innerSplitView == null
                || _namespaceColumnElement == null
                || _objectColumnElement == null
                || float.IsNaN(_namespaceColumnElement.resolvedStyle.width)
                || float.IsNaN(_objectColumnElement.resolvedStyle.width)
            )
            {
                return;
            }

            float currentOuterWidth = _namespaceColumnElement.resolvedStyle.width;
            float currentInnerWidth = _objectColumnElement.resolvedStyle.width;

            if (!Mathf.Approximately(currentOuterWidth, _lastSavedOuterWidth))
            {
                EditorPrefs.SetFloat(PrefsSplitterOuterKey, currentOuterWidth);
                _lastSavedOuterWidth = currentOuterWidth;
            }

            if (!Mathf.Approximately(currentInnerWidth, _lastSavedInnerWidth))
            {
                EditorPrefs.SetFloat(PrefsSplitterInnerKey, currentInnerWidth);
                _lastSavedInnerWidth = currentInnerWidth;
            }
        }

        private void OnUndoRedoPerformed()
        {
            ScheduleRefresh();
        }

        private void RestorePreviousSelection()
        {
            if (_scriptableObjectTypes.Count == 0)
            {
                return;
            }

            string savedNamespaceKey = GetLastSelectedNamespaceKey();
            List<Type> typesInNamespace;
            int namespaceIndex = -1;

            if (!string.IsNullOrWhiteSpace(savedNamespaceKey))
            {
                namespaceIndex = _scriptableObjectTypes.FindIndex(kvp =>
                    string.Equals(kvp.key, savedNamespaceKey, StringComparison.Ordinal)
                );
            }

            if (0 <= namespaceIndex)
            {
                typesInNamespace = _scriptableObjectTypes[namespaceIndex].types;
            }
            else if (0 < _scriptableObjectTypes.Count)
            {
                (string key, List<Type> types) types = _scriptableObjectTypes[0];
                typesInNamespace = types.types;
            }
            else
            {
                typesInNamespace = null;
            }

            if (typesInNamespace is not { Count: > 0 })
            {
                return;
            }

            string savedTypeName = GetLastSelectedTypeName();
            Type selectedType = null;

            if (!string.IsNullOrWhiteSpace(savedTypeName))
            {
                selectedType = typesInNamespace.Find(t =>
                    string.Equals(t.Name, savedTypeName, StringComparison.Ordinal)
                );
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

            string savedObjectGuid = null;
            if (_selectedType != null)
            {
                savedObjectGuid = GetLastSelectedObjectGuidForType(_selectedType.Name); // Use helper
            }
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

            if (objectToSelect == null && 0 < _selectedObjects.Count)
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

            Button settingsButton = new(() =>
            {
                if (_settings == null)
                {
                    _settings = LoadOrCreateSettings();
                }

                bool wasSettingsAssetMode = _settings.PersistStateInSettingsAsset;
                // Show the modal window, passing settings and the callback with the original mode
                var popupWindow = DataVisualizerSettingsPopup.CreateAndConfigureInstance(
                    _settings,
                    () => HandleSettingsPopupClosed(wasSettingsAssetMode) // Pass callback referencing original mode
                );
                popupWindow.ShowModal();
            })
            {
                text = "…",
                name = "settings-button",
            };
            settingsButton.AddToClassList("icon-button");
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
            _namespaceColumnElement = CreateNamespaceColumn();
            _objectColumnElement = CreateObjectColumn();
            VisualElement inspectorColumn = CreateInspectorColumn();

            _innerSplitView = new TwoPaneSplitView(
                0,
                (int)initialInnerWidth,
                TwoPaneSplitViewOrientation.Horizontal
            )
            {
                name = "inner-split-view",
                style = { flexGrow = 1 },
            };

            _innerSplitView.Add(_objectColumnElement);
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
            _outerSplitView.Add(_namespaceColumnElement);
            _outerSplitView.Add(_innerSplitView);
            root.Add(_outerSplitView);

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
            BuildNamespaceView();
            BuildObjectsView();
            BuildInspectorView();
        }

        private void HandleSettingsPopupClosed(bool previousModeWasSettingsAsset)
        {
            if (_settings == null)
            {
                _settings = LoadOrCreateSettings();
            }

            if (_settings != null && EditorUtility.IsDirty(_settings))
            {
                AssetDatabase.SaveAssets();
            }

            bool currentModeIsSettingsAsset = _settings.PersistStateInSettingsAsset;
            bool migrationNeeded = (previousModeWasSettingsAsset != currentModeIsSettingsAsset);

            // 4. Perform Migration if needed
            if (migrationNeeded)
            {
                Debug.Log(
                    $"Persistence mode changed from {(previousModeWasSettingsAsset ? "SettingsAsset" : "UserFile")} to {(currentModeIsSettingsAsset ? "SettingsAsset" : "UserFile")}. Migrating state..."
                );
                MigratePersistenceState(migrateToSettingsAsset: currentModeIsSettingsAsset); // Pass the NEW mode as target

                // 5. Persist Migrated Data
                if (currentModeIsSettingsAsset)
                {
                    // Data was migrated TO settings asset, MarkSettingsDirty was called inside Migrate...
                    // Save the settings asset again to persist migrated list data etc.
                    if (EditorUtility.IsDirty(_settings))
                    { // Check if migration actually changed anything
                        Debug.Log("Saving settings asset again after migration.");
                        AssetDatabase.SaveAssets();
                    }
                }
                else
                {
                    // Data was migrated TO user state object (_userState)
                    // Save the user state file
                    Debug.Log("Saving user state file after migration.");
                    SaveUserStateToFile(); // Save the migrated state to the JSON file
                }
            }

            // 6. Optional: Force a full refresh of the main window UI
            //    This might be good practice after changing persistence method or settings.
            Debug.Log("Scheduling full refresh after settings close.");
            ScheduleRefresh();
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
                },
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
                        marginTop = 4,
                        marginLeft = 1,
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
                },
            };

            VisualElement objectHeader = new()
            {
                name = "object-header",
                style =
                {
                    flexDirection = FlexDirection.Row,
                    //justifyContent = Justify.SpaceBetween,
                    justifyContent = Justify.FlexStart,
                    alignItems = Align.Center,
                    paddingBottom = 3,
                    paddingTop = 3,
                    paddingLeft = 3,
                    paddingRight = 3,
                    height = 24,
                    flexShrink = 0,
                    borderBottomWidth = 1,
                    borderBottomColor = Color.gray,
                },
            };

            objectHeader.Add(
                new Label("Objects")
                {
                    style =
                    {
                        unityFontStyleAndWeight = FontStyle.Bold,
                        paddingBottom = 5,
                        marginRight = 5,
                        marginTop = 1,
                    },
                }
            );
            Button createButton = new(CreateNewObject)
            {
                text = "+",
                tooltip = "Create New Object",
                name = "create-object-button",
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = Color.black,
                    backgroundColor = new Color(0f, 0.8f, 0f),
                    width = 20,
                    height = 20,
                    paddingLeft = 0,
                    paddingRight = 0,
                    marginBottom = 2,
                },
            };
            createButton.AddToClassList("icon-button");
            objectHeader.Add(createButton);
            objectColumn.Add(objectHeader);
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
                style = { flexGrow = 1, height = Length.Percent(100) },
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
            if (_selectedType == null)
            {
                EditorUtility.DisplayDialog(
                    "Cannot Create Object",
                    "Please select a Type in the first column before creating an object.",
                    "OK"
                );
                return;
            }

            if (_settings == null || string.IsNullOrWhiteSpace(_settings.DataFolderPath))
            {
                EditorUtility.DisplayDialog(
                    "Cannot Create Object",
                    "Data Folder Path is not set correctly in Settings.",
                    "OK"
                );
                return;
            }

            string targetDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                _settings.DataFolderPath
            );
            targetDirectory = Path.GetFullPath(targetDirectory).Replace('\\', '/');

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

            ScriptableObject instance = CreateInstance(_selectedType);
            if (instance == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Failed to create instance of type '{_selectedType.Name}'.",
                    "OK"
                );
                return;
            }

            string baseAssetName = $"New {_selectedType.Name}.asset";
            string proposedPath = Path.Combine(_settings.DataFolderPath, baseAssetName)
                .Replace('\\', '/');
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(proposedPath);

            try
            {
                AssetDatabase.CreateAsset(instance, uniquePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                BaseDataObject newObject = AssetDatabase.LoadAssetAtPath<BaseDataObject>(
                    uniquePath
                );

                if (newObject != null)
                {
                    _selectedObjects.Insert(0, newObject);
                    UpdateAndSaveObjectCustomOrder();
                    BuildObjectsView();
                    SelectObject(newObject);
                }
                else
                {
                    this.LogError($"Failed to load the newly created asset at {uniquePath}");
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
                DestroyImmediate(instance);
            }
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
            bool titlePotentiallyChanged = string.Equals(
                property.Name,
                titleFieldName,
                StringComparison.Ordinal
            );

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

            VisualElement prefsToggleContainer = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 10,
                },
            };
            _settingsPopup.Add(prefsToggleContainer);

            Toggle prefsToggle = new("Use Settings Asset for State:")
            {
                value = _settings.PersistStateInSettingsAsset,
                tooltip =
                    $"If checked, window state (selection, order, collapse) is saved within the DataVisualizerSettings asset file.{Environment.NewLine}If unchecked state is saved locally in a JSON file (persistent data path).",
            };
            prefsToggle.RegisterValueChangedCallback(evt =>
            {
                if (_settings != null)
                {
                    bool newModeIsSettingsAsset = evt.newValue;
                    bool previousModeWasSettingsAsset = _settings.PersistStateInSettingsAsset;

                    if (previousModeWasSettingsAsset != newModeIsSettingsAsset)
                    {
                        // Update flag FIRST
                        _settings.PersistStateInSettingsAsset = newModeIsSettingsAsset;
                        Debug.Log(
                            $"Persistence mode changed to: {(newModeIsSettingsAsset ? "Settings Asset" : "User File")}"
                        );

                        // Perform Migration
                        MigratePersistenceState(migrateToSettingsAsset: newModeIsSettingsAsset); // Pass the NEW mode flag

                        // Mark settings SO dirty (flag changed, maybe data too)
                        MarkSettingsDirty();
                        // Save settings SO immediately to persist flag change and potential migrated data
                        AssetDatabase.SaveAssets();

                        // Save user state file if THAT is the new mode
                        if (!newModeIsSettingsAsset)
                        {
                            SaveUserStateToFile();
                        }

                        Debug.Log("Persistence mode change and migration complete.");
                    }
                }
            });
            prefsToggleContainer.Add(prefsToggle);

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
                if (types.Count == 0)
                {
                    namespaceGroupItem.AddToClassList("namespace-group-item--empty");
                }
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

                bool isCollapsed = GetIsNamespaceCollapsed(key);
                ApplyNamespaceCollapsedState(indicator, typesContainer, isCollapsed, false);

                indicator.RegisterCallback<PointerDownEvent>(evt =>
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
                        && !string.IsNullOrWhiteSpace(nsKey)
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
                    VisualElement typeItem = new()
                    {
                        name = $"type-item-{type.Name}",
                        userData = type,
                        pickingMode = PickingMode.Position,
                        focusable = true,
                    };

                    typeItem.AddToClassList(TypeItemClass);
                    Label typeLabel = new(type.Name) { name = "type-item-label" };
                    typeLabel.AddToClassList(TypeLabelClass);
                    typeItem.Add(typeLabel);

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

            if (_selectedType != null && _selectedObjects.Count == 0)
            {
                Label emptyLabel = new(
                    $"No objects of type '{_selectedType.Name}' found.\nUse the '+' button above to create one."
                )
                {
                    name = "empty-object-list-label",
                };
                emptyLabel.AddToClassList("empty-object-list-label");
                _objectListContainer.Add(emptyLabel);
            }

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
                objectItemRow.AddToClassList(ObjectItemClass);
                objectItemRow.style.flexDirection = FlexDirection.Row;
                objectItemRow.style.alignItems = Align.Center;
                objectItemRow.userData = dataObject;
                objectItemRow.RegisterCallback<PointerDownEvent>(OnObjectPointerDown);

                VisualElement contentArea = new() { name = "content" };
                contentArea.AddToClassList(ObjectItemContentClass);
                objectItemRow.Add(contentArea);

                Label titleLabel = new(dataObject.Title) { name = "object-item-label" };
                titleLabel.AddToClassList("object-item__label");
                contentArea.Add(titleLabel);

                VisualElement actionsArea = new()
                {
                    name = "actions",
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center,
                        flexShrink = 0,
                    },
                };
                actionsArea.AddToClassList(ObjectItemActionsClass);
                objectItemRow.Add(actionsArea);

                Button cloneButton = new(() => CloneObject(dataObject))
                {
                    text = "++",
                    tooltip = "Clone Object",
                    style =
                    {
                        unityFontStyleAndWeight = FontStyle.Bold,
                        color = new Color(0.4f, 0.7f, 0.4f),
                    },
                };
                cloneButton.AddToClassList(ActionButtonClass);
                actionsArea.Add(cloneButton);

                Button renameButton = new(() => OpenRenamePopup(dataObject))
                {
                    text = "✎",
                    style =
                    {
                        unityFontStyleAndWeight = FontStyle.Bold,
                        color = new Color(0.2f, 0.6f, 0.9f),
                    },
                };
                renameButton.AddToClassList(ActionButtonClass);
                actionsArea.Add(renameButton);

                Button deleteButton = new(() => DeleteObject(dataObject))
                {
                    text = "X",
                    style =
                    {
                        unityFontStyleAndWeight = FontStyle.Bold,
                        color = new Color(0.9f, 0.4f, 0.4f),
                    },
                };
                deleteButton.AddToClassList(ActionButtonClass);
                deleteButton.AddToClassList("delete-button");
                actionsArea.Add(deleteButton);

                _objectVisualElementMap[dataObject] = objectItemRow;
                _objectListContainer.Add(objectItemRow);

                if (_selectedObject == dataObject)
                {
                    objectItemRow.AddToClassList("selected");
                    _selectedElement = objectItemRow;
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

                    if (serializedProperty.NextVisible(true))
                    {
                        using (
                            new EditorGUI.DisabledScope(
                                string.Equals(
                                    "m_Script",
                                    serializedProperty.propertyPath,
                                    StringComparison.Ordinal
                                )
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
            string objectName = objectToDelete.name; // Capture name before potential deletion

            // --- Show Custom Confirmation Dialog ---
            var popup = ConfirmActionPopup.CreateAndConfigureInstance(
                title: "Confirm Delete",
                message: $"Are you sure you want to delete the asset '{objectName}'?\nThis action cannot be undone.",
                confirmButtonText: "Delete", // Text for the confirmation button
                cancelButtonText: "Cancel", // Text for the cancel button
                position,
                onComplete: (confirmed) =>
                { // Callback executed AFTER popup closes
                    // Only proceed if the user clicked "Delete" (confirmed is true)
                    if (confirmed)
                    {
                        // --- Deletion Logic (Now inside the callback) ---
                        string path = AssetDatabase.GetAssetPath(objectToDelete);
                        if (string.IsNullOrEmpty(path))
                        {
                            Debug.LogError(
                                $"Could not find asset path for '{objectName}' post-confirmation. Cannot delete."
                            );
                            // Maybe refresh UI here just in case?
                            ScheduleRefresh();
                            return;
                        }

                        Debug.Log($"User confirmed deletion. Attempting to delete asset: {path}");

                        // Remove from internal list and map FIRST
                        bool removed = _selectedObjects.Remove(objectToDelete);
                        _objectVisualElementMap.Remove(objectToDelete, out var visualElement);

                        // Delete the asset file
                        bool deleted = AssetDatabase.DeleteAsset(path);

                        if (deleted)
                        {
                            Debug.Log($"Asset '{path}' deleted successfully.");
                            // Don't need SaveAssets after DeleteAsset usually. Refresh is good.
                            AssetDatabase.Refresh();

                            // Remove visual element from the list container
                            visualElement?.RemoveFromHierarchy();

                            // Clear selection if the deleted object was selected
                            if (_selectedObject == objectToDelete)
                            {
                                SelectObject(null); // Clears selection & updates inspector
                            }
                            // The AssetPostprocessor might trigger a refresh anyway,
                            // but removing the element manually provides immediate feedback.
                        }
                        else
                        {
                            Debug.LogError(
                                $"Failed to delete asset at '{path}'. Rebuilding view to sync."
                            );
                            // Rebuild view fully to reflect actual state if delete failed
                            ScheduleRefresh(); // Use the reliable refresh mechanism
                        }
                        // --- End Deletion Logic ---
                    }
                    else
                    {
                        Debug.Log("User cancelled deletion.");
                    }
                } // End callback lambda
            ); // End CreateAndConfigureInstance call

            // --- End Custom Confirmation Dialog ---

            // Show the popup modally relative to this DataVisualizer window
            // This ensures it's centered and blocks input to the parent.
            popup.ShowModalUtility();
            // Remove the old EditorUtility.DisplayDialog call entirely
        }

        private void OpenRenamePopup(BaseDataObject objectToRename)
        {
            if (objectToRename == null)
            {
                return;
            }

            string currentPath = AssetDatabase.GetAssetPath(objectToRename);
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Cannot rename object: Asset path not found.",
                    "OK"
                );
                return;
            }

            DataVisualizer mainVisualizerWindow = this;

            RenameAssetPopup.ShowWindow(
                currentPath,
                (renameSuccessful) =>
                {
                    if (renameSuccessful)
                    {
                        if (_selectedType != null)
                        {
                            LoadObjectTypes(_selectedType);
                            BuildObjectsView();
                            SelectObject(objectToRename);
                        }
                        else
                        {
                            BuildObjectsView();
                        }
                    }
                    mainVisualizerWindow?.Focus();
                }
            );
        }

        private void CloneObject(BaseDataObject originalObject)
        {
            if (originalObject == null)
            {
                return;
            }

            string originalPath = AssetDatabase.GetAssetPath(originalObject);
            if (string.IsNullOrWhiteSpace(originalPath))
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Cannot clone object: Original asset path not found.",
                    "OK"
                );
                return;
            }

            BaseDataObject cloneInstance = Instantiate(originalObject);
            if (cloneInstance == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Failed to instantiate a clone of the object.",
                    "OK"
                );
                return;
            }
            cloneInstance._assetGuid = Guid.NewGuid().ToString();

            string originalDirectory = Path.GetDirectoryName(originalPath);
            if (string.IsNullOrWhiteSpace(originalDirectory))
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Cannot clone object: Original asset path is invalid.",
                    "OK"
                );
                return;
            }

            string directory = originalDirectory.Replace('\\', '/');
            string originalName = Path.GetFileNameWithoutExtension(originalPath);
            string extension = Path.GetExtension(originalPath);
            string proposedName = $"{originalName} (Clone){extension}";
            string proposedPath = Path.Combine(directory, proposedName).Replace('\\', '/');
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(proposedPath);
            string uniqueName = Path.GetFileNameWithoutExtension(uniquePath);
            cloneInstance._title = uniqueName;

            try
            {
                AssetDatabase.CreateAsset(cloneInstance, uniquePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                BaseDataObject cloneAsset = AssetDatabase.LoadAssetAtPath<BaseDataObject>(
                    uniquePath
                );
                if (cloneAsset != null)
                {
                    int originalIndex = _selectedObjects.IndexOf(originalObject);
                    if (0 <= originalIndex)
                    {
                        _selectedObjects.Insert(originalIndex + 1, cloneAsset);
                    }
                    else
                    {
                        _selectedObjects.Add(cloneAsset);
                    }

                    UpdateAndSaveObjectCustomOrder();
                    BuildObjectsView();
                    SelectObject(cloneAsset);
                }
                else
                {
                    this.LogError($"Failed to load the cloned asset at {uniquePath}");
                    BuildObjectsView();
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    "Error Cloning Asset",
                    $"Failed to create cloned asset at '{uniquePath}': {e.Message}",
                    "OK"
                );
                DestroyImmediate(cloneInstance);
            }
        }

        // Helper method called when a Type is selected to decide which object to auto-select
        private BaseDataObject DetermineObjectToAutoSelect()
        {
            BaseDataObject objectToSelect = null;

            // 1. Ensure there's a selected type and objects have been loaded for it
            if (_selectedType == null || _selectedObjects == null || _selectedObjects.Count == 0)
            {
                // Debug.Log("DetermineObjectToAutoSelect: No selected type or no objects loaded.");
                return null; // No objects available for this type
            }

            // 2. Get the last selected Object GUID using the persistence helper
            //    The helper handles checking _settings.PersistStateInSettingsAsset
            string savedObjectGuid = GetLastSelectedObjectGuidForType(_selectedType.Name);
            // ... (Find objectToSelect using savedObjectGuid, fallback to first object) ...
            if (!string.IsNullOrEmpty(savedObjectGuid))
            {
                objectToSelect = _selectedObjects.FirstOrDefault(obj =>
                    obj != null
                    && AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj))
                        == savedObjectGuid
                );
            }
            if (objectToSelect == null && _selectedObjects.Count > 0)
            { // Fallback
                objectToSelect = _selectedObjects[0];
            }

            // Debug.Log($"DetermineObjectToAutoSelect: Checking for last object for type '{_selectedType.Name}'. Saved GUID: '{savedObjectGuid ?? "None"}'");

            // 3. Try to find the object matching the GUID in the current list
            if (!string.IsNullOrEmpty(savedObjectGuid))
            {
                objectToSelect = _selectedObjects.FirstOrDefault(obj =>
                {
                    if (obj == null)
                        return false;
                    string path = AssetDatabase.GetAssetPath(obj);
                    // Safely compare GUIDs
                    return !string.IsNullOrEmpty(path)
                        && AssetDatabase.AssetPathToGUID(path) == savedObjectGuid;
                });

                if (objectToSelect != null)
                {
                    // Debug.Log($"DetermineObjectToAutoSelect: Found last selected object by GUID: {objectToSelect.name}");
                }
                else
                {
                    // Debug.Log($"DetermineObjectToAutoSelect: Saved object GUID '{savedObjectGuid}' not found or invalid for type '{_selectedType.Name}'.");
                    // Optionally clear the now-stale preference?
                    // ClearLastSelectedObjectGuid();
                }
            }

            // 4. Fallback: If no GUID was saved, or the saved object wasn't found
            if (objectToSelect == null)
            {
                // Select the first object in the list (_selectedObjects should be correctly sorted by LoadObjectTypes)
                objectToSelect = _selectedObjects[0];
                // Debug.Log($"DetermineObjectToAutoSelect: Falling back to first object: {objectToSelect?.name}");
            }

            // Return the object to be selected (could be last selected, first, or potentially null if list was empty initially)
            return objectToSelect;
        }

        private void ApplyNamespaceCollapsedState(
            Label indicator,
            VisualElement typesContainer,
            bool collapsed,
            bool saveState
        )
        {
            if (_settings == null || indicator == null || typesContainer == null)
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
                SetIsNamespaceCollapsed(namespaceKey, collapsed);
            }
        }

        private void SaveNamespaceAndTypeSelectionState(string namespaceKey, string typeName)
        {
            if (_settings == null)
            {
                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(namespaceKey))
                {
                    return;
                }

                SetLastSelectedNamespaceKey(namespaceKey);
                if (string.IsNullOrWhiteSpace(typeName))
                {
                    return;
                }

                if (!string.IsNullOrEmpty(typeName))
                {
                    SetLastSelectedTypeName(typeName); // Save *new* type
                    // DO NOT clear object Guid here. Let SelectObject(null) handle clearing for the type that WAS selected.
                }
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
                if (string.IsNullOrWhiteSpace(assetPath))
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

            if (_settings == null)
            {
                _settings = LoadOrCreateSettings();
            }

            List<string> customNamespaceOrder = GetNamespaceOrder();
            _scriptableObjectTypes.Sort(
                (lhs, rhs) => CompareUsingCustomOrder(lhs.key, rhs.key, customNamespaceOrder)
            );
            foreach ((string key, List<Type> types) in _scriptableObjectTypes)
            {
                List<string> customTypeNameOrder = GetTypeOrderForNamespace(key);
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
                        SetLastSelectedNamespaceKey(namespaceKey);
                        SetLastSelectedTypeName(typeName);
                        SetLastSelectedObjectGuidForType(typeName, objectGuid);
                    }
                }
                catch (Exception e)
                {
                    this.LogError($"Error saving selection state.", e);
                }
            }
            else
            {
                if (_selectedType != null)
                {
                    string typeName = _selectedType.Name;
                    // Debug.Log($"Clearing last object pref for type {typeName} due to null/invalid selection.");
                    // Call SET with null guid to clear/remove the entry for this specific type
                    SetLastSelectedObjectGuidForType(typeName, null);
                }
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
                if (DragDistanceThreshold < Vector2.Distance(evt.position, _dragStartPosition))
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

            int oldDataIndex = _scriptableObjectTypes.FindIndex(kvp =>
                string.Equals(kvp.key, draggedKey, StringComparison.Ordinal)
            );
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
            if (_settings == null)
            {
                return;
            }

            List<string> newNamespaceOrder = _scriptableObjectTypes.Select(kvp => kvp.key).ToList();
            SetNamespaceOrder(newNamespaceOrder);
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
                || string.IsNullOrWhiteSpace(namespaceKey)
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

            int namespaceIndex = _scriptableObjectTypes.FindIndex(kvp =>
                string.Equals(kvp.key, namespaceKey, StringComparison.Ordinal)
            );
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
            if (_settings == null)
            {
                return;
            }

            List<string> newTypeNameOrder = orderedTypes.Select(t => t.Name).ToList();
            SetTypeOrderForNamespace(namespaceKey, newTypeNameOrder);
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

            if (0 < dirtyObjects.Count)
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

        private void MarkSettingsDirty()
        {
            if (_settings != null)
            {
                EditorUtility.SetDirty(_settings);
            }
        }

        private void LoadUserStateFromFile()
        {
            if (File.Exists(_userStateFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_userStateFilePath);
                    _userState = JsonUtility.FromJson<DataVisualizerUserState>(json); // Or your preferred JSON lib
                    if (_userState == null)
                    { // Handle case where file is empty or invalid JSON
                        Debug.LogWarning(
                            $"User state file '{_userStateFilePath}' was empty or invalid. Creating new state."
                        );
                        _userState = new DataVisualizerUserState();
                    }
                    else
                    {
                        Debug.Log("Loaded user state from file.");
                    }
                    // Optional: Version check/migration if _userState.Version is old
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"Error loading user state from '{_userStateFilePath}': {e}. Using default state."
                    );
                    _userState = new DataVisualizerUserState();
                }
            }
            else
            {
                Debug.Log(
                    $"User state file not found at '{_userStateFilePath}'. Creating new state."
                );
                _userState = new DataVisualizerUserState();
                // No need to save immediately, save happens on first change.
            }
            _userStateDirty = false; // Reset dirty flag after loading
        }

        private void SaveUserStateToFile()
        {
            if (_userState == null)
                return; // Should not happen if loaded correctly

            try
            {
                string json = JsonUtility.ToJson(_userState, true); // Use pretty print
                File.WriteAllText(_userStateFilePath, json);
                _userStateDirty = false; // Reset dirty flag after saving
                // Debug.Log($"User state saved to '{_userStateFilePath}'");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving user state to '{_userStateFilePath}': {e}");
            }
        }

        // Helper to mark user state dirty and trigger save (if in file mode)
        private void MarkUserStateDirty()
        {
            if (!_settings.PersistStateInSettingsAsset)
            {
                _userStateDirty = true;
                // Save immediately? Or rely on OnDisable/periodically?
                // Let's save immediately for simplicity now.
                SaveUserStateToFile();
            }
        }

        // Update Migration Method
        private void MigratePersistenceState(bool migrateToSettingsAsset)
        {
            Debug.Log(
                $"Migrating persistence state. Target is now: {(migrateToSettingsAsset ? "Settings Asset" : "User File")}"
            );
            if (_settings == null)
                return;
            if (!migrateToSettingsAsset && _userState == null)
            {
                LoadUserStateFromFile(); // Ensure user state loaded if migrating TO file
                if (_userState == null)
                {
                    Debug.LogError(
                        "Cannot migrate state to file: User state failed to load/create."
                    );
                    return;
                }
            }

            try
            {
                if (migrateToSettingsAsset) // Migrating FROM User File TO Settings Object
                {
                    Debug.Log("Migrating FROM User File TO Settings Object...");
                    if (_userState == null)
                    {
                        Debug.LogWarning("User state is null during migration to settings asset.");
                        return;
                    }

                    // Simple Key/Value Pairs
                    _settings.InternalLastSelectedNamespaceKey =
                        _userState.LastSelectedNamespaceKey;
                    _settings.InternalLastSelectedTypeName = _userState.LastSelectedTypeName;
                    // --- Migrate Last Object Selections List ---
                    _settings.InternalLastObjectSelections =
                        _userState
                            .LastObjectSelections?.Select(us => new LastObjectSelectionEntry
                            {
                                TypeName = us.TypeName,
                                ObjectGuid = us.ObjectGuid,
                            })
                            .ToList() ?? new List<LastObjectSelectionEntry>();
                    // ---

                    // Lists (create copies)
                    _settings.InternalNamespaceOrder = new List<string>(
                        _userState.NamespaceOrder ?? new List<string>()
                    );
                    _settings.InternalTypeOrders =
                        _userState
                            .TypeOrders?.Select(uo => new DataVisualizerSettings.NamespaceTypeOrder
                            {
                                NamespaceKey = uo.NamespaceKey,
                                TypeNames = new List<string>(uo.TypeNames ?? new List<string>()),
                            })
                            .ToList() ?? new List<DataVisualizerSettings.NamespaceTypeOrder>();
                    _settings.InternalNamespaceCollapseStates =
                        _userState
                            .NamespaceCollapseStates?.Select(
                                ucs => new DataVisualizerSettings.NamespaceCollapseState
                                {
                                    NamespaceKey = ucs.NamespaceKey,
                                    IsCollapsed = ucs.IsCollapsed,
                                }
                            )
                            .ToList() ?? new List<DataVisualizerSettings.NamespaceCollapseState>();

                    MarkSettingsDirty(); // Mark settings dirty as they received data
                    Debug.Log("Migration TO Settings Object complete.");
                }
                else // Migrating FROM Settings Object TO User File
                {
                    Debug.Log("Migrating FROM Settings Object TO User File...");
                    if (_userState == null)
                        _userState = new DataVisualizerUserState(); // Ensure instance exists

                    // Simple Key/Value Pairs
                    _userState.LastSelectedNamespaceKey =
                        _settings.InternalLastSelectedNamespaceKey;
                    _userState.LastSelectedTypeName = _settings.InternalLastSelectedTypeName;
                    // --- Migrate Last Object Selections List ---
                    _userState.LastObjectSelections =
                        _settings
                            .InternalLastObjectSelections?.Select(so => new LastObjectSelectionEntry
                            {
                                TypeName = so.TypeName,
                                ObjectGuid = so.ObjectGuid,
                            })
                            .ToList() ?? new List<LastObjectSelectionEntry>();
                    // ---

                    // Lists (create copies)
                    _userState.NamespaceOrder = new List<string>(
                        _settings.InternalNamespaceOrder ?? new List<string>()
                    );
                    _userState.TypeOrders =
                        _settings
                            .InternalTypeOrders?.Select(so => new UserStateTypeOrder
                            {
                                NamespaceKey = so.NamespaceKey,
                                TypeNames = new List<string>(so.TypeNames ?? new List<string>()),
                            })
                            .ToList() ?? new List<UserStateTypeOrder>();
                    _userState.NamespaceCollapseStates =
                        _settings
                            .InternalNamespaceCollapseStates?.Select(
                                scs => new UserStateNamespaceCollapseState
                                {
                                    NamespaceKey = scs.NamespaceKey,
                                    IsCollapsed = scs.IsCollapsed,
                                }
                            )
                            .ToList() ?? new List<UserStateNamespaceCollapseState>();

                    MarkUserStateDirty(); // Mark user state dirty so it gets saved
                    Debug.Log("Migration TO User File complete.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during persistence state migration: {e}");
            }
        }

        // --- Specific Persistence Accessors ---

        private string GetLastSelectedNamespaceKey()
        {
            if (_settings == null)
                return null;
            return _settings.PersistStateInSettingsAsset
                ? _settings.InternalLastSelectedNamespaceKey
                : _userState?.LastSelectedNamespaceKey;
        }

        private void SetLastSelectedNamespaceKey(string value)
        {
            if (_settings == null)
                return;
            if (_settings.PersistStateInSettingsAsset)
            {
                if (_settings.InternalLastSelectedNamespaceKey != value)
                {
                    _settings.InternalLastSelectedNamespaceKey = value;
                    MarkSettingsDirty();
                }
            }
            else if (_userState != null)
            {
                if (_userState.LastSelectedNamespaceKey != value)
                {
                    _userState.LastSelectedNamespaceKey = value;
                    MarkUserStateDirty();
                }
            }
        }

        private string GetLastSelectedTypeName()
        { // Type depends on the *context* of the last namespace
            if (_settings == null)
                return null;
            // This state is simple enough to store directly without context mapping for now
            return _settings.PersistStateInSettingsAsset
                ? _settings.InternalLastSelectedTypeName
                : _userState?.LastSelectedTypeName;
        }

        private void SetLastSelectedTypeName(string value)
        {
            Debug.Log($"Setting Last Selected Type Name to '{value}'");
            if (_settings == null)
                return;
            if (_settings.PersistStateInSettingsAsset)
            {
                if (_settings.InternalLastSelectedTypeName != value)
                {
                    _settings.InternalLastSelectedTypeName = value;
                    MarkSettingsDirty();
                }
            }
            else if (_userState != null)
            {
                if (_userState.LastSelectedTypeName != value)
                {
                    _userState.LastSelectedTypeName = value;
                    MarkUserStateDirty();
                }
            }
        }

        private string GetLastSelectedObjectGuidForType(string typeName)
        {
            if (_settings == null || string.IsNullOrEmpty(typeName))
                return null;

            if (_settings.PersistStateInSettingsAsset)
            {
                // Use helper on settings object or find manually
                return _settings.GetLastObjectForType(typeName);
                // Manual find: return _settings.InternalLastObjectSelections?.Find(e => e.TypeName == typeName)?.ObjectGuid;
            }
            else
            {
                if (_userState == null)
                    LoadUserStateFromFile(); // Load if needed
                // Use helper on user state object or find manually
                return _userState?.GetLastObjectForType(typeName);
                // Manual find: return _userState?.LastObjectSelections?.Find(e => e.TypeName == typeName)?.ObjectGuid;
            }
        }

        private void SetLastSelectedObjectGuidForType(string typeName, string objectGuid)
        {
            Debug.Log($"Setting Last Selected Object Guid for Type '{typeName}' to '{objectGuid}'");
            if (_settings == null || string.IsNullOrEmpty(typeName))
                return;

            if (_settings.PersistStateInSettingsAsset)
            {
                // Use helper on settings object
                _settings.SetLastObjectForType(typeName, objectGuid);
                MarkSettingsDirty(); // Mark dirty as list might have changed
            }
            else if (_userState != null)
            {
                // Use helper on user state object
                _userState.SetLastObjectForType(typeName, objectGuid);
                MarkUserStateDirty(); // Mark user state dirty as list might have changed
            }
        }

        private List<string> GetNamespaceOrder()
        {
            if (_settings == null)
                return new List<string>();
            // Ensure list exists in chosen backend
            if (_settings.PersistStateInSettingsAsset)
            {
                if (_settings.InternalNamespaceOrder == null)
                    _settings.InternalNamespaceOrder = new List<string>();
                return _settings.InternalNamespaceOrder;
            }
            else
            {
                if (_userState == null)
                    LoadUserStateFromFile(); // Load if missing
                if (_userState.NamespaceOrder == null)
                    _userState.NamespaceOrder = new List<string>();
                return _userState.NamespaceOrder;
            }
        }

        private void SetNamespaceOrder(List<string> value)
        {
            if (_settings == null || value == null)
                return;
            if (_settings.PersistStateInSettingsAsset)
            {
                // Check if different before assigning and marking dirty
                if (
                    _settings.InternalNamespaceOrder == null
                    || !_settings.InternalNamespaceOrder.SequenceEqual(value)
                )
                {
                    _settings.InternalNamespaceOrder = new List<string>(value); // Assign copy
                    MarkSettingsDirty();
                }
            }
            else if (_userState != null)
            {
                if (
                    _userState.NamespaceOrder == null
                    || !_userState.NamespaceOrder.SequenceEqual(value)
                )
                {
                    _userState.NamespaceOrder = new List<string>(value); // Assign copy
                    MarkUserStateDirty();
                }
            }
        }

        private List<string> GetTypeOrderForNamespace(string namespaceKey)
        {
            if (_settings == null || string.IsNullOrEmpty(namespaceKey))
                return new List<string>();
            if (_settings.PersistStateInSettingsAsset)
            {
                var entry = _settings.InternalTypeOrders?.Find(o => o.NamespaceKey == namespaceKey);
                return entry?.TypeNames ?? new List<string>(); // Return empty if not found
            }
            else
            {
                if (_userState == null)
                    LoadUserStateFromFile();
                var entry = _userState.TypeOrders?.Find(o => o.NamespaceKey == namespaceKey);
                return entry?.TypeNames ?? new List<string>();
            }
        }

        private void SetTypeOrderForNamespace(string namespaceKey, List<string> typeNames)
        {
            if (_settings == null || string.IsNullOrEmpty(namespaceKey) || typeNames == null)
                return;
            if (_settings.PersistStateInSettingsAsset)
            {
                // Use helper to get/create entry, check if different
                var entryList = _settings.GetOrCreateTypeOrderList(namespaceKey);
                if (!entryList.SequenceEqual(typeNames))
                {
                    entryList.Clear();
                    entryList.AddRange(typeNames);
                    MarkSettingsDirty();
                }
            }
            else if (_userState != null)
            {
                var entryList = _userState.GetOrCreateTypeOrderList(namespaceKey);
                if (!entryList.SequenceEqual(typeNames))
                {
                    entryList.Clear();
                    entryList.AddRange(typeNames);
                    MarkUserStateDirty();
                }
            }
        }

        private bool GetIsNamespaceCollapsed(string namespaceKey)
        {
            if (_settings == null || string.IsNullOrEmpty(namespaceKey))
                return false; // Default to expanded
            if (_settings.PersistStateInSettingsAsset)
            {
                var entry = _settings.InternalNamespaceCollapseStates?.Find(o =>
                    o.NamespaceKey == namespaceKey
                );
                return entry?.IsCollapsed ?? false; // Default to false (expanded)
            }
            else
            {
                if (_userState == null)
                    LoadUserStateFromFile();
                var entry = _userState.NamespaceCollapseStates?.Find(o =>
                    o.NamespaceKey == namespaceKey
                );
                return entry?.IsCollapsed ?? false;
            }
        }

        private void SetIsNamespaceCollapsed(string namespaceKey, bool isCollapsed)
        {
            if (_settings == null || string.IsNullOrEmpty(namespaceKey))
                return;
            if (_settings.PersistStateInSettingsAsset)
            {
                var entry = _settings.GetOrCreateCollapseState(namespaceKey);
                if (entry.IsCollapsed != isCollapsed)
                {
                    entry.IsCollapsed = isCollapsed;
                    MarkSettingsDirty();
                }
            }
            else if (_userState != null)
            {
                var entry = _userState.GetOrCreateCollapseState(namespaceKey);
                if (entry.IsCollapsed != isCollapsed)
                {
                    entry.IsCollapsed = isCollapsed;
                    MarkUserStateDirty();
                }
            }
        }

        // Remove generic helpers Set/Get/DeletePersistentString/Bool

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

            return 0 <= indexB ? 1 : string.Compare(keyA, keyB, StringComparison.OrdinalIgnoreCase);
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

namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using Components;
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

            string savedNamespaceKey = GetPersistentString(LastSelectedNamespaceKey, string.Empty);
            string selectedNamespaceKey;
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
                selectedNamespaceKey = savedNamespaceKey;
                typesInNamespace = _scriptableObjectTypes[namespaceIndex].types;
            }
            else if (0 < _scriptableObjectTypes.Count)
            {
                (string key, List<Type> types) types = _scriptableObjectTypes[0];
                selectedNamespaceKey = types.key;
                typesInNamespace = types.types;
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
            string savedTypeName = GetPersistentString(typePrefsKey, string.Empty);
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

            string objPrefsKey = string.Format(LastSelectedObjectFormat, _selectedType.Name);
            string savedObjectGuid = GetPersistentString(objPrefsKey, string.Empty);
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
                bool wasUsingEditorPrefs = _settings.UseEditorPrefsForState;
                DataVisualizerSettingsPopup popup = CreateInstance<DataVisualizerSettingsPopup>();
                popup.titleContent = new GUIContent("Data Visualizer Settings");
                popup._settings = _settings;
                popup._onCloseCallback = () => HandleSettingsPopupClosed(wasUsingEditorPrefs);
                popup.minSize = new Vector2(370, 130);
                popup.maxSize = new Vector2(370, 130);
                popup.ShowModal();
            })
            {
                text = "…",
                name = "settings-button",
                tooltip = "Open Settings",
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
            BuildSettingsPopup();
            BuildNamespaceView();
            BuildObjectsView();
            BuildInspectorView();
        }

        private void HandleSettingsPopupClosed(bool previousModeWasEditorPrefs)
        {
            if (_settings != null && EditorUtility.IsDirty(_settings))
            {
                AssetDatabase.SaveAssets();
            }

            bool migrationNeeded =
                _settings != null && previousModeWasEditorPrefs != _settings.UseEditorPrefsForState;

            if (migrationNeeded)
            {
                MigratePersistenceState(!_settings.UseEditorPrefsForState);
                if (!_settings.UseEditorPrefsForState)
                {
                    AssetDatabase.SaveAssets();
                }
            }
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

            Toggle prefsToggle = new("Use EditorPrefs for State:")
            {
                value = _settings.UseEditorPrefsForState,
                tooltip =
                    "If checked, window state (selection, order, collapse) is saved globally in EditorPrefs.\nIf unchecked, state is saved within the DataVisualizerSettings asset file.",
            };
            prefsToggle.RegisterValueChangedCallback(evt =>
            {
                if (_settings != null)
                {
                    bool migratingToSettingsObject = !evt.newValue;
                    bool previousModeWasEditorPrefs = _settings.UseEditorPrefsForState;
                    _settings.UseEditorPrefsForState = evt.newValue;
                    if (previousModeWasEditorPrefs != evt.newValue)
                    {
                        MigratePersistenceState(migratingToSettingsObject);
                    }

                    MarkSettingsDirty();
                    AssetDatabase.SaveAssets();
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

                string collapsePrefsKey = string.Format(NamespaceCollapsedStateFormat, key);
                bool isCollapsed = GetPersistentBool(collapsePrefsKey, false);
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
                    tooltip = "Rename Asset",
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
                    tooltip = "Delete Object",
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
            {
                return;
            }

            if (
                !EditorUtility.DisplayDialog(
                    "Confirm Delete",
                    $"Are you sure you want to delete the asset '{objectToDelete.name}'?\nThis action cannot be undone.",
                    "Delete",
                    "Cancel"
                )
            )
            {
                return;
            }

            string path = AssetDatabase.GetAssetPath(objectToDelete);
            if (string.IsNullOrWhiteSpace(path))
            {
                this.LogError(
                    $"Could not find asset path for '{objectToDelete.name}'. Cannot delete."
                );
                return;
            }

            _selectedObjects.Remove(objectToDelete);
            _objectVisualElementMap.Remove(objectToDelete, out VisualElement visualElement);
            bool deleted = AssetDatabase.DeleteAsset(path);
            if (deleted)
            {
                AssetDatabase.Refresh();
                visualElement?.RemoveFromHierarchy();
                foreach (BaseDataObject dataObject in _selectedObjects)
                {
                    if (dataObject != null)
                    {
                        SelectObject(dataObject);
                        return;
                    }
                }

                SelectObject(null);
            }
            else
            {
                this.LogError($"Failed to delete asset at '{path}'.");
                LoadObjectTypes(_selectedType);
                BuildObjectsView();
                SelectObject(_selectedObject);
            }
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

            string directory = originalPath.Replace('\\', '/');
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

        private BaseDataObject DetermineObjectToAutoSelect()
        {
            if (_selectedObjects.Count == 0 || _selectedType == null)
            {
                return null;
            }

            BaseDataObject objectToSelect = null;
            string objPrefsKey = string.Format(LastSelectedObjectFormat, _selectedType.Name);
            string savedObjectGuid = GetPersistentString(objPrefsKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(savedObjectGuid))
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
            if (objectToSelect == null)
            {
                objectToSelect = _selectedObjects[0];
            }
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

                string collapsePrefsKey = string.Format(
                    NamespaceCollapsedStateFormat,
                    namespaceKey
                );
                SetPersistentBool(collapsePrefsKey, collapsed);
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

                SetPersistentString(LastSelectedNamespaceKey, namespaceKey);
                if (string.IsNullOrWhiteSpace(typeName))
                {
                    return;
                }

                SetPersistentString(LastSelectedTypeFormat + namespaceKey, typeName);
                string objPrefsKey = string.Format(LastSelectedObjectFormat, typeName);
                if (_settings.UseEditorPrefsForState)
                {
                    DeletePersistentKey(objPrefsKey);
                }
                else
                {
                    if (_settings.InternalLastSelectedObjectGuid == null)
                    {
                        return;
                    }

                    _settings.InternalLastSelectedObjectGuid = null;
                    MarkSettingsDirty();
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

            List<string> customNamespaceOrder;
            if (_settings.UseEditorPrefsForState)
            {
                customNamespaceOrder = LoadCustomOrder(CustomNamespaceOrderKey);
            }
            else
            {
                customNamespaceOrder = _settings.InternalNamespaceOrder ?? new List<string>();
            }
            _scriptableObjectTypes.Sort(
                (lhs, rhs) => CompareUsingCustomOrder(lhs.key, rhs.key, customNamespaceOrder)
            );
            foreach ((string key, List<Type> types) in _scriptableObjectTypes)
            {
                List<string> customTypeNameOrder;
                if (_settings.UseEditorPrefsForState)
                {
                    string prefsKey = string.Format(CustomTypeOrderKeyFormat, key);
                    customTypeNameOrder = LoadCustomOrder(prefsKey);
                }
                else
                {
                    DataVisualizerSettings.NamespaceTypeOrder orderEntry =
                        _settings.InternalTypeOrders.Find(o =>
                            string.Equals(o.NamespaceKey, key, StringComparison.Ordinal)
                        );
                    customTypeNameOrder = orderEntry?.TypeNames ?? new List<string>();
                }
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
                            SetPersistentString(LastSelectedNamespaceKey, namespaceKey);
                        }

                        if (!string.IsNullOrWhiteSpace(typeName))
                        {
                            string typePrefsKey = string.Format(
                                LastSelectedTypeFormat,
                                namespaceKey
                            );
                            SetPersistentString(typePrefsKey, typeName);
                        }

                        if (!string.IsNullOrWhiteSpace(objectGuid))
                        {
                            string objPrefsKey = string.Format(LastSelectedObjectFormat, typeName);
                            SetPersistentString(objPrefsKey, objectGuid);
                        }
                        else
                        {
                            string objPrefsKey = string.Format(LastSelectedObjectFormat, typeName);
                            DeletePersistentKey(objPrefsKey);
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
            if (_settings.UseEditorPrefsForState)
            {
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
            else
            {
                if (!_settings.InternalNamespaceOrder.SequenceEqual(newNamespaceOrder))
                {
                    _settings.InternalNamespaceOrder = newNamespaceOrder;
                    MarkSettingsDirty();
                }
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
            if (_settings.UseEditorPrefsForState)
            {
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
            else
            {
                List<string> orderEntry = _settings.GetOrCreateTypeOrderList(namespaceKey);
                if (!orderEntry.SequenceEqual(newTypeNameOrder))
                {
                    orderEntry.Clear();
                    orderEntry.AddRange(newTypeNameOrder);
                    MarkSettingsDirty();
                }
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

        private void DeletePersistentKey(string key)
        {
            if (_settings == null)
            {
                return;
            }

            if (_settings.UseEditorPrefsForState)
            {
                EditorPrefs.DeleteKey(key);
            }
            else
            {
                bool changed = false;

                if (string.Equals(key, LastSelectedNamespaceKey, StringComparison.Ordinal))
                {
                    if (_settings.InternalLastSelectedNamespaceKey != null)
                    {
                        _settings.InternalLastSelectedNamespaceKey = null;
                        changed = true;
                    }
                }
                else if (
                    key.StartsWith(
                        LastSelectedTypeFormat.Substring(0, LastSelectedTypeFormat.Length - 3),
                        StringComparison.Ordinal
                    )
                )
                {
                    if (_settings.InternalLastSelectedTypeName != null)
                    {
                        _settings.InternalLastSelectedTypeName = null;
                        changed = true;
                    }
                }
                else if (
                    key.StartsWith(
                        LastSelectedObjectFormat.Substring(0, LastSelectedObjectFormat.Length - 3),
                        StringComparison.Ordinal
                    )
                )
                {
                    if (_settings.InternalLastSelectedObjectGuid != null)
                    {
                        _settings.InternalLastSelectedObjectGuid = null;
                        changed = true;
                    }
                }
                else if (
                    key.StartsWith(
                        NamespaceCollapsedStateFormat.Substring(
                            0,
                            NamespaceCollapsedStateFormat.Length - 3
                        ),
                        StringComparison.Ordinal
                    )
                )
                {
                    string namespaceKey = key.Substring(NamespaceCollapsedStateFormat.Length - 2);

                    int removedIndex = _settings.InternalNamespaceCollapseStates.FindIndex(o =>
                        string.Equals(o.NamespaceKey, namespaceKey, StringComparison.Ordinal)
                    );
                    if (removedIndex != -1)
                    {
                        _settings.InternalNamespaceCollapseStates.RemoveAt(removedIndex);
                        changed = true;
                    }
                }

                if (changed)
                {
                    MarkSettingsDirty();
                }
            }
        }

        private void SetPersistentString(string key, string value)
        {
            if (_settings.UseEditorPrefsForState)
            {
                EditorPrefs.SetString(key, value);
            }
            else if (_settings != null)
            {
                bool changed = false;
                if (string.Equals(key, LastSelectedNamespaceKey, StringComparison.Ordinal))
                {
                    if (_settings.InternalLastSelectedNamespaceKey != value)
                    {
                        _settings.InternalLastSelectedNamespaceKey = value;
                        changed = true;
                    }
                }
                else if (
                    key.StartsWith(
                        LastSelectedTypeFormat.Substring(0, LastSelectedTypeFormat.Length - 3),
                        StringComparison.Ordinal
                    )
                )
                {
                    if (_settings.InternalLastSelectedTypeName != value)
                    {
                        _settings.InternalLastSelectedTypeName = value;
                        changed = true;
                    }
                }
                else if (
                    key.StartsWith(
                        LastSelectedObjectFormat.Substring(0, LastSelectedObjectFormat.Length - 3),
                        StringComparison.Ordinal
                    )
                )
                {
                    if (_settings.InternalLastSelectedObjectGuid != value)
                    {
                        _settings.InternalLastSelectedObjectGuid = value;
                        changed = true;
                    }
                }

                if (changed)
                {
                    MarkSettingsDirty();
                }
            }
        }

        private string GetPersistentString(string key, string defaultValue)
        {
            if (_settings.UseEditorPrefsForState)
            {
                return EditorPrefs.GetString(key, defaultValue);
            }

            if (_settings == null)
            {
                return defaultValue;
            }

            if (string.Equals(key, LastSelectedNamespaceKey, StringComparison.Ordinal))
            {
                return string.IsNullOrWhiteSpace(_settings.InternalLastSelectedNamespaceKey)
                    ? defaultValue
                    : _settings.InternalLastSelectedNamespaceKey;
            }
            if (
                key.StartsWith(
                    LastSelectedTypeFormat.Substring(0, LastSelectedTypeFormat.Length - 3),
                    StringComparison.Ordinal
                )
            )
            {
                return string.IsNullOrWhiteSpace(_settings.InternalLastSelectedTypeName)
                    ? defaultValue
                    : _settings.InternalLastSelectedTypeName;
            }
            if (
                key.StartsWith(
                    LastSelectedObjectFormat.Substring(0, LastSelectedObjectFormat.Length - 3),
                    StringComparison.Ordinal
                )
            )
            {
                return string.IsNullOrWhiteSpace(_settings.InternalLastSelectedObjectGuid)
                    ? defaultValue
                    : _settings.InternalLastSelectedObjectGuid;
            }
            return defaultValue;
        }

        private void SetPersistentBool(string key, bool value)
        {
            if (_settings.UseEditorPrefsForState)
            {
                EditorPrefs.SetBool(key, value);
            }
            else if (_settings != null)
            {
                if (
                    key.StartsWith(
                        NamespaceCollapsedStateFormat.Substring(
                            0,
                            NamespaceCollapsedStateFormat.Length - 3
                        ),
                        StringComparison.Ordinal
                    )
                )
                {
                    string namespaceKey = key.Substring(NamespaceCollapsedStateFormat.Length - 2);
                    DataVisualizerSettings.NamespaceCollapseState stateEntry =
                        _settings.GetOrCreateCollapseState(namespaceKey);
                    if (stateEntry.IsCollapsed != value)
                    {
                        stateEntry.IsCollapsed = value;
                        MarkSettingsDirty();
                    }
                }
            }
        }

        private bool GetPersistentBool(string key, bool defaultValue)
        {
            if (_settings.UseEditorPrefsForState)
            {
                return EditorPrefs.GetBool(key, defaultValue);
            }
            if (_settings != null)
            {
                if (
                    key.StartsWith(
                        NamespaceCollapsedStateFormat.Substring(
                            0,
                            NamespaceCollapsedStateFormat.Length - 3
                        ),
                        StringComparison.Ordinal
                    )
                )
                {
                    string namespaceKey = key.Substring(NamespaceCollapsedStateFormat.Length - 2);
                    DataVisualizerSettings.NamespaceCollapseState stateEntry =
                        _settings.InternalNamespaceCollapseStates.Find(o =>
                            string.Equals(o.NamespaceKey, namespaceKey, StringComparison.Ordinal)
                        );
                    return stateEntry?.IsCollapsed ?? defaultValue;
                }
            }
            return defaultValue;
        }

        private void MarkSettingsDirty()
        {
            if (_settings != null)
            {
                EditorUtility.SetDirty(_settings);
            }
        }

        private void MigratePersistenceState(bool targetIsSettingsObject)
        {
            if (_settings == null)
            {
                this.LogError($"Cannot migrate state: Settings object is null.");
                return;
            }

            if (_scriptableObjectTypes == null || _scriptableObjectTypes.Count == 0)
            {
                LoadScriptableObjectTypes();
            }
            List<string> currentNamespaceKeys =
                _scriptableObjectTypes?.Select(kvp => kvp.key).ToList() ?? new List<string>();

            try
            {
                if (targetIsSettingsObject)
                {
                    _settings.InternalLastSelectedNamespaceKey = EditorPrefs.GetString(
                        LastSelectedNamespaceKey,
                        null
                    );

                    string nsKeyForTypePref = _settings.InternalLastSelectedNamespaceKey;
                    string typeNameForObjPref = null;
                    if (!string.IsNullOrWhiteSpace(nsKeyForTypePref))
                    {
                        string typePrefsKey = string.Format(
                            LastSelectedTypeFormat,
                            nsKeyForTypePref
                        );
                        typeNameForObjPref = EditorPrefs.GetString(typePrefsKey, null);
                        _settings.InternalLastSelectedTypeName = typeNameForObjPref;
                    }
                    else
                    {
                        _settings.InternalLastSelectedTypeName = null;
                    }
                    if (!string.IsNullOrWhiteSpace(typeNameForObjPref))
                    {
                        string objPrefsKey = string.Format(
                            LastSelectedObjectFormat,
                            typeNameForObjPref
                        );
                        _settings.InternalLastSelectedObjectGuid = EditorPrefs.GetString(
                            objPrefsKey,
                            null
                        );
                    }
                    else
                    {
                        _settings.InternalLastSelectedObjectGuid = null;
                    }

                    string nsOrderJson = EditorPrefs.GetString(CustomNamespaceOrderKey, "[]");
                    _settings.InternalNamespaceOrder =
                        Serializer.JsonDeserialize<List<string>>(nsOrderJson) ?? new List<string>();

                    _settings.InternalTypeOrders =
                        new List<DataVisualizerSettings.NamespaceTypeOrder>();
                    foreach (string namespaceKey in currentNamespaceKeys)
                    {
                        string typeOrderPrefsKey = string.Format(
                            CustomTypeOrderKeyFormat,
                            namespaceKey
                        );
                        string typeOrderJson = EditorPrefs.GetString(typeOrderPrefsKey, "[]");
                        List<string> typeNames = Serializer.JsonDeserialize<List<string>>(
                            typeOrderJson
                        );
                        if (typeNames is { Count: > 0 })
                        {
                            List<string> entry = _settings.GetOrCreateTypeOrderList(namespaceKey);
                            entry.Clear();
                            entry.AddRange(typeNames);
                        }
                    }

                    _settings.InternalNamespaceCollapseStates =
                        new List<DataVisualizerSettings.NamespaceCollapseState>();
                    foreach (string namespaceKey in currentNamespaceKeys)
                    {
                        string collapsePrefsKey = string.Format(
                            NamespaceCollapsedStateFormat,
                            namespaceKey
                        );
                        if (EditorPrefs.HasKey(collapsePrefsKey))
                        {
                            bool isCollapsed = EditorPrefs.GetBool(collapsePrefsKey, false);
                            DataVisualizerSettings.NamespaceCollapseState entry =
                                _settings.GetOrCreateCollapseState(namespaceKey);
                            entry.IsCollapsed = isCollapsed;
                        }
                    }
                }
                else
                {
                    EditorPrefs.SetString(
                        LastSelectedNamespaceKey,
                        _settings.InternalLastSelectedNamespaceKey
                    );

                    string nsKeyForTypePref = _settings.InternalLastSelectedNamespaceKey;
                    string typeNameForObjPref = _settings.InternalLastSelectedTypeName;

                    if (!string.IsNullOrWhiteSpace(nsKeyForTypePref))
                    {
                        string typePrefsKey = string.Format(
                            LastSelectedTypeFormat,
                            nsKeyForTypePref
                        );
                        EditorPrefs.SetString(typePrefsKey, typeNameForObjPref);
                    }

                    if (!string.IsNullOrWhiteSpace(typeNameForObjPref))
                    {
                        string objPrefsKey = string.Format(
                            LastSelectedObjectFormat,
                            typeNameForObjPref
                        );
                        EditorPrefs.SetString(
                            objPrefsKey,
                            _settings.InternalLastSelectedObjectGuid
                        );
                    }

                    string nsOrderJson = Serializer.JsonStringify(
                        _settings.InternalNamespaceOrder ?? new List<string>()
                    );
                    EditorPrefs.SetString(CustomNamespaceOrderKey, nsOrderJson);

                    foreach (
                        DataVisualizerSettings.NamespaceTypeOrder typeOrderEntry in _settings.InternalTypeOrders
                    )
                    {
                        string typeOrderPrefsKey = string.Format(
                            CustomTypeOrderKeyFormat,
                            typeOrderEntry.NamespaceKey
                        );
                        string typeOrderJson = Serializer.JsonStringify(
                            typeOrderEntry.TypeNames ?? new List<string>()
                        );
                        EditorPrefs.SetString(typeOrderPrefsKey, typeOrderJson);
                    }

                    foreach (
                        DataVisualizerSettings.NamespaceCollapseState collapseEntry in _settings.InternalNamespaceCollapseStates
                    )
                    {
                        string collapsePrefsKey = string.Format(
                            NamespaceCollapsedStateFormat,
                            collapseEntry.NamespaceKey
                        );
                        EditorPrefs.SetBool(collapsePrefsKey, collapseEntry.IsCollapsed);
                    }
                }
            }
            catch (Exception e)
            {
                this.LogError($"Error during persistence state migration.", e);
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

            return 0 <= indexB ? 1 : string.Compare(keyA, keyB, StringComparison.OrdinalIgnoreCase);
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

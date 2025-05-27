namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.Extension;
    using Core.Helper;
    using UI;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Object = UnityEngine.Object;

    public class AnimationViewerWindow : EditorWindow
    {
        private const string PackageId = "com.wallstop-studios.unity-helpers";

        [MenuItem("Tools/2D Animation Viewer (Immutable Layers)")]
        public static void ShowWindow()
        {
            AnimationViewerWindow wnd = GetWindow<AnimationViewerWindow>();
            wnd.titleContent = new GUIContent("2D Animation Viewer");
            wnd.minSize = new Vector2(750, 500);
        }

        private VisualTreeAsset _visualTree;
        private StyleSheet _styleSheet;

        // --- UI Elements ---
        private ObjectField _addAnimationClipField;
        private Button _browseAndAddButton;
        private FloatField _fpsField;
        private Button _applyFpsButton; // Renamed from ApplyPreviewFPS for clarity
        private Button _saveClipButton;
        private VisualElement _loadedClipsContainer;
        private VisualElement _previewPanelHost; // The UXML element that will host LayeredImage
        private LayeredImage _animationPreview; // Instance of user's LayeredImage
        private VisualElement _framesContainer;
        private Label _fpsDebugLabel;
        private Label _framesPanelTitle;

        // --- Data Management ---
        // Helper class to manage editable state for each conceptual layer
        private class EditorLayerData
        {
            public AnimationClip SourceClip { get; }
            public List<Sprite> Sprites { get; set; } // This list is mutable for editing
            public string ClipName => SourceClip != null ? SourceClip.name : "Unnamed Layer";
            public float OriginalClipFps { get; }
            public string BindingPath { get; private set; } // Path of the m_Sprite binding

            public EditorLayerData(AnimationClip clip)
            {
                SourceClip = clip;
                Sprites = clip.GetSpritesFromClip()?.ToList(); // Ensure list exists
                OriginalClipFps =
                    clip.frameRate > 0 ? clip.frameRate : AnimatedSpriteLayer.FrameRate;

                // Find binding path for saving
                BindingPath = string.Empty;
                if (SourceClip != null)
                {
                    foreach (
                        EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(
                            SourceClip
                        )
                    )
                    {
                        if (
                            binding.type == typeof(SpriteRenderer)
                            && binding.propertyName == "m_Sprite"
                        )
                        {
                            BindingPath = binding.path;
                            break;
                        }
                    }
                }
            }
        }

        private List<EditorLayerData> _loadedEditorLayers = new List<EditorLayerData>();
        private EditorLayerData _activeEditorLayer; // The layer whose frames are shown in the frame list
        private List<SpriteFrameUIData> _currentFramesForEditingUI = new List<SpriteFrameUIData>(); // UI representation
        private float _currentPreviewFps = AnimatedSpriteLayer.FrameRate;

        // --- Drag and Drop state for frames ---
        private VisualElement _draggedFrameElement; // The VisualElement of the frame item being dragged
        private int _draggedFrameOriginalDataIndex; // Index in _activeEditorLayer.Sprites
        private VisualElement _frameDropPlaceholder; // Placeholder for frame list (was _dropPlaceholder)

        private VisualElement _draggedLoadedClipElement;
        private int _draggedLoadedClipOriginalIndex; // Index in _loadedEditorLayers
        private VisualElement _loadedClipDropPlaceholder;

        // Represents a single frame in our editor UI for the *active* clip
        private class SpriteFrameUIData
        {
            public Sprite Sprite;
            public int DisplayIndex;
            public VisualElement VisualElement;

            public SpriteFrameUIData(Sprite sprite, int displayIndex)
            {
                Sprite = sprite;
                DisplayIndex = displayIndex;
            }
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            TryLoadStyleSheets();

            if (_visualTree == null)
            {
                root.Add(new Label("Error: AnimationViewer.uxml not found."));
                return;
            }
            if (_styleSheet != null)
            {
                root.styleSheets.Add(_styleSheet);
            }

            _visualTree.CloneTree(root);

            // Query UI elements (ensure names match your UXML)
            _addAnimationClipField = root.Q<ObjectField>("addAnimationClipField");
            _browseAndAddButton = root.Q<Button>("browseAndAddButton");
            _fpsField = root.Q<FloatField>("fpsField");
            _applyFpsButton = root.Q<Button>("applyFpsButton"); // Changed name in UXML too
            _saveClipButton = root.Q<Button>("saveClipButton");
            _loadedClipsContainer = root.Q<VisualElement>("loadedClipsContainer");
            _previewPanelHost = root.Q<VisualElement>("preview-panel"); // UXML element to host LayeredImage
            _framesContainer = root.Q<VisualElement>("framesContainer");
            _fpsDebugLabel = root.Q<Label>("fpsDebugLabel");
            _framesPanelTitle = root.Q<Label>("framesPanelTitle");

            // Style the host for LayeredImage (from USS or inline)
            // LayeredImage will set its own size, so host should accommodate.
            _previewPanelHost.AddToClassList("animation-preview-container"); // USS class for alignment/max-size

            _fpsField.value = _currentPreviewFps;
            _saveClipButton.SetEnabled(false);

            _addAnimationClipField.RegisterValueChangedCallback(OnAddAnimationClipFieldChanged);
            _browseAndAddButton.text = "Add Selected Clips from Project";
            _browseAndAddButton.clicked -= OnBrowseAndAddClicked;
            _browseAndAddButton.clicked += OnAddSelectedClipsFromProjectClicked;
            _applyFpsButton.clicked += OnApplyFpsToPreviewClicked;
            _saveClipButton.clicked += OnSaveClipClicked;

            _frameDropPlaceholder = new VisualElement(); // Renamed from _dropPlaceholder
            _frameDropPlaceholder.AddToClassList("drop-placeholder");
            _frameDropPlaceholder.style.height = 5; // Make it smaller for frame list items
            _frameDropPlaceholder.style.visibility = Visibility.Hidden;

            // --- Register Drag & Drop Callbacks for the _framesContainer ---
            _framesContainer.RegisterCallback<DragUpdatedEvent>(OnFramesContainerDragUpdated);
            _framesContainer.RegisterCallback<DragPerformEvent>(OnFramesContainerDragPerform);
            _framesContainer.RegisterCallback<DragLeaveEvent>(OnFramesContainerDragLeave);

            _loadedClipDropPlaceholder = new VisualElement();
            _loadedClipDropPlaceholder.AddToClassList("drop-placeholder"); // Reuse existing style or make a new one
            _loadedClipDropPlaceholder.style.height = 5; // Make it smaller for clip list items
            _loadedClipDropPlaceholder.style.visibility = Visibility.Hidden;

            // --- Register Drag & Drop Callbacks for the _loadedClipsContainer ---
            _loadedClipsContainer.RegisterCallback<DragUpdatedEvent>(
                OnLoadedClipsContainerDragUpdated
            );
            _loadedClipsContainer.RegisterCallback<DragPerformEvent>(
                OnLoadedClipsContainerDragPerform
            );
            _loadedClipsContainer.RegisterCallback<DragLeaveEvent>(OnLoadedClipsContainerDragLeave);

            UpdateFramesPanelTitle();
            RebuildLoadedClipsUI();
            RecreatePreviewImage(); // Initial empty preview
        }

        private void TryLoadStyleSheets()
        {
            string packageRoot = DirectoryHelper.FindPackageRootPath(
                DirectoryHelper.GetCallerScriptDirectory()
            );
            if (!string.IsNullOrWhiteSpace(packageRoot))
            {
                if (
                    packageRoot.StartsWith("Packages", StringComparison.OrdinalIgnoreCase)
                    && !packageRoot.Contains(PackageId, StringComparison.OrdinalIgnoreCase)
                )
                {
                    int helpersIndex = packageRoot.LastIndexOf(
                        "UnityHelpers",
                        StringComparison.Ordinal
                    );
                    if (0 <= helpersIndex)
                    {
                        packageRoot = packageRoot[..helpersIndex];
                        packageRoot += PackageId;
                    }
                }

                char pathSeparator = Path.DirectorySeparatorChar;
                string styleSheetPath =
                    $"{packageRoot}{pathSeparator}Editor{pathSeparator}Styles{pathSeparator}AnimationViewer.uss";
                string unityRelativeStyleSheetPath = DirectoryHelper.AbsoluteToUnityRelativePath(
                    styleSheetPath
                );
                unityRelativeStyleSheetPath = unityRelativeStyleSheetPath.SanitizePath();

                const string packageCache = "PackageCache/";
                int packageCacheIndex;
                if (!string.IsNullOrWhiteSpace(unityRelativeStyleSheetPath))
                {
                    _styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                        unityRelativeStyleSheetPath
                    );
                }

                if (_styleSheet == null && !string.IsNullOrWhiteSpace(unityRelativeStyleSheetPath))
                {
                    packageCacheIndex = unityRelativeStyleSheetPath.IndexOf(
                        packageCache,
                        StringComparison.OrdinalIgnoreCase
                    );
                    if (0 <= packageCacheIndex)
                    {
                        unityRelativeStyleSheetPath = unityRelativeStyleSheetPath[
                            (packageCacheIndex + packageCache.Length)..
                        ];
                        int forwardIndex = unityRelativeStyleSheetPath.IndexOf(
                            "/",
                            StringComparison.Ordinal
                        );
                        if (0 <= forwardIndex)
                        {
                            unityRelativeStyleSheetPath = unityRelativeStyleSheetPath.Substring(
                                forwardIndex
                            );
                            unityRelativeStyleSheetPath =
                                "Packages/" + PackageId + "/" + unityRelativeStyleSheetPath;
                        }
                        else
                        {
                            unityRelativeStyleSheetPath = "Packages/" + unityRelativeStyleSheetPath;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(unityRelativeStyleSheetPath))
                    {
                        _styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                            unityRelativeStyleSheetPath
                        );
                        if (_styleSheet == null)
                        {
                            Debug.LogError(
                                $"Failed to load Animation Viewer style sheet (package root: '{packageRoot}'), relative path '{unityRelativeStyleSheetPath}'."
                            );
                        }
                    }
                    else
                    {
                        Debug.LogError(
                            $"Failed to convert absolute path '{styleSheetPath}' to Unity relative path."
                        );
                    }
                }

                string visualTreePath =
                    $"{packageRoot}{pathSeparator}Editor{pathSeparator}Styles{pathSeparator}AnimationViewer.uxml";
                string unityRelativeVisualTreePath = DirectoryHelper.AbsoluteToUnityRelativePath(
                    visualTreePath
                );

                _visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    unityRelativeVisualTreePath
                );
                if (_visualTree == null)
                {
                    packageCacheIndex = unityRelativeVisualTreePath.IndexOf(
                        packageCache,
                        StringComparison.OrdinalIgnoreCase
                    );
                    if (0 <= packageCacheIndex)
                    {
                        unityRelativeVisualTreePath = unityRelativeVisualTreePath[
                            (packageCacheIndex + packageCache.Length)..
                        ];
                        int forwardIndex = unityRelativeVisualTreePath.IndexOf(
                            "/",
                            StringComparison.Ordinal
                        );
                        if (0 <= forwardIndex)
                        {
                            unityRelativeVisualTreePath = unityRelativeVisualTreePath.Substring(
                                forwardIndex
                            );
                            unityRelativeVisualTreePath =
                                "Packages/" + PackageId + "/" + unityRelativeVisualTreePath;
                        }
                        else
                        {
                            unityRelativeVisualTreePath = "Packages/" + unityRelativeVisualTreePath;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(unityRelativeVisualTreePath))
                    {
                        _visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                            unityRelativeVisualTreePath
                        );
                    }
                }
            }
            else
            {
                Debug.LogError(
                    $"Failed to find Animation Viewer style sheet (package root: '{packageRoot}')."
                );
            }
        }

        private void OnAddAnimationClipFieldChanged(ChangeEvent<Object> evt)
        {
            AnimationClip clip = evt.newValue as AnimationClip;
            if (clip != null)
            {
                AddEditorLayer(clip);
                _addAnimationClipField.SetValueWithoutNotify(null);
            }
        }

        private void OnAddSelectedClipsFromProjectClicked()
        {
            Object[] selectedObjects = Selection.GetFiltered(
                typeof(AnimationClip),
                SelectionMode.Assets
            );

            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Clips Selected",
                    "Please select one or more AnimationClip assets in the Project window first.",
                    "OK"
                );
                return;
            }

            int clipsAddedCount = 0;
            foreach (Object obj in selectedObjects)
            {
                if (obj is AnimationClip clip)
                {
                    // Use your existing AddEditorLayer but prevent duplicate logging if it handles it
                    bool alreadyExists = _loadedEditorLayers.Any(layer => layer.SourceClip == clip);
                    if (!alreadyExists)
                    {
                        AddEditorLayer(clip); // Your existing method to add a single clip data representation
                        clipsAddedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"Clip '{clip.name}' is already loaded. Skipping.");
                    }
                }
            }

            if (clipsAddedCount > 0)
            {
                Debug.Log($"Added {clipsAddedCount} new AnimationClip(s) to the viewer.");
            }
            else if (selectedObjects.Length > 0) // Some were selected, but all were duplicates
            {
                Debug.Log("All selected AnimationClips were already loaded.");
            }
            // No need to clear _addAnimationClipField here as this button doesn't use it.
        }

        private void OnBrowseAndAddClicked()
        {
            string path = EditorUtility.OpenFilePanelWithFilters(
                "Select Animation Clip to Add",
                ProjectAnimationSettings.Instance.lastAnimationPath, // Assumes ProjectAnimationSettings.cs exists
                new string[] { "Animation Clip", "anim" }
            );

            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                ProjectAnimationSettings.Instance.lastAnimationPath = Path.GetDirectoryName(path);
                ProjectAnimationSettings.Instance.Save();

                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                    AddEditorLayer(clip);
            }
        }

        private void AddEditorLayer(AnimationClip clip)
        {
            if (clip == null || _loadedEditorLayers.Any(layer => layer.SourceClip == clip))
            {
                Debug.LogWarning($"Clip '{clip?.name}' is null or already loaded.");
                return;
            }

            EditorLayerData newEditorLayer = new EditorLayerData(clip);
            _loadedEditorLayers.Add(newEditorLayer);

            if (_activeEditorLayer == null && newEditorLayer.Sprites.Count > 0)
            {
                SetActiveEditorLayer(newEditorLayer);
            }
            else if (_activeEditorLayer == null) // If first clip has no sprites, still make it "active" contextually
            {
                SetActiveEditorLayer(newEditorLayer);
            }

            RebuildLoadedClipsUI();
            RecreatePreviewImage();
        }

        private void RemoveEditorLayer(EditorLayerData layerToRemove)
        {
            if (layerToRemove == null)
                return;
            _loadedEditorLayers.Remove(layerToRemove);

            if (_activeEditorLayer == layerToRemove)
            {
                SetActiveEditorLayer(_loadedEditorLayers.FirstOrDefault());
            }

            RebuildLoadedClipsUI();
            RecreatePreviewImage();
        }

        private void SetActiveEditorLayer(EditorLayerData layer)
        {
            _activeEditorLayer = layer;
            _currentFramesForEditingUI.Clear();
            _framesContainer.Clear();
            _fpsDebugLabel.text = "Detected FPS Info (Active Clip):";

            if (_activeEditorLayer != null)
            {
                for (int i = 0; i < _activeEditorLayer.Sprites.Count; i++)
                {
                    _currentFramesForEditingUI.Add(
                        new SpriteFrameUIData(_activeEditorLayer.Sprites[i], i)
                    );
                }
                UpdateFpsDebugLabelForActiveLayer();
                _saveClipButton.SetEnabled(_activeEditorLayer.SourceClip != null);
            }
            else
            {
                _saveClipButton.SetEnabled(false);
            }

            RebuildFramesListUI();
            RebuildLoadedClipsUI();
            UpdateFramesPanelTitle();
            // Preview is handled by RecreatePreviewImage when layers change,
            // or by setting FPS on existing preview.
        }

        private void UpdateFramesPanelTitle()
        {
            _framesPanelTitle.text =
                _activeEditorLayer != null
                    ? $"Frames (Editing: {_activeEditorLayer.ClipName})"
                    : "Frames (No Active Clip Selected)";
        }

        private void RebuildLoadedClipsUI()
        {
            _loadedClipsContainer.Clear();
            if (_loadedClipDropPlaceholder.parent == _loadedClipsContainer)
                _loadedClipsContainer.Remove(_loadedClipDropPlaceholder);

            for (int i = 0; i < _loadedEditorLayers.Count; i++)
            {
                EditorLayerData editorLayer = _loadedEditorLayers[i];

                var itemElement = new VisualElement();
                itemElement.AddToClassList("loaded-clip-item");
                if (editorLayer == _activeEditorLayer)
                {
                    itemElement.AddToClassList("loaded-clip-item--active");
                }
                itemElement.userData = i; // Store its current index in _loadedEditorLayers

                var label = new Label(editorLayer.ClipName);
                itemElement.Add(label);

                var removeButton = new Button(() => RemoveEditorLayer(editorLayer)) { text = "X" };
                itemElement.Add(removeButton);

                // Click to make active (remains the same)
                itemElement.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0 && _draggedLoadedClipElement == null) // Don't activate if starting a drag
                    {
                        SetActiveEditorLayer(editorLayer);
                    }
                });

                // --- NEW: Register Drag Handlers for this loaded clip item ---
                int currentIndex = i; // Capture current index for the lambda
                itemElement.RegisterCallback<PointerDownEvent>(evt =>
                    OnLoadedClipItemPointerDown(evt, itemElement, currentIndex)
                );
                // PointerUp and PointerLeave on the item itself will be handled similarly to frame dragging for robust cleanup

                _loadedClipsContainer.Add(itemElement);
            }
        }

        private void OnLoadedClipItemPointerDown(
            PointerDownEvent evt,
            VisualElement clipElement,
            int originalListIndex
        )
        {
            if (evt.button != 0 || _draggedLoadedClipElement != null)
                return; // Only left click, no concurrent drags

            _draggedLoadedClipElement = clipElement;
            _draggedLoadedClipOriginalIndex = originalListIndex;

            // Register PointerUp on the item itself for cleanup if drag ends without a valid drop
            _draggedLoadedClipElement.RegisterCallback<PointerUpEvent>(
                OnDraggedLoadedClipItemPointerUp
            );
            //_draggedLoadedClipElement.RegisterCallback<PointerLeaveEvent>(OnDraggedLoadedClipItemPointerLeave); // Optional

            _draggedLoadedClipElement.CapturePointer(evt.pointerId);
            _draggedLoadedClipElement.AddToClassList("frame-item-dragged"); // Reuse or make a new style for dragged clip item

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData("DraggedLoadedClipIndex", _draggedLoadedClipOriginalIndex);
            // Use a distinct generic data key to avoid conflict with frame dragging if that could somehow overlap.

            Object dragContextObject =
                (_loadedEditorLayers[_draggedLoadedClipOriginalIndex]?.SourceClip)
                ?? (Object)ScriptableObject.CreateInstance<ScriptableObject>();
            DragAndDrop.objectReferences = new Object[] { dragContextObject };
            DragAndDrop.StartDrag(
                _loadedEditorLayers[_draggedLoadedClipOriginalIndex].ClipName ?? "Dragging Layer"
            );

            evt.StopPropagation();
        }

        private void OnDraggedLoadedClipItemPointerUp(PointerUpEvent evt)
        {
            if (_draggedLoadedClipElement == null || evt.currentTarget != _draggedLoadedClipElement)
                return;

            if (DragAndDrop.GetGenericData("DraggedLoadedClipIndex") != null) // A drag we initiated is active and not yet dropped
            {
                CleanupLoadedClipDragState(evt.pointerId);
            }
            else if (
                _draggedLoadedClipElement != null
                && _draggedLoadedClipElement.HasPointerCapture(evt.pointerId)
            )
            {
                // Minimal cleanup if drag was handled by a drop elsewhere but pointerup still fires here
                _draggedLoadedClipElement.ReleasePointer(evt.pointerId);
                _draggedLoadedClipElement.UnregisterCallback<PointerUpEvent>(
                    OnDraggedLoadedClipItemPointerUp
                );
                //_draggedLoadedClipElement.UnregisterCallback<PointerLeaveEvent>(OnDraggedLoadedClipItemPointerLeave);
                _draggedLoadedClipElement.RemoveFromClassList("frame-item-dragged");
                _draggedLoadedClipElement = null; // Ensure it's null if not already.
            }
            evt.StopPropagation();
        }

        private void OnLoadedClipsContainerDragUpdated(DragUpdatedEvent evt)
        {
            object draggedIndexData = DragAndDrop.GetGenericData("DraggedLoadedClipIndex");
            if (draggedIndexData != null && _draggedLoadedClipElement != null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                float mouseY = evt.localMousePosition.y;
                int newVisualIndex = -1;

                if (_loadedClipDropPlaceholder.parent == _loadedClipsContainer)
                    _loadedClipsContainer.Remove(_loadedClipDropPlaceholder);

                for (int i = 0; i < _loadedClipsContainer.childCount; i++)
                {
                    VisualElement child = _loadedClipsContainer[i];
                    if (child == _draggedLoadedClipElement)
                        continue;

                    float childMidY = child.layout.yMin + child.layout.height / 2f;
                    if (mouseY < childMidY)
                    {
                        newVisualIndex = i;
                        break;
                    }
                }
                if (
                    newVisualIndex == -1
                    && _loadedClipsContainer.childCount > 0
                    && _draggedLoadedClipElement
                        != _loadedClipsContainer.ElementAt(_loadedClipsContainer.childCount - 1)
                )
                {
                    newVisualIndex = _loadedClipsContainer.childCount;
                }

                if (newVisualIndex != -1)
                {
                    _loadedClipsContainer.Insert(newVisualIndex, _loadedClipDropPlaceholder);
                    _loadedClipDropPlaceholder.style.visibility = Visibility.Visible;
                }
                else if (_loadedClipsContainer.childCount == 0 && _draggedLoadedClipElement != null)
                {
                    _loadedClipsContainer.Add(_loadedClipDropPlaceholder);
                    _loadedClipDropPlaceholder.style.visibility = Visibility.Visible;
                }
                else
                {
                    if (_loadedClipDropPlaceholder.parent == _loadedClipsContainer)
                        _loadedClipsContainer.Remove(_loadedClipDropPlaceholder);
                    _loadedClipDropPlaceholder.style.visibility = Visibility.Hidden;
                }
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
            evt.StopPropagation();
        }

        private void OnLoadedClipsContainerDragPerform(DragPerformEvent evt)
        {
            object draggedIndexData = DragAndDrop.GetGenericData("DraggedLoadedClipIndex");
            if (draggedIndexData != null && _draggedLoadedClipElement != null)
            {
                int originalListIndex = (int)draggedIndexData;

                if (originalListIndex < 0 || originalListIndex >= _loadedEditorLayers.Count)
                {
                    Debug.LogError(
                        "DragPerform (LoadedClips): Stale or invalid dragged index. Aborting drop."
                    );
                    CleanupLoadedClipDragState(-1);
                    evt.StopPropagation();
                    return;
                }

                EditorLayerData movedLayer = _loadedEditorLayers[originalListIndex];
                _loadedEditorLayers.RemoveAt(originalListIndex);

                int placeholderVisualIndex = _loadedClipsContainer.IndexOf(
                    _loadedClipDropPlaceholder
                );
                int targetListIndex;

                if (placeholderVisualIndex != -1)
                {
                    int itemsBeforePlaceholder = 0;
                    for (int i = 0; i < placeholderVisualIndex; i++)
                    {
                        if (
                            _loadedClipsContainer[i] != _draggedLoadedClipElement
                            && _loadedClipsContainer[i] != _loadedClipDropPlaceholder
                        )
                        {
                            itemsBeforePlaceholder++;
                        }
                    }
                    targetListIndex = itemsBeforePlaceholder;
                }
                else
                {
                    targetListIndex = _loadedEditorLayers.Count; // Add to end
                }
                targetListIndex = Mathf.Clamp(targetListIndex, 0, _loadedEditorLayers.Count);
                _loadedEditorLayers.Insert(targetListIndex, movedLayer);

                DragAndDrop.AcceptDrag();
                CleanupLoadedClipDragState(-1); // Important: Cleanup after data manipulation

                RebuildLoadedClipsUI(); // Rebuild the UI list for loaded clips
                RecreatePreviewImage(); // Recreate the LayeredImage with new layer order
            }
            else
            {
                if (DragAndDrop.GetGenericData("DraggedLoadedClipIndex") != null)
                {
                    CleanupLoadedClipDragState(-1);
                }
            }
            evt.StopPropagation();
        }

        private void OnLoadedClipsContainerDragLeave(DragLeaveEvent evt)
        {
            if (evt.target == _loadedClipsContainer) // Mouse truly left the container
            {
                if (_loadedClipDropPlaceholder.parent == _loadedClipsContainer)
                    _loadedClipsContainer.Remove(_loadedClipDropPlaceholder);
                if (_loadedClipDropPlaceholder != null)
                    _loadedClipDropPlaceholder.style.visibility = Visibility.Hidden;
            }
        }

        private void CleanupLoadedClipDragState(int pointerIdToRelease)
        {
            if (_draggedLoadedClipElement != null)
            {
                if (
                    pointerIdToRelease != -1
                    && _draggedLoadedClipElement.HasPointerCapture(pointerIdToRelease)
                )
                    _draggedLoadedClipElement.ReleasePointer(pointerIdToRelease);
                else if (
                    pointerIdToRelease == -1
                    && _draggedLoadedClipElement.HasPointerCapture(-1)
                )
                    _draggedLoadedClipElement.ReleasePointer(-1);

                _draggedLoadedClipElement.UnregisterCallback<PointerUpEvent>(
                    OnDraggedLoadedClipItemPointerUp
                );
                //_draggedLoadedClipElement.UnregisterCallback<PointerLeaveEvent>(OnDraggedLoadedClipItemPointerLeave);
                _draggedLoadedClipElement.RemoveFromClassList("frame-item-dragged"); // Or your specific class
                _draggedLoadedClipElement = null;
            }
            _draggedLoadedClipOriginalIndex = -1;

            if (_loadedClipDropPlaceholder != null)
            {
                if (_loadedClipDropPlaceholder.parent == _loadedClipsContainer)
                    _loadedClipsContainer.Remove(_loadedClipDropPlaceholder);
                _loadedClipDropPlaceholder.style.visibility = Visibility.Hidden;
            }
            DragAndDrop.SetGenericData("DraggedLoadedClipIndex", null);
        }

        // --- Preview Management ---
        private void RecreatePreviewImage()
        {
            if (_animationPreview != null)
            {
                // LayeredImage uses EditorApplication.update, manually detach if needed
                // Your LayeredImage seems to handle its own Tick detachment in Fps setter or OnDisable
                if (_animationPreview.parent == _previewPanelHost)
                {
                    _previewPanelHost.Remove(_animationPreview);
                }
                // If LayeredImage implemented IDisposable, call Dispose here.
                _animationPreview = null;
            }

            if (_previewPanelHost == null)
                return; // Should not happen if CreateGUI ran

            var animatedSpriteLayers = new List<AnimatedSpriteLayer>();
            if (_loadedEditorLayers.Count > 0)
            {
                foreach (var editorLayer in _loadedEditorLayers)
                {
                    // Construct NEW AnimatedSpriteLayer using current (possibly reordered) sprites
                    // Assuming worldSpaceOffsets and alpha are default for this tool for now.
                    // If these need to be configurable per layer in the tool, EditorLayerData would store them.
                    animatedSpriteLayers.Add(
                        new AnimatedSpriteLayer(editorLayer.Sprites, null, 1f)
                    );
                }
            }

            // Create new LayeredImage instance
            // Using Color.clear for background as LayeredImage handles its own default or passed color.
            _animationPreview = new LayeredImage(
                animatedSpriteLayers,
                Color.clear,
                _currentPreviewFps
            );
            _animationPreview.name = "animationPreviewElement";
            // LayeredImage sets its own size. USS on animation-preview-container can set max-width/height.
            _previewPanelHost.Add(_animationPreview);
        }

        private void OnFramesContainerDragUpdated(DragUpdatedEvent evt)
        {
            object draggedIndexData = DragAndDrop.GetGenericData("DraggedFrameDataIndex");
            if (draggedIndexData != null && _draggedFrameElement != null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                float mouseY = evt.localMousePosition.y;
                int newVisualIndex = -1;

                if (_frameDropPlaceholder.parent == _framesContainer)
                    _framesContainer.Remove(_frameDropPlaceholder);

                for (int i = 0; i < _framesContainer.childCount; i++)
                {
                    VisualElement child = _framesContainer[i];
                    if (child == _draggedFrameElement)
                        continue;

                    float childMidY = child.layout.yMin + child.layout.height / 2f;
                    if (mouseY < childMidY)
                    {
                        newVisualIndex = i;
                        break;
                    }
                }
                if (
                    newVisualIndex == -1
                    && _framesContainer.childCount > 0
                    && _draggedFrameElement
                        != _framesContainer.ElementAt(_framesContainer.childCount - 1)
                )
                {
                    newVisualIndex = _framesContainer.childCount;
                }

                if (newVisualIndex != -1)
                {
                    _framesContainer.Insert(newVisualIndex, _frameDropPlaceholder);
                    _frameDropPlaceholder.style.visibility = Visibility.Visible;
                }
                else if (_framesContainer.childCount == 0 && _draggedFrameElement != null)
                {
                    _framesContainer.Add(_frameDropPlaceholder);
                    _frameDropPlaceholder.style.visibility = Visibility.Visible;
                }
                else
                {
                    if (_frameDropPlaceholder.parent == _framesContainer)
                        _framesContainer.Remove(_frameDropPlaceholder);
                    _frameDropPlaceholder.style.visibility = Visibility.Hidden;
                }
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
            evt.StopPropagation();
        }

        private void OnFramesContainerDragPerform(DragPerformEvent evt)
        {
            object draggedIndexData = DragAndDrop.GetGenericData("DraggedFrameDataIndex");
            if (
                draggedIndexData != null
                && _draggedFrameElement != null
                && _activeEditorLayer != null
            )
            {
                int originalDataIndex = (int)draggedIndexData; // Index in _activeEditorLayer.Sprites

                if (originalDataIndex < 0 || originalDataIndex >= _activeEditorLayer.Sprites.Count)
                {
                    Debug.LogError(
                        "DragPerform (Frames): Stale or invalid dragged index. Aborting drop."
                    );
                    CleanupFrameDragState(-1);
                    evt.StopPropagation();
                    return;
                }

                Sprite movedSprite = _activeEditorLayer.Sprites[originalDataIndex];
                _activeEditorLayer.Sprites.RemoveAt(originalDataIndex); // Remove from old data position

                int placeholderVisualIndex = _framesContainer.IndexOf(_frameDropPlaceholder);
                int targetDataIndex; // Target index in _activeEditorLayer.Sprites

                if (placeholderVisualIndex != -1)
                {
                    int itemsBeforePlaceholder = 0;
                    for (int i = 0; i < placeholderVisualIndex; i++)
                    {
                        if (
                            _framesContainer[i] != _draggedFrameElement
                            && _framesContainer[i] != _frameDropPlaceholder
                        )
                        {
                            itemsBeforePlaceholder++;
                        }
                    }
                    targetDataIndex = itemsBeforePlaceholder;
                }
                else
                {
                    targetDataIndex = _activeEditorLayer.Sprites.Count; // Add to end
                }
                targetDataIndex = Mathf.Clamp(targetDataIndex, 0, _activeEditorLayer.Sprites.Count);
                _activeEditorLayer.Sprites.Insert(targetDataIndex, movedSprite); // Insert into new data position

                DragAndDrop.AcceptDrag();
                CleanupFrameDragState(-1); // Cleanup after data manipulation

                RebuildFramesListUI(); // Rebuild the UI list for frames
                RecreatePreviewImage(); // Recreate the LayeredImage as sprite order in active layer changed
            }
            else
            {
                if (DragAndDrop.GetGenericData("DraggedFrameDataIndex") != null)
                {
                    CleanupFrameDragState(-1);
                }
            }
            evt.StopPropagation();
        }

        private void OnFramesContainerDragLeave(DragLeaveEvent evt)
        {
            if (evt.target == _framesContainer)
            {
                if (_frameDropPlaceholder.parent == _framesContainer)
                    _framesContainer.Remove(_frameDropPlaceholder);
                if (_frameDropPlaceholder != null)
                    _frameDropPlaceholder.style.visibility = Visibility.Hidden;
            }
        }

        private void CleanupFrameDragState(int pointerIdToRelease)
        {
            if (_draggedFrameElement != null)
            {
                if (
                    pointerIdToRelease != -1
                    && _draggedFrameElement.HasPointerCapture(pointerIdToRelease)
                )
                    _draggedFrameElement.ReleasePointer(pointerIdToRelease);

                _draggedFrameElement.UnregisterCallback<PointerUpEvent>(
                    OnDraggedFrameItemPointerUp
                );
                //_draggedFrameElement.UnregisterCallback<PointerLeaveEvent>(OnDraggedFrameItemPointerLeave);
                _draggedFrameElement.RemoveFromClassList("frame-item-dragged");
                _draggedFrameElement = null;
            }
            _draggedFrameOriginalDataIndex = -1;

            if (_frameDropPlaceholder != null)
            {
                if (_frameDropPlaceholder.parent == _framesContainer)
                    _framesContainer.Remove(_frameDropPlaceholder);
                _frameDropPlaceholder.style.visibility = Visibility.Hidden;
            }
            DragAndDrop.SetGenericData("DraggedFrameDataIndex", null); // Use the correct key
        }

        private void UpdateFpsDebugLabelForActiveLayer()
        {
            if (_activeEditorLayer == null)
            {
                _fpsDebugLabel.text = "Detected FPS Info: No active clip.";
                return;
            }
            // Your AnimatedSpriteLayer has a const FrameRate. If clips have inherent FPS,
            // use _activeEditorLayer.OriginalClipFps
            _fpsDebugLabel.text =
                $"Active Clip FPS (Original): {FormatFps(_activeEditorLayer.OriginalClipFps)}fps. Preview uses global FPS.";
        }

        private string FormatFps(float fps)
        {
            return fps.ToString("F1");
        }

        // Inside AnimationViewerWindow.cs

        // ... (other parts of the class) ...

        // Rebuilds the list of draggable frame items based on the _activeEditorLayer.Sprites
        private void RebuildFramesListUI()
        {
            // 1. Clear previous UI elements and UI data list
            _framesContainer.Clear();
            _currentFramesForEditingUI.Clear(); // This list stores SpriteFrameUIData if you need to reference them

            // 2. Ensure the drop placeholder is not lingering in the container from a previous operation
            if (_frameDropPlaceholder != null && _frameDropPlaceholder.parent == _framesContainer)
            {
                _framesContainer.Remove(_frameDropPlaceholder);
            }

            // 3. If there's no active layer or it has no sprites, there's nothing to build
            if (_activeEditorLayer == null || _activeEditorLayer.Sprites == null)
            {
                // Optionally, display a message in _framesContainer if it's empty
                // _framesContainer.Add(new Label("No frames in the active layer."));
                return;
            }

            // 4. Iterate through sprites in the active layer and create UI for each
            for (int i = 0; i < _activeEditorLayer.Sprites.Count; i++)
            {
                Sprite sprite = _activeEditorLayer.Sprites[i];

                // Create a data object for UI representation (optional if only index is needed for drag)
                var frameUIData = new SpriteFrameUIData(sprite, i); // 'i' is its current index in _activeEditorLayer.Sprites
                _currentFramesForEditingUI.Add(frameUIData);

                // 5. Create the main visual element for this frame item
                VisualElement frameElement = new VisualElement();
                frameElement.AddToClassList("frame-item"); // Apply USS styling

                // --- Populate frameElement with content ---
                // Image for the sprite
                Image frameImage = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit };
                frameImage.AddToClassList("frame-image"); // USS styling for the image
                frameElement.Add(frameImage);

                // Container for text information
                VisualElement frameInfo = new VisualElement();
                frameInfo.AddToClassList("frame-info"); // USS styling for info section
                frameInfo.Add(new Label($"Frame: {i + 1}")); // Display 1-based index to user
                frameInfo.Add(new Label($"Sprite: {(sprite != null ? sprite.name : "(None)")}"));
                frameElement.Add(frameInfo);

                // Read-only field for display order (optional, could be part of frameInfo)
                IntegerField indexField = new IntegerField(null) { value = i + 1 };
                indexField.AddToClassList("frame-index-field"); // USS styling for index field
                frameElement.Add(indexField);

                VisualElement orderFieldContainer = new VisualElement();
                orderFieldContainer.style.flexDirection = FlexDirection.Row;
                orderFieldContainer.style.alignItems = Align.Center;
                orderFieldContainer.style.marginLeft = StyleKeyword.Auto; // This pushes the container to the right

                Label orderLabel = new Label("Order:");
                orderLabel.style.marginRight = 3; // Space between "Order:" and the number field
                orderFieldContainer.Add(orderLabel);
                orderFieldContainer.Add(indexField);
                frameElement.Add(orderFieldContainer);

                // --- End content population ---

                // 6. Register the PointerDownEvent for initiating a drag operation
                //    'i' is the crucial 'originalDataIndex' for the sprite in _activeEditorLayer.Sprites
                int currentDataIndex = i; // Capture 'i' for the lambda expression
                frameElement.RegisterCallback<PointerDownEvent>(evt =>
                    OnFrameItemPointerDown(evt, frameElement, currentDataIndex)
                );

                indexField.userData = currentDataIndex;

                indexField.RegisterCallback<FocusInEvent>(evt =>
                {
                    CleanupFrameDragState(-1); // Ensure drag state is cleaned if window closes mid-drag
                    CleanupLoadedClipDragState(-1); // For loaded clips
                });

                // Listen for when the user presses Enter/Return or when the field loses focus
                indexField.RegisterCallback<FocusOutEvent>(evt =>
                    OnFrameIndexFieldChanged(indexField)
                );
                indexField.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        OnFrameIndexFieldChanged(indexField);
                        // Optionally, blur the field to signify completion
                        indexField.Blur();
                    }
                });

                // 7. Add the fully constructed frameElement to the scrollable container
                _framesContainer.Add(frameElement);

                // Store a reference to the VisualElement in our UI data object (optional, if needed elsewhere)
                frameUIData.VisualElement = frameElement;
            }
        }

        private void OnFrameIndexFieldChanged(IntegerField field)
        {
            if (_activeEditorLayer == null || _activeEditorLayer.Sprites == null)
                return;

            CleanupFrameDragState(-1); // Ensure drag state is cleaned if window closes mid-drag
            CleanupLoadedClipDragState(-1); // For loaded clips

            int originalDataIndex = (int)field.userData; // Get the original 0-based index of the sprite
            int newUiIndex = field.value; // Get the new 1-based index from the UI field

            // Convert UI index (1-based) to data index (0-based)
            int newRequestedDataIndex = newUiIndex - 1;

            // Clamp the new data index to valid bounds
            int newClampedDataIndex = Mathf.Clamp(
                newRequestedDataIndex,
                0,
                _activeEditorLayer.Sprites.Count - 1
            );

            // If the (clamped) new data index is different from the original data index, then reorder
            if (newClampedDataIndex != originalDataIndex)
            {
                // Get the sprite that needs to be moved
                if (originalDataIndex < 0 || originalDataIndex >= _activeEditorLayer.Sprites.Count)
                {
                    Debug.LogWarning(
                        $"Original index {originalDataIndex} out of bounds. Rebuilding UI to correct."
                    );
                    RebuildFramesListUI(); // Rebuild to fix any display inconsistencies
                    return;
                }
                Sprite spriteToMove = _activeEditorLayer.Sprites[originalDataIndex];

                // Remove from the old position
                _activeEditorLayer.Sprites.RemoveAt(originalDataIndex);

                // Insert at the new (clamped) position
                // The target index for insertion might need adjustment if originalDataIndex < newClampedDataIndex
                // because the list size has changed. However, newClampedDataIndex was based on Count-1 *before* removal.
                // So, if inserting back, it should be fine.
                _activeEditorLayer.Sprites.Insert(newClampedDataIndex, spriteToMove);

                // Rebuild the entire frames list UI to reflect new order and indices
                RebuildFramesListUI();
                // Recreate the preview image
                RecreatePreviewImage();
            }
            else if (newUiIndex - 1 != newClampedDataIndex) // If user entered an out-of-bounds value that got clamped
            {
                // The logical position didn't change, but the UI field might show a
                // number that's different from its actual new position after clamping.
                // Rebuild to correct the UI field's displayed value.
                RebuildFramesListUI();
            }
            // If newClampedDataIndex == originalDataIndex and no clamping occurred, do nothing.
        }

        private void OnFrameItemPointerDown(
            PointerDownEvent evt,
            VisualElement frameElement,
            int originalDataIndex
        )
        {
            if (evt.button != 0 || _draggedFrameElement != null)
                return; // Already dragging or not left button

            // Ensure active layer and index are valid BEFORE accessing sprites
            if (
                _activeEditorLayer == null
                || originalDataIndex < 0
                || originalDataIndex >= _activeEditorLayer.Sprites.Count
            )
            {
                Debug.LogError(
                    $"OnFrameItemPointerDown: Invalid originalDataIndex ({originalDataIndex}) or no active layer. Sprite count: {(_activeEditorLayer?.Sprites?.Count ?? -1)}"
                );
                return;
            }

            // All checks passed, proceed with drag initiation
            _draggedFrameElement = frameElement;
            _draggedFrameOriginalDataIndex = originalDataIndex;

            try // Add a try-catch block for robustness during drag setup
            {
                _draggedFrameElement.RegisterCallback<PointerUpEvent>(OnDraggedFrameItemPointerUp);
                //_draggedFrameElement.CapturePointer(evt.pointerId);
                _draggedFrameElement.AddToClassList("frame-item-dragged"); // Ensure this class exists and has visible styling

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData("DraggedFrameDataIndex", _draggedFrameOriginalDataIndex);

                // Object references: Identical handling to clip dragging
                Object dragContextObject =
                    (_activeEditorLayer.SourceClip)
                    ?? (Object)ScriptableObject.CreateInstance<ScriptableObject>();
                if (dragContextObject == null) // Should not happen with ScriptableObject.CreateInstance fallback
                {
                    Debug.LogError("Failed to create dragContextObject for frame drag.");
                    // Attempt to cleanup what we've done so far to prevent stuck state
                    _draggedFrameElement.ReleasePointer(evt.pointerId);
                    _draggedFrameElement.UnregisterCallback<PointerUpEvent>(
                        OnDraggedFrameItemPointerUp
                    );
                    _draggedFrameElement.RemoveFromClassList("frame-item-dragged");
                    _draggedFrameElement = null;
                    return;
                }
                DragAndDrop.objectReferences = new Object[] { dragContextObject };

                // Drag title: Safer handling for potentially null sprites
                Sprite spriteBeingDragged = _activeEditorLayer.Sprites[originalDataIndex];
                string dragTitle;
                if (spriteBeingDragged != null)
                {
                    dragTitle = !string.IsNullOrEmpty(spriteBeingDragged.name)
                        ? spriteBeingDragged.name
                        : $"Unnamed Sprite Frame {originalDataIndex + 1}";
                }
                else
                {
                    dragTitle = $"Empty Frame {originalDataIndex + 1}";
                }

                // Ensure dragTitle is never null or empty for StartDrag
                if (string.IsNullOrEmpty(dragTitle))
                {
                    dragTitle = "Dragging Frame"; // Absolute fallback
                }

                DragAndDrop.StartDrag(dragTitle); // This is the critical call
            }
            catch (System.Exception e)
            {
                Debug.LogError(
                    $"Exception during OnFrameItemPointerDown before StartDrag: {e.Message}\n{e.StackTrace}"
                );
                // If an exception occurred, try to clean up to prevent a stuck state
                if (_draggedFrameElement != null)
                {
                    if (_draggedFrameElement.HasPointerCapture(evt.pointerId))
                        _draggedFrameElement.ReleasePointer(evt.pointerId);
                    _draggedFrameElement.UnregisterCallback<PointerUpEvent>(
                        OnDraggedFrameItemPointerUp
                    );
                    _draggedFrameElement.RemoveFromClassList("frame-item-dragged");
                    _draggedFrameElement = null;
                }
                _draggedFrameOriginalDataIndex = -1;
                // Do not try to clear DragAndDrop.genericData here as StartDrag might not have been prepared fully
                return; // Stop further processing
            }

            // evt.StopPropagation();
        }

        private void OnDraggedFrameItemPointerUp(PointerUpEvent evt)
        {
            if (_draggedFrameElement == null || evt.currentTarget != _draggedFrameElement)
                return;

            if (DragAndDrop.GetGenericData("DraggedFrameDataIndex") != null)
            {
                CleanupFrameDragState(evt.pointerId);
            }
            else if (
                _draggedFrameElement != null
                && _draggedFrameElement.HasPointerCapture(evt.pointerId)
            )
            {
                // Minimal cleanup if drag was handled by a drop elsewhere
                _draggedFrameElement.ReleasePointer(evt.pointerId);
                _draggedFrameElement.UnregisterCallback<PointerUpEvent>(
                    OnDraggedFrameItemPointerUp
                );
                _draggedFrameElement.RemoveFromClassList("frame-item-dragged");
                _draggedFrameElement = null;
            }
            evt.StopPropagation();
        }

        private void OnApplyFpsToPreviewClicked()
        {
            _currentPreviewFps = Mathf.Max(0.1f, _fpsField.value);
            _fpsField.SetValueWithoutNotify(_currentPreviewFps);
            if (_animationPreview != null)
            {
                _animationPreview.Fps = _currentPreviewFps; // Your LayeredImage has an Fps setter
            }
        }

        private void OnSaveClipClicked()
        {
            if (_activeEditorLayer == null || _activeEditorLayer.SourceClip == null)
            {
                Debug.LogError("No active animation clip to save.");
                return;
            }

            AnimationClip clipToSave = _activeEditorLayer.SourceClip;
            string bindingPath = _activeEditorLayer.BindingPath;

            EditorCurveBinding spriteBinding = default;
            bool bindingFound = false;
            EditorCurveBinding[] allBindings = AnimationUtility.GetObjectReferenceCurveBindings(
                clipToSave
            );

            foreach (var b in allBindings)
            {
                if (
                    b.type == typeof(SpriteRenderer)
                    && b.propertyName == "m_Sprite"
                    && (string.IsNullOrEmpty(bindingPath) || b.path == bindingPath)
                ) // Match path if available
                {
                    spriteBinding = b;
                    bindingFound = true;
                    break;
                }
            }
            // If no specific binding path matched (e.g. path was empty or object renamed),
            // try to find *any* m_Sprite binding as a fallback.
            if (!bindingFound)
            {
                foreach (var b in allBindings)
                {
                    if (b.type == typeof(SpriteRenderer) && b.propertyName == "m_Sprite")
                    {
                        spriteBinding = b;
                        bindingFound = true;
                        Debug.LogWarning(
                            $"Saving to first available m_Sprite binding on '{clipToSave.name}' as specific path '{bindingPath}' was not found or empty."
                        );
                        break;
                    }
                }
            }

            if (!bindingFound)
            {
                Debug.LogError(
                    $"Cannot save '{clipToSave.name}': No SpriteRenderer m_Sprite binding found (Path Hint: '{bindingPath}'). Clip might be empty or not a sprite animation."
                );
                return;
            }

            // Use current (potentially reordered) sprites from _activeEditorLayer
            List<Sprite> spritesToSave = _activeEditorLayer.Sprites;
            ObjectReferenceKeyframe[] newKeyframes = new ObjectReferenceKeyframe[
                spritesToSave.Count
            ];
            float timePerFrame = (_currentPreviewFps > 0) ? (1.0f / _currentPreviewFps) : 0f;

            for (int i = 0; i < spritesToSave.Count; i++)
            {
                newKeyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i * timePerFrame,
                    value = spritesToSave[i],
                };
            }

            Undo.RecordObject(clipToSave, "Modify Animation Clip Frames");
            AnimationUtility.SetObjectReferenceCurve(clipToSave, spriteBinding, newKeyframes);
            clipToSave.frameRate = _currentPreviewFps;

            EditorUtility.SetDirty(clipToSave);
            AssetDatabase.SaveAssets(); // Save the modified asset
            // AssetDatabase.Refresh(); // Usually not needed after SaveAssets, but can ensure project view updates

            Debug.Log(
                $"Animation clip '{clipToSave.name}' saved with {spritesToSave.Count} frames at {_currentPreviewFps} FPS."
            );

            // Optional: Refresh internal data for the saved layer if needed,
            // but since _activeEditorLayer.Sprites *is* the source of truth now, it's consistent.
            // If user reloads this clip or makes another active then this one, it will read from the file.
        }

        private void OnDisable() // Or OnDestroy for EditorWindow
        {
            CleanupFrameDragState(-1); // Ensure drag state is cleaned if window closes mid-drag
            CleanupLoadedClipDragState(-1); // For loaded clips

            // If LayeredImage uses EditorApplication.update, ensure it's unhooked.
            // Your LayeredImage Fps setter has logic for this, but an explicit cleanup might be good.
            // If _animationPreview has a dispose method or explicit unhook for EditorApplication.update, call it.
            // e.g., if (_animationPreview is IDisposable disp) disp.Dispose();
            if (_animationPreview != null && _animationPreview.parent == _previewPanelHost)
            {
                _previewPanelHost.Remove(_animationPreview); // Remove from UI
            }
            _animationPreview = null;
        }
    }
}

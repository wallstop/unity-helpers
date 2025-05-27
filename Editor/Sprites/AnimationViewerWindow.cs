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
        private VisualElement _draggedElement;
        private int _draggedElementOriginalIndex;
        private VisualElement _dropPlaceholder;

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

            _framesContainer.RegisterCallback<DragUpdatedEvent>(OnFramesContainerDragUpdated);
            _framesContainer.RegisterCallback<DragPerformEvent>(OnFramesContainerDragPerform);
            _framesContainer.RegisterCallback<DragLeaveEvent>(OnFramesContainerDragLeave);

            _dropPlaceholder = new VisualElement();
            _dropPlaceholder.AddToClassList("drop-placeholder");
            _dropPlaceholder.style.visibility = Visibility.Hidden;

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
            foreach (var editorLayer in _loadedEditorLayers)
            {
                var itemElement = new VisualElement();
                itemElement.AddToClassList("loaded-clip-item");
                if (editorLayer == _activeEditorLayer)
                    itemElement.AddToClassList("loaded-clip-item--active");

                itemElement.Add(new Label(editorLayer.ClipName));
                var removeButton = new Button(() => RemoveEditorLayer(editorLayer)) { text = "X" };
                itemElement.Add(removeButton);

                itemElement.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0)
                        SetActiveEditorLayer(editorLayer);
                });
                _loadedClipsContainer.Add(itemElement);
            }
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

        // --- Drag and Drop Logic (Largely same as previous, operates on _currentFramesForEditingUI and _activeEditorLayer.Sprites) ---
        private void OnFramePointerDown(
            PointerDownEvent evt,
            VisualElement frameElement,
            int spriteListIndex
        )
        {
            // Only respond to left mouse button and if no drag is already in progress
            if (evt.button != 0 || _draggedElement != null)
                return;

            // Ensure spriteListIndex is valid
            if (
                _activeEditorLayer == null
                || spriteListIndex < 0
                || spriteListIndex >= _activeEditorLayer.Sprites.Count
            )
            {
                Debug.LogError(
                    $"OnFramePointerDown: Invalid spriteListIndex {spriteListIndex} or no active layer."
                );
                return;
            }

            _draggedElement = frameElement;
            _draggedElementOriginalIndex = spriteListIndex; // This is the index in _activeEditorLayer.Sprites

            // Register a PointerUpEvent on the dragged element itself for cleanup if the drag ends prematurely or is cancelled.
            // This is crucial.
            _draggedElement.RegisterCallback<PointerUpEvent>(OnDraggedElementPointerUp);
            _draggedElement.RegisterCallback<PointerLeaveEvent>(OnDraggedElementPointerLeave); // For when mouse leaves item while dragging

            _draggedElement.CapturePointer(evt.pointerId); // Capture the pointer to this element
            _draggedElement.AddToClassList("frame-item-dragged"); // Visual feedback

            DragAndDrop.PrepareStartDrag(); // Initialize system for a new drag
            // Store the original index of the sprite in the data list (_activeEditorLayer.Sprites)
            DragAndDrop.SetGenericData("DraggedSpriteListIndex", _draggedElementOriginalIndex);

            // DragAndDrop.objectReferences needs to be set to something non-null for StartDrag to work properly in editor.
            // Using the source clip is a good practice. If no clip, a dummy object.
            Object dragContextObject =
                (_activeEditorLayer?.SourceClip)
                ?? (Object)ScriptableObject.CreateInstance<ScriptableObject>();
            DragAndDrop.objectReferences = new Object[] { dragContextObject };

            // Start the drag operation. The string is just a title shown by some OS drag systems.
            Sprite spriteBeingDragged = _activeEditorLayer.Sprites[spriteListIndex];
            string dragTitle =
                spriteBeingDragged != null
                    ? spriteBeingDragged.name
                    : $"Frame {spriteListIndex + 1}";
            DragAndDrop.StartDrag(dragTitle);

            evt.StopPropagation(); // Prevent other handlers from interfering
        }

        private void OnDraggedElementPointerLeave(PointerLeaveEvent evt)
        {
            if (_draggedElement == null || evt.currentTarget != _draggedElement)
                return;
            // If DragAndDrop.GetGenericData indicates an active drag we initiated,
            // we don't do cleanup here. Cleanup happens on PointerUp or DragPerform.
            // This event is mostly for visual feedback if needed (e.g. change cursor if it leaves window).
        }

        private void OnDraggedElementPointerUp(PointerUpEvent evt)
        {
            // This event is on _draggedElement. If it's null, something is wrong or event is stale.
            if (_draggedElement == null || evt.currentTarget != _draggedElement)
                return;

            // If DragAndDrop.GetGenericData shows a drag was actually started and not completed by a drop target.
            // This check ensures we only cleanup if this PointerUp truly signifies the end of our specific drag op.
            if (DragAndDrop.GetGenericData("DraggedSpriteListIndex") != null)
            {
                // This means the drag ended without a successful drop on _framesContainer.
                // Perform full cleanup.
                CleanupDragState(evt.pointerId);
            }
            // If a drop *did* occur, DragPerform would have already called CleanupDragState and cleared GenericData.
            // In that case, we still want to release pointer and unregister, which CleanupDragState handles if _draggedElement is set.
            else if (_draggedElement != null && _draggedElement.HasPointerCapture(evt.pointerId)) // Ensure it's still captured
            {
                // Minimal cleanup if drag was handled by a drop elsewhere but pointerup still fires here
                _draggedElement.ReleasePointer(evt.pointerId);
                _draggedElement.UnregisterCallback<PointerUpEvent>(OnDraggedElementPointerUp);
                _draggedElement.UnregisterCallback<PointerLeaveEvent>(OnDraggedElementPointerLeave);
                _draggedElement.RemoveFromClassList("frame-item-dragged"); // Could be redundant if full cleanup ran
                if (_draggedElement.userData == null)
                { // If DragPerform fully cleaned up, it might have nulled _draggedElement
                    _draggedElement = null; // Ensure it's null if not already.
                }
            }
            evt.StopPropagation();
        }

        private void OnFramesContainerDragUpdated(DragUpdatedEvent evt)
        {
            object draggedIndexData = DragAndDrop.GetGenericData("DraggedSpriteListIndex");

            // Check if the data being dragged is what we expect (an index from our frame list)
            if (draggedIndexData != null && _draggedElement != null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move; // Indicate that a drop is possible

                // --- Placeholder Logic (same as before, ensure it's robust) ---
                float mouseY = evt.localMousePosition.y;
                int newVisualIndex = -1;

                // Temporarily remove placeholder to recalculate its position
                if (_dropPlaceholder.parent == _framesContainer)
                    _framesContainer.Remove(_dropPlaceholder);

                for (int i = 0; i < _framesContainer.childCount; i++)
                {
                    VisualElement child = _framesContainer[i];
                    if (child == _draggedElement)
                        continue; // Skip the element being dragged

                    float childMidY = child.layout.yMin + child.layout.height / 2f;
                    if (mouseY < childMidY)
                    {
                        newVisualIndex = i;
                        break;
                    }
                }
                // If mouse is below all other items (and not the last item itself being dragged to the end)
                if (newVisualIndex == -1 && _framesContainer.childCount > 0)
                {
                    bool draggedIsLastVisible = false;
                    if (
                        _framesContainer.childCount > 0
                        && _draggedElement
                            == _framesContainer.ElementAt(_framesContainer.childCount - 1)
                    )
                    {
                        draggedIsLastVisible = true;
                    }
                    if (!draggedIsLastVisible || _framesContainer.childCount > 1)
                    { // Allow drop at very end if not dragging last item
                        newVisualIndex = _framesContainer.childCount;
                    }
                }

                if (newVisualIndex != -1)
                {
                    _framesContainer.Insert(newVisualIndex, _dropPlaceholder);
                    _dropPlaceholder.style.visibility = Visibility.Visible;
                }
                else if (_framesContainer.childCount == 0 && _draggedElement != null) // Dragging into an empty list
                {
                    _framesContainer.Add(_dropPlaceholder);
                    _dropPlaceholder.style.visibility = Visibility.Visible;
                }
                else // No valid insertion point, hide placeholder
                {
                    if (_dropPlaceholder.parent == _framesContainer)
                        _framesContainer.Remove(_dropPlaceholder);
                    _dropPlaceholder.style.visibility = Visibility.Hidden;
                }
                // --- End Placeholder Logic ---
            }
            else
            {
                // If the drag data is not what we expect, reject the drag
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
            evt.StopPropagation();
        }

        private void OnFramesContainerDragPerform(DragPerformEvent evt)
        {
            object draggedIndexData = DragAndDrop.GetGenericData("DraggedSpriteListIndex");

            if (draggedIndexData != null && _draggedElement != null && _activeEditorLayer != null)
            {
                int originalSpriteListIndex = (int)draggedIndexData;

                // Validate index (belt and braces)
                if (
                    originalSpriteListIndex < 0
                    || originalSpriteListIndex >= _activeEditorLayer.Sprites.Count
                )
                {
                    Debug.LogError("DragPerform: Stale or invalid dragged index. Aborting drop.");
                    CleanupDragState(-1); // Clean up with a generic pointer ID
                    evt.StopPropagation();
                    return;
                }

                Sprite movedSprite = _activeEditorLayer.Sprites[originalSpriteListIndex];
                _activeEditorLayer.Sprites.RemoveAt(originalSpriteListIndex); // Remove from old data position

                // Determine insertion index in the data list (_activeEditorLayer.Sprites)
                int placeholderVisualIndex = _framesContainer.IndexOf(_dropPlaceholder);
                int targetSpriteListIndex;

                if (placeholderVisualIndex != -1)
                {
                    // Count actual non-dragged, non-placeholder items before the placeholder's visual position
                    int itemsBeforePlaceholder = 0;
                    for (int i = 0; i < placeholderVisualIndex; i++)
                    {
                        VisualElement currentElement = _framesContainer[i];
                        // Only count elements that are NOT the one being dragged and NOT the placeholder itself
                        if (currentElement != _draggedElement && currentElement != _dropPlaceholder)
                        {
                            itemsBeforePlaceholder++;
                        }
                    }
                    targetSpriteListIndex = itemsBeforePlaceholder;
                }
                else // No placeholder visible (e.g., dropped at the very end or into empty list)
                {
                    targetSpriteListIndex = _activeEditorLayer.Sprites.Count; // Add to the end of the data list
                }
                targetSpriteListIndex = Mathf.Clamp(
                    targetSpriteListIndex,
                    0,
                    _activeEditorLayer.Sprites.Count
                );
                _activeEditorLayer.Sprites.Insert(targetSpriteListIndex, movedSprite); // Insert into new data position

                DragAndDrop.AcceptDrag(); // Signal to the system that the drop was successful

                // Crucially, perform full cleanup AFTER data ops but BEFORE UI rebuilds that might affect element references.
                // We pass -1 for pointerId because DragPerform doesn't have a specific pointerId from its event args.
                // The original pointerId from PointerDown was on _draggedElement.
                CleanupDragState(-1);

                RebuildFramesListUI(); // Rebuild the UI list of frames
                RecreatePreviewImage(); // Recreate the LayeredImage preview
            }
            else // Dragged data was not valid, or state was inconsistent
            {
                // Even if we didn't accept, if a drag was in progress, clean it up.
                if (DragAndDrop.GetGenericData("DraggedSpriteListIndex") != null)
                {
                    CleanupDragState(-1);
                }
            }
            evt.StopPropagation();
        }

        private void CleanupDragState(int pointerIdToRelease)
        {
            if (_draggedElement != null)
            {
                // Release pointer capture if this element still has it.
                // Check for specific pointerId if provided and valid, otherwise try to release any.
                if (
                    pointerIdToRelease != -1
                    && _draggedElement.HasPointerCapture(pointerIdToRelease)
                )
                {
                    _draggedElement.ReleasePointer(pointerIdToRelease);
                }
                else if (pointerIdToRelease == -1 && _draggedElement.HasPointerCapture(-1)) // Check for any capture
                {
                    _draggedElement.ReleasePointer(-1); // Release all captures
                }

                _draggedElement.UnregisterCallback<PointerUpEvent>(OnDraggedElementPointerUp);
                _draggedElement.UnregisterCallback<PointerLeaveEvent>(OnDraggedElementPointerLeave);
                _draggedElement.RemoveFromClassList("frame-item-dragged");
                _draggedElement = null; // Nullify the reference
            }

            _draggedElementOriginalIndex = -1; // Reset original index

            // Remove and hide the drop placeholder
            if (_dropPlaceholder != null && _dropPlaceholder.parent == _framesContainer)
            {
                _framesContainer.Remove(_dropPlaceholder);
            }
            if (_dropPlaceholder != null)
                _dropPlaceholder.style.visibility = Visibility.Hidden;

            // Clear the generic data associated with the drag
            DragAndDrop.SetGenericData("DraggedSpriteListIndex", null);
        }

        private void OnFramesContainerDragLeave(DragLeaveEvent evt)
        {
            // Only hide placeholder if the mouse truly leaves the container,
            // not just moving from the container onto one of its children (like the placeholder itself).
            if (evt.target == _framesContainer)
            {
                if (_dropPlaceholder.parent == _framesContainer)
                    _framesContainer.Remove(_dropPlaceholder);
                _dropPlaceholder.style.visibility = Visibility.Hidden;
            }
            // Do NOT clean up _draggedElement here. The drag might still be active globally.
            // Cleanup is handled by OnFramesContainerDragPerform or OnDraggedElementPointerUp.
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

        private void RebuildFramesListUI() // For the _activeEditorLayer
        {
            _framesContainer.Clear();
            _currentFramesForEditingUI.Clear(); // Clear old UI data list

            if (_dropPlaceholder.parent == _framesContainer)
                _framesContainer.Remove(_dropPlaceholder);
            if (_activeEditorLayer == null)
                return;

            for (int i = 0; i < _activeEditorLayer.Sprites.Count; i++)
            {
                Sprite sprite = _activeEditorLayer.Sprites[i];
                var frameUIData = new SpriteFrameUIData(sprite, i); // Store current list index as DisplayIndex
                _currentFramesForEditingUI.Add(frameUIData); // Add to UI data list

                VisualElement frameElement = new VisualElement();
                frameElement.AddToClassList("frame-item");
                // frameElement.userData = frameUIData; // Store UI data if needed, but index is enough for drag

                Image frameImage = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit };
                frameImage.AddToClassList("frame-image");

                VisualElement frameInfo = new VisualElement();
                frameInfo.AddToClassList("frame-info");
                frameInfo.Add(new Label($"Frame: {i + 1}")); // UI is 1-based
                frameInfo.Add(new Label($"Sprite: {(sprite != null ? sprite.name : "(None)")}"));

                IntegerField indexField = new IntegerField($"Order:")
                {
                    value = i + 1,
                    isReadOnly = true,
                };
                indexField.AddToClassList("frame-index-field");

                frameElement.Add(frameImage);
                frameElement.Add(frameInfo);
                frameElement.Add(indexField);

                // Pass 'i' which is the current index in _activeEditorLayer.Sprites and _currentFramesForEditingUI
                int currentIndexInList = i;
                frameElement.RegisterCallback<PointerDownEvent>(evt =>
                    OnFramePointerDown(evt, frameElement, currentIndexInList)
                );

                _framesContainer.Add(frameElement);
                frameUIData.VisualElement = frameElement;
            }
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
            CleanupDragState(-1); // Ensure drag state is cleaned if window closes mid-drag

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

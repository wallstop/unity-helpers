namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using Core.Extension;
    using Core.Helper;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Debug = UnityEngine.Debug;
    using Object = UnityEngine.Object;

    public sealed class SpriteSheetAnimationCreator : EditorWindow
    {
        private const float ThumbnailSize = 64f;

        private Texture2D _selectedSpriteSheet;
        private readonly List<Sprite> _availableSprites = new();

        private readonly List<AnimationDefinition> _animationDefinitions = new();

        private ObjectField _spriteSheetField;
        private Button _refreshSpritesButton;
        private Button _loadSpritesButton;
        private ScrollView _spriteThumbnailsScrollView;
        private VisualElement _spriteThumbnailsContainer;
        private ListView _animationDefinitionsListView;
        private Button _addAnimationDefinitionButton;
        private Button _generateAnimationsButton;

        private VisualElement _previewContainer;
        private Image _previewImage;
        private Label _previewFrameLabel;
        private Button _playPreviewButton;
        private Button _stopPreviewButton;
        private Button _prevFrameButton;
        private Button _nextFrameButton;
        private Slider _previewScrubber;

        private bool _isPreviewing;
        private int _currentPreviewAnimDefIndex = -1;
        private int _currentPreviewSpriteIndex;
        private AnimationDefinition _currentPreviewDefinition;
        private readonly EditorApplication.CallbackFunction _editorUpdateCallback;
        private readonly Stopwatch _timer = Stopwatch.StartNew();

        private TimeSpan? _lastTick;

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Sprite Sheet Animation Creator")]
        public static void ShowWindow()
        {
            SpriteSheetAnimationCreator window = GetWindow<SpriteSheetAnimationCreator>();
            window.titleContent = new GUIContent("Sprite Animation Creator");
            window.minSize = new Vector2(600, 700);
        }

        [Serializable]
        [DataContract]
        public sealed class AnimationDefinition
        {
            public string Name = "New Animation";
            public int StartSpriteIndex;
            public int EndSpriteIndex;
            public float DefaultFrameRate = 12f;
            public AnimationCurve FrameRateCurve = AnimationCurve.Constant(0, 1, 12f);
            public List<Sprite> SpritesToAnimate = new();

            public TextField nameField;
            public IntegerField startIndexField;
            public IntegerField endIndexField;
            public FloatField defaultFrameRateField;
            public CurveField frameRateCurveField;
            public Label spriteCountLabel;
            public Button previewButton;
            public Button removeButton;
        }

        public SpriteSheetAnimationCreator()
        {
            _editorUpdateCallback = OnEditorUpdate;
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;

            VisualElement topSection = new()
            {
                style = { flexDirection = FlexDirection.Row, marginBottom = 10 },
            };
            _spriteSheetField = new ObjectField("Sprite Sheet")
            {
                objectType = typeof(Texture2D),
                allowSceneObjects = false,
                style =
                {
                    flexGrow = 1,
                    flexShrink = 0,
                    minHeight = 20,
                },
            };
            _spriteSheetField.RegisterValueChangedCallback(OnSpriteSheetSelected);
            topSection.Add(_spriteSheetField);

            _loadSpritesButton = new Button(() =>
            {
                string filePath = string.Empty;
                if (_spriteSheetField.value != null)
                {
                    filePath = AssetDatabase.GetAssetPath(_spriteSheetField.value);
                }

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    filePath = Application.dataPath;
                }

                string selectedPath = EditorUtility.OpenFilePanel(
                    "Select Sprite Sheet",
                    filePath,
                    "png,jpg,gif,bmp,psd"
                );
                if (string.IsNullOrWhiteSpace(selectedPath))
                {
                    return;
                }

                string relativePath = DirectoryHelper.AbsoluteToUnityRelativePath(selectedPath);
                if (!string.IsNullOrWhiteSpace(relativePath))
                {
                    Texture2D loadedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                        relativePath
                    );
                    if (loadedTexture != null)
                    {
                        _spriteSheetField.value = loadedTexture;
                    }
                }
            })
            {
                text = "Load Sprites",
                style = { marginLeft = 5, minHeight = 20 },
            };
            topSection.Add(_loadSpritesButton);

            _refreshSpritesButton = new Button(LoadAndDisplaySprites)
            {
                text = "Refresh Sprites",
                style = { marginLeft = 5, minHeight = 20 },
            };
            topSection.Add(_refreshSpritesButton);
            root.Add(topSection);

            Label thumbnailsLabel = new("Available Sprites:")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 5,
                    marginBottom = 5,
                },
            };
            root.Add(thumbnailsLabel);
            _spriteThumbnailsScrollView = new ScrollView(ScrollViewMode.Horizontal)
            {
                style =
                {
                    height = ThumbnailSize + 20,
                    minHeight = ThumbnailSize + 20,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderBottomColor = Color.gray,
                    borderTopColor = Color.gray,
                    borderLeftColor = Color.gray,
                    borderRightColor = Color.gray,
                    paddingLeft = 5,
                    paddingRight = 5,
                    paddingTop = 5,
                    paddingBottom = 5,
                    marginBottom = 10,
                },
            };
            _spriteThumbnailsContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row },
            };
            _spriteThumbnailsScrollView.Add(_spriteThumbnailsContainer);
            root.Add(_spriteThumbnailsScrollView);

            Label animDefsLabel = new("Animation Definitions:")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 10,
                    marginBottom = 5,
                },
            };
            root.Add(animDefsLabel);

            _animationDefinitionsListView = new ListView(
                _animationDefinitions,
                130,
                MakeAnimationDefinitionItem,
                BindAnimationDefinitionItem
            )
            {
                selectionType = SelectionType.None,
                style = { flexGrow = 1, minHeight = 200 },
            };
            root.Add(_animationDefinitionsListView);

            _addAnimationDefinitionButton = new Button(AddAnimationDefinition)
            {
                text = "Add Animation Definition",
                style = { marginTop = 5 },
            };
            root.Add(_addAnimationDefinitionButton);

            Label previewSectionLabel = new("Animation Preview:")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 15,
                    marginBottom = 5,
                },
            };
            root.Add(previewSectionLabel);

            _previewContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    alignItems = Align.Center,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderBottomColor = Color.gray,
                    borderTopColor = Color.gray,
                    borderLeftColor = Color.gray,
                    borderRightColor = Color.gray,
                    paddingBottom = 10,
                    paddingTop = 10,
                    minHeight = 150,
                },
            };
            _previewImage = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    width = 128,
                    height = 128,
                    marginBottom = 10,
                    backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f)),
                },
            };
            _previewContainer.Add(_previewImage);

            _previewFrameLabel = new Label("Frame: -/- | FPS: -")
            {
                style = { alignSelf = Align.Center, marginBottom = 5 },
            };
            _previewContainer.Add(_previewFrameLabel);

            _previewScrubber = new Slider(0, 1)
            {
                style =
                {
                    minWidth = 200,
                    marginBottom = 5,
                    visibility = Visibility.Hidden,
                },
            };
            _previewScrubber.RegisterValueChangedCallback(evt =>
            {
                if (
                    _currentPreviewDefinition != null
                    && 0 < _currentPreviewDefinition.SpritesToAnimate.Count
                )
                {
                    int frame = Mathf.FloorToInt(
                        evt.newValue * (_currentPreviewDefinition.SpritesToAnimate.Count - 1)
                    );
                    SetPreviewFrame(frame);
                }
            });
            _previewContainer.Add(_previewScrubber);

            VisualElement previewControls = new()
            {
                style = { flexDirection = FlexDirection.Row, justifyContent = Justify.Center },
            };
            _prevFrameButton = new Button(() => AdjustPreviewFrame(-1))
            {
                text = "◀",
                style = { minWidth = 40 },
            };
            _playPreviewButton = new Button(PlayCurrentPreview)
            {
                text = "▶ Play",
                style = { minWidth = 70 },
            };
            _stopPreviewButton = new Button(StopCurrentPreview)
            {
                text = "◼ Stop",
                style = { minWidth = 70, display = DisplayStyle.None },
            };
            _nextFrameButton = new Button(() => AdjustPreviewFrame(1))
            {
                text = "▶",
                style = { minWidth = 40 },
            };
            previewControls.Add(_prevFrameButton);
            previewControls.Add(_playPreviewButton);
            previewControls.Add(_stopPreviewButton);
            previewControls.Add(_nextFrameButton);
            _previewContainer.Add(previewControls);
            root.Add(_previewContainer);

            _generateAnimationsButton = new Button(GenerateAnimations)
            {
                text = "Generate Animation Files",
                style = { marginTop = 15, height = 30 },
            };
            root.Add(_generateAnimationsButton);

            if (_selectedSpriteSheet != null)
            {
                _spriteSheetField.SetValueWithoutNotify(_selectedSpriteSheet);
                LoadAndDisplaySprites();
            }
            _animationDefinitionsListView.Rebuild();
        }

        private void OnEnable()
        {
            EditorApplication.update += _editorUpdateCallback;

            string data = SessionState.GetString(GetType().FullName, "");
            if (!string.IsNullOrEmpty(data))
            {
                JsonUtility.FromJsonOverwrite(data, this);
            }
            if (_selectedSpriteSheet != null)
            {
                EditorApplication.delayCall += () =>
                {
                    if (_spriteSheetField != null)
                    {
                        _spriteSheetField.value = _selectedSpriteSheet;
                    }
                    LoadAndDisplaySprites();
                    _animationDefinitionsListView.Rebuild();
                };
            }
        }

        private void OnDisable()
        {
            EditorApplication.update -= _editorUpdateCallback;
            StopCurrentPreview();

            string data = JsonUtility.ToJson(this);
            SessionState.SetString(GetType().FullName, data);
        }

        private void OnEditorUpdate()
        {
            if (
                !_isPreviewing
                || _currentPreviewDefinition is not { SpritesToAnimate: { Count: > 0 } }
            )
            {
                return;
            }

            _lastTick ??= _timer.Elapsed;
            float targetFps = 0;
            if (1 < _currentPreviewDefinition.SpritesToAnimate.Count)
            {
                _currentPreviewDefinition.FrameRateCurve.Evaluate(
                    _currentPreviewSpriteIndex
                        / (_currentPreviewDefinition.SpritesToAnimate.Count - 1f)
                );
            }

            if (targetFps <= 0)
            {
                targetFps = _currentPreviewDefinition.DefaultFrameRate;
            }

            if (targetFps <= 0)
            {
                targetFps = 1;
            }

            TimeSpan elapsed = _timer.Elapsed;
            TimeSpan deltaTime = TimeSpan.FromMilliseconds(1000 / targetFps);
            if (_lastTick + deltaTime > elapsed)
            {
                return;
            }

            _lastTick += deltaTime;
            int nextFrame = _currentPreviewSpriteIndex.WrappedIncrement(
                _currentPreviewDefinition.SpritesToAnimate.Count
            );
            SetPreviewFrame(nextFrame);
        }

        private void OnSpriteSheetSelected(ChangeEvent<Object> evt)
        {
            _selectedSpriteSheet = evt.newValue as Texture2D;
            _animationDefinitions.Clear();
            AddAnimationDefinition();
            _animationDefinitionsListView.Rebuild();
            LoadAndDisplaySprites();
            StopCurrentPreview();
            _previewImage.sprite = null;
            _previewImage.style.backgroundImage = null;
            _previewFrameLabel.text = "Frame: -/- | FPS: -";
        }

        private void LoadAndDisplaySprites()
        {
            if (_selectedSpriteSheet == null)
            {
                EditorUtility.DisplayDialog("Error", "No sprite sheet selected.", "OK");
                _availableSprites.Clear();
                UpdateSpriteThumbnails();
                return;
            }

            string path = AssetDatabase.GetAssetPath(_selectedSpriteSheet);
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Error", "Selected texture is not an asset.", "OK");
                return;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Could not get TextureImporter for the selected texture.",
                    "OK"
                );
                return;
            }

            bool importSettingsChanged = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importSettingsChanged = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                bool fix = EditorUtility.DisplayDialog(
                    "Sprite Mode",
                    "The selected texture is not in 'Sprite Mode: Multiple'. This is required to extract individual sprites.\n\nAttempt to change it automatically?",
                    "Yes, Change It",
                    "No, I'll Do It"
                );
                if (fix)
                {
                    importer.spriteImportMode = SpriteImportMode.Multiple;

                    importSettingsChanged = true;
                }
                else
                {
                    _availableSprites.Clear();
                    UpdateSpriteThumbnails();
                    return;
                }
            }

            if (importSettingsChanged)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                AssetDatabase.Refresh();
            }

            _availableSprites.Clear();
            Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite && sprite != null)
                {
                    _availableSprites.Add(sprite);
                }
            }

            _availableSprites.SortByName();

            if (_availableSprites.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Sprites",
                    "No sprites found in the selected Texture. Ensure it's sliced correctly in the Sprite Editor.",
                    "OK"
                );
            }

            UpdateSpriteThumbnails();
            UpdateAllAnimationDefinitionSprites();
            _animationDefinitionsListView.Rebuild();
        }

        private void UpdateSpriteThumbnails()
        {
            _spriteThumbnailsContainer.Clear();
            if (_availableSprites.Count == 0)
            {
                _spriteThumbnailsContainer.Add(new Label("No sprites loaded or sheet not sliced."));
                return;
            }

            for (int i = 0; i < _availableSprites.Count; ++i)
            {
                Sprite sprite = _availableSprites[i];
                VisualElement thumbContainer = new()
                {
                    style = { alignItems = Align.Center, marginRight = 5 },
                };
                Image img = new()
                {
                    sprite = sprite,
                    scaleMode = ScaleMode.ScaleToFit,
                    style = { width = ThumbnailSize, height = ThumbnailSize },
                };
                thumbContainer.Add(img);
                thumbContainer.Add(new Label($"{i}") { style = { fontSize = 9 } });
                _spriteThumbnailsContainer.Add(thumbContainer);
            }
        }

        private void UpdateAllAnimationDefinitionSprites()
        {
            foreach (AnimationDefinition def in _animationDefinitions)
            {
                UpdateSpritesForDefinition(def);
            }
        }

        private VisualElement MakeAnimationDefinitionItem()
        {
            VisualElement container = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    borderBottomWidth = 1,
                    borderBottomColor = Color.gray,
                    paddingBottom = 10,
                    paddingTop = 5,
                },
            };
            VisualElement firstRow = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 3,
                },
            };
            VisualElement secondRow = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 3,
                },
            };
            VisualElement thirdRow = new()
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center },
            };

            TextField nameField = new("Name:")
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    marginRight = 5,
                },
            };
            Label spriteCountLabel = new("Sprites: 0")
            {
                style = { minWidth = 80, marginRight = 5 },
            };
            Button removeButton = new() { text = "Remove", style = { minWidth = 60 } };

            firstRow.Add(nameField);
            firstRow.Add(spriteCountLabel);
            firstRow.Add(removeButton);

            IntegerField startField = new("Start Idx:")
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    marginRight = 5,
                },
                tooltip = "Index of the first sprite (from 'Available Sprites' above, 0-based).",
            };
            IntegerField endField = new("End Idx:")
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    marginRight = 5,
                },
                tooltip = "Index of the last sprite (inclusive).",
            };
            Button previewButton = new() { text = "Preview This", style = { minWidth = 100 } };

            secondRow.Add(startField);
            secondRow.Add(endField);
            secondRow.Add(previewButton);

            FloatField fpsField = new("Default FPS:")
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    marginRight = 5,
                },
            };
            CurveField curveField = new("FPS Curve:") { style = { flexGrow = 1, flexShrink = 1 } };

            thirdRow.Add(fpsField);
            thirdRow.Add(curveField);

            container.Add(firstRow);
            container.Add(secondRow);
            container.Add(thirdRow);

            container.userData = new AnimationDefUITags
            {
                nameField = nameField,
                startIndexField = startField,
                endIndexField = endField,
                defaultFrameRateField = fpsField,
                frameRateCurveField = curveField,
                spriteCountLabel = spriteCountLabel,
                previewButton = previewButton,
                removeButton = removeButton,
            };
            return container;
        }

        private class AnimationDefUITags
        {
            public TextField nameField;
            public IntegerField startIndexField;
            public IntegerField endIndexField;
            public FloatField defaultFrameRateField;
            public CurveField frameRateCurveField;
            public Label spriteCountLabel;
            public Button previewButton;
            public Button removeButton;
        }

        private void BindAnimationDefinitionItem(VisualElement element, int index)
        {
            AnimationDefinition def = _animationDefinitions[index];
            if (element.userData is not AnimationDefUITags tags)
            {
                this.LogError(
                    $"Element UserData was not AnimationDefUITags, found: {element.userData?.GetType()}."
                );
                return;
            }

            def.nameField?.UnregisterValueChangedCallback(
                def.nameField.userData as EventCallback<ChangeEvent<string>>
            );

            def.startIndexField?.UnregisterValueChangedCallback(
                def.startIndexField.userData as EventCallback<ChangeEvent<int>>
            );

            def.endIndexField?.UnregisterValueChangedCallback(
                def.endIndexField.userData as EventCallback<ChangeEvent<int>>
            );

            def.defaultFrameRateField?.UnregisterValueChangedCallback(
                def.defaultFrameRateField.userData as EventCallback<ChangeEvent<float>>
            );

            def.frameRateCurveField?.UnregisterValueChangedCallback(
                def.frameRateCurveField.userData as EventCallback<ChangeEvent<AnimationCurve>>
            );

            if (def.removeButton != null)
            {
                def.removeButton.clicked -= (Action)def.removeButton.userData;
            }

            if (def.previewButton != null)
            {
                def.previewButton.clicked -= (Action)def.previewButton.userData;
            }

            def.nameField = tags.nameField;
            def.startIndexField = tags.startIndexField;
            def.endIndexField = tags.endIndexField;
            def.defaultFrameRateField = tags.defaultFrameRateField;
            def.frameRateCurveField = tags.frameRateCurveField;
            def.spriteCountLabel = tags.spriteCountLabel;
            def.removeButton = tags.removeButton;
            def.previewButton = tags.previewButton;

            def.nameField.SetValueWithoutNotify(def.Name);
            EventCallback<ChangeEvent<string>> nameChangeCallback = evt =>
            {
                def.Name = evt.newValue;
            };
            def.nameField.RegisterValueChangedCallback(nameChangeCallback);
            def.nameField.userData = nameChangeCallback;

            def.startIndexField.SetValueWithoutNotify(def.StartSpriteIndex);
            EventCallback<ChangeEvent<int>> startChangeCallback = evt =>
            {
                def.StartSpriteIndex = Mathf.Clamp(
                    evt.newValue,
                    0,
                    _availableSprites.Count > 0 ? _availableSprites.Count - 1 : 0
                );
                if (def.StartSpriteIndex > def.EndSpriteIndex && 0 < _availableSprites.Count)
                {
                    def.EndSpriteIndex = def.StartSpriteIndex;
                }

                def.startIndexField.SetValueWithoutNotify(def.StartSpriteIndex);
                UpdateSpritesForDefinition(def);
                if (_currentPreviewAnimDefIndex == index)
                {
                    StartOrUpdateCurrentPreview(def);
                }
            };
            def.startIndexField.RegisterValueChangedCallback(startChangeCallback);
            def.startIndexField.userData = startChangeCallback;

            def.endIndexField.SetValueWithoutNotify(def.EndSpriteIndex);
            EventCallback<ChangeEvent<int>> endChangeCallback = evt =>
            {
                def.EndSpriteIndex = Mathf.Clamp(
                    evt.newValue,
                    0,
                    _availableSprites.Count > 0 ? _availableSprites.Count - 1 : 0
                );
                if (def.EndSpriteIndex < def.StartSpriteIndex && 0 < _availableSprites.Count)
                {
                    def.StartSpriteIndex = def.EndSpriteIndex;
                }

                def.endIndexField.SetValueWithoutNotify(def.EndSpriteIndex);
                UpdateSpritesForDefinition(def);
                if (_currentPreviewAnimDefIndex == index)
                {
                    StartOrUpdateCurrentPreview(def);
                }
            };
            def.endIndexField.RegisterValueChangedCallback(endChangeCallback);
            def.endIndexField.userData = endChangeCallback;

            def.defaultFrameRateField.SetValueWithoutNotify(def.DefaultFrameRate);
            EventCallback<ChangeEvent<float>> fpsChangeCallback = evt =>
            {
                def.DefaultFrameRate = Mathf.Max(0.1f, evt.newValue);
                def.defaultFrameRateField.SetValueWithoutNotify(def.DefaultFrameRate);

                if (IsCurveConstant(def.FrameRateCurve))
                {
                    def.FrameRateCurve = AnimationCurve.Constant(0, 1, def.DefaultFrameRate);
                    def.frameRateCurveField.SetValueWithoutNotify(def.FrameRateCurve);
                }
                if (_currentPreviewAnimDefIndex == index)
                {
                    StartOrUpdateCurrentPreview(def);
                }
            };
            def.defaultFrameRateField.RegisterValueChangedCallback(fpsChangeCallback);
            def.defaultFrameRateField.userData = fpsChangeCallback;

            def.frameRateCurveField.SetValueWithoutNotify(def.FrameRateCurve);
            EventCallback<ChangeEvent<AnimationCurve>> curveChangeCallback = evt =>
            {
                def.FrameRateCurve = evt.newValue;
                if (_currentPreviewAnimDefIndex == index)
                {
                    StartOrUpdateCurrentPreview(def);
                }
            };
            def.frameRateCurveField.RegisterValueChangedCallback(curveChangeCallback);
            def.frameRateCurveField.userData = curveChangeCallback;

            Action removeAction = () => RemoveAnimationDefinition(index);
            def.removeButton.clicked += removeAction;
            def.removeButton.userData = removeAction;

            Action previewAction = () =>
            {
                _currentPreviewAnimDefIndex = index;
                StartOrUpdateCurrentPreview(def);
            };
            def.previewButton.clicked += previewAction;
            def.previewButton.userData = previewAction;

            UpdateSpritesForDefinition(def);
        }

        private static bool IsCurveConstant(AnimationCurve curve)
        {
            if (curve == null || curve.keys.Length < 2)
            {
                return true;
            }

            float firstValue = curve.keys[0].value;
            for (int i = 1; i < curve.keys.Length; ++i)
            {
                if (!Mathf.Approximately(curve.keys[i].value, firstValue))
                {
                    return false;
                }
            }
            return true;
        }

        private void UpdateSpritesForDefinition(AnimationDefinition def)
        {
            def.SpritesToAnimate.Clear();
            if (
                0 < _availableSprites.Count
                && def.StartSpriteIndex <= def.EndSpriteIndex
                && def.StartSpriteIndex < _availableSprites.Count
                && def.EndSpriteIndex < _availableSprites.Count
                && def.StartSpriteIndex >= 0
                && def.EndSpriteIndex >= 0
            )
            {
                for (int i = def.StartSpriteIndex; i <= def.EndSpriteIndex; ++i)
                {
                    def.SpritesToAnimate.Add(_availableSprites[i]);
                }
            }
            if (def.spriteCountLabel != null)
            {
                def.spriteCountLabel.text = $"Sprites: {def.SpritesToAnimate.Count}";
            }
        }

        private void AddAnimationDefinition()
        {
            AnimationDefinition newDef = new();
            if (_selectedSpriteSheet != null)
            {
                newDef.Name = $"{_selectedSpriteSheet.name}_Anim_{_animationDefinitions.Count}";
            }
            if (0 < _availableSprites.Count)
            {
                newDef.EndSpriteIndex = _availableSprites.Count - 1;
            }
            newDef.FrameRateCurve = AnimationCurve.Constant(0, 1, newDef.DefaultFrameRate);
            _animationDefinitions.Add(newDef);
            UpdateSpritesForDefinition(newDef);
            _animationDefinitionsListView.Rebuild();
        }

        private void RemoveAnimationDefinition(int index)
        {
            if (index >= 0 && index < _animationDefinitions.Count)
            {
                if (_currentPreviewAnimDefIndex == index)
                {
                    StopCurrentPreview();
                }

                _animationDefinitions.RemoveAt(index);
                _animationDefinitionsListView.Rebuild();
                if (_currentPreviewAnimDefIndex > index)
                {
                    _currentPreviewAnimDefIndex--;
                }
            }
        }

        private void StartOrUpdateCurrentPreview(AnimationDefinition def)
        {
            _currentPreviewDefinition = def;
            if (def == null || def.SpritesToAnimate.Count == 0)
            {
                StopCurrentPreview();
                _previewImage.sprite = null;
                _previewImage.style.backgroundImage = null;
                _previewFrameLabel.text = "Frame: -/- | FPS: -";
                _previewScrubber.style.visibility = Visibility.Hidden;
                return;
            }

            _previewScrubber.style.visibility = Visibility.Visible;
            _previewScrubber.highValue = def.SpritesToAnimate.Count > 1 ? 1f : 0f;
            _previewScrubber.SetValueWithoutNotify(0);

            SetPreviewFrame(0);
        }

        private void PlayCurrentPreview()
        {
            if (
                _currentPreviewDefinition == null
                || _currentPreviewDefinition.SpritesToAnimate.Count == 0
            )
            {
                EditorUtility.DisplayDialog(
                    "Preview Error",
                    "No animation definition selected or definition has no sprites. Click 'Preview This' on an animation definition first.",
                    "OK"
                );
                return;
            }

            _isPreviewing = true;
            _playPreviewButton.style.display = DisplayStyle.None;
            _stopPreviewButton.style.display = DisplayStyle.Flex;
        }

        private void StopCurrentPreview()
        {
            _isPreviewing = false;
            _lastTick = null;
            _playPreviewButton.style.display = DisplayStyle.Flex;
            _stopPreviewButton.style.display = DisplayStyle.None;
        }

        private void SetPreviewFrame(int frameIndex)
        {
            if (_currentPreviewDefinition is not { SpritesToAnimate: { Count: > 0 } })
            {
                return;
            }

            _currentPreviewSpriteIndex = Mathf.Clamp(
                frameIndex,
                0,
                _currentPreviewDefinition.SpritesToAnimate.Count - 1
            );

            Sprite spriteToShow = _currentPreviewDefinition.SpritesToAnimate[
                _currentPreviewSpriteIndex
            ];
            _previewImage.sprite = spriteToShow;
            _previewImage.MarkDirtyRepaint();

            float currentCurveTime = 0f;
            if (_currentPreviewDefinition.SpritesToAnimate.Count > 1)
            {
                currentCurveTime =
                    (float)_currentPreviewSpriteIndex
                    / (_currentPreviewDefinition.SpritesToAnimate.Count - 1);
            }

            float fpsAtCurrent = _currentPreviewDefinition.FrameRateCurve.Evaluate(
                currentCurveTime
                    * _currentPreviewDefinition.FrameRateCurve.keys.LastOrDefault().time
            );
            if (fpsAtCurrent <= 0)
            {
                fpsAtCurrent = _currentPreviewDefinition.DefaultFrameRate;
            }

            _previewFrameLabel.text =
                $"Frame: {_currentPreviewSpriteIndex + 1}/{_currentPreviewDefinition.SpritesToAnimate.Count} | FPS: {fpsAtCurrent:F1}";

            if (_currentPreviewDefinition.SpritesToAnimate.Count > 1)
            {
                _previewScrubber.SetValueWithoutNotify(
                    (float)_currentPreviewSpriteIndex
                        / (_currentPreviewDefinition.SpritesToAnimate.Count - 1)
                );
            }
            else
            {
                _previewScrubber.SetValueWithoutNotify(0);
            }
        }

        private void AdjustPreviewFrame(int direction)
        {
            if (_currentPreviewDefinition is not { SpritesToAnimate: { Count: > 0 } })
            {
                return;
            }

            StopCurrentPreview();

            int newFrame = _currentPreviewSpriteIndex + direction;

            int count = _currentPreviewDefinition.SpritesToAnimate.Count;
            if (newFrame < 0)
            {
                newFrame = count - 1;
            }

            if (newFrame >= count)
            {
                newFrame = 0;
            }

            SetPreviewFrame(newFrame);
        }

        private void GenerateAnimations()
        {
            if (_selectedSpriteSheet == null)
            {
                EditorUtility.DisplayDialog("Error", "No sprite sheet loaded.", "OK");
                return;
            }
            if (_animationDefinitions.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No animation definitions created.", "OK");
                return;
            }

            string sheetPath = AssetDatabase.GetAssetPath(_selectedSpriteSheet);
            string directory = Path.GetDirectoryName(sheetPath);
            string animationsFolder = EditorUtility.OpenFolderPanel(
                "Select Output Directory",
                directory,
                string.Empty
            );
            if (string.IsNullOrWhiteSpace(animationsFolder))
            {
                return;
            }

            if (!Directory.Exists(animationsFolder))
            {
                Directory.CreateDirectory(animationsFolder);
            }

            int createdCount = 0;
            foreach (AnimationDefinition def in _animationDefinitions)
            {
                if (def.SpritesToAnimate.Count == 0)
                {
                    Debug.LogWarning($"Skipping animation '{def.Name}' as it has no sprites.");
                    continue;
                }

                AnimationClip clip = new() { frameRate = 60 };

                EditorCurveBinding spriteBinding = new()
                {
                    type = typeof(SpriteRenderer),
                    path = "",
                    propertyName = "m_Sprite",
                };

                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[
                    def.SpritesToAnimate.Count
                ];
                float currentTime = 0f;
                AnimationCurve curve = def.FrameRateCurve;
                if (curve == null || curve.keys.Length == 0)
                {
                    Debug.LogWarning(
                        $"Animation '{def.Name}' has an invalid FrameRateCurve. Falling back to DefaultFrameRate."
                    );
                    curve = AnimationCurve.Constant(0, 1, def.DefaultFrameRate);
                }

                if (curve.keys.Length == 0)
                {
                    curve.AddKey(0, def.DefaultFrameRate);
                }

                float curveDuration = curve.keys.LastOrDefault().time;
                if (curveDuration <= 0)
                {
                    curveDuration = 1f;
                }

                for (int i = 0; i < def.SpritesToAnimate.Count; ++i)
                {
                    keyframes[i] = new ObjectReferenceKeyframe
                    {
                        time = currentTime,
                        value = def.SpritesToAnimate[i],
                    };

                    if (i < def.SpritesToAnimate.Count - 1)
                    {
                        float normalizedTimeForCurve =
                            def.SpritesToAnimate.Count > 1
                                ? (float)i / (def.SpritesToAnimate.Count - 1)
                                : 0;
                        float timeForCurveEval = normalizedTimeForCurve * curveDuration;

                        float fps = curve.Evaluate(timeForCurveEval);
                        if (fps <= 0)
                        {
                            fps = def.DefaultFrameRate;
                        }

                        if (fps <= 0)
                        {
                            fps = 1;
                        }

                        currentTime += 1.0f / fps;
                    }
                }

                AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                string animName = string.IsNullOrEmpty(def.Name) ? "UnnamedAnim" : def.Name;

                foreach (char character in Path.GetInvalidFileNameChars())
                {
                    animName = animName.Replace(character, '_');
                }
                string assetPath = Path.Combine(animationsFolder, $"{animName}.anim");
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

                AssetDatabase.CreateAsset(clip, assetPath);
                createdCount++;
            }

            if (createdCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog(
                    "Success",
                    $"{createdCount} animation(s) created in:\n{animationsFolder}",
                    "OK"
                );
                EditorGUIUtility.PingObject(
                    AssetDatabase.LoadAssetAtPath<Object>(animationsFolder)
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Finished",
                    "No valid animations were generated.",
                    "OK"
                );
            }
        }

        private static void OnRootDragUpdated(DragUpdatedEvent evt)
        {
            if (DragAndDrop.objectReferences.Any(obj => obj is Texture2D))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }
        }

        private void OnRootDragPerform(DragPerformEvent evt)
        {
            Texture2D draggedTexture =
                DragAndDrop.objectReferences.FirstOrDefault(obj => obj is Texture2D) as Texture2D;
            if (draggedTexture != null)
            {
                _spriteSheetField.value = draggedTexture;
                DragAndDrop.AcceptDrag();
            }
        }

        public void OnBecameVisible()
        {
            rootVisualElement.RegisterCallback<DragUpdatedEvent>(OnRootDragUpdated);
            rootVisualElement.RegisterCallback<DragPerformEvent>(OnRootDragPerform);
        }

        public void OnBecameInvisible()
        {
            rootVisualElement.UnregisterCallback<DragUpdatedEvent>(OnRootDragUpdated);
            rootVisualElement.UnregisterCallback<DragPerformEvent>(OnRootDragPerform);
        }
    }
#endif
}

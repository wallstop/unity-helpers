namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEngine;
    using Core.Extension;
    using CustomEditors;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Object = UnityEngine.Object;

    public sealed class SpriteCropper : EditorWindow
    {
        private const string Name = "Sprite Cropper";

        private const string CroppedPrefix = "Cropped_";
        private static readonly string[] ImageFileExtensions =
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".bmp",
            ".tga",
            ".psd",
            ".gif",
        };

        private const float AlphaThreshold = 0.01f;

        [SerializeField]
        private List<Object> _inputDirectories = new();

        [SerializeField]
        private string _spriteNameRegex = ".*";

        [SerializeField]
        private bool _onlyNecessary;

        [SerializeField]
        private int _leftPadding;

        [SerializeField]
        private int _rightPadding;

        [SerializeField]
        private int _topPadding;

        [SerializeField]
        private int _bottomPadding;

        private List<string> _filesToProcess;
        private SerializedObject _serializedObject;
        private SerializedProperty _inputDirectoriesProperty;
        private SerializedProperty _onlyNecessaryProperty;
        private SerializedProperty _leftPaddingProperty;
        private SerializedProperty _rightPaddingProperty;
        private SerializedProperty _topPaddingProperty;
        private SerializedProperty _bottomPaddingProperty;
        private SerializedProperty _spriteNameRegexProperty;

        private Regex _regex;

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/" + Name)]
        private static void ShowWindow() => GetWindow<SpriteCropper>(Name);

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _inputDirectoriesProperty = _serializedObject.FindProperty(nameof(_inputDirectories));
            _onlyNecessaryProperty = _serializedObject.FindProperty(nameof(_onlyNecessary));
            _leftPaddingProperty = _serializedObject.FindProperty(nameof(_leftPadding));
            _rightPaddingProperty = _serializedObject.FindProperty(nameof(_rightPadding));
            _topPaddingProperty = _serializedObject.FindProperty(nameof(_topPadding));
            _bottomPaddingProperty = _serializedObject.FindProperty(nameof(_bottomPadding));
            _spriteNameRegexProperty = _serializedObject.FindProperty(nameof(_spriteNameRegex));
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Input directories", EditorStyles.boldLabel);
            _serializedObject.Update();
            PersistentDirectoryGUI.PathSelectorObjectArray(
                _inputDirectoriesProperty,
                nameof(SpriteCropper)
            );
            EditorGUILayout.PropertyField(_spriteNameRegexProperty, true);
            EditorGUILayout.PropertyField(_onlyNecessaryProperty, true);
            EditorGUILayout.PropertyField(_leftPaddingProperty, true);
            EditorGUILayout.PropertyField(_rightPaddingProperty, true);
            EditorGUILayout.PropertyField(_topPaddingProperty, true);
            EditorGUILayout.PropertyField(_bottomPaddingProperty, true);
            _serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Find Sprites To Process"))
            {
                _regex = !string.IsNullOrWhiteSpace(_spriteNameRegex)
                    ? new Regex(_spriteNameRegex)
                    : null;
                FindFilesToProcess();
            }

            if (_filesToProcess is { Count: > 0 })
            {
                GUILayout.Label(
                    $"Found {_filesToProcess.Count} sprites to process.",
                    EditorStyles.boldLabel
                );
                if (GUILayout.Button($"Process {_filesToProcess.Count} Sprites"))
                {
                    ProcessFoundSprites();
                    _filesToProcess = null;
                }
            }
            else if (_filesToProcess != null)
            {
                GUILayout.Label(
                    "No sprites found to process in the selected directories.",
                    EditorStyles.label
                );
            }
        }

        private void FindFilesToProcess()
        {
            _filesToProcess = new List<string>();
            if (_inputDirectories is not { Count: > 0 })
            {
                this.LogWarn($"No input directories selected.");
                return;
            }

            foreach (Object maybeDirectory in _inputDirectories.Where(d => d != null))
            {
                string assetPath = AssetDatabase.GetAssetPath(maybeDirectory);
                if (!AssetDatabase.IsValidFolder(assetPath))
                {
                    this.LogWarn($"Skipping invalid path: {assetPath}");
                    continue;
                }

                IEnumerable<string> files = Directory
                    .GetFiles(assetPath, "*.*", SearchOption.AllDirectories)
                    .Where(file =>
                        Array.Exists(
                            ImageFileExtensions,
                            extension =>
                                file.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                        )
                    );

                foreach (string file in files)
                {
                    if (file.Contains(CroppedPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (_regex != null && !_regex.IsMatch(fileName))
                    {
                        continue;
                    }

                    _filesToProcess.Add(file);
                }
            }
            Repaint();
        }

        private void ProcessFoundSprites()
        {
            if (_filesToProcess is not { Count: > 0 })
            {
                this.LogWarn($"No files found or selected for processing.");
                return;
            }

            string lastProcessed = null;
            try
            {
                int total = _filesToProcess.Count;
                List<TextureImporter> newImporters = new();
                AssetDatabase.StartAssetEditing();
                try
                {
                    for (int i = 0; i < _filesToProcess.Count; ++i)
                    {
                        string file = _filesToProcess[i];
                        lastProcessed = file;
                        EditorUtility.DisplayProgressBar(
                            Name,
                            $"Pre-processing {i + 1}/{total}: {Path.GetFileName(file)}",
                            i / (float)total
                        );
                        CheckPreProcessNeeded(file);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                AssetDatabase.StartAssetEditing();
                try
                {
                    for (int i = 0; i < _filesToProcess.Count; ++i)
                    {
                        string file = _filesToProcess[i];
                        lastProcessed = file;
                        EditorUtility.DisplayProgressBar(
                            Name,
                            $"Processing {i + 1}/{total}: {Path.GetFileName(file)}",
                            i / (float)total
                        );
                        TextureImporter newImporter = ProcessSprite(file);
                        if (newImporter != null)
                        {
                            newImporters.Add(newImporter);
                        }
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    foreach (TextureImporter newImporter in newImporters)
                    {
                        newImporter.SaveAndReimport();
                    }
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                this.Log($"{newImporters.Count} sprites processed successfully.");
            }
            catch (Exception e)
            {
                this.LogError(
                    $"An error occurred during processing. Last processed: {lastProcessed}.",
                    e
                );
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void CheckPreProcessNeeded(string assetPath)
        {
            string assetDirectory = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrWhiteSpace(assetDirectory))
            {
                return;
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer is not { textureType: TextureImporterType.Sprite })
            {
                return;
            }

            TextureImporterSettings originalSettings = new();
            importer.ReadTextureSettings(originalSettings);

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }

        private TextureImporter ProcessSprite(string assetPath)
        {
            string assetDirectory = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrWhiteSpace(assetDirectory))
            {
                return null;
            }

            if (
                AssetImporter.GetAtPath(assetPath)
                is not TextureImporter { textureType: TextureImporterType.Sprite } importer
            )
            {
                return null;
            }

            TextureImporterSettings originalSettings = new();
            importer.ReadTextureSettings(originalSettings);

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
            {
                return null;
            }

            Color32[] pixels = tex.GetPixels32();
            int width = tex.width;
            int height = tex.height;
            int minX = width;
            int minY = height;
            int maxX = 0;
            int maxY = 0;
            bool hasVisible = false;
            object lockObject = new();
            Parallel.For(
                0,
                width * height,
                () => (minX: width, minY: height, maxX: 0, maxY: 0, hasVisible: false),
                (index, _, localState) =>
                {
                    int x = index % width;
                    int y = index / width;

                    float a = pixels[index].a / 255f;
                    if (a > AlphaThreshold)
                    {
                        localState.hasVisible = true;
                        localState.minX = Mathf.Min(localState.minX, x);
                        localState.minY = Mathf.Min(localState.minY, y);
                        localState.maxX = Mathf.Max(localState.maxX, x);
                        localState.maxY = Mathf.Max(localState.maxY, y);
                    }
                    return localState;
                },
                finalLocalState =>
                {
                    if (finalLocalState.hasVisible)
                    {
                        lock (lockObject)
                        {
                            hasVisible = true;
                            minX = Mathf.Min(minX, finalLocalState.minX);
                            minY = Mathf.Min(minY, finalLocalState.minY);
                            maxX = Mathf.Max(maxX, finalLocalState.maxX);
                            maxY = Mathf.Max(maxY, finalLocalState.maxY);
                        }
                    }
                }
            );

            int cropWidth;
            int cropHeight;
            if (!hasVisible)
            {
                cropWidth = 1;
                cropHeight = 1;
                minX = 0;
                minY = 0;
            }
            else
            {
                minX -= _leftPadding;
                minY -= _bottomPadding;
                maxX += _rightPadding;
                maxY += _topPadding;
                cropWidth = maxX - minX + 1;
                cropHeight = maxY - minY + 1;
            }

            if (_onlyNecessary && (!hasVisible || (cropWidth == width && cropHeight == height)))
            {
                return null;
            }

            Texture2D cropped = new(cropWidth, cropHeight, TextureFormat.RGBA32, false);
            Color32[] croppedPixels = new Color32[cropWidth * cropHeight];

            Parallel.For(
                0,
                cropHeight,
                y =>
                {
                    int sourceYOffset = (y + minY) * width;
                    int destYOffset = y * cropWidth;
                    for (int x = 0; x < cropWidth; ++x)
                    {
                        int sourceIndex = sourceYOffset + x + minX;
                        Color32 sourcePixel;
                        if (sourceIndex < 0 || pixels.Length <= sourceIndex)
                        {
                            sourcePixel = Color.clear;
                        }
                        else
                        {
                            sourcePixel = pixels[sourceIndex];
                        }
                        croppedPixels[destYOffset + x] = sourcePixel;
                    }
                }
            );

            cropped.SetPixels32(croppedPixels);
            cropped.Apply();

            string newPath = Path.Combine(
                assetDirectory,
                CroppedPrefix + Path.GetFileName(assetPath)
            );
            File.WriteAllBytes(newPath, cropped.EncodeToPNG());
            DestroyImmediate(cropped);
            AssetDatabase.ImportAsset(newPath);
            TextureImporter newImporter = AssetImporter.GetAtPath(newPath) as TextureImporter;
            if (newImporter == null)
            {
                return null;
            }

            TextureImporterSettings newSettings = new();
            importer.ReadTextureSettings(newSettings);
            Vector2 origPivot = GetSpritePivot(importer);
            Vector2 origCenter = new(width * origPivot.x, height * origPivot.y);
            Vector2 newPivotPixels = origCenter - new Vector2(minX, minY);
            Vector2 newPivotNorm = new(
                cropWidth > 0 ? newPivotPixels.x / cropWidth : 0.5f,
                cropHeight > 0 ? newPivotPixels.y / cropHeight : 0.5f
            );

            if (!hasVisible)
            {
                newPivotNorm = new Vector2(0.5f, 0.5f);
            }

            newSettings.spritePivot = newPivotNorm;
            newSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            newImporter.SetTextureSettings(newSettings);
            newImporter.spriteImportMode = SpriteImportMode.Single;
            newImporter.spritePivot = newPivotNorm;
            newImporter.textureType = importer.textureType;
            newImporter.spriteImportMode = importer.spriteImportMode;
            newImporter.filterMode = importer.filterMode;
            newImporter.textureCompression = importer.textureCompression;
            newImporter.wrapMode = importer.wrapMode;
            newImporter.mipmapEnabled = importer.mipmapEnabled;
            newImporter.spritePixelsPerUnit = importer.spritePixelsPerUnit;
            newImporter.isReadable = true;
            newImporter.SaveAndReimport();

            return newImporter;
        }

        private static Vector2 GetSpritePivot(TextureImporter importer)
        {
            if (importer.spriteImportMode == SpriteImportMode.Single)
            {
                return importer.spritePivot;
            }

            return new Vector2(0.5f, 0.5f);
        }
    }
#endif
}

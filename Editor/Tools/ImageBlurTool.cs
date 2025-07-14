﻿namespace WallstopStudios.UnityHelpers.Editor.Tools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.CustomEditors;
    using Object = UnityEngine.Object;

    public sealed class ImageBlurTool : EditorWindow
    {
        public List<Object> imageSources = new();

        private readonly List<Texture2D> _orderedTextures = new();
        private readonly List<Texture2D> _manualTextures = new();

        private int _blurRadius = 1;
        private Vector2 _scrollPosition;

        private GUIStyle _impactButtonStyle;
        private SerializedObject _serializedObject;
        private SerializedProperty _imageSourcesProperty;

        private readonly List<Object> _lastSeenImageSources = new();

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Image Blur")]
        public static void ShowWindow()
        {
            GetWindow<ImageBlurTool>("Image Blur Tool");
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _imageSourcesProperty = _serializedObject.FindProperty(nameof(imageSources));
        }

        private void OnGUI()
        {
            _serializedObject.Update();

            _impactButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = Color.yellow },
                fontStyle = FontStyle.Bold,
            };

            EditorGUILayout.LabelField("Image Blur Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Manual Folder Selection", EditorStyles.boldLabel);
            PersistentDirectoryGUI.PathSelectorObjectArray(
                _imageSourcesProperty,
                nameof(ImageBlurTool)
            );

            if (
                _serializedObject.ApplyModifiedProperties()
                || !_lastSeenImageSources.SequenceEqual(imageSources)
            )
            {
                _lastSeenImageSources.Clear();
                _lastSeenImageSources.AddRange(imageSources);
                _manualTextures.Clear();
                foreach (Object directory in imageSources.Where(Objects.NotNull))
                {
                    string path = AssetDatabase.GetAssetPath(directory);
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    TrySyncDirectory(path, _manualTextures);
                }
            }

            Event evt = Event.current;
            Rect dropArea = GUILayoutUtility.GetRect(0f, 75f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop Images/Folders Here");

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                {
                    if (!dropArea.Contains(evt.mousePosition))
                    {
                        return;
                    }

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            if (draggedObject == null)
                            {
                                continue;
                            }

                            string path = AssetDatabase.GetAssetPath(draggedObject);
                            if (string.IsNullOrWhiteSpace(path))
                            {
                                continue;
                            }

                            if (AssetDatabase.IsValidFolder(path))
                            {
                                TrySyncDirectory(path, _orderedTextures);
                            }
                            else if (
                                draggedObject is Texture2D texture
                                && !_orderedTextures.Contains(texture)
                            )
                            {
                                _orderedTextures.Add(texture);
                            }
                        }
                    }

                    break;
                }
            }

            EditorGUILayout.Space();

            if (_orderedTextures.Count > 0 || _manualTextures.Count > 0)
            {
                EditorGUILayout.LabelField("Selected Images:", EditorStyles.boldLabel);
                _scrollPosition = EditorGUILayout.BeginScrollView(
                    _scrollPosition,
                    GUILayout.Height(200)
                );
                foreach (Texture2D tex in _manualTextures.Concat(_orderedTextures).Distinct())
                {
                    EditorGUILayout.ObjectField(tex.name, tex, typeof(Texture2D), false);
                }
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Clear Selection", _impactButtonStyle))
                {
                    _orderedTextures.Clear();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Drag images or folders into the area above to select them for blurring.",
                    MessageType.Info
                );
            }

            EditorGUILayout.Space();
            _blurRadius = EditorGUILayout.IntSlider("Blur Radius", _blurRadius, 1, 200);
            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Blur", _impactButtonStyle))
            {
                ApplyBlurToSelectedTextures();
            }
        }

        private static void TrySyncDirectory(string directory, List<Texture2D> output)
        {
            if (!AssetDatabase.IsValidFolder(directory))
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { directory });
            foreach (string guid in guids)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    AssetDatabase.GUIDToAssetPath(guid)
                );
                if (texture != null && !output.Contains(texture))
                {
                    output.Add(texture);
                }
            }
        }

        private void ApplyBlurToSelectedTextures()
        {
            int processedCount = 0;
            Texture2D[] toProcess = _manualTextures.Concat(_orderedTextures).Distinct().ToArray();
            foreach (Texture2D originalTexture in toProcess)
            {
                string assetPath = AssetDatabase.GetAssetPath(originalTexture);
                EditorUtility.DisplayProgressBar(
                    "Applying Blur",
                    $"Processing {originalTexture.name}...",
                    (float)processedCount / toProcess.Length
                );
                try
                {
                    TextureImporter importer =
                        AssetImporter.GetAtPath(assetPath) as TextureImporter;

                    bool importSettingsChanged = false;

                    if (importer != null)
                    {
                        if (!importer.isReadable)
                        {
                            importer.isReadable = true;
                            importSettingsChanged = true;
                        }

                        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                        {
                            importer.textureCompression = TextureImporterCompression.Uncompressed;
                            importSettingsChanged = true;
                        }

                        if (importSettingsChanged)
                        {
                            importer.SaveAndReimport();
                        }
                    }

                    Texture2D currentTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

                    if (currentTexture == null || !currentTexture.isReadable)
                    {
                        this.LogError(
                            $"Texture is null or could not be made readable: {assetPath}. Please check 'Read/Write Enabled' in its import settings if the issue persists. Skipping."
                        );
                        processedCount++;
                        continue;
                    }

                    Texture2D blurredTexture = CreateBlurredTexture(currentTexture, _blurRadius);

                    if (blurredTexture != null)
                    {
                        string directory = Path.GetDirectoryName(assetPath);
                        if (string.IsNullOrWhiteSpace(directory))
                        {
                            continue;
                        }

                        string fileName = Path.GetFileNameWithoutExtension(assetPath);
                        string extension = Path.GetExtension(assetPath);
                        string newPathBase = Path.Combine(
                            directory,
                            $"{fileName}_blurred_{_blurRadius}"
                        );

                        string finalPath = newPathBase + extension;
                        int counter = 0;
                        while (File.Exists(finalPath))
                        {
                            counter++;
                            finalPath = $"{newPathBase}_{counter}{extension}";
                        }

                        byte[] bytes;
                        if (
                            string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            bytes = blurredTexture.EncodeToJPG(100);
                        }
                        else
                        {
                            finalPath = Path.ChangeExtension(finalPath, ".png");
                            bytes = blurredTexture.EncodeToPNG();
                        }

                        if (bytes != null)
                        {
                            File.WriteAllBytes(finalPath, bytes);
                            AssetDatabase.Refresh();
                            this.Log($"Saved blurred image to: {finalPath}");
                        }
                        else
                        {
                            this.LogError($"Failed to encode texture: {currentTexture.name}.");
                        }
                    }
                    else
                    {
                        this.LogError(
                            $"Failed to create blurred texture for: {originalTexture.name}."
                        );
                    }

                    processedCount++;
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog(
                        "Blur Operation Complete",
                        $"Successfully blurred {processedCount} images.",
                        "OK"
                    );
                }
            }
        }

        private static Texture2D CreateBlurredTexture(Texture2D original, int radius)
        {
            Texture2D blurred = new(original.width, original.height, original.format, false);
            Color[] pixels = original.GetPixels();
            Color[] blurredPixels = new Color[pixels.Length];
            int width = original.width;
            int height = original.height;

            // A temporary buffer for the first pass
            Color[] tempPixels = new Color[pixels.Length];

            // Generate the kernel for the weighted average
            float[] kernel = GenerateGaussianKernel(radius);

            // --- Horizontal Pass ---
            Parallel.For(
                0,
                height,
                y =>
                {
                    int yOffset = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        Color weightedSum = Color.clear;
                        float weightTotal = 0f;

                        for (int k = -radius; k <= radius; k++)
                        {
                            int currentX = x + k;
                            if (currentX >= 0 && currentX < width)
                            {
                                float weight = kernel[k + radius];
                                weightedSum += pixels[yOffset + currentX] * weight;
                                weightTotal += weight;
                            }
                        }
                        tempPixels[yOffset + x] = weightedSum / weightTotal;
                    }
                }
            );

            // --- Vertical Pass ---
            Parallel.For(
                0,
                width,
                x =>
                {
                    for (int y = 0; y < height; y++)
                    {
                        Color weightedSum = Color.clear;
                        float weightTotal = 0f;

                        for (int k = -radius; k <= radius; k++)
                        {
                            int currentY = y + k;
                            if (currentY >= 0 && currentY < height)
                            {
                                float weight = kernel[k + radius];
                                weightedSum += tempPixels[currentY * width + x] * weight;
                                weightTotal += weight;
                            }
                        }
                        blurredPixels[y * width + x] = weightedSum / weightTotal;
                    }
                }
            );

            blurred.SetPixels(blurredPixels);
            blurred.Apply();
            return blurred;
        }

        private static float[] GenerateGaussianKernel(int radius)
        {
            int size = radius * 2 + 1;
            float[] kernel = new float[size];
            float sigma = radius / 3.0f; // A good rule of thumb for sigma
            float twoSigmaSquare = 2.0f * sigma * sigma;
            float sum = 0f;

            for (int i = 0; i < size; i++)
            {
                int distance = i - radius;
                kernel[i] =
                    Mathf.Exp(-(distance * distance) / twoSigmaSquare)
                    / (Mathf.Sqrt(Mathf.PI * twoSigmaSquare));
                sum += kernel[i];
            }

            // Normalize the kernel so that the weights sum to 1
            for (int i = 0; i < size; i++)
            {
                kernel[i] /= sum;
            }

            return kernel;
        }
    }
}

namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Extension;
    using UnityEditor;
    using UnityEngine;
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
        private Object[] _inputDirectories;

        [SerializeField]
        private bool _onlyNecessary;

        private List<string> _filesToProcess;

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/" + Name)]
        private static void ShowWindow() => GetWindow<SpriteCropper>(Name);

        private void OnGUI()
        {
            GUILayout.Label("Drag folders below", EditorStyles.boldLabel);
            SerializedObject so = new(this);
            SerializedProperty dirs = so.FindProperty(nameof(_inputDirectories));
            EditorGUILayout.PropertyField(dirs, true);
            SerializedProperty onlyNecessary = so.FindProperty(nameof(_onlyNecessary));
            EditorGUILayout.PropertyField(onlyNecessary, true);
            so.ApplyModifiedProperties();

            if (GUILayout.Button("Find Sprites To Process"))
            {
                FindFilesToProcess();
            }

            if (_filesToProcess != null && _filesToProcess.Count > 0)
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
            if (_inputDirectories == null || _inputDirectories.Length == 0)
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
                    _filesToProcess.Add(file);
                }
            }
            Repaint();
        }

        private void ProcessFoundSprites()
        {
            if (_filesToProcess == null || _filesToProcess.Count == 0)
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

                    foreach (TextureImporter newImporter in newImporters)
                    {
                        newImporter.SaveAndReimport();
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
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
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
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
            if (importer == null || !importer.textureType.Equals(TextureImporterType.Sprite))
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

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null || !importer.textureType.Equals(TextureImporterType.Sprite))
            {
                return null;
            }

            TextureImporterSettings originalSettings = new();
            importer.ReadTextureSettings(originalSettings);

            bool originalReadableState = importer.isReadable;
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
                return null;

            TextureImporter resultImporter;
            try
            {
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

                if (!hasVisible)
                {
                    return null;
                }

                int cropWidth = maxX - minX + 1;
                int cropHeight = maxY - minY + 1;

                if (_onlyNecessary && cropWidth == width && cropHeight == height)
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
                            croppedPixels[destYOffset + x] = pixels[sourceYOffset + x + minX];
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

                newImporter.textureType = importer.textureType;
                newImporter.spriteImportMode = importer.spriteImportMode;
                newImporter.filterMode = importer.filterMode;
                newImporter.textureCompression = importer.textureCompression;
                newImporter.wrapMode = importer.wrapMode;
                newImporter.mipmapEnabled = importer.mipmapEnabled;
                newImporter.spritePixelsPerUnit = importer.spritePixelsPerUnit;

                TextureImporterSettings newSettings = new();
                newImporter.ReadTextureSettings(newSettings);
                newSettings.spriteExtrude = originalSettings.spriteExtrude;

                Vector2 origPivot = GetSpritePivot(importer);
                Vector2 origCenter = new(width * origPivot.x, height * origPivot.y);
                Vector2 newPivotPixels = origCenter - new Vector2(minX, minY);
                Vector2 newPivotNorm = new(
                    cropWidth > 0 ? newPivotPixels.x / cropWidth : 0.5f,
                    cropHeight > 0 ? newPivotPixels.y / cropHeight : 0.5f
                );

                newImporter.spriteImportMode = SpriteImportMode.Single;
                newImporter.spritePivot = newPivotNorm;
                newSettings.spritePivot = newPivotNorm;
                newSettings.spriteAlignment = (int)SpriteAlignment.Custom;

                newImporter.SetTextureSettings(newSettings);
                newImporter.isReadable = false;

                resultImporter = newImporter;
            }
            finally
            {
                if (importer != null && importer.isReadable != originalReadableState)
                {
                    importer.isReadable = originalReadableState;
                    importer.SaveAndReimport();
                }
            }

            return resultImporter;
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

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
    using CustomEditors;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Utils;
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

        private enum OutputReadability
        {
            MirrorSource = 0,
            Readable = 1,
            NotReadable = 2,
        }

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

        [SerializeField]
        private bool _overwriteOriginals;

        [SerializeField]
        private Object _outputDirectory;

        [SerializeField]
        private OutputReadability _outputReadability = OutputReadability.MirrorSource;

        [SerializeField]
        private bool _copyDefaultPlatformSettings = true;

        private List<string> _filesToProcess;
        private SerializedObject _serializedObject;
        private SerializedProperty _inputDirectoriesProperty;
        private SerializedProperty _onlyNecessaryProperty;
        private SerializedProperty _leftPaddingProperty;
        private SerializedProperty _rightPaddingProperty;
        private SerializedProperty _topPaddingProperty;
        private SerializedProperty _bottomPaddingProperty;
        private SerializedProperty _spriteNameRegexProperty;
        private SerializedProperty _overwriteOriginalsProperty;
        private SerializedProperty _outputDirectoryProperty;
        private SerializedProperty _outputReadabilityProperty;
        private SerializedProperty _copyDefaultPlatformSettingsProperty;

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
            _overwriteOriginalsProperty = _serializedObject.FindProperty(
                nameof(_overwriteOriginals)
            );
            _outputDirectoryProperty = _serializedObject.FindProperty(nameof(_outputDirectory));
            _outputReadabilityProperty = _serializedObject.FindProperty(nameof(_outputReadability));
            _copyDefaultPlatformSettingsProperty = _serializedObject.FindProperty(
                nameof(_copyDefaultPlatformSettings)
            );
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
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _overwriteOriginalsProperty,
                new GUIContent("Overwrite Originals")
            );
            using (new EditorGUI.DisabledScope(_overwriteOriginals))
            {
                EditorGUILayout.PropertyField(
                    _outputDirectoryProperty,
                    new GUIContent("Output Directory (optional)")
                );
            }
            EditorGUILayout.PropertyField(
                _outputReadabilityProperty,
                new GUIContent("Output Readability")
            );
            EditorGUILayout.PropertyField(
                _copyDefaultPlatformSettingsProperty,
                new GUIContent("Copy Default Platform Settings")
            );
            _serializedObject.ApplyModifiedProperties();

            // Clamp paddings to non-negative
            _leftPadding = Mathf.Max(0, _leftPadding);
            _rightPadding = Mathf.Max(0, _rightPadding);
            _topPadding = Mathf.Max(0, _topPadding);
            _bottomPadding = Mathf.Max(0, _bottomPadding);

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

            HashSet<string> processedFiles = new(StringComparer.OrdinalIgnoreCase);
            List<string> needReprocessing = new();
            Dictionary<string, bool> originalReadable = new(StringComparer.OrdinalIgnoreCase);
            string lastProcessed = null;
            bool canceled = false;
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
                        if (
                            EditorUtility.DisplayCancelableProgressBar(
                                Name,
                                $"Pre-processing {i + 1}/{total}: {Path.GetFileName(file)}",
                                i / (float)total
                            )
                        )
                        {
                            canceled = true;
                            break;
                        }
                        CheckPreProcessNeeded(file, originalReadable);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                if (!canceled)
                {
                    AssetDatabase.StartAssetEditing();
                    try
                    {
                        for (int i = 0; i < _filesToProcess.Count; ++i)
                        {
                            string file = _filesToProcess[i];
                            if (!processedFiles.Add(file))
                            {
                                continue;
                            }
                            lastProcessed = file;
                            if (
                                EditorUtility.DisplayCancelableProgressBar(
                                    Name,
                                    $"Processing {i + 1}/{total}: {Path.GetFileName(file)}",
                                    i / (float)total
                                )
                            )
                            {
                                canceled = true;
                                break;
                            }

                            ProcessOutcome outcome;
                            TextureImporter newImporter = ProcessSprite(
                                file,
                                out outcome,
                                originalReadable
                            );
                            switch (outcome)
                            {
                                case ProcessOutcome.Success:
                                    if (newImporter != null)
                                    {
                                        newImporters.Add(newImporter);
                                    }
                                    break;
                                case ProcessOutcome.SkippedNoChange:
                                    // No-op
                                    break;
                                case ProcessOutcome.RetryableError:
                                    needReprocessing.Add(file);
                                    break;
                                case ProcessOutcome.FatalError:
                                    // Log already handled inside ProcessSprite; skip
                                    break;
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
                }

                if (!canceled && needReprocessing.Any())
                {
                    newImporters.Clear();
                    AssetDatabase.StartAssetEditing();
                    try
                    {
                        foreach (string file in needReprocessing)
                        {
                            ProcessOutcome outcome;
                            TextureImporter newImporter = ProcessSprite(
                                file,
                                out outcome,
                                originalReadable
                            );
                            if (outcome == ProcessOutcome.Success && newImporter != null)
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
                }

                // Restore readability to originals that we changed
                if (originalReadable.Count > 0 && !_overwriteOriginals)
                {
                    AssetDatabase.StartAssetEditing();
                    try
                    {
                        foreach ((string path, bool wasReadable) in originalReadable)
                        {
                            try
                            {
                                if (
                                    AssetImporter.GetAtPath(path) is TextureImporter
                                    {
                                        textureType: TextureImporterType.Sprite
                                    } srcImporter
                                )
                                {
                                    if (srcImporter.isReadable != wasReadable)
                                    {
                                        srcImporter.isReadable = wasReadable;
                                        srcImporter.SaveAndReimport();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                this.LogError($"Failed to restore readability for '{path}'.", ex);
                            }
                        }
                    }
                    finally
                    {
                        AssetDatabase.StopAssetEditing();
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }

                if (canceled)
                {
                    this.LogWarn("Sprite cropping canceled by user.");
                }
                else
                {
                    this.Log(
                        $"{newImporters.Count} sprites processed successfully. Skipped: {_filesToProcess.Count - needReprocessing.Count - newImporters.Count}"
                    );
                }
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

        private static void CheckPreProcessNeeded(
            string assetPath,
            Dictionary<string, bool> originalReadable
        )
        {
            string assetDirectory = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrWhiteSpace(assetDirectory))
            {
                return;
            }

            if (
                AssetImporter.GetAtPath(assetPath)
                is not TextureImporter { textureType: TextureImporterType.Sprite } importer
            )
            {
                return;
            }

            // Make readable if needed and remember original state to restore after processing
            if (!importer.isReadable)
            {
                if (!originalReadable.ContainsKey(assetPath))
                {
                    originalReadable[assetPath] = false;
                }
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
            else if (!originalReadable.ContainsKey(assetPath))
            {
                originalReadable[assetPath] = true;
            }
        }

        private enum ProcessOutcome
        {
            Success,
            SkippedNoChange,
            RetryableError,
            FatalError,
        }

        private TextureImporter ProcessSprite(
            string assetPath,
            out ProcessOutcome outcome,
            Dictionary<string, bool> originalReadable
        )
        {
            outcome = ProcessOutcome.FatalError;
            string assetDirectory = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrWhiteSpace(assetDirectory))
            {
                outcome = ProcessOutcome.FatalError;
                return null;
            }

            if (
                AssetImporter.GetAtPath(assetPath)
                is not TextureImporter { textureType: TextureImporterType.Sprite } importer
            )
            {
                outcome = ProcessOutcome.FatalError;
                return null;
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
            {
                outcome = ProcessOutcome.RetryableError;
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
            byte alphaByteThreshold = (byte)
                Mathf.Clamp(Mathf.RoundToInt(AlphaThreshold * 255f), 0, 255);
            Parallel.For(
                0,
                width * height,
                () => (minX: width, minY: height, maxX: 0, maxY: 0, hasVisible: false),
                (index, _, localState) =>
                {
                    int x = index % width;
                    int y = index / width;

                    byte a = pixels[index].a;
                    if (a > alphaByteThreshold)
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

            int origMinX = minX;
            int origMinY = minY;
            int origMaxX = maxX;
            int origMaxY = maxY;

            int visibleMinX = minX;
            int visibleMinY = minY;
            int visibleMaxX = maxX;
            int visibleMaxY = maxY;

            if (hasVisible)
            {
                visibleMinX -= _leftPadding;
                visibleMinY -= _bottomPadding;
                visibleMaxX += _rightPadding;
                visibleMaxY += _topPadding;
            }
            else
            {
                visibleMinX = visibleMinY = 0;
                visibleMaxX = visibleMaxY = 0;
            }

            int cropWidth = visibleMaxX - visibleMinX + 1;
            int cropHeight = visibleMaxY - visibleMinY + 1;

            if (_onlyNecessary && (!hasVisible || (cropWidth == width && cropHeight == height)))
            {
                outcome = ProcessOutcome.SkippedNoChange;
                return null;
            }

            Texture2D cropped = new(cropWidth, cropHeight, TextureFormat.RGBA32, false);
            using PooledResource<Color32[]> pooledCropped = WallstopFastArrayPool<Color32>.Get(
                cropWidth * cropHeight,
                out Color32[] croppedPixels
            );

            int srcX0 = Mathf.Max(visibleMinX, 0);
            int srcY0 = Mathf.Max(visibleMinY, 0);
            int srcX1 = Mathf.Min(visibleMaxX, width - 1);
            int srcY1 = Mathf.Min(visibleMaxY, height - 1);

            Parallel.For(
                0,
                cropHeight,
                y =>
                {
                    int destRow = y * cropWidth;
                    int srcY = visibleMinY + y;
                    if (srcY < 0 || srcY >= height || srcY < srcY0 || srcY > srcY1)
                    {
                        Array.Clear(croppedPixels, destRow, cropWidth);
                        return;
                    }

                    int copyStartDestX = Mathf.Max(0, srcX0 - visibleMinX);
                    int copyEndDestX = Mathf.Min(cropWidth - 1, srcX1 - visibleMinX);

                    int leftClear = copyStartDestX;
                    int rightClear = cropWidth - 1 - copyEndDestX;

                    if (leftClear > 0)
                    {
                        Array.Clear(croppedPixels, destRow, leftClear);
                    }

                    if (copyEndDestX >= copyStartDestX)
                    {
                        int numToCopy = copyEndDestX - copyStartDestX + 1;
                        int srcStartX = srcX0;
                        int srcIndex = srcY * width + srcStartX;
                        int destIndex = destRow + copyStartDestX;
                        Array.Copy(pixels, srcIndex, croppedPixels, destIndex, numToCopy);
                    }

                    if (rightClear > 0)
                    {
                        Array.Clear(croppedPixels, destRow + (cropWidth - rightClear), rightClear);
                    }
                }
            );

            cropped.SetPixels32(croppedPixels);
            cropped.Apply();

            // Determine output path and importer to modify
            string outputDirectory = assetDirectory;
            if (!_overwriteOriginals && _outputDirectory != null)
            {
                string dirPath = AssetDatabase.GetAssetPath(_outputDirectory);
                if (!string.IsNullOrWhiteSpace(dirPath) && AssetDatabase.IsValidFolder(dirPath))
                {
                    outputDirectory = dirPath;
                }
            }

            string outputFileName = _overwriteOriginals
                ? Path.GetFileName(assetPath)
                : (CroppedPrefix + Path.GetFileName(assetPath));
            string newPath = Path.Combine(outputDirectory, outputFileName);

            byte[] pngBytes = cropped.EncodeToPNG();
            File.WriteAllBytes(newPath, pngBytes);
            DestroyImmediate(cropped);
            AssetDatabase.ImportAsset(newPath);

            TextureImporter newImporter = AssetImporter.GetAtPath(newPath) as TextureImporter;
            if (newImporter == null)
            {
                outcome = ProcessOutcome.RetryableError;
                return null;
            }

            TextureImporterSettings newSettings = new();
            importer.ReadTextureSettings(newSettings);
            Vector2 origPivot = GetSpritePivot(importer);
            Vector2 origCenter = new(width * origPivot.x, height * origPivot.y);
            Vector2 newPivotPixels = origCenter - new Vector2(visibleMinX, visibleMinY);
            Vector2 newPivotNorm = new(
                cropWidth > 0 ? newPivotPixels.x / cropWidth : 0.5f,
                cropHeight > 0 ? newPivotPixels.y / cropHeight : 0.5f
            );

            if (!hasVisible)
            {
                newPivotNorm = new Vector2(0.5f, 0.5f);
            }

            // Adjust 9-slice borders based on trimming from edges
            Vector4 border = newSettings.spriteBorder;
            int deltaLeft = visibleMinX; // pixels trimmed from left of full image
            int deltaBottom = visibleMinY; // trimmed from bottom
            int deltaRight = (width - 1) - visibleMaxX; // trimmed from right
            int deltaTop = (height - 1) - visibleMaxY; // trimmed from top
            border.x = Mathf.Max(0, border.x - deltaLeft);
            border.y = Mathf.Max(0, border.y - deltaBottom);
            border.z = Mathf.Max(0, border.z - deltaRight);
            border.w = Mathf.Max(0, border.w - deltaTop);

            newSettings.spritePivot = newPivotNorm;
            newSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            newSettings.spriteBorder = border;
            newImporter.SetTextureSettings(newSettings);
            // Always import the cropped output as Single unless we implement full metadata migration
            newImporter.spriteImportMode = SpriteImportMode.Single;
            newImporter.spritePivot = newPivotNorm;
            newImporter.textureType = importer.textureType;
            newImporter.filterMode = importer.filterMode;
            newImporter.textureCompression = importer.textureCompression;
            newImporter.wrapMode = importer.wrapMode;
            newImporter.mipmapEnabled = importer.mipmapEnabled;
            newImporter.spritePixelsPerUnit = importer.spritePixelsPerUnit;

            // Copy default platform settings if requested
            if (_copyDefaultPlatformSettings)
            {
                try
                {
                    TextureImporterPlatformSettings srcDefault =
                        importer.GetDefaultPlatformTextureSettings();
                    if (!string.IsNullOrWhiteSpace(srcDefault.name))
                    {
                        newImporter.SetPlatformTextureSettings(srcDefault);
                    }
                }
                catch (Exception ex)
                {
                    this.LogWarn(
                        $"Failed to copy default platform settings for '{assetPath}'.",
                        ex
                    );
                }
            }

            // Set readability based on option
            bool srcOriginalReadable = originalReadable.TryGetValue(assetPath, out bool wasReadable)
                ? wasReadable
                : importer.isReadable;
            switch (_outputReadability)
            {
                case OutputReadability.MirrorSource:
                    newImporter.isReadable = srcOriginalReadable;
                    break;
                case OutputReadability.Readable:
                    newImporter.isReadable = true;
                    break;
                case OutputReadability.NotReadable:
                    newImporter.isReadable = false;
                    break;
            }
            newImporter.SaveAndReimport();

            outcome = ProcessOutcome.Success;
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

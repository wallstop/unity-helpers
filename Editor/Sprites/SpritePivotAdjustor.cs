namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Extension;
    using CustomEditors;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public class SpritePivotAdjuster : EditorWindow
    {
        private const float AlphaCutoff = 0.01f;

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

        [SerializeField]
        private List<Object> _directoryPaths = new();

        [SerializeField]
        private string _spriteNameRegex = ".*";

        private SerializedObject _serializedObject;
        private SerializedProperty _directoryPathsProperty;
        private SerializedProperty _spriteNameRegexProperty;

        private List<string> _filesToProcess;
        private Regex _regex;

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Sprite Pivot Adjuster")]
        public static void ShowWindow()
        {
            GetWindow<SpritePivotAdjuster>("Sprite Pivot Adjuster");
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _directoryPathsProperty = _serializedObject.FindProperty(nameof(_directoryPaths));
            _spriteNameRegexProperty = _serializedObject.FindProperty(nameof(_spriteNameRegex));
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Input directories", EditorStyles.boldLabel);
            _serializedObject.Update();
            PersistentDirectoryGUI.PathSelectorObjectArray(
                _directoryPathsProperty,
                nameof(SpriteCropper)
            );

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
                if (GUILayout.Button("Adjust Pivots in Directory"))
                {
                    AdjustPivotsInDirectory();
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
            if (_directoryPaths is not { Count: > 0 })
            {
                this.LogWarn($"No input directories selected.");
                return;
            }

            foreach (Object maybeDirectory in _directoryPaths.Where(d => d != null))
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

        private void AdjustPivotsInDirectory()
        {
            HashSet<string> processedFiles = new(StringComparer.OrdinalIgnoreCase);
            List<TextureImporter> importers = new();
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < _filesToProcess.Count; i++)
                {
                    string assetPath = _filesToProcess[i];
                    if (!processedFiles.Add(assetPath))
                    {
                        continue;
                    }
                    if (
                        AssetImporter.GetAtPath(assetPath)
                        is not TextureImporter { textureType: TextureImporterType.Sprite } importer
                    )
                    {
                        return;
                    }

                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

                    if (sprite == null)
                    {
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(
                        "Processing sprites",
                        $"Processing {sprite.name}",
                        (float)i / _filesToProcess.Count
                    );

                    if (importer.spriteImportMode == SpriteImportMode.Single)
                    {
                        TextureImporterSettings settings = new();
                        importer.ReadTextureSettings(settings);

                        Vector2 newPivot = CalculateCenterOfMassPivot(sprite);
                        settings.spritePivot = newPivot;
                        settings.spriteAlignment = (int)SpriteAlignment.Custom;
                        importer.SetTextureSettings(settings);

                        importer.spritePivot = newPivot;
                        importer.SaveAndReimport();
                        importers.Add(importer);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                foreach (TextureImporter importer in importers)
                {
                    importer.SaveAndReimport();
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static Vector2 CalculateCenterOfMassPivot(Sprite sprite)
        {
            Texture2D texture = sprite.texture;
            Rect spriteRect = sprite.rect;
            int startX = Mathf.FloorToInt(spriteRect.x);
            int startY = Mathf.FloorToInt(spriteRect.y);
            int width = Mathf.FloorToInt(spriteRect.width);
            int height = Mathf.FloorToInt(spriteRect.height);

            long totalX = 0;
            long totalY = 0;
            long pixelCount = 0;

            Color[] pixels = texture.GetPixels(startX, startY, width, height);

            Parallel.For(
                0,
                height,
                () => (sumX: 0L, sumY: 0L, count: 0L),
                (y, _, local) =>
                {
                    int rowOffset = y * width;
                    for (int x = 0; x < width; ++x)
                    {
                        if (pixels[rowOffset + x].a > AlphaCutoff)
                        {
                            local.sumX += x;
                            local.sumY += y;
                            local.count++;
                        }
                    }
                    return local;
                },
                local =>
                {
                    Interlocked.Add(ref totalX, local.sumX);
                    Interlocked.Add(ref totalY, local.sumY);
                    Interlocked.Add(ref pixelCount, local.count);
                }
            );

            if (pixelCount == 0L)
            {
                return new Vector2(0.5f, 0.5f);
            }

            double averageX = (double)totalX / pixelCount;
            double averageY = (double)totalY / pixelCount;

            double pivotX = averageX / width;
            double pivotY = averageY / height;

            return new Vector2((float)pivotX, (float)pivotY);
        }
    }
#endif
}

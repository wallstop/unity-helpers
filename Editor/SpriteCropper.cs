namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Helper;
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

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/" + Name)]
        private static void ShowWindow() => GetWindow<SpriteCropper>(Name);

        private void OnGUI()
        {
            GUILayout.Label("Drag folders below", EditorStyles.boldLabel);
            SerializedObject so = new(this);
            SerializedProperty dirs = so.FindProperty(nameof(_inputDirectories));
            EditorGUILayout.PropertyField(dirs, true);
            so.ApplyModifiedProperties();

            if (GUILayout.Button("Process Sprites"))
            {
                List<string> allFiles = new();
                foreach (Object maybeDirectory in _inputDirectories.Where(Objects.NotNull))
                {
                    string assetPath = AssetDatabase.GetAssetPath(maybeDirectory);
                    if (!AssetDatabase.IsValidFolder(assetPath))
                    {
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

                        allFiles.Add(file);
                    }
                }

                AssetDatabase.StartAssetEditing();
                try
                {
                    int total = allFiles.Count;
                    List<TextureImporter> newImporters = new();
                    for (int i = 0; i < allFiles.Count; ++i)
                    {
                        string file = allFiles[i];
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
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        private static TextureImporter ProcessSprite(string assetPath)
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

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            Color32[] pixels = tex.GetPixels32();

            int width = tex.width;
            int height = tex.height;
            int minX = width;
            int minY = height;
            int maxX = 0;
            int maxY = 0;
            bool hasVisible = false;

            Parallel.For(
                0,
                width * height,
                index =>
                {
                    int x = index % width;
                    int y = index / width;

                    float a = pixels[index].a / 255f;
                    if (a <= AlphaThreshold)
                    {
                        return;
                    }

                    hasVisible = true;
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            );

            if (!hasVisible)
            {
                return null;
            }

            int cropWidth = maxX - minX + 1;
            int cropHeight = maxY - minY + 1;
            Texture2D cropped = new(cropWidth, cropHeight, TextureFormat.RGBA32, false);

            Color32[] croppedPixels = new Color32[cropWidth * cropHeight];
            Parallel.For(
                0,
                croppedPixels.Length,
                index =>
                {
                    int x = index % cropWidth;
                    int y = index / cropWidth;
                    int outputX = x + minX;
                    int outputY = y + minY;
                    croppedPixels[index] = pixels[outputY * width + outputX];
                }
            );
            cropped.SetPixels32(croppedPixels);
            cropped.Apply();

            string newPath = Path.Combine(
                assetDirectory,
                CroppedPrefix + Path.GetFileName(assetPath)
            );
            File.WriteAllBytes(newPath, cropped.EncodeToPNG());
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
            Vector2 origCenter = new(tex.width * origPivot.x, tex.height * origPivot.y);
            Vector2 newPivotPixels = origCenter - new Vector2(minX, minY);
            Vector2 newPivotNorm = new(newPivotPixels.x / cropWidth, newPivotPixels.y / cropHeight);

            newImporter.spriteImportMode = SpriteImportMode.Single;
            newImporter.spritePivot = newPivotNorm;
            newSettings.spritePivot = newPivotNorm;
            newSettings.spriteAlignment = (int)SpriteAlignment.Custom;

            newImporter.SetTextureSettings(newSettings);
            newImporter.isReadable = true;

            return newImporter;
        }

        private static Vector2 GetSpritePivot(TextureImporter importer)
        {
            if (importer.spriteImportMode == SpriteImportMode.Single)
            {
                return importer.spritePivot;
            }
            throw new InvalidEnumArgumentException(
                nameof(importer.spriteImportMode),
                (int)importer.spriteImportMode,
                typeof(SpriteImportMode)
            );
        }
    }
#endif
}

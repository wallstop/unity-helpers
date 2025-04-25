namespace UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using Core.Extension;
    using Core.Helper;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Serialization;
    using UnityHelpers.Utils;
    using Utils;
    using Object = UnityEngine.Object;

    public sealed class TextureResizerWizard : ScriptableWizard
    {
        public enum ResizeAlgorithm
        {
            Bilinear,
            Point,
        }

        public List<Texture2D> textures = new();

        [FormerlySerializedAs("animationSources")]
        [Tooltip(
            "Drag a folder from Unity here to apply the configuration to all textures under it. No textures are modified if no directories are provided."
        )]
        public List<Object> textureSourcePaths = new();

        public int numResizes = 1;

        [Tooltip("Resize algorithm to use for scaling.")]
        public ResizeAlgorithm scalingResizeAlgorithm = ResizeAlgorithm.Bilinear;

        public int pixelsPerUnit = 100;
        public float widthMultiplier = 0.54f;
        public float heightMultiplier = 0.245f;

        [MenuItem("Tools/Unity Helpers/Texture Resizer")]
        public static void ResizeTextures()
        {
            _ = DisplayWizard<TextureResizerWizard>("Texture Resizer", "Resize");
        }

        private void OnWizardCreate()
        {
            textures ??= new List<Texture2D>();
            textureSourcePaths ??= new List<Object>();
            HashSet<string> animationPaths = new();
            foreach (Object animationSource in textureSourcePaths)
            {
                string assetPath = AssetDatabase.GetAssetPath(animationSource);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    _ = animationPaths.Add(assetPath);
                }
            }

            if (animationPaths.Any())
            {
                foreach (
                    string assetGuid in AssetDatabase.FindAssets(
                        "t:texture2D",
                        animationPaths.ToArray()
                    )
                )
                {
                    string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }

                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture != null)
                    {
                        textures.Add(texture);
                    }
                }
            }

            textures = textures.Distinct().OrderBy(texture => texture.name).ToList();
            if (textures.Count <= 0 || numResizes <= 0)
            {
                return;
            }

            for (int i = 0; i < numResizes; ++i)
            {
                foreach (Texture2D inputTexture in textures)
                {
                    Texture2D texture = inputTexture;
                    string assetPath = AssetDatabase.GetAssetPath(texture);
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        continue;
                    }

                    TextureImporter tImporter =
                        AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (tImporter == null)
                    {
                        continue;
                    }

                    bool isReadable = tImporter.isReadable;
                    if (!isReadable)
                    {
                        tImporter.isReadable = true;
                        tImporter.SaveAndReimport();
                        AssetDatabase.Refresh();
                        texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    }

                    Texture2D copy = Instantiate(texture);
                    try
                    {
                        int extraWidth = (int)
                            Math.Round(copy.width / (pixelsPerUnit * widthMultiplier));
                        int extraHeight = (int)
                            Math.Round(copy.height / (pixelsPerUnit * heightMultiplier));
                        if (extraWidth == 0 && extraHeight == 0)
                        {
                            continue;
                        }

                        switch (scalingResizeAlgorithm)
                        {
                            case ResizeAlgorithm.Bilinear:
                                TextureScale.Bilinear(
                                    copy,
                                    copy.width + extraWidth,
                                    copy.height + extraHeight
                                );
                                break;
                            case ResizeAlgorithm.Point:
                                TextureScale.Point(
                                    copy,
                                    copy.width + extraWidth,
                                    copy.height + extraHeight
                                );
                                break;
                            default:
                                throw new InvalidEnumArgumentException(
                                    nameof(scalingResizeAlgorithm),
                                    (int)scalingResizeAlgorithm,
                                    typeof(ResizeAlgorithm)
                                );
                        }

                        byte[] bytes = copy.EncodeToPNG();
                        File.WriteAllBytes(assetPath, bytes);
                        this.Log(
                            $"Resized {texture.name} from [{texture.width}x{texture.height}] to [{copy.width}x{copy.height}]"
                        );
                    }
                    catch (Exception e)
                    {
                        this.LogError($"Failed to resize {texture.name}.", e);
                    }
                    finally
                    {
                        copy.Destroy();
                    }
                }

                AssetDatabase.Refresh();
            }
        }
    }
#endif
}

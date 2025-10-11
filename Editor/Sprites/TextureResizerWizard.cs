namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Serialization;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Utils;
    using Object = UnityEngine.Object;

    /// <summary>
    /// ScriptableWizard to batch resize textures by a computed delta using a chosen algorithm.
    /// Useful for adjusting imported assets to target pixel density or scale without external tools.
    /// </summary>
    /// <remarks>
    /// <para>
    /// How it works: for each selected texture (or those discovered under provided directories), the
    /// tool ensures readability, clones the texture, computes a size increment from
    /// <c>pixelsPerUnit</c> and the width/height multipliers, resizes via bilinear or point, and
    /// writes the PNG back to the original asset path. It refreshes the AssetDatabase between
    /// passes for multi-iteration resizing.
    /// </para>
    /// <para>
    /// Pros: fast iteration inside Unity, supports multiple discovery paths, preserves import
    /// settings, and can be run multiple times (<c>numResizes</c>) for step changes.
    /// </para>
    /// <para>
    /// Caveats: overwrites files in-place; ensure version control. If textures are non-readable,
    /// importer is temporarily toggled which may dirties the asset. Consider backing up.
    /// </para>
    /// <example>
    /// <![CDATA[
    /// // Open from menu: Tools/Wallstop Studios/Unity Helpers/Texture Resizer
    /// // Typical settings for retro pixel art:
    /// //   scalingResizeAlgorithm = Point
    /// //   pixelsPerUnit = 100
    /// //   widthMultiplier = 1.0f, heightMultiplier = 1.0f (double by setting numResizes=1 and multipliers)
    /// ]]>
    /// </example>
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

        [Tooltip("If true, only simulates the operation without writing files.")]
        public bool dryRun;

        [Tooltip(
            "Optional output folder (Unity project relative). If set, resized PNGs are written here instead of overwriting originals."
        )]
        public DefaultAsset outputFolder;

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Texture Resizer")]
        public static void ResizeTextures()
        {
            _ = DisplayWizard<TextureResizerWizard>("Texture Resizer", "Resize");
        }

        internal void OnWizardCreate()
        {
            textures ??= new List<Texture2D>();
            textureSourcePaths ??= new List<Object>();

            // Discover textures from provided folders using a pooled set to avoid duplicates.
            using (Buffers<string>.HashSet.Get(out HashSet<string> sourcePaths))
            {
                foreach (Object pathObj in textureSourcePaths)
                {
                    string p = AssetDatabase.GetAssetPath(pathObj);
                    if (!string.IsNullOrEmpty(p))
                    {
                        _ = sourcePaths.Add(p);
                    }
                }

                if (sourcePaths.Count > 0)
                {
                    foreach (
                        string guid in AssetDatabase.FindAssets(
                            "t:texture2D",
                            sourcePaths.ToArray()
                        )
                    )
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
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
            }

            textures = textures.Where(t => t != null).Distinct().OrderBy(t => t.name).ToList();

            if (textures.Count <= 0 || numResizes <= 0)
            {
                return;
            }

            int processed = 0;
            int resized = 0;
            int skippedWrongExt = 0;
            int skippedZeroDelta = 0;
            int errors = 0;
            bool anyChanges = false;

            string outputDirAssetPath =
                outputFolder != null ? AssetDatabase.GetAssetPath(outputFolder) : null;

            // Batch edits for performance
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int idx = 0; idx < textures.Count; ++idx)
                {
                    Texture2D texture = textures[idx];
                    string assetPath = AssetDatabase.GetAssetPath(texture);
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        continue;
                    }

                    // Only process PNGs by default to avoid corrupting non-PNG assets.
                    if (
                        !string.Equals(
                            Path.GetExtension(assetPath),
                            ".png",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        ++skippedWrongExt;
                        continue;
                    }

                    // Progress/cancel UI
                    bool cancel =
                        WallstopStudios.UnityHelpers.Editor.Utils.EditorUi.CancelableProgress(
                            "Resizing Textures",
                            $"Processing {texture.name} ({idx + 1}/{textures.Count})",
                            (float)(idx + 1) / textures.Count
                        );
                    if (cancel)
                    {
                        break;
                    }

                    ++processed;

                    TextureImporter tImporter =
                        AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (tImporter == null)
                    {
                        continue;
                    }

                    bool originalReadable = tImporter.isReadable;
                    Texture2D working = texture;
                    try
                    {
                        if (!originalReadable)
                        {
                            // Temporarily end batch to guarantee reimport applies immediately
                            AssetDatabase.StopAssetEditing();
                            tImporter.isReadable = true;
                            tImporter.SaveAndReimport();
                            // Reload to ensure readability state reflects on the instance
                            working = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                            // Resume batch edits
                            AssetDatabase.StartAssetEditing();
                        }

                        int origW = working.width;
                        int origH = working.height;

                        // Compute final target size by simulating numResizes passes.
                        (int targetW, int targetH) = ComputeFinalSize(
                            origW,
                            origH,
                            numResizes,
                            pixelsPerUnit,
                            widthMultiplier,
                            heightMultiplier
                        );

                        // Clamp to sane limits
                        targetW = Mathf.Clamp(targetW, 1, 16384);
                        targetH = Mathf.Clamp(targetH, 1, 16384);

                        if (targetW == working.width && targetH == working.height)
                        {
                            ++skippedZeroDelta;
                            continue;
                        }

                        // If writing to separate folder, avoid mutating the original asset in memory.
                        Texture2D resizeSource = working;
                        Texture2D scratch = null;
                        bool useScratch = !string.IsNullOrEmpty(outputDirAssetPath);
                        if (useScratch)
                        {
                            // Create a scratch texture and copy pixels; avoids modifying the original in memory.
                            scratch = new Texture2D(
                                working.width,
                                working.height,
                                TextureFormat.RGBA32,
                                false
                            );
                            scratch.SetPixels(working.GetPixels());
                            scratch.Apply(false);
                            resizeSource = scratch;
                        }

                        try
                        {
                            switch (scalingResizeAlgorithm)
                            {
                                case ResizeAlgorithm.Bilinear:
                                    TextureScale.Bilinear(resizeSource, targetW, targetH);
                                    break;
                                case ResizeAlgorithm.Point:
                                    TextureScale.Point(resizeSource, targetW, targetH);
                                    break;
                                default:
                                    throw new InvalidEnumArgumentException(
                                        nameof(scalingResizeAlgorithm),
                                        (int)scalingResizeAlgorithm,
                                        typeof(ResizeAlgorithm)
                                    );
                            }

                            if (dryRun)
                            {
                                this.Log(
                                    $"[DryRun] Would resize {texture.name} to [{targetW}x{targetH}]"
                                );
                                ++resized; // counts as a planned resize
                                continue;
                            }

                            // Encode and atomically write the PNG
                            byte[] bytes = resizeSource.EncodeToPNG();

                            string finalAssetPath = assetPath;
                            if (!string.IsNullOrEmpty(outputDirAssetPath))
                            {
                                string fileName = Path.GetFileName(assetPath);
                                finalAssetPath = Path.Combine(outputDirAssetPath, fileName)
                                    .Replace('\\', '/');
                                EnsureDirectory(finalAssetPath);
                            }

                            string fullDest = ToFullPath(finalAssetPath);
                            string tempPath = fullDest + ".tmp";

                            File.WriteAllBytes(tempPath, bytes);

                            if (File.Exists(fullDest))
                            {
                                string backupPath = fullDest + ".bak";
                                File.Replace(tempPath, fullDest, backupPath, true);
                                // Best-effort cleanup of backup to avoid clutter in VCS; keep if replace failed.
                                try
                                {
                                    File.Delete(backupPath);
                                }
                                catch
                                { /* ignore */
                                }
                            }
                            else
                            {
                                // New file write
                                File.Move(tempPath, fullDest);
                            }

                            anyChanges = true;
                            ++resized;
                            this.Log(
                                $"Resized {texture.name} from [{origW}x{origH}] to [{targetW}x{targetH}]"
                            );
                        }
                        finally
                        {
                            if (scratch != null)
                            {
                                Object.DestroyImmediate(scratch);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        ++errors;
                        this.LogError($"Failed to resize {texture.name}.", e);
                    }
                    finally
                    {
                        // Restore importer readability to original state
                        if (tImporter.isReadable != originalReadable)
                        {
                            // Exit batch to restore importer reliably
                            AssetDatabase.StopAssetEditing();
                            tImporter.isReadable = originalReadable;
                            try
                            {
                                tImporter.SaveAndReimport();
                            }
                            catch
                            { /* ignore restore errors */
                            }
                            finally
                            {
                                AssetDatabase.StartAssetEditing();
                            }
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                WallstopStudios.UnityHelpers.Editor.Utils.EditorUi.ClearProgress();
            }

            if (anyChanges)
            {
                AssetDatabase.Refresh();
            }

            this.Log(
                $"Summary: processed={processed}, resized={(dryRun ? "planned:" : string.Empty)}{resized}, skippedExt={skippedWrongExt}, skippedNoChange={skippedZeroDelta}, errors={errors}"
            );
        }

        private static (int width, int height) ComputeFinalSize(
            int startWidth,
            int startHeight,
            int passes,
            int pixelsPerUnit,
            float widthMultiplier,
            float heightMultiplier
        )
        {
            int w = startWidth;
            int h = startHeight;
            for (int i = 0; i < passes; ++i)
            {
                int extraWidth = (int)Math.Round(w / (pixelsPerUnit * widthMultiplier));
                int extraHeight = (int)Math.Round(h / (pixelsPerUnit * heightMultiplier));
                // If both are zero, further passes won’t change the size; break early.
                if (extraWidth == 0 && extraHeight == 0)
                {
                    break;
                }

                w += extraWidth;
                h += extraHeight;
            }

            return (w, h);
        }

        private static string ToFullPath(string assetPath)
        {
            // Convert "Assets/..." to full system path
            string projectRoot = Application.dataPath.Substring(
                0,
                Application.dataPath.Length - "Assets".Length
            );
            return Path.Combine(projectRoot, assetPath).Replace('\\', '/');
        }

        private static void EnsureDirectory(string assetPath)
        {
            string dirAsset = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dirAsset))
            {
                return;
            }

            // Create physical directory if missing
            string fullDir = ToFullPath(dirAsset);
            if (!Directory.Exists(fullDir))
            {
                _ = Directory.CreateDirectory(fullDir);
            }

            // Ensure Unity knows about folders
            string[] parts = dirAsset.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(cur, parts[i]);
                }
                cur = next;
            }
        }
    }
#endif
}

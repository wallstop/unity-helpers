namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Programmatic API for applying generic texture importer settings (non-sprite specific)
    /// with support for selective fields and default-platform overrides.
    /// </summary>
    public static class TextureSettingsApplierAPI
    {
        [Serializable]
        public struct Config
        {
            // Read/Write
            public bool applyReadWriteEnabled;
            public bool readWriteEnabled;

            // MipMaps
            public bool applyMipMaps;
            public bool generateMipMaps;

            // Sampler
            public bool applyWrapMode;
            public TextureWrapMode wrapMode;
            public bool applyFilterMode;
            public FilterMode filterMode;

            // Importer-level compression convenience (kept for parity with Sprite API)
            public bool applyCompression;
            public TextureImporterCompression compression;
            public bool applyCrunchCompression;
            public bool useCrunchCompression;

            // Default Platform Settings overrides
            public bool applyPlatformResizeAlgorithm;
            public TextureResizeAlgorithm platformResizeAlgorithm;
            public bool applyPlatformMaxTextureSize;
            public int platformMaxTextureSize;
            public bool applyPlatformFormat;
            public TextureImporterFormat platformFormat;
            public bool applyPlatformCompression;
            public TextureImporterCompression platformCompression;
            public bool applyPlatformCrunchCompression;
            public bool platformUseCrunchCompression;
        }

        public static bool WillTextureSettingsChange(
            string assetPath,
            in Config config,
            TextureImporterSettings buffer = null
        )
        {
            TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti == null)
            {
                return false;
            }

            bool changed = false;

            if (config.applyReadWriteEnabled)
            {
                changed |= ti.isReadable != config.readWriteEnabled;
            }
            if (config.applyMipMaps)
            {
                changed |= ti.mipmapEnabled != config.generateMipMaps;
            }
            if (config.applyWrapMode)
            {
                changed |= ti.wrapMode != config.wrapMode;
            }
            if (config.applyFilterMode)
            {
                changed |= ti.filterMode != config.filterMode;
            }
            if (config.applyCompression)
            {
                changed |= ti.textureCompression != config.compression;
            }
            if (config.applyCrunchCompression)
            {
                changed |= ti.crunchedCompression != config.useCrunchCompression;
            }

            // Read current texture settings into a buffer for potential comparisons
            buffer ??= new TextureImporterSettings();
            ti.ReadTextureSettings(buffer);

            // Default platform settings
            TextureImporterPlatformSettings ps = ti.GetDefaultPlatformTextureSettings();
            if (config.applyPlatformResizeAlgorithm)
            {
                changed |= ps.resizeAlgorithm != config.platformResizeAlgorithm;
            }
            if (config.applyPlatformMaxTextureSize)
            {
                changed |= ps.maxTextureSize != config.platformMaxTextureSize;
            }
            if (config.applyPlatformFormat)
            {
                changed |= ps.format != config.platformFormat;
            }
            if (config.applyPlatformCompression)
            {
                changed |= ps.textureCompression != config.platformCompression;
            }
            if (config.applyPlatformCrunchCompression)
            {
                changed |= ps.crunchedCompression != config.platformUseCrunchCompression;
            }

            return changed;
        }

        public static bool TryUpdateTextureSettings(
            string assetPath,
            in Config config,
            out TextureImporter textureImporter,
            TextureImporterSettings buffer = null
        )
        {
            textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (textureImporter == null)
            {
                return false;
            }

            bool changed = false;
            bool settingsChanged = false;

            // Importer-level fields
            if (config.applyReadWriteEnabled)
            {
                if (textureImporter.isReadable != config.readWriteEnabled)
                {
                    textureImporter.isReadable = config.readWriteEnabled;
                    changed = true;
                }
            }
            if (config.applyMipMaps)
            {
                if (textureImporter.mipmapEnabled != config.generateMipMaps)
                {
                    textureImporter.mipmapEnabled = config.generateMipMaps;
                    changed = true;
                }
            }
            if (config.applyWrapMode)
            {
                if (textureImporter.wrapMode != config.wrapMode)
                {
                    textureImporter.wrapMode = config.wrapMode;
                    changed = true;
                }
            }
            if (config.applyFilterMode)
            {
                if (textureImporter.filterMode != config.filterMode)
                {
                    textureImporter.filterMode = config.filterMode;
                    changed = true;
                }
            }
            if (config.applyCompression)
            {
                if (textureImporter.textureCompression != config.compression)
                {
                    textureImporter.textureCompression = config.compression;
                    changed = true;
                }
            }
            if (config.applyCrunchCompression)
            {
                if (textureImporter.crunchedCompression != config.useCrunchCompression)
                {
                    textureImporter.crunchedCompression = config.useCrunchCompression;
                    changed = true;
                }
            }

            // Buffer for SetTextureSettings if we need it later (kept for parity/extension)
            buffer ??= new TextureImporterSettings();
            textureImporter.ReadTextureSettings(buffer);

            // Default platform settings
            bool platformChanged = false;
            TextureImporterPlatformSettings ps =
                textureImporter.GetDefaultPlatformTextureSettings();
            if (
                config.applyPlatformResizeAlgorithm
                && ps.resizeAlgorithm != config.platformResizeAlgorithm
            )
            {
                ps.resizeAlgorithm = config.platformResizeAlgorithm;
                platformChanged = true;
            }
            if (
                config.applyPlatformMaxTextureSize
                && ps.maxTextureSize != config.platformMaxTextureSize
            )
            {
                ps.maxTextureSize = config.platformMaxTextureSize;
                platformChanged = true;
            }
            if (config.applyPlatformFormat && ps.format != config.platformFormat)
            {
                ps.format = config.platformFormat;
                platformChanged = true;
            }
            if (
                config.applyPlatformCompression
                && ps.textureCompression != config.platformCompression
            )
            {
                ps.textureCompression = config.platformCompression;
                platformChanged = true;
            }
            if (
                config.applyPlatformCrunchCompression
                && ps.crunchedCompression != config.platformUseCrunchCompression
            )
            {
                ps.crunchedCompression = config.platformUseCrunchCompression;
                platformChanged = true;
            }
            if (platformChanged)
            {
                textureImporter.SetPlatformTextureSettings(ps);
                changed = true;
            }

            // We currently do not mutate buffer-only fields for non-sprite textures.
            if (settingsChanged)
            {
                textureImporter.SetTextureSettings(buffer);
            }

            return changed || settingsChanged;
        }
    }
#endif
}

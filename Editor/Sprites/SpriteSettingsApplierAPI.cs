// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;

    /// <summary>
    /// Public API to apply SpriteSettings profiles to assets. Mirrors the window logic
    /// but can be called from tests and scripts without UI.
    /// </summary>
    public static class SpriteSettingsApplierAPI
    {
        public sealed class PreparedProfile
        {
            public SpriteSettings settings;
            public SpriteSettings.MatchMode mode;
            public string nameLower;
            public string patternLower;
            public string extWithDot;
            public Regex regex;
            public int priority;
        }

        public static List<PreparedProfile> PrepareProfiles(List<SpriteSettings> profiles)
        {
            List<PreparedProfile> result = new(profiles?.Count ?? 0);
            if (profiles == null)
            {
                return result;
            }

            for (int i = 0; i < profiles.Count; i++)
            {
                SpriteSettings s = profiles[i];
                if (s == null)
                {
                    continue;
                }

                string trimmedPattern = string.IsNullOrEmpty(s.matchPattern)
                    ? null
                    : s.matchPattern.Trim();

                PreparedProfile p = new()
                {
                    settings = s,
                    mode = s.matchBy,
                    nameLower = string.IsNullOrEmpty(s.name) ? null : s.name.ToLowerInvariant(),
                    patternLower = string.IsNullOrEmpty(trimmedPattern)
                        ? null
                        : trimmedPattern.ToLowerInvariant(),
                    extWithDot =
                        string.IsNullOrEmpty(trimmedPattern) ? null
                        : trimmedPattern.StartsWith(".") ? trimmedPattern
                        : "." + trimmedPattern,
                    priority = s.priority,
                };
                if (
                    s.matchBy == SpriteSettings.MatchMode.Regex
                    && !string.IsNullOrEmpty(trimmedPattern)
                )
                {
                    try
                    {
                        p.regex = new Regex(
                            trimmedPattern,
                            RegexOptions.IgnoreCase | RegexOptions.Compiled
                        );
                    }
                    catch
                    {
                        p.regex = null;
                    }
                }
                result.Add(p);
            }
            return result;
        }

        private static string SanitizePath(string p)
        {
            return string.IsNullOrEmpty(p) ? p : p.SanitizePath();
        }

        public static SpriteSettings FindMatchingSettings(
            string assetPath,
            List<PreparedProfile> prepared
        )
        {
            assetPath = SanitizePath(assetPath);
            if (prepared == null || prepared.Count == 0)
            {
                return null;
            }

            string fileName = Path.GetFileName(assetPath);
            string fileNameLower = fileName.ToLowerInvariant();
            string pathLower = assetPath.ToLowerInvariant();
            string ext = Path.GetExtension(assetPath);

            SpriteSettings best = null;
            int bestPriority = int.MinValue;
            for (int i = 0; i < prepared.Count; i++)
            {
                PreparedProfile p = prepared[i];
                bool matches = false;
                switch (p.mode)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    case SpriteSettings.MatchMode.None:
#pragma warning restore CS0618 // Type or member is obsolete
                        break;
                    case SpriteSettings.MatchMode.Any:
                        matches =
                            string.IsNullOrEmpty(p.nameLower)
                            || fileNameLower.Contains(p.nameLower);
                        break;
                    case SpriteSettings.MatchMode.NameContains:
                        matches =
                            !string.IsNullOrEmpty(p.patternLower)
                            && fileNameLower.Contains(p.patternLower);
                        break;
                    case SpriteSettings.MatchMode.PathContains:
                        matches =
                            !string.IsNullOrEmpty(p.patternLower)
                            && pathLower.Contains(p.patternLower);
                        break;
                    case SpriteSettings.MatchMode.Extension:
                        matches =
                            !string.IsNullOrEmpty(p.extWithDot)
                            && string.Equals(ext, p.extWithDot, StringComparison.OrdinalIgnoreCase);
                        break;
                    case SpriteSettings.MatchMode.Regex:
                        matches = p.regex != null && p.regex.IsMatch(assetPath);
                        break;
                }
                if (!matches)
                {
                    continue;
                }

                if (best == null || p.priority > bestPriority)
                {
                    best = p.settings;
                    bestPriority = p.priority;
                }
            }
            return best;
        }

        public static bool WillTextureSettingsChange(
            string assetPath,
            List<PreparedProfile> prepared,
            TextureImporterSettings buffer = null
        )
        {
            assetPath = SanitizePath(assetPath);
            TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (textureImporter == null)
            {
                return false;
            }

            // Use Unity's canonical assetPath for matching to avoid path separator issues.
            string realPath = textureImporter.assetPath;
            SpriteSettings spriteData = FindMatchingSettings(realPath, prepared);
            if (spriteData == null)
            {
                return false;
            }

            bool changed = false;
            bool anyApplied =
                spriteData.applyTextureType
                || spriteData.applySpriteMode
                || spriteData.applyPixelsPerUnit
                || spriteData.applyPivot
                || spriteData.applyGenerateMipMaps
                || spriteData.applyAlphaIsTransparency
                || spriteData.applyReadWriteEnabled
                || spriteData.applyExtrudeEdges
                || spriteData.applyWrapMode
                || spriteData.applyFilterMode
                || spriteData.applyCrunchCompression
                || spriteData.applyCompression;
            if (spriteData.applyPixelsPerUnit)
            {
                changed |= textureImporter.spritePixelsPerUnit != spriteData.pixelsPerUnit;
            }
            if (spriteData.applyPivot)
            {
                changed |= textureImporter.spritePivot != spriteData.pivot;
            }
            if (spriteData.applyGenerateMipMaps)
            {
                changed |= textureImporter.mipmapEnabled != spriteData.generateMipMaps;
            }
            if (spriteData.applyCrunchCompression)
            {
                changed |= textureImporter.crunchedCompression != spriteData.useCrunchCompression;
            }
            if (spriteData.applyCompression)
            {
                changed |= textureImporter.textureCompression != spriteData.compressionLevel;
            }

            buffer ??= new TextureImporterSettings();
            textureImporter.ReadTextureSettings(buffer);
            if (spriteData.applyTextureType)
            {
                changed |= textureImporter.textureType != spriteData.textureType;
            }
            if (spriteData.applyPivot)
            {
                changed |= buffer.spriteAlignment != (int)SpriteAlignment.Custom;
            }
            if (spriteData.applyAlphaIsTransparency)
            {
                changed |= buffer.alphaIsTransparency != spriteData.alphaIsTransparency;
            }
            if (spriteData.applyReadWriteEnabled)
            {
                changed |= buffer.readable != spriteData.readWriteEnabled;
            }
            if (spriteData.applySpriteMode)
            {
                changed |= buffer.spriteMode != (int)spriteData.spriteMode;
            }
            if (spriteData.applyExtrudeEdges)
            {
                changed |= buffer.spriteExtrude != spriteData.extrudeEdges;
            }
            if (spriteData.applyWrapMode)
            {
                changed |= buffer.wrapMode != spriteData.wrapMode;
            }
            if (spriteData.applyFilterMode)
            {
                changed |= buffer.filterMode != spriteData.filterMode;
            }
            return changed || anyApplied;
        }

        public static bool TryUpdateTextureSettings(
            string assetPath,
            List<PreparedProfile> prepared,
            out TextureImporter textureImporter,
            TextureImporterSettings buffer = null
        )
        {
            assetPath = SanitizePath(assetPath);
            textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (textureImporter == null)
            {
                return false;
            }

            // Use Unity's canonical assetPath for matching to avoid path separator issues.
            string realPath = textureImporter.assetPath;
            SpriteSettings spriteData = FindMatchingSettings(realPath, prepared);
            if (spriteData == null)
            {
                return false;
            }

            bool changed = false;
            bool settingsChanged = false;
            bool anyApplied =
                spriteData.applyTextureType
                || spriteData.applySpriteMode
                || spriteData.applyPixelsPerUnit
                || spriteData.applyPivot
                || spriteData.applyGenerateMipMaps
                || spriteData.applyAlphaIsTransparency
                || spriteData.applyReadWriteEnabled
                || spriteData.applyExtrudeEdges
                || spriteData.applyWrapMode
                || spriteData.applyFilterMode
                || spriteData.applyCrunchCompression
                || spriteData.applyCompression;

            buffer ??= new TextureImporterSettings();
            textureImporter.ReadTextureSettings(buffer);

            if (spriteData.applyTextureType)
            {
                if (textureImporter.textureType != spriteData.textureType)
                {
                    textureImporter.textureType = spriteData.textureType;
                    changed = true;
                }
            }

            if (spriteData.applySpriteMode)
            {
                if (textureImporter.spriteImportMode != spriteData.spriteMode)
                {
                    textureImporter.spriteImportMode = spriteData.spriteMode;
                    changed = true;
                }
                if (buffer.spriteMode != (int)spriteData.spriteMode)
                {
                    buffer.spriteMode = (int)spriteData.spriteMode;
                    settingsChanged = true;
                }
            }
            if (spriteData.applyPixelsPerUnit)
            {
                if (textureImporter.spritePixelsPerUnit != spriteData.pixelsPerUnit)
                {
                    textureImporter.spritePixelsPerUnit = spriteData.pixelsPerUnit;
                    changed = true;
                }
                if (buffer.spritePixelsPerUnit != spriteData.pixelsPerUnit)
                {
                    buffer.spritePixelsPerUnit = spriteData.pixelsPerUnit;
                    settingsChanged = true;
                }
            }
            if (spriteData.applyPivot)
            {
                if (textureImporter.spritePivot != spriteData.pivot)
                {
                    textureImporter.spritePivot = spriteData.pivot;
                    changed = true;
                }
                if (buffer.spriteAlignment != (int)SpriteAlignment.Custom)
                {
                    buffer.spriteAlignment = (int)SpriteAlignment.Custom;
                    settingsChanged = true;
                }
                if (buffer.spritePivot != spriteData.pivot)
                {
                    buffer.spritePivot = spriteData.pivot;
                    settingsChanged = true;
                }
            }
            if (spriteData.applyGenerateMipMaps)
            {
                if (textureImporter.mipmapEnabled != spriteData.generateMipMaps)
                {
                    textureImporter.mipmapEnabled = spriteData.generateMipMaps;
                    changed = true;
                }
                if (buffer.mipmapEnabled != spriteData.generateMipMaps)
                {
                    buffer.mipmapEnabled = spriteData.generateMipMaps;
                    settingsChanged = true;
                }
            }
            if (spriteData.applyCrunchCompression)
            {
                if (textureImporter.crunchedCompression != spriteData.useCrunchCompression)
                {
                    textureImporter.crunchedCompression = spriteData.useCrunchCompression;
                    changed = true;
                }
            }
            if (spriteData.applyCompression)
            {
                if (textureImporter.textureCompression != spriteData.compressionLevel)
                {
                    textureImporter.textureCompression = spriteData.compressionLevel;
                    changed = true;
                }
            }
            if (spriteData.applyAlphaIsTransparency)
            {
                if (textureImporter.alphaIsTransparency != spriteData.alphaIsTransparency)
                {
                    textureImporter.alphaIsTransparency = spriteData.alphaIsTransparency;
                    changed = true;
                }
                if (buffer.alphaIsTransparency != spriteData.alphaIsTransparency)
                {
                    buffer.alphaIsTransparency = spriteData.alphaIsTransparency;
                    settingsChanged = true;
                }
            }
            if (spriteData.applyReadWriteEnabled)
            {
                if (textureImporter.isReadable != spriteData.readWriteEnabled)
                {
                    textureImporter.isReadable = spriteData.readWriteEnabled;
                    changed = true;
                }
                if (buffer.readable != spriteData.readWriteEnabled)
                {
                    buffer.readable = spriteData.readWriteEnabled;
                    settingsChanged = true;
                }
            }
            if (spriteData.applyExtrudeEdges)
            {
                if (buffer.spriteExtrude != spriteData.extrudeEdges)
                {
                    buffer.spriteExtrude = spriteData.extrudeEdges;
                    settingsChanged = true;
                }
            }
            if (spriteData.applyWrapMode)
            {
                if (textureImporter.wrapMode != spriteData.wrapMode)
                {
                    textureImporter.wrapMode = spriteData.wrapMode;
                    changed = true;
                }
                if (buffer.wrapMode != spriteData.wrapMode)
                {
                    buffer.wrapMode = spriteData.wrapMode;
                    settingsChanged = true;
                }
            }
            if (spriteData.applyFilterMode)
            {
                if (textureImporter.filterMode != spriteData.filterMode)
                {
                    textureImporter.filterMode = spriteData.filterMode;
                    changed = true;
                }
                if (buffer.filterMode != spriteData.filterMode)
                {
                    buffer.filterMode = spriteData.filterMode;
                    settingsChanged = true;
                }
            }

            if (settingsChanged)
            {
                textureImporter.SetTextureSettings(buffer);
            }

            return changed || settingsChanged || anyApplied;
        }
    }
#endif
}

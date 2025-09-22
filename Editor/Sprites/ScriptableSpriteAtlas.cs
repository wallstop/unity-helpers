namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using System.Collections.Generic;
    using Core.Attributes;
    using Core.Helper;
    using UnityEditor;

    [Flags]
    public enum SpriteSelectionMode
    {
        [Obsolete("Please select a valid value")]
        None = 0,
        Regex = 1 << 0,
        Labels = 1 << 1,
    }

    [Flags]
    public enum LabelSelectionMode
    {
        [Obsolete("Please select a valid value")]
        None = 0,
        All = 1 << 0,
        AnyOf = 1 << 1,
    }

    [Flags]
    public enum SpriteSelectionBooleanLogic
    {
        [Obsolete("Please select a valid value")]
        None = 0,
        And = 1 << 0,
        Or = 1 << 1,
    }

    [Serializable]
    public sealed class SourceFolderEntry
    {
        [Tooltip("Folder to scan for sprites. Path relative to Assets/.")]
        public string folderPath = "Assets/Sprites/";

        public SpriteSelectionMode selectionMode = SpriteSelectionMode.Regex;

        [WShowIf(
            nameof(selectionMode),
            expectedValues = new object[]
            {
                SpriteSelectionMode.Regex,
                SpriteSelectionMode.Regex | SpriteSelectionMode.Labels,
                (SpriteSelectionMode)(-1),
            }
        )]
        [Tooltip(
            "Regex patterns to match sprite file names within this specific folder. All regexes must match (AND logic). e.g., \"^icon_.*\\.png$\""
        )]
        public List<string> regexes = new();

        [WShowIf(
            nameof(selectionMode),
            expectedValues = new object[]
            {
                SpriteSelectionMode.Regex | SpriteSelectionMode.Labels,
                (SpriteSelectionMode)(-1),
            }
        )]
        public SpriteSelectionBooleanLogic regexAndTagLogic = SpriteSelectionBooleanLogic.And;

        [WShowIf(
            nameof(selectionMode),
            expectedValues = new object[]
            {
                SpriteSelectionMode.Labels,
                SpriteSelectionMode.Regex | SpriteSelectionMode.Labels,
                (SpriteSelectionMode)(-1),
            }
        )]
        public LabelSelectionMode labelSelectionMode = LabelSelectionMode.All;

        [WShowIf(
            nameof(selectionMode),
            expectedValues = new object[]
            {
                SpriteSelectionMode.Labels,
                SpriteSelectionMode.Regex | SpriteSelectionMode.Labels,
                (SpriteSelectionMode)(-1),
            }
        )]
        [Tooltip("Asset labels to include in the folders.")]
        [StringInList(typeof(Helpers), nameof(Helpers.GetAllSpriteLabelNames))]
        public List<string> labels = new();
    }

    [CreateAssetMenu(
        fileName = "NewScriptableSpriteAtlas",
        menuName = "Wallstop Studios/Unity Helpers/Scriptable Sprite Atlas Config"
    )]
    public sealed class ScriptableSpriteAtlas : ScriptableObject
    {
        [Header("Sprite Sources")]
        [Tooltip(
            "Manually added sprites. These will always be included in addition to scanned sprites."
        )]
        public List<Sprite> spritesToPack = new();

        [Tooltip("Define folders and their specific regex patterns for finding sprites.")]
        public List<SourceFolderEntry> sourceFolderEntries = new();

        [Header("Output Atlas Settings")]
        [Tooltip("Directory where the .spriteatlas asset will be saved. Relative to Assets/.")]
        public string outputSpriteAtlasDirectory = "Assets/Sprites/Atlases";
        public string outputSpriteAtlasName = "MyNewAtlas";

        public string FullOutputPath
        {
            get
            {
                if (
                    string.IsNullOrWhiteSpace(outputSpriteAtlasDirectory)
                    || string.IsNullOrWhiteSpace(outputSpriteAtlasName)
                )
                {
                    return null;
                }

                return System
                    .IO.Path.Combine(
                        outputSpriteAtlasDirectory,
                        outputSpriteAtlasName + ".spriteatlas"
                    )
                    .SanitizePath();
            }
        }

        [Header("Packing Settings")]
        [IntDropdown(32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384)]
        public int maxTextureSize = 8192;

        [Tooltip("Allow Unity to rotate sprites to fit them better.")]
        public bool enableRotation = true;

        [Tooltip("Padding in pixels between sprites in the atlas.")]
        [IntDropdown(0, 2, 4, 8, 16, 32)]
        public int padding = 4;

        public bool enableTightPacking = true;

        public bool enableAlphaDilation = false;

        [Tooltip(
            "Enable Read/Write on the generated atlas texture. Needed for some runtime operations, but increases memory."
        )]
        public bool readWriteEnabled = true;

        [Header("Compression Settings")]
        public bool useCrunchCompression = true;

        [Range(0, 100)]
        [WShowIf(nameof(useCrunchCompression))]
        public int crunchCompressionLevel = 50;

        public TextureImporterCompression compression = TextureImporterCompression.Compressed;
    }
#endif
}

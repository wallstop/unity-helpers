// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Sprites;

    /// <summary>
    /// A ScriptableObject test host for testing TexturePlatformOverrideEntryDrawer.
    /// </summary>
    internal sealed class TexturePlatformOverrideEntryTestHost : ScriptableObject
    {
        public TextureSettingsApplierWindow.PlatformOverrideEntry entry =
            new TextureSettingsApplierWindow.PlatformOverrideEntry();

        public List<TextureSettingsApplierWindow.PlatformOverrideEntry> entries =
            new List<TextureSettingsApplierWindow.PlatformOverrideEntry>
            {
                new TextureSettingsApplierWindow.PlatformOverrideEntry
                {
                    platformName = "Standalone",
                    applyResizeAlgorithm = true,
                },
                new TextureSettingsApplierWindow.PlatformOverrideEntry
                {
                    platformName = "Android",
                    applyMaxTextureSize = true,
                    applyFormat = true,
                },
            };
    }
#endif
}

// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Sprites;

    [Serializable]
    internal sealed class MultiObjectTexturePlatformOverrideTarget : ScriptableObject
    {
        public TextureSettingsApplierWindow.PlatformOverrideEntry entry =
            new TextureSettingsApplierWindow.PlatformOverrideEntry();
    }
#endif
}

// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEngine;

    public sealed class SpriteSettingsProfileCollection : ScriptableObject
    {
        public List<SpriteSettings> profiles = new();
    }
#endif
}

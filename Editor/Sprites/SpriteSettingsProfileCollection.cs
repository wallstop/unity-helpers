namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public sealed class SpriteSettingsProfileCollection : ScriptableObject
    {
        public List<SpriteSettings> profiles = new();
    }
#endif
}

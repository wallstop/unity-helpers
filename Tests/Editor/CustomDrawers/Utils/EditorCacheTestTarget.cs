// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers.Utils
{
#if UNITY_EDITOR

    using UnityEngine;

    internal sealed class EditorCacheTestTarget : ScriptableObject
    {
        public int testValue;
        public string testString;
    }

#endif
}

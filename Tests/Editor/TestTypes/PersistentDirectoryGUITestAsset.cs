// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;

    /// <summary>
    /// A ScriptableObject test asset for testing PersistentDirectoryGUI methods.
    /// </summary>
    internal sealed class PersistentDirectoryGUITestAsset : ScriptableObject
    {
        public string stringPath = "Assets/DefaultPath";
        public int intValue = 42;
        public Object objectReference;
    }
}

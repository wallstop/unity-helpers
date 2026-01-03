// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using UnityEngine;

    /// <summary>
    /// Test host for color data dictionary operations.
    /// </summary>
    public sealed class ColorDataDictionaryHost : ScriptableObject
    {
        public StringColorDataDictionary dictionary = new();
    }
}

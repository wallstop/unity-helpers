// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using UnityEngine;

    /// <summary>
    /// Test host for dictionary with trailing scalar field.
    /// </summary>
    public sealed class DictionaryScalarAfterHost : ScriptableObject
    {
        public IntStringDictionary dictionary = new();
        public int trailingScalar;
    }
}

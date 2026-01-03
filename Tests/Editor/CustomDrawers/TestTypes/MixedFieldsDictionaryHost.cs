// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using UnityEngine;

    /// <summary>
    /// Test host for mixed scalar and dictionary field testing.
    /// </summary>
    public sealed class MixedFieldsDictionaryHost : ScriptableObject
    {
        public int scalarValue;
        public IntStringDictionary dictionary = new();
    }
}

// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using UnityEngine;

    /// <summary>
    /// Test host for complex value dictionary operations.
    /// </summary>
    public sealed class ComplexValueDictionaryHost : ScriptableObject
    {
        public StringComplexDictionary dictionary = new();
    }
}

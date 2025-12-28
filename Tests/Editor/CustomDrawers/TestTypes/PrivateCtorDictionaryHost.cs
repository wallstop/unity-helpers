// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using UnityEngine;

    /// <summary>
    /// Test host for private constructor key/value dictionary operations.
    /// </summary>
    public sealed class PrivateCtorDictionaryHost : ScriptableObject
    {
        public PrivateCtorDictionary dictionary = new();
    }
}

// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using UnityEngine;

    /// <summary>
    /// Test host for string-string dictionary operations.
    /// </summary>
    public sealed class StringDictionaryHost : ScriptableObject
    {
        // ReSharper disable once NotAccessedField.Local
        public StringStringDictionary dictionary = new();
    }
}

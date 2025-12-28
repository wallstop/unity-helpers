// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for nested lists via wrapper classes.
    /// </summary>
    internal sealed class WrappedNestedListTarget : ScriptableObject
    {
        [WGroup("WrappedLists", "Wrapped Lists")]
        public List<IntListWrapper> wrappedLists = new();

        [WGroupEnd("WrappedLists")]
        public int afterWrappedField;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif

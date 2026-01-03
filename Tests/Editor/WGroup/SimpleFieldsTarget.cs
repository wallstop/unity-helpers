// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for simple primitive fields within WGroups.
    /// </summary>
    internal sealed class SimpleFieldsTarget : ScriptableObject
    {
        [WGroup(
            "Primitives",
            "Primitive Types",
            autoIncludeCount: WGroupAttribute.InfiniteAutoInclude
        )]
        public int intField;

        public float floatField;

        public string stringField;

        [WGroup("Primitives"), WGroupEnd("Primitives")]
        public bool boolField;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif

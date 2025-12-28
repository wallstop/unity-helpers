// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for custom serializable classes with children.
    /// </summary>
    internal sealed class SerializableClassTarget : ScriptableObject
    {
        [WGroup("Nested", "Nested Objects", autoIncludeCount: WGroupAttribute.InfiniteAutoInclude)]
        public NestedData nestedData = new();

        [WGroup("Nested"), WGroupEnd("Nested")]
        public NestedData anotherNestedData = new();

        public int afterNestedField;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif

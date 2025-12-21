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
        [WGroup("Nested", "Nested Objects")]
        public NestedData nestedData = new();

        [WGroupEnd("Nested")]
        public NestedData anotherNestedData = new();

        public int afterNestedField;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif

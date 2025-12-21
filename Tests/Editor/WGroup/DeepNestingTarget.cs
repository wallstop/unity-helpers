#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for deeply nested serializable structures.
    /// </summary>
    internal sealed class DeepNestingTarget : ScriptableObject
    {
        [WGroup("Deep", "Deep Nesting")]
        public DeepNestedData deepData = new();

        [WGroupEnd("Deep")]
        public int afterDeepField;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif

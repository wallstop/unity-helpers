#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for multiple sibling children at the same nesting level.
    /// </summary>
    public sealed class SiblingNestedTarget : ScriptableObject
    {
        [WGroup("parent", "Parent")]
        public string parentField;

        [WGroup("child1", "Child 1", parentGroup: "parent")]
        public string child1Field;

        [WGroupEnd("child1")]
        [WGroup("child2", "Child 2", parentGroup: "parent")]
        public string child2Field;

        [WGroupEnd("child2")]
        [WGroupEnd("parent")]
        public string afterParent;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif

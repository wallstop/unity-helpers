#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for verifying ungrouped [HideInInspector] fields are tracked
    /// in HiddenPropertyPaths and WGroupDrawOperation.IsHiddenInInspector.
    /// </summary>
    /// <remarks>
    /// Field layout:
    /// - visibleField1: No attributes, visible
    /// - ungroupedHiddenField1: [HideInInspector], should be in HiddenPropertyPaths
    /// - groupedField: [WGroup] anchor
    /// - ungroupedHiddenField2: [HideInInspector], should be in HiddenPropertyPaths
    /// - visibleField2: No attributes, visible
    /// </remarks>
    public sealed class WGroupUngroupedHiddenFieldTestTarget : ScriptableObject
    {
        public int visibleField1;

        [HideInInspector]
        [SerializeField]
#pragma warning disable CS0169 // Field is never used
        private int _ungroupedHiddenField1;
#pragma warning restore CS0169 // Field is never used

        [WGroup("Test Group", autoIncludeCount: 0)]
        public int groupedField;

        [HideInInspector]
        [SerializeField]
#pragma warning disable CS0169 // Field is never used
        private int _ungroupedHiddenField2;
#pragma warning restore CS0169 // Field is never used

        public int visibleField2;
    }
}
#endif

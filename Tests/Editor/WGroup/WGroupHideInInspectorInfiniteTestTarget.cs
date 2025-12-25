#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for infinite auto-include mode with hidden fields.
    /// </summary>
    public sealed class WGroupHideInInspectorInfiniteTestTarget : ScriptableObject
    {
        [WGroup("Infinite Group")]
        public int groupAnchor;

        [HideInInspector]
        [SerializeField]
#pragma warning disable CS0169 // Field is never used
        private int _hiddenField;
#pragma warning restore CS0169 // Field is never used

        public int visibleField1;
        public int visibleField2;
    }
}
#endif

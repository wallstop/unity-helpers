namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with empty/null group names and conflicting draw orders.
    /// Buttons without group names should NOT be merged, even if they have different draw orders.
    /// </summary>
    public sealed class WButtonUngroupedDifferentDrawOrdersTarget : ScriptableObject
    {
        [WButton("Ungrouped A", drawOrder: 0)]
        public void UngroupedA() { }

        [WButton("Ungrouped B", drawOrder: 5)]
        public void UngroupedB() { }

        [WButton("Ungrouped C", drawOrder: -2)]
        public void UngroupedC() { }
    }
}

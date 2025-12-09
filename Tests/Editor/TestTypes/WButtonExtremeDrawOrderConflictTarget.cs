namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with extreme draw order values in the same group.
    /// Expected: Uses int.MinValue (first declared) as the canonical draw order.
    /// </summary>
    public sealed class WButtonExtremeDrawOrderConflictTarget : ScriptableObject
    {
        [WButton("Extreme Low", drawOrder: int.MinValue, groupName: "Extreme")]
        public void ExtremeLow() { }

        [WButton("Extreme High", drawOrder: int.MaxValue, groupName: "Extreme")]
        public void ExtremeHigh() { }

        [WButton("Middle", drawOrder: 0, groupName: "Extreme")]
        public void Middle() { }
    }
}

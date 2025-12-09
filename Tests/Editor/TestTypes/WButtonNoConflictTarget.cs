namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with identical draw orders (no conflict) for comparison.
    /// Expected: No warning, all buttons in same group with draw order 0.
    /// </summary>
    public sealed class WButtonNoConflictTarget : ScriptableObject
    {
        [WButton("First", drawOrder: 0, groupName: "NoConflict")]
        public void First() { }

        [WButton("Second", drawOrder: 0, groupName: "NoConflict")]
        public void Second() { }

        [WButton("Third", drawOrder: 0, groupName: "NoConflict")]
        public void Third() { }
    }
}

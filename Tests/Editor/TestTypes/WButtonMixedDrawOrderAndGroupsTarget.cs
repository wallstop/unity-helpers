namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with mixed draw orders and groups to verify complex scenarios.
    /// </summary>
    public sealed class WButtonMixedDrawOrderAndGroupsTarget : ScriptableObject
    {
        // Draw order 0
        [WButton("Zero First", drawOrder: 0, groupName: "First Group")]
        public void ZeroFirst() { }

        [WButton("Zero Second", drawOrder: 0, groupName: "Second Group")]
        public void ZeroSecond() { }

        // Draw order -1 (still top placement)
        [WButton("Minus One First", drawOrder: -1, groupName: "A Group")]
        public void MinusOneFirst() { }

        [WButton("Minus One Second", drawOrder: -1, groupName: "B Group")]
        public void MinusOneSecond() { }

        // Draw order -2 (bottom placement)
        [WButton("Minus Two First", drawOrder: -2, groupName: "Bottom A")]
        public void MinusTwoFirst() { }

        [WButton("Minus Two Second", drawOrder: -2, groupName: "Bottom B")]
        public void MinusTwoSecond() { }
    }
}

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for verifying WButton draw order and group name functionality.
    /// Contains buttons with various draw orders and group names to test:
    /// - Arbitrary negative draw orders render at bottom
    /// - Arbitrary positive draw orders render at top
    /// - Different group names at same draw order render separately
    /// - Declaration order is preserved within same draw order
    /// </summary>
    public sealed class WButtonDrawOrderTestTarget : ScriptableObject
    {
        // Top placement buttons (draw order >= 0)
        [WButton("Top Action 1", drawOrder: 0, groupName: "Actions")]
        public void TopAction1() { }

        [WButton("Top Action 2", drawOrder: 0, groupName: "Actions")]
        public void TopAction2() { }

        [WButton("Top Debug 1", drawOrder: 0, groupName: "Debug")]
        public void TopDebug1() { }

        [WButton("Top Action 3", drawOrder: 1, groupName: "Actions")]
        public void TopAction3() { }

        [WButton("Top Utility", drawOrder: 5)]
        public void TopUtility() { }

        [WButton("Top High Order", drawOrder: 100)]
        public void TopHighOrder() { }

        // Bottom placement buttons (draw order < 0)
        [WButton("Bottom Action 1", drawOrder: -1, groupName: "Bottom Actions")]
        public void BottomAction1() { }

        [WButton("Bottom Action 2", drawOrder: -1, groupName: "Bottom Actions")]
        public void BottomAction2() { }

        [WButton("Bottom Debug 1", drawOrder: -1, groupName: "Bottom Debug")]
        public void BottomDebug1() { }

        [WButton("Bottom Low Order", drawOrder: -2)]
        public void BottomLowOrder() { }

        [WButton("Bottom Very Low Order", drawOrder: -100)]
        public void BottomVeryLowOrder() { }
    }
}

// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for testing arbitrary integer draw orders.
    /// </summary>
    public sealed class WButtonArbitraryDrawOrderTarget : ScriptableObject
    {
        [WButton("Order Int Max", drawOrder: int.MaxValue)]
        public void OrderIntMax() { }

        [WButton("Order 1000", drawOrder: 1000)]
        public void Order1000() { }

        [WButton("Order 100", drawOrder: 100)]
        public void Order100() { }

        [WButton("Order 10", drawOrder: 10)]
        public void Order10() { }

        [WButton("Order 1", drawOrder: 1)]
        public void Order1() { }

        [WButton("Order 0", drawOrder: 0)]
        public void Order0() { }

        [WButton("Order Minus 1", drawOrder: -1)]
        public void OrderMinus1() { }

        [WButton("Order Minus 10", drawOrder: -10)]
        public void OrderMinus10() { }

        [WButton("Order Minus 100", drawOrder: -100)]
        public void OrderMinus100() { }

        [WButton("Order Minus 1000", drawOrder: -1000)]
        public void OrderMinus1000() { }

        [WButton("Order Int Min", drawOrder: int.MinValue)]
        public void OrderIntMin() { }
    }
}

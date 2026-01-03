// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with three buttons in the same group but three different draw orders.
    /// Expected: All three should be in one "Actions" group, using draw order 5 (first declared).
    /// </summary>
    public sealed class WButtonThreeWayConflictTarget : ScriptableObject
    {
        [WButton("First Action", drawOrder: 5, groupName: "Actions")]
        public void FirstAction() { }

        [WButton("Second Action", drawOrder: -10, groupName: "Actions")]
        public void SecondAction() { }

        [WButton("Third Action", drawOrder: 100, groupName: "Actions")]
        public void ThirdAction() { }
    }
}

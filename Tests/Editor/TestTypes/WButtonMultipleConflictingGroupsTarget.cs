// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with multiple groups, each having conflicting draw orders.
    /// Expected: GroupA uses draw order 0 (from FirstA), GroupB uses draw order -5 (from FirstB).
    /// </summary>
    public sealed class WButtonMultipleConflictingGroupsTarget : ScriptableObject
    {
        [WButton("First A", drawOrder: 0, groupName: "GroupA")]
        public void FirstA() { }

        [WButton("First B", drawOrder: -5, groupName: "GroupB")]
        public void FirstB() { }

        [WButton("Second A", drawOrder: 10, groupName: "GroupA")]
        public void SecondA() { }

        [WButton("Second B", drawOrder: -20, groupName: "GroupB")]
        public void SecondB() { }

        [WButton("Third A", drawOrder: -3, groupName: "GroupA")]
        public void ThirdA() { }
    }
}

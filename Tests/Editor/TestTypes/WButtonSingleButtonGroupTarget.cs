// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with a single button in a group (edge case).
    /// Expected: No warning, single button group.
    /// </summary>
    public sealed class WButtonSingleButtonGroupTarget : ScriptableObject
    {
        [WButton("Only Button", drawOrder: 5, groupName: "Single")]
        public void OnlyButton() { }
    }
}

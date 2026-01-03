// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with alphabetically reversed group names to ensure
    /// declaration order takes precedence over alphabetical order.
    /// Groups declared: Zebra, Yak, Xenon (reverse alphabetical)
    /// Expected order: Zebra, Yak, Xenon (declaration order, not alphabetical)
    /// </summary>
    public sealed class WButtonReverseAlphabeticalGroupsTarget : ScriptableObject
    {
        [WButton("Zebra Action", drawOrder: 0, groupName: "Zebra")]
        public void ZebraAction() { }

        [WButton("Yak Action", drawOrder: 0, groupName: "Yak")]
        public void YakAction() { }

        [WButton("Xenon Action", drawOrder: 0, groupName: "Xenon")]
        public void XenonAction() { }
    }
}

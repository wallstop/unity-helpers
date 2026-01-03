// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for declaration order within same group.
    /// </summary>
    public sealed class WButtonDeclarationOrderTarget : ScriptableObject
    {
        [WButton("First", drawOrder: 0, groupName: "Order Test")]
        public void First() { }

        [WButton("Second", drawOrder: 0, groupName: "Order Test")]
        public void Second() { }

        [WButton("Third", drawOrder: 0, groupName: "Order Test")]
        public void Third() { }

        [WButton("Fourth", drawOrder: 0, groupName: "Other Group")]
        public void Fourth() { }

        [WButton("Fifth", drawOrder: 0, groupName: "Order Test")]
        public void Fifth() { }

        [WButton("Sixth", drawOrder: 0, groupName: "Other Group")]
        public void Sixth() { }
    }
}

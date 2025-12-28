// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target where the second button has a lower draw order than the first.
    /// Expected: Still uses first declared button's draw order (10), not the lower one (-10).
    /// </summary>
    public sealed class WButtonFirstDeclarationWinsTarget : ScriptableObject
    {
        [WButton("Declared First", drawOrder: 10, groupName: "Test")]
        public void DeclaredFirst() { }

        [WButton("Declared Second With Lower Order", drawOrder: -10, groupName: "Test")]
        public void DeclaredSecondWithLowerOrder() { }
    }
}

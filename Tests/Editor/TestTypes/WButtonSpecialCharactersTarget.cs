// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with special characters in names to verify they don't affect declaration order.
    /// Declaration order: _underscore, @at, #hash, !exclaim
    /// Alphabetical would sort by ASCII: ! (33), # (35), @ (64), _ (95)
    /// Expected: _underscore, @at, #hash, !exclaim (declaration order)
    /// </summary>
    public sealed class WButtonSpecialCharactersTarget : ScriptableObject
    {
        [WButton("_underscore first", drawOrder: 0)]
        public void UnderscoreMethod() { }

        [WButton("@at second", drawOrder: 0)]
        public void AtMethod() { }

        [WButton("#hash third", drawOrder: 0)]
        public void HashMethod() { }

        [WButton("!exclaim fourth", drawOrder: 0)]
        public void ExclaimMethod() { }
    }
}

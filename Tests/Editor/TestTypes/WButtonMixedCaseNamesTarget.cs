// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with mixed case names to verify case-insensitive alphabetical
    /// sorting does not override declaration order.
    /// Declaration order: apple, BANANA, cherry, Date
    /// Alphabetical (case insensitive): apple, BANANA, cherry, Date
    /// This is tricky because case-insensitive sort happens to match, but we want declaration order.
    /// </summary>
    public sealed class WButtonMixedCaseNamesTarget : ScriptableObject
    {
        [WButton("apple", drawOrder: 0)]
        public void AppleMethod() { }

        [WButton("BANANA", drawOrder: 0)]
        public void BananaMethod() { }

        [WButton("cherry", drawOrder: 0)]
        public void CherryMethod() { }

        [WButton("Date", drawOrder: 0)]
        public void DateMethod() { }
    }
}

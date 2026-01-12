// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with numeric prefix names that would sort differently numerically vs alphabetically.
    /// Declaration order: 2, 10, 1, 100
    /// Alphabetical: 1, 10, 100, 2
    /// Numeric: 1, 2, 10, 100
    /// Expected: 2, 10, 1, 100 (declaration order)
    /// </summary>
    public sealed class WButtonNumericPrefixNamesTarget : ScriptableObject
    {
        [WButton("2 - Second declared", drawOrder: 0)]
        public void Button2() { }

        [WButton("10 - Third declared", drawOrder: 0)]
        public void Button10() { }

        [WButton("1 - Fourth declared", drawOrder: 0)]
        public void Button1() { }

        [WButton("100 - Fifth declared", drawOrder: 0)]
        public void Button100() { }
    }
}

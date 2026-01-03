// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with empty display names (uses method name as display).
    /// Ensures empty/null display names fall back to method names but still use declaration order.
    /// </summary>
    public sealed class WButtonEmptyDisplayNameTarget : ScriptableObject
    {
        [WButton(drawOrder: 0)]
        public void ZZZFirstMethod() { }

        [WButton(drawOrder: 0)]
        public void AAASecondMethod() { }

        [WButton(drawOrder: 0)]
        public void MMMThirdMethod() { }
    }
}

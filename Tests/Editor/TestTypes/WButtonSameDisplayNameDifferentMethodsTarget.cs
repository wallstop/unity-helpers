// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with same display name but different methods.
    /// This tests that even with identical display names, declaration order is preserved.
    /// The fallback to method name alphabetical sorting should NOT apply.
    /// </summary>
    public sealed class WButtonSameDisplayNameDifferentMethodsTarget : ScriptableObject
    {
        [WButton("Action", drawOrder: 0)]
        public void ZZZFirstDeclaration() { }

        [WButton("Action", drawOrder: 0)]
        public void AAASecondDeclaration() { }

        [WButton("Action", drawOrder: 0)]
        public void MMMThirdDeclaration() { }
    }
}

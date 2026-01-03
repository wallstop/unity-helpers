// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with a single button to verify edge case handling.
    /// </summary>
    public sealed class WButtonSingleButtonTarget : ScriptableObject
    {
        [WButton("Only Button", drawOrder: 0)]
        public void OnlyMethod() { }
    }
}

// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// A MonoBehaviour test target with a single WButton method for testing WButtonInspector.
    /// </summary>
    internal sealed class WButtonMonoBehaviourTestTarget : MonoBehaviour
    {
        [WButton("Test Button")]
        public void TestMethod() { }
    }
}

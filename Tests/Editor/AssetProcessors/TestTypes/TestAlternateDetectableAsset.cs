// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using UnityEngine;

    /// <summary>
    /// An alternate ScriptableObject asset type used for testing multi-type asset change detection.
    /// </summary>
    internal sealed class TestAlternateDetectableAsset : ScriptableObject { }
}
#endif

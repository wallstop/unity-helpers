// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using UnityEngine;

    /// <summary>
    /// A ScriptableObject asset type used for testing asset change detection.
    /// Implements ITestDetectableContract to test interface-based matching.
    /// </summary>
    public sealed class TestDetectableAsset : ScriptableObject, ITestDetectableContract { }
}
#endif

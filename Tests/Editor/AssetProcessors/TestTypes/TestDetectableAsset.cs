#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using UnityEngine;

    /// <summary>
    /// A ScriptableObject asset type used for testing asset change detection.
    /// Implements ITestDetectableContract to test interface-based matching.
    /// </summary>
    internal sealed class TestDetectableAsset : ScriptableObject, ITestDetectableContract { }
}
#endif

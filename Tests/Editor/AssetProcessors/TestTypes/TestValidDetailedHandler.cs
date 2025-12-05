#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using UnityEngine;

    /// <summary>
    /// Test handler with valid detailed signature for validation testing.
    /// </summary>
    internal sealed class TestValidDetailedHandler : ScriptableObject
    {
        private void OnValidDetailed(TestDetectableAsset[] createdAssets, string[] deletedPaths)
        {
            _ = createdAssets;
            _ = deletedPaths;
        }
    }
}
#endif

// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using UnityEngine;

    /// <summary>
    /// Test handler with invalid created parameter (non-array) for signature validation testing.
    /// </summary>
    internal sealed class TestInvalidCreatedParameterHandler : ScriptableObject
    {
        private void OnInvalidCreated(TestDetectableAsset created, string[] deletedPaths)
        {
            _ = created;
            _ = deletedPaths;
        }
    }
}
#endif

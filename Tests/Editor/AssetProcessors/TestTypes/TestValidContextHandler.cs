// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test handler with valid context parameter signature for validation testing.
    /// </summary>
    internal sealed class TestValidContextHandler : ScriptableObject
    {
        private void OnValidContext(AssetChangeContext context)
        {
            _ = context;
        }
    }
}
#endif

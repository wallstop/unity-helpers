#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using UnityEngine;

    /// <summary>
    /// Test handler with valid no-parameter signature for validation testing.
    /// </summary>
    internal sealed class TestValidNoParametersHandler : ScriptableObject
    {
        private void OnValidNoParameters()
        {
            // No-op handler with valid no-parameter signature
        }
    }
}
#endif

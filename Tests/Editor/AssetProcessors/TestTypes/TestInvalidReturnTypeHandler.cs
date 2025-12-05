#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using UnityEngine;

    /// <summary>
    /// Test handler with invalid return type for signature validation testing.
    /// </summary>
    internal sealed class TestInvalidReturnTypeHandler : ScriptableObject
    {
        private int OnInvalidReturnType()
        {
            return 0;
        }
    }
}
#endif

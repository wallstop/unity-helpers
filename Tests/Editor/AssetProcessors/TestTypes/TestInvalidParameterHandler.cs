#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using UnityEngine;

    /// <summary>
    /// Test handler with invalid parameter type for signature validation testing.
    /// </summary>
    internal sealed class TestInvalidParameterHandler : ScriptableObject
    {
        private void OnInvalidSingleParameter(string unexpected)
        {
            _ = unexpected;
        }
    }
}
#endif

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test handler that counts invocations for loop detection testing.
    /// </summary>
    internal static class TestLoopingHandler
    {
        private static int _invocationCount;

        public static int InvocationCount => _invocationCount;

        public static void Clear()
        {
            _invocationCount = 0;
        }

        [DetectAssetChanged(typeof(TestDetectableAsset))]
        private static void OnLoopingChange(AssetChangeContext context)
        {
            _invocationCount++;
        }
    }
}
#endif

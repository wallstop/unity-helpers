#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using System.Collections.Generic;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Static test handler for asset change detection.
    /// </summary>
    internal static class TestStaticAssetChangeHandler
    {
        private static readonly List<AssetChangeContext> Recorded = new();

        public static IReadOnlyList<AssetChangeContext> RecordedContexts => Recorded;

        public static void Clear()
        {
            Recorded.Clear();
        }

        [DetectAssetChanged(typeof(TestDetectableAsset))]
        private static void OnTestAssetChanged(AssetChangeContext context)
        {
            Recorded.Add(context);
        }
    }
}
#endif

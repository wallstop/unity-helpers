#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test MonoBehaviour handler that uses SearchPrefabs option for asset change detection.
    /// This handler should be found when attached to prefabs.
    /// </summary>
    internal sealed class TestPrefabAssetChangeHandler : MonoBehaviour
    {
        private static readonly List<AssetChangeContext> Recorded = new();
        private static readonly List<TestPrefabAssetChangeHandler> InvokedInstances = new();

        public static IReadOnlyList<AssetChangeContext> RecordedContexts => Recorded;
        public static IReadOnlyList<TestPrefabAssetChangeHandler> RecordedInstances =>
            InvokedInstances;

        public static void Clear()
        {
            Recorded.Clear();
            InvokedInstances.Clear();
        }

        [DetectAssetChanged(
            typeof(TestDetectableAsset),
            AssetChangeFlags.Created | AssetChangeFlags.Deleted,
            DetectAssetChangedOptions.SearchPrefabs
        )]
        private void OnTestAssetChanged(AssetChangeContext context)
        {
            Recorded.Add(context);
            InvokedInstances.Add(this);
        }
    }
}
#endif

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test MonoBehaviour handler that uses both SearchPrefabs and SearchSceneObjects options.
    /// This handler should be found when attached to either prefabs or scene objects.
    /// </summary>
    internal sealed class TestCombinedSearchHandler : MonoBehaviour
    {
        private static readonly List<AssetChangeContext> Recorded = new();
        private static readonly List<TestCombinedSearchHandler> InvokedInstances = new();

        public static IReadOnlyList<AssetChangeContext> RecordedContexts => Recorded;
        public static IReadOnlyList<TestCombinedSearchHandler> RecordedInstances =>
            InvokedInstances;

        public static void Clear()
        {
            Recorded.Clear();
            InvokedInstances.Clear();
        }

        [DetectAssetChanged(
            typeof(TestDetectableAsset),
            AssetChangeFlags.Created | AssetChangeFlags.Deleted,
            DetectAssetChangedOptions.SearchPrefabs | DetectAssetChangedOptions.SearchSceneObjects
        )]
        private void OnTestAssetChanged(AssetChangeContext context)
        {
            Recorded.Add(context);
            InvokedInstances.Add(this);
        }
    }
}
#endif

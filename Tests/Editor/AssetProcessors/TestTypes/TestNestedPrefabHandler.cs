#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test MonoBehaviour handler for testing nested prefab component discovery.
    /// This handler can be placed on child objects within prefabs.
    /// </summary>
    internal sealed class TestNestedPrefabHandler : MonoBehaviour
    {
        private static readonly List<AssetChangeContext> Recorded = new();
        private static readonly List<TestNestedPrefabHandler> InvokedInstances = new();

        public static IReadOnlyList<AssetChangeContext> RecordedContexts => Recorded;
        public static IReadOnlyList<TestNestedPrefabHandler> RecordedInstances => InvokedInstances;

        public static void Clear()
        {
            Recorded.Clear();
            InvokedInstances.Clear();
        }

        [DetectAssetChanged(
            typeof(TestDetectableAsset),
            AssetChangeFlags.Created,
            DetectAssetChangedOptions.SearchPrefabs
        )]
        private void OnTestAssetCreated(AssetChangeContext context)
        {
            Recorded.Add(context);
            InvokedInstances.Add(this);
        }
    }
}
#endif

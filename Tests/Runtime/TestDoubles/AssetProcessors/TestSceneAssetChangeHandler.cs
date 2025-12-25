#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test MonoBehaviour handler that uses SearchSceneObjects option for asset change detection.
    /// This handler should be found when attached to GameObjects in open scenes.
    /// </summary>
    /// <remarks>
    /// This class must be in a non-Editor folder so it can be attached to GameObjects.
    /// </remarks>
    public sealed class TestSceneAssetChangeHandler : MonoBehaviour
    {
        private static readonly List<AssetChangeContext> Recorded = new();
        private static readonly List<TestSceneAssetChangeHandler> InvokedInstances = new();

        public static IReadOnlyList<AssetChangeContext> RecordedContexts => Recorded;
        public static IReadOnlyList<TestSceneAssetChangeHandler> RecordedInstances =>
            InvokedInstances;

        public static void Clear()
        {
            Recorded.Clear();
            InvokedInstances.Clear();
        }

        [DetectAssetChanged(
            typeof(TestDetectableAsset),
            AssetChangeFlags.Created | AssetChangeFlags.Deleted,
            DetectAssetChangedOptions.SearchSceneObjects
        )]
        private void OnTestAssetChanged(AssetChangeContext context)
        {
            Recorded.Add(context);
            InvokedInstances.Add(this);
        }
    }
}
#endif

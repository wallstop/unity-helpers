namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System.Collections;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class SceneHelperTests : CommonTestBase
    {
        // Async disposals queued via TrackAsyncDisposal in base

        [Test]
        public void GetScenesInBuild()
        {
            // This will only pass if you have scenes in your build path
            string[] scenes = SceneHelper.GetScenesInBuild();
            Assert.That(scenes, Is.Not.Empty);
        }

        [Test]
        public void GetAllScenePaths()
        {
            string[] scenePaths = SceneHelper.GetAllScenePaths();
            Assert.That(scenePaths, Is.Not.Empty);
            Assert.IsTrue(
                scenePaths.Any(path => path.Contains("Test1")),
                string.Join(",", scenePaths)
            );
            Assert.IsTrue(
                scenePaths.Any(path => path.Contains("Test2")),
                string.Join(",", scenePaths)
            );
        }

        [UnityTest]
        public IEnumerator GetObjectOfTypeInScene()
        {
            ValueTask<DeferredDisposalResult<SpriteRenderer>> task =
                SceneHelper.GetObjectOfTypeInScene<SpriteRenderer>(
                    @"Packages\com.wallstop-studios.unity-helpers\Tests\Runtime\Scenes\Test1.unity"
                );
            while (!task.IsCompleted)
            {
                yield return null;
            }
            Assert.IsTrue(task.IsCompletedSuccessfully);

            TrackAsyncDisposal(task.Result.DisposeAsync);
            SpriteRenderer found = task.Result.result;
            Assert.IsTrue(found != null);
        }

        [UnityTest]
        public IEnumerator GetAllObjectOfTypeInScene()
        {
            ValueTask<DeferredDisposalResult<SpriteRenderer[]>> task =
                SceneHelper.GetAllObjectsOfTypeInScene<SpriteRenderer>(
                    @"Packages\com.wallstop-studios.unity-helpers\Tests\Runtime\Scenes\Test1.unity"
                );

            while (!task.IsCompleted)
            {
                yield return null;
            }

            Assert.IsTrue(task.IsCompletedSuccessfully);
            TrackAsyncDisposal(task.Result.DisposeAsync);
            SpriteRenderer[] found = task.Result.result;
            Assert.That(found, Has.Length.EqualTo(7));
        }
    }
}

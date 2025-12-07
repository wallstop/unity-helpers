namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class MatchTransformEdgeTests : CommonTestBase
    {
        [UnityTest]
        public IEnumerator MatchingSelfAppliesOffset()
        {
            GameObject go = Track(
                new GameObject("Follower", typeof(MatchTransform))
                {
                    transform = { position = new Vector3(1f, 2f, 3f) },
                }
            );
            MatchTransform matcher = go.GetComponent<MatchTransform>();
            matcher.toMatch = go.transform;
            matcher.mode = MatchTransformMode.Update;
            matcher.localOffset = new Vector3(5f, -2f, 0.5f);

            matcher.SendMessage("Awake");
            matcher.SendMessage("Update");
            yield return null;

            Vector3 expected = new(6f, 0f, 3.5f);
            Assert.AreEqual(expected, go.transform.position);
        }
    }
}

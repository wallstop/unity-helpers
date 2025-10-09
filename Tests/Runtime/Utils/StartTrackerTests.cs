namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class StartTrackerTests
    {
        [UnityTest]
        public IEnumerator StartedBecomesTrueOnStart()
        {
            GameObject go = new("Tracker", typeof(StartTracker));
            StartTracker tracker = go.GetComponent<StartTracker>();
            Assert.IsFalse(tracker.Started);

            yield return null;

            Assert.IsTrue(tracker.Started);
        }
    }
}

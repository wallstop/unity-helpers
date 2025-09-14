namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class RandomExtensionTests
    {
        [Test]
        public void NextVector2InRange()
        {
            HashSet<float> seenAngles = new();
            for (int i = 0; i < 1_000; ++i)
            {
                Vector2 vector = PRNG.Instance.NextVector2(-100, 100);
                float range = PRNG.Instance.NextFloat(100f);
                Vector2 inRange = PRNG.Instance.NextVector2InRange(range, vector);
                Assert.LessOrEqual(Vector2.Distance(vector, inRange), range);
                seenAngles.Add(Vector2.SignedAngle(vector, inRange));
            }

            Assert.LessOrEqual(3, seenAngles.Count);
        }
    }
}

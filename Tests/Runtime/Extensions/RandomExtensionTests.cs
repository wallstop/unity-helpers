namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Random;
    using WallstopStudios.UnityHelpers.Tests.Random;

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

        [Test]
        public void NextOfParamsCoversAllInputs()
        {
            HashSet<int> seen = new();
            for (int i = 0; i < 100; ++i)
            {
                int value = PRNG.Instance.NextOfParams(1, 2, 3);
                Assert.That(new[] { 1, 2, 3 }, Does.Contain(value));
                seen.Add(value);
            }

            Assert.AreEqual(3, seen.Count);
        }

        [Test]
        public void NextEnumExceptSkipsAllProvidedExceptions()
        {
            HashSet<TestValues> seen = new();
            for (int i = 0; i < 200; ++i)
            {
                TestValues value = PRNG.Instance.NextEnumExcept(
                    TestValues.Value0,
                    TestValues.Value1,
                    TestValues.Value2,
                    TestValues.Value3,
                    TestValues.Value4
                );
                Assert.IsFalse(
                    value
                        is TestValues.Value0
                            or TestValues.Value1
                            or TestValues.Value2
                            or TestValues.Value3
                            or TestValues.Value4
                );
                seen.Add(value);
            }

            Assert.GreaterOrEqual(seen.Count, 1);
        }
    }
}

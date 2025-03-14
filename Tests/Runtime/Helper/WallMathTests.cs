namespace UnityHelpers.Tests.Tests.Runtime.Helper
{
    using Core.Extension;
    using Core.Helper;
    using Core.Random;
    using NUnit.Framework;
    using UnityEngine;

    public sealed class WallMathTests
    {
        private const int TestIterations = 10_000;

        [Test]
        public void ApproximatelyRandom()
        {
            for (int i = 0; i < TestIterations; ++i)
            {
                float target = PRNG.Instance.NextFloat(-1_000, 1_000);
                float delta = PRNG.Instance.NextFloat(0f, 10f);

                float insideOffset = delta * PRNG.Instance.NextFloat();
                if (PRNG.Instance.NextBool())
                {
                    insideOffset *= -1;
                }

                float inside = target + insideOffset;
                Assert.IsTrue(
                    target.Approximately(inside, delta),
                    $"Target {target} is not close enough to {inside} with a delta of {delta}."
                );

                float outsideOffset = delta * PRNG.Instance.NextFloat(1.001f, 10f);
                if (PRNG.Instance.NextBool())
                {
                    outsideOffset *= -1;
                }

                float outside = target + outsideOffset;
                Assert.IsFalse(
                    target.Approximately(outside, delta),
                    $"Target {target} was unexpectedly close to {outside} with a delta of {delta}."
                );
            }
        }

        [Test]
        public void PositiveMod()
        {
            Assert.AreEqual(9, (-1).PositiveMod(10));
            Assert.AreEqual(1, 1.PositiveMod(10));
            Assert.AreEqual(9f, (-1f).PositiveMod(10f));
            Assert.AreEqual(1f, 1f.PositiveMod(10f));
            Assert.AreEqual(9.0, (-1.0).PositiveMod(10.0));
            Assert.AreEqual(1.0, 1.0.PositiveMod(10.0));
            Assert.AreEqual(9L, (-1L).PositiveMod(10L));
            Assert.AreEqual(1L, 1L.PositiveMod(10L));
        }

        [Test]
        public void ApproximatelyExpected()
        {
            Assert.IsTrue(0f.Approximately(0f, 0f));
            Assert.IsTrue(0f.Approximately(0.5f, 1f));
            Assert.IsFalse(0.001f.Approximately(0f, 0f));
            Assert.IsFalse(100f.Approximately(5f, 2.4f));
            Assert.IsTrue(0.001f.Approximately(0.0001f));
        }

        [Test]
        public void ClampRectInsideRandom()
        {
            for (int i = 0; i < TestIterations; ++i)
            {
                Vector2 position = PRNG.Instance.NextVector2InRange(10f);

                Vector2 size;
                do
                {
                    size = PRNG.Instance.NextVector2InRange(10f);
                } while (size.x < 0 || size.y < 0);

                Rect rect = new(position, size);

                Vector2 inside = new(
                    PRNG.Instance.NextFloat(rect.min.x, rect.max.x),
                    PRNG.Instance.NextFloat(rect.min.y, rect.max.y)
                );
                Assert.IsTrue(rect.Contains(inside), $"Rect {rect} does not contain {inside}.");
                Vector2 clamped = rect.Clamp(inside);
                Assert.AreEqual(inside, clamped);
            }
        }

        [Test]
        public void ClampRectInsideExpected()
        {
            Rect rect = new Rect(Vector2.one, Vector2.one);
            Vector2 clamped = rect.Clamp(rect.center);
            Assert.AreEqual(rect.center, clamped);

            Vector2 inside =
                rect.center
                + new Vector2(PRNG.Instance.NextFloat(0.1f), PRNG.Instance.NextFloat(0.1f));
            clamped = rect.Clamp(inside);
            Assert.AreEqual(
                inside,
                clamped,
                $"Expected inside point {inside} unexpectedly was clamped to {clamped} for rect {rect}."
            );
        }

        [Test]
        public void ClampRectOutsideRandom()
        {
            Rect rect = new(Vector2.one, Vector2.one);
            for (int i = 0; i < TestIterations; ++i)
            {
                Vector2 outside;
                do
                {
                    outside = PRNG.Instance.NextVector2InRange(10f, rect.center);
                } while (rect.Contains(outside));

                Vector2 originalOutside = outside;
                Vector2 clamped = rect.Clamp(ref outside);
                Debug.Log($"Clamped {originalOutside} to {clamped}.");
                Assert.AreNotEqual(originalOutside, clamped);
                Assert.AreEqual(clamped, outside);

                // Shrink just a little to ensure within bounds, clamping will put it right on the edge
                const float shrinkScaleToAccountForPrecision = 0.99f;
                Vector2 delta = rect.center - clamped;
                delta *= shrinkScaleToAccountForPrecision;
                clamped = rect.center + delta;
                Assert.IsTrue(
                    rect.Contains(clamped),
                    $"Expected rect {rect} to contain {clamped}, but it did not."
                );
            }
        }

        [Test]
        public void ClampRectOutsideExpected()
        {
            Rect rect = new(Vector2.one, Vector2.one);
            Vector2 outside = new(rect.center.x, 100f);
            Vector2 clamped = rect.Clamp(ref outside);
            Assert.AreEqual(clamped, outside);
            Assert.AreEqual(new Vector2(rect.center.x, 2f), clamped);

            // Shrink just a little to ensure within bounds, clamping will put it right on the edge
            const float shrinkScaleToAccountForPrecision = 0.99f;
            Vector2 delta = rect.center - clamped;
            delta *= shrinkScaleToAccountForPrecision;
            clamped = rect.center + delta;
            Assert.IsTrue(
                rect.Contains(clamped),
                $"Expected rect to contain {clamped}, but it did not."
            );
        }

        [Test]
        public void ClampFloat()
        {
            float value = 1.25f;
            float clamped = value.Clamp(0, 1);
            Assert.AreEqual(1, clamped);
            Assert.AreEqual(1.25f, value);

            value = 0.5f;
            clamped = value.Clamp(0, 1);
            Assert.AreEqual(0.5f, clamped);
            Assert.AreEqual(0.5f, value);
        }

        [Test]
        public void WrappedIncrementWrapped()
        {
            int ten = 10;
            int one = ten.WrappedIncrement(10);
            Assert.AreEqual(1, one);
            Assert.AreEqual(10, ten);

            int toChangeToOne = 10;
            int returned = WallMath.WrappedIncrement(ref toChangeToOne, 10);
            Assert.AreEqual(1, returned);
            Assert.AreEqual(1, toChangeToOne);
        }

        [Test]
        public void WrappedIncrementUnwrapped()
        {
            int ten = 10;
            int eleven = ten.WrappedIncrement(100);
            Assert.AreEqual(11, eleven);
            Assert.AreEqual(10, ten);

            int toChangeToEleven = 10;
            int returned = WallMath.WrappedIncrement(ref toChangeToEleven, 100);
            Assert.AreEqual(11, returned);
            Assert.AreEqual(11, toChangeToEleven);
        }

        [Test]
        public void WrappedAddWrapped()
        {
            int ten = 10;
            int two = ten.WrappedAdd(2, 10);
            Assert.AreEqual(10, ten);
            Assert.AreEqual(2, two);

            int toChangeToTwo = 10;
            int returned = WallMath.WrappedAdd(ref toChangeToTwo, 2, 10);
            Assert.AreEqual(2, returned);
            Assert.AreEqual(2, toChangeToTwo);
        }

        [Test]
        public void WrappedAddUnwrapped()
        {
            int ten = 10;
            int twelve = ten.WrappedAdd(2, 100);
            Assert.AreEqual(10, ten);
            Assert.AreEqual(12, twelve);

            int toChangeToTwelve = 10;
            int returned = WallMath.WrappedAdd(ref toChangeToTwelve, 2, 100);
            Assert.AreEqual(12, returned);
            Assert.AreEqual(12, toChangeToTwelve);
        }
    }
}

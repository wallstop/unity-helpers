namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class UnityExtensionsBasicTests
    {
        [Test]
        public void GetCenterUsesCenterPointOffsetWhenAvailable()
        {
            GameObject go = new("CenterPointTest", typeof(CenterPointOffset));
            try
            {
                go.transform.position = new Vector3(5f, 5f, 0f);
                CenterPointOffset offset = go.GetComponent<CenterPointOffset>();
                offset.offset = new Vector2(3f, 4f);

                Assert.AreEqual(offset.CenterPoint, go.GetCenter());

                Object.DestroyImmediate(offset);
                Assert.AreEqual((Vector2)go.transform.position, go.GetCenter());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BoundsAndRectConversionsAreInverse()
        {
            Rect rect = new(1f, 2f, 3f, 4f);
            Bounds bounds = rect.Bounds();
            Rect reconstructed = bounds.Rect();
            Assert.AreEqual(rect, reconstructed);
        }

        [Test]
        public void GetWorldRectComputesRectFromCorners()
        {
            GameObject go = new("RectTransformTest", typeof(RectTransform));
            try
            {
                RectTransform rectTransform = go.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(100f, 50f);
                rectTransform.position = new Vector3(10f, 20f, 0f);

                Rect worldRect = rectTransform.GetWorldRect();
                Assert.AreEqual(100f, worldRect.width, 1e-3f);
                Assert.AreEqual(50f, worldRect.height, 1e-3f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ToJsonStringSerializesVector()
        {
            Vector3 vector3 = new(1f, 2f, 3f);
            Assert.AreEqual("{1, 2, 3}", vector3.ToJsonString());

            Vector2 vector2 = new(4f, 5f);
            Assert.AreEqual("{4, 5}", vector2.ToJsonString());
        }

        [Test]
        public void IsNoiseUsesThreshold()
        {
            Assert.IsTrue(new Vector2(0.1f, -0.1f).IsNoise());
            Assert.IsFalse(new Vector2(0.5f, 0f).IsNoise());
        }

        [Test]
        public void StopResetsRigidBody()
        {
            GameObject go = new("RigidBodyTest", typeof(Rigidbody2D));
            try
            {
                Rigidbody2D body = go.GetComponent<Rigidbody2D>();
                body.velocity = new Vector2(10f, 5f);
                body.angularVelocity = 15f;
                body.Stop();
                Assert.AreEqual(Vector2.zero, body.velocity);
                Assert.AreEqual(0f, body.angularVelocity);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExpandBoundsReturnsEnclosingBounds()
        {
            BoundsInt a = new(0, 0, 0, 2, 2, 2);
            BoundsInt b = new(-1, -1, -1, 1, 1, 1);
            BoundsInt expanded = a.ExpandBounds(b);
            Assert.AreEqual(-1, expanded.xMin);
            Assert.AreEqual(2, expanded.xMax);
        }

        [Test]
        public void GetBoundsComputesFromPoints()
        {
            List<Vector3Int> points = new() { new(1, 2, 3), new(4, 5, 6) };
            BoundsInt? bounds = points.GetBounds();
            Assert.IsTrue(bounds.HasValue);
            Assert.AreEqual(1, bounds.Value.xMin);
            Assert.AreEqual(5, bounds.Value.xMax);
            Assert.AreEqual(2, bounds.Value.yMin);
            Assert.AreEqual(6, bounds.Value.yMax);

            bounds = points.GetBounds(inclusive: true);
            Assert.IsTrue(bounds.HasValue);
            Assert.AreEqual(1, bounds.Value.xMin);
            Assert.AreEqual(4, bounds.Value.xMax);
            Assert.AreEqual(2, bounds.Value.yMin);
            Assert.AreEqual(5, bounds.Value.yMax);
        }

        [Test]
        public void ContainsFastVectorEvaluatesPosition()
        {
            FastVector3Int point = new(0, 0, 0);
            BoundsInt bounds = new(0, 0, 0, 1, 1, 1);
            Assert.IsTrue(bounds.Contains(point));
        }
    }
}

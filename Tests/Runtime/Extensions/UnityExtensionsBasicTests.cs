namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;
    using Object = UnityEngine.Object;

    public sealed class UnityExtensionsBasicTests
    {
        private readonly List<Object> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var spawned in _spawned.ToArray())
            {
                if (spawned != null)
                {
                    spawned.Destroy();
                }
            }
            _spawned.Clear();
        }

        [Test]
        public void GetCenterUsesCenterPointOffsetWhenAvailable()
        {
            GameObject go = new("CenterPointTest", typeof(CenterPointOffset));
            _spawned.Add(go);

            go.transform.position = new Vector3(5f, 5f, 0f);
            CenterPointOffset offset = go.GetComponent<CenterPointOffset>();
            offset.offset = new Vector2(3f, 4f);

            Assert.AreEqual(offset.CenterPoint, go.GetCenter());

            Object.DestroyImmediate(offset);
            Assert.AreEqual((Vector2)go.transform.position, go.GetCenter());
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
            _spawned.Add(go);

            RectTransform rectTransform = go.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100f, 50f);
            rectTransform.position = new Vector3(10f, 20f, 0f);

            Rect worldRect = rectTransform.GetWorldRect();
            Assert.AreEqual(100f, worldRect.width, 1e-3f);
            Assert.AreEqual(50f, worldRect.height, 1e-3f);
        }

        [Test]
        public void OrthographicBoundsUsesCameraDepthAndCenter()
        {
            GameObject go = new("CameraBoundsTest", typeof(Camera));
            _spawned.Add(go);

            Camera camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 50f;
            go.transform.position = new Vector3(1f, 2f, -10f);

            Bounds bounds = camera.OrthographicBounds();
            Assert.AreEqual(go.transform.position, bounds.center);

            int screenHeight = Screen.height == 0 ? 1 : Screen.height;
            float screenAspect = (float)Screen.width / screenHeight;
            float expectedHeight = camera.orthographicSize * 2f;
            float expectedDepth = camera.farClipPlane - camera.nearClipPlane;
            Vector3 expectedSize = new(
                expectedHeight * screenAspect,
                expectedHeight,
                expectedDepth
            );
            Assert.AreEqual(expectedSize, bounds.size);
        }

        [Test]
        public void OrthographicBoundsThrowsWhenCameraIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => UnityExtensions.OrthographicBounds(null));
        }

        [Test]
        public void ToJsonStringSerializesVector()
        {
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                Vector3 vector3 = new(1.5f, 2f, 3f);
                Assert.AreEqual("{1.5, 2, 3}", vector3.ToJsonString());

                Vector2 vector2 = new(4.25f, 5f);
                Assert.AreEqual("{4.25, 5}", vector2.ToJsonString());
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Test]
        public void IsNoiseUsesThreshold()
        {
            Assert.IsTrue(new Vector2(0.1f, -0.1f).IsNoise());
            Assert.IsFalse(new Vector2(0.5f, 0f).IsNoise());
            Assert.IsTrue(new Vector2(0.3f, 0f).IsNoise(0.5f));
            Assert.IsFalse(new Vector2(0.3f, 0f).IsNoise(0.2f));
            Assert.IsTrue(new Vector2(-0.3f, 0f).IsNoise(-0.5f));
        }

        [Test]
        public void StopResetsRigidBody()
        {
            GameObject go = new("RigidBodyTest", typeof(Rigidbody2D));
            _spawned.Add(go);

            Rigidbody2D body = go.GetComponent<Rigidbody2D>();
            body.velocity = new Vector2(10f, 5f);
            body.angularVelocity = 15f;
            body.Stop();
            Assert.AreEqual(Vector2.zero, body.velocity);
            Assert.AreEqual(0f, body.angularVelocity);
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

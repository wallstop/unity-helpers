namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using UnityEngine.UI;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class MatchColliderToSpriteTests : CommonTestBase
    {
        private Sprite _testSprite;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Texture2D texture = new(64, 64);
            _testSprite = Sprite.Create(
                texture,
                new Rect(0, 0, 64, 64),
                new Vector2(0.5f, 0.5f),
                100f
            );

            // Define a physics shape for the sprite (a simple rectangle)
            Vector2[] physicsShape = new[]
            {
                new Vector2(-0.5f, -0.5f),
                new Vector2(-0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, -0.5f),
            };
            _testSprite.OverridePhysicsShape(new[] { physicsShape });
        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            if (_testSprite != null)
            {
                Object.Destroy(_testSprite.texture);
                Object.Destroy(_testSprite);
            }
        }

        // Per-test cleanup handled by CommonTestBase

        [UnityTest]
        public IEnumerator FindsPolygonColliderOnAwake()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(SpriteRenderer),
                    typeof(MatchColliderToSprite)
                )
            );
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();

            yield return null;

            Assert.IsTrue(matcher.polygonCollider != null);
        }

        [UnityTest]
        public IEnumerator FindsSpriteRendererOnValidate()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(SpriteRenderer),
                    typeof(MatchColliderToSprite)
                )
            );
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = _testSprite;
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();

            matcher.OnValidate();
            yield return null;

            Assert.IsTrue(matcher.spriteRenderer != null);
        }

        [UnityTest]
        public IEnumerator UpdatesColliderWhenSpriteChanges()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(SpriteRenderer),
                    typeof(MatchColliderToSprite)
                )
            );
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();
            PolygonCollider2D collider = go.GetComponent<PolygonCollider2D>();

            renderer.sprite = null;
            matcher.OnValidate();
            yield return null;

            int pathCountWithoutSprite = collider.pathCount;

            renderer.sprite = _testSprite;
            yield return null;
            yield return null;

            Assert.AreNotEqual(pathCountWithoutSprite, collider.pathCount);
        }

        [UnityTest]
        public IEnumerator InvokesColliderUpdatedEvent()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(SpriteRenderer),
                    typeof(MatchColliderToSprite)
                )
            );
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = _testSprite;
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();

            bool eventInvoked = false;
            matcher.colliderUpdated += () =>
            {
                eventInvoked = true;
            };

            matcher.OnValidate();
            yield return null;

            Assert.IsTrue(eventInvoked);
        }

        [UnityTest]
        public IEnumerator HandlesNullSprite()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(SpriteRenderer),
                    typeof(MatchColliderToSprite)
                )
            );
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = null;
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();
            PolygonCollider2D collider = go.GetComponent<PolygonCollider2D>();

            matcher.OnValidate();
            yield return null;

            Assert.AreEqual(0, collider.pathCount);
        }

        [UnityTest]
        public IEnumerator WorksWithImageComponent()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(MatchColliderToSprite)
                )
            );
            Image image = go.GetComponent<Image>();
            image.sprite = _testSprite;
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();

            matcher.OnValidate();
            yield return null;

            Assert.IsTrue(matcher.image != null);
        }

        [UnityTest]
        public IEnumerator UpdatesOnlyWhenSpriteChanges()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(SpriteRenderer),
                    typeof(MatchColliderToSprite)
                )
            );
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = _testSprite;
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();

            matcher.OnValidate();
            yield return null;

            int eventCount = 0;
            matcher.colliderUpdated += () =>
            {
                eventCount++;
            };

            matcher.SendMessage("Update");
            yield return null;

            Assert.AreEqual(0, eventCount);

            Texture2D newTexture = new(32, 32);
            Sprite newSprite = Sprite.Create(
                newTexture,
                new Rect(0, 0, 32, 32),
                new Vector2(0.5f, 0.5f),
                100f
            );
            renderer.sprite = newSprite;

            matcher.SendMessage("Update");
            yield return null;

            Assert.Greater(eventCount, 0);
        }

        [UnityTest]
        public IEnumerator TracksLastHandledSprite()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(SpriteRenderer),
                    typeof(MatchColliderToSprite)
                )
            );
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = _testSprite;
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();

            matcher.OnValidate();
            yield return null;

            Assert.AreEqual(_testSprite, matcher._lastHandled);
        }

        [UnityTest]
        public IEnumerator SpriteOverrideProducerTakesPriority()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(SpriteRenderer),
                    typeof(MatchColliderToSprite)
                )
            );
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();

            Texture2D overrideTexture = new(16, 16);
            Sprite overrideSprite = Sprite.Create(
                overrideTexture,
                new Rect(0, 0, 16, 16),
                new Vector2(0.5f, 0.5f),
                100f
            );

            renderer.sprite = _testSprite;
            matcher.spriteOverrideProducer = () => overrideSprite;

            matcher.OnValidate();
            yield return null;

            Assert.AreEqual(overrideSprite, matcher._lastHandled);
        }

        [UnityTest]
        public IEnumerator ReturnsEarlyWithoutCollider()
        {
            GameObject go = Track(
                new GameObject("Test", typeof(SpriteRenderer), typeof(MatchColliderToSprite))
            );
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = _testSprite;
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();

            matcher.OnValidate();
            yield return null;

            Assert.IsTrue(matcher.polygonCollider == null);
        }

        [UnityTest]
        public IEnumerator ClearsColliderPointsBeforeUpdate()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(SpriteRenderer),
                    typeof(MatchColliderToSprite)
                )
            );
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = _testSprite;
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();
            PolygonCollider2D collider = go.GetComponent<PolygonCollider2D>();

            collider.SetPath(0, new[] { Vector2.zero, Vector2.one, Vector2.up });

            Assert.AreEqual(3, collider.points.Length);
            yield return null;

            Assert.AreEqual(4, collider.points.Length);
        }

        [UnityTest]
        public IEnumerator SetsPathCountFromPhysicsShapes()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(SpriteRenderer),
                    typeof(MatchColliderToSprite)
                )
            );
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = _testSprite;
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();
            PolygonCollider2D collider = go.GetComponent<PolygonCollider2D>();

            matcher.OnValidate();
            yield return null;

            int expectedPathCount = _testSprite.GetPhysicsShapeCount();
            Assert.AreEqual(expectedPathCount, collider.pathCount);
        }

        [UnityTest]
        public IEnumerator UpdatesInUpdateLoop()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(SpriteRenderer),
                    typeof(MatchColliderToSprite)
                )
            );
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();

            renderer.sprite = null;
            matcher.OnValidate();
            yield return null;

            int eventCount = 0;
            matcher.colliderUpdated += () =>
            {
                eventCount++;
            };

            renderer.sprite = _testSprite;
            matcher.SendMessage("Update");
            yield return null;

            Assert.Greater(eventCount, 0);
        }

        [UnityTest]
        public IEnumerator DoesNotUpdateWhenSpriteIsSame()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(SpriteRenderer),
                    typeof(MatchColliderToSprite)
                )
            );
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = _testSprite;
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();

            matcher.OnValidate();
            yield return null;

            int eventCount = 0;
            matcher.colliderUpdated += () =>
            {
                eventCount++;
            };

            for (int i = 0; i < 5; i++)
            {
                matcher.SendMessage("Update");
                yield return null;
            }

            Assert.AreEqual(0, eventCount);
        }

        [UnityTest]
        public IEnumerator WorksWithBothSpriteRendererAndImage()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(SpriteRenderer),
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(MatchColliderToSprite)
                )
            );
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            Image image = go.GetComponent<Image>();
            renderer.sprite = _testSprite;
            image.sprite = null;
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();

            matcher.OnValidate();
            yield return null;

            Assert.AreEqual(_testSprite, matcher._lastHandled);
        }

        [UnityTest]
        public IEnumerator MultipleUpdatesWithDifferentSprites()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(SpriteRenderer),
                    typeof(MatchColliderToSprite)
                )
            );
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();

            Texture2D tex1 = new(32, 32);
            Sprite sprite1 = Sprite.Create(tex1, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);

            Texture2D tex2 = new(16, 16);
            Sprite sprite2 = Sprite.Create(tex2, new Rect(0, 0, 16, 16), Vector2.one * 0.5f);

            renderer.sprite = sprite1;
            matcher.OnValidate();
            yield return null;

            Assert.AreEqual(sprite1, matcher._lastHandled);

            renderer.sprite = sprite2;
            matcher.SendMessage("Update");
            yield return null;

            Assert.AreEqual(sprite2, matcher._lastHandled);

            renderer.sprite = null;
            matcher.SendMessage("Update");
            yield return null;

            Assert.IsTrue(matcher._lastHandled == null);
        }

        [UnityTest]
        public IEnumerator EventInvokedEvenOnFailure()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(SpriteRenderer),
                    typeof(MatchColliderToSprite)
                )
            );
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = null;
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();

            bool eventInvoked = false;
            matcher.colliderUpdated += () =>
            {
                eventInvoked = true;
            };

            matcher.OnValidate();
            yield return null;

            Assert.IsTrue(eventInvoked);
        }
    }
}

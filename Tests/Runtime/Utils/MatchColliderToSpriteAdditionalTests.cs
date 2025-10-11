namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using UnityEngine.UI;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class MatchColliderToSpriteAdditionalTests : CommonTestBase
    {
        private Sprite _spriteWithNoShapes;

        [OneTimeSetUp]
        public void Setup()
        {
            Texture2D tex = new(8, 8);
            _spriteWithNoShapes = Sprite.Create(
                tex,
                new Rect(0, 0, 8, 8),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            if (_spriteWithNoShapes != null)
            {
                Object.Destroy(_spriteWithNoShapes.texture);
                Object.Destroy(_spriteWithNoShapes);
            }
        }

        [UnityTest]
        public IEnumerator OverrideProducerNullWinsOverComponents()
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
            renderer.sprite = _spriteWithNoShapes;
            image.sprite = _spriteWithNoShapes;

            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();
            matcher.spriteOverrideProducer = () => null;

            matcher.OnValidate();
            yield return null;

            Assert.IsTrue(matcher._lastHandled == null);
            Assert.AreEqual(0, go.GetComponent<PolygonCollider2D>().pathCount);
        }

        [UnityTest]
        public IEnumerator SpriteWithNoPhysicsShapesClearsCollider()
        {
            GameObject go = Track(
                new GameObject(
                    "Test",
                    typeof(PolygonCollider2D),
                    typeof(SpriteRenderer),
                    typeof(MatchColliderToSprite)
                )
            );
            go.GetComponent<SpriteRenderer>().sprite = _spriteWithNoShapes;
            MatchColliderToSprite matcher = go.GetComponent<MatchColliderToSprite>();

            matcher.OnValidate();
            yield return null;

            Assert.AreEqual(0, go.GetComponent<PolygonCollider2D>().pathCount);
        }
    }
}

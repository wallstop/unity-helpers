namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using UnityEngine.UI;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class MatchColliderToSpriteAdditionalTests : CommonTestBase
    {
        private Sprite _spriteWithNoShapes;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            Texture2D tex = Track(new Texture2D(8, 8));
            _spriteWithNoShapes = Track(
                Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 100f)
            );
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

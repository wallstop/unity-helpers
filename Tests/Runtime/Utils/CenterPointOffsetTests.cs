namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class CenterPointOffsetTests : CommonTestBase
    {
        [UnityTest]
        public IEnumerator CenterPointIncludesScaleAndPosition()
        {
            GameObject go = Track(
                new GameObject("Center", typeof(CenterPointOffset))
                {
                    transform =
                    {
                        position = new Vector3(10f, -5f, 0f),
                        localScale = new Vector3(2f, -3f, 1f),
                    },
                }
            );
            CenterPointOffset offset = go.GetComponent<CenterPointOffset>();
            offset.offset = new Vector2(1f, 2f);

            yield return null;

            Vector2 expected = new(10f + 1f * 2f, -5f + 2f * -3f);
            Assert.AreEqual(expected, offset.CenterPoint);
        }

        [UnityTest]
        public IEnumerator CenterPointUnaffectedBySpriteUsesOffsetFlag()
        {
            GameObject go = Track(new GameObject("Center", typeof(CenterPointOffset)));
            CenterPointOffset offset = go.GetComponent<CenterPointOffset>();
            offset.offset = new Vector2(3f, 4f);
            offset.spriteUsesOffset = false;

            yield return null;

            Vector2 center1 = offset.CenterPoint;
            offset.spriteUsesOffset = true;
            Vector2 center2 = offset.CenterPoint;
            Assert.AreEqual(center1, center2);
        }

        [UnityTest]
        public IEnumerator ZeroOffsetReturnsTransformPosition()
        {
            GameObject go = Track(
                new GameObject("Center", typeof(CenterPointOffset))
                {
                    transform = { position = new Vector3(1f, 2f, 3f) },
                }
            );
            CenterPointOffset offset = go.GetComponent<CenterPointOffset>();
            offset.offset = Vector2.zero;

            yield return null;

            Assert.AreEqual(new Vector2(1f, 2f), offset.CenterPoint);
        }
    }
}

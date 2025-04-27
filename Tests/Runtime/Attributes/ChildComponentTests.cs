namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System.Collections;
    using System.Linq;
    using Components;
    using UnityEngine;
    using UnityEngine.Assertions;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildComponentTests
    {
        [UnityTest]
        public IEnumerator Nominal()
        {
            GameObject parent = new("Parent-ChildComponentTest", typeof(SpriteRenderer));
            GameObject baseGameObject = new(
                "Base-ChildComponentTest",
                typeof(SpriteRenderer),
                typeof(ExpectChildSpriteRenderers)
            );
            baseGameObject.transform.SetParent(parent.transform);
            GameObject childLevel1 = new("ChildLevel1", typeof(SpriteRenderer));
            childLevel1.transform.SetParent(baseGameObject.transform);
            GameObject childLevel2 = new("ChildLevel2", typeof(SpriteRenderer));
            childLevel2.transform.SetParent(childLevel1.transform);
            GameObject childLevel2Point1 = new("ChildLevel2.1", typeof(SpriteRenderer));
            childLevel2Point1.transform.SetParent(childLevel1.transform);

            ExpectChildSpriteRenderers expect =
                baseGameObject.GetComponent<ExpectChildSpriteRenderers>();
            expect.AssignChildComponents();

            Assert.AreEqual(4, expect.exclusiveChildrenArray.Length);
            Assert.AreEqual(4, expect.exclusiveChildrenList.Count);
            Assert.IsTrue(
                expect.exclusiveChildrenList.Contains(baseGameObject.GetComponent<SpriteRenderer>())
            );
            Assert.IsTrue(
                expect.exclusiveChildrenList.Contains(childLevel1.GetComponent<SpriteRenderer>())
            );
            Assert.IsTrue(
                expect.exclusiveChildrenList.Contains(childLevel2.GetComponent<SpriteRenderer>())
            );
            Assert.IsTrue(
                expect.exclusiveChildrenList.Contains(
                    childLevel2Point1.GetComponent<SpriteRenderer>()
                )
            );
            Assert.IsTrue(
                expect.exclusiveChildrenList.ToHashSet().SetEquals(expect.exclusiveChildrenArray)
            );

            Assert.AreEqual(3, expect.inclusiveChildrenArray.Length);
            Assert.AreEqual(3, expect.inclusiveChildrenList.Count);

            Assert.IsTrue(
                expect.inclusiveChildrenList.Contains(childLevel1.GetComponent<SpriteRenderer>())
            );
            Assert.IsTrue(
                expect.inclusiveChildrenList.Contains(childLevel2.GetComponent<SpriteRenderer>())
            );
            Assert.IsTrue(
                expect.inclusiveChildrenList.Contains(
                    childLevel2Point1.GetComponent<SpriteRenderer>()
                )
            );
            Assert.IsTrue(
                expect.inclusiveChildrenList.ToHashSet().SetEquals(expect.inclusiveChildrenArray)
            );

            Assert.IsTrue(expect.exclusiveChild != null);
            Assert.AreEqual(expect.GetComponent<SpriteRenderer>(), expect.exclusiveChild);

            Assert.IsTrue(expect.inclusiveChild != null);
            Assert.AreEqual(childLevel1.GetComponent<SpriteRenderer>(), expect.inclusiveChild);

            yield break;
        }
    }
}

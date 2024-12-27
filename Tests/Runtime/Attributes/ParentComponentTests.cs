namespace UnityHelpers.Tests.Attributes
{
    using System.Collections;
    using System.Linq;
    using Components;
    using Core.Attributes;
    using UnityEngine;
    using UnityEngine.Assertions;
    using UnityEngine.TestTools;

    public sealed class ParentComponentTests
    {
        [UnityTest]
        public IEnumerator Nominal()
        {
            GameObject root = new("PartComponentTest - Root", typeof(SpriteRenderer));
            GameObject parentLevel1 = new("ParentLevel1", typeof(SpriteRenderer));
            parentLevel1.transform.SetParent(root.transform);
            GameObject parentLevel2 = new("ParentLevel2", typeof(SpriteRenderer));
            parentLevel2.transform.SetParent(parentLevel1.transform);
            GameObject parentLevel3 = new(
                "ParentLevel3",
                typeof(SpriteRenderer),
                typeof(ExpectParentSpriteRenderers)
            );
            parentLevel3.transform.SetParent(parentLevel2.transform);

            ExpectParentSpriteRenderers expect =
                parentLevel3.GetComponent<ExpectParentSpriteRenderers>();
            expect.AssignParentComponents();

            Assert.AreEqual(4, expect.exclusiveParentList.Count);
            Assert.IsTrue(
                expect.exclusiveParentList.Contains(parentLevel3.GetComponent<SpriteRenderer>())
            );
            Assert.IsTrue(
                expect.exclusiveParentList.Contains(parentLevel2.GetComponent<SpriteRenderer>())
            );
            Assert.IsTrue(
                expect.exclusiveParentList.Contains(parentLevel1.GetComponent<SpriteRenderer>())
            );
            Assert.IsTrue(expect.exclusiveParentList.Contains(root.GetComponent<SpriteRenderer>()));
            Assert.IsTrue(
                expect.exclusiveParentList.ToHashSet().SetEquals(expect.exclusiveParentArray)
            );

            Assert.AreEqual(3, expect.inclusiveParentList.Count);
            Assert.IsTrue(
                expect.inclusiveParentList.Contains(parentLevel2.GetComponent<SpriteRenderer>())
            );
            Assert.IsTrue(
                expect.inclusiveParentList.Contains(parentLevel1.GetComponent<SpriteRenderer>())
            );
            Assert.IsTrue(expect.inclusiveParentList.Contains(root.GetComponent<SpriteRenderer>()));
            Assert.IsTrue(
                expect.inclusiveParentList.ToHashSet().SetEquals(expect.inclusiveParentArray)
            );

            Assert.IsTrue(expect.exclusiveParent != null);
            Assert.AreEqual(expect.GetComponent<SpriteRenderer>(), expect.exclusiveParent);

            Assert.IsTrue(expect.inclusiveParent != null);
            Assert.AreEqual(parentLevel2.GetComponent<SpriteRenderer>(), expect.inclusiveParent);

            yield break;
        }
    }
}

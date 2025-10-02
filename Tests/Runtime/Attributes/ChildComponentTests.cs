namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Components;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [TestFixture]
    public sealed class ChildComponentTests
    {
        private readonly List<Object> _spawned = new();

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (Object spawned in _spawned)
            {
                if (spawned != null)
                {
                    Object.Destroy(spawned);
                    yield return null;
                }
            }
            _spawned.Clear();
        }

        [UnityTest]
        public IEnumerator Nominal()
        {
            GameObject parent = new("Parent-ChildComponentTest", typeof(SpriteRenderer));
            _spawned.Add(parent);
            GameObject baseGameObject = new(
                "Base-ChildComponentTest",
                typeof(SpriteRenderer),
                typeof(ExpectChildSpriteRenderers)
            );
            _spawned.Add(baseGameObject);
            baseGameObject.transform.SetParent(parent.transform);
            GameObject childLevel1 = new("ChildLevel1", typeof(SpriteRenderer));
            _spawned.Add(childLevel1);
            childLevel1.transform.SetParent(baseGameObject.transform);
            GameObject childLevel2 = new("ChildLevel2", typeof(SpriteRenderer));
            _spawned.Add(childLevel2);
            childLevel2.transform.SetParent(childLevel1.transform);
            GameObject childLevel2Point1 = new("ChildLevel2.1", typeof(SpriteRenderer));
            _spawned.Add(childLevel2Point1);
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

        [UnityTest]
        public IEnumerator IncludeInactiveFalseSkipsInactiveDescendents()
        {
            GameObject root = new("Child-InactiveRoot", typeof(ChildAssignmentTester));
            _spawned.Add(root);
            ChildAssignmentTester tester = root.GetComponent<ChildAssignmentTester>();

            GameObject activeChild = new("ActiveChild", typeof(SpriteRenderer));
            _spawned.Add(activeChild);
            activeChild.transform.SetParent(root.transform);
            GameObject inactiveChild = new("InactiveChild", typeof(SpriteRenderer));
            _spawned.Add(inactiveChild);
            inactiveChild.transform.SetParent(root.transform);
            inactiveChild.SetActive(false);

            tester.AssignChildComponents();

            Assert.AreSame(activeChild.GetComponent<SpriteRenderer>(), tester.activeOnly);
            CollectionAssert.AreEquivalent(
                new[] { activeChild.GetComponent<SpriteRenderer>() },
                tester.descendentsActiveOnlyList
            );

            CollectionAssert.AreEquivalent(
                new[] { activeChild.GetComponent<SpriteRenderer>() },
                tester.descendentsActiveOnlyArray
            );

            CollectionAssert.AreEquivalent(
                new[]
                {
                    activeChild.GetComponent<SpriteRenderer>(),
                    inactiveChild.GetComponent<SpriteRenderer>(),
                },
                tester.descendentsAllArray
            );

            CollectionAssert.AreEquivalent(
                new[]
                {
                    activeChild.GetComponent<SpriteRenderer>(),
                    inactiveChild.GetComponent<SpriteRenderer>(),
                },
                tester.descendentsAllList
            );

            yield break;
        }

        [UnityTest]
        public IEnumerator MissingRequiredChildLogsError()
        {
            GameObject root = new("Child-Missing", typeof(ChildMissingTester));
            _spawned.Add(root);
            ChildMissingTester tester = root.GetComponent<ChildMissingTester>();

            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|Child-Missing\[ChildMissingTester\]\|Unable to find child component of type UnityEngine\.SpriteRenderer$"
                )
            );

            tester.AssignChildComponents();

            Assert.IsNull(tester.requiredRenderer);

            yield break;
        }

        [UnityTest]
        public IEnumerator SkipIfAssignedPreservesExistingValues()
        {
            GameObject root = new("ChildSkipIfAssigned", typeof(ChildSkipIfAssignedTester));
            _spawned.Add(root);
            ChildSkipIfAssignedTester tester = root.GetComponent<ChildSkipIfAssignedTester>();
            SpriteRenderer rootRenderer = root.AddComponent<SpriteRenderer>();

            GameObject child = new("Child", typeof(SpriteRenderer));
            _spawned.Add(child);
            child.transform.SetParent(root.transform);
            SpriteRenderer childRenderer = child.GetComponent<SpriteRenderer>();

            // Pre-assign values that should NOT be overwritten
            tester.preAssignedChild = rootRenderer;
            tester.preAssignedChildArray = new SpriteRenderer[] { rootRenderer };
            tester.preAssignedChildList = new List<SpriteRenderer> { rootRenderer };

            // Call assignment
            tester.AssignChildComponents();

            // Verify pre-assigned values were preserved (skipIfAssigned = true)
            Assert.AreSame(rootRenderer, tester.preAssignedChild);
            Assert.AreEqual(1, tester.preAssignedChildArray.Length);
            Assert.AreSame(rootRenderer, tester.preAssignedChildArray[0]);
            Assert.AreEqual(1, tester.preAssignedChildList.Count);
            Assert.AreSame(rootRenderer, tester.preAssignedChildList[0]);

            // Verify normal assignments (without skipIfAssigned) were assigned
            Assert.AreSame(rootRenderer, tester.normalChild);

            yield break;
        }
    }

    internal sealed class ChildAssignmentTester : MonoBehaviour
    {
        [ChildComponent(onlyDescendents = true, includeInactive = false)]
        public SpriteRenderer activeOnly;

        [ChildComponent(onlyDescendents = true, includeInactive = true)]
        public SpriteRenderer inactive;

        [ChildComponent(onlyDescendents = true, includeInactive = false)]
        public List<SpriteRenderer> descendentsActiveOnlyList;

        [ChildComponent(onlyDescendents = true, includeInactive = true)]
        public List<SpriteRenderer> descendentsAllList;

        [ChildComponent(onlyDescendents = true, includeInactive = false)]
        public SpriteRenderer[] descendentsActiveOnlyArray;

        [ChildComponent(onlyDescendents = true, includeInactive = true)]
        public SpriteRenderer[] descendentsAllArray;
    }

    internal sealed class ChildMissingTester : MonoBehaviour
    {
        [ChildComponent(onlyDescendents = true)]
        public SpriteRenderer requiredRenderer;
    }

    internal sealed class ChildSkipIfAssignedTester : MonoBehaviour
    {
        [ChildComponent(skipIfAssigned = true)]
        public SpriteRenderer preAssignedChild;

        [ChildComponent(skipIfAssigned = true)]
        public SpriteRenderer[] preAssignedChildArray;

        [ChildComponent(skipIfAssigned = true)]
        public List<SpriteRenderer> preAssignedChildList;

        [ChildComponent]
        public SpriteRenderer normalChild;
    }
}

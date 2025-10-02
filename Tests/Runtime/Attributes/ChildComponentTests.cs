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

        [UnityTest]
        public IEnumerator SkipIfAssignedDoesNotSkipEmptyCollections()
        {
            GameObject root = new("ChildSkipEmpty", typeof(ChildSkipIfAssignedTester));
            _spawned.Add(root);
            ChildSkipIfAssignedTester tester = root.GetComponent<ChildSkipIfAssignedTester>();
            SpriteRenderer rootRenderer = root.AddComponent<SpriteRenderer>();

            GameObject child = new("Child", typeof(SpriteRenderer));
            _spawned.Add(child);
            child.transform.SetParent(root.transform);

            // Pre-assign EMPTY collections (should be overwritten)
            tester.preAssignedChildArray = new SpriteRenderer[0];
            tester.preAssignedChildList = new List<SpriteRenderer>();

            tester.AssignChildComponents();

            // Empty collections should have been overwritten
            Assert.AreEqual(2, tester.preAssignedChildArray.Length);
            Assert.AreEqual(2, tester.preAssignedChildList.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator SkipIfAssignedWithNullUnityObjectStillAssigns()
        {
            GameObject root = new("ChildSkipNull", typeof(ChildSkipIfAssignedTester));
            _spawned.Add(root);
            ChildSkipIfAssignedTester tester = root.GetComponent<ChildSkipIfAssignedTester>();
            SpriteRenderer rootRenderer = root.AddComponent<SpriteRenderer>();

            // Explicitly set to null (destroyed Unity object)
            tester.preAssignedChild = null;

            tester.AssignChildComponents();

            // Null Unity object should have been reassigned
            Assert.AreSame(rootRenderer, tester.preAssignedChild);

            yield break;
        }

        [UnityTest]
        public IEnumerator OptionalChildDoesNotLogErrorWhenMissing()
        {
            GameObject root = new("ChildOptional", typeof(ChildOptionalTester));
            _spawned.Add(root);
            ChildOptionalTester tester = root.GetComponent<ChildOptionalTester>();

            // Should NOT log error for optional component
            tester.AssignChildComponents();

            Assert.IsNull(tester.optionalRenderer);
            yield break;
        }

        [UnityTest]
        public IEnumerator OnlyDescendentsExcludesSelf()
        {
            GameObject root = new("ChildOnlyDescendents", typeof(SpriteRenderer));
            _spawned.Add(root);
            SpriteRenderer rootRenderer = root.GetComponent<SpriteRenderer>();
            ChildOnlyDescendentsTester tester = root.AddComponent<ChildOnlyDescendentsTester>();

            GameObject child = new("Child", typeof(SpriteRenderer));
            _spawned.Add(child);
            child.transform.SetParent(root.transform);
            SpriteRenderer childRenderer = child.GetComponent<SpriteRenderer>();

            tester.AssignChildComponents();

            // onlyDescendents=true should exclude self
            Assert.AreSame(childRenderer, tester.descendentOnly);
            CollectionAssert.AreEquivalent(new[] { childRenderer }, tester.descendentOnlyArray);

            // onlyDescendents=false should include self
            Assert.AreSame(rootRenderer, tester.includeSelf);
            CollectionAssert.AreEquivalent(
                new[] { rootRenderer, childRenderer },
                tester.includeSelfArray
            );

            yield break;
        }

        [UnityTest]
        public IEnumerator OnlyDescendentsWithNoChildrenReturnsNothing()
        {
            GameObject root = new("ChildNoDescendents", typeof(ChildOnlyDescendentsTester));
            _spawned.Add(root);
            ChildOnlyDescendentsTester tester = root.GetComponent<ChildOnlyDescendentsTester>();

            // Expect error for descendentOnly (SpriteRenderer)
            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|ChildNoDescendents\[ChildOnlyDescendentsTester\]\|Unable to find child component of type UnityEngine\.SpriteRenderer$"
                )
            );

            // Expect error for descendentOnlyArray (SpriteRenderer[])
            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|ChildNoDescendents\[ChildOnlyDescendentsTester\]\|Unable to find child component of type UnityEngine\.SpriteRenderer\[\]$"
                )
            );

            // Expect error for includeSelf (SpriteRenderer) - no SpriteRenderer on root
            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|ChildNoDescendents\[ChildOnlyDescendentsTester\]\|Unable to find child component of type UnityEngine\.SpriteRenderer$"
                )
            );

            // Expect error for includeSelfArray (SpriteRenderer[]) - no SpriteRenderer on root
            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|ChildNoDescendents\[ChildOnlyDescendentsTester\]\|Unable to find child component of type UnityEngine\.SpriteRenderer\[\]$"
                )
            );

            tester.AssignChildComponents();

            Assert.IsNull(tester.descendentOnly);
            Assert.AreEqual(0, tester.descendentOnlyArray.Length);

            yield break;
        }

        [UnityTest]
        public IEnumerator DeepHierarchyHandledCorrectly()
        {
            GameObject root = new("ChildDeepRoot", typeof(SpriteRenderer));
            _spawned.Add(root);
            ChildMultipleTester tester = root.AddComponent<ChildMultipleTester>();
            GameObject current = root;

            // Create deep hierarchy (10 levels)
            for (int i = 0; i < 10; i++)
            {
                GameObject next = new($"ChildDeepLevel{i}", typeof(SpriteRenderer));
                _spawned.Add(next);
                next.transform.SetParent(current.transform);
                current = next;
            }

            tester.AssignChildComponents();

            // Should find all 11 children (including self)
            Assert.AreEqual(11, tester.allChildren.Length);
            Assert.AreEqual(11, tester.allChildrenList.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator BreadthFirstSearchOrderVerified()
        {
            GameObject root = new("ChildBFSRoot", typeof(SpriteRenderer));
            _spawned.Add(root);
            ChildMultipleTester tester = root.AddComponent<ChildMultipleTester>();

            GameObject child1 = new("Child1", typeof(SpriteRenderer));
            _spawned.Add(child1);
            child1.transform.SetParent(root.transform);

            GameObject child2 = new("Child2", typeof(SpriteRenderer));
            _spawned.Add(child2);
            child2.transform.SetParent(root.transform);

            GameObject grandchild1 = new("Grandchild1", typeof(SpriteRenderer));
            _spawned.Add(grandchild1);
            grandchild1.transform.SetParent(child1.transform);

            tester.AssignChildComponents();

            // BFS order should be: root, child1, child2, grandchild1
            Assert.AreEqual(4, tester.allChildren.Length);
            Assert.AreSame(root.GetComponent<SpriteRenderer>(), tester.allChildren[0]);
            // child1 and child2 order depends on Unity's internal ordering
            // grandchild1 should be after both children
            Assert.AreSame(grandchild1.GetComponent<SpriteRenderer>(), tester.allChildren[3]);

            yield break;
        }

        [UnityTest]
        public IEnumerator InactiveGameObjectExcludedWhenIncludeInactiveFalse()
        {
            GameObject root = new("ChildInactiveRoot", typeof(ChildInactiveTester));
            _spawned.Add(root);
            ChildInactiveTester tester = root.GetComponent<ChildInactiveTester>();

            GameObject activeChild = new("ActiveChild", typeof(SpriteRenderer));
            _spawned.Add(activeChild);
            activeChild.transform.SetParent(root.transform);

            GameObject inactiveChild = new("InactiveChild", typeof(SpriteRenderer));
            _spawned.Add(inactiveChild);
            inactiveChild.transform.SetParent(root.transform);
            inactiveChild.SetActive(false);

            tester.AssignChildComponents();

            // includeInactive=false should skip inactive child
            Assert.AreSame(activeChild.GetComponent<SpriteRenderer>(), tester.activeOnly);
            CollectionAssert.AreEquivalent(
                new[] { activeChild.GetComponent<SpriteRenderer>() },
                tester.activeOnlyArray
            );

            // includeInactive=true should include both
            CollectionAssert.AreEquivalent(
                new[]
                {
                    activeChild.GetComponent<SpriteRenderer>(),
                    inactiveChild.GetComponent<SpriteRenderer>(),
                },
                tester.includeInactiveArray
            );

            yield break;
        }

        [UnityTest]
        public IEnumerator DisabledBehaviourExcludedWhenIncludeInactiveFalse()
        {
            GameObject root = new("ChildDisabledRoot", typeof(ChildDisabledBehaviourTester));
            _spawned.Add(root);
            ChildDisabledBehaviourTester tester = root.GetComponent<ChildDisabledBehaviourTester>();

            GameObject child = new("ChildWithDisabled", typeof(BoxCollider));
            _spawned.Add(child);
            child.transform.SetParent(root.transform);
            BoxCollider childCollider = child.GetComponent<BoxCollider>();
            childCollider.enabled = false;
            yield return null;

            // Expect error logs for fields with includeInactive=false when disabled Behaviour is present
            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    "Unable to find child component of type UnityEngine.BoxCollider$"
                )
            );
            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    "Unable to find child component of type UnityEngine.BoxCollider\\[\\]$"
                )
            );

            tester.AssignChildComponents();

            // includeInactive=false should exclude disabled Behaviour
            Assert.IsTrue(tester.activeOnly == null);
            Assert.AreEqual(0, tester.activeOnlyArray.Length);

            // includeInactive=true should include disabled Behaviour
            Assert.AreSame(childCollider, tester.includeInactive);
            CollectionAssert.AreEquivalent(new[] { childCollider }, tester.includeInactiveArray);

            yield break;
        }

        [UnityTest]
        public IEnumerator MultipleChildComponentsOnSameGameObject()
        {
            GameObject root = new("ChildMultiRoot", typeof(ChildMultiComponentTester));
            _spawned.Add(root);
            ChildMultiComponentTester tester = root.GetComponent<ChildMultiComponentTester>();

            GameObject child = new("Child");
            _spawned.Add(child);
            child.transform.SetParent(root.transform);
            BoxCollider first = child.AddComponent<BoxCollider>();
            BoxCollider second = child.AddComponent<BoxCollider>();
            BoxCollider third = child.AddComponent<BoxCollider>();

            tester.AssignChildComponents();

            // Should find all three colliders on the same child
            Assert.AreEqual(3, tester.colliders.Length);
            CollectionAssert.Contains(tester.colliders, first);
            CollectionAssert.Contains(tester.colliders, second);
            CollectionAssert.Contains(tester.colliders, third);

            yield break;
        }

        [UnityTest]
        public IEnumerator ComplexHierarchyWithMultipleBranches()
        {
            GameObject root = new("ChildComplexRoot", typeof(SpriteRenderer));
            _spawned.Add(root);
            ChildMultipleTester tester = root.AddComponent<ChildMultipleTester>();

            // Branch 1: 3 levels deep
            GameObject branch1L1 = new("Branch1L1", typeof(SpriteRenderer));
            _spawned.Add(branch1L1);
            branch1L1.transform.SetParent(root.transform);
            GameObject branch1L2 = new("Branch1L2", typeof(SpriteRenderer));
            _spawned.Add(branch1L2);
            branch1L2.transform.SetParent(branch1L1.transform);
            GameObject branch1L3 = new("Branch1L3", typeof(SpriteRenderer));
            _spawned.Add(branch1L3);
            branch1L3.transform.SetParent(branch1L2.transform);

            // Branch 2: 2 levels deep
            GameObject branch2L1 = new("Branch2L1", typeof(SpriteRenderer));
            _spawned.Add(branch2L1);
            branch2L1.transform.SetParent(root.transform);
            GameObject branch2L2 = new("Branch2L2", typeof(SpriteRenderer));
            _spawned.Add(branch2L2);
            branch2L2.transform.SetParent(branch2L1.transform);

            tester.AssignChildComponents();

            // Should find root + 5 descendants = 6 total
            Assert.AreEqual(6, tester.allChildren.Length);
            Assert.AreEqual(6, tester.allChildrenList.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator CacheIsolationBetweenDifferentComponentTypes()
        {
            GameObject root = new("ChildCacheRoot", typeof(SpriteRenderer));
            _spawned.Add(root);
            ChildCacheIsolationTesterA testerA = root.AddComponent<ChildCacheIsolationTesterA>();
            ChildCacheIsolationTesterB testerB = root.AddComponent<ChildCacheIsolationTesterB>();

            testerA.AssignChildComponents();
            testerB.AssignChildComponents();

            // Both should have their own cached field info
            Assert.IsNotNull(testerA.childRenderer);
            Assert.IsNotNull(testerB.childRenderer);
            Assert.AreSame(testerA.childRenderer, testerB.childRenderer);

            yield break;
        }

        [UnityTest]
        public IEnumerator RepeatedAssignmentsAreIdempotent()
        {
            GameObject root = new("ChildIdempotentRoot", typeof(SpriteRenderer));
            _spawned.Add(root);
            ChildMultipleTester tester = root.AddComponent<ChildMultipleTester>();

            GameObject child = new("Child", typeof(SpriteRenderer));
            _spawned.Add(child);
            child.transform.SetParent(root.transform);

            tester.AssignChildComponents();
            SpriteRenderer[] firstAssignment = tester.allChildren;

            tester.AssignChildComponents();
            SpriteRenderer[] secondAssignment = tester.allChildren;

            // Repeated calls should produce same results
            CollectionAssert.AreEqual(firstAssignment, secondAssignment);

            yield break;
        }

        [UnityTest]
        public IEnumerator ChildComponentWithMixedActiveStatesInHierarchy()
        {
            GameObject root = new("ChildMixedRoot", typeof(ChildInactiveTester));
            _spawned.Add(root);
            ChildInactiveTester tester = root.GetComponent<ChildInactiveTester>();

            GameObject activeParent = new("ActiveParent", typeof(SpriteRenderer));
            _spawned.Add(activeParent);
            activeParent.transform.SetParent(root.transform);

            GameObject inactiveChild = new("InactiveChild", typeof(SpriteRenderer));
            _spawned.Add(inactiveChild);
            inactiveChild.transform.SetParent(activeParent.transform);
            inactiveChild.SetActive(false);

            GameObject inactiveParent = new("InactiveParent", typeof(SpriteRenderer));
            _spawned.Add(inactiveParent);
            inactiveParent.transform.SetParent(root.transform);
            inactiveParent.SetActive(false);

            GameObject childOfInactive = new("ChildOfInactive", typeof(SpriteRenderer));
            _spawned.Add(childOfInactive);
            childOfInactive.transform.SetParent(inactiveParent.transform);

            tester.AssignChildComponents();

            // activeOnly should only find activeParent
            CollectionAssert.AreEquivalent(
                new[] { activeParent.GetComponent<SpriteRenderer>() },
                tester.activeOnlyArray
            );

            // includeInactive should find all
            Assert.AreEqual(4, tester.includeInactiveArray.Length);

            yield break;
        }

        [UnityTest]
        public IEnumerator ChildComponentFindsFirstMatchInBFSOrder()
        {
            GameObject root = new("ChildFirstMatch", typeof(SpriteRenderer));
            _spawned.Add(root);
            ChildSingleTester tester = root.AddComponent<ChildSingleTester>();

            GameObject child1 = new("Child1", typeof(SpriteRenderer));
            _spawned.Add(child1);
            child1.transform.SetParent(root.transform);

            GameObject child2 = new("Child2", typeof(SpriteRenderer));
            _spawned.Add(child2);
            child2.transform.SetParent(root.transform);

            tester.AssignChildComponents();

            // Should find root first (self is included by default)
            Assert.AreSame(root.GetComponent<SpriteRenderer>(), tester.single);

            yield break;
        }

        [UnityTest]
        public IEnumerator ChildComponentHandlesEmptyHierarchy()
        {
            GameObject root = new("ChildEmpty", typeof(ChildOptionalTester));
            _spawned.Add(root);
            ChildOptionalTester tester = root.GetComponent<ChildOptionalTester>();

            // No children, no SpriteRenderer on root
            tester.AssignChildComponents();

            Assert.IsNull(tester.optionalRenderer);
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

    internal sealed class ChildOptionalTester : MonoBehaviour
    {
        [ChildComponent(optional = true)]
        public SpriteRenderer optionalRenderer;
    }

    internal sealed class ChildOnlyDescendentsTester : MonoBehaviour
    {
        [ChildComponent(onlyDescendents = true)]
        public SpriteRenderer descendentOnly;

        [ChildComponent(onlyDescendents = true)]
        public SpriteRenderer[] descendentOnlyArray;

        [ChildComponent(onlyDescendents = false)]
        public SpriteRenderer includeSelf;

        [ChildComponent(onlyDescendents = false)]
        public SpriteRenderer[] includeSelfArray;
    }

    internal sealed class ChildMultipleTester : MonoBehaviour
    {
        [ChildComponent(includeInactive = true)]
        public SpriteRenderer[] allChildren;

        [ChildComponent(includeInactive = true)]
        public List<SpriteRenderer> allChildrenList;
    }

    internal sealed class ChildInactiveTester : MonoBehaviour
    {
        [ChildComponent(onlyDescendents = true, includeInactive = false)]
        public SpriteRenderer activeOnly;

        [ChildComponent(onlyDescendents = true, includeInactive = false)]
        public SpriteRenderer[] activeOnlyArray;

        [ChildComponent(onlyDescendents = true, includeInactive = true)]
        public SpriteRenderer[] includeInactiveArray;
    }

    internal sealed class ChildDisabledBehaviourTester : MonoBehaviour
    {
        [ChildComponent(onlyDescendents = true, includeInactive = false)]
        public BoxCollider activeOnly;

        [ChildComponent(onlyDescendents = true, includeInactive = false)]
        public BoxCollider[] activeOnlyArray;

        [ChildComponent(onlyDescendents = true, includeInactive = true)]
        public BoxCollider includeInactive;

        [ChildComponent(onlyDescendents = true, includeInactive = true)]
        public BoxCollider[] includeInactiveArray;
    }

    internal sealed class ChildMultiComponentTester : MonoBehaviour
    {
        [ChildComponent(onlyDescendents = true)]
        public BoxCollider[] colliders;
    }

    internal sealed class ChildCacheIsolationTesterA : MonoBehaviour
    {
        [ChildComponent]
        public SpriteRenderer childRenderer;
    }

    internal sealed class ChildCacheIsolationTesterB : MonoBehaviour
    {
        [ChildComponent]
        public SpriteRenderer childRenderer;
    }

    internal sealed class ChildSingleTester : MonoBehaviour
    {
        [ChildComponent]
        public SpriteRenderer single;
    }
}

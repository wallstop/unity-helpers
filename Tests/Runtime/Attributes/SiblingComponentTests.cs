namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using Object = UnityEngine.Object;

    [TestFixture]
    public sealed class SiblingComponentTests
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
        public IEnumerator AssignSiblingComponentsPopulatesSupportedFieldShapes()
        {
            GameObject root = new("SiblingAssignments");
            _spawned.Add(root);
            BoxCollider first = root.AddComponent<BoxCollider>();
            BoxCollider second = root.AddComponent<BoxCollider>();
            SiblingAssignmentComponent tester = root.AddComponent<SiblingAssignmentComponent>();

            tester.AssignSiblingComponents();

            Assert.AreSame(first, tester.single);

            CollectionAssert.AreEquivalent(new[] { first, second }, tester.array);
            CollectionAssert.AreEquivalent(new[] { first, second }, tester.list);

            Assert.IsTrue(tester.optional == null);
            yield break;
        }

        [UnityTest]
        public IEnumerator AssignSiblingComponentsLogsErrorWhenRequiredSiblingMissing()
        {
            GameObject root = new("SiblingMissing", typeof(SiblingMissingComponent));
            _spawned.Add(root);
            SiblingMissingComponent tester = root.GetComponent<SiblingMissingComponent>();

            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|SiblingMissing\[SiblingMissingComponent\]\|Unable to find sibling component of type UnityEngine\.Rigidbody for field 'required'$"
                )
            );

            tester.AssignSiblingComponents();

            Assert.IsTrue(tester.required == null);
            yield break;
        }

        [UnityTest]
        public IEnumerator SkipIfAssignedPreservesExistingValues()
        {
            GameObject root = new("SiblingSkipIfAssigned");
            _spawned.Add(root);
            BoxCollider first = root.AddComponent<BoxCollider>();
            BoxCollider second = root.AddComponent<BoxCollider>();
            SiblingSkipIfAssignedTester tester = root.AddComponent<SiblingSkipIfAssignedTester>();

            // Pre-assign values that should NOT be overwritten
            tester.preAssignedSibling = second;
            tester.preAssignedSiblingArray = new BoxCollider[] { second };
            tester.preAssignedSiblingList = new List<BoxCollider> { second };

            // Call assignment
            tester.AssignSiblingComponents();

            // Verify pre-assigned values were preserved (SkipIfAssigned = true)
            Assert.AreSame(second, tester.preAssignedSibling);
            Assert.AreEqual(1, tester.preAssignedSiblingArray.Length);
            Assert.AreSame(second, tester.preAssignedSiblingArray[0]);
            Assert.AreEqual(1, tester.preAssignedSiblingList.Count);
            Assert.AreSame(second, tester.preAssignedSiblingList[0]);

            // Verify normal assignments (without skipIfAssigned) were assigned
            Assert.AreSame(first, tester.normalSibling);

            yield break;
        }

        [UnityTest]
        public IEnumerator SkipIfAssignedDoesNotSkipEmptyCollections()
        {
            GameObject root = new("SiblingSkipEmpty");
            _spawned.Add(root);
            _ = root.AddComponent<BoxCollider>();
            SiblingSkipIfAssignedTester tester = root.AddComponent<SiblingSkipIfAssignedTester>();

            // Pre-assign EMPTY collections (should be overwritten)
            tester.preAssignedSiblingArray = Array.Empty<BoxCollider>();
            tester.preAssignedSiblingList = new List<BoxCollider>();

            tester.AssignSiblingComponents();

            // Empty collections should have been overwritten
            Assert.AreEqual(1, tester.preAssignedSiblingArray.Length);
            Assert.AreEqual(1, tester.preAssignedSiblingList.Count);
            yield break;
        }

        [UnityTest]
        public IEnumerator SkipIfAssignedWithNullUnityObjectStillAssigns()
        {
            GameObject root = new("SiblingSkipNull");
            _spawned.Add(root);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            SiblingSkipIfAssignedTester tester = root.AddComponent<SiblingSkipIfAssignedTester>();

            // Explicitly set to null (destroyed Unity object)
            tester.preAssignedSibling = null;

            tester.AssignSiblingComponents();

            // Null Unity object should have been reassigned
            Assert.AreSame(collider, tester.preAssignedSibling);

            yield break;
        }

        [UnityTest]
        public IEnumerator OptionalSiblingDoesNotLogErrorWhenMissing()
        {
            GameObject root = new("SiblingOptional", typeof(SiblingOptionalTester));
            _spawned.Add(root);
            SiblingOptionalTester tester = root.GetComponent<SiblingOptionalTester>();

            // Should NOT log error for optional component
            tester.AssignSiblingComponents();

            Assert.IsTrue(tester.optionalCollider == null);
            yield break;
        }

        [UnityTest]
        public IEnumerator MultipleSiblingComponentsOfSameType()
        {
            GameObject root = new("SiblingMultiple");
            _spawned.Add(root);
            BoxCollider first = root.AddComponent<BoxCollider>();
            BoxCollider second = root.AddComponent<BoxCollider>();
            BoxCollider third = root.AddComponent<BoxCollider>();
            SiblingMultipleTester tester = root.AddComponent<SiblingMultipleTester>();

            tester.AssignSiblingComponents();

            // Single should return first one found
            Assert.IsTrue(tester.single != null);
            Assert.IsTrue(
                tester.single == first || tester.single == second || tester.single == third
            );

            // Array and List should contain all instances
            Assert.AreEqual(3, tester.array.Length);
            Assert.AreEqual(3, tester.list.Count);
            CollectionAssert.Contains(tester.array, first);
            CollectionAssert.Contains(tester.array, second);
            CollectionAssert.Contains(tester.array, third);
            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingComponentIncludesSelf()
        {
            GameObject root = new("SiblingSelf", typeof(SpriteRenderer));
            _spawned.Add(root);
            SpriteRenderer selfRenderer = root.GetComponent<SpriteRenderer>();
            SiblingSelfInclusionTester tester = root.AddComponent<SiblingSelfInclusionTester>();

            tester.AssignSiblingComponents();

            // Sibling search should include the component itself
            Assert.AreSame(selfRenderer, tester.siblingRenderer);
            CollectionAssert.AreEquivalent(new[] { selfRenderer }, tester.rendererArray);
            CollectionAssert.AreEquivalent(new[] { selfRenderer }, tester.rendererList);

            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingComponentExcludesOtherGameObjects()
        {
            GameObject root = new("SiblingExclude");
            _spawned.Add(root);
            BoxCollider rootCollider = root.AddComponent<BoxCollider>();
            SiblingExclusionTester tester = root.AddComponent<SiblingExclusionTester>();

            GameObject child = new("SiblingChild", typeof(BoxCollider));
            _spawned.Add(child);
            child.transform.SetParent(root.transform);

            GameObject sibling = new("SiblingSibling", typeof(BoxCollider));
            _spawned.Add(sibling);
            sibling.transform.SetParent(root.transform.parent);

            tester.AssignSiblingComponents();

            // Should only find components on the same GameObject
            Assert.AreEqual(1, tester.colliders.Length);
            CollectionAssert.Contains(tester.colliders, rootCollider);
            CollectionAssert.DoesNotContain(tester.colliders, child.GetComponent<BoxCollider>());
            CollectionAssert.DoesNotContain(tester.colliders, sibling.GetComponent<BoxCollider>());
            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingComponentWithOnlyOneComponent()
        {
            GameObject root = new("SiblingOne", typeof(BoxCollider));
            _spawned.Add(root);
            BoxCollider collider = root.GetComponent<BoxCollider>();
            SiblingOneTester tester = root.AddComponent<SiblingOneTester>();

            tester.AssignSiblingComponents();

            Assert.AreSame(collider, tester.single);
            CollectionAssert.AreEquivalent(new[] { collider }, tester.array);
            CollectionAssert.AreEquivalent(new[] { collider }, tester.list);

            yield break;
        }

        [UnityTest]
        public IEnumerator CacheIsolationBetweenDifferentComponentTypes()
        {
            GameObject root = new("SiblingCache", typeof(BoxCollider));
            _spawned.Add(root);
            SiblingCacheIsolationTesterA testerA =
                root.AddComponent<SiblingCacheIsolationTesterA>();
            SiblingCacheIsolationTesterB testerB =
                root.AddComponent<SiblingCacheIsolationTesterB>();
            BoxCollider collider = root.GetComponent<BoxCollider>();

            testerA.AssignSiblingComponents();
            testerB.AssignSiblingComponents();

            // Both should have their own cached field info
            Assert.AreSame(collider, testerA.siblingCollider);
            Assert.AreSame(collider, testerB.siblingCollider);

            yield break;
        }

        [UnityTest]
        public IEnumerator RepeatedAssignmentsAreIdempotent()
        {
            GameObject root = new("SiblingIdempotent");
            _spawned.Add(root);
            _ = root.AddComponent<BoxCollider>();
            _ = root.AddComponent<BoxCollider>();
            SiblingMultipleTester tester = root.AddComponent<SiblingMultipleTester>();

            tester.AssignSiblingComponents();
            BoxCollider[] firstAssignment = tester.array.ToArray();
            List<BoxCollider> firstListAssignment = tester.list.ToList();

            tester.AssignSiblingComponents();
            BoxCollider[] secondAssignment = tester.array;

            // Repeated calls should produce same results
            CollectionAssert.AreEquivalent(firstAssignment, secondAssignment);
            CollectionAssert.AreEquivalent(firstListAssignment, tester.list);

            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingComponentWithMixedComponentTypes()
        {
            GameObject root = new("SiblingMixed");
            _spawned.Add(root);
            root.AddComponent<BoxCollider>();
            root.AddComponent<SpriteRenderer>();
            root.AddComponent<Rigidbody>();
            SiblingMixedTester tester = root.AddComponent<SiblingMixedTester>();

            tester.AssignSiblingComponents();

            Assert.IsTrue(tester.siblingCollider != null);
            Assert.IsTrue(tester.siblingRenderer != null);
            Assert.IsTrue(tester.siblingRigidBody != null);

            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingComponentDoesNotFindDisabledBehaviours()
        {
            GameObject root = new("SiblingDisabled", typeof(BoxCollider));
            _spawned.Add(root);
            BoxCollider collider = root.GetComponent<BoxCollider>();
            collider.enabled = false;
            SiblingDisabledTester tester = root.AddComponent<SiblingDisabledTester>();

            tester.AssignSiblingComponents();

            // Disabled Behaviour components should still be found
            // (GetComponent doesn't filter by enabled state)
            Assert.AreSame(collider, tester.siblingCollider);

            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingComponentWithNoMatchingTypeReturnsNull()
        {
            GameObject root = new("SiblingNoMatch", typeof(SiblingNoMatchTester));
            _spawned.Add(root);
            SiblingNoMatchTester tester = root.GetComponent<SiblingNoMatchTester>();

            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|SiblingNoMatch\[SiblingNoMatchTester\]\|Unable to find sibling component of type UnityEngine\.BoxCollider for field 'siblingCollider'$"
                )
            );

            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|SiblingNoMatch\[SiblingNoMatchTester\]\|Unable to find sibling component of type UnityEngine\.BoxCollider\[\] for field 'colliderArray'$"
                )
            );

            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|SiblingNoMatch\[SiblingNoMatchTester\]\|Unable to find sibling component of type System\.Collections\.Generic\.List`1\[UnityEngine\.BoxCollider\] for field 'colliderList'$"
                )
            );

            tester.AssignSiblingComponents();

            Assert.IsTrue(tester.siblingCollider == null);
            Assert.AreEqual(0, tester.colliderArray.Length);
            Assert.AreEqual(0, tester.colliderList.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingComponentFindsComponentsInOrder()
        {
            GameObject root = new("SiblingOrder");
            _spawned.Add(root);

            // Add components in specific order
            BoxCollider first = root.AddComponent<BoxCollider>();
            BoxCollider second = root.AddComponent<BoxCollider>();
            BoxCollider third = root.AddComponent<BoxCollider>();
            SiblingOrderTester tester = root.AddComponent<SiblingOrderTester>();

            tester.AssignSiblingComponents();

            // GetComponents returns in the order they were added
            Assert.AreEqual(3, tester.colliders.Count);
            Assert.AreSame(first, tester.colliders[0]);
            Assert.AreSame(second, tester.colliders[1]);
            Assert.AreSame(third, tester.colliders[2]);

            yield break;
        }

        [UnityTest]
        public IEnumerator IncludeInactiveFindsAllComponentsOnActiveGameObject()
        {
            GameObject root = new("SiblingIncludeInactive");
            _spawned.Add(root);
            BoxCollider first = root.AddComponent<BoxCollider>();
            BoxCollider second = root.AddComponent<BoxCollider>();
            second.enabled = false;
            SiblingIncludeInactiveTester tester = root.AddComponent<SiblingIncludeInactiveTester>();

            tester.AssignSiblingComponents();

            // includeInactive=true should find both enabled and disabled components
            Assert.IsTrue(tester.includeInactiveSingle != null);
            Assert.AreEqual(2, tester.includeInactiveArray.Length);
            CollectionAssert.Contains(tester.includeInactiveArray, first);
            CollectionAssert.Contains(tester.includeInactiveArray, second);
            Assert.AreEqual(2, tester.includeInactiveList.Count);
            CollectionAssert.Contains(tester.includeInactiveList, first);
            CollectionAssert.Contains(tester.includeInactiveList, second);

            yield break;
        }

        [UnityTest]
        public IEnumerator ExcludeInactiveFiltersDisabledComponents()
        {
            GameObject root = new("SiblingExcludeInactive");
            _spawned.Add(root);
            BoxCollider first = root.AddComponent<BoxCollider>();
            BoxCollider second = root.AddComponent<BoxCollider>();
            second.enabled = false;
            SiblingExcludeInactiveTester tester = root.AddComponent<SiblingExcludeInactiveTester>();

            tester.AssignSiblingComponents();

            // includeInactive=false should filter out disabled components
            Assert.AreSame(first, tester.activeOnlySingle);
            Assert.AreEqual(1, tester.activeOnlyArray.Length);
            Assert.AreSame(first, tester.activeOnlyArray[0]);
            Assert.AreEqual(1, tester.activeOnlyList.Count);
            Assert.AreSame(first, tester.activeOnlyList[0]);

            yield break;
        }

        [UnityTest]
        public IEnumerator ExcludeInactiveOnInactiveGameObjectFindsNothing()
        {
            GameObject root = new("SiblingInactiveGameObject");
            _spawned.Add(root);
            root.SetActive(false);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            SiblingExcludeInactiveTester tester = root.AddComponent<SiblingExcludeInactiveTester>();

            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|SiblingInactiveGameObject\[SiblingExcludeInactiveTester\]\|Unable to find sibling component of type UnityEngine\.BoxCollider for field 'activeOnlySingle'$"
                )
            );
            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|SiblingInactiveGameObject\[SiblingExcludeInactiveTester\]\|Unable to find sibling component of type UnityEngine\.BoxCollider\[\] for field 'activeOnlyArray'$"
                )
            );
            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|SiblingInactiveGameObject\[SiblingExcludeInactiveTester\]\|Unable to find sibling component of type System\.Collections\.Generic\.List`1\[UnityEngine\.BoxCollider\] for field 'activeOnlyList'$"
                )
            );

            tester.AssignSiblingComponents();

            // includeInactive=false on inactive GameObject should find nothing
            Assert.IsTrue(tester.activeOnlySingle == null);
            Assert.AreEqual(0, tester.activeOnlyArray.Length);
            Assert.AreEqual(0, tester.activeOnlyList.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator IncludeInactiveOnInactiveGameObjectFindsComponents()
        {
            GameObject root = new("SiblingInactiveGameObjectInclude");
            _spawned.Add(root);
            root.SetActive(false);
            BoxCollider first = root.AddComponent<BoxCollider>();
            BoxCollider second = root.AddComponent<BoxCollider>();
            SiblingIncludeInactiveTester tester = root.AddComponent<SiblingIncludeInactiveTester>();

            tester.AssignSiblingComponents();

            // includeInactive=true on inactive GameObject should still find components
            Assert.IsTrue(tester.includeInactiveSingle != null);
            Assert.AreEqual(2, tester.includeInactiveArray.Length);
            Assert.AreEqual(2, tester.includeInactiveList.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator MixedActiveInactiveComponentsFilteredCorrectly()
        {
            GameObject root = new("SiblingMixedActive");
            _spawned.Add(root);
            BoxCollider first = root.AddComponent<BoxCollider>();
            first.enabled = true;
            BoxCollider second = root.AddComponent<BoxCollider>();
            second.enabled = false;
            BoxCollider third = root.AddComponent<BoxCollider>();
            third.enabled = true;
            BoxCollider fourth = root.AddComponent<BoxCollider>();
            fourth.enabled = false;

            SiblingMixedActiveTester tester = root.AddComponent<SiblingMixedActiveTester>();

            tester.AssignSiblingComponents();

            // includeInactive=false should only find enabled components
            Assert.AreEqual(2, tester.activeOnly.Length);
            CollectionAssert.Contains(tester.activeOnly, first);
            CollectionAssert.Contains(tester.activeOnly, third);
            CollectionAssert.DoesNotContain(tester.activeOnly, second);
            CollectionAssert.DoesNotContain(tester.activeOnly, fourth);

            // includeInactive=true should find all components
            Assert.AreEqual(4, tester.includeInactive.Length);
            CollectionAssert.Contains(tester.includeInactive, first);
            CollectionAssert.Contains(tester.includeInactive, second);
            CollectionAssert.Contains(tester.includeInactive, third);
            CollectionAssert.Contains(tester.includeInactive, fourth);

            yield break;
        }

        [UnityTest]
        public IEnumerator IncludeInactiveFindsBehavioursRegardlessOfEnabledState()
        {
            GameObject root = new("SiblingBehaviours");
            _spawned.Add(root);
            SiblingTestBehaviour first = root.AddComponent<SiblingTestBehaviour>();
            first.enabled = true;
            SiblingTestBehaviour second = root.AddComponent<SiblingTestBehaviour>();
            second.enabled = false;
            SiblingBehaviourTester tester = root.AddComponent<SiblingBehaviourTester>();

            tester.AssignSiblingComponents();

            // includeInactive=true should find both enabled and disabled behaviours
            Assert.AreEqual(2, tester.allBehaviours.Length);
            CollectionAssert.Contains(tester.allBehaviours, first);
            CollectionAssert.Contains(tester.allBehaviours, second);

            yield break;
        }

        [UnityTest]
        public IEnumerator ExcludeInactiveFiltersBehavioursByEnabledState()
        {
            GameObject root = new("SiblingBehavioursFiltered");
            _spawned.Add(root);
            SiblingTestBehaviour first = root.AddComponent<SiblingTestBehaviour>();
            first.enabled = true;
            SiblingTestBehaviour second = root.AddComponent<SiblingTestBehaviour>();
            second.enabled = false;
            SiblingTestBehaviour third = root.AddComponent<SiblingTestBehaviour>();
            third.enabled = true;
            SiblingBehaviourFilterTester tester = root.AddComponent<SiblingBehaviourFilterTester>();

            tester.AssignSiblingComponents();

            // includeInactive=false should only find enabled behaviours
            Assert.AreEqual(2, tester.activeBehaviours.Length);
            CollectionAssert.Contains(tester.activeBehaviours, first);
            CollectionAssert.Contains(tester.activeBehaviours, third);
            CollectionAssert.DoesNotContain(tester.activeBehaviours, second);

            yield break;
        }
    }

    internal sealed class SiblingAssignmentComponent : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider single;

        [SiblingComponent]
        public BoxCollider[] array;

        [SiblingComponent]
        public List<BoxCollider> list;

        [SiblingComponent(Optional = true)]
        public Rigidbody optional;
    }

    internal sealed class SiblingMissingComponent : MonoBehaviour
    {
        [SiblingComponent]
        public Rigidbody required;
    }

    internal sealed class SiblingSkipIfAssignedTester : MonoBehaviour
    {
        [SiblingComponent(SkipIfAssigned = true)]
        public BoxCollider preAssignedSibling;

        [SiblingComponent(SkipIfAssigned = true)]
        public BoxCollider[] preAssignedSiblingArray;

        [SiblingComponent(SkipIfAssigned = true)]
        public List<BoxCollider> preAssignedSiblingList;

        [SiblingComponent]
        public BoxCollider normalSibling;
    }

    internal sealed class SiblingOptionalTester : MonoBehaviour
    {
        [SiblingComponent(Optional = true)]
        public BoxCollider optionalCollider;
    }

    internal sealed class SiblingMultipleTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider single;

        [SiblingComponent]
        public BoxCollider[] array;

        [SiblingComponent]
        public List<BoxCollider> list;
    }

    internal sealed class SiblingSelfInclusionTester : MonoBehaviour
    {
        [SiblingComponent]
        public SpriteRenderer siblingRenderer;

        [SiblingComponent]
        public SpriteRenderer[] rendererArray;

        [SiblingComponent]
        public List<SpriteRenderer> rendererList;
    }

    internal sealed class SiblingExclusionTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider[] colliders;
    }

    internal sealed class SiblingOneTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider single;

        [SiblingComponent]
        public BoxCollider[] array;

        [SiblingComponent]
        public List<BoxCollider> list;
    }

    internal sealed class SiblingCacheIsolationTesterA : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider siblingCollider;
    }

    internal sealed class SiblingCacheIsolationTesterB : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider siblingCollider;
    }

    internal sealed class SiblingMixedTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider siblingCollider;

        [SiblingComponent]
        public SpriteRenderer siblingRenderer;

        [SiblingComponent]
        public Rigidbody siblingRigidBody;
    }

    internal sealed class SiblingDisabledTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider siblingCollider;
    }

    internal sealed class SiblingNoMatchTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider siblingCollider;

        [SiblingComponent]
        public BoxCollider[] colliderArray;

        [SiblingComponent]
        public List<BoxCollider> colliderList;
    }

    internal sealed class SiblingOrderTester : MonoBehaviour
    {
        [SiblingComponent]
        public List<BoxCollider> colliders;
    }

    internal sealed class SiblingIncludeInactiveTester : MonoBehaviour
    {
        [SiblingComponent(IncludeInactive = true)]
        public BoxCollider includeInactiveSingle;

        [SiblingComponent(IncludeInactive = true)]
        public BoxCollider[] includeInactiveArray;

        [SiblingComponent(IncludeInactive = true)]
        public List<BoxCollider> includeInactiveList;
    }

    internal sealed class SiblingExcludeInactiveTester : MonoBehaviour
    {
        [SiblingComponent(IncludeInactive = false)]
        public BoxCollider activeOnlySingle;

        [SiblingComponent(IncludeInactive = false)]
        public BoxCollider[] activeOnlyArray;

        [SiblingComponent(IncludeInactive = false)]
        public List<BoxCollider> activeOnlyList;
    }

    internal sealed class SiblingMixedActiveTester : MonoBehaviour
    {
        [SiblingComponent(IncludeInactive = false)]
        public BoxCollider[] activeOnly;

        [SiblingComponent(IncludeInactive = true)]
        public BoxCollider[] includeInactive;
    }

    internal sealed class SiblingTestBehaviour : MonoBehaviour { }

    internal sealed class SiblingBehaviourTester : MonoBehaviour
    {
        [SiblingComponent(IncludeInactive = true)]
        public SiblingTestBehaviour[] allBehaviours;
    }

    internal sealed class SiblingBehaviourFilterTester : MonoBehaviour
    {
        [SiblingComponent(IncludeInactive = false)]
        public SiblingTestBehaviour[] activeBehaviours;
    }
}

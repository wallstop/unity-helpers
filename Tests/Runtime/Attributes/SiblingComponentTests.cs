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
    using WallstopStudios.UnityHelpers.Tests.TestUtils;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    [TestFixture]
    public sealed class SiblingComponentTests : CommonTestBase
    {
        [UnityTest]
        public IEnumerator AssignSiblingComponentsPopulatesSupportedFieldShapes()
        {
            GameObject root = Track(new GameObject("SiblingAssignments"));
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
            GameObject root = Track(
                new GameObject("SiblingMissing", typeof(SiblingMissingComponent))
            );
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
            GameObject root = Track(new GameObject("SiblingSkipIfAssigned"));
            BoxCollider first = root.AddComponent<BoxCollider>();
            BoxCollider second = root.AddComponent<BoxCollider>();
            SiblingSkipIfAssignedTester tester = root.AddComponent<SiblingSkipIfAssignedTester>();

            // Pre-assign values that should NOT be overwritten
            tester.preAssignedSibling = second;
            tester.preAssignedSiblingArray = new[] { second };
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
            GameObject root = Track(new GameObject("SiblingSkipEmpty"));
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
            GameObject root = Track(new GameObject("SiblingSkipNull"));
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
            GameObject root = Track(
                new GameObject("SiblingOptional", typeof(SiblingOptionalTester))
            );
            SiblingOptionalTester tester = root.GetComponent<SiblingOptionalTester>();

            // Should NOT log error for optional component
            tester.AssignSiblingComponents();

            Assert.IsTrue(tester.optionalCollider == null);
            yield break;
        }

        [UnityTest]
        public IEnumerator MultipleSiblingComponentsOfSameType()
        {
            GameObject root = Track(new GameObject("SiblingMultiple"));
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
            GameObject root = Track(new GameObject("SiblingSelf", typeof(SpriteRenderer)));
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
            GameObject root = Track(new GameObject("SiblingExclude"));
            BoxCollider rootCollider = root.AddComponent<BoxCollider>();
            SiblingExclusionTester tester = root.AddComponent<SiblingExclusionTester>();

            GameObject child = Track(new GameObject("SiblingChild", typeof(BoxCollider)));
            child.transform.SetParent(root.transform);

            GameObject sibling = Track(new GameObject("SiblingSibling", typeof(BoxCollider)));
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
            GameObject root = Track(new GameObject("SiblingOne", typeof(BoxCollider)));
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
            GameObject root = Track(new GameObject("SiblingCache", typeof(BoxCollider)));
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
            GameObject root = Track(new GameObject("SiblingIdempotent"));
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
            Track(root);
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
            Track(root);
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
            Track(root);
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
            Track(root);

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
            Track(root);
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
            Track(root);
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
            Track(root);
            root.SetActive(false);
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
            Track(root);
            root.SetActive(false);
            // Add sibling components while inactive to validate IncludeInactive behavior
            root.AddComponent<BoxCollider>();
            root.AddComponent<BoxCollider>();
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
            Track(root);
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
            Track(root);
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
            Track(root);
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

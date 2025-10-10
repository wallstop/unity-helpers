namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    [TestFixture]
    public sealed class RelationalComponentTagAndNameFilterEdgeTests : CommonTestBase
    {
        [UnityTest]
        public IEnumerator IncludeInactiveExcludesDisabledAndInactive()
        {
            GameObject root = Track(new GameObject("InactiveRoot", typeof(IncludeInactiveTester)));
            IncludeInactiveTester tester = root.GetComponent<IncludeInactiveTester>();

            GameObject activeChild = Track(new GameObject("ActiveChild", typeof(SpriteRenderer)));
            activeChild.tag = "Player";
            activeChild.transform.SetParent(root.transform);

            GameObject inactiveChild = Track(
                new GameObject("InactiveChild", typeof(SpriteRenderer))
            );
            inactiveChild.tag = "Player";
            inactiveChild.transform.SetParent(root.transform);
            inactiveChild.SetActive(false);

            GameObject disabledChild = Track(
                new GameObject("DisabledChild", typeof(SpriteRenderer))
            );
            disabledChild.tag = "Player";
            disabledChild.transform.SetParent(root.transform);
            disabledChild.GetComponent<SpriteRenderer>().enabled = false;

            tester.AssignChildComponents();

            Assert.AreEqual(1, tester.onlyActivePlayers.Count);
            Assert.AreSame(activeChild.GetComponent<SpriteRenderer>(), tester.onlyActivePlayers[0]);

            yield break;
        }

        [UnityTest]
        public IEnumerator CombinedTagAndNameFilterRequiresBoth()
        {
            GameObject root = Track(new GameObject("Root", typeof(CombinedFilterTester)));
            CombinedFilterTester tester = root.GetComponent<CombinedFilterTester>();

            GameObject playerWrongName = Track(new GameObject("EnemyOne", typeof(SpriteRenderer)));
            playerWrongName.tag = "Player";
            playerWrongName.transform.SetParent(root.transform);

            GameObject wrongTagRightName = Track(
                new GameObject("PlayerOne", typeof(SpriteRenderer))
            );
            wrongTagRightName.tag = "Untagged";
            wrongTagRightName.transform.SetParent(root.transform);

            GameObject correct = Track(new GameObject("PlayerAlpha", typeof(SpriteRenderer)));
            correct.tag = "Player";
            correct.transform.SetParent(root.transform);

            tester.AssignChildComponents();

            Assert.AreEqual(1, tester.matched.Count);
            Assert.AreSame(correct.GetComponent<SpriteRenderer>(), tester.matched[0]);

            yield break;
        }

        [UnityTest]
        public IEnumerator TagFilterMatchesUntagged()
        {
            GameObject root = Track(new GameObject("Root", typeof(UntaggedFilterTester)));
            UntaggedFilterTester tester = root.GetComponent<UntaggedFilterTester>();

            GameObject child1 = Track(new GameObject("Child1", typeof(SpriteRenderer)));
            child1.tag = "Untagged";
            child1.transform.SetParent(root.transform);

            GameObject child2 = Track(new GameObject("Child2", typeof(SpriteRenderer)));
            child2.tag = "Player";
            child2.transform.SetParent(root.transform);

            tester.AssignChildComponents();

            Assert.AreEqual(1, tester.untagged.Count);
            Assert.AreSame(child1.GetComponent<SpriteRenderer>(), tester.untagged[0]);

            yield break;
        }

        [UnityTest]
        public IEnumerator OnlyDescendantsIncludesSelfWhenFalse()
        {
            GameObject root = Track(
                new GameObject("SelfRoot", typeof(SelfInclusionTester), typeof(SpriteRenderer))
            );
            SelfInclusionTester tester = root.GetComponent<SelfInclusionTester>();

            tester.AssignChildComponents();

            Assert.IsTrue(tester.selfRenderer != null);
            Assert.AreSame(root.GetComponent<SpriteRenderer>(), tester.selfRenderer);

            yield break;
        }

        [UnityTest]
        public IEnumerator AllowInterfacesFalseDisablesInterfaceResolution()
        {
            GameObject root = Track(
                new GameObject("InterfaceRoot", typeof(InterfacesDisabledTester))
            );
            InterfacesDisabledTester tester = root.GetComponent<InterfacesDisabledTester>();

            GameObject child = Track(new GameObject("Child", typeof(TestInterfaceComponent)));
            child.transform.SetParent(root.transform);

            LogAssert.Expect(
                LogType.Error,
                new Regex(@"Unable to find child component of type .* for field 'iface'")
            );

            tester.AssignChildComponents();

            Assert.IsTrue((UnityEngine.Object)tester.iface == null);
            yield break;
        }

        [UnityTest]
        public IEnumerator OptionalSuppressesMissingErrors()
        {
            GameObject root = Track(new GameObject("OptionalRoot", typeof(OptionalTester)));
            OptionalTester tester = root.GetComponent<OptionalTester>();

            tester.AssignSiblingComponents();

            Assert.IsTrue(tester.missingOptional == null);
            LogAssert.NoUnexpectedReceived();
            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingTagFilterNoMatchLogsError()
        {
            GameObject root = new("SiblingTagFilterRoot");
            Track(root);
            root.tag = "Untagged";
            root.AddComponent<BoxCollider>();
            SiblingNoMatchTagTester tester = root.AddComponent<SiblingNoMatchTagTester>();

            LogAssert.Expect(
                LogType.Error,
                new Regex(
                    @"Unable to find sibling component of type .* for field 'siblingCollider'"
                )
            );

            tester.AssignSiblingComponents();
            Assert.IsTrue(tester.siblingCollider == null);

            yield break;
        }

        [UnityTest]
        public IEnumerator SkipIfAssignedDoesNotOverride()
        {
            GameObject root = new("SkipRoot", typeof(SkipIfAssignedTesterEdgeCase));
            Track(root);
            SkipIfAssignedTesterEdgeCase tester = root.GetComponent<SkipIfAssignedTesterEdgeCase>();

            SpriteRenderer preassigned = root.AddComponent<SpriteRenderer>();
            tester.alreadyAssigned = preassigned;

            tester.AssignSiblingComponents();

            Assert.AreSame(preassigned, tester.alreadyAssigned);
            yield break;
        }
    }

    internal sealed class IncludeInactiveTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, IncludeInactive = false, TagFilter = "Player")]
        public List<SpriteRenderer> onlyActivePlayers;
    }

    internal sealed class CombinedFilterTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, TagFilter = "Player", NameFilter = "Player")]
        public List<SpriteRenderer> matched;
    }

    internal sealed class UntaggedFilterTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, TagFilter = "Untagged")]
        public List<SpriteRenderer> untagged;
    }

    internal sealed class SelfInclusionTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = false)]
        public SpriteRenderer selfRenderer;
    }

    internal sealed class InterfacesDisabledTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, AllowInterfaces = false)]
        public ITestInterface iface;
    }

    internal sealed class OptionalTester : MonoBehaviour
    {
        [SiblingComponent(Optional = true)]
        public Rigidbody missingOptional;
    }

    internal sealed class SkipIfAssignedTesterEdgeCase : MonoBehaviour
    {
        [SiblingComponent(SkipIfAssigned = true)]
        public SpriteRenderer alreadyAssigned;
    }

    internal sealed class SiblingNoMatchTagTester : MonoBehaviour
    {
        [SiblingComponent(TagFilter = "Player")]
        public BoxCollider siblingCollider;
    }
}

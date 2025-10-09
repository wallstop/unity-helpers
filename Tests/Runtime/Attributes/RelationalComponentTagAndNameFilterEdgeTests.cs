namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using Object = UnityEngine.Object;

    [TestFixture]
    public sealed class RelationalComponentTagAndNameFilterEdgeTests
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
        public IEnumerator IncludeInactiveExcludesDisabledAndInactive()
        {
            GameObject root = new("InactiveRoot", typeof(IncludeInactiveTester));
            _spawned.Add(root);
            IncludeInactiveTester tester = root.GetComponent<IncludeInactiveTester>();

            GameObject activeChild = new("ActiveChild", typeof(SpriteRenderer));
            _spawned.Add(activeChild);
            activeChild.tag = "Player";
            activeChild.transform.SetParent(root.transform);

            GameObject inactiveChild = new("InactiveChild", typeof(SpriteRenderer));
            _spawned.Add(inactiveChild);
            inactiveChild.tag = "Player";
            inactiveChild.transform.SetParent(root.transform);
            inactiveChild.SetActive(false);

            GameObject disabledChild = new("DisabledChild", typeof(SpriteRenderer));
            _spawned.Add(disabledChild);
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
            GameObject root = new("Root", typeof(CombinedFilterTester));
            _spawned.Add(root);
            CombinedFilterTester tester = root.GetComponent<CombinedFilterTester>();

            GameObject playerWrongName = new("EnemyOne", typeof(SpriteRenderer));
            _spawned.Add(playerWrongName);
            playerWrongName.tag = "Player";
            playerWrongName.transform.SetParent(root.transform);

            GameObject wrongTagRightName = new("PlayerOne", typeof(SpriteRenderer));
            _spawned.Add(wrongTagRightName);
            wrongTagRightName.tag = "Untagged";
            wrongTagRightName.transform.SetParent(root.transform);

            GameObject correct = new("PlayerAlpha", typeof(SpriteRenderer));
            _spawned.Add(correct);
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
            GameObject root = new("Root", typeof(UntaggedFilterTester));
            _spawned.Add(root);
            UntaggedFilterTester tester = root.GetComponent<UntaggedFilterTester>();

            GameObject child1 = new("Child1", typeof(SpriteRenderer));
            _spawned.Add(child1);
            child1.tag = "Untagged";
            child1.transform.SetParent(root.transform);

            GameObject child2 = new("Child2", typeof(SpriteRenderer));
            _spawned.Add(child2);
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
            GameObject root = new("SelfRoot", typeof(SelfInclusionTester), typeof(SpriteRenderer));
            _spawned.Add(root);
            SelfInclusionTester tester = root.GetComponent<SelfInclusionTester>();

            tester.AssignChildComponents();

            Assert.IsNotNull(tester.selfRenderer);
            Assert.AreSame(root.GetComponent<SpriteRenderer>(), tester.selfRenderer);

            yield break;
        }

        [UnityTest]
        public IEnumerator AllowInterfacesFalseDisablesInterfaceResolution()
        {
            GameObject root = new("InterfaceRoot", typeof(InterfacesDisabledTester));
            _spawned.Add(root);
            InterfacesDisabledTester tester = root.GetComponent<InterfacesDisabledTester>();

            GameObject child = new("Child", typeof(TestInterfaceComponent));
            _spawned.Add(child);
            child.transform.SetParent(root.transform);

            LogAssert.Expect(
                LogType.Error,
                new Regex(@"Unable to find child component of type .* for field 'iface'")
            );

            tester.AssignChildComponents();

            Assert.IsNull(tester.iface);
            yield break;
        }

        [UnityTest]
        public IEnumerator OptionalSuppressesMissingErrors()
        {
            GameObject root = new("OptionalRoot", typeof(OptionalTester));
            _spawned.Add(root);
            OptionalTester tester = root.GetComponent<OptionalTester>();

            tester.AssignSiblingComponents();

            Assert.IsNull(tester.missingOptional);
            LogAssert.NoUnexpectedReceived();
            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingTagFilterNoMatchLogsError()
        {
            GameObject root = new("SiblingTagFilterRoot");
            _spawned.Add(root);
            root.tag = "Untagged";
            root.AddComponent<BoxCollider>();
            SiblingNoMatchTagTester tester = root.AddComponent<SiblingNoMatchTagTester>();

            LogAssert.Expect(
                LogType.Error,
                new Regex(@"Unable to find sibling component of type .* for field 'collider'")
            );

            tester.AssignSiblingComponents();
            Assert.IsNull(tester.collider);

            yield break;
        }

        [UnityTest]
        public IEnumerator SkipIfAssignedDoesNotOverride()
        {
            GameObject root = new("SkipRoot", typeof(SkipIfAssignedTester));
            _spawned.Add(root);
            SkipIfAssignedTester tester = root.GetComponent<SkipIfAssignedTester>();

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
        public BoxCollider collider;
    }
}

namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    /// <summary>
    /// Tests for advanced features of relational component attributes:
    /// - MaxCount
    /// - MaxDepth
    /// - TagFilter
    /// - NameFilter
    /// - Interface support
    /// </summary>
    [TestFixture]
    public sealed class RelationalComponentAdvancedTests : CommonTestBase
    {
        [UnityTest]
        public IEnumerator ParentMaxCountLimitsResults()
        {
            GameObject root = Track(new GameObject("MaxCountRoot", typeof(SpriteRenderer)));
            GameObject parent1 = Track(new GameObject("Parent1", typeof(SpriteRenderer)));
            parent1.transform.SetParent(root.transform);
            GameObject parent2 = Track(new GameObject("Parent2", typeof(SpriteRenderer)));
            parent2.transform.SetParent(parent1.transform);
            GameObject child = Track(new GameObject("Child", typeof(ParentMaxCountTester)));
            child.transform.SetParent(parent2.transform);

            ParentMaxCountTester tester = child.GetComponent<ParentMaxCountTester>();
            tester.AssignParentComponents();

            // Should find only 2 parents despite 3 being available
            Assert.AreEqual(2, tester.limitedParents.Length);
            // Should find all parents when no limit
            Assert.AreEqual(3, tester.allParents.Length);

            yield break;
        }

        [UnityTest]
        public IEnumerator ChildMaxCountLimitsResults()
        {
            GameObject root = Track(new GameObject("MaxCountRoot", typeof(ChildMaxCountTester)));
            ChildMaxCountTester tester = root.GetComponent<ChildMaxCountTester>();

            for (int i = 0; i < 5; i++)
            {
                GameObject child = Track(new GameObject($"Child{i}", typeof(SpriteRenderer)));
                child.transform.SetParent(root.transform);
            }

            tester.AssignChildComponents();

            // Should find only 3 children despite 5 being available
            Assert.AreEqual(3, tester.limitedChildren.Count);
            // Should find all children when no limit
            Assert.AreEqual(5, tester.allChildren.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingMaxCountLimitsResults()
        {
            GameObject root = Track(new GameObject("MaxCountRoot"));

            for (int i = 0; i < 5; i++)
            {
                root.AddComponent<BoxCollider>();
            }

            SiblingMaxCountTester tester = root.AddComponent<SiblingMaxCountTester>();
            tester.AssignSiblingComponents();

            // Should find only 2 siblings despite 5 being available
            Assert.AreEqual(2, tester.limitedSiblings.Length);
            // Should find all siblings when no limit
            Assert.AreEqual(5, tester.allSiblings.Length);

            yield break;
        }

        [UnityTest]
        public IEnumerator ParentMaxDepthLimitsSearch()
        {
            GameObject root = Track(new GameObject("DepthRoot", typeof(SpriteRenderer)));
            GameObject level1 = Track(new GameObject("Level1", typeof(SpriteRenderer)));
            level1.transform.SetParent(root.transform);
            GameObject level2 = Track(new GameObject("Level2", typeof(SpriteRenderer)));
            level2.transform.SetParent(level1.transform);
            GameObject level3 = Track(new GameObject("Level3", typeof(SpriteRenderer)));
            level3.transform.SetParent(level2.transform);
            GameObject child = Track(new GameObject("Child", typeof(ParentMaxDepthTester)));
            child.transform.SetParent(level3.transform);

            ParentMaxDepthTester tester = child.GetComponent<ParentMaxDepthTester>();
            tester.AssignParentComponents();

            // depth1Only should find only level3 (immediate parent)
            Assert.IsTrue(tester.depth1Only != null);
            Assert.AreSame(level3.GetComponent<SpriteRenderer>(), tester.depth1Only);

            // depth2Array should find level3 and level2
            Assert.AreEqual(2, tester.depth2Array.Length);

            // allDepthList should find all 4 parents
            Assert.AreEqual(4, tester.allDepthList.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator ChildMaxDepthLimitsSearch()
        {
            GameObject root = Track(new GameObject("DepthRoot", typeof(ChildMaxDepthTester)));
            ChildMaxDepthTester tester = root.GetComponent<ChildMaxDepthTester>();

            GameObject level1 = Track(new GameObject("Level1", typeof(SpriteRenderer)));
            level1.transform.SetParent(root.transform);

            GameObject level2 = Track(new GameObject("Level2", typeof(SpriteRenderer)));
            level2.transform.SetParent(level1.transform);

            GameObject level3 = Track(new GameObject("Level3", typeof(SpriteRenderer)));
            level3.transform.SetParent(level2.transform);

            tester.AssignChildComponents();

            // depth1Only should find only level1 (immediate child)
            Assert.IsTrue(tester.depth1Only != null);
            Assert.AreSame(level1.GetComponent<SpriteRenderer>(), tester.depth1Only);

            // depth2Array should find level1 and level2
            Assert.AreEqual(2, tester.depth2Array.Length);

            // allDepthList should find all 3 children
            Assert.AreEqual(3, tester.allDepthList.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator ParentTagFilterOnlyFindsMatchingTags()
        {
            GameObject root = Track(new GameObject("TagRoot"));
            root.tag = "Player";
            root.AddComponent<SpriteRenderer>();

            GameObject parent1 = Track(new GameObject("Parent1"));
            parent1.tag = "Untagged";
            parent1.AddComponent<SpriteRenderer>();
            parent1.transform.SetParent(root.transform);

            GameObject child = Track(new GameObject("Child", typeof(ParentTagFilterTester)));
            child.transform.SetParent(parent1.transform);

            ParentTagFilterTester tester = child.GetComponent<ParentTagFilterTester>();
            tester.AssignParentComponents();

            // Should only find the Player-tagged parent
            Assert.IsTrue(tester.playerTaggedParent != null);
            Assert.AreSame(root.GetComponent<SpriteRenderer>(), tester.playerTaggedParent);

            // Should find all parents when no filter
            Assert.AreEqual(2, tester.allParents.Length);

            yield break;
        }

        [UnityTest]
        public IEnumerator ChildTagFilterOnlyFindsMatchingTags()
        {
            GameObject root = Track(new GameObject("TagRoot", typeof(ChildTagFilterTester)));
            ChildTagFilterTester tester = root.GetComponent<ChildTagFilterTester>();

            GameObject child1 = Track(new GameObject("Child1"));
            child1.tag = "Player";
            child1.AddComponent<SpriteRenderer>();
            child1.transform.SetParent(root.transform);

            GameObject child2 = new("Child2");
            Track(child2);
            child2.tag = "Untagged";
            child2.AddComponent<SpriteRenderer>();
            child2.transform.SetParent(root.transform);

            tester.AssignChildComponents();

            // Should only find the Player-tagged child
            Assert.AreEqual(1, tester.playerTaggedChildren.Count);
            Assert.AreSame(child1.GetComponent<SpriteRenderer>(), tester.playerTaggedChildren[0]);

            // Should find all children when no filter
            Assert.AreEqual(2, tester.allChildren.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingTagFilterOnlyFindsMatchingTags()
        {
            GameObject root = new("TagRoot");
            Track(root);
            root.tag = "Player";

            root.AddComponent<BoxCollider>();
            root.AddComponent<SpriteRenderer>();

            SiblingTagFilterTester tester = root.AddComponent<SiblingTagFilterTester>();
            tester.AssignSiblingComponents();

            // Should find both siblings since they're on the same GameObject with Player tag
            Assert.IsTrue(tester.playerTaggedCollider != null);
            Assert.AreEqual(1, tester.playerTaggedRenderers.Length);

            yield break;
        }

        [UnityTest]
        public IEnumerator ParentNameFilterOnlyFindsMatchingNames()
        {
            GameObject root = new("PlayerRoot", typeof(SpriteRenderer));
            Track(root);
            GameObject parent1 = new("EnemyParent", typeof(SpriteRenderer));
            Track(parent1);
            parent1.transform.SetParent(root.transform);
            GameObject child = new("Child", typeof(ParentNameFilterTester));
            Track(child);
            child.transform.SetParent(parent1.transform);

            ParentNameFilterTester tester = child.GetComponent<ParentNameFilterTester>();
            tester.AssignParentComponents();

            // Should only find parents with "Player" in name
            Assert.IsTrue(tester.playerNamedParent != null);
            Assert.AreSame(root.GetComponent<SpriteRenderer>(), tester.playerNamedParent);

            // Should find all parents when no filter
            Assert.AreEqual(2, tester.allParents.Length);

            yield break;
        }

        [UnityTest]
        public IEnumerator ChildNameFilterOnlyFindsMatchingNames()
        {
            GameObject root = new("Root", typeof(ChildNameFilterTester));
            Track(root);
            ChildNameFilterTester tester = root.GetComponent<ChildNameFilterTester>();

            GameObject child1 = new("PlayerChild", typeof(SpriteRenderer));
            Track(child1);
            child1.transform.SetParent(root.transform);

            GameObject child2 = new("EnemyChild", typeof(SpriteRenderer));
            Track(child2);
            child2.transform.SetParent(root.transform);

            tester.AssignChildComponents();

            // Should only find children with "Player" in name
            Assert.AreEqual(1, tester.playerNamedChildren.Count);
            Assert.AreSame(child1.GetComponent<SpriteRenderer>(), tester.playerNamedChildren[0]);

            // Should find all children when no filter
            Assert.AreEqual(2, tester.allChildren.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator ParentCanFindInterfaceComponents()
        {
            GameObject root = new("InterfaceRoot", typeof(TestInterfaceComponent));
            Track(root);
            GameObject child = new("Child", typeof(ParentInterfaceTester));
            Track(child);
            child.transform.SetParent(root.transform);

            ParentInterfaceTester tester = child.GetComponent<ParentInterfaceTester>();
            tester.AssignParentComponents();

            // Should find component implementing ITestInterface
            Assert.IsTrue((Object)tester.interfaceParent != null);
            Assert.IsInstanceOf<ITestInterface>(tester.interfaceParent);

            // Should find in array too
            Assert.AreEqual(1, tester.interfaceParentArray.Length);
            Assert.IsInstanceOf<ITestInterface>(tester.interfaceParentArray[0]);

            yield break;
        }

        [UnityTest]
        public IEnumerator ChildCanFindInterfaceComponents()
        {
            GameObject root = new("Root", typeof(ChildInterfaceTester));
            Track(root);
            ChildInterfaceTester tester = root.GetComponent<ChildInterfaceTester>();

            GameObject child = new("Child", typeof(TestInterfaceComponent));
            Track(child);
            child.transform.SetParent(root.transform);

            tester.AssignChildComponents();

            // Should find component implementing ITestInterface
            Assert.IsTrue((Object)tester.interfaceChild != null);
            Assert.IsInstanceOf<ITestInterface>(tester.interfaceChild);

            // Should find in list too
            Assert.AreEqual(1, tester.interfaceChildList.Count);
            Assert.IsInstanceOf<ITestInterface>(tester.interfaceChildList[0]);

            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingCanFindInterfaceComponents()
        {
            GameObject root = new("Root");
            Track(root);
            root.AddComponent<TestInterfaceComponent>();
            SiblingInterfaceTester tester = root.AddComponent<SiblingInterfaceTester>();

            tester.AssignSiblingComponents();

            // Should find component implementing ITestInterface
            Assert.IsTrue((Object)tester.interfaceSibling != null);
            Assert.IsInstanceOf<ITestInterface>(tester.interfaceSibling);

            yield break;
        }

        [UnityTest]
        public IEnumerator InterfaceSearchFindsMultipleImplementations()
        {
            GameObject root = new("Root", typeof(ChildMultiInterfaceTester));
            Track(root);
            ChildMultiInterfaceTester tester = root.GetComponent<ChildMultiInterfaceTester>();

            GameObject child1 = new("Child1");
            Track(child1);
            child1.AddComponent<TestInterfaceComponent>();
            child1.AddComponent<AnotherInterfaceComponent>();
            child1.transform.SetParent(root.transform);

            GameObject child2 = new("Child2");
            Track(child2);
            child2.AddComponent<TestInterfaceComponent>();
            child2.transform.SetParent(root.transform);

            tester.AssignChildComponents();

            // Should find all 3 components implementing ITestInterface
            Assert.AreEqual(3, tester.allInterfaces.Length);

            yield break;
        }

        [UnityTest]
        public IEnumerator CombinedMaxCountAndTagFilter()
        {
            GameObject root = new("Root", typeof(ChildCombinedTester));
            Track(root);
            ChildCombinedTester tester = root.GetComponent<ChildCombinedTester>();

            // Create 3 player-tagged children and 2 enemy-tagged
            for (int i = 0; i < 3; i++)
            {
                GameObject child = new($"PlayerChild{i}", typeof(SpriteRenderer));
                Track(child);
                child.tag = "Player";
                child.transform.SetParent(root.transform);
            }

            for (int i = 0; i < 2; i++)
            {
                GameObject child = new($"EnemyChild{i}", typeof(SpriteRenderer));
                Track(child);
                child.tag = "Untagged";
                child.transform.SetParent(root.transform);
            }

            tester.AssignChildComponents();

            // Should find max 2 player-tagged children
            Assert.AreEqual(2, tester.limitedPlayerChildren.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator CombinedMaxDepthAndNameFilter()
        {
            GameObject root = new("Root", typeof(ChildDepthAndNameTester));
            Track(root);
            ChildDepthAndNameTester tester = root.GetComponent<ChildDepthAndNameTester>();

            GameObject level1Player = new("PlayerLevel1", typeof(SpriteRenderer));
            Track(level1Player);
            level1Player.transform.SetParent(root.transform);

            GameObject level2Player = new("PlayerLevel2", typeof(SpriteRenderer));
            Track(level2Player);
            level2Player.transform.SetParent(level1Player.transform);

            GameObject level1Enemy = new("EnemyLevel1", typeof(SpriteRenderer));
            Track(level1Enemy);
            level1Enemy.transform.SetParent(root.transform);

            tester.AssignChildComponents();

            // Should find only depth-1 children with "Player" in name
            Assert.AreEqual(1, tester.depth1PlayerChildren.Length);
            Assert.AreSame(
                level1Player.GetComponent<SpriteRenderer>(),
                tester.depth1PlayerChildren[0]
            );

            yield break;
        }

        [UnityTest]
        public IEnumerator ErrorMessageIncludesFieldName()
        {
            GameObject root = new("ErrorRoot", typeof(ErrorMessageTester));
            Track(root);
            ErrorMessageTester tester = root.GetComponent<ErrorMessageTester>();

            // Expect error with field name
            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    "Unable to find parent component of type .* for field 'missingParentRenderer'"
                )
            );

            tester.AssignParentComponents();

            yield break;
        }
    }

    // MaxCount test components
    internal sealed class ParentMaxCountTester : MonoBehaviour
    {
        [ParentComponent(MaxCount = 2)]
        public SpriteRenderer[] limitedParents;

        [ParentComponent]
        public SpriteRenderer[] allParents;
    }

    internal sealed class ChildMaxCountTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, MaxCount = 3)]
        public List<SpriteRenderer> limitedChildren;

        [ChildComponent(OnlyDescendants = true)]
        public List<SpriteRenderer> allChildren;
    }

    internal sealed class SiblingMaxCountTester : MonoBehaviour
    {
        [SiblingComponent(MaxCount = 2)]
        public BoxCollider[] limitedSiblings;

        [SiblingComponent]
        public BoxCollider[] allSiblings;
    }

    // MaxDepth test components
    internal sealed class ParentMaxDepthTester : MonoBehaviour
    {
        [ParentComponent(OnlyAncestors = true, MaxDepth = 1)]
        public SpriteRenderer depth1Only;

        [ParentComponent(OnlyAncestors = true, MaxDepth = 2)]
        public SpriteRenderer[] depth2Array;

        [ParentComponent(OnlyAncestors = true)]
        public List<SpriteRenderer> allDepthList;
    }

    internal sealed class ChildMaxDepthTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, MaxDepth = 1)]
        public SpriteRenderer depth1Only;

        [ChildComponent(OnlyDescendants = true, MaxDepth = 2)]
        public SpriteRenderer[] depth2Array;

        [ChildComponent(OnlyDescendants = true)]
        public List<SpriteRenderer> allDepthList;
    }

    // Tag filter test components
    internal sealed class ParentTagFilterTester : MonoBehaviour
    {
        [ParentComponent(TagFilter = "Player")]
        public SpriteRenderer playerTaggedParent;

        [ParentComponent]
        public SpriteRenderer[] allParents;
    }

    internal sealed class ChildTagFilterTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, TagFilter = "Player")]
        public List<SpriteRenderer> playerTaggedChildren;

        [ChildComponent(OnlyDescendants = true)]
        public List<SpriteRenderer> allChildren;
    }

    internal sealed class SiblingTagFilterTester : MonoBehaviour
    {
        [SiblingComponent(TagFilter = "Player")]
        public BoxCollider playerTaggedCollider;

        [SiblingComponent(TagFilter = "Player")]
        public SpriteRenderer[] playerTaggedRenderers;
    }

    // Name filter test components
    internal sealed class ParentNameFilterTester : MonoBehaviour
    {
        [ParentComponent(NameFilter = "Player")]
        public SpriteRenderer playerNamedParent;

        [ParentComponent]
        public SpriteRenderer[] allParents;
    }

    internal sealed class ChildNameFilterTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, NameFilter = "Player")]
        public List<SpriteRenderer> playerNamedChildren;

        [ChildComponent(OnlyDescendants = true)]
        public List<SpriteRenderer> allChildren;
    }

    // Interface test components
    public interface ITestInterface
    {
        string GetTestValue();
    }

    internal sealed class TestInterfaceComponent : MonoBehaviour, ITestInterface
    {
        public string GetTestValue() => "Test";
    }

    internal sealed class AnotherInterfaceComponent : MonoBehaviour, ITestInterface
    {
        public string GetTestValue() => "Another";
    }

    internal sealed class ParentInterfaceTester : MonoBehaviour
    {
        [ParentComponent]
        public ITestInterface interfaceParent;

        [ParentComponent]
        public ITestInterface[] interfaceParentArray;
    }

    internal sealed class ChildInterfaceTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public ITestInterface interfaceChild;

        [ChildComponent(OnlyDescendants = true)]
        public List<ITestInterface> interfaceChildList;
    }

    internal sealed class SiblingInterfaceTester : MonoBehaviour
    {
        [SiblingComponent]
        public ITestInterface interfaceSibling;
    }

    internal sealed class ChildMultiInterfaceTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public ITestInterface[] allInterfaces;
    }

    // Combined features test components
    internal sealed class ChildCombinedTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, TagFilter = "Player", MaxCount = 2)]
        public List<SpriteRenderer> limitedPlayerChildren;
    }

    internal sealed class ChildDepthAndNameTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, MaxDepth = 1, NameFilter = "Player")]
        public SpriteRenderer[] depth1PlayerChildren;
    }

    // Error message test component
    internal sealed class ErrorMessageTester : MonoBehaviour
    {
        [ParentComponent(OnlyAncestors = true)]
        public SpriteRenderer missingParentRenderer;
    }
}

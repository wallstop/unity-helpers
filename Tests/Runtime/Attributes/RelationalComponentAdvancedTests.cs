namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Tests for advanced features of relational component attributes:
    /// - MaxCount
    /// - MaxDepth
    /// - TagFilter
    /// - NameFilter
    /// - Interface support
    /// </summary>
    [TestFixture]
    public sealed class RelationalComponentAdvancedTests
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
        public IEnumerator ParentMaxCountLimitsResults()
        {
            GameObject root = new("MaxCountRoot", typeof(SpriteRenderer));
            _spawned.Add(root);
            GameObject parent1 = new("Parent1", typeof(SpriteRenderer));
            _spawned.Add(parent1);
            parent1.transform.SetParent(root.transform);
            GameObject parent2 = new("Parent2", typeof(SpriteRenderer));
            _spawned.Add(parent2);
            parent2.transform.SetParent(parent1.transform);
            GameObject child = new("Child", typeof(ParentMaxCountTester));
            _spawned.Add(child);
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
            GameObject root = new("MaxCountRoot", typeof(ChildMaxCountTester));
            _spawned.Add(root);
            ChildMaxCountTester tester = root.GetComponent<ChildMaxCountTester>();

            for (int i = 0; i < 5; i++)
            {
                GameObject child = new($"Child{i}", typeof(SpriteRenderer));
                _spawned.Add(child);
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
            GameObject root = new("MaxCountRoot");
            _spawned.Add(root);

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
            GameObject root = new("DepthRoot", typeof(SpriteRenderer));
            _spawned.Add(root);
            GameObject level1 = new("Level1", typeof(SpriteRenderer));
            _spawned.Add(level1);
            level1.transform.SetParent(root.transform);
            GameObject level2 = new("Level2", typeof(SpriteRenderer));
            _spawned.Add(level2);
            level2.transform.SetParent(level1.transform);
            GameObject level3 = new("Level3", typeof(SpriteRenderer));
            _spawned.Add(level3);
            level3.transform.SetParent(level2.transform);
            GameObject child = new("Child", typeof(ParentMaxDepthTester));
            _spawned.Add(child);
            child.transform.SetParent(level3.transform);

            ParentMaxDepthTester tester = child.GetComponent<ParentMaxDepthTester>();
            tester.AssignParentComponents();

            // depth1Only should find only level3 (immediate parent)
            Assert.IsNotNull(tester.depth1Only);
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
            GameObject root = new("DepthRoot", typeof(ChildMaxDepthTester));
            _spawned.Add(root);
            ChildMaxDepthTester tester = root.GetComponent<ChildMaxDepthTester>();

            GameObject level1 = new("Level1", typeof(SpriteRenderer));
            _spawned.Add(level1);
            level1.transform.SetParent(root.transform);

            GameObject level2 = new("Level2", typeof(SpriteRenderer));
            _spawned.Add(level2);
            level2.transform.SetParent(level1.transform);

            GameObject level3 = new("Level3", typeof(SpriteRenderer));
            _spawned.Add(level3);
            level3.transform.SetParent(level2.transform);

            tester.AssignChildComponents();

            // depth1Only should find only level1 (immediate child)
            Assert.IsNotNull(tester.depth1Only);
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
            GameObject root = new("TagRoot");
            _spawned.Add(root);
            root.tag = "Player";
            root.AddComponent<SpriteRenderer>();

            GameObject parent1 = new("Parent1");
            _spawned.Add(parent1);
            parent1.tag = "Untagged";
            parent1.AddComponent<SpriteRenderer>();
            parent1.transform.SetParent(root.transform);

            GameObject child = new("Child", typeof(ParentTagFilterTester));
            _spawned.Add(child);
            child.transform.SetParent(parent1.transform);

            ParentTagFilterTester tester = child.GetComponent<ParentTagFilterTester>();
            tester.AssignParentComponents();

            // Should only find the Player-tagged parent
            Assert.IsNotNull(tester.playerTaggedParent);
            Assert.AreSame(root.GetComponent<SpriteRenderer>(), tester.playerTaggedParent);

            // Should find all parents when no filter
            Assert.AreEqual(2, tester.allParents.Length);

            yield break;
        }

        [UnityTest]
        public IEnumerator ChildTagFilterOnlyFindsMatchingTags()
        {
            GameObject root = new("TagRoot", typeof(ChildTagFilterTester));
            _spawned.Add(root);
            ChildTagFilterTester tester = root.GetComponent<ChildTagFilterTester>();

            GameObject child1 = new("Child1");
            _spawned.Add(child1);
            child1.tag = "Player";
            child1.AddComponent<SpriteRenderer>();
            child1.transform.SetParent(root.transform);

            GameObject child2 = new("Child2");
            _spawned.Add(child2);
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
            _spawned.Add(root);
            root.tag = "Player";

            root.AddComponent<BoxCollider>();
            root.AddComponent<SpriteRenderer>();

            SiblingTagFilterTester tester = root.AddComponent<SiblingTagFilterTester>();
            tester.AssignSiblingComponents();

            // Should find both siblings since they're on the same GameObject with Player tag
            Assert.IsNotNull(tester.playerTaggedCollider);
            Assert.AreEqual(1, tester.playerTaggedRenderers.Length);

            yield break;
        }

        [UnityTest]
        public IEnumerator ParentNameFilterOnlyFindsMatchingNames()
        {
            GameObject root = new("PlayerRoot", typeof(SpriteRenderer));
            _spawned.Add(root);
            GameObject parent1 = new("EnemyParent", typeof(SpriteRenderer));
            _spawned.Add(parent1);
            parent1.transform.SetParent(root.transform);
            GameObject child = new("Child", typeof(ParentNameFilterTester));
            _spawned.Add(child);
            child.transform.SetParent(parent1.transform);

            ParentNameFilterTester tester = child.GetComponent<ParentNameFilterTester>();
            tester.AssignParentComponents();

            // Should only find parents with "Player" in name
            Assert.IsNotNull(tester.playerNamedParent);
            Assert.AreSame(root.GetComponent<SpriteRenderer>(), tester.playerNamedParent);

            // Should find all parents when no filter
            Assert.AreEqual(2, tester.allParents.Length);

            yield break;
        }

        [UnityTest]
        public IEnumerator ChildNameFilterOnlyFindsMatchingNames()
        {
            GameObject root = new("Root", typeof(ChildNameFilterTester));
            _spawned.Add(root);
            ChildNameFilterTester tester = root.GetComponent<ChildNameFilterTester>();

            GameObject child1 = new("PlayerChild", typeof(SpriteRenderer));
            _spawned.Add(child1);
            child1.transform.SetParent(root.transform);

            GameObject child2 = new("EnemyChild", typeof(SpriteRenderer));
            _spawned.Add(child2);
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
            _spawned.Add(root);
            GameObject child = new("Child", typeof(ParentInterfaceTester));
            _spawned.Add(child);
            child.transform.SetParent(root.transform);

            ParentInterfaceTester tester = child.GetComponent<ParentInterfaceTester>();
            tester.AssignParentComponents();

            // Should find component implementing ITestInterface
            Assert.IsNotNull(tester.interfaceParent);
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
            _spawned.Add(root);
            ChildInterfaceTester tester = root.GetComponent<ChildInterfaceTester>();

            GameObject child = new("Child", typeof(TestInterfaceComponent));
            _spawned.Add(child);
            child.transform.SetParent(root.transform);

            tester.AssignChildComponents();

            // Should find component implementing ITestInterface
            Assert.IsNotNull(tester.interfaceChild);
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
            _spawned.Add(root);
            root.AddComponent<TestInterfaceComponent>();
            SiblingInterfaceTester tester = root.AddComponent<SiblingInterfaceTester>();

            tester.AssignSiblingComponents();

            // Should find component implementing ITestInterface
            Assert.IsNotNull(tester.interfaceSibling);
            Assert.IsInstanceOf<ITestInterface>(tester.interfaceSibling);

            yield break;
        }

        [UnityTest]
        public IEnumerator InterfaceSearchFindsMultipleImplementations()
        {
            GameObject root = new("Root", typeof(ChildMultiInterfaceTester));
            _spawned.Add(root);
            ChildMultiInterfaceTester tester = root.GetComponent<ChildMultiInterfaceTester>();

            GameObject child1 = new("Child1");
            _spawned.Add(child1);
            child1.AddComponent<TestInterfaceComponent>();
            child1.AddComponent<AnotherInterfaceComponent>();
            child1.transform.SetParent(root.transform);

            GameObject child2 = new("Child2");
            _spawned.Add(child2);
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
            _spawned.Add(root);
            ChildCombinedTester tester = root.GetComponent<ChildCombinedTester>();

            // Create 3 player-tagged children and 2 enemy-tagged
            for (int i = 0; i < 3; i++)
            {
                GameObject child = new($"PlayerChild{i}", typeof(SpriteRenderer));
                _spawned.Add(child);
                child.tag = "Player";
                child.transform.SetParent(root.transform);
            }

            for (int i = 0; i < 2; i++)
            {
                GameObject child = new($"EnemyChild{i}", typeof(SpriteRenderer));
                _spawned.Add(child);
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
            _spawned.Add(root);
            ChildDepthAndNameTester tester = root.GetComponent<ChildDepthAndNameTester>();

            GameObject level1Player = new("PlayerLevel1", typeof(SpriteRenderer));
            _spawned.Add(level1Player);
            level1Player.transform.SetParent(root.transform);

            GameObject level2Player = new("PlayerLevel2", typeof(SpriteRenderer));
            _spawned.Add(level2Player);
            level2Player.transform.SetParent(level1Player.transform);

            GameObject level1Enemy = new("EnemyLevel1", typeof(SpriteRenderer));
            _spawned.Add(level1Enemy);
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
            _spawned.Add(root);
            ErrorMessageTester tester = root.GetComponent<ErrorMessageTester>();

            // Expect error with field name
            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    @"Unable to find parent component of type .* for field 'missingParentRenderer'"
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

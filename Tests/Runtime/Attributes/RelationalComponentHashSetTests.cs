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
    /// Tests for HashSet support in relational component attributes
    /// </summary>
    [TestFixture]
    public sealed class RelationalComponentHashSetTests
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
        public IEnumerator ParentHashSetFindsComponents()
        {
            GameObject root = new("Root", typeof(SpriteRenderer));
            _spawned.Add(root);
            GameObject parent1 = new("Parent1", typeof(SpriteRenderer));
            _spawned.Add(parent1);
            parent1.transform.SetParent(root.transform);
            GameObject child = new("Child", typeof(ParentHashSetTester));
            _spawned.Add(child);
            child.transform.SetParent(parent1.transform);

            ParentHashSetTester tester = child.GetComponent<ParentHashSetTester>();
            tester.AssignParentComponents();

            // Should find 2 parent SpriteRenderers
            Assert.IsNotNull(tester.parentRenderers);
            Assert.AreEqual(2, tester.parentRenderers.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator ChildHashSetFindsComponents()
        {
            GameObject root = new("Root", typeof(ChildHashSetTester));
            _spawned.Add(root);
            ChildHashSetTester tester = root.GetComponent<ChildHashSetTester>();

            for (int i = 0; i < 3; i++)
            {
                GameObject child = new($"Child{i}", typeof(SpriteRenderer));
                _spawned.Add(child);
                child.transform.SetParent(root.transform);
            }

            tester.AssignChildComponents();

            // Should find 3 child SpriteRenderers
            Assert.IsNotNull(tester.childRenderers);
            Assert.AreEqual(3, tester.childRenderers.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingHashSetFindsComponents()
        {
            GameObject root = new("Root");
            _spawned.Add(root);

            for (int i = 0; i < 3; i++)
            {
                root.AddComponent<BoxCollider>();
            }

            SiblingHashSetTester tester = root.AddComponent<SiblingHashSetTester>();
            tester.AssignSiblingComponents();

            // Should find 3 sibling BoxColliders
            Assert.IsNotNull(tester.siblingColliders);
            Assert.AreEqual(3, tester.siblingColliders.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator HashSetAutomaticallyDeduplicates()
        {
            // This test verifies HashSet's natural deduplication
            // Even if the same component appears multiple times in search results,
            // HashSet should only contain unique instances
            GameObject root = new("Root", typeof(ChildHashSetDeduplicationTester));
            _spawned.Add(root);
            ChildHashSetDeduplicationTester tester =
                root.GetComponent<ChildHashSetDeduplicationTester>();

            GameObject child = new("Child", typeof(SpriteRenderer));
            _spawned.Add(child);
            child.transform.SetParent(root.transform);

            tester.AssignChildComponents();

            // Should find 1 unique SpriteRenderer (HashSet ensures uniqueness)
            Assert.IsNotNull(tester.uniqueChildren);
            Assert.AreEqual(1, tester.uniqueChildren.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator HashSetSupportsMaxCount()
        {
            GameObject root = new("Root", typeof(ChildHashSetMaxCountTester));
            _spawned.Add(root);
            ChildHashSetMaxCountTester tester = root.GetComponent<ChildHashSetMaxCountTester>();

            for (int i = 0; i < 5; i++)
            {
                GameObject child = new($"Child{i}", typeof(SpriteRenderer));
                _spawned.Add(child);
                child.transform.SetParent(root.transform);
            }

            tester.AssignChildComponents();

            // Should find only 2 children despite 5 being available
            Assert.AreEqual(2, tester.limitedChildren.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator HashSetSupportsInterfaces()
        {
            GameObject root = new("Root", typeof(ChildHashSetInterfaceTester));
            _spawned.Add(root);
            ChildHashSetInterfaceTester tester = root.GetComponent<ChildHashSetInterfaceTester>();

            GameObject child1 = new("Child1", typeof(TestInterfaceComponent));
            _spawned.Add(child1);
            child1.transform.SetParent(root.transform);

            GameObject child2 = new("Child2", typeof(AnotherInterfaceComponent));
            _spawned.Add(child2);
            child2.transform.SetParent(root.transform);

            tester.AssignChildComponents();

            // Should find 2 components implementing ITestInterface
            Assert.IsNotNull(tester.interfaceChildren);
            Assert.AreEqual(2, tester.interfaceChildren.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator HashSetWorksWithFilters()
        {
            GameObject root = new("Root", typeof(ChildHashSetFilterTester));
            _spawned.Add(root);
            ChildHashSetFilterTester tester = root.GetComponent<ChildHashSetFilterTester>();

            GameObject child1 = new("PlayerChild", typeof(SpriteRenderer));
            _spawned.Add(child1);
            child1.tag = "Player";
            child1.transform.SetParent(root.transform);

            GameObject child2 = new("EnemyChild", typeof(SpriteRenderer));
            _spawned.Add(child2);
            child2.tag = "Untagged";
            child2.transform.SetParent(root.transform);

            tester.AssignChildComponents();

            // Should find only the Player-tagged child
            Assert.AreEqual(1, tester.playerChildren.Count);

            yield break;
        }
    }

    // Test components
    internal sealed class ParentHashSetTester : MonoBehaviour
    {
        [ParentComponent]
        public HashSet<SpriteRenderer> parentRenderers;
    }

    internal sealed class ChildHashSetTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public HashSet<SpriteRenderer> childRenderers;
    }

    internal sealed class SiblingHashSetTester : MonoBehaviour
    {
        [SiblingComponent]
        public HashSet<BoxCollider> siblingColliders;
    }

    internal sealed class ChildHashSetDeduplicationTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public HashSet<SpriteRenderer> uniqueChildren;
    }

    internal sealed class ChildHashSetMaxCountTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, MaxCount = 2)]
        public HashSet<SpriteRenderer> limitedChildren;
    }

    internal sealed class ChildHashSetInterfaceTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public HashSet<ITestInterface> interfaceChildren;
    }

    internal sealed class ChildHashSetFilterTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, TagFilter = "Player")]
        public HashSet<SpriteRenderer> playerChildren;
    }

    // Reuse test interfaces from RelationalComponentAdvancedTests
    public interface ITestInterface2
    {
        string GetTestValue();
    }

    internal sealed class TestInterfaceComponent2 : MonoBehaviour, ITestInterface
    {
        public string GetTestValue() => "Test";
    }

    internal sealed class AnotherInterfaceComponent2 : MonoBehaviour, ITestInterface
    {
        public string GetTestValue() => "Another";
    }
}

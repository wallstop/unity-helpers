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
    /// Tests to ensure backward compatibility after refactoring to use base classes.
    /// These tests verify that all existing functionality still works exactly as before.
    /// </summary>
    [TestFixture]
    public sealed class RelationalComponentBackwardCompatibilityTests : CommonTestBase
    {
        [UnityTest]
        public IEnumerator ParentBasicFunctionalityUnchanged()
        {
            GameObject root = Track(new GameObject("Root", typeof(SpriteRenderer)));
            GameObject child = Track(new GameObject("Child", typeof(BasicParentTester)));
            child.transform.SetParent(root.transform);

            BasicParentTester tester = child.GetComponent<BasicParentTester>();
            tester.AssignParentComponents();

            Assert.IsTrue(tester.parent != null);
            Assert.AreSame(root.GetComponent<SpriteRenderer>(), tester.parent);

            yield break;
        }

        [UnityTest]
        public IEnumerator ParentArrayStillWorks()
        {
            GameObject root = Track(new GameObject("Root", typeof(SpriteRenderer)));
            GameObject parent = Track(new GameObject("Parent", typeof(SpriteRenderer)));
            parent.transform.SetParent(root.transform);
            GameObject child = Track(new GameObject("Child", typeof(ParentArrayTester)));
            child.transform.SetParent(parent.transform);

            ParentArrayTester tester = child.GetComponent<ParentArrayTester>();
            tester.AssignParentComponents();

            Assert.AreEqual(2, tester.parents.Length);

            yield break;
        }

        [UnityTest]
        public IEnumerator ParentListStillWorks()
        {
            GameObject root = Track(new GameObject("Root", typeof(SpriteRenderer)));
            GameObject child = Track(new GameObject("Child", typeof(ParentListTester)));
            child.transform.SetParent(root.transform);

            ParentListTester tester = child.GetComponent<ParentListTester>();
            tester.AssignParentComponents();

            Assert.AreEqual(1, tester.parents.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator ParentOptionalStillWorks()
        {
            GameObject orphan = Track(new GameObject("Orphan", typeof(ParentOptionalTester)));
            ParentOptionalTester tester = orphan.GetComponent<ParentOptionalTester>();

            // Should NOT log error
            tester.AssignParentComponents();

            Assert.IsTrue(tester.optionalRenderer == null);

            yield break;
        }

        [UnityTest]
        public IEnumerator ParentOnlyAncestorsStillWorks()
        {
            GameObject root = Track(new GameObject("Root", typeof(SpriteRenderer)));
            GameObject child = new(
                "Child",
                typeof(SpriteRenderer),
                typeof(ParentOnlyAncestorsTester)
            );
            child = Track(child);
            child.transform.SetParent(root.transform);

            ParentOnlyAncestorsTester tester = child.GetComponent<ParentOnlyAncestorsTester>();
            tester.AssignParentComponents();

            // Should not include self
            Assert.AreSame(root.GetComponent<SpriteRenderer>(), tester.ancestorOnly);

            yield break;
        }

        [UnityTest]
        public IEnumerator ParentIncludeInactiveStillWorks()
        {
            GameObject root = Track(new GameObject("Root", typeof(SpriteRenderer)));
            GameObject inactive = Track(new GameObject("Inactive", typeof(SpriteRenderer)));
            inactive.SetActive(false);
            inactive.transform.SetParent(root.transform);
            GameObject child = Track(new GameObject("Child", typeof(ParentInactiveTester)));
            child.transform.SetParent(inactive.transform);

            ParentInactiveTester tester = child.GetComponent<ParentInactiveTester>();
            tester.AssignParentComponents();

            // Should find inactive parent
            Assert.IsTrue(tester.inactiveOnly != null);
            Assert.AreSame(inactive.GetComponent<SpriteRenderer>(), tester.inactiveOnly);

            // Should skip inactive parent
            Assert.IsTrue(tester.activeOnly != null);
            Assert.AreSame(root.GetComponent<SpriteRenderer>(), tester.activeOnly);

            yield break;
        }

        [UnityTest]
        public IEnumerator ChildBasicFunctionalityUnchanged()
        {
            GameObject root = Track(new GameObject("Root", typeof(BasicChildTester)));
            BasicChildTester tester = root.GetComponent<BasicChildTester>();

            GameObject child = Track(new GameObject("Child", typeof(SpriteRenderer)));
            child.transform.SetParent(root.transform);

            tester.AssignChildComponents();

            Assert.IsTrue(tester.child != null);
            Assert.AreSame(child.GetComponent<SpriteRenderer>(), tester.child);

            yield break;
        }

        [UnityTest]
        public IEnumerator ChildArrayStillWorks()
        {
            GameObject root = Track(new GameObject("Root", typeof(ChildArrayTester)));
            ChildArrayTester tester = root.GetComponent<ChildArrayTester>();

            for (int i = 0; i < 3; i++)
            {
                GameObject child = Track(new GameObject($"Child{i}", typeof(SpriteRenderer)));
                child.transform.SetParent(root.transform);
            }

            tester.AssignChildComponents();

            Assert.AreEqual(3, tester.children.Length);

            yield break;
        }

        [UnityTest]
        public IEnumerator ChildListStillWorks()
        {
            GameObject root = new("Root", typeof(ChildListTester));
            Track(root);
            ChildListTester tester = root.GetComponent<ChildListTester>();

            GameObject child = new("Child", typeof(SpriteRenderer));
            Track(child);
            child.transform.SetParent(root.transform);

            tester.AssignChildComponents();

            Assert.AreEqual(1, tester.children.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator ChildOnlyDescendantsStillWorks()
        {
            GameObject root = new(
                "Root",
                typeof(SpriteRenderer),
                typeof(ChildOnlyDescendantsTester)
            );
            Track(root);
            ChildOnlyDescendantsTester tester = root.GetComponent<ChildOnlyDescendantsTester>();

            GameObject child = new("Child", typeof(SpriteRenderer));
            Track(child);
            child.transform.SetParent(root.transform);

            tester.AssignChildComponents();

            // Should not include self
            Assert.AreSame(child.GetComponent<SpriteRenderer>(), tester.descendantOnly);

            yield break;
        }

        [UnityTest]
        public IEnumerator ChildBreadthFirstOrderMaintained()
        {
            GameObject root = new("Root", typeof(SpriteRenderer), typeof(ChildOrderTester));
            Track(root);
            ChildOrderTester tester = root.GetComponent<ChildOrderTester>();

            GameObject child1 = new("Child1", typeof(SpriteRenderer));
            Track(child1);
            child1.transform.SetParent(root.transform);

            GameObject child2 = new("Child2", typeof(SpriteRenderer));
            Track(child2);
            child2.transform.SetParent(root.transform);

            GameObject grandchild = new("Grandchild", typeof(SpriteRenderer));
            Track(grandchild);
            grandchild.transform.SetParent(child1.transform);

            tester.AssignChildComponents();

            // BFS order: root, child1, child2, grandchild
            Assert.AreEqual(4, tester.children.Length);
            Assert.AreSame(root.GetComponent<SpriteRenderer>(), tester.children[0]);
            // Grandchild should be last (after both children)
            Assert.AreSame(grandchild.GetComponent<SpriteRenderer>(), tester.children[3]);

            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingBasicFunctionalityUnchanged()
        {
            GameObject root = new("Root");
            Track(root);
            root.AddComponent<BoxCollider>();
            BasicSiblingTester tester = root.AddComponent<BasicSiblingTester>();

            tester.AssignSiblingComponents();

            Assert.IsTrue(tester.sibling != null);

            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingArrayStillWorks()
        {
            GameObject root = new("Root");
            Track(root);
            root.AddComponent<BoxCollider>();
            root.AddComponent<BoxCollider>();
            SiblingArrayTester tester = root.AddComponent<SiblingArrayTester>();

            tester.AssignSiblingComponents();

            Assert.AreEqual(2, tester.siblings.Length);

            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingListStillWorks()
        {
            GameObject root = new("Root");
            Track(root);
            root.AddComponent<BoxCollider>();
            SiblingListTester tester = root.AddComponent<SiblingListTester>();

            tester.AssignSiblingComponents();

            Assert.AreEqual(1, tester.siblings.Count);

            yield break;
        }

        [UnityTest]
        public IEnumerator SiblingIncludeInactiveStillWorks()
        {
            GameObject root = new("Root");
            Track(root);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.enabled = false;
            SiblingInactiveTester tester = root.AddComponent<SiblingInactiveTester>();

            tester.AssignSiblingComponents();

            // Should find disabled component with IncludeInactive=true
            Assert.IsTrue(tester.includeInactive != null);

            // Should not find disabled component with IncludeInactive=false
            Assert.IsTrue(tester.excludeInactive == null);

            yield break;
        }

        [UnityTest]
        public IEnumerator AssignRelationalComponentsStillWorksForAll()
        {
            GameObject root = new("Root", typeof(SpriteRenderer));
            Track(root);
            root.AddComponent<BoxCollider>();

            GameObject child = new("Child", typeof(SpriteRenderer), typeof(AllRelationalTester));
            Track(child);
            child.transform.SetParent(root.transform);
            child.AddComponent<BoxCollider>();

            AllRelationalTester tester = child.GetComponent<AllRelationalTester>();
            tester.AssignRelationalComponents();

            // Parent should be assigned
            Assert.IsTrue(tester.parentRenderer != null);
            Assert.AreSame(root.GetComponent<SpriteRenderer>(), tester.parentRenderer);

            // Self should be assigned as child
            Assert.IsTrue(tester.childRenderer != null);
            Assert.AreSame(child.GetComponent<SpriteRenderer>(), tester.childRenderer);

            // Sibling should be assigned
            Assert.IsTrue(tester.siblingCollider != null);
            Assert.AreSame(child.GetComponent<BoxCollider>(), tester.siblingCollider);

            yield break;
        }

        [UnityTest]
        public IEnumerator SkipIfAssignedStillWorks()
        {
            GameObject root = new("Root", typeof(SpriteRenderer));
            Track(root);
            GameObject child = new("Child", typeof(SkipIfAssignedTester));
            Track(child);
            child.transform.SetParent(root.transform);

            SkipIfAssignedTester tester = child.GetComponent<SkipIfAssignedTester>();
            SpriteRenderer rootRenderer = root.GetComponent<SpriteRenderer>();

            // Pre-assign a value
            SpriteRenderer dummyRenderer = new GameObject("Dummy").AddComponent<SpriteRenderer>();
            Track(dummyRenderer.gameObject);
            tester.preAssigned = dummyRenderer;

            tester.AssignParentComponents();

            // Should preserve pre-assigned value
            Assert.AreSame(dummyRenderer, tester.preAssigned);

            // Should assign normal value
            Assert.AreSame(rootRenderer, tester.normal);

            yield break;
        }
    }

    // Parent test components
    internal sealed class BasicParentTester : MonoBehaviour
    {
        [ParentComponent]
        public SpriteRenderer parent;
    }

    internal sealed class ParentArrayTester : MonoBehaviour
    {
        [ParentComponent]
        public SpriteRenderer[] parents;
    }

    internal sealed class ParentListTester : MonoBehaviour
    {
        [ParentComponent]
        public List<SpriteRenderer> parents;
    }

    internal sealed class ParentOptionalTester2 : MonoBehaviour
    {
        [ParentComponent(Optional = true)]
        public SpriteRenderer optionalParent;
    }

    internal sealed class ParentOnlyAncestorsTester2 : MonoBehaviour
    {
        [ParentComponent(OnlyAncestors = true)]
        public SpriteRenderer ancestorOnly;
    }

    internal sealed class ParentInactiveTester2 : MonoBehaviour
    {
        [ParentComponent(IncludeInactive = true)]
        public SpriteRenderer includeInactive;

        [ParentComponent(IncludeInactive = false)]
        public SpriteRenderer excludeInactive;
    }

    // Child test components
    internal sealed class BasicChildTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public SpriteRenderer child;
    }

    internal sealed class ChildArrayTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public SpriteRenderer[] children;
    }

    internal sealed class ChildListTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public List<SpriteRenderer> children;
    }

    internal sealed class ChildOnlyDescendantsTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public SpriteRenderer descendantOnly;
    }

    internal sealed class ChildOrderTester : MonoBehaviour
    {
        [ChildComponent]
        public SpriteRenderer[] children;
    }

    // Sibling test components
    internal sealed class BasicSiblingTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider sibling;
    }

    internal sealed class SiblingArrayTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider[] siblings;
    }

    internal sealed class SiblingListTester : MonoBehaviour
    {
        [SiblingComponent]
        public List<BoxCollider> siblings;
    }

    internal sealed class SiblingInactiveTester : MonoBehaviour
    {
        [SiblingComponent(IncludeInactive = true)]
        public BoxCollider includeInactive;

        [SiblingComponent(IncludeInactive = false, Optional = true)]
        public BoxCollider excludeInactive;
    }

    // Relational test component
    internal sealed class AllRelationalTester : MonoBehaviour
    {
        [ParentComponent(OnlyAncestors = true)]
        public SpriteRenderer parentRenderer;

        [ChildComponent(OnlyDescendants = false)]
        public SpriteRenderer childRenderer;

        [SiblingComponent]
        public BoxCollider siblingCollider;
    }

    // SkipIfAssigned test component
    internal sealed class SkipIfAssignedTester : MonoBehaviour
    {
        [ParentComponent(SkipIfAssigned = true)]
        public SpriteRenderer preAssigned;

        [ParentComponent]
        public SpriteRenderer normal;
    }
}

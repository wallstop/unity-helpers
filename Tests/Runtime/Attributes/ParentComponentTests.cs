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

    public sealed class ParentComponentTests
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
            GameObject root = new("PartComponentTest - Root", typeof(SpriteRenderer));
            _spawned.Add(root);
            GameObject parentLevel1 = new("ParentLevel1", typeof(SpriteRenderer));
            _spawned.Add(parentLevel1);
            parentLevel1.transform.SetParent(root.transform);
            GameObject parentLevel2 = new("ParentLevel2", typeof(SpriteRenderer));
            _spawned.Add(parentLevel2);
            parentLevel2.transform.SetParent(parentLevel1.transform);
            GameObject parentLevel3 = new(
                "ParentLevel3",
                typeof(SpriteRenderer),
                typeof(ExpectParentSpriteRenderers)
            );
            _spawned.Add(parentLevel3);
            parentLevel3.transform.SetParent(parentLevel2.transform);

            ExpectParentSpriteRenderers expect =
                parentLevel3.GetComponent<ExpectParentSpriteRenderers>();
            expect.AssignParentComponents();

            Assert.AreEqual(4, expect.exclusiveParentList.Count);
            Assert.IsTrue(
                expect.exclusiveParentList.Contains(parentLevel3.GetComponent<SpriteRenderer>())
            );
            Assert.IsTrue(
                expect.exclusiveParentList.Contains(parentLevel2.GetComponent<SpriteRenderer>())
            );
            Assert.IsTrue(
                expect.exclusiveParentList.Contains(parentLevel1.GetComponent<SpriteRenderer>())
            );
            Assert.IsTrue(expect.exclusiveParentList.Contains(root.GetComponent<SpriteRenderer>()));
            Assert.IsTrue(
                expect.exclusiveParentList.ToHashSet().SetEquals(expect.exclusiveParentArray)
            );

            Assert.AreEqual(3, expect.inclusiveParentList.Count);
            Assert.IsTrue(
                expect.inclusiveParentList.Contains(parentLevel2.GetComponent<SpriteRenderer>())
            );
            Assert.IsTrue(
                expect.inclusiveParentList.Contains(parentLevel1.GetComponent<SpriteRenderer>())
            );
            Assert.IsTrue(expect.inclusiveParentList.Contains(root.GetComponent<SpriteRenderer>()));
            Assert.IsTrue(
                expect.inclusiveParentList.ToHashSet().SetEquals(expect.inclusiveParentArray)
            );

            Assert.IsTrue(expect.exclusiveParent != null);
            Assert.AreEqual(expect.GetComponent<SpriteRenderer>(), expect.exclusiveParent);

            Assert.IsTrue(expect.inclusiveParent != null);
            Assert.AreEqual(parentLevel2.GetComponent<SpriteRenderer>(), expect.inclusiveParent);

            yield break;
        }

        [UnityTest]
        public IEnumerator IncludeInactiveControlsAncestorSelection()
        {
            GameObject activeRoot = new("ParentActiveRoot", typeof(SpriteRenderer));
            _spawned.Add(activeRoot);
            SpriteRenderer rootRenderer = activeRoot.GetComponent<SpriteRenderer>();

            GameObject inactiveParent = new("ParentInactive", typeof(SpriteRenderer));
            _spawned.Add(inactiveParent);
            inactiveParent.transform.SetParent(activeRoot.transform);
            inactiveParent.SetActive(false);
            SpriteRenderer inactiveRenderer = inactiveParent.GetComponent<SpriteRenderer>();

            GameObject child = new(
                "ParentChild",
                typeof(SpriteRenderer),
                typeof(ParentAssignmentTester)
            );
            _spawned.Add(child);
            child.transform.SetParent(inactiveParent.transform);
            ParentAssignmentTester tester = child.GetComponent<ParentAssignmentTester>();
            SpriteRenderer childRenderer = child.GetComponent<SpriteRenderer>();

            tester.AssignParentComponents();

            Assert.AreSame(rootRenderer, tester.ancestorsActiveOnly);
            Assert.AreSame(inactiveRenderer, tester.ancestorsIncludeInactive);
            CollectionAssert.AreEquivalent(
                new[] { childRenderer, inactiveRenderer, rootRenderer },
                tester.allParents
            );

            yield break;
        }

        [UnityTest]
        public IEnumerator MissingRequiredParentLogsError()
        {
            GameObject orphan = new("ParentOrphan", typeof(ParentMissingTester));
            _spawned.Add(orphan);
            ParentMissingTester tester = orphan.GetComponent<ParentMissingTester>();

            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|ParentOrphan\[ParentMissingTester\]\|Unable to find parent component of type UnityEngine\.SpriteRenderer$"
                )
            );

            tester.AssignParentComponents();
            Assert.IsNull(tester.requiredRenderer);
            yield break;
        }

        [UnityTest]
        public IEnumerator SkipIfAssignedPreservesExistingValues()
        {
            GameObject root = new("SkipIfAssignedRoot", typeof(SpriteRenderer));
            _spawned.Add(root);
            GameObject child = new(
                "SkipIfAssignedChild",
                typeof(SpriteRenderer),
                typeof(ParentSkipIfAssignedTester)
            );
            _spawned.Add(child);
            child.transform.SetParent(root.transform);

            ParentSkipIfAssignedTester tester = child.GetComponent<ParentSkipIfAssignedTester>();
            SpriteRenderer childRenderer = child.GetComponent<SpriteRenderer>();
            SpriteRenderer rootRenderer = root.GetComponent<SpriteRenderer>();

            // Pre-assign values that should NOT be overwritten
            tester.preAssignedParent = childRenderer;
            tester.preAssignedParentArray = new SpriteRenderer[] { childRenderer };
            tester.preAssignedParentList = new List<SpriteRenderer> { childRenderer };

            // Call assignment
            tester.AssignParentComponents();

            // Verify pre-assigned values were preserved (skipIfAssigned = true)
            Assert.AreSame(childRenderer, tester.preAssignedParent);
            Assert.AreEqual(1, tester.preAssignedParentArray.Length);
            Assert.AreSame(childRenderer, tester.preAssignedParentArray[0]);
            Assert.AreEqual(1, tester.preAssignedParentList.Count);
            Assert.AreSame(childRenderer, tester.preAssignedParentList[0]);

            // Verify normal assignments (without skipIfAssigned) were assigned
            Assert.AreSame(rootRenderer, tester.normalParent);

            yield break;
        }
    }

    internal sealed class ParentAssignmentTester : MonoBehaviour
    {
        [ParentComponent(onlyAncestors = true, includeInactive = false)]
        public SpriteRenderer ancestorsActiveOnly;

        [ParentComponent(onlyAncestors = true, includeInactive = true)]
        public SpriteRenderer ancestorsIncludeInactive;

        [ParentComponent(includeInactive = true)]
        public List<SpriteRenderer> allParents;
    }

    internal sealed class ParentMissingTester : MonoBehaviour
    {
        [ParentComponent(onlyAncestors = true)]
        public SpriteRenderer requiredRenderer;
    }

    internal sealed class ParentSkipIfAssignedTester : MonoBehaviour
    {
        [ParentComponent(skipIfAssigned = true)]
        public SpriteRenderer preAssignedParent;

        [ParentComponent(skipIfAssigned = true)]
        public SpriteRenderer[] preAssignedParentArray;

        [ParentComponent(skipIfAssigned = true)]
        public List<SpriteRenderer> preAssignedParentList;

        [ParentComponent]
        public SpriteRenderer normalParent;
    }
}

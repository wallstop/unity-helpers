namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;

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

            Assert.IsNull(tester.optional);
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
                    @"^\d+(\.\d+)?\|SiblingMissing\[SiblingMissingComponent\]\|Unable to find sibling component of type UnityEngine\.Rigidbody$"
                )
            );

            tester.AssignSiblingComponents();

            Assert.IsNull(tester.required);
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

            // Verify pre-assigned values were preserved (skipIfAssigned = true)
            Assert.AreSame(second, tester.preAssignedSibling);
            Assert.AreEqual(1, tester.preAssignedSiblingArray.Length);
            Assert.AreSame(second, tester.preAssignedSiblingArray[0]);
            Assert.AreEqual(1, tester.preAssignedSiblingList.Count);
            Assert.AreSame(second, tester.preAssignedSiblingList[0]);

            // Verify normal assignments (without skipIfAssigned) were assigned
            Assert.AreSame(first, tester.normalSibling);

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

        [SiblingComponent(optional = true)]
        public Rigidbody optional;
    }

    internal sealed class SiblingMissingComponent : MonoBehaviour
    {
        [SiblingComponent]
        public Rigidbody required;
    }

    internal sealed class SiblingSkipIfAssignedTester : MonoBehaviour
    {
        [SiblingComponent(skipIfAssigned = true)]
        public BoxCollider preAssignedSibling;

        [SiblingComponent(skipIfAssigned = true)]
        public BoxCollider[] preAssignedSiblingArray;

        [SiblingComponent(skipIfAssigned = true)]
        public List<BoxCollider> preAssignedSiblingList;

        [SiblingComponent]
        public BoxCollider normalSibling;
    }
}

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
}

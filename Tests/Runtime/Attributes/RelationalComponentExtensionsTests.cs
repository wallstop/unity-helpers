namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [TestFixture]
    public sealed class RelationalComponentExtensionsTests
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
        public IEnumerator AssignRelationalComponentsResolvesParentSiblingAndChild()
        {
            GameObject parent = new("RelationalParent", typeof(Rigidbody));
            _spawned.Add(parent);
            Rigidbody parentBody = parent.GetComponent<Rigidbody>();

            GameObject middle = new(
                "RelationalMiddle",
                typeof(BoxCollider),
                typeof(RelationalComponentTester)
            );
            _spawned.Add(middle);
            middle.transform.SetParent(parent.transform);
            BoxCollider siblingCollider = middle.GetComponent<BoxCollider>();

            GameObject child = new("RelationalChild", typeof(CapsuleCollider));
            _spawned.Add(child);
            child.transform.SetParent(middle.transform);
            CapsuleCollider childCollider = child.GetComponent<CapsuleCollider>();

            RelationalComponentTester tester = middle.GetComponent<RelationalComponentTester>();
            tester.AssignRelationalComponents();

            Assert.AreSame(parentBody, tester.parentBody);
            Assert.AreSame(siblingCollider, tester.siblingCollider);
            Assert.AreSame(childCollider, tester.childCollider);

            yield break;
        }
    }

    internal sealed class RelationalComponentTester : MonoBehaviour
    {
        [ParentComponent(onlyAncestors = true)]
        public Rigidbody parentBody;

        [SiblingComponent]
        public BoxCollider siblingCollider;

        [ChildComponent(onlyDescendents = true)]
        public CapsuleCollider childCollider;
    }
}

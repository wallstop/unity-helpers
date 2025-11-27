namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    [TestFixture]
    public sealed class RelationalComponentExtensionsTests : CommonTestBase
    {
        // Tracking handled by CommonTestBase

        [UnityTest]
        public IEnumerator AssignRelationalComponentsResolvesParentSiblingAndChild()
        {
            GameObject parent = Track(new GameObject("RelationalParent", typeof(Rigidbody)));
            Rigidbody parentBody = parent.GetComponent<Rigidbody>();

            GameObject middle = new(
                "RelationalMiddle",
                typeof(BoxCollider),
                typeof(RelationalComponentTester)
            );
            middle = Track(middle);
            middle.transform.SetParent(parent.transform);
            BoxCollider siblingCollider = middle.GetComponent<BoxCollider>();

            GameObject child = Track(new GameObject("RelationalChild", typeof(CapsuleCollider)));
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
        [ParentComponent(OnlyAncestors = true)]
        public Rigidbody parentBody;

        [SiblingComponent]
        public BoxCollider siblingCollider;

        [ChildComponent(OnlyDescendants = true)]
        public CapsuleCollider childCollider;
    }
}

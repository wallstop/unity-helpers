namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildMultiComponentTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public BoxCollider[] colliders;
    }
}

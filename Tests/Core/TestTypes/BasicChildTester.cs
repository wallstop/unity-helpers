namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class BasicChildTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public SpriteRenderer child;
    }
}

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildMissingTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public SpriteRenderer requiredRenderer;
    }
}

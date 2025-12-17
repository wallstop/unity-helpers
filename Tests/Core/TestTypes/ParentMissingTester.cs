namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentMissingTester : MonoBehaviour
    {
        [ParentComponent(OnlyAncestors = true)]
        public SpriteRenderer requiredRenderer;
    }
}

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildDepthAndNameTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, MaxDepth = 1, NameFilter = "Player")]
        public SpriteRenderer[] depth1PlayerChildren;
    }
}

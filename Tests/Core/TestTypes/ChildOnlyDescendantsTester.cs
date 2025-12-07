namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildOnlyDescendantsTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public SpriteRenderer descendantOnly;
    }
}

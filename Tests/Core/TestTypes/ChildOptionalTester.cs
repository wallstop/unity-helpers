namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildOptionalTester : MonoBehaviour
    {
        [ChildComponent(Optional = true)]
        public SpriteRenderer optionalRenderer;
    }
}

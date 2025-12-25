namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentOptionalTester : MonoBehaviour
    {
        [ParentComponent(Optional = true)]
        public SpriteRenderer optionalRenderer;
    }
}

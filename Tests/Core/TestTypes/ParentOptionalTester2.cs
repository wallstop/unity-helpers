namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    internal sealed class ParentOptionalTester2 : MonoBehaviour
    {
        [ParentComponent(Optional = true)]
        public SpriteRenderer optionalParent;
    }
}

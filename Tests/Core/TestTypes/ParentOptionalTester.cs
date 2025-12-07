using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class ParentOptionalTester : MonoBehaviour
    {
        [ParentComponent(Optional = true)]
        public SpriteRenderer optionalRenderer;
    }
}

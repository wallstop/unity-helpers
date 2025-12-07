using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SiblingOptionalTester : MonoBehaviour
    {
        [SiblingComponent(Optional = true)]
        public BoxCollider optionalCollider;
    }
}

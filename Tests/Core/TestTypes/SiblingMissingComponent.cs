using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SiblingMissingComponent : MonoBehaviour
    {
        [SiblingComponent]
        public Rigidbody required;
    }
}

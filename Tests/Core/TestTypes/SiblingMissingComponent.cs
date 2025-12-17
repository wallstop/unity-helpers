namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingMissingComponent : MonoBehaviour
    {
        [SiblingComponent]
        public Rigidbody required;
    }
}

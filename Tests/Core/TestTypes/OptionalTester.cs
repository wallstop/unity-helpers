namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class OptionalTester : MonoBehaviour
    {
        [SiblingComponent(Optional = true)]
        public Rigidbody missingOptional;
    }
}

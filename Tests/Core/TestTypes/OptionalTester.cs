using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class OptionalTester : MonoBehaviour
    {
        [SiblingComponent(Optional = true)]
        public Rigidbody missingOptional;
    }
}

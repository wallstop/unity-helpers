namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingListTester : MonoBehaviour
    {
        [SiblingComponent]
        public List<BoxCollider> siblings;
    }
}

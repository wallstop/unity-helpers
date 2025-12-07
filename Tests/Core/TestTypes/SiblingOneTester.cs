using System.Collections.Generic;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SiblingOneTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider single;

        [SiblingComponent]
        public BoxCollider[] array;

        [SiblingComponent]
        public List<BoxCollider> list;
    }
}

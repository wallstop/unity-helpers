using System.Collections.Generic;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SiblingExcludeInactiveTester : MonoBehaviour
    {
        [SiblingComponent(IncludeInactive = false)]
        public BoxCollider activeOnlySingle;

        [SiblingComponent(IncludeInactive = false)]
        public BoxCollider[] activeOnlyArray;

        [SiblingComponent(IncludeInactive = false)]
        public List<BoxCollider> activeOnlyList;
    }
}

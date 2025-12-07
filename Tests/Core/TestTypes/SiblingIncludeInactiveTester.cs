using System.Collections.Generic;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SiblingIncludeInactiveTester : MonoBehaviour
    {
        [SiblingComponent(IncludeInactive = true)]
        public BoxCollider includeInactiveSingle;

        [SiblingComponent(IncludeInactive = true)]
        public BoxCollider[] includeInactiveArray;

        [SiblingComponent(IncludeInactive = true)]
        public List<BoxCollider> includeInactiveList;
    }
}

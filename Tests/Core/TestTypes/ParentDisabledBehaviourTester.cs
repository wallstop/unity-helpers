using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class ParentDisabledBehaviourTester : MonoBehaviour
    {
        [ParentComponent(IncludeInactive = false)]
        public BoxCollider parentCollider;
    }
}

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentDisabledBehaviourTester : MonoBehaviour
    {
        [ParentComponent(IncludeInactive = false)]
        public BoxCollider parentCollider;
    }
}

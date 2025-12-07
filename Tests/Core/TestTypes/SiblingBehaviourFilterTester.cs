using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SiblingBehaviourFilterTester : MonoBehaviour
    {
        [SiblingComponent(IncludeInactive = false)]
        public SiblingTestBehaviour[] activeBehaviours;
    }
}

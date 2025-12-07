using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SiblingBehaviourTester : MonoBehaviour
    {
        [SiblingComponent(IncludeInactive = true)]
        public SiblingTestBehaviour[] allBehaviours;
    }
}

namespace WallstopStudios.UnityHelpers.Tests.Runtime.TestTypes.Odin.ValidateAssignment
{
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for ValidateAssignment on SerializedMonoBehaviour with Transform field.
    /// </summary>
    public sealed class OdinValidateAssignmentMonoBehaviourTarget : SerializedMonoBehaviour
    {
        [ValidateAssignment]
        public Transform validateReference;
    }
#endif
}

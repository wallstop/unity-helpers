namespace WallstopStudios.UnityHelpers.Tests.Runtime.TestTypes.Odin.ValidateAssignment
{
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for ValidateAssignment on SerializedMonoBehaviour with multiple fields.
    /// </summary>
    public sealed class OdinValidateAssignmentMonoBehaviourFieldsTarget : SerializedMonoBehaviour
    {
        [ValidateAssignment]
        public Transform validateTransform;

        [ValidateAssignment]
        public string validateName;
    }
#endif
}

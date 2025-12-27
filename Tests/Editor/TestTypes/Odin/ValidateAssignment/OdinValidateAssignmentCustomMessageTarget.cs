namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ValidateAssignment
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for ValidateAssignment with custom message.
    /// </summary>
    internal sealed class OdinValidateAssignmentCustomMessageTarget : SerializedScriptableObject
    {
        [ValidateAssignment("Enemy prefab must be assigned for spawning to work")]
        public GameObject validateWithCustomMessage;
    }
#endif
}

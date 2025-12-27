namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ValidateAssignment
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for ValidateAssignment on SerializedScriptableObject with GameObject field.
    /// </summary>
    internal sealed class OdinValidateAssignmentScriptableObjectTarget : SerializedScriptableObject
    {
        [ValidateAssignment]
        public GameObject validateObject;
    }
#endif
}

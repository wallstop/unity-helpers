namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ValidateAssignment
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for ValidateAssignment with Error message type.
    /// </summary>
    internal sealed class OdinValidateAssignmentErrorTypeTarget : SerializedScriptableObject
    {
        [ValidateAssignment(ValidateAssignmentMessageType.Error)]
        public string validateError;
    }
#endif
}

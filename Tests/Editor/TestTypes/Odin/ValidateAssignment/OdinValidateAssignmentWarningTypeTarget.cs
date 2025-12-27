namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ValidateAssignment
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for ValidateAssignment with Warning message type.
    /// </summary>
    internal sealed class OdinValidateAssignmentWarningTypeTarget : SerializedScriptableObject
    {
        [ValidateAssignment(ValidateAssignmentMessageType.Warning)]
        public string validateWarning;
    }
#endif
}

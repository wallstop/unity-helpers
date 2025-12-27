namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ValidateAssignment
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for ValidateAssignment on List fields.
    /// </summary>
    internal sealed class OdinValidateAssignmentListTarget : SerializedScriptableObject
    {
        [ValidateAssignment]
        public List<string> validateList;
    }
#endif
}

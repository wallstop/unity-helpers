namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ValidateAssignment
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for ValidateAssignment on Stack fields.
    /// </summary>
    internal sealed class OdinValidateAssignmentStackTarget : SerializedScriptableObject
    {
        [ValidateAssignment]
        public Stack<int> validateStack;
    }
#endif
}

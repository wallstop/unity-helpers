namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ValidateAssignment
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for ValidateAssignment on HashSet fields.
    /// </summary>
    internal sealed class OdinValidateAssignmentHashSetTarget : SerializedScriptableObject
    {
        [ValidateAssignment]
        public HashSet<int> validateHashSet;
    }
#endif
}

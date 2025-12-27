namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ValidateAssignment
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for ValidateAssignment on array fields.
    /// </summary>
    internal sealed class OdinValidateAssignmentArrayTarget : SerializedScriptableObject
    {
        [ValidateAssignment]
        public int[] validateArray;
    }
#endif
}

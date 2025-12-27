namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ReadOnly
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for mixed WReadOnly and editable fields with Odin Inspector.
    /// </summary>
    internal sealed class OdinReadOnlyMixedFieldsTarget : SerializedScriptableObject
    {
        [WReadOnly]
        public string readOnlyField;

        public string editableField;
    }
#endif
}

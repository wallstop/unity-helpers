namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.StringInList
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for StringInList attribute with unsupported field type (float).
    /// </summary>
    internal sealed class OdinStringInListUnsupportedTypeTarget : SerializedScriptableObject
    {
        [StringInList("Option1", "Option2")]
        public float unsupportedField;
    }
#endif
}

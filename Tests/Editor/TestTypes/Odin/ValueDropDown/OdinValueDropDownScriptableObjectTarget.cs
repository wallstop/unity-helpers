namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ValueDropDown
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WValueDropDown with inline string options on SerializedScriptableObject.
    /// </summary>
    internal sealed class OdinValueDropDownScriptableObjectTarget : SerializedScriptableObject
    {
        [WValueDropDown("Option1", "Option2", "Option3")]
        public string selectedOption;
    }
#endif
}

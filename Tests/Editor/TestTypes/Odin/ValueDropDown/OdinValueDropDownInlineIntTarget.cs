namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ValueDropDown
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WValueDropDown with inline integer options.
    /// </summary>
    internal sealed class OdinValueDropDownInlineIntTarget : SerializedScriptableObject
    {
        [WValueDropDown(0, 25, 50, 75, 100)]
        public int percentage;
    }
#endif
}

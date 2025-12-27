namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.IntDropDown
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for IntDropDown attribute on a SerializedScriptableObject with inline int options.
    /// </summary>
    internal sealed class OdinIntDropDownScriptableObjectTarget : SerializedScriptableObject
    {
        [IntDropDown(30, 60, 120)]
        public int frameRate;
    }
#endif
}

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ValueDropDown
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WValueDropDown with instance method provider for dynamic options.
    /// </summary>
    internal sealed class OdinValueDropDownInstanceProviderTarget : SerializedScriptableObject
    {
        public string[] availableOptions = { "InstanceA", "InstanceB", "InstanceC" };

        [WValueDropDown(nameof(GetDynamicOptions), typeof(string))]
        public string dynamicSelection;

        public IEnumerable<string> GetDynamicOptions()
        {
            return availableOptions;
        }
    }
#endif
}

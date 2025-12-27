namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.StringInList
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for StringInList attribute with instance method provider.
    /// </summary>
    internal sealed class OdinStringInListInstanceProviderTarget : SerializedScriptableObject
    {
        public string[] dynamicOptions = { "DynamicA", "DynamicB", "DynamicC" };

        [StringInList(nameof(GetInstanceOptions))]
        public string instanceProviderSelection;

        public IEnumerable<string> GetInstanceOptions()
        {
            return dynamicOptions;
        }
    }
#endif
}

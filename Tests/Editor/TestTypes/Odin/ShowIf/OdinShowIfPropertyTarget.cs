namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ShowIf
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WShowIf property-based condition tests with Odin Inspector.
    /// </summary>
    internal sealed class OdinShowIfPropertyTarget : SerializedScriptableObject
    {
        public bool boolField;
        public int intField;

        public bool ComputedProperty => boolField && intField > 0;

        [WShowIf(nameof(ComputedProperty))]
        public int dependentField;
    }
#endif
}

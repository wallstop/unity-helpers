namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ReadOnly
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WReadOnly attribute on Color field with Odin Inspector.
    /// </summary>
    internal sealed class OdinReadOnlyColorTarget : SerializedScriptableObject
    {
        [WReadOnly]
        public Color readOnlyColor;
    }
#endif
}

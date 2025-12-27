namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ReadOnly
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WReadOnly attribute on Vector3Int field with Odin Inspector.
    /// </summary>
    internal sealed class OdinReadOnlyVector3IntTarget : SerializedScriptableObject
    {
        [WReadOnly]
        public Vector3Int readOnlyVector3Int;
    }
#endif
}

// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ReadOnly
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WReadOnly attribute on int field with Odin Inspector.
    /// </summary>
    internal sealed class OdinReadOnlyIntTarget : SerializedScriptableObject
    {
        [WReadOnly]
        public int readOnlyInt;
    }
#endif
}

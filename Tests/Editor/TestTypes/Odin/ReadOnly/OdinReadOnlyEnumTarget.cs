// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ReadOnly
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums;

    /// <summary>
    /// Test target for WReadOnly attribute on enum field with Odin Inspector.
    /// </summary>
    internal sealed class OdinReadOnlyEnumTarget : SerializedScriptableObject
    {
        [WReadOnly]
        public OdinTestEnum readOnlyEnum;
    }
#endif
}

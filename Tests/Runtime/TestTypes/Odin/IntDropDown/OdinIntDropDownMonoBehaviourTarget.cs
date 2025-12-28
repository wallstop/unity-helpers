// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Runtime.TestTypes.Odin.IntDropDown
{
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for IntDropDown attribute on a SerializedMonoBehaviour with inline int options.
    /// </summary>
    public sealed class OdinIntDropDownMonoBehaviourTarget : SerializedMonoBehaviour
    {
        [IntDropDown(1, 2, 3, 4, 5)]
        public int selectedLevel;
    }
#endif
}

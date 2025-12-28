// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Runtime.TestTypes.Odin.ValueDropDown
{
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WValueDropDown with inline int options on SerializedMonoBehaviour.
    /// </summary>
    public sealed class OdinValueDropDownMonoBehaviourTarget : SerializedMonoBehaviour
    {
        [WValueDropDown(10, 20, 30, 40, 50)]
        public int selectedValue;
    }
#endif
}

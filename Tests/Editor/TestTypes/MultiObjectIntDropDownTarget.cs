// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class MultiObjectIntDropDownTarget : ScriptableObject
    {
        [IntDropDown(10, 20, 30, 40, 50)]
        public int selection = 10;
    }
#endif
}

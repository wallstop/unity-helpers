// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using Extension;
    using UnityEngine;

    public static partial class Helpers
    {
        public static void LogNotAssigned(this Object component, string name)
        {
            component.LogWarn($"{name} not found.");
        }
    }
}

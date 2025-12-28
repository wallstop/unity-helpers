// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AnimationEventAttribute : Attribute
    {
        public bool ignoreDerived = true;
    }
}

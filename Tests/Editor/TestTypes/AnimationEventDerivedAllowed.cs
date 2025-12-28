// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class AnimationEventDerivedAllowed : AnimationEventSource
    {
        [AnimationEvent(ignoreDerived = false)]
        internal void DerivedOnly() { }
    }
}

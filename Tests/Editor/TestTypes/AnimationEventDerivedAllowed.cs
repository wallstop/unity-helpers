using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    public sealed class AnimationEventDerivedAllowed : AnimationEventSource
    {
        [AnimationEvent(ignoreDerived = false)]
        internal void DerivedOnly() { }
    }
}

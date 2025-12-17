namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class AnimationEventDerivedAllowed : AnimationEventSource
    {
        [AnimationEvent(ignoreDerived = false)]
        internal void DerivedOnly() { }
    }
}

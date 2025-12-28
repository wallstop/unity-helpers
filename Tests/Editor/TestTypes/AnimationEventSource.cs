// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public class AnimationEventSource : MonoBehaviour
    {
        [AnimationEvent]
        protected internal void SimpleEvent() { }

        [AnimationEvent(ignoreDerived = false)]
        protected internal void AllowDerived() { }

        [AnimationEvent]
        private int InvalidReturn() => 0;

        [AnimationEvent]
        private void InvalidParameter(Vector3 _) { }
    }
}

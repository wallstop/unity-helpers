using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
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

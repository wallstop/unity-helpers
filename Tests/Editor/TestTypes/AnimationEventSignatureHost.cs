namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tests.Attributes;

    public sealed class AnimationEventSignatureHost : MonoBehaviour
    {
        [AnimationEvent]
        public void NoArgs() { }

        [AnimationEvent]
        public void WithInt(int value) { }

        [AnimationEvent]
        public void WithEnum(AnimationEventSignal signal) { }

        [AnimationEvent]
        public void WithFloat(float value) { }

        [AnimationEvent]
        public void WithString(string text) { }

        [AnimationEvent]
        public void WithUnityObject(Object target) { }

        [AnimationEvent]
        public void TwoParameters(int value, string text) { }

        [AnimationEvent]
        public int NonVoidReturn() => 0;
    }
}

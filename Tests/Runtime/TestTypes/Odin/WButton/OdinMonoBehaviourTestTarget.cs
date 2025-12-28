// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Runtime.TestTypes.Odin.WButton
{
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WButton attribute on SerializedMonoBehaviour with Odin Inspector.
    /// </summary>
    public sealed class OdinMonoBehaviourTestTarget : SerializedMonoBehaviour
    {
        public int ActionCount;

        [WButton("Test Action")]
        public void TestAction()
        {
            ActionCount++;
        }

        [WButton]
        private void PrivateButton() { }
    }
#endif
}

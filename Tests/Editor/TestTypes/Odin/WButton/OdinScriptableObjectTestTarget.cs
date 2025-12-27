namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.WButton
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WButton attribute on SerializedScriptableObject with Odin Inspector.
    /// </summary>
    internal sealed class OdinScriptableObjectTestTarget : SerializedScriptableObject
    {
        public int InvocationCount;

        [WButton]
        public void SimpleButton()
        {
            InvocationCount++;
        }

        [WButton("Custom Display Name")]
        public void MethodWithCustomDisplay()
        {
            // Custom display name test
        }
    }
#endif
}

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ReadOnly
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System;

    /// <summary>
    /// A test serializable class used by OdinReadOnlySerializableClassTarget.
    /// </summary>
    [Serializable]
    internal sealed class OdinTestSerializableClass
    {
        public int intValue;
        public string stringValue;
    }
#endif
}

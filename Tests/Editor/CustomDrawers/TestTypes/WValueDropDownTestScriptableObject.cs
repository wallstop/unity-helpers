namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using UnityEngine;

    /// <summary>
    /// A simple ScriptableObject for testing WValueDropDown object reference dropdown functionality.
    /// </summary>
    internal sealed class WValueDropDownTestScriptableObject : ScriptableObject
    {
        public string displayValue = string.Empty;
        public int identifier;
    }
#endif
}

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for verifying group name with unicode characters.
    /// </summary>
    public sealed class WButtonUnicodeGroupNameTarget : ScriptableObject
    {
        [WButton("Validate Config", drawOrder: 0, groupName: "Setup")]
        public void ValidateConfig() { }

        [WButton("Run Tests", drawOrder: 0, groupName: "Testing")]
        public void RunTests() { }
    }
}

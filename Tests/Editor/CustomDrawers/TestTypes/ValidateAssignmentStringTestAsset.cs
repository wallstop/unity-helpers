namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for ValidateAssignment attribute with string fields.
    /// </summary>
    internal sealed class ValidateAssignmentStringTestAsset : ScriptableObject
    {
        [ValidateAssignment]
        public string requiredString;

        [ValidateAssignment(ValidateAssignmentMessageType.Error, "Configuration name is required")]
        public string configName;
    }
#endif
}

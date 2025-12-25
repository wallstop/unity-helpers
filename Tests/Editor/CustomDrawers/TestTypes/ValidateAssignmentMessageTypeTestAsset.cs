namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for ValidateAssignment attribute with custom message types.
    /// </summary>
    internal sealed class ValidateAssignmentMessageTypeTestAsset : ScriptableObject
    {
        [ValidateAssignment(ValidateAssignmentMessageType.Warning)]
        public GameObject warningField;

        [ValidateAssignment(ValidateAssignmentMessageType.Error)]
        public GameObject errorField;

        [ValidateAssignment]
        public GameObject defaultField;
    }
#endif
}

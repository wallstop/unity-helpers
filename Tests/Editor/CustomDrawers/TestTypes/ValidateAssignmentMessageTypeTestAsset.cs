// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

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

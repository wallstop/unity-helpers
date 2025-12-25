namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for ValidateAssignment attribute with custom messages.
    /// </summary>
    internal sealed class ValidateAssignmentCustomMessageTestAsset : ScriptableObject
    {
        [ValidateAssignment("Player prefab is required for spawning")]
        public GameObject playerPrefab;

        [ValidateAssignment(
            ValidateAssignmentMessageType.Error,
            "Audio source must be assigned for sound effects"
        )]
        public AudioSource audioSource;

        [ValidateAssignment(ValidateAssignmentMessageType.Warning, "Optional warning message")]
        public Transform optionalTransform;
    }
#endif
}

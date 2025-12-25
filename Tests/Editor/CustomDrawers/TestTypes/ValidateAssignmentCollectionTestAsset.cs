namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for ValidateAssignment attribute with collection fields.
    /// </summary>
    internal sealed class ValidateAssignmentCollectionTestAsset : ScriptableObject
    {
        [ValidateAssignment]
        public List<int> requiredList = new();

        [ValidateAssignment]
        public int[] requiredArray;

        [ValidateAssignment(ValidateAssignmentMessageType.Error, "Spawn points cannot be empty")]
        public List<Transform> spawnPoints = new();
    }
#endif
}

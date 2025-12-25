namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for ValidateAssignment attribute with object reference fields.
    /// </summary>
    internal sealed class ValidateAssignmentObjectReferenceTestAsset : ScriptableObject
    {
        [ValidateAssignment]
        public GameObject requiredGameObject;

        [ValidateAssignment]
        public Transform requiredTransform;

        [ValidateAssignment]
        public ScriptableObject requiredScriptableObject;

        [ValidateAssignment]
        public Material requiredMaterial;
    }
#endif
}

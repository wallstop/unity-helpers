namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for ValidateAssignment attribute with multiple field types to test various scenarios.
    /// </summary>
    internal sealed class ValidateAssignmentMixedFieldsTestAsset : ScriptableObject
    {
        [ValidateAssignment]
        public GameObject nullableGameObject;

        [ValidateAssignment]
        public string nullableString;

        [ValidateAssignment]
        public List<int> nullableList = new();

        [ValidateAssignment]
        public Sprite nullableSprite;

        public int nonDecoratedIntField;

        public string nonDecoratedStringField;

        public GameObject nonDecoratedGameObject;
    }
#endif
}

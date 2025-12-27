namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ValidateAssignment
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for ValidateAssignment with fields containing valid values.
    /// </summary>
    internal sealed class OdinValidateAssignmentValidValuesTarget : SerializedScriptableObject
    {
        [ValidateAssignment]
        public GameObject validObject;

        [ValidateAssignment]
        public string validString;

        [ValidateAssignment]
        public List<int> validList;

        [ValidateAssignment]
        public float[] validArray;
    }
#endif
}

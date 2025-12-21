#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for array fields within WGroups.
    /// </summary>
    internal sealed class ArrayFieldsTarget : ScriptableObject
    {
        [WGroup("Arrays", "Array Types")]
        public int[] intArray = Array.Empty<int>();

        public string[] stringArray = Array.Empty<string>();

        [WGroupEnd("Arrays")]
        public float[] floatArray = Array.Empty<float>();

        public int afterArrayField;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif

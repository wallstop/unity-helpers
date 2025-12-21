#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for mixed simple and complex fields.
    /// </summary>
    internal sealed class MixedFieldsTarget : ScriptableObject
    {
        [WGroup("Mixed", "Mixed Types")]
        public int simpleInt;

        public List<int> listField = new();

        public string simpleString;

        public NestedData nestedField = new();

        [WGroupEnd("Mixed")]
        public float simpleFloat;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif

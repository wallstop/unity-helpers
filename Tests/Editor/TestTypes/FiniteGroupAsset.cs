namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    internal sealed class FiniteGroupAsset : ScriptableObject
    {
        [WGroup("Stats")]
        public int primary;

        public int secondary;

        public int tertiary;

        public int outside;
    }
}

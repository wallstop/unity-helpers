// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

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

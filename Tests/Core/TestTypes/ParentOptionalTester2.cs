// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    internal sealed class ParentOptionalTester2 : MonoBehaviour
    {
        [ParentComponent(Optional = true)]
        public SpriteRenderer optionalParent;
    }
}

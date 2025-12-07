using System.Collections.Generic;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class ChildHashSetMaxCountTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, MaxCount = 2)]
        public HashSet<SpriteRenderer> limitedChildren;
    }
}

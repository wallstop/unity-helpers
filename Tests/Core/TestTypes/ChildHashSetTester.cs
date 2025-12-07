using System.Collections.Generic;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class ChildHashSetTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public HashSet<SpriteRenderer> childRenderers;
    }
}

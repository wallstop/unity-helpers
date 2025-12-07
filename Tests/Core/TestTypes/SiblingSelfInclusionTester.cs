using System.Collections.Generic;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SiblingSelfInclusionTester : MonoBehaviour
    {
        [SiblingComponent]
        public SpriteRenderer siblingRenderer;

        [SiblingComponent]
        public SpriteRenderer[] rendererArray;

        [SiblingComponent]
        public List<SpriteRenderer> rendererList;
    }
}

using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class ParentCacheIsolationTesterA : MonoBehaviour
    {
        [ParentComponent]
        public SpriteRenderer parentRenderer;
    }
}

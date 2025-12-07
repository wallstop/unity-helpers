using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class ParentCacheIsolationTesterB : MonoBehaviour
    {
        [ParentComponent]
        public SpriteRenderer parentRenderer;
    }
}

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentCacheIsolationTesterB : MonoBehaviour
    {
        [ParentComponent]
        public SpriteRenderer parentRenderer;
    }
}

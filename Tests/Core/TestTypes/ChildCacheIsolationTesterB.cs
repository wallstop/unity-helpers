namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildCacheIsolationTesterB : MonoBehaviour
    {
        [ChildComponent]
        public SpriteRenderer childRenderer;
    }
}

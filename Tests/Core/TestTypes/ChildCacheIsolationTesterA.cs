namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildCacheIsolationTesterA : MonoBehaviour
    {
        [ChildComponent]
        public SpriteRenderer childRenderer;
    }
}

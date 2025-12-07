namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildOrderTester : MonoBehaviour
    {
        [ChildComponent]
        public SpriteRenderer[] children;
    }
}

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildSingleTester : MonoBehaviour
    {
        [ChildComponent]
        public SpriteRenderer single;
    }
}

namespace WallstopStudios.UnityHelpers.Utils
{
    using UnityEngine;

    [DisallowMultipleComponent]
    public sealed class CenterPointOffset : MonoBehaviour
    {
        public Vector2 offset = Vector2.zero;

        public bool spriteUsesOffset = true;

        public Vector2 CenterPoint
        {
            get
            {
                Transform localTransform = transform;
                Vector2 scaledOffset = offset * localTransform.localScale;
                return (Vector2)localTransform.position + scaledOffset;
            }
        }
    }
}

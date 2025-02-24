namespace UnityHelpers.Utils
{
    using Core.Attributes;
    using UnityEngine;

    [DisallowMultipleComponent]
    public sealed class CenterPointOffset : MonoBehaviour
    {
        public Vector2 offset = Vector2.zero;

        public bool spriteUsesOffset = true;

        [SiblingComponent]
        private Transform _transform;

        private void Awake()
        {
            this.AssignSiblingComponents();
        }

        public Vector2 CenterPoint
        {
            get
            {
                Vector2 scaledOffset = offset * _transform.localScale;
                return (Vector2)_transform.position + scaledOffset;
            }
        }
    }
}

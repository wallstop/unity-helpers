namespace UnityHelpers.Tests.Components
{
    namespace UnityHelpers.Tests.Components
    {
        using Core.Attributes;
        using UnityEngine;

        [DisallowMultipleComponent]
        [RequireComponent(typeof(SpriteRenderer))]
        [RequireComponent(typeof(BoxCollider2D))]
        [RequireComponent(typeof(BoxCollider2D))]
        [RequireComponent(typeof(PolygonCollider2D))]
        public sealed class RelationalComponentTesterSimple : MonoBehaviour
        {
            [SiblingComponent]
            internal SpriteRenderer _spriteRenderer;

            [SiblingComponent]
            internal Transform _transform;

            [SiblingComponent]
            internal PolygonCollider2D _polygonCollider;

            // [ParentComponent]
            // internal PolygonCollider2D _polygonColliderParent;

            [SiblingComponent]
            internal BoxCollider2D _boxCollider;

            // [ParentComponent]
            // internal BoxCollider2D _boxColliderParent;
            //
            // [ChildComponent]
            // internal BoxCollider2D _boxColliderChild;
            //
            // [ChildComponent]
            // internal BoxCollider2D _boxColliderParentChild;
        }
    }
}

namespace UnityHelpers.Tests.Components
{
    using Core.Attributes;
    using UnityEngine;

    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(PolygonCollider2D))]
    public sealed class RelationalComponentTester : MonoBehaviour
    {
        [SiblingComponent]
        internal SpriteRenderer _spriteRenderer;

        [SiblingComponent]
        internal Transform _transform;

        [SiblingComponent]
        internal PolygonCollider2D _polygonCollider;

        [SiblingComponent]
        internal BoxCollider2D _boxCollider;

        [ChildComponent]
        internal Collider2D[] _childColliders;

        [ParentComponent]
        internal Collider2D[] _parentColliders;

        [SiblingComponent]
        internal Collider2D[] _siblingColliders;
    }
}

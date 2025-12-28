// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Attributes.Components
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(PolygonCollider2D))]
    public sealed class RelationalComponentTesterComplex : MonoBehaviour
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

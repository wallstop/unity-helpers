namespace UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using Core.Attributes;
    using Core.Extension;
    using UnityEngine;

    [DisallowMultipleComponent]
    public sealed class MatchColliderToSprite : MonoBehaviour
    {
        [SerializeField]
        [SiblingComponent]
        private SpriteRenderer _spriteRenderer;
        
        [SerializeField]
        [SiblingComponent]
        private PolygonCollider2D _collider;

        private Sprite _lastHandled;

        private void Awake()
        {
            this.AssignSiblingComponents();
            OnValidate();
        }

        private void Update()
        {
            if (_lastHandled == _spriteRenderer.sprite)
            {
                return;
            }

            OnValidate();
        }

        // Visible for testing
        public void OnValidate()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
                if (_spriteRenderer == null)
                {
                    this.LogError("No SpriteRenderer detected - cannot match collider shape.");
                    return;
                }
            }

            if (_collider == null)
            {
                _collider = GetComponent<PolygonCollider2D>();
                if (_collider == null)
                {
                    this.LogError("No PolygonCollider2D detected - cannot match collider shape.");
                    return;
                }
            }

            _lastHandled = _spriteRenderer.sprite;
            _collider.points = Array.Empty<Vector2>();
            if (_lastHandled == null)
            {
                _collider.pathCount = 0;
                return;
            }

            int physicsShapes = _lastHandled.GetPhysicsShapeCount();
            _collider.pathCount = physicsShapes;
            List<Vector2> buffer = Buffers<Vector2>.List;
            for (int i = 0; i < physicsShapes; ++i)
            {
                buffer.Clear();
                _ = _lastHandled.GetPhysicsShape(i, buffer);
                _collider.SetPath(i, buffer);
            }
        }
    }
}
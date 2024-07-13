namespace UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    [DisallowMultipleComponent]
    public sealed class MatchColliderToSprite : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer _spriteRenderer;

        [SerializeField]
        private Image _image;

        [SerializeField]
        private PolygonCollider2D _collider;

        private Sprite _lastHandled;

        private void Awake()
        {
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
            Sprite sprite;
            if (_spriteRenderer != null || TryGetComponent(out _spriteRenderer))
            {
                sprite = _spriteRenderer.sprite;
            }
            else if (_image != null || TryGetComponent(out _image))
            {
                sprite = _image.sprite;
            }
            else
            {
                sprite = null;
            }

            if (_collider == null || !TryGetComponent(out _collider))
            {
                return;
            }

            _lastHandled = sprite;
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
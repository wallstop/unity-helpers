﻿namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Serialization;
    using UnityEngine.UI;

    [DisallowMultipleComponent]
    public sealed class MatchColliderToSprite : MonoBehaviour
    {
        public Func<Sprite> spriteOverrideProducer;

        [FormerlySerializedAs("_spriteRenderer")]
        public SpriteRenderer spriteRenderer;

        [FormerlySerializedAs("_image")]
        public Image image;

        [FormerlySerializedAs("_collider")]
        public PolygonCollider2D polygonCollider;

        private Sprite _lastHandled;

        private void Awake()
        {
            OnValidate();
        }

        private void Update()
        {
            if (spriteOverrideProducer != null && _lastHandled == spriteOverrideProducer())
            {
                return;
            }

            if (spriteRenderer != null && _lastHandled == spriteRenderer.sprite)
            {
                return;
            }

            if (image != null && _lastHandled == image.sprite)
            {
                return;
            }

            OnValidate();
        }

        public void OnValidate()
        {
            if (polygonCollider == null && !TryGetComponent(out polygonCollider))
            {
                return;
            }

            Sprite sprite;
            if (spriteOverrideProducer != null)
            {
                sprite = spriteOverrideProducer();
            }
            else if (spriteRenderer != null || TryGetComponent(out spriteRenderer))
            {
                sprite = spriteRenderer.sprite;
            }
            else if (image != null || TryGetComponent(out image))
            {
                sprite = image.sprite;
            }
            else
            {
                sprite = null;
            }

            _lastHandled = sprite;
            polygonCollider.points = Array.Empty<Vector2>();
            if (_lastHandled == null)
            {
                polygonCollider.pathCount = 0;
                return;
            }

            int physicsShapes = _lastHandled.GetPhysicsShapeCount();
            polygonCollider.pathCount = physicsShapes;
            List<Vector2> buffer = Buffers<Vector2>.List;
            for (int i = 0; i < physicsShapes; ++i)
            {
                buffer.Clear();
                _ = _lastHandled.GetPhysicsShape(i, buffer);
                polygonCollider.SetPath(i, buffer);
            }
        }
    }
}

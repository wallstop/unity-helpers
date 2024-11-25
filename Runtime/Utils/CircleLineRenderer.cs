﻿namespace UnityHelpers.Utils
{
    using Core.Attributes;
    using Core.Extension;
    using Core.Helper;
    using Core.Random;
    using UnityEngine;

    [RequireComponent(typeof(LineRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    [DisallowMultipleComponent]
    public sealed class CircleLineRenderer : MonoBehaviour
    {
        public float minLineWidth = 0.005f;
        public float maxLineWidth = 0.02f;
        public int numSegments = 4;
        public int baseSegments = 4;
        public float updateRateSeconds = 0.1f;
        public Color color = Color.grey;

        public Vector3 Offset
        {
            get => _offset;
            set => transform.localPosition = _offset = value;
        }

        [SiblingComponent]
        private CircleCollider2D _collider;

        [SiblingComponent]
        private LineRenderer[] _lineRenderers;

        private Vector3 _offset;

        private Coroutine _update;

        private void Awake()
        {
            this.AssignSiblingComponents();
        }

        private void OnEnable()
        {
            if (_update != null)
            {
                StopCoroutine(_update);
            }
            _update = this.StartFunctionAsCoroutine(Render, updateRateSeconds);
        }

        private void OnDisable()
        {
            if (_update != null)
            {
                StopCoroutine(_update);
                _update = null;
            }
        }

        private void OnValidate()
        {
            if (numSegments <= 2)
            {
                this.LogWarn("Invalid number of segments {0}.", numSegments);
            }

            if (updateRateSeconds <= 0)
            {
                this.LogWarn("Invalid update rate {0}.", updateRateSeconds);
            }

            if (maxLineWidth < minLineWidth)
            {
                this.LogWarn(
                    "MaxLineWidth {0} smaller than MinLineWidth {1}.",
                    maxLineWidth,
                    minLineWidth
                );
            }
        }

        private void Update()
        {
            foreach (LineRenderer lineRenderer in _lineRenderers)
            {
                lineRenderer.enabled = _collider.enabled;
            }
        }

        private void Render()
        {
            foreach (LineRenderer lineRenderer in _lineRenderers)
            {
                if (!lineRenderer.enabled)
                {
                    lineRenderer.positionCount = 0;
                    return;
                }

                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
                lineRenderer.loop = true;
                lineRenderer.positionCount = numSegments;

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                float lineWidth =
                    minLineWidth == maxLineWidth
                        ? minLineWidth
                        : PRNG.Instance.NextFloat(minLineWidth, maxLineWidth);

                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;
                lineRenderer.useWorldSpace = false; // All below positions are local space
                float distanceMultiplier = _collider.radius;

                float angle = 360f / numSegments;
                float offsetRadians = PRNG.Instance.NextFloat(angle);
                float currentOffset = offsetRadians;
                Vector3[] positions = new Vector3[numSegments];
                for (int i = 0; i < numSegments; ++i)
                {
                    positions[i] =
                        new Vector3(
                            Mathf.Cos(Mathf.Deg2Rad * currentOffset),
                            Mathf.Sin(Mathf.Deg2Rad * currentOffset)
                        ) * distanceMultiplier;
                    currentOffset += angle % 360f;
                }

                lineRenderer.SetPositions(positions);
            }
        }
    }
}

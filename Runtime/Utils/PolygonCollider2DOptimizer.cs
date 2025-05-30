namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using Core.Attributes;
    using Core.Helper;
    using UnityEngine;

    /// <summary>
    /// Polygon collider optimizer. Removes points from the collider polygon with
    /// the given reduction Tolerance
    /// </summary>
    [AddComponentMenu("2D Collider Optimization/ Polygon Collider Optimizer")]
    [RequireComponent(typeof(PolygonCollider2D))]
    public sealed class PolygonCollider2DOptimizer : MonoBehaviour
    {
        [Serializable]
        private sealed class Path
        {
            public List<Vector2> points = new();

            public Path() { }

            public Path(IEnumerable<Vector2> points)
            {
                this.points.AddRange(points);
            }
        }

        public double tolerance;

        [SiblingComponent]
        private PolygonCollider2D _collider;

        [SerializeField]
        private List<Path> _originalPaths = new();

        public void Refresh()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            if (_collider == null)
            {
                this.AssignRelationalComponents();
            }

            /*
                When first getting a reference to the collider save the paths
                so that the optimization is re-doable (by performing it on the original path
                every time)
             */
            if (_originalPaths.Count == 0)
            {
                for (int i = 0; i < _collider.pathCount; ++i)
                {
                    List<Vector2> points = new(_collider.GetPath(i));
                    Path path = new(points);
                    _originalPaths.Add(path);
                }
            }

            //Reset the original paths
            if (tolerance <= 0)
            {
                for (int i = 0; i < _originalPaths.Count; ++i)
                {
                    _collider.SetPath(i, _originalPaths[i].points.ToArray());
                }
                return;
            }

            for (int i = 0; i < _originalPaths.Count; ++i)
            {
                List<Vector2> path = _originalPaths[i].points;
                List<Vector2> updatedPath = LineHelper.SimplifyPrecise(path, tolerance);
                _collider.SetPath(i, updatedPath.ToArray());
            }
        }
    }
}

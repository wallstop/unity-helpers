namespace Core.Math
{
    using Extension;
    using UnityEngine;

    // https://pastebin.com/iQDhQTFN
    public readonly struct Line
    {
        public readonly Vector2 from;
        public readonly Vector2 to;

        private readonly float _a;
        private readonly float _b;
        private readonly float _c;

        public Line(Vector2 from, Vector2 to)
        {
            this.from = from;
            this.to = to;
            _a = to.y - from.y;
            _b = from.x - to.x;
            _c = _a * from.x + _b * from.y;
        }

        public bool Intersects(Line other)
        {
            return UnityExtensions.Intersects(from, to, other.from, other.to);
        }

        public bool TryGetIntersectionPoint(Line other, out Vector2 intersection)
        {
            if (!Intersects(other))
            {
                intersection = default;
                return false;
            }

            float determinant = _a * other._b - other._a * _b;
            if (Mathf.Approximately(determinant, 0))
            {
                intersection = default;
                return false;
            }

            float x = (other._b * _c - _b * other._c) / determinant;
            float y = (_a * other._c - other._a * _c) / determinant;
            intersection = new Vector2(x, y);
            return true;
        }
    }
}

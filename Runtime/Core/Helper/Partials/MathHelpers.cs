namespace UnityHelpers.Core.Helper
{
    using UnityEngine;

    public static partial class Helpers
    {
        public static bool IsLeft(Vector2 a, Vector2 b, Vector2 point)
        {
            // http://alienryderflex.com/point_left_of_ray/

            //check which side of line AB the point P is on
            if ((b.x - a.x) * (point.y - a.y) - (point.x - a.x) * (b.y - a.y) > 0)
            {
                return false;
            }

            return true;
        }

        public static Vector2 RadianToVector2(float radian)
        {
            return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
        }

        public static Vector2 DegreeToVector2(float degree)
        {
            return RadianToVector2(degree * Mathf.Deg2Rad);
        }
    }
}

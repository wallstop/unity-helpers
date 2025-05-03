namespace WallstopStudios.UnityHelpers.Core.Math
{
    using UnityEngine;

    public static class PointPolygonCheck
    {
        public static bool IsPointInsidePolygon(Vector2 point, Vector2[] polygon)
        {
            bool result = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (
                    polygon[i].y < point.y && polygon[j].y >= point.y
                    || polygon[j].y < point.y && polygon[i].y >= point.y
                )
                {
                    if (
                        polygon[i].x
                            + (point.y - polygon[i].y)
                                / (polygon[j].y - polygon[i].y)
                                * (polygon[j].x - polygon[i].x)
                        < point.x
                    )
                    {
                        result = !result;
                    }
                }

                j = i;
            }

            return result;
        }
    }
}

namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System.Collections.Generic;
    using DataStructure;
    using DataStructure.Adapters;
    using UnityEngine;

    public static class CircleExtensions
    {
        public static IEnumerable<FastVector3Int> EnumerateArea(this Circle circle, int z = 0)
        {
            for (int x = (int)-circle.radius; x < circle.radius; ++x)
            {
                for (int y = (int)-circle.radius; y < circle.radius; ++y)
                {
                    Vector2 point = new(x, y);
                    if (circle.Contains(point))
                    {
                        yield return new FastVector3Int(x, y, z);
                    }
                }
            }
        }

        public static List<FastVector3Int> EnumerateArea(
            this Circle circle,
            List<FastVector3Int> buffer,
            int z = 0
        )
        {
            buffer.Clear();
            for (int x = (int)-circle.radius; x < circle.radius; ++x)
            {
                for (int y = (int)-circle.radius; y < circle.radius; ++y)
                {
                    Vector2 point = new(x, y);
                    if (circle.Contains(point))
                    {
                        FastVector3Int fastPoint = new FastVector3Int(x, y, z);
                        buffer.Add(fastPoint);
                    }
                }
            }

            return buffer;
        }
    }
}

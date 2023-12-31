namespace UnityHelpers.Core.Extension
{
    using Gameplay.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Helper;
    using UnityEngine;

    public static class DirectionExtensions
    {
        private static readonly List<Direction> Directions = Enum.GetValues(typeof(Direction)).OfType<Direction>().Except(Enumerables.Of(Direction.None)).ToList();

        public static Direction Opposite(this Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return Direction.South;
                case Direction.NorthEast:
                    return Direction.SouthWest;
                case Direction.East:
                    return Direction.West;
                case Direction.SouthEast:
                    return Direction.NorthWest;
                case Direction.South:
                    return Direction.North;
                case Direction.SouthWest:
                    return Direction.NorthEast;
                case Direction.West:
                    return Direction.East;
                case Direction.NorthWest:
                    return Direction.SouthEast;
                case Direction.None:
                    return Direction.None;
                default:
                    throw new ArgumentException($"Unknown direction {direction}.");
            }
        }

        public static Vector2Int AsVector2Int(this Direction direction)
        {
            switch (direction)
            {
                case Direction.None:
                    return Vector2Int.zero;
                case Direction.North:
                    return Vector2Int.up;
                case Direction.NorthEast:
                    return new Vector2Int(1, 1);
                case Direction.East:
                    return Vector2Int.right;
                case Direction.SouthEast:
                    return new Vector2Int(1, -1);
                case Direction.South:
                    return Vector2Int.down;
                case Direction.SouthWest:
                    return new Vector2Int(-1, -1);
                case Direction.West:
                    return Vector2Int.left;
                case Direction.NorthWest:
                    return new Vector2Int(-1, 1);
                default:
                    throw new ArgumentException($"Unknown direction {direction}.");
            }
        }

        public static Vector2 AsVector2(this Direction direction)
        {
            return direction.AsVector2Int();
        }

        public static Direction AsDirection(this Vector3 vector3)
        {
            return AsDirection((Vector2)vector3);
        }

        public static IEnumerable<Direction> Split(this Direction direction)
        {
            bool foundAny = false;
            foreach (Direction singleDirection in Directions)
            {
                if (direction.HasFlag(singleDirection))
                {
                    foundAny = true;
                    yield return singleDirection;
                }
            }

            if (!foundAny)
            {
                yield return Direction.None;
            }
        }

        public static Direction Combine(this IEnumerable<Direction> directions)
        {
            Direction combined = Direction.None;
            foreach (Direction direction in directions)
            {
                combined |= direction;
            }

            return combined;
        }

        public static Direction AsDirection(this Vector2 vector, bool preferAngles = false)
        {
            if (vector.x == 0 && vector.y == 0)
            {
                return Direction.None;
            }

            float angle;
            if (vector.x < 0)
            {
                angle = 360 - Mathf.Atan2(vector.x, vector.y) * Mathf.Rad2Deg * -1;
            }
            else
            {
                angle = Mathf.Atan2(vector.x, vector.y) * Mathf.Rad2Deg;
            }

            if (preferAngles)
            {
                if (345 <= angle || angle < 15)
                {
                    return Direction.North;
                }

                if (15 <= angle && angle < 75)
                {
                    return Direction.NorthEast;
                }

                if (75 <= angle && angle < 105)
                {
                    return Direction.East;
                }

                if (105 <= angle && angle < 165)
                {
                    return Direction.SouthEast;
                }

                if (165 <= angle && angle < 195)
                {
                    return Direction.South;
                }

                if (195 <= angle && angle < 255)
                {
                    return Direction.SouthWest;
                }

                if (255 <= angle && angle < 285)
                {
                    return Direction.West;
                }

                if (285 <= angle && angle < 345)
                {
                    return Direction.NorthWest;
                }
            }

            if (337.5 <= angle || angle < 22.5)
            {
                return Direction.North;
            }

            if (22.5 <= angle && angle < 67.5)
            {
                return Direction.NorthEast;
            }

            if (67.5 <= angle && angle < 112.5)
            {
                return Direction.East;
            }

            if (112.5 <= angle && angle < 157.5)
            {
                return Direction.SouthEast;
            }

            if (157.5 <= angle && angle < 202.5)
            {
                return Direction.South;
            }

            if (202.5 <= angle && angle < 247.5)
            {
                return Direction.SouthWest;
            }

            if (247.5 <= angle && angle < 292.5)
            {
                return Direction.West;
            }

            if (292.5 <= angle && angle < 337.5)
            {
                return Direction.NorthWest;
            }

            return Direction.None;
        }
    }
}

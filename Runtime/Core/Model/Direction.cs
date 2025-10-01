namespace WallstopStudios.UnityHelpers.Core.Model
{
    using System;

    [Flags]
    [Serializable]
    public enum Direction : short
    {
        None = 0,
        North = 1 << 0,
        NorthEast = 1 << 1,
        East = 1 << 2,
        SouthEast = 1 << 3,
        South = 1 << 4,
        SouthWest = 1 << 5,
        West = 1 << 6,
        NorthWest = 1 << 7,
    }

    public static class DirectionConstants
    {
        public const int NumDirections = 4;
        public const int AllDirections = 8;
    }
}

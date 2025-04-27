namespace WallstopStudios.UnityHelpers.Core.Model
{
    using System;
    using System.Runtime.Serialization;

    [Flags]
    [Serializable]
    public enum Direction : short
    {
        [EnumMember]
        None = 0,

        [EnumMember]
        North = 1,

        [EnumMember]
        NorthEast = 2,

        [EnumMember]
        East = 4,

        [EnumMember]
        SouthEast = 8,

        [EnumMember]
        South = 16,

        [EnumMember]
        SouthWest = 32,

        [EnumMember]
        West = 64,

        [EnumMember]
        NorthWest = 128,
    }

    public static class DirectionConstants
    {
        public const int NumDirections = 4;
        public const int AllDirections = 8;
    }
}

namespace WallstopStudios.UnityHelpers.Utils
{
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    public static class Buffers
    {
        public const int BufferSize = 10_000;

        public static readonly Collider2D[] Colliders = new Collider2D[BufferSize];
        public static readonly RaycastHit2D[] RaycastHits = new RaycastHit2D[BufferSize];

        /*
            Note: Only use with CONSTANT time values, otherwise this is a memory leak.
            DO NOT USE with random values.
         */
        public static readonly Dictionary<float, WaitForSeconds> WaitForSeconds = new();
        public static readonly Dictionary<float, WaitForSecondsRealtime> WaitForSecondsRealtime =
            new();
        public static readonly System.Random Random = new();
        public static readonly WaitForFixedUpdate WaitForFixedUpdate = new();
        public static readonly WaitForEndOfFrame WaitForEndOfFrame = new();

        public static readonly StringBuilder StringBuilder = new();
    }

    public static class Buffers<T>
    {
        public static readonly T[] Array = new T[Buffers.BufferSize];
        public static readonly List<T> List = new();
        public static readonly HashSet<T> HashSet = new();
        public static readonly Queue<T> Queue = new();
        public static readonly Stack<T> Stack = new();
        public static readonly LinkedList<T> LinkedList = new();
    }
}

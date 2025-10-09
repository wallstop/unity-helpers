namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System.Collections.Generic;

    /// <summary>
    /// Helpers for iterating over multidimensional arrays with tuples or buffered lists.
    /// </summary>
    public static class IterationHelpers
    {
        /// <summary>
        /// Enumerates all (i, j) indices of a 2D array.
        /// </summary>
        public static IEnumerable<(int, int)> IndexOver<T>(this T[,] array)
        {
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    yield return (i, j);
                }
            }
        }

        /// <summary>
        /// Fills a buffer with all (i, j) indices of a 2D array and returns the same buffer.
        /// </summary>
        public static List<(int, int)> IndexOver<T>(this T[,] array, List<(int, int)> buffer)
        {
            buffer.Clear();
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    (int i, int j) tuple = (i, j);
                    buffer.Add(tuple);
                }
            }

            return buffer;
        }

        /// <summary>
        /// Enumerates all (i, j, k) indices of a 3D array.
        /// </summary>
        public static IEnumerable<(int, int, int)> IndexOver<T>(this T[,,] array)
        {
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    for (int k = 0; k < array.GetLength(2); k++)
                    {
                        yield return (i, j, k);
                    }
                }
            }
        }

        /// <summary>
        /// Fills a buffer with all (i, j, k) indices of a 3D array and returns the same buffer.
        /// </summary>
        public static List<(int, int, int)> IndexOver<T>(
            this T[,,] array,
            List<(int, int, int)> buffer
        )
        {
            buffer.Clear();
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    for (int k = 0; k < array.GetLength(2); k++)
                    {
                        (int i, int j, int k) tuple = (i, j, k);
                        buffer.Add(tuple);
                    }
                }
            }

            return buffer;
        }
    }
}

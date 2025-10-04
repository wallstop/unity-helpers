namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System.Collections.Generic;

    public static class IterationHelpers
    {
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

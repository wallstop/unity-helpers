namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;

    public static class IReadonlyListExtensions
    {
        public static int IndexOf<T>(this IReadOnlyList<T> readonlyList, T element)
        {
            switch (readonlyList)
            {
                case T[] array:
                {
                    return Array.IndexOf(array, element);
                }
                case List<T> list:
                {
                    return list.IndexOf(element);
                }
                default:
                {
                    // TODO: Configurable?
                    EqualityComparer<T> comparer = EqualityComparer<T>.Default;
                    for (int i = 0; i < readonlyList.Count; i++)
                    {
                        if (comparer.Equals(readonlyList[i], element))
                        {
                            return i;
                        }
                    }

                    return -1;
                }
            }
        }
    }
}

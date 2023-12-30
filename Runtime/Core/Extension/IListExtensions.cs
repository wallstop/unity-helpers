namespace Core.Extension
{
    using Random;
    using System.Collections.Generic;

    public static class IListExtensions
    {
        public static void Shuffle<T>(this IList<T> list, IRandom random = null)
        {
            if (list is not { Count: > 1 })
            {
                return;
            }

            random ??= PcgRandom.Instance;

            int length = list.Count;
            for (int i = 0; i < length - 1; ++i)
            {
                int nextIndex = random.Next(i, length);
                if (nextIndex == i)
                {
                    continue;
                }
                (list[i], list[nextIndex]) = (list[nextIndex], list[i]);
            }
        }

        public static void RemoveAtSwapBack<T>(this IList<T> list, int index)
        {
            if (list.Count <= 1)
            {
                list.Clear();
                return;
            }

            int lastIndex = list.Count - 1;
            if (index == lastIndex)
            {
                list.RemoveAt(index);
                return;
            }

            T last = list[lastIndex];
            list[index] = last;
            list.RemoveAt(lastIndex);
        }
    }
}

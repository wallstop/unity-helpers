namespace Core.Extension
{
    using Random;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public static class RandomExtensions
    {
        public static Vector2 NextVector2(this IRandom random, float amplitude)
        {
            return random.NextVector2(-amplitude, amplitude);
        }

        public static Vector2 NextVector2(this IRandom random, float minAmplitude, float maxAmplitude)
        {
            float x = random.NextFloat(minAmplitude, maxAmplitude);
            float y = random.NextFloat(minAmplitude, maxAmplitude);
            return new Vector2(x, y);
        }

        public static Vector3 NextVector3(this IRandom random, float amplitude)
        {
            return random.NextVector3(-amplitude, amplitude);
        }

        public static Vector3 NextVector3(this IRandom random, float minAmplitude, float maxAmplitude)
        {
            float z = random.NextFloat(minAmplitude, maxAmplitude);
            Vector3 result = random.NextVector2(minAmplitude, maxAmplitude);
            result.z = z;
            return result;
        }

        public static T NextEnum<T>(this IRandom random) where T : struct
        {
            T[] enumValues = (T[])Enum.GetValues(typeof(T));
            if (enumValues.Length == 0)
            {
                return default(T);
            }

            if (enumValues.Length == 1)
            {
                return enumValues[0];
            }

            if (enumValues.Length == 2)
            {
                return random.NextBool() ? enumValues[0] : enumValues[1];
            }

            int nextIndex = random.Next(0, enumValues.Length);
            return enumValues[nextIndex];
        }

        public static T Next<T>(this IRandom random, IList<T> elements)
        {
            if (ReferenceEquals(elements, null) || elements.Count == 0)
            {
                return default(T);
            }

            switch (elements.Count)
            {
                case 1:
                    return elements[0];
                case 2:
                    return random.NextBool() ? elements[0] : elements[1];
                default:
                    int index = random.Next(0, elements.Count);
                    return elements[index];
            }
        }

        public static T Next<T>(this IRandom random, IEnumerable<T> elements)
        {
            if (ReferenceEquals(elements, null))
            {
                return default(T);
            }

            IList<T> elementsList = elements as IList<T>;
            if (!ReferenceEquals(elementsList, null))
            {
                return Next(random, elementsList);
            }

            ICollection<T> maybeCollection = elements as ICollection<T>;
            if (!ReferenceEquals(maybeCollection, null))
            {
                int count = maybeCollection.Count;
                int randomIndex = random.Next(0, count);

                int i = 0;
                foreach (T element in maybeCollection)
                {
                    if (i++ == randomIndex)
                    {
                        return element;
                    }
                }
            }

            elementsList = elements.ToArray();
            return Next(random, elementsList);
        }
    }
}

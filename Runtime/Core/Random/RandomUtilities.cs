namespace WallstopStudios.UnityHelpers.Core.Random
{
    using UnityEngine;

    public static class RandomUtilities
    {
        public static float GetRandomVariance(this IRandom random, float baseValue, float variance)
        {
            if (variance < 0.0f)
            {
                Debug.LogError("Variance cannot be negative");
                return baseValue;
            }

            if (variance == 0.0f)
            {
                return baseValue;
            }

            float higher = variance / 2;
            float lower = -higher;

            return baseValue + random.NextFloat(lower, higher);
        }
    }
}

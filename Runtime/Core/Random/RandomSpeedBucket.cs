// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Random
{
    /// <summary>
    /// Relative speed bucket for RNG performance comparisons.
    /// </summary>
    public enum RandomSpeedBucket
    {
        Unknown = 0,
        VerySlow,
        Slow,
        Moderate,
        Fast,
        VeryFast,
        Fastest,
    }

    public static class RandomSpeedBucketExtensions
    {
        public static string ToLabel(this RandomSpeedBucket bucket)
        {
            return bucket switch
            {
                RandomSpeedBucket.Fastest => "Fastest",
                RandomSpeedBucket.VeryFast => "Very Fast",
                RandomSpeedBucket.Fast => "Fast",
                RandomSpeedBucket.Moderate => "Moderate",
                RandomSpeedBucket.Slow => "Slow",
                RandomSpeedBucket.VerySlow => "Very Slow",
                _ => "Unknown",
            };
        }

        public static RandomSpeedBucket FromRatio(double ratio)
        {
            if (double.IsNaN(ratio) || ratio <= 0)
            {
                return RandomSpeedBucket.Unknown;
            }

            if (ratio >= 0.95d)
            {
                return RandomSpeedBucket.Fastest;
            }

            if (ratio >= 0.75d)
            {
                return RandomSpeedBucket.VeryFast;
            }

            if (ratio >= 0.55d)
            {
                return RandomSpeedBucket.Fast;
            }

            if (ratio >= 0.35d)
            {
                return RandomSpeedBucket.Moderate;
            }

            if (ratio >= 0.2d)
            {
                return RandomSpeedBucket.Slow;
            }

            return RandomSpeedBucket.VerySlow;
        }
    }
}

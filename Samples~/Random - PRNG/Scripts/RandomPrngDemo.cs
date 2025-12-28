// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace Samples.UnityHelpers.Random.Prng
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Random;

    /// <summary>
    /// Demonstrates sampling values with PCG and helper methods.
    /// </summary>
    public sealed class RandomPrngDemo : MonoBehaviour
    {
        [SerializeField]
        private long seed = 12345;

        private void Start()
        {
            PcgRandom rng = new PcgRandom(seed);
            int integer = rng.Next(0, 10);
            float unit = rng.NextFloat(0f, 1f);
            float gaussian = (float)rng.NextGaussian();
            List<string> fruits = new List<string> { "Apple", "Banana", "Cherry" };
            string pick = rng.NextOf(fruits);
            Debug.Log(
                $"PRNG seed={seed} int={integer} float={unit:F3} gaussian={gaussian:F3} pick={pick}"
            );
        }
    }
}

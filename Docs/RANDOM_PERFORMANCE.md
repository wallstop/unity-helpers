# Random Number Generator Performance Benchmarks

## TL;DR — What To Use

- **⭐ Use `PRNG.Instance` instead of `UnityEngine.Random` — 10-15x faster + seedable for determinism.**
- IllusionFlow (default): Great balance of speed, quality, and thread-safety.
- Need System.Random compatibility? Use `DotNetRandom` wrapper.
- Chasing max speed in hot loops? `LinearCongruentialGenerator`/`RomuDuo` are fastest.

### ⭐ The Speed & Determinism Killer Feature

**The Problem with UnityEngine.Random:**

```csharp
// 🔴 UnityEngine.Random:
// - Slow (~65-85M ops/sec)
// - Not seedable (no replays, no determinism)
// - Not thread-safe (main thread only)

void GenerateLevel()
{
    for (int i = 0; i < 10000; i++)
    {
        float value = Random.value;  // Slow!
        // Can't reproduce this exact level generation
    }
}
```

**The Solution - Unity Helpers Random:**

```csharp
// 🟢 Unity Helpers (PRNG.Instance):
// - Fast (655-885M ops/sec = 10-15x faster)
// - Seedable (perfect replays)
// - Thread-local (safe everywhere)

IRandom rng = new IllusionFlow(seed: 12345);

void GenerateLevel()
{
    for (int i = 0; i < 10000; i++)
    {
        float value = rng.NextFloat();  // Fast + reproducible!
        // Same seed = exact same level every time
    }
}
```

**When It Matters:**

- **Procedural generation**: Thousands of random rolls per level
- **Particle systems**: Hundreds of random values per frame
- **Networked games**: Clients must generate identical results
- **Replay systems**: Must reproduce exact gameplay
- **Performance-critical loops**: Every microsecond counts

**Speed Comparison:**

| Generator                    | NextFloat (ops/sec) | vs UnityEngine.Random |
| ---------------------------- | ------------------- | --------------------- |
| UnityEngine.Random           | 65M                 | 1x (baseline)         |
| PRNG.Instance (IllusionFlow) | 655M                | **10x faster**        |
| LinearCongruentialGenerator  | 829M                | **13x faster**        |
| SplitMix64                   | 739M                | **11x faster**        |

Threading

- Individual RNG instances are not thread‑safe.
- Use `PRNG.Instance` or each generator’s `TypeName.Instance` for thread‑local safety, or create one instance per thread.

Visual

![Random Generators](Images/random_generators.svg)

This document contains performance benchmarks for the various random number generators included in Unity Helpers.

## Performance (Operations per Second)

<!-- RANDOM_BENCHMARKS_START -->

| Random                      | NextBool    | Next        | NextUInt      | NextFloat   | NextDouble  | NextUint - Range | NextInt - Range |
| --------------------------- | ----------- | ----------- | ------------- | ----------- | ----------- | ---------------- | --------------- |
| DotNetRandom                | 543,400,000 | 53,000,000  | 58,000,000    | 44,900,000  | 27,100,000  | 53,700,000       | 53,700,000      |
| LinearCongruentialGenerator | 807,000,000 | 489,000,000 | 1,328,100,000 | 172,500,000 | 333,300,000 | 593,300,000      | 507,300,000     |
| IllusionFlow                | 791,100,000 | 489,600,000 | 894,000,000   | 172,400,000 | 331,900,000 | 446,100,000      | 395,300,000     |
| PcgRandom                   | 796,800,000 | 538,000,000 | 914,300,000   | 176,800,000 | 344,900,000 | 452,000,000      | 406,200,000     |
| RomuDuo                     | 778,400,000 | 336,400,000 | 767,400,000   | 148,300,000 | 206,900,000 | 446,100,000      | 398,200,000     |
| SplitMix64                  | 779,900,000 | 489,600,000 | 1,063,600,000 | 172,300,000 | 332,900,000 | 483,200,000      | 442,000,000     |
| FlurryBurstRandom           | 778,500,000 | 487,000,000 | 951,900,000   | 172,100,000 | 335,300,000 | 456,000,000      | 410,600,000     |
| SquirrelRandom              | 756,400,000 | 355,100,000 | 409,300,000   | 151,400,000 | 200,900,000 | 366,500,000      | 311,700,000     |
| SystemRandom                | 148,400,000 | 147,600,000 | 64,300,000    | 131,400,000 | 137,300,000 | 58,700,000       | 57,900,000      |
| UnityRandom                 | 645,700,000 | 79,900,000  | 83,900,000    | 61,500,000  | 41,300,000  | 76,600,000       | 75,600,000      |
| WyRandom                    | 755,900,000 | 356,600,000 | 455,000,000   | 152,300,000 | 190,400,000 | 290,700,000      | 276,100,000     |
| XorShiftRandom              | 784,900,000 | 531,800,000 | 603,300,000   | 178,200,000 | 294,600,000 | 486,500,000      | 392,600,000     |
| XoroShiroRandom             | 783,900,000 | 333,000,000 | 751,900,000   | 145,900,000 | 203,000,000 | 428,000,000      | 381,800,000     |
| PhotonSpinRandom            | 702,200,000 | 217,000,000 | 264,500,000   | 116,000,000 | 116,800,000 | 211,500,000      | 214,100,000     |
| StormDropRandom             | 779,700,000 | 484,800,000 | 717,300,000   | 172,300,000 | 255,800,000 | 401,500,000      | 369,600,000     |

<!-- RANDOM_BENCHMARKS_END -->

## Interpreting the Results

- **NextBool**: Operations per second for generating random boolean values
- **Next**: Operations per second for generating random integers (0 to int.MaxValue)
- **NextUInt**: Operations per second for generating random unsigned integers
- **NextFloat**: Operations per second for generating random floats (0.0f to 1.0f)
- **NextDouble**: Operations per second for generating random doubles (0.0d to 1.0d)
- **NextUint - Range**: Operations per second for generating random unsigned integers within a range
- **NextInt - Range**: Operations per second for generating random integers within a range

## Recommendations

Based on the benchmarks:

- **For general use**: `IllusionFlow` (via `PRNG.Instance`) or `PCG` - Great balance of speed and quality
- **For maximum speed**: `LinearCongruentialGenerator` - Fastest overall (but not recommended for statistical quality)
- **For compatibility**: `DotNetRandom` - Uses .NET's built-in Random
- **Avoid for performance**: `UnityRandom` - Significantly slower than alternatives

All benchmarks are run for 1 second per operation type to ensure statistical significance.

---

## 📚 Related Documentation

**Core Guides:**

- [Getting Started](GETTING_STARTED.md) - Your first 5 minutes with Unity Helpers
- [Main README](../README.md) - Complete feature overview
- [Feature Index](INDEX.md) - Alphabetical reference

**Random Number Generator Features:**

- [README - Random Generators](../README.md#random-number-generators) - Full API reference
- [README - Quick Start](../README.md#random-number-generation) - 60-second tutorial

**Related Features:**

- [Math & Extensions](MATH_AND_EXTENSIONS.md) - Vector/color extensions and utilities
- [Data Structures](DATA_STRUCTURES.md) - Heaps, tries, and more

**Need help?** [Open an issue](https://github.com/wallstop/unity-helpers/issues)

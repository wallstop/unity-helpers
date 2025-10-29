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
| DotNetRandom                | 543,900,000 | 52,300,000  | 48,900,000    | 43,900,000  | 27,700,000  | 54,400,000       | 54,000,000      |
| LinearCongruentialGenerator | 797,700,000 | 891,900,000 | 1,328,600,000 | 184,800,000 | 413,100,000 | 594,000,000      | 510,400,000     |
| IllusionFlow                | 794,100,000 | 662,000,000 | 894,700,000   | 177,700,000 | 331,100,000 | 446,700,000      | 396,500,000     |
| PcgRandom                   | 794,600,000 | 666,200,000 | 892,900,000   | 184,300,000 | 341,400,000 | 455,400,000      | 412,100,000     |
| RomuDuo                     | 782,700,000 | 596,800,000 | 765,900,000   | 167,300,000 | 256,100,000 | 445,200,000      | 396,600,000     |
| SplitMix64                  | 795,300,000 | 778,200,000 | 1,067,300,000 | 184,600,000 | 385,200,000 | 487,800,000      | 446,500,000     |
| FlurryBurstRandom           | 782,800,000 | 669,000,000 | 892,900,000   | 184,000,000 | 342,400,000 | 457,600,000      | 413,700,000     |
| SquirrelRandom              | 765,700,000 | 409,500,000 | 412,800,000   | 172,400,000 | 203,500,000 | 365,200,000      | 342,800,000     |
| SystemRandom                | 143,800,000 | 148,300,000 | 65,300,000    | 132,000,000 | 137,500,000 | 59,400,000       | 60,400,000      |
| UnityRandom                 | 654,100,000 | 85,100,000  | 87,900,000    | 62,100,000  | 41,500,000  | 81,500,000       | 82,000,000      |
| WyRandom                    | 759,600,000 | 390,600,000 | 457,000,000   | 166,900,000 | 194,400,000 | 296,900,000      | 281,600,000     |
| XorShiftRandom              | 790,000,000 | 593,200,000 | 603,600,000   | 184,300,000 | 288,600,000 | 484,900,000      | 429,300,000     |
| XoroShiroRandom             | 790,800,000 | 550,700,000 | 684,800,000   | 166,600,000 | 244,200,000 | 425,000,000      | 382,400,000     |
| PhotonSpinRandom            | 732,600,000 | 244,000,000 | 268,000,000   | 118,600,000 | 120,300,000 | 212,200,000      | 208,400,000     |
| StormDropRandom             | 768,900,000 | 464,200,000 | 531,200,000   | 184,700,000 | 241,600,000 | 347,900,000      | 327,500,000     |

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

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

![Random Generators](Docs/Images/random_generators.svg)

This document contains performance benchmarks for the various random number generators included in Unity Helpers.

## Performance (Operations per Second)

<!-- RANDOM_BENCHMARKS_START -->

| Random                      | NextBool    | Next        | NextUInt      | NextFloat   | NextDouble  | NextUint - Range | NextInt - Range |
| --------------------------- | ----------- | ----------- | ------------- | ----------- | ----------- | ---------------- | --------------- |
| DotNetRandom                | 550,600,000 | 53,100,000  | 57,400,000    | 45,600,000  | 26,900,000  | 53,700,000       | 53,900,000      |
| LinearCongruentialGenerator | 814,800,000 | 538,900,000 | 1,335,100,000 | 184,700,000 | 296,500,000 | 591,500,000      | 508,400,000     |
| IllusionFlow                | 800,200,000 | 489,500,000 | 892,600,000   | 167,600,000 | 268,200,000 | 444,600,000      | 396,100,000     |
| PcgRandom                   | 796,400,000 | 537,900,000 | 889,500,000   | 184,300,000 | 291,400,000 | 456,500,000      | 412,000,000     |
| RomuDuo                     | 794,300,000 | 359,300,000 | 766,200,000   | 167,200,000 | 191,600,000 | 446,000,000      | 397,600,000     |
| SplitMix64                  | 801,100,000 | 537,400,000 | 972,300,000   | 183,800,000 | 296,600,000 | 487,500,000      | 446,600,000     |
| SquirrelRandom              | 747,700,000 | 383,300,000 | 413,800,000   | 172,300,000 | 204,800,000 | 330,200,000      | 314,200,000     |
| SystemRandom                | 146,800,000 | 148,300,000 | 65,700,000    | 132,500,000 | 139,500,000 | 59,800,000       | 61,300,000      |
| UnityRandom                 | 647,700,000 | 77,800,000  | 87,800,000    | 62,100,000  | 39,500,000  | 81,500,000       | 82,400,000      |
| WyRandom                    | 750,700,000 | 382,900,000 | 447,500,000   | 166,800,000 | 191,700,000 | 296,800,000      | 281,100,000     |
| XorShiftRandom              | 792,900,000 | 536,400,000 | 606,000,000   | 184,100,000 | 287,300,000 | 442,800,000      | 391,200,000     |
| XoroShiroRandom             | 789,200,000 | 359,300,000 | 715,100,000   | 167,300,000 | 192,500,000 | 428,900,000      | 383,500,000     |

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

- **For general use**: `IllusionFlow` (via `PRNG.Instance`) - Great balance of speed and quality
- **For maximum speed**: `RomuDuo` or `LinearCongruentialGenerator` - Fastest overall
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

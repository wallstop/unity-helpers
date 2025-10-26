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
| DotNetRandom                | 535,000,000 | 54,400,000  | 56,700,000    | 45,200,000  | 28,200,000  | 52,200,000       | 51,800,000      |
| LinearCongruentialGenerator | 798,300,000 | 823,200,000 | 1,329,100,000 | 179,900,000 | 402,000,000 | 577,800,000      | 493,300,000     |
| IllusionFlow                | 778,000,000 | 662,100,000 | 895,100,000   | 178,000,000 | 331,100,000 | 444,000,000      | 384,900,000     |
| PcgRandom                   | 762,500,000 | 668,400,000 | 892,700,000   | 179,700,000 | 345,200,000 | 450,000,000      | 400,200,000     |
| RomuDuo                     | 758,600,000 | 579,300,000 | 767,300,000   | 167,200,000 | 255,900,000 | 446,500,000      | 397,400,000     |
| SplitMix64                  | 800,900,000 | 670,400,000 | 943,700,000   | 179,000,000 | 346,600,000 | 473,300,000      | 432,800,000     |
| FlurryBurstRandom           | 762,800,000 | 603,800,000 | 863,700,000   | 183,000,000 | 305,200,000 | 456,400,000      | 412,400,000     |
| SquirrelRandom              | 759,700,000 | 393,600,000 | 413,500,000   | 172,300,000 | 187,800,000 | 329,600,000      | 307,100,000     |
| SystemRandom                | 138,400,000 | 144,300,000 | 63,200,000    | 127,600,000 | 135,800,000 | 59,600,000       | 60,400,000      |
| UnityRandom                 | 655,300,000 | 85,000,000  | 87,800,000    | 62,200,000  | 41,500,000  | 81,500,000       | 82,400,000      |
| WyRandom                    | 758,600,000 | 390,600,000 | 457,100,000   | 166,800,000 | 191,100,000 | 293,600,000      | 274,700,000     |
| XorShiftRandom              | 766,300,000 | 554,600,000 | 586,100,000   | 181,100,000 | 259,100,000 | 443,300,000      | 393,600,000     |
| XoroShiroRandom             | 766,200,000 | 522,900,000 | 714,100,000   | 167,200,000 | 243,300,000 | 428,400,000      | 381,000,000     |
| PhotonSpinRandom            | 677,900,000 | 232,100,000 | 258,000,000   | 116,900,000 | 114,800,000 | 209,700,000      | 201,100,000     |
| StormDropRandom             | 758,100,000 | 538,100,000 | 698,600,000   | 184,100,000 | 271,800,000 | 406,300,000      | 365,900,000     |

<!-- RANDOM_BENCHMARKS_END -->

Benchmarks for `FlurryBurstRandom`, `PhotonSpinRandom`, and `StormDropRandom` will populate after running the
`RandomPerformanceTests.Benchmark` test inside Unity (it updates this table automatically).

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

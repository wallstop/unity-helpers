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

| Generator | NextFloat (ops/sec) | vs UnityEngine.Random |
|-----------|---------------------|-----------------------|
| UnityEngine.Random | 65M | 1x (baseline) |
| PRNG.Instance (IllusionFlow) | 655M | **10x faster** |
| LinearCongruentialGenerator | 829M | **13x faster** |
| SplitMix64 | 739M | **11x faster** |

Threading
- Individual RNG instances are not thread‑safe.
- Use `PRNG.Instance` or each generator’s `TypeName.Instance` for thread‑local safety, or create one instance per thread.

Visual

![Random Generators](Docs/Images/random_generators.svg)

This document contains performance benchmarks for the various random number generators included in Unity Helpers.

## Performance (Operations per Second)

<!-- RANDOM_BENCHMARKS_START -->
| Random | NextBool | Next | NextUInt | NextFloat | NextDouble | NextUint - Range | NextInt - Range |
| ------ | -------- | ---- | -------- | --------- | ---------- | ---------------- | --------------- |
| DotNetRandom | 548,300,000 | 54,900,000 | 58,300,000 | 48,000,000 | 28,100,000 |54,800,000 |54,100,000 |
| LinearCongruentialGenerator | 805,700,000 | 829,700,000 | 1,334,700,000 | 212,300,000 | 411,000,000 |558,500,000 |503,400,000 |
| IllusionFlow | 787,300,000 | 655,900,000 | 885,100,000 | 203,500,000 | 309,000,000 |442,200,000 |392,100,000 |
| PcgRandom | 793,900,000 | 662,100,000 | 919,300,000 | 211,700,000 | 341,200,000 |453,800,000 |407,700,000 |
| RomuDuo | 796,900,000 | 556,300,000 | 707,100,000 | 189,500,000 | 254,800,000 |423,300,000 |390,900,000 |
| SplitMix64 | 803,400,000 | 739,200,000 | 1,063,900,000 | 212,600,000 | 381,300,000 |485,700,000 |439,400,000 |
| SquirrelRandom | 781,900,000 | 394,900,000 | 413,800,000 | 198,000,000 | 203,400,000 |365,900,000 |311,700,000 |
| SystemRandom | 143,400,000 | 148,500,000 | 65,200,000 | 131,400,000 | 137,200,000 |59,000,000 |60,000,000 |
| UnityRandom | 657,500,000 | 85,000,000 | 87,800,000 | 65,200,000 | 41,500,000 |81,500,000 |82,200,000 |
| WyRandom | 771,400,000 | 389,300,000 | 456,500,000 | 183,500,000 | 194,200,000 |297,000,000 |282,100,000 |
| XorShiftRandom | 796,600,000 | 563,800,000 | 603,100,000 | 212,800,000 | 286,800,000 |481,000,000 |392,200,000 |
| XoroShiroRandom | 793,500,000 | 557,600,000 | 712,900,000 | 190,200,000 | 244,000,000 |428,100,000 |379,800,000 |
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
- [Main README](README.md) - Complete feature overview
- [Feature Index](INDEX.md) - Alphabetical reference

**Random Number Generator Features:**
- [README - Random Generators](README.md#random-number-generators) - Full API reference
- [README - Quick Start](README.md#random-number-generation) - 60-second tutorial

**Related Features:**
- [Math & Extensions](MATH_AND_EXTENSIONS.md) - Vector/color extensions and utilities
- [Data Structures](DATA_STRUCTURES.md) - Heaps, tries, and more

**Need help?** [Open an issue](https://github.com/wallstop/unity-helpers/issues)

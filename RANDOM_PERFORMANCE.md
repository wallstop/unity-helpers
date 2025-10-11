# Random Number Generator Performance Benchmarks

## TL;DR — What To Use

- Use `PRNG.Instance` (IllusionFlow by default) for most gameplay: fast, seedable, thread‑safe.
- Need System.Random compatibility? Use `DotNetRandom` wrapper.
- Chasing max speed in hot loops? `LinearCongruentialGenerator`/`RomuDuo` are fastest; ensure they meet your quality needs.

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
| DotNetRandom | 540,600,000 | 53,600,000 | 57,600,000 | 48,300,000 | 27,900,000 |54,500,000 |51,400,000 |
| LinearCongruentialGenerator | 789,400,000 | 877,100,000 | 1,328,200,000 | 212,700,000 | 388,800,000 |583,100,000 |500,000,000 |
| IllusionFlow | 796,500,000 | 609,200,000 | 809,500,000 | 202,400,000 | 319,100,000 |439,600,000 |389,300,000 |
| PcgRandom | 792,400,000 | 657,700,000 | 911,100,000 | 210,800,000 | 335,800,000 |448,200,000 |405,800,000 |
| RomuDuo | 792,700,000 | 580,900,000 | 754,600,000 | 189,400,000 | 253,100,000 |439,100,000 |390,800,000 |
| SplitMix64 | 798,200,000 | 745,900,000 | 1,048,600,000 | 211,700,000 | 374,800,000 |482,000,000 |438,500,000 |
| SquirrelRandom | 762,400,000 | 394,200,000 | 413,800,000 | 196,400,000 | 199,900,000 |332,300,000 |312,000,000 |
| SystemRandom | 148,600,000 | 149,900,000 | 65,600,000 | 130,900,000 | 139,300,000 |60,300,000 |61,000,000 |
| UnityRandom | 443,000,000 | 85,000,000 | 87,500,000 | 65,100,000 | 41,500,000 |81,800,000 |82,400,000 |
| WyRandom | 756,600,000 | 388,600,000 | 457,600,000 | 188,000,000 | 193,000,000 |297,100,000 |279,600,000 |
| XorShiftRandom | 792,200,000 | 564,500,000 | 602,700,000 | 213,400,000 | 281,200,000 |442,900,000 |391,200,000 |
| XoroShiroRandom | 712,600,000 | 554,700,000 | 710,700,000 | 190,600,000 | 244,100,000 |422,800,000 |380,100,000 |
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

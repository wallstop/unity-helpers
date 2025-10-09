# Random Number Generator Performance Benchmarks

This document contains performance benchmarks for the various random number generators included in Unity Helpers.

## Performance (Operations per Second)

<!-- RANDOM_BENCHMARKS_START -->
| Random | NextBool | Next | NextUInt | NextFloat | NextDouble | NextUint - Range | NextInt - Range |
| ------ | -------- | ---- | -------- | --------- | ---------- | ---------------- | --------------- |
| DotNetRandom | 54,900,000 | 55,400,000 | 60,100,000 | 47,500,000 | 48,000,000 |32,900,000 |32,200,000 |
| LinearCongruentialGenerator | 866,600,000 | 866,500,000 | 1,310,200,000 | 186,900,000 | 182,300,000 |67,000,000 |65,400,000 |
| IllusionFlow | 643,700,000 | 643,900,000 | 870,500,000 | 181,000,000 | 176,200,000 |66,300,000 |64,200,000 |
| PcgRandom | 670,300,000 | 672,500,000 | 896,900,000 | 186,600,000 | 181,400,000 |67,000,000 |65,600,000 |
| RomuDuo | 877,100,000 | 812,200,000 | 1,170,700,000 | 188,600,000 | 183,200,000 |67,000,000 |64,800,000 |
| SplitMix64 | 752,200,000 | 761,500,000 | 1,051,800,000 | 188,600,000 | 184,000,000 |67,400,000 |66,000,000 |
| SquirrelRandom | 407,200,000 | 408,400,000 | 413,900,000 | 176,500,000 | 170,800,000 |66,000,000 |64,200,000 |
| SystemRandom | 144,800,000 | 149,200,000 | 64,600,000 | 132,600,000 | 139,700,000 |60,400,000 |57,800,000 |
| UnityRandom | 83,900,000 | 83,900,000 | 86,600,000 | 62,400,000 | 61,700,000 |38,700,000 |38,200,000 |
| WyRandom | 384,300,000 | 384,200,000 | 450,000,000 | 153,100,000 | 165,100,000 |64,600,000 |63,100,000 |
| XorShiftRandom | 756,800,000 | 759,200,000 | 885,400,000 | 188,500,000 | 181,600,000 |67,000,000 |65,500,000 |
| XoroShiroRandom | 740,900,000 | 743,500,000 | 1,063,700,000 | 188,900,000 | 182,600,000 |66,600,000 |65,200,000 |
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

- **For general use**: `PcgRandom` (via `PRNG.Instance`) - Great balance of speed and quality
- **For maximum speed**: `RomuDuo` or `LinearCongruentialGenerator` - Fastest overall
- **For compatibility**: `DotNetRandom` - Uses .NET's built-in Random
- **Avoid for performance**: `UnityRandom` - Significantly slower than alternatives

All benchmarks are run for 1 second per operation type to ensure statistical significance.

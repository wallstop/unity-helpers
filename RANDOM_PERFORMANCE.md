# Random Number Generator Performance Benchmarks

This document contains performance benchmarks for the various random number generators included in Unity Helpers.

## Performance (Operations per Second)

<!-- RANDOM_BENCHMARKS_START -->
| Random | NextBool | Next | NextUInt | NextFloat | NextDouble | NextUint - Range | NextInt - Range |
| ------ | -------- | ---- | -------- | --------- | ---------- | ---------------- | --------------- |
| DotNetRandom | 530,700,000 | 56,100,000 | 58,200,000 | 48,300,000 | 27,400,000 |53,800,000 |53,900,000 |
| LinearCongruentialGenerator | 801,200,000 | 876,000,000 | 1,316,000,000 | 211,100,000 | 407,200,000 |583,600,000 |500,400,000 |
| IllusionFlow | 796,700,000 | 650,400,000 | 879,200,000 | 202,500,000 | 322,200,000 |438,400,000 |389,500,000 |
| PcgRandom | 790,600,000 | 658,000,000 | 910,400,000 | 210,600,000 | 323,600,000 |450,100,000 |406,400,000 |
| RomuDuo | 550,500,000 | 582,300,000 | 749,500,000 | 189,500,000 | 253,800,000 |422,600,000 |390,200,000 |
| SplitMix64 | 794,600,000 | 747,900,000 | 1,061,500,000 | 210,700,000 | 360,200,000 |478,700,000 |438,200,000 |
| SquirrelRandom | 745,700,000 | 394,700,000 | 414,400,000 | 195,100,000 | 186,500,000 |328,300,000 |309,100,000 |
| SystemRandom | 143,000,000 | 148,000,000 | 65,300,000 | 131,300,000 | 137,200,000 |59,700,000 |60,400,000 |
| UnityRandom | 653,100,000 | 85,000,000 | 87,000,000 | 64,100,000 | 41,200,000 |81,500,000 |82,400,000 |
| WyRandom | 758,300,000 | 386,300,000 | 433,000,000 | 185,800,000 | 187,100,000 |295,000,000 |276,000,000 |
| XorShiftRandom | 789,000,000 | 565,900,000 | 600,800,000 | 210,300,000 | 259,400,000 |440,000,000 |386,400,000 |
| XoroShiroRandom | 789,900,000 | 571,500,000 | 752,900,000 | 187,300,000 | 248,600,000 |405,500,000 |376,500,000 |
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

# Random Number Generator Performance Benchmarks

> Auto-generated via RandomPerformanceTests.Benchmark. Run the test to refresh these summary and detail tables.

<!-- RANDOM_BENCHMARKS_START -->

## Summary (fastest first)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Random</th>
      <th align="right">NextUint (ops/s)</th>
      <th align="left">Speed</th>
      <th align="left">Quality</th>
      <th align="left">Notes</th>
    </tr>
  </thead>
  <tbody>
    <tr><td>LinearCongruentialGenerator</td><td align="right">1,333,700,000</td><td>Fastest</td><td>Poor</td><td>Minimal standard LCG; fails spectral tests and exhibits lattice artifacts beyond small dimensions.</td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">1,323,500,000</td><td>Fastest</td><td>Experimental</td><td>Single-word chaotic generator; author notes period 2^64 but provides no formal test results—treat as experimental.</td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">1,068,900,000</td><td>Very Fast</td><td>Good</td><td>Empirical PractRand testing to 32GB shows strong diffusion; designed as a chaotic ARX mixer rather than a proven statistically optimal generator.</td></tr>
    <tr><td>SplitMix64</td><td align="right">1,042,200,000</td><td>Very Fast</td><td>Very Good</td><td>Well-known SplitMix64 mixer; passes TestU01 BigCrush and PractRand up to large data sizes in literature. <a href="https://prng.di.unimi.it/splitmix64.c">Vigna 2014</a></td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">947,500,000</td><td>Fast</td><td>Excellent</td><td>Six-word ARX-style generator tuned for all-around use; passes TestU01 BigCrush per upstream reference implementation. <a href="https://github.com/wileylooper/flurryburst">Will Stafford Parsons (wileylooper)</a></td></tr>
    <tr><td>PcgRandom</td><td align="right">916,300,000</td><td>Fast</td><td>Excellent</td><td>PCG XSH RR 64/32 variant; passes TestU01 BigCrush and PractRand in published results. <a href="https://www.pcg-random.org/paper.html">O&#39;Neill 2014</a></td></tr>
    <tr><td>IllusionFlow</td><td align="right">891,400,000</td><td>Fast</td><td>Excellent</td><td>Hybridized PCG + xorshift design; upstream PractRand 64GB passes with no anomalies per author.</td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">762,900,000</td><td>Fast</td><td>Very Good</td><td>xoshiro128** variant; authors recommend for general-purpose use and report clean BigCrush performance with jump functions. <a href="https://prng.di.unimi.it/xoshiro128starstar.c">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>RomuDuo</td><td align="right">757,900,000</td><td>Fast</td><td>Very Good</td><td>ROMU family member (RomuDuo); authors report strong BigCrush results with minor low-bit weaknesses in some rotations.</td></tr>
    <tr><td>StormDropRandom</td><td align="right">713,500,000</td><td>Moderate</td><td>Excellent</td><td>20-word ARX generator derived from SHISHUA; author reports excellent PractRand performance and long periods.</td></tr>
    <tr><td>XorShiftRandom</td><td align="right">599,800,000</td><td>Moderate</td><td>Fair</td><td>Classic 32-bit xorshift; known to fail portions of TestU01 and PractRand, acceptable for lightweight effects only. <a href="https://www.jstatsoft.org/article/view/v008i14">Marsaglia 2003</a></td></tr>
    <tr><td>WyRandom</td><td align="right">440,100,000</td><td>Slow</td><td>Very Good</td><td>Wyhash-based generator; published testing shows it clears BigCrush/PractRand with wide seed coverage. <a href="https://github.com/wangyi-fudan/wyhash">Wang Yi 2019</a></td></tr>
    <tr><td>SquirrelRandom</td><td align="right">409,000,000</td><td>Slow</td><td>Good</td><td>Hash-based generator built on Squirrel3; good equidistribution for table lookups but not extensively tested beyond moderate ranges. <a href="https://youtu.be/LWFzPP8ZbdU?t=2673">Squirrel Eiserloh</a></td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">260,200,000</td><td>Very Slow</td><td>Excellent</td><td>SHISHUA-inspired generator; independent testing (PractRand 128GB) by author indicates excellent distribution properties.</td></tr>
    <tr><td>UnityRandom</td><td align="right">86,800,000</td><td>Very Slow</td><td>Fair</td><td>Mirrors UnityEngine.Random (Xorshift196 + additive); suitable for legacy compatibility but not high-stakes simulation. <a href="https://blog.unity.com/technology/random-numbers-on-the-gpu">Unity Random Internals</a></td></tr>
    <tr><td>SystemRandom</td><td align="right">64,200,000</td><td>Very Slow</td><td>Poor</td><td>Thin wrapper over System.Random; inherits same LCG weaknesses and fails modern statistical batteries. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
    <tr><td>DotNetRandom</td><td align="right">54,300,000</td><td>Very Slow</td><td>Poor</td><td>Linear congruential generator (mod 2^31) with known correlation failures; unsuitable for high-quality simulations. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
  </tbody>
</table>

## Detailed Metrics

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Random</th>
      <th align="right">NextBool</th>
      <th align="right">Next</th>
      <th align="right">NextUint</th>
      <th align="right">NextFloat</th>
      <th align="right">NextDouble</th>
      <th align="right">NextUint (Range)</th>
      <th align="right">NextInt (Range)</th>
    </tr>
  </thead>
  <tbody>
    <tr><td>LinearCongruentialGenerator</td><td align="right">812,500,000</td><td align="right">844,800,000</td><td align="right">1,333,700,000</td><td align="right">184,600,000</td><td align="right">387,000,000</td><td align="right">580,800,000</td><td align="right">505,400,000</td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">780,800,000</td><td align="right">721,900,000</td><td align="right">1,323,500,000</td><td align="right">182,500,000</td><td align="right">400,700,000</td><td align="right">532,600,000</td><td align="right">463,400,000</td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">785,700,000</td><td align="right">716,200,000</td><td align="right">1,068,900,000</td><td align="right">183,300,000</td><td align="right">353,100,000</td><td align="right">485,400,000</td><td align="right">422,600,000</td></tr>
    <tr><td>SplitMix64</td><td align="right">777,700,000</td><td align="right">774,200,000</td><td align="right">1,042,200,000</td><td align="right">183,200,000</td><td align="right">362,300,000</td><td align="right">484,000,000</td><td align="right">436,100,000</td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">771,200,000</td><td align="right">678,400,000</td><td align="right">947,500,000</td><td align="right">183,500,000</td><td align="right">311,600,000</td><td align="right">445,700,000</td><td align="right">405,200,000</td></tr>
    <tr><td>PcgRandom</td><td align="right">777,500,000</td><td align="right">673,700,000</td><td align="right">916,300,000</td><td align="right">184,200,000</td><td align="right">328,700,000</td><td align="right">454,100,000</td><td align="right">409,400,000</td></tr>
    <tr><td>IllusionFlow</td><td align="right">789,200,000</td><td align="right">641,300,000</td><td align="right">891,400,000</td><td align="right">177,800,000</td><td align="right">309,100,000</td><td align="right">445,700,000</td><td align="right">396,000,000</td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">770,000,000</td><td align="right">592,300,000</td><td align="right">762,900,000</td><td align="right">165,800,000</td><td align="right">242,400,000</td><td align="right">427,700,000</td><td align="right">381,800,000</td></tr>
    <tr><td>RomuDuo</td><td align="right">783,100,000</td><td align="right">592,000,000</td><td align="right">757,900,000</td><td align="right">164,500,000</td><td align="right">249,800,000</td><td align="right">443,500,000</td><td align="right">394,800,000</td></tr>
    <tr><td>StormDropRandom</td><td align="right">776,100,000</td><td align="right">568,200,000</td><td align="right">713,500,000</td><td align="right">181,400,000</td><td align="right">264,100,000</td><td align="right">397,600,000</td><td align="right">364,300,000</td></tr>
    <tr><td>XorShiftRandom</td><td align="right">779,800,000</td><td align="right">587,300,000</td><td align="right">599,800,000</td><td align="right">183,300,000</td><td align="right">272,400,000</td><td align="right">432,200,000</td><td align="right">388,700,000</td></tr>
    <tr><td>WyRandom</td><td align="right">749,900,000</td><td align="right">380,600,000</td><td align="right">440,100,000</td><td align="right">164,700,000</td><td align="right">186,700,000</td><td align="right">293,600,000</td><td align="right">274,300,000</td></tr>
    <tr><td>SquirrelRandom</td><td align="right">759,200,000</td><td align="right">407,200,000</td><td align="right">409,000,000</td><td align="right">169,500,000</td><td align="right">193,400,000</td><td align="right">331,400,000</td><td align="right">311,200,000</td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">701,900,000</td><td align="right">246,500,000</td><td align="right">260,200,000</td><td align="right">118,000,000</td><td align="right">117,000,000</td><td align="right">211,300,000</td><td align="right">204,300,000</td></tr>
    <tr><td>UnityRandom</td><td align="right">623,500,000</td><td align="right">84,100,000</td><td align="right">86,800,000</td><td align="right">61,200,000</td><td align="right">41,000,000</td><td align="right">80,400,000</td><td align="right">81,400,000</td></tr>
    <tr><td>SystemRandom</td><td align="right">147,100,000</td><td align="right">148,100,000</td><td align="right">64,200,000</td><td align="right">130,900,000</td><td align="right">137,700,000</td><td align="right">58,500,000</td><td align="right">56,900,000</td></tr>
    <tr><td>DotNetRandom</td><td align="right">539,800,000</td><td align="right">52,500,000</td><td align="right">54,300,000</td><td align="right">45,800,000</td><td align="right">26,700,000</td><td align="right">53,500,000</td><td align="right">53,000,000</td></tr>
  </tbody>
</table>
<!-- RANDOM_BENCHMARKS_END -->

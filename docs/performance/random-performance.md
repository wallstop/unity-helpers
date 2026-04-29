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
    <tr><td>LinearCongruentialGenerator</td><td align="right">1,323,600,000</td><td data-sort-value="6">Fastest</td><td data-sort-value="5">Poor</td><td>Minimal standard LCG; fails spectral tests and exhibits lattice artifacts beyond small dimensions.</td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">1,171,900,000</td><td data-sort-value="5">Very Fast</td><td data-sort-value="6">Experimental</td><td>Single-word chaotic generator; author notes period 2^64 but provides no formal test results—treat as experimental.</td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">1,054,000,000</td><td data-sort-value="5">Very Fast</td><td data-sort-value="3">Good</td><td>Empirical PractRand testing to 32GB shows strong diffusion; designed as a chaotic ARX mixer rather than a proven statistically optimal generator.</td></tr>
    <tr><td>SplitMix64</td><td align="right">1,044,400,000</td><td data-sort-value="5">Very Fast</td><td data-sort-value="2">Very Good</td><td>Well-known SplitMix64 mixer; passes TestU01 BigCrush and PractRand up to large data sizes in literature. <a href="https://prng.di.unimi.it/splitmix64.c">Vigna 2014</a></td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">923,800,000</td><td data-sort-value="4">Fast</td><td data-sort-value="1">Excellent</td><td>Six-word ARX-style generator tuned for all-around use; passes TestU01 BigCrush per upstream reference implementation. <a href="https://github.com/wileylooper/flurryburst">Will Stafford Parsons (wileylooper)</a></td></tr>
    <tr><td>PcgRandom</td><td align="right">918,200,000</td><td data-sort-value="4">Fast</td><td data-sort-value="1">Excellent</td><td>PCG XSH RR 64/32 variant; passes TestU01 BigCrush and PractRand in published results. <a href="https://www.pcg-random.org/paper.html">O&#39;Neill 2014</a></td></tr>
    <tr><td>IllusionFlow</td><td align="right">844,500,000</td><td data-sort-value="4">Fast</td><td data-sort-value="1">Excellent</td><td>Hybridized PCG + xorshift design; upstream PractRand 64GB passes with no anomalies per author.</td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">756,600,000</td><td data-sort-value="4">Fast</td><td data-sort-value="2">Very Good</td><td>xoshiro128** variant; authors recommend for general-purpose use and report clean BigCrush performance with jump functions. <a href="https://prng.di.unimi.it/xoshiro128starstar.c">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>RomuDuo</td><td align="right">746,400,000</td><td data-sort-value="4">Fast</td><td data-sort-value="2">Very Good</td><td>ROMU family member (RomuDuo); authors report strong BigCrush results with minor low-bit weaknesses in some rotations.</td></tr>
    <tr><td>StormDropRandom</td><td align="right">712,800,000</td><td data-sort-value="3">Moderate</td><td data-sort-value="1">Excellent</td><td>20-word ARX generator derived from SHISHUA; author reports excellent PractRand performance and long periods.</td></tr>
    <tr><td>XorShiftRandom</td><td align="right">594,200,000</td><td data-sort-value="3">Moderate</td><td data-sort-value="4">Fair</td><td>Classic 32-bit xorshift; known to fail portions of TestU01 and PractRand, acceptable for lightweight effects only. <a href="https://doi.org/10.18637/jss.v008.i14">Marsaglia 2003</a></td></tr>
    <tr><td>WyRandom</td><td align="right">446,800,000</td><td data-sort-value="2">Slow</td><td data-sort-value="2">Very Good</td><td>Wyhash-based generator; published testing shows it clears BigCrush/PractRand with wide seed coverage. <a href="https://github.com/wangyi-fudan/wyhash">Wang Yi 2019</a></td></tr>
    <tr><td>SquirrelRandom</td><td align="right">407,100,000</td><td data-sort-value="2">Slow</td><td data-sort-value="3">Good</td><td>Hash-based generator built on Squirrel3; good equidistribution for table lookups but not extensively tested beyond moderate ranges. <a href="https://youtu.be/LWFzPP8ZbdU?t=2673">Squirrel Eiserloh</a></td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">260,100,000</td><td data-sort-value="1">Very Slow</td><td data-sort-value="1">Excellent</td><td>SHISHUA-inspired generator; independent testing (PractRand 128GB) by author indicates excellent distribution properties.</td></tr>
    <tr><td>UnityRandom</td><td align="right">86,400,000</td><td data-sort-value="1">Very Slow</td><td data-sort-value="4">Fair</td><td>Mirrors UnityEngine.Random (Xorshift196 + additive); suitable for legacy compatibility but not high-stakes simulation. <a href="https://unity.com/blog/technology/random-numbers-on-the-gpu">Unity Random Internals</a></td></tr>
    <tr><td>SystemRandom</td><td align="right">64,100,000</td><td data-sort-value="1">Very Slow</td><td data-sort-value="5">Poor</td><td>Thin wrapper over System.Random; inherits same LCG weaknesses and fails modern statistical batteries. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
    <tr><td>DotNetRandom</td><td align="right">57,200,000</td><td data-sort-value="1">Very Slow</td><td data-sort-value="5">Poor</td><td>Linear congruential generator (mod 2^31) with known correlation failures; unsuitable for high-quality simulations. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
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
    <tr><td>LinearCongruentialGenerator</td><td align="right">783,800,000</td><td align="right">833,800,000</td><td align="right">1,323,600,000</td><td align="right">182,100,000</td><td align="right">375,300,000</td><td align="right">583,500,000</td><td align="right">499,500,000</td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">784,400,000</td><td align="right">807,500,000</td><td align="right">1,171,900,000</td><td align="right">181,900,000</td><td align="right">405,500,000</td><td align="right">530,000,000</td><td align="right">461,200,000</td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">775,200,000</td><td align="right">703,900,000</td><td align="right">1,054,000,000</td><td align="right">181,500,000</td><td align="right">360,500,000</td><td align="right">476,600,000</td><td align="right">422,300,000</td></tr>
    <tr><td>SplitMix64</td><td align="right">737,500,000</td><td align="right">705,400,000</td><td align="right">1,044,400,000</td><td align="right">181,900,000</td><td align="right">372,700,000</td><td align="right">482,400,000</td><td align="right">441,800,000</td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">766,300,000</td><td align="right">678,200,000</td><td align="right">923,800,000</td><td align="right">181,300,000</td><td align="right">336,700,000</td><td align="right">449,700,000</td><td align="right">399,800,000</td></tr>
    <tr><td>PcgRandom</td><td align="right">773,200,000</td><td align="right">665,800,000</td><td align="right">918,200,000</td><td align="right">181,600,000</td><td align="right">313,100,000</td><td align="right">450,000,000</td><td align="right">406,300,000</td></tr>
    <tr><td>IllusionFlow</td><td align="right">774,400,000</td><td align="right">629,900,000</td><td align="right">844,500,000</td><td align="right">174,800,000</td><td align="right">294,200,000</td><td align="right">439,100,000</td><td align="right">391,200,000</td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">759,600,000</td><td align="right">583,400,000</td><td align="right">756,600,000</td><td align="right">164,600,000</td><td align="right">252,100,000</td><td align="right">420,900,000</td><td align="right">376,000,000</td></tr>
    <tr><td>RomuDuo</td><td align="right">777,000,000</td><td align="right">586,100,000</td><td align="right">746,400,000</td><td align="right">164,800,000</td><td align="right">252,300,000</td><td align="right">438,900,000</td><td align="right">390,400,000</td></tr>
    <tr><td>StormDropRandom</td><td align="right">755,200,000</td><td align="right">569,300,000</td><td align="right">712,800,000</td><td align="right">180,600,000</td><td align="right">270,300,000</td><td align="right">396,400,000</td><td align="right">359,100,000</td></tr>
    <tr><td>XorShiftRandom</td><td align="right">785,800,000</td><td align="right">583,100,000</td><td align="right">594,200,000</td><td align="right">181,600,000</td><td align="right">285,900,000</td><td align="right">434,400,000</td><td align="right">387,700,000</td></tr>
    <tr><td>WyRandom</td><td align="right">739,600,000</td><td align="right">385,100,000</td><td align="right">446,800,000</td><td align="right">164,200,000</td><td align="right">183,800,000</td><td align="right">291,200,000</td><td align="right">277,500,000</td></tr>
    <tr><td>SquirrelRandom</td><td align="right">742,300,000</td><td align="right">405,300,000</td><td align="right">407,100,000</td><td align="right">169,800,000</td><td align="right">199,100,000</td><td align="right">329,600,000</td><td align="right">310,900,000</td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">693,800,000</td><td align="right">247,600,000</td><td align="right">260,100,000</td><td align="right">118,900,000</td><td align="right">119,200,000</td><td align="right">209,800,000</td><td align="right">203,300,000</td></tr>
    <tr><td>UnityRandom</td><td align="right">632,700,000</td><td align="right">83,600,000</td><td align="right">86,400,000</td><td align="right">61,400,000</td><td align="right">40,800,000</td><td align="right">80,400,000</td><td align="right">81,100,000</td></tr>
    <tr><td>SystemRandom</td><td align="right">142,100,000</td><td align="right">145,400,000</td><td align="right">64,100,000</td><td align="right">130,800,000</td><td align="right">130,900,000</td><td align="right">59,000,000</td><td align="right">59,000,000</td></tr>
    <tr><td>DotNetRandom</td><td align="right">518,800,000</td><td align="right">54,400,000</td><td align="right">57,200,000</td><td align="right">46,100,000</td><td align="right">26,900,000</td><td align="right">53,800,000</td><td align="right">52,800,000</td></tr>
  </tbody>
</table>
<!-- RANDOM_BENCHMARKS_END -->

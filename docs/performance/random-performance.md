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
    <tr><td>LinearCongruentialGenerator</td><td align="right">1,338,500,000</td><td>Fastest</td><td>Poor</td><td>Minimal standard LCG; fails spectral tests and exhibits lattice artifacts beyond small dimensions.</td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">1,311,200,000</td><td>Fastest</td><td>Experimental</td><td>Single-word chaotic generator; author notes period 2^64 but provides no formal test results—treat as experimental.</td></tr>
    <tr><td>SplitMix64</td><td align="right">1,065,500,000</td><td>Very Fast</td><td>Very Good</td><td>Well-known SplitMix64 mixer; passes TestU01 BigCrush and PractRand up to large data sizes in literature. <a href="https://prng.di.unimi.it/splitmix64.c">Vigna 2014</a></td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">1,063,400,000</td><td>Very Fast</td><td>Good</td><td>Empirical PractRand testing to 32GB shows strong diffusion; designed as a chaotic ARX mixer rather than a proven statistically optimal generator.</td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">943,000,000</td><td>Fast</td><td>Excellent</td><td>Six-word ARX-style generator tuned for all-around use; passes TestU01 BigCrush per upstream reference implementation. <a href="https://github.com/wileylooper/flurryburst">Will Stafford Parsons (wileylooper)</a></td></tr>
    <tr><td>PcgRandom</td><td align="right">919,000,000</td><td>Fast</td><td>Excellent</td><td>PCG XSH RR 64/32 variant; passes TestU01 BigCrush and PractRand in published results. <a href="https://www.pcg-random.org/paper.html">O&#39;Neill 2014</a></td></tr>
    <tr><td>IllusionFlow</td><td align="right">849,900,000</td><td>Fast</td><td>Excellent</td><td>Hybridized PCG + xorshift design; upstream PractRand 64GB passes with no anomalies per author.</td></tr>
    <tr><td>RomuDuo</td><td align="right">759,500,000</td><td>Fast</td><td>Very Good</td><td>ROMU family member (RomuDuo); authors report strong BigCrush results with minor low-bit weaknesses in some rotations.</td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">742,500,000</td><td>Fast</td><td>Very Good</td><td>xoshiro128** variant; authors recommend for general-purpose use and report clean BigCrush performance with jump functions. <a href="https://prng.di.unimi.it/xoshiro128starstar.c">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>StormDropRandom</td><td align="right">721,400,000</td><td>Moderate</td><td>Excellent</td><td>20-word ARX generator derived from SHISHUA; author reports excellent PractRand performance and long periods.</td></tr>
    <tr><td>XorShiftRandom</td><td align="right">601,900,000</td><td>Moderate</td><td>Fair</td><td>Classic 32-bit xorshift; known to fail portions of TestU01 and PractRand, acceptable for lightweight effects only. <a href="https://www.jstatsoft.org/article/view/v008i14">Marsaglia 2003</a></td></tr>
    <tr><td>WyRandom</td><td align="right">452,700,000</td><td>Slow</td><td>Very Good</td><td>Wyhash-based generator; published testing shows it clears BigCrush/PractRand with wide seed coverage. <a href="https://github.com/wangyi-fudan/wyhash">Wang Yi 2019</a></td></tr>
    <tr><td>SquirrelRandom</td><td align="right">414,300,000</td><td>Slow</td><td>Good</td><td>Hash-based generator built on Squirrel3; good equidistribution for table lookups but not extensively tested beyond moderate ranges. <a href="https://youtu.be/LWFzPP8ZbdU?t=2673">Squirrel Eiserloh</a></td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">268,800,000</td><td>Slow</td><td>Excellent</td><td>SHISHUA-inspired generator; independent testing (PractRand 128GB) by author indicates excellent distribution properties.</td></tr>
    <tr><td>UnityRandom</td><td align="right">87,100,000</td><td>Very Slow</td><td>Fair</td><td>Mirrors UnityEngine.Random (Xorshift196 + additive); suitable for legacy compatibility but not high-stakes simulation. <a href="https://blog.unity.com/technology/random-numbers-on-the-gpu">Unity Random Internals</a></td></tr>
    <tr><td>SystemRandom</td><td align="right">65,600,000</td><td>Very Slow</td><td>Poor</td><td>Thin wrapper over System.Random; inherits same LCG weaknesses and fails modern statistical batteries. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
    <tr><td>DotNetRandom</td><td align="right">56,600,000</td><td>Very Slow</td><td>Poor</td><td>Linear congruential generator (mod 2^31) with known correlation failures; unsuitable for high-quality simulations. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
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
    <tr><td>LinearCongruentialGenerator</td><td align="right">791,900,000</td><td align="right">835,000,000</td><td align="right">1,338,500,000</td><td align="right">184,400,000</td><td align="right">381,500,000</td><td align="right">589,100,000</td><td align="right">505,400,000</td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">792,100,000</td><td align="right">819,300,000</td><td align="right">1,311,200,000</td><td align="right">184,500,000</td><td align="right">411,000,000</td><td align="right">532,900,000</td><td align="right">464,100,000</td></tr>
    <tr><td>SplitMix64</td><td align="right">794,900,000</td><td align="right">741,600,000</td><td align="right">1,065,500,000</td><td align="right">183,400,000</td><td align="right">352,600,000</td><td align="right">488,100,000</td><td align="right">445,800,000</td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">785,000,000</td><td align="right">656,700,000</td><td align="right">1,063,400,000</td><td align="right">182,600,000</td><td align="right">360,100,000</td><td align="right">486,400,000</td><td align="right">424,300,000</td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">786,000,000</td><td align="right">649,300,000</td><td align="right">943,000,000</td><td align="right">180,500,000</td><td align="right">287,500,000</td><td align="right">455,200,000</td><td align="right">408,400,000</td></tr>
    <tr><td>PcgRandom</td><td align="right">783,900,000</td><td align="right">654,600,000</td><td align="right">919,000,000</td><td align="right">184,300,000</td><td align="right">318,600,000</td><td align="right">454,500,000</td><td align="right">410,100,000</td></tr>
    <tr><td>IllusionFlow</td><td align="right">778,300,000</td><td align="right">640,900,000</td><td align="right">849,900,000</td><td align="right">178,100,000</td><td align="right">296,300,000</td><td align="right">446,000,000</td><td align="right">395,800,000</td></tr>
    <tr><td>RomuDuo</td><td align="right">786,300,000</td><td align="right">590,000,000</td><td align="right">759,500,000</td><td align="right">167,200,000</td><td align="right">254,700,000</td><td align="right">443,500,000</td><td align="right">396,200,000</td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">766,900,000</td><td align="right">563,100,000</td><td align="right">742,500,000</td><td align="right">166,500,000</td><td align="right">243,800,000</td><td align="right">426,700,000</td><td align="right">381,400,000</td></tr>
    <tr><td>StormDropRandom</td><td align="right">758,200,000</td><td align="right">531,300,000</td><td align="right">721,400,000</td><td align="right">182,100,000</td><td align="right">258,900,000</td><td align="right">394,700,000</td><td align="right">338,100,000</td></tr>
    <tr><td>XorShiftRandom</td><td align="right">786,200,000</td><td align="right">593,500,000</td><td align="right">601,900,000</td><td align="right">184,300,000</td><td align="right">288,000,000</td><td align="right">440,400,000</td><td align="right">393,100,000</td></tr>
    <tr><td>WyRandom</td><td align="right">751,000,000</td><td align="right">387,500,000</td><td align="right">452,700,000</td><td align="right">166,400,000</td><td align="right">189,300,000</td><td align="right">297,400,000</td><td align="right">281,700,000</td></tr>
    <tr><td>SquirrelRandom</td><td align="right">753,500,000</td><td align="right">409,100,000</td><td align="right">414,300,000</td><td align="right">172,300,000</td><td align="right">202,200,000</td><td align="right">328,800,000</td><td align="right">313,300,000</td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">708,700,000</td><td align="right">256,400,000</td><td align="right">268,800,000</td><td align="right">121,100,000</td><td align="right">123,500,000</td><td align="right">216,300,000</td><td align="right">210,800,000</td></tr>
    <tr><td>UnityRandom</td><td align="right">647,000,000</td><td align="right">85,000,000</td><td align="right">87,100,000</td><td align="right">62,200,000</td><td align="right">41,500,000</td><td align="right">81,700,000</td><td align="right">82,400,000</td></tr>
    <tr><td>SystemRandom</td><td align="right">145,300,000</td><td align="right">147,400,000</td><td align="right">65,600,000</td><td align="right">131,500,000</td><td align="right">140,100,000</td><td align="right">60,100,000</td><td align="right">60,500,000</td></tr>
    <tr><td>DotNetRandom</td><td align="right">522,600,000</td><td align="right">54,600,000</td><td align="right">56,600,000</td><td align="right">46,200,000</td><td align="right">27,000,000</td><td align="right">53,900,000</td><td align="right">54,000,000</td></tr>
  </tbody>
</table>
<!-- RANDOM_BENCHMARKS_END -->

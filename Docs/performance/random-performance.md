# Random Number Generator Performance Benchmarks

> Auto-generated via RandomPerformanceTests.Benchmark. Run the test to refresh these summary and detail tables.

<!-- RANDOM_BENCHMARKS_START -->

## Summary (fastest first)

<table>
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
    <tr><td>LinearCongruentialGenerator</td><td align="right">1,316,600,000</td><td>Fastest</td><td>Poor</td><td>Minimal standard LCG; fails spectral tests and exhibits lattice artifacts beyond small dimensions. <a href="https://doi.org/10.1145/63039.63042">Park &amp; Miller 1988</a></td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">1,166,100,000</td><td>Very Fast</td><td>Experimental</td><td>Single-word chaotic generator; author notes period 2^64 but provides no formal test results—treat as experimental. <a href="https://github.com/wileylooper/wavesplat">wileylooper/wavesplat</a></td></tr>
    <tr><td>SplitMix64</td><td align="right">1,044,000,000</td><td>Very Fast</td><td>Very Good</td><td>Well-known SplitMix64 mixer; passes TestU01 BigCrush and PractRand up to large data sizes in literature. <a href="http://xoshiro.di.unimi.it/splitmix64.c">Vigna 2014</a></td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">1,042,900,000</td><td>Very Fast</td><td>Good</td><td>Empirical PractRand testing to 32GB shows strong diffusion; designed as a chaotic ARX mixer rather than a proven statistically optimal generator. <a href="https://github.com/wileylooper/blastcircuit">wileylooper/blastcircuit</a></td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">923,100,000</td><td>Fast</td><td>Excellent</td><td>Hybrid Xoshiro/PCG variant tuned for all-around use; passes TestU01 BigCrush per upstream reference implementation. <a href="http://xoshiro.di.unimi.it">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>PcgRandom</td><td align="right">903,900,000</td><td>Fast</td><td>Excellent</td><td>PCG XSH RR 64/32 variant; passes TestU01 BigCrush and PractRand in published results. <a href="https://www.pcg-random.org/paper.html">O&#39;Neill 2014</a></td></tr>
    <tr><td>IllusionFlow</td><td align="right">875,000,000</td><td>Fast</td><td>Excellent</td><td>Hybridized PCG + xorshift design; upstream PractRand 64GB passes with no anomalies per author. <a href="https://github.com/wileylooper/illusionflow">wileylooper/illusionflow</a></td></tr>
    <tr><td>RomuDuo</td><td align="right">746,800,000</td><td>Fast</td><td>Very Good</td><td>ROMU family member (RomuDuo); authors report strong BigCrush results with minor low-bit weaknesses in some rotations. <a href="https://romu-random.org/">Markus &amp; Crow 2019</a></td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">746,800,000</td><td>Fast</td><td>Very Good</td><td>xoshiro128** variant; authors recommend for general-purpose use and report clean BigCrush performance with jump functions. <a href="http://xoshiro.di.unimi.it/xoshiro128starstar.c">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>StormDropRandom</td><td align="right">707,600,000</td><td>Moderate</td><td>Excellent</td><td>20-word ARX generator derived from SHISHUA; author reports excellent PractRand performance and long periods. <a href="https://github.com/wileylooper/stormdrop">wileylooper/stormdrop</a></td></tr>
    <tr><td>XorShiftRandom</td><td align="right">592,000,000</td><td>Moderate</td><td>Fair</td><td>Classic 32-bit xorshift; known to fail portions of TestU01 and PractRand, acceptable for lightweight effects only. <a href="https://www.jstatsoft.org/article/view/v008i14">Marsaglia 2003</a></td></tr>
    <tr><td>WyRandom</td><td align="right">444,900,000</td><td>Slow</td><td>Very Good</td><td>Wyhash-based generator; published testing shows it clears BigCrush/PractRand with wide seed coverage. <a href="https://github.com/wangyi-fudan/wyhash">Wang Yi 2019</a></td></tr>
    <tr><td>SquirrelRandom</td><td align="right">409,200,000</td><td>Slow</td><td>Good</td><td>Hash-based generator built on Squirrel3; good equidistribution for table lookups but not extensively tested beyond moderate ranges. <a href="https://github.com/squirrel-org/squirrel3">Squirrel3</a></td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">261,500,000</td><td>Very Slow</td><td>Excellent</td><td>SHISHUA-inspired generator; independent testing (PractRand 128GB) by author indicates excellent distribution properties. <a href="https://github.com/wileylooper/photonspin">wileylooper/photonspin</a></td></tr>
    <tr><td>UnityRandom</td><td align="right">86,300,000</td><td>Very Slow</td><td>Fair</td><td>Mirrors UnityEngine.Random (Xorshift196 + additive); suitable for legacy compatibility but not high-stakes simulation. <a href="https://blog.unity.com/technology/random-numbers-on-the-gpu">Unity Random Internals</a></td></tr>
    <tr><td>SystemRandom</td><td align="right">64,100,000</td><td>Very Slow</td><td>Poor</td><td>Thin wrapper over System.Random; inherits same LCG weaknesses and fails modern statistical batteries. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
    <tr><td>DotNetRandom</td><td align="right">54,500,000</td><td>Very Slow</td><td>Poor</td><td>Linear congruential generator (mod 2^31) with known correlation failures; unsuitable for high-quality simulations. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
  </tbody>
</table>

## Detailed Metrics

<table>
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
    <tr><td>LinearCongruentialGenerator</td><td align="right">796,900,000</td><td align="right">805,600,000</td><td align="right">1,316,600,000</td><td align="right">187,900,000</td><td align="right">405,000,000</td><td align="right">582,700,000</td><td align="right">501,100,000</td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">772,300,000</td><td align="right">746,600,000</td><td align="right">1,166,100,000</td><td align="right">187,800,000</td><td align="right">383,500,000</td><td align="right">525,200,000</td><td align="right">457,500,000</td></tr>
    <tr><td>SplitMix64</td><td align="right">775,600,000</td><td align="right">698,600,000</td><td align="right">1,044,000,000</td><td align="right">187,800,000</td><td align="right">351,000,000</td><td align="right">479,000,000</td><td align="right">438,100,000</td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">747,100,000</td><td align="right">681,600,000</td><td align="right">1,042,900,000</td><td align="right">186,800,000</td><td align="right">337,900,000</td><td align="right">477,500,000</td><td align="right">420,600,000</td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">773,800,000</td><td align="right">647,700,000</td><td align="right">923,100,000</td><td align="right">187,400,000</td><td align="right">325,000,000</td><td align="right">447,100,000</td><td align="right">402,900,000</td></tr>
    <tr><td>PcgRandom</td><td align="right">776,700,000</td><td align="right">677,000,000</td><td align="right">903,900,000</td><td align="right">187,700,000</td><td align="right">339,300,000</td><td align="right">448,300,000</td><td align="right">405,100,000</td></tr>
    <tr><td>IllusionFlow</td><td align="right">779,600,000</td><td align="right">645,400,000</td><td align="right">875,000,000</td><td align="right">181,200,000</td><td align="right">322,300,000</td><td align="right">438,700,000</td><td align="right">389,600,000</td></tr>
    <tr><td>RomuDuo</td><td align="right">769,200,000</td><td align="right">583,100,000</td><td align="right">746,800,000</td><td align="right">169,700,000</td><td align="right">251,300,000</td><td align="right">437,900,000</td><td align="right">388,700,000</td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">764,300,000</td><td align="right">570,200,000</td><td align="right">746,800,000</td><td align="right">169,700,000</td><td align="right">249,100,000</td><td align="right">418,600,000</td><td align="right">374,300,000</td></tr>
    <tr><td>StormDropRandom</td><td align="right">757,900,000</td><td align="right">544,600,000</td><td align="right">707,600,000</td><td align="right">186,800,000</td><td align="right">264,800,000</td><td align="right">395,600,000</td><td align="right">358,200,000</td></tr>
    <tr><td>XorShiftRandom</td><td align="right">775,100,000</td><td align="right">558,900,000</td><td align="right">592,000,000</td><td align="right">187,500,000</td><td align="right">255,400,000</td><td align="right">433,500,000</td><td align="right">385,400,000</td></tr>
    <tr><td>WyRandom</td><td align="right">736,100,000</td><td align="right">377,200,000</td><td align="right">444,900,000</td><td align="right">169,100,000</td><td align="right">185,000,000</td><td align="right">291,000,000</td><td align="right">275,200,000</td></tr>
    <tr><td>SquirrelRandom</td><td align="right">741,800,000</td><td align="right">390,600,000</td><td align="right">409,200,000</td><td align="right">175,100,000</td><td align="right">187,100,000</td><td align="right">322,500,000</td><td align="right">306,600,000</td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">692,500,000</td><td align="right">242,000,000</td><td align="right">261,500,000</td><td align="right">121,600,000</td><td align="right">117,900,000</td><td align="right">213,700,000</td><td align="right">204,100,000</td></tr>
    <tr><td>UnityRandom</td><td align="right">640,400,000</td><td align="right">83,400,000</td><td align="right">86,300,000</td><td align="right">62,100,000</td><td align="right">40,900,000</td><td align="right">80,200,000</td><td align="right">80,700,000</td></tr>
    <tr><td>SystemRandom</td><td align="right">140,900,000</td><td align="right">145,500,000</td><td align="right">64,100,000</td><td align="right">129,300,000</td><td align="right">135,000,000</td><td align="right">58,700,000</td><td align="right">59,300,000</td></tr>
    <tr><td>DotNetRandom</td><td align="right">530,300,000</td><td align="right">50,700,000</td><td align="right">54,500,000</td><td align="right">45,000,000</td><td align="right">27,400,000</td><td align="right">54,400,000</td><td align="right">54,200,000</td></tr>
  </tbody>
</table>
<!-- RANDOM_BENCHMARKS_END -->

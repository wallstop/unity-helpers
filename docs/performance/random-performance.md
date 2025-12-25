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
    <tr><td>LinearCongruentialGenerator</td><td align="right">1,324,800,000</td><td>Fastest</td><td>Poor</td><td>Minimal standard LCG; fails spectral tests and exhibits lattice artifacts beyond small dimensions. <a href="https://doi.org/10.1145/63039.63042">Park &amp; Miller 1988</a></td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">1,314,800,000</td><td>Fastest</td><td>Experimental</td><td>Single-word chaotic generator; author notes period 2^64 but provides no formal test results—treat as experimental. Will Stafford Parsons (repo offline)</td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">1,062,200,000</td><td>Very Fast</td><td>Good</td><td>Empirical PractRand testing to 32GB shows strong diffusion; designed as a chaotic ARX mixer rather than a proven statistically optimal generator. Will Stafford Parsons (repo offline)</td></tr>
    <tr><td>SplitMix64</td><td align="right">1,045,700,000</td><td>Very Fast</td><td>Very Good</td><td>Well-known SplitMix64 mixer; passes TestU01 BigCrush and PractRand up to large data sizes in literature. <a href="http://xoshiro.di.unimi.it/splitmix64.c">Vigna 2014</a></td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">943,000,000</td><td>Fast</td><td>Excellent</td><td>Hybrid Xoshiro/PCG variant tuned for all-around use; passes TestU01 BigCrush per upstream reference implementation. <a href="http://xoshiro.di.unimi.it">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>PcgRandom</td><td align="right">913,300,000</td><td>Fast</td><td>Excellent</td><td>PCG XSH RR 64/32 variant; passes TestU01 BigCrush and PractRand in published results. <a href="https://www.pcg-random.org/paper.html">O&#39;Neill 2014</a></td></tr>
    <tr><td>IllusionFlow</td><td align="right">879,200,000</td><td>Fast</td><td>Excellent</td><td>Hybridized PCG + xorshift design; upstream PractRand 64GB passes with no anomalies per author. Will Stafford Parsons (repo offline)</td></tr>
    <tr><td>RomuDuo</td><td align="right">756,600,000</td><td>Fast</td><td>Very Good</td><td>ROMU family member (RomuDuo); authors report strong BigCrush results with minor low-bit weaknesses in some rotations. <a href="https://web.archive.org/web/20240101000000*/https://romu-random.org/">Markus &amp; Crow 2019</a></td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">714,100,000</td><td>Moderate</td><td>Very Good</td><td>xoshiro128** variant; authors recommend for general-purpose use and report clean BigCrush performance with jump functions. <a href="http://xoshiro.di.unimi.it/xoshiro128starstar.c">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>StormDropRandom</td><td align="right">707,500,000</td><td>Moderate</td><td>Excellent</td><td>20-word ARX generator derived from SHISHUA; author reports excellent PractRand performance and long periods. Will Stafford Parsons (repo offline)</td></tr>
    <tr><td>XorShiftRandom</td><td align="right">595,500,000</td><td>Moderate</td><td>Fair</td><td>Classic 32-bit xorshift; known to fail portions of TestU01 and PractRand, acceptable for lightweight effects only. <a href="https://www.jstatsoft.org/article/view/v008i14">Marsaglia 2003</a></td></tr>
    <tr><td>WyRandom</td><td align="right">452,400,000</td><td>Slow</td><td>Very Good</td><td>Wyhash-based generator; published testing shows it clears BigCrush/PractRand with wide seed coverage. <a href="https://github.com/wangyi-fudan/wyhash">Wang Yi 2019</a></td></tr>
    <tr><td>SquirrelRandom</td><td align="right">410,400,000</td><td>Slow</td><td>Good</td><td>Hash-based generator built on Squirrel3; good equidistribution for table lookups but not extensively tested beyond moderate ranges. <a href="https://youtu.be/LWFzPP8ZbdU?t=2673">Squirrel Eiserloh GDC 2017</a></td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">265,100,000</td><td>Slow</td><td>Excellent</td><td>SHISHUA-inspired generator; independent testing (PractRand 128GB) by author indicates excellent distribution properties. Will Stafford Parsons (repo offline)</td></tr>
    <tr><td>UnityRandom</td><td align="right">86,800,000</td><td>Very Slow</td><td>Fair</td><td>Mirrors UnityEngine.Random (Xorshift196 + additive); suitable for legacy compatibility but not high-stakes simulation. <a href="https://blog.unity.com/technology/random-numbers-on-the-gpu">Unity Random Internals</a></td></tr>
    <tr><td>SystemRandom</td><td align="right">64,500,000</td><td>Very Slow</td><td>Poor</td><td>Thin wrapper over System.Random; inherits same LCG weaknesses and fails modern statistical batteries. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
    <tr><td>DotNetRandom</td><td align="right">57,600,000</td><td>Very Slow</td><td>Poor</td><td>Linear congruential generator (mod 2^31) with known correlation failures; unsuitable for high-quality simulations. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
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
    <tr><td>LinearCongruentialGenerator</td><td align="right">778,800,000</td><td align="right">877,500,000</td><td align="right">1,324,800,000</td><td align="right">183,100,000</td><td align="right">388,100,000</td><td align="right">583,000,000</td><td align="right">502,900,000</td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">783,500,000</td><td align="right">750,500,000</td><td align="right">1,314,800,000</td><td align="right">182,000,000</td><td align="right">397,000,000</td><td align="right">530,500,000</td><td align="right">461,100,000</td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">777,300,000</td><td align="right">620,600,000</td><td align="right">1,062,200,000</td><td align="right">182,700,000</td><td align="right">354,100,000</td><td align="right">481,700,000</td><td align="right">424,000,000</td></tr>
    <tr><td>SplitMix64</td><td align="right">754,800,000</td><td align="right">624,900,000</td><td align="right">1,045,700,000</td><td align="right">182,600,000</td><td align="right">358,800,000</td><td align="right">482,600,000</td><td align="right">441,500,000</td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">781,200,000</td><td align="right">626,300,000</td><td align="right">943,000,000</td><td align="right">182,400,000</td><td align="right">305,300,000</td><td align="right">452,800,000</td><td align="right">408,100,000</td></tr>
    <tr><td>PcgRandom</td><td align="right">781,600,000</td><td align="right">639,100,000</td><td align="right">913,300,000</td><td align="right">181,700,000</td><td align="right">325,900,000</td><td align="right">448,800,000</td><td align="right">406,600,000</td></tr>
    <tr><td>IllusionFlow</td><td align="right">776,600,000</td><td align="right">589,000,000</td><td align="right">879,200,000</td><td align="right">175,700,000</td><td align="right">308,100,000</td><td align="right">442,700,000</td><td align="right">391,600,000</td></tr>
    <tr><td>RomuDuo</td><td align="right">781,000,000</td><td align="right">590,000,000</td><td align="right">756,600,000</td><td align="right">165,800,000</td><td align="right">251,800,000</td><td align="right">440,500,000</td><td align="right">393,800,000</td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">775,800,000</td><td align="right">519,600,000</td><td align="right">714,100,000</td><td align="right">164,700,000</td><td align="right">239,700,000</td><td align="right">419,100,000</td><td align="right">378,400,000</td></tr>
    <tr><td>StormDropRandom</td><td align="right">768,100,000</td><td align="right">546,000,000</td><td align="right">707,500,000</td><td align="right">181,900,000</td><td align="right">264,600,000</td><td align="right">399,400,000</td><td align="right">362,000,000</td></tr>
    <tr><td>XorShiftRandom</td><td align="right">787,000,000</td><td align="right">587,400,000</td><td align="right">595,500,000</td><td align="right">182,700,000</td><td align="right">264,600,000</td><td align="right">477,100,000</td><td align="right">422,600,000</td></tr>
    <tr><td>WyRandom</td><td align="right">770,600,000</td><td align="right">387,800,000</td><td align="right">452,400,000</td><td align="right">164,700,000</td><td align="right">185,600,000</td><td align="right">294,600,000</td><td align="right">277,800,000</td></tr>
    <tr><td>SquirrelRandom</td><td align="right">771,000,000</td><td align="right">406,400,000</td><td align="right">410,400,000</td><td align="right">169,200,000</td><td align="right">189,000,000</td><td align="right">361,500,000</td><td align="right">337,200,000</td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">700,300,000</td><td align="right">240,900,000</td><td align="right">265,100,000</td><td align="right">120,000,000</td><td align="right">122,100,000</td><td align="right">219,000,000</td><td align="right">200,500,000</td></tr>
    <tr><td>UnityRandom</td><td align="right">647,900,000</td><td align="right">84,300,000</td><td align="right">86,800,000</td><td align="right">61,200,000</td><td align="right">41,200,000</td><td align="right">81,400,000</td><td align="right">82,200,000</td></tr>
    <tr><td>SystemRandom</td><td align="right">147,200,000</td><td align="right">145,700,000</td><td align="right">64,500,000</td><td align="right">131,100,000</td><td align="right">136,900,000</td><td align="right">59,300,000</td><td align="right">59,100,000</td></tr>
    <tr><td>DotNetRandom</td><td align="right">539,800,000</td><td align="right">54,900,000</td><td align="right">57,600,000</td><td align="right">46,900,000</td><td align="right">27,300,000</td><td align="right">54,500,000</td><td align="right">54,200,000</td></tr>
  </tbody>
</table>
<!-- RANDOM_BENCHMARKS_END -->
